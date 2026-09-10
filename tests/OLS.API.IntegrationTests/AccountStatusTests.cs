using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.API.IntegrationTests;

/// <summary>
/// FİRMA DURUMU — CARİ FİRMALAR / DİĞER FİRMALAR.
///
/// Siber firmayı <c>sbr_firmadurum</c> ile ikiye ayırıyor ve ayrım fiilen
/// kullanılıyor: canlıda 7.462 firmanın 4.250'si CARİ FİRMALAR, 3.212'si
/// DİĞER FİRMALAR. Uygulama bu alanı hiç sormuyordu ve
/// <c>sbr_firma.firmadurumid</c>'ye SABİT olarak CARİ FİRMALAR'ın kimliğini
/// yazıyordu — yani programdan açılan her firma Siber'de cari görünüyordu.
///
/// Seçenekler yerelde tohumlanmaz, Siber'den aynalanır; bu yüzden testler
/// satırları kendileri açar.
/// </summary>
[Collection("OlsApi")]
public sealed class AccountStatusTests
{
    private readonly OlsApiFactory _factory;

    public AccountStatusTests(OlsApiFactory factory) => _factory = factory;

    private async Task<AccountStatus> AddStatusAsync(string name, string? siberId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var status = new AccountStatus
        {
            Id = Guid.NewGuid(),
            Name = name,
            SiberId = siberId,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };

        db.AccountStatuses.Add(status);
        await db.SaveChangesAsync();
        return status;
    }

    [Fact]
    public async Task SecilenDurum_KaydedilirVeDetaydaDoner()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var status = await AddStatusAsync(
            $"DİĞER FİRMALAR {Guid.NewGuid():N}"[..24], Guid.NewGuid().ToString());

        var name = $"Durum Testi {Guid.NewGuid():N}"[..28];
        using var form = await TestAccountHelper.MinimalAccountFormAsync(admin, name);
        form.Add(new StringContent(status.Id.ToString()), "account_status_id");

        var created = await admin.PostAsync("/api/v1/account", form);
        created.EnsureSuccessStatusCode();

        var accountId = (await created.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();

        var detail = await admin.GetAsync($"/api/v1/account/{accountId}");
        detail.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await detail.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("account_status").GetProperty("id")
            .GetGuid().Should().Be(status.Id);
    }

    /// <summary>
    /// Durum GÖNDERİLMEZSE mevcut değer korunur. Aksi hâlde firma durumunu
    /// göndermeyen her kaydetme, Siber'deki "DİĞER FİRMALAR" işaretini sessizce
    /// silerdi — ülke sütunlarındaki ISNULL kuralının aynı gerekçesi.
    /// </summary>
    [Fact]
    public async Task DurumGonderilmezse_MevcutDegerKorunur()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var status = await AddStatusAsync(
            $"DİĞER {Guid.NewGuid():N}"[..20], Guid.NewGuid().ToString());

        var name = $"Koruma Testi {Guid.NewGuid():N}"[..28];
        using var createForm = await TestAccountHelper.MinimalAccountFormAsync(admin, name);
        createForm.Add(new StringContent(status.Id.ToString()), "account_status_id");

        var created = await admin.PostAsync("/api/v1/account", createForm);
        created.EnsureSuccessStatusCode();
        var accountId = (await created.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();

        // Durum alanı OLMADAN güncelle.
        using var updateForm = await TestAccountHelper.MinimalAccountFormAsync(admin, name);
        updateForm.Add(new StringContent(accountId.ToString()), "id");
        (await admin.PostAsync("/api/v1/account/update", updateForm)).EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var saved = await db.Accounts.AsNoTracking()
            .Where(a => a.Id == accountId).Select(a => a.AccountStatusId).FirstAsync();

        saved.Should().Be(status.Id);
    }

    /// <summary>
    /// Siber karşılığı olmayan satır LİSTELENMEZ — seçilemeyen bir seçenek
    /// kaydetme anında Siber'de karşılıksız referansa dönüşürdü (şehir
    /// listesinde birebir aynı kural).
    /// </summary>
    [Fact]
    public async Task SiberKarsiligiOlmayanDurum_Listelenmez()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var visible = await AddStatusAsync($"CARİ {Guid.NewGuid():N}"[..20], Guid.NewGuid().ToString());
        var hidden = await AddStatusAsync($"SİBERSİZ {Guid.NewGuid():N}"[..20], null);

        var response = await admin.GetAsync("/api/v1/account/statuses");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var ids = (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").EnumerateArray()
            .Select(s => s.GetProperty("id").GetGuid())
            .ToList();

        ids.Should().Contain(visible.Id);
        ids.Should().NotContain(hidden.Id);
    }
}
