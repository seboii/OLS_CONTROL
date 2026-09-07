using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.API.IntegrationTests;

/// <summary>
/// OPERASYON YETKİLİSİ — MÜŞTERİNİNKİ VARSA DEĞİŞMEZ.
///
/// Kural (kullanıcı isteği):
///   1. Müşteriye tanımlı operasyon yetkilisi varsa O yazılır; istekten gelen
///      değer yok sayılır.
///   2. Tanımlı değilse formdan elle seçilen kişi yazılır.
///   3. O da yoksa kaydeden kullanıcıya düşülür — Siber aktarımı boş
///      <c>musteritemsilcisi</c>/<c>insuser</c> kabul etmiyor.
///
/// Eskiden 1. adım yoktu: daima kaydeden kullanıcı yazılıyordu ve canlıda
/// 3.209 caride dolu olan bağ yalnızca sistem hesabında kullanılıyordu.
///
/// AYRILMIŞ PERSONEL "VAR" SAYILMAZ: o 3.209 carinin 1.651'inde yetkili
/// pasif/silinmiş bir kullanıcı; göreve yazmak işi şirkette olmayan birine
/// atamak olurdu.
/// </summary>
[Collection("OlsApi")]
public sealed class OperationOfficerTests
{
    private const int OperationOfficerType = 1;

    private readonly OlsApiFactory _factory;

    public OperationOfficerTests(OlsApiFactory factory) => _factory = factory;

    private async Task<long> CreateAccountAsync(HttpClient admin)
    {
        using var form = await TestAccountHelper.MinimalAccountFormAsync(
            admin, $"Yetkili Testi {Guid.NewGuid():N}"[..28]);

        var response = await admin.PostAsync("/api/v1/account", form);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();
    }

    private async Task LinkRepresentativeAsync(long accountId, long userId, bool active)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        db.AccountRepresentatives.Add(new AccountRepresentative
        {
            AccountId = (int)accountId,
            UserId = (int)userId,
            UserType = OperationOfficerType,
        });

        if (!active)
        {
            var user = await db.Users.FirstAsync(u => u.Id == userId);
            user.Status = false;
        }

        await db.SaveChangesAsync();
    }

    private async Task<long> SaveOfferAsync(
        HttpClient client, long accountId, long? requestedOfficerId)
    {
        using var form = new MultipartFormDataContent
        {
            { new StringContent("1"), "work_type_id" },
            { new StringContent("1"), "loading_type_id" },
            { new StringContent("1"), "payment_type_id" },
            { new StringContent("4"), "status_type_id" },
            { new StringContent("1"), "department_id" },
            { new StringContent(accountId.ToString()), "customer_id" },
            { new StringContent("2026-09-01"), "offer_date" },
            { new StringContent("2026-09-30"), "offer_validity_date" },
            { new StringContent("2026-09-01"), "marketing_notification_date" },
        };

        if (requestedOfficerId is { } officerId)
        {
            form.Add(new StringContent(officerId.ToString()), "load_charge_person[0][user_id]");
            form.Add(new StringContent("1"), "load_charge_person[0][user_type]");
        }

        var response = await client.PostAsync("/api/v1/load", form);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();
    }

    private async Task<long?> OfficerOfAsync(long loadId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        return await db.LoadChargePeople.AsNoTracking()
            .Where(p => p.LoadId == (int)loadId && p.UserType == OperationOfficerType)
            .Select(p => (long?)p.UserId)
            .FirstOrDefaultAsync();
    }

    private async Task<long> AdminIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        return await db.Users.Where(u => u.Email == "admin@ols-scoped.local")
            .Select(u => u.Id).FirstAsync();
    }

    [Fact]
    public async Task MusterininYetkilisiVarsa_OYazilir()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountId = await CreateAccountAsync(admin);
        var officerId = await admin.CreateUserAsync($"yetkili-{Guid.NewGuid():N}@example.test");
        await LinkRepresentativeAsync(accountId, officerId, active: true);

        var loadId = await SaveOfferAsync(admin, accountId, requestedOfficerId: null);

        (await OfficerOfAsync(loadId)).Should().Be(officerId);
    }

    /// <summary>"varsa değişmesin" — istekten gelen değer müşterininkini EZEMEZ.</summary>
    [Fact]
    public async Task MusterininYetkilisiVarsa_IstektenGelenYokSayilir()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountId = await CreateAccountAsync(admin);
        var officerId = await admin.CreateUserAsync($"yetkili-{Guid.NewGuid():N}@example.test");
        var baskasi = await admin.CreateUserAsync($"baskasi-{Guid.NewGuid():N}@example.test");
        await LinkRepresentativeAsync(accountId, officerId, active: true);

        var loadId = await SaveOfferAsync(admin, accountId, requestedOfficerId: baskasi);

        (await OfficerOfAsync(loadId)).Should().Be(officerId);
    }

    /// <summary>"yoksa manuel girilebilsin".</summary>
    [Fact]
    public async Task MusterininYetkilisiYoksa_ElleSecilenYazilir()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountId = await CreateAccountAsync(admin);
        var secilen = await admin.CreateUserAsync($"secilen-{Guid.NewGuid():N}@example.test");

        var loadId = await SaveOfferAsync(admin, accountId, requestedOfficerId: secilen);

        (await OfficerOfAsync(loadId)).Should().Be(secilen);
    }

    [Fact]
    public async Task MusterininYetkilisiYokVeElleSecimYoksa_KaydedenYazilir()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountId = await CreateAccountAsync(admin);

        var loadId = await SaveOfferAsync(admin, accountId, requestedOfficerId: null);

        (await OfficerOfAsync(loadId)).Should().Be(await AdminIdAsync());
    }

    /// <summary>
    /// Ayrılmış personel "tanımlı yetkili" sayılmaz; alan elle seçime açılır.
    /// </summary>
    [Fact]
    public async Task MusterininYetkilisiPasifse_ElleSecilenYazilir()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountId = await CreateAccountAsync(admin);
        var ayrilan = await admin.CreateUserAsync($"ayrilan-{Guid.NewGuid():N}@example.test");
        var secilen = await admin.CreateUserAsync($"secilen-{Guid.NewGuid():N}@example.test");
        await LinkRepresentativeAsync(accountId, ayrilan, active: false);

        var loadId = await SaveOfferAsync(admin, accountId, requestedOfficerId: secilen);

        (await OfficerOfAsync(loadId)).Should().Be(secilen);
    }

    /// <summary>
    /// Var olmayan bir kimlik yazılmaz: Siber'e boş kod gönderilir, teklif
    /// aktarılamaz hâle gelirdi.
    /// </summary>
    [Fact]
    public async Task ElleSecilenKullaniciYoksa_KaydedeneDuser()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountId = await CreateAccountAsync(admin);

        var loadId = await SaveOfferAsync(admin, accountId, requestedOfficerId: 999_999_999);

        (await OfficerOfAsync(loadId)).Should().Be(await AdminIdAsync());
    }

    /// <summary>
    /// Cari temsilcileri ucu da AKTİF süzgecini uygular — arayüz, sunucunun
    /// kaydetmeyeceği bir kişiyi "tanımlı" diye göstermemeli.
    /// </summary>
    [Fact]
    public async Task TemsilciUcu_PasifKullaniciyiDondurmez()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountId = await CreateAccountAsync(admin);
        var ayrilan = await admin.CreateUserAsync($"ayrilan-{Guid.NewGuid():N}@example.test");
        await LinkRepresentativeAsync(accountId, ayrilan, active: false);

        var response = await admin.GetAsync($"/api/v1/account/{accountId}/representatives");
        response.EnsureSuccessStatusCode();

        var data = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        data.GetProperty("operation_officer").ValueKind.Should().Be(JsonValueKind.Null);
    }
}
