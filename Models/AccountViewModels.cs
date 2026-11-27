using System.ComponentModel.DataAnnotations;
namespace VetKlinik.Models;

public class RegisterVM
{
    [Required(ErrorMessage = "Ad soyad gerekli"), StringLength(100), Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = "";
    [Required(ErrorMessage = "E-posta gerekli"), EmailAddress(ErrorMessage = "Geçerli bir e-posta girin"), Display(Name = "E-posta")]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Parola gerekli"), StringLength(100, MinimumLength = 3, ErrorMessage = "Parola en az 3 karakter olmalı"), DataType(DataType.Password), Display(Name = "Parola")]
    public string Password { get; set; } = "";
    [DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Parolalar eşleşmiyor"), Display(Name = "Parola (tekrar)")]
    public string ConfirmPassword { get; set; } = "";
}

public class LoginVM
{
    [Required(ErrorMessage = "E-posta gerekli"), EmailAddress, Display(Name = "E-posta")]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Parola gerekli"), DataType(DataType.Password), Display(Name = "Parola")]
    public string Password { get; set; } = "";
    [Display(Name = "Beni hatırla")]
    public bool RememberMe { get; set; }
}