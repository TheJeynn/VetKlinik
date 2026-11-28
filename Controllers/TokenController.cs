using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetKlinik.Auth;
using VetKlinik.Data;
using VetKlinik.Models;

namespace VetKlinik.Controllers;

[Authorize]
public class TokenController(AppDbContext db) : Controller
{
    string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index() =>
        View(await db.McpTokens.Where(t => t.UserId == Uid).OrderByDescending(t => t.CreatedAt).Select(t => t.CreatedAt).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate()
    {
        var raw = "vk_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        db.McpTokens.Add(new McpToken { UserId = Uid, TokenHash = TokenAuthHandler.Hash(raw) });
        await db.SaveChangesAsync();
        TempData["NewToken"] = raw;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke()
    {
        db.McpTokens.RemoveRange(db.McpTokens.Where(t => t.UserId == Uid));
        await db.SaveChangesAsync();
        TempData["Ok"] = "Tüm token'larınız iptal edildi.";
        return RedirectToAction(nameof(Index));
    }
}