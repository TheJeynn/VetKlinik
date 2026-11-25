using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VetKlinik.Models;
namespace VetKlinik.Data;

public static class Seed
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        var rm = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var um = sp.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var r in new[] { "Admin", "Veteriner", "Uye" })
            if (!await rm.RoleExistsAsync(r)) await rm.CreateAsync(new IdentityRole(r));

        async Task<ApplicationUser> U(string mail, string name, string role)
        {
            var u = await um.FindByEmailAsync(mail);
            if (u is null)
            {
                u = new ApplicationUser { UserName = mail, Email = mail, FullName = name, EmailConfirmed = true };
                await um.CreateAsync(u, "sau");
                await um.AddToRoleAsync(u, role);
            }
            return u;
        }
        await U("b241210084@sakarya.edu.tr", "Burak Kahveci", "Admin");   // numaranı kontrol et
        var vu = await U("veteriner@vet.com", "Dr. Ayşe Demir", "Veteriner");
        await U("uye@vet.com", "Örnek Üye", "Uye");
        if (await db.Clinics.AnyAsync()) return;

        var muayene = new ClinicService { Name = "Muayene", DurationMinutes = 30, Price = 500 };
        var asi = new ClinicService { Name = "Aşı", DurationMinutes = 20, Price = 350 };
        var kisir = new ClinicService { Name = "Kısırlaştırma", DurationMinutes = 90, Price = 2500 };
        var tiras = new ClinicService { Name = "Tıraş ve Bakım", DurationMinutes = 60, Price = 600 };
        var clinic = new Clinic { Name = "Pati Veteriner Kliniği", Address = "Serdivan, Sakarya", Phone = "0264 000 00 00" };
        clinic.Services.AddRange([muayene, asi, kisir, tiras]);
        var vet = new Vet
        {
            FullName = vu.FullName,
            Specialties = "Küçük hayvan, Cerrahi",
            UserId = vu.Id,
            Services = [muayene, asi, kisir, tiras]
        };
        vet.Availabilities = Enumerable.Range(1, 5).Select(d => new VetAvailability
        { Day = (DayOfWeek)d, Start = new(9, 0), End = new(17, 0) }).ToList();
        clinic.Vets.Add(vet);
        db.Clinics.Add(clinic);
        await db.SaveChangesAsync();
    }
}