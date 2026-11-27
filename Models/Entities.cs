using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
namespace VetKlinik.Models;

public class ApplicationUser : IdentityUser { [Display(Name = "Ad Soyad")] public string FullName { get; set; } = ""; }

public class Clinic
{
    public int Id { get; set; }
    [Required, StringLength(100)] [Display(Name = "Ad")] public string Name { get; set; } = "";
    [Required, StringLength(250)] [Display(Name = "Adres")] public string Address { get; set; } = "";
    [Phone] [Display(Name = "Telefon")] public string? Phone { get; set; }
    [Display(Name = "Açılış Saati")] public TimeOnly OpenTime { get; set; } = new(9, 0);
    [Display(Name = "Kapanış Saati")] public TimeOnly CloseTime { get; set; } = new(18, 0);
    public List<ClinicService> Services { get; set; } = [];
    public List<Vet> Vets { get; set; } = [];
}

public class ClinicService
{
    public int Id { get; set; }
    [Display(Name = "Klinik")] public int ClinicId { get; set; }
    [Display(Name = "Klinik")] public Clinic Clinic { get; set; } = null!;
    [Required, StringLength(80)] [Display(Name = "Ad")] public string Name { get; set; } = "";
    [Range(5, 480)] [Display(Name = "Süre (dk)")] public int DurationMinutes { get; set; }
    [Range(0, 100000)] [Display(Name = "Ücret (₺)")] public decimal Price { get; set; }
    public List<Vet> Vets { get; set; } = [];
}

public class Vet
{
    public int Id { get; set; }
    [Display(Name = "Klinik")] public int ClinicId { get; set; }
    [Display(Name = "Klinik")] public Clinic Clinic { get; set; } = null!;
    [Required, StringLength(100)] [Display(Name = "Ad Soyad")] public string FullName { get; set; } = "";
    [StringLength(200)] [Display(Name = "Uzmanlık")] public string? Specialties { get; set; }
    public string? UserId { get; set; }            // giriş yapabilen veteriner hesabı
    [Display(Name = "Hesap")] public ApplicationUser? User { get; set; }
    public List<ClinicService> Services { get; set; } = [];
    public List<VetAvailability> Availabilities { get; set; } = [];
}

public class VetAvailability
{
    public int Id { get; set; }
    [Display(Name = "Veteriner")] public int VetId { get; set; }
    [Display(Name = "Veteriner")] public Vet Vet { get; set; } = null!;
    [Display(Name = "Gün")] public DayOfWeek Day { get; set; }
    [Display(Name = "Başlangıç")] public TimeOnly Start { get; set; }
    [Display(Name = "Bitiş")] public TimeOnly End { get; set; }
}

public class Pet
{
    public int Id { get; set; }
    [Display(Name = "Sahip")] public string OwnerId { get; set; } = "";
    [Display(Name = "Sahip")] public ApplicationUser Owner { get; set; } = null!;
    [Required, StringLength(60)] [Display(Name = "Ad")] public string Name { get; set; } = "";
    [Required, StringLength(40)] [Display(Name = "Tür")] public string Species { get; set; } = "";
    [StringLength(60)] [Display(Name = "Cins")] public string? Breed { get; set; }
    [Display(Name = "Doğum Tarihi")] public DateOnly BirthDate { get; set; }
    [Range(0.05, 200)] [Display(Name = "Ağırlık (kg)")] public decimal WeightKg { get; set; }
    [Display(Name = "Fotoğraf Yolu")] public string? PhotoPath { get; set; }
    public List<Vaccination> Vaccinations { get; set; } = [];
    public List<Appointment> Appointments { get; set; } = [];
}

public class Vaccination
{
    public int Id { get; set; }
    [Display(Name = "Hayvan")] public int PetId { get; set; }
    [Display(Name = "Hayvan")] public Pet Pet { get; set; } = null!;
    [Required, StringLength(80)] [Display(Name = "Aşı Adı")] public string VaccineName { get; set; } = "";
    [Display(Name = "Uygulama Tarihi")] public DateOnly DateGiven { get; set; }
    [Display(Name = "Sonraki Doz Tarihi")] public DateOnly? NextDoseDate { get; set; }
}

public enum AppointmentStatus { Bekliyor, Onaylandi, Reddedildi, Tamamlandi }

public class Appointment
{
    public int Id { get; set; }
    [Display(Name = "Hayvan")] public int PetId { get; set; }
    [Display(Name = "Hayvan")] public Pet Pet { get; set; } = null!;
    [Display(Name = "Hizmet")] public int ClinicServiceId { get; set; }
    [Display(Name = "Hizmet")] public ClinicService ClinicService { get; set; } = null!;
    [Display(Name = "Veteriner")] public int VetId { get; set; }
    [Display(Name = "Veteriner")] public Vet Vet { get; set; } = null!;
    [Display(Name = "Başlangıç")] public DateTime Start { get; set; }
    [Display(Name = "Bitiş")] public DateTime End { get; set; }
    [Display(Name = "Süre (dk)")] public int DurationMinutes { get; set; }       // randevu anındaki değerler saklanır
    [Display(Name = "Ücret (₺)")] public decimal Price { get; set; }
    [Display(Name = "Durum")] public AppointmentStatus Status { get; set; } = AppointmentStatus.Bekliyor;
    [Display(Name = "Muayene Notu")] public string? ExamNote { get; set; }
    [Display(Name = "Red Nedeni")] public string? RejectReason { get; set; }
}

public class McpToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    [Display(Name = "Hesap")] public ApplicationUser User { get; set; } = null!;
    public string TokenHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}