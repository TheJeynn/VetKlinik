using Microsoft.EntityFrameworkCore;
using VetKlinik.Data;
using VetKlinik.Models;
namespace VetKlinik.Services;

public record SlotDto(int VetId, string VetName, DateTime Start, DateTime End);
public record VaccineDto(string VaccineName, DateOnly DateGiven, DateOnly? NextDoseDate, bool Upcoming);
public record BookingResult(bool Ok, string Message, int? AppointmentId = null);

public interface IBookingService
{
    Task<List<SlotDto>> GetAvailableSlotsAsync(int serviceId, DateOnly date);
    Task<string?> CheckAsync(int vetId, int serviceId, DateTime start, int? ignoreId = null);
    Task<BookingResult> CreateAppointmentAsync(string ownerId, int petId, int serviceId, int vetId, DateTime start);
    Task<List<VaccineDto>?> GetVaccineScheduleAsync(string ownerId, string petName);
}

public class BookingService(AppDbContext db) : IBookingService
{
    const int Step = 30;
    static int Mins(TimeOnly t) => t.Hour * 60 + t.Minute;
    static bool Active(Appointment a) => a.Status == AppointmentStatus.Bekliyor || a.Status == AppointmentStatus.Onaylandi;

    public async Task<List<SlotDto>> GetAvailableSlotsAsync(int serviceId, DateOnly date)
    {
        var svc = await db.ClinicServices.Include(s => s.Clinic).FirstOrDefaultAsync(s => s.Id == serviceId);
        if (svc is null) return [];
        var vets = await db.Vets.Include(v => v.Availabilities)
            .Where(v => v.Services.Any(s => s.Id == serviceId)).ToListAsync();
        var d0 = date.ToDateTime(TimeOnly.MinValue);
        var busy = await db.Appointments
            .Where(a => a.Start >= d0 && a.Start < d0.AddDays(1)
                && (a.Status == AppointmentStatus.Bekliyor || a.Status == AppointmentStatus.Onaylandi))
            .Select(a => new { a.VetId, a.Start, a.End }).ToListAsync();

        var list = new List<SlotDto>();
        foreach (var v in vets)
            foreach (var av in v.Availabilities.Where(x => x.Day == date.DayOfWeek))
            {
                int from = Math.Max(Mins(av.Start), Mins(svc.Clinic.OpenTime));
                int to = Math.Min(Mins(av.End), Mins(svc.Clinic.CloseTime));
                for (int m = from; m + svc.DurationMinutes <= to; m += Step)
                {
                    var s = d0.AddMinutes(m); var e = s.AddMinutes(svc.DurationMinutes);
                    if (s <= DateTime.Now) continue;
                    if (busy.Any(b => b.VetId == v.Id && s < b.End && e > b.Start)) continue;
                    list.Add(new SlotDto(v.Id, v.FullName, s, e));
                }
            }
        return list.OrderBy(x => x.Start).ThenBy(x => x.VetName).ToList();
    }

    // null = uygun, aksi halde nedenini döner
    public async Task<string?> CheckAsync(int vetId, int serviceId, DateTime start, int? ignoreId = null)
    {
        var svc = await db.ClinicServices.Include(s => s.Clinic).FirstOrDefaultAsync(s => s.Id == serviceId);
        var vet = await db.Vets.Include(v => v.Services).Include(v => v.Availabilities).FirstOrDefaultAsync(v => v.Id == vetId);
        if (svc is null || vet is null) return "Hizmet veya hekim bulunamadı.";
        if (start <= DateTime.Now) return "Geçmiş bir tarih/saat seçilemez.";
        if (!vet.Services.Any(s => s.Id == serviceId)) return $"{vet.FullName} bu hizmeti vermiyor.";

        var end = start.AddMinutes(svc.DurationMinutes);
        if (end.Date != start.Date) return "Randevu gün sonunu aşıyor.";
        int sm = (int)start.TimeOfDay.TotalMinutes, em = sm + svc.DurationMinutes;
        if (sm < Mins(svc.Clinic.OpenTime) || em > Mins(svc.Clinic.CloseTime))
            return $"Klinik çalışma saatleri {svc.Clinic.OpenTime:HH\\:mm}-{svc.Clinic.CloseTime:HH\\:mm}; {svc.DurationMinutes} dk'lık hizmet bu saate sığmıyor.";
        if (!vet.Availabilities.Any(a => a.Day == start.DayOfWeek && sm >= Mins(a.Start) && em <= Mins(a.End)))
            return $"{vet.FullName} bu gün/saat aralığında müsait değil.";

        var clash = await db.Appointments.FirstOrDefaultAsync(a => a.VetId == vetId && a.Id != (ignoreId ?? 0)
            && (a.Status == AppointmentStatus.Bekliyor || a.Status == AppointmentStatus.Onaylandi)
            && start < a.End && end > a.Start);
        return clash is null ? null : $"Hekimin {clash.Start:HH:mm}-{clash.End:HH:mm} arasında başka randevusu var.";
    }

    public async Task<BookingResult> CreateAppointmentAsync(string ownerId, int petId, int serviceId, int vetId, DateTime start)
    {
        var pet = await db.Pets.FirstOrDefaultAsync(p => p.Id == petId && p.OwnerId == ownerId);
        if (pet is null) return new(false, "Evcil hayvan bulunamadı veya size ait değil.");
        var reason = await CheckAsync(vetId, serviceId, start);
        if (reason is not null) return new(false, reason);

        var svc = await db.ClinicServices.FindAsync(serviceId);
        var a = new Appointment
        {
            PetId = petId,
            VetId = vetId,
            ClinicServiceId = serviceId,
            Start = start,
            End = start.AddMinutes(svc!.DurationMinutes),
            DurationMinutes = svc.DurationMinutes,
            Price = svc.Price
        };
        db.Appointments.Add(a);
        await db.SaveChangesAsync();
        return new(true, "Randevu 'Bekliyor' durumunda oluşturuldu.", a.Id);
    }

    public async Task<List<VaccineDto>?> GetVaccineScheduleAsync(string ownerId, string petName)
    {
        var pet = await db.Pets.Include(p => p.Vaccinations)
            .FirstOrDefaultAsync(p => p.OwnerId == ownerId && p.Name.ToLower() == petName.ToLower());
        if (pet is null) return null;
        var today = DateOnly.FromDateTime(DateTime.Today);
        return pet.Vaccinations.OrderBy(v => v.DateGiven)
            .Select(v => new VaccineDto(v.VaccineName, v.DateGiven, v.NextDoseDate, v.NextDoseDate >= today)).ToList();
    }
}