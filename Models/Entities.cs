using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
namespace VetKlinik.Models;

public class ApplicationUser : IdentityUser { public string FullName { get; set; } = ""; }

public class Clinic
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required, StringLength(250)] public string Address { get; set; } = "";
    [Phone] public string? Phone { get; set; }
    public TimeOnly OpenTime { get; set; } = new(9, 0);
    public TimeOnly CloseTime { get; set; } = new(18, 0);
    public List<ClinicService> Services { get; set; } = [];
    public List<Vet> Vets { get; set; } = [];
}

public class ClinicService
{
    public int Id { get; set; }
    public int ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;
    [Required, StringLength(80)] public string Name { get; set; } = "";
    [Range(5, 480)] public int DurationMinutes { get; set; }
    [Range(0, 100000)] public decimal Price { get; set; }
    public List<Vet> Vets { get; set; } = [];
}

public class Vet
{
    public int Id { get; set; }
    public int ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    [StringLength(200)] public string? Specialties { get; set; }
    public string? UserId { get; set; }            // giriş yapabilen veteriner hesabı
    public ApplicationUser? User { get; set; }
    public List<ClinicService> Services { get; set; } = [];
    public List<VetAvailability> Availabilities { get; set; } = [];
}

public class VetAvailability
{
    public int Id { get; set; }
    public int VetId { get; set; }
    public Vet Vet { get; set; } = null!;
    public DayOfWeek Day { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
}

public class Pet
{
    public int Id { get; set; }
    public string OwnerId { get; set; } = "";
    public ApplicationUser Owner { get; set; } = null!;
    [Required, StringLength(60)] public string Name { get; set; } = "";
    [Required, StringLength(40)] public string Species { get; set; } = "";
    [StringLength(60)] public string? Breed { get; set; }
    public DateOnly BirthDate { get; set; }
    [Range(0.05, 200)] public decimal WeightKg { get; set; }
    public string? PhotoPath { get; set; }
    public List<Vaccination> Vaccinations { get; set; } = [];
    public List<Appointment> Appointments { get; set; } = [];
}

public class Vaccination
{
    public int Id { get; set; }
    public int PetId { get; set; }
    public Pet Pet { get; set; } = null!;
    [Required, StringLength(80)] public string VaccineName { get; set; } = "";
    public DateOnly DateGiven { get; set; }
    public DateOnly? NextDoseDate { get; set; }
}

public enum AppointmentStatus { Bekliyor, Onaylandi, Reddedildi, Tamamlandi }

public class Appointment
{
    public int Id { get; set; }
    public int PetId { get; set; }
    public Pet Pet { get; set; } = null!;
    public int ClinicServiceId { get; set; }
    public ClinicService ClinicService { get; set; } = null!;
    public int VetId { get; set; }
    public Vet Vet { get; set; } = null!;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public int DurationMinutes { get; set; }       // randevu anındaki değerler saklanır
    public decimal Price { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Bekliyor;
    public string? ExamNote { get; set; }
    public string? RejectReason { get; set; }
}

public class McpToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;
    public string TokenHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}