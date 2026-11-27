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
    public class PetsController(AppDbContext _context) : Controller
    {
        string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        bool Staff => User.IsInRole("Admin") || User.IsInRole("Veteriner");

        // Uye sadece kendi hayvanlarini gorur (sahiplik kontrolu)
        IQueryable<Pet> Mine()
        {
            var q = _context.Pets.Include(p => p.Owner).AsQueryable();
            return Staff ? q : q.Where(p => p.OwnerId == Uid);
        }

        void Owners(string? sel = null)
        {
            var u = Staff ? _context.Users.AsQueryable() : _context.Users.Where(x => x.Id == Uid);
            ViewData["OwnerId"] = new SelectList(u.ToList(), "Id", "Email", sel ?? Uid);
        }

        public async Task<IActionResult> Index() => View(await Mine().ToListAsync());

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var p = await Mine().FirstOrDefaultAsync(x => x.Id == id);
            return p == null ? NotFound() : View(p);
        }

        public IActionResult Create() { Owners(); return View(); }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OwnerId,Name,Species,Breed,BirthDate,WeightKg,PhotoPath")] Pet pet)
        {
            if (!Staff) pet.OwnerId = Uid;
            ModelState.Remove(nameof(Pet.Owner));
            if (ModelState.IsValid)
            {
                _context.Add(pet);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            Owners(pet.OwnerId);
            return View(pet);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var p = await Mine().FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();
            Owners(p.OwnerId);
            return View(p);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,OwnerId,Name,Species,Breed,BirthDate,WeightKg,PhotoPath")] Pet form)
        {
            if (id != form.Id) return NotFound();
            var p = await Mine().FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();
            ModelState.Remove(nameof(Pet.Owner));
            if (ModelState.IsValid)
            {
                p.Name = form.Name; p.Species = form.Species; p.Breed = form.Breed;
                p.BirthDate = form.BirthDate; p.WeightKg = form.WeightKg; p.PhotoPath = form.PhotoPath;
                if (Staff) p.OwnerId = form.OwnerId;
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            Owners(form.OwnerId);
            return View(form);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var p = await Mine().FirstOrDefaultAsync(x => x.Id == id);
            return p == null ? NotFound() : View(p);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var p = await Mine().FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();
            _context.Pets.Remove(p);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}