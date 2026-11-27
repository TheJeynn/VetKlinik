using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetKlinik.Data;
using VetKlinik.Models;

namespace VetKlinik.Controllers;

public record RevenueRow(string Service, int Count, decimal Total);
public record UserRow(string Id, string FullName, string Email, string Role);

[Authorize(Roles = "Admin")]
public class AdminController(AppDbContext db, UserManager<ApplicationUser> um) : Controller
{
    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        ViewBag.Users = await db.Users.CountAsync();
        ViewBag.Pets = await db.Pets.CountAsync();
        ViewBag.Pending = await db.Appointments.CountAsync(a => a.Status == AppointmentStatus.Bekliyor);
        ViewBag.Today = await db.Appointments.CountAsync(a => a.Start >= today && a.Start < today.AddDays(1));

        // SQLite decimal toplamayi desteklemedigi icin gruplama bellekte yapilir
        var paid = await db.Appointments
            .Where(a => a.Status == AppointmentStatus.Onaylandi || a.Status == AppointmentStatus.Tamamlandi)
            .Select(a => new { Service = a.ClinicService.Name, a.Price }).ToListAsync();
        var rev = paid.GroupBy(x => x.Service)
            .Select(g => new RevenueRow(g.Key, g.Count(), g.Sum(x => x.Price)))
            .OrderByDescending(r => r.Total).ToList();
        ViewBag.Total = rev.Sum(r => r.Total);
        ViewBag.Recent = await db.Appointments.Include(a => a.Pet).Include(a => a.ClinicService)
            .OrderByDescending(a => a.Start).Take(8).ToListAsync();
        return View(rev);
    }

    public async Task<IActionResult> Users()
    {
        var list = new List<UserRow>();
        foreach (var u in await db.Users.OrderBy(u => u.Email).ToListAsync())
            list.Add(new UserRow(u.Id, u.FullName, u.Email ?? "", (await um.GetRolesAsync(u)).FirstOrDefault() ?? "-"));
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string id, string role)
    {
        if (!new[] { "Admin", "Veteriner", "Uye" }.Contains(role)) return BadRequest();
        var u = await um.FindByIdAsync(id);
        if (u == null) return NotFound();
        if (u.Id == um.GetUserId(User)) { TempData["Error"] = "Kendi rolünüzü değiştiremezsiniz."; return RedirectToAction(nameof(Users)); }
        await um.RemoveFromRolesAsync(u, await um.GetRolesAsync(u));
        await um.AddToRoleAsync(u, role);
        TempData["Ok"] = $"{u.Email} kullanıcısının rolü {role} olarak güncellendi.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var u = await um.FindByIdAsync(id);
        if (u == null) return NotFound();
        if (u.Id == um.GetUserId(User)) { TempData["Error"] = "Kendi hesabınızı silemezsiniz."; return RedirectToAction(nameof(Users)); }
        foreach (var v in db.Vets.Where(v => v.UserId == id)) v.UserId = null;
        await db.SaveChangesAsync();
        await um.DeleteAsync(u);
        TempData["Ok"] = "Kullanıcı silindi.";
        return RedirectToAction(nameof(Users));
    }
}