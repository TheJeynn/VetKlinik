using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VetKlinik.Data;
using VetKlinik.Models;

namespace VetKlinik.Controllers
{
    [Authorize]
    public class VaccinationsController(AppDbContext _context) : Controller
    {
        string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        bool Staff => User.IsInRole("Admin") || User.IsInRole("Veteriner");

        IQueryable<Vaccination> Scoped()
        {
            var q = _context.Vaccinations.Include(v => v.Pet).AsQueryable();
            return Staff ? q : q.Where(v => v.Pet.OwnerId == Uid);
        }

        void Pets(int? sel = null) => ViewData["PetId"] = new SelectList(_context.Pets.ToList(), "Id", "Name", sel);

        public async Task<IActionResult> Index() => View(await Scoped().OrderBy(v => v.Pet.Name).ThenBy(v => v.DateGiven).ToListAsync());

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var v = await Scoped().FirstOrDefaultAsync(x => x.Id == id);
            return v == null ? NotFound() : View(v);
        }

        [Authorize(Roles = "Admin,Veteriner")]
        public IActionResult Create() { Pets(); return View(); }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Veteriner")]
        public async Task<IActionResult> Create([Bind("PetId,VaccineName,DateGiven,NextDoseDate")] Vaccination v)
        {
            ModelState.Remove(nameof(Vaccination.Pet));
            if (ModelState.IsValid)
            {
                _context.Add(v);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            Pets(v.PetId);
            return View(v);
        }

        [Authorize(Roles = "Admin,Veteriner")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var v = await Scoped().FirstOrDefaultAsync(x => x.Id == id);
            if (v == null) return NotFound();
            Pets(v.PetId);
            return View(v);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Veteriner")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,PetId,VaccineName,DateGiven,NextDoseDate")] Vaccination form)
        {
            if (id != form.Id) return NotFound();
            var v = await Scoped().FirstOrDefaultAsync(x => x.Id == id);
            if (v == null) return NotFound();
            ModelState.Remove(nameof(Vaccination.Pet));
            if (ModelState.IsValid)
            {
                v.PetId = form.PetId; v.VaccineName = form.VaccineName;
                v.DateGiven = form.DateGiven; v.NextDoseDate = form.NextDoseDate;
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            Pets(form.PetId);
            return View(form);
        }

        [Authorize(Roles = "Admin,Veteriner")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var v = await Scoped().FirstOrDefaultAsync(x => x.Id == id);
            return v == null ? NotFound() : View(v);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken, Authorize(Roles = "Admin,Veteriner")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var v = await Scoped().FirstOrDefaultAsync(x => x.Id == id);
            if (v == null) return NotFound();
            _context.Vaccinations.Remove(v);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}