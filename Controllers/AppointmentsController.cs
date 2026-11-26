using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VetKlinik.Data;
using VetKlinik.Models;
using VetKlinik.Services;

namespace VetKlinik.Controllers
{
    public class AppointmentsController(AppDbContext _context, IBookingService booking) : Controller
    {
        public async Task<IActionResult> Index() =>
            View(await _context.Appointments.Include(a => a.Pet).Include(a => a.ClinicService)
                .Include(a => a.Vet).OrderByDescending(a => a.Start).ToListAsync());

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var a = await _context.Appointments.Include(x => x.Pet).Include(x => x.ClinicService)
                .Include(x => x.Vet).FirstOrDefaultAsync(x => x.Id == id);
            return a == null ? NotFound() : View(a);
        }

        void FillLists(Appointment? a = null)
        {
            ViewData["PetId"] = new SelectList(_context.Pets, "Id", "Name", a?.PetId);
            ViewData["ClinicServiceId"] = new SelectList(_context.ClinicServices, "Id", "Name", a?.ClinicServiceId);
            ViewData["VetId"] = new SelectList(_context.Vets, "Id", "FullName", a?.VetId);
        }

        public IActionResult Create()
        {
            FillLists();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("PetId,ClinicServiceId,VetId,Start,ExamNote")] Appointment appointment)
        {
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

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var a = await _context.Appointments.FindAsync(id);
            if (a == null) return NotFound();
            FillLists(a);
            return View(a);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,PetId,ClinicServiceId,VetId,Start,Status,ExamNote,RejectReason")] Appointment form)
        {
            if (id != form.Id) return NotFound();
            var a = await _context.Appointments.FindAsync(id);
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

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var a = await _context.Appointments.Include(x => x.Pet).Include(x => x.ClinicService)
                .Include(x => x.Vet).FirstOrDefaultAsync(x => x.Id == id);
            return a == null ? NotFound() : View(a);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var a = await _context.Appointments.FindAsync(id);
            if (a != null) _context.Appointments.Remove(a);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}