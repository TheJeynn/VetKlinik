using System.ComponentModel;
using System.Globalization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using VetKlinik.Data;
using VetKlinik.Services;
namespace VetKlinik.Mcp;

[McpServerToolType]
public class VetTools(IBookingService svc, IHttpContextAccessor http, AppDbContext db)
{
    string UserId => http.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [McpServerTool(Name = "hizmetleri_listele"), Description("Kliniğin hizmetlerini, ID, süre ve ücretleriyle listeler.")]
    public async Task<object> Hizmetler() =>
        await db.ClinicServices.Select(s => new { s.Id, s.Name, s.DurationMinutes, s.Price }).ToListAsync();

    [McpServerTool(Name = "uygun_saatler"), Description("Verilen gün ve hizmet için boş hekim ve saatleri listeler.")]
    public async Task<object> UygunSaatler([Description("Hizmet ID")] int hizmetId,
        [Description("Tarih, yyyy-MM-dd")] string tarih)
    {
        if (!DateOnly.TryParse(tarih, CultureInfo.InvariantCulture, out var d)) return "Tarih formatı yyyy-MM-dd olmalı.";
        var slots = await svc.GetAvailableSlotsAsync(hizmetId, d);
        return slots.Count == 0 ? "Bu gün için uygun saat yok." : slots;
    }

    [McpServerTool(Name = "asi_takvimi"), Description("Kullanıcının evcil hayvanının geçmiş ve yaklaşan aşılarını döner.")]
    public async Task<object> AsiTakvimi([Description("Evcil hayvanın adı")] string hayvanAdi) =>
        (object?)await svc.GetVaccineScheduleAsync(UserId, hayvanAdi) ?? "Bu isimde size ait bir hayvan bulunamadı.";

    [McpServerTool(Name = "randevu_olustur"), Description("'Bekliyor' durumunda randevu oluşturur; çakışma varsa nedenini söyler.")]
    public async Task<string> RandevuOlustur([Description("Evcil hayvan adı")] string hayvanAdi,
        [Description("Hizmet ID")] int hizmetId, [Description("Hekim ID")] int hekimId,
        [Description("Tarih-saat, yyyy-MM-dd HH:mm")] string tarihSaat)
    {
        if (!DateTime.TryParse(tarihSaat, CultureInfo.InvariantCulture, out var start)) return "Tarih formatı: yyyy-MM-dd HH:mm";
        var pet = await db.Pets.FirstOrDefaultAsync(p => p.OwnerId == UserId && p.Name.ToLower() == hayvanAdi.ToLower());
        if (pet is null) return "Bu isimde size ait bir hayvan bulunamadı.";
        var r = await svc.CreateAppointmentAsync(UserId, pet.Id, hizmetId, hekimId, start);
        return r.Ok ? $"{r.Message} Randevu no: {r.AppointmentId}" : $"Randevu oluşturulamadı: {r.Message}";
    }
}