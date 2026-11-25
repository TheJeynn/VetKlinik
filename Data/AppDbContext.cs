using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using VetKlinik.Models;
namespace VetKlinik.Data;

public class AppDbContext(DbContextOptions<AppDbContext> o) : IdentityDbContext<ApplicationUser>(o)
{
    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<ClinicService> ClinicServices => Set<ClinicService>();
    public DbSet<Vet> Vets => Set<Vet>();
    public DbSet<VetAvailability> VetAvailabilities => Set<VetAvailability>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<Vaccination> Vaccinations => Set<Vaccination>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<McpToken> McpTokens => Set<McpToken>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Vet>().HasMany(v => v.Services).WithMany(s => s.Vets);
        b.Entity<Appointment>().HasOne(a => a.Vet).WithMany().HasForeignKey(a => a.VetId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Appointment>().HasOne(a => a.ClinicService).WithMany().HasForeignKey(a => a.ClinicServiceId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<McpToken>().HasIndex(t => t.TokenHash).IsUnique();
    }
}