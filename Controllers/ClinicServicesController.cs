using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VetKlinik.Data;
using VetKlinik.Models;

namespace VetKlinik.Controllers
{
    [Authorize]
    public class ClinicServicesController : Controller
    {
        private readonly AppDbContext _context;

        public ClinicServicesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: ClinicServices
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.ClinicServices.Include(c => c.Clinic);
            return View(await appDbContext.ToListAsync());
        }

        // GET: ClinicServices/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var clinicService = await _context.ClinicServices
                .Include(c => c.Clinic)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (clinicService == null)
            {
                return NotFound();
            }

            return View(clinicService);
        }

        // GET: ClinicServices/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["ClinicId"] = new SelectList(_context.Clinics, "Id", "Address");
            return View();
        }

        // POST: ClinicServices/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,ClinicId,Name,DurationMinutes,Price")] ClinicService clinicService)
        {
            if (ModelState.IsValid)
            {
                _context.Add(clinicService);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClinicId"] = new SelectList(_context.Clinics, "Id", "Address", clinicService.ClinicId);
            return View(clinicService);
        }

        // GET: ClinicServices/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var clinicService = await _context.ClinicServices.FindAsync(id);
            if (clinicService == null)
            {
                return NotFound();
            }
            ViewData["ClinicId"] = new SelectList(_context.Clinics, "Id", "Address", clinicService.ClinicId);
            return View(clinicService);
        }

        // POST: ClinicServices/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClinicId,Name,DurationMinutes,Price")] ClinicService clinicService)
        {
            if (id != clinicService.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(clinicService);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClinicServiceExists(clinicService.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClinicId"] = new SelectList(_context.Clinics, "Id", "Address", clinicService.ClinicId);
            return View(clinicService);
        }

        // GET: ClinicServices/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var clinicService = await _context.ClinicServices
                .Include(c => c.Clinic)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (clinicService == null)
            {
                return NotFound();
            }

            return View(clinicService);
        }

        // POST: ClinicServices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var clinicService = await _context.ClinicServices.FindAsync(id);
            if (clinicService != null)
            {
                _context.ClinicServices.Remove(clinicService);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ClinicServiceExists(int id)
        {
            return _context.ClinicServices.Any(e => e.Id == id);
        }
    }
}
