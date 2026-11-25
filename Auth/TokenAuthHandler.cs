using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VetKlinik.Data;
namespace VetKlinik.Auth;

public class TokenAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> o, ILoggerFactory l,
    UrlEncoder e, AppDbContext db) : AuthenticationHandler<AuthenticationSchemeOptions>(o, l, e)
{
    public static string Hash(string t) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(t)));

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var h = Request.Headers.Authorization.ToString();
        if (!h.StartsWith("Bearer ")) return AuthenticateResult.NoResult();
        var hash = Hash(h[7..].Trim());
        var t = await db.McpTokens.Include(x => x.User).FirstOrDefaultAsync(x => x.TokenHash == hash);
        if (t is null) return AuthenticateResult.Fail("Geçersiz token");
        var id = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, t.UserId),
                                     new Claim(ClaimTypes.Name, t.User.UserName!)], Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(id), Scheme.Name));
    }
}