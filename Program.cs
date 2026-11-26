using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VetKlinik.Auth;
using VetKlinik.Data;
using VetKlinik.Models;
using VetKlinik.Services;

var builder = WebApplication.CreateBuilder(args);
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "vet.db");
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={dbPath}")); builder.Services.AddIdentity<ApplicationUser, IdentityRole>(o =>
{
    o.Password.RequiredLength = 3; o.Password.RequireDigit = false; o.Password.RequireLowercase = false;
    o.Password.RequireUppercase = false; o.Password.RequireNonAlphanumeric = false;
    o.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(o => { o.LoginPath = "/Account/Login"; o.AccessDeniedPath = "/Account/AccessDenied"; });
builder.Services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, TokenAuthHandler>("McpToken", null);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddControllersWithViews(o => o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
builder.Services.AddMcpServer().WithHttpTransport(o => o.Stateless = true).WithToolsFromAssembly();

var app = builder.Build();
using (var s = app.Services.CreateScope()) await Seed.RunAsync(s.ServiceProvider);

if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapMcp("/mcp").RequireAuthorization(p => p.AddAuthenticationSchemes("McpToken").RequireAuthenticatedUser());
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();