using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetKlinik.Data;
using VetKlinik.Models;
using VetKlinik.Services;

namespace VetKlinik.Api;

public static class ApiAuth { public const string Schemes = "Identity.Application,McpToken"; }

public record ServiceDto(int Id, string Name, int DurationMinutes, decimal Price);
public record VetDto(int Id, string FullName, string? Specialties, List<string> Services);
public record PetDto(int Id, string Name, string Species, string? Breed, DateOnly BirthDate, decimal WeightKg);
public record AppointmentDto(int Id, string Pet, string Service, string Vet, DateTime Start, DateTime End, decimal Price, string Status, string? RejectReason);
public record RevenueDto(string Service, int Count, decimal Total);

public class CreateAppointmentRequest
{
    [Range(1, int.MaxValue)] public int PetId { get; set; }
    [Range(1, int.MaxValue)] public int ClinicServiceId { get; set; }
    [Range(1, int.MaxValue)] public int VetId { get; set; }
    [Required] public DateTime? Start { get; set; }
}
public class StatusRequest
{
    [Required] public string Status { get; set; } = "";
    public string? RejectReason { get; set; }
}

[ApiController]
[Route("api")]
[Authorize(AuthenticationSchemes = ApiAuth.Schemes)]
public class VetApiController(AppDbContext db, IBookingService booking) : ControllerBase
{
    string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    bool Staff => User.IsInRole("Admin") || User.IsInRole("Veteriner");

    IQueryable<Appointment> ScopedAppointments()
    {
        var uid = Uid;
        var q = db.Appointments.AsQueryable();
        if (User.IsInRole("Admin")) return q;
        if (User.IsInRole("Veteriner")) return q.Where(a => a.Vet.UserId == uid);
        return q.Where(a => a.Pet.OwnerId == uid);
    }

    static AppointmentDto ToDto(Appointment a) =>
        new(a.Id, a.Pet.Name, a.ClinicService.Name, a.Vet.FullName, a.Start, a.End, a.Price, a.Status.ToString(), a.RejectReason);

    // ---- Herkese acik ----
    [AllowAnonymous, HttpGet("services")]
    public async Task<ActionResult<List<ServiceDto>>> Services([FromQuery] string? q, [FromQuery] decimal? maxPrice)
    {
        var query = db.ClinicServices.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(s => s.Name.Contains(q));
        var list = await query.OrderBy(s => s.Name).ToListAsync();
        if (maxPrice is not null) list = list.Where(s => s.Price <= maxPrice).ToList();   // SQLite decimal karsilastirmasi bellekte
        return Ok(list.Select(s => new ServiceDto(s.Id, s.Name, s.DurationMinutes, s.Price)).ToList());
    }

    [AllowAnonymous, HttpGet("vets")]
    public async Task<ActionResult<List<VetDto>>> Vets([FromQuery] int? serviceId)
    {
        var q = db.Vets.AsQueryable();
        if (serviceId is not null) q = q.Where(v => v.Services.Any(s => s.Id == serviceId));
        return Ok(await q.OrderBy(v => v.FullName)
            .Select(v => new VetDto(v.Id, v.FullName, v.Specialties, v.Services.Select(s => s.Name).ToList())).ToListAsync());
    }

    [AllowAnonymous, HttpGet("slots")]
    public async Task<ActionResult<List<SlotDto>>> Slots([FromQuery] int serviceId, [FromQuery] DateOnly date)
    {
        if (date == default) return BadRequest(new { message = "date parametresi gerekli (yyyy-MM-dd)." });
        if (!await db.ClinicServices.AnyAsync(s => s.Id == serviceId)) return NotFound(new { message = "Hizmet bulunamadı." });
        return Ok(await booking.GetAvailableSlotsAsync(serviceId, date));
    }

    // ---- Giris gerektirir ----
    [HttpGet("pets")]
    public async Task<ActionResult<List<PetDto>>> Pets()
    {
        var uid = Uid; var staff = Staff;
        return Ok(await db.Pets.Where(p => staff || p.OwnerId == uid).OrderBy(p => p.Name)
            .Select(p => new PetDto(p.Id, p.Name, p.Species, p.Breed, p.BirthDate, p.WeightKg)).ToListAsync());
    }

    [HttpGet("pets/{id:int}")]
    public async Task<ActionResult<PetDto>> Pet(int id)
    {
        var uid = Uid; var staff = Staff;
        var p = await db.Pets.Where(x => x.Id == id && (staff || x.OwnerId == uid))
            .Select(x => new PetDto(x.Id, x.Name, x.Species, x.Breed, x.BirthDate, x.WeightKg)).FirstOrDefaultAsync();
        return p is null ? NotFound(new { message = "Hayvan bulunamadı." }) : Ok(p);
    }

    [HttpGet("pets/{id:int}/vaccines")]
    public async Task<ActionResult<List<VaccineDto>>> Vaccines(int id)
    {
        var uid = Uid; var staff = Staff;
        if (!await db.Pets.AnyAsync(x => x.Id == id && (staff || x.OwnerId == uid))) return NotFound(new { message = "Hayvan bulunamadı." });
        var today = DateOnly.FromDateTime(DateTime.Today);
        return Ok(await db.Vaccinations.Where(v => v.PetId == id).OrderBy(v => v.DateGiven)
            .Select(v => new VaccineDto(v.VaccineName, v.DateGiven, v.NextDoseDate, v.NextDoseDate >= today)).ToListAsync());
    }

    [HttpGet("appointments")]
    public async Task<ActionResult<List<AppointmentDto>>> Appointments(
        [FromQuery] AppointmentStatus? status, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var q = ScopedAppointments();
        if (status is not null) q = q.Where(a => a.Status == status);
        if (from is not null) q = q.Where(a => a.Start >= from);
        if (to is not null) q = q.Where(a => a.Start <= to);
        return Ok(await q.OrderBy(a => a.Start)
            .Select(a => new AppointmentDto(a.Id, a.Pet.Name, a.ClinicService.Name, a.Vet.FullName, a.Start, a.End, a.Price, a.Status.ToString(), a.RejectReason)).ToListAsync());
    }

    [HttpGet("appointments/{id:int}")]
    public async Task<ActionResult<AppointmentDto>> GetAppointment(int id)
    {
        var a = await ScopedAppointments().Include(x => x.Pet).Include(x => x.ClinicService).Include(x => x.Vet).FirstOrDefaultAsync(x => x.Id == id);
        return a is null ? NotFound(new { message = "Randevu bulunamadı." }) : Ok(ToDto(a));
    }

    [HttpPost("appointments")]
    public async Task<ActionResult<AppointmentDto>> CreateAppointment(CreateAppointmentRequest r)
    {
        var res = await booking.CreateAppointmentAsync(Uid, r.PetId, r.ClinicServiceId, r.VetId, r.Start!.Value);
        if (!res.Ok) return res.Message.Contains("bulunamadı") ? NotFound(new { message = res.Message }) : BadRequest(new { message = res.Message });
        var a = await db.Appointments.Include(x => x.Pet).Include(x => x.ClinicService).Include(x => x.Vet).FirstAsync(x => x.Id == res.AppointmentId);
        return CreatedAtAction(nameof(GetAppointment), new { id = a.Id }, ToDto(a));
    }

    [HttpPatch("appointments/{id:int}/status")]
    [Authorize(Roles = "Admin,Veteriner", AuthenticationSchemes = ApiAuth.Schemes)]
    public async Task<ActionResult<AppointmentDto>> SetStatus(int id, StatusRequest r)
    {
        if (!Enum.TryParse<AppointmentStatus>(r.Status, true, out var st))
            return BadRequest(new { message = "Geçerli durumlar: Bekliyor, Onaylandi, Reddedildi, Tamamlandi." });
        var a = await ScopedAppointments().Include(x => x.Pet).Include(x => x.ClinicService).Include(x => x.Vet).FirstOrDefaultAsync(x => x.Id == id);
        if (a is null) return NotFound(new { message = "Randevu bulunamadı." });
        if (st == AppointmentStatus.Onaylandi)
        {
            var reason = await booking.CheckAsync(a.VetId, a.ClinicServiceId, a.Start, id);
            if (reason is not null) return BadRequest(new { message = reason });
        }
        if (st == AppointmentStatus.Reddedildi && string.IsNullOrWhiteSpace(r.RejectReason))
            return BadRequest(new { message = "Red için rejectReason gerekli." });
        a.Status = st; a.RejectReason = st == AppointmentStatus.Reddedildi ? r.RejectReason : null;
        await db.SaveChangesAsync();
        return Ok(ToDto(a));
    }

    [HttpGet("reports/revenue")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = ApiAuth.Schemes)]
    public async Task<ActionResult<List<RevenueDto>>> Revenue()
    {
        var paid = await db.Appointments.Where(a => a.Status == AppointmentStatus.Onaylandi || a.Status == AppointmentStatus.Tamamlandi)
            .Select(a => new { Service = a.ClinicService.Name, a.Price }).ToListAsync();
        return Ok(paid.GroupBy(x => x.Service).Select(g => new RevenueDto(g.Key, g.Count(), g.Sum(x => x.Price))).OrderByDescending(r => r.Total).ToList());
    }
}