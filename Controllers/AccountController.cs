using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VetKlinik.Models;

namespace VetKlinik.Controllers;

public class AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn) : Controller
{
    [HttpGet] public IActionResult Register() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVM m)
    {
        if (!ModelState.IsValid) return View(m);
        var u = new ApplicationUser { UserName = m.Email, Email = m.Email, FullName = m.FullName, EmailConfirmed = true };
        var r = await users.CreateAsync(u, m.Password);
        if (!r.Succeeded)
        {
            foreach (var e in r.Errors) ModelState.AddModelError("", e.Description);
            return View(m);
        }
        await users.AddToRoleAsync(u, "Uye");
        await signIn.SignInAsync(u, false);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet] public IActionResult Login(string? returnUrl = null) { ViewData["ReturnUrl"] = returnUrl; return View(); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVM m, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(m);
        var r = await signIn.PasswordSignInAsync(m.Email, m.Password, m.RememberMe, false);
        if (r.Succeeded) return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction("Index", "Home");
        ModelState.AddModelError("", "E-posta veya parola hatalı.");
        return View(m);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout() { await signIn.SignOutAsync(); return RedirectToAction("Index", "Home"); }

    public IActionResult AccessDenied() => View();
}