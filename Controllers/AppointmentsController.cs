using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VetKlinik.Data;
using VetKlinik.Models;
using VetKlinik.Services;

namespace VetKlinik.Controllers
{
    [Authorize]
    public class AppointmentsController(AppDbContext _context, IBookingService booking) : Controller
    {
        string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        bool Admin => User.IsInRole("Admin");
        bool Staff => Admin || User.IsInRole("Veteriner");

        // Admin hepsini, veteriner kendi randevularini, uye kendi hayvanlarinin randevularini gorur
        IQueryable<Appointment> Scoped()
        {
            var q = _context.Appointments.Include(a => a.Pet).Include(a => a.ClinicService).Include(a => a.Vet).AsQueryable();
            if (Admin) return q;
            if (User.IsInRole("Veteriner")) return q.Where(a => a.Vet.UserId == Uid);
            return q.Where(a => a.Pet.OwnerId == Uid);
        }

        void FillLists(Appointment? a = null)
        {
            var pets = Staff ? _context.Pets.AsQueryable() : _context.Pets.Where(p => p.OwnerId == Uid);
            ViewData["PetId"] = new SelectList(pets.ToList(), "Id", "Name", a?.PetId);
            ViewData["ClinicServiceId"] = new SelectList(_context.ClinicServices.ToList(), "Id", "Name", a?.ClinicServiceId);
            ViewData["VetId"] = new SelectList(_context.Vets.ToList(), "Id", "FullName", a?.VetId);
        }

        public async Task<IActionResult> Index() => View(await Scoped().OrderByDescending(a => a.Start).ToListAsync());

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var a = await Scoped().FirstOrDefaultAsync(x => x.Id == id);
            return a == null ? NotFound() : View(a);
        }

        public IActionResult Create() { FillLists(); return View(); }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PetId,ClinicServiceId,VetId,Start,ExamNote")] Appointment appointment)
        {
            if (!Staff && !await _context.Pets.AnyAsync(p => p.Id == appointment.PetId && p.OwnerId == Uid))
                return Forbid();

            var reason = await booking.CheckAsync(appointment.VetId, appointment.ClinicServiceId, appointment.Start);
            if (reason is not null) ModelState.AddModelError("", reason);

            if (ModelState.IsValid)
            {
                var svc = await _context.ClinicServices.FindAsync(appointment.ClinicServiceId);
                appointment.End = appointment.Start.AddMinutes(svc!.DurationMinutes);
                appointment.DurationMinutes = svc.DurationMinutes;
                appointment.Price = svc.Price;
                appointment.Status = AppointmentStatus.Bekliyor;
                _context.Add(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            FillLists(appointment);
            return View(appointment);
        }

        [Authorize(Roles = "Admin,Veteriner")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var a = await Scoped().FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return NotFound();
            FillLists(a);
            return View(a);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Veteriner")]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,PetId,ClinicServiceId,VetId,Start,Status,ExamNote,RejectReason")] Appointment form)
        {
            if (id != form.Id) return NotFound();
            var a = await Scoped().FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return NotFound();

            var reason = await booking.CheckAsync(form.VetId, form.ClinicServiceId, form.Start, id);
            if (reason is not null && form.Status != AppointmentStatus.Reddedildi && form.Status != AppointmentStatus.Tamamlandi)
                ModelState.AddModelError("", reason);

            if (ModelState.IsValid)
            {
                var svc = await _context.ClinicServices.FindAsync(form.ClinicServiceId);
                a.PetId = form.PetId; a.ClinicServiceId = form.ClinicServiceId; a.VetId = form.VetId;
                a.Start = form.Start; a.End = form.Start.AddMinutes(svc!.DurationMinutes);
                a.DurationMinutes = svc.DurationMinutes; a.Price = svc.Price;
                a.Status = form.Status; a.ExamNote = form.ExamNote; a.RejectReason = form.RejectReason;
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            FillLists(form);
            return View(form);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Veteriner")]
        public async Task<IActionResult> Approve(int id)
        {
            var a = await Scoped().FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return NotFound();
            var reason = await booking.CheckAsync(a.VetId, a.ClinicServiceId, a.Start, id);
            if (reason is not null) { TempData["Error"] = reason; return RedirectToAction(nameof(Index)); }
            a.Status = AppointmentStatus.Onaylandi;
            a.RejectReason = null;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Veteriner")]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var a = await Scoped().FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return NotFound();
            a.Status = AppointmentStatus.Reddedildi;
            a.RejectReason = reason;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var a = await Scoped().FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return NotFound();
            if (!Staff && a.Status != AppointmentStatus.Bekliyor) return Forbid();
            return View(a);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var a = await Scoped().FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return NotFound();
            if (!Staff && a.Status != AppointmentStatus.Bekliyor) return Forbid();
            _context.Appointments.Remove(a);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}