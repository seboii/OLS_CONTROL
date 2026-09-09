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
    private const int SalesRepType = 2;
    private const int PricingUserType = 3;

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

    private async Task LinkRepresentativeAsync(
        long accountId, long userId, bool active, int userType = OperationOfficerType)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        db.AccountRepresentatives.Add(new AccountRepresentative
        {
            AccountId = (int)accountId,
            UserId = (int)userId,
            UserType = userType,
        });

        if (!active)
        {
            var user = await db.Users.FirstAsync(u => u.Id == userId);
            user.Status = false;
        }

        await db.SaveChangesAsync();
    }

    private async Task<long> SaveOfferAsync(
        HttpClient client, long accountId, long? requestedPricingUserId = null,
        params long[] requestedOfficerIds)
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

        for (var i = 0; i < requestedOfficerIds.Length; i++)
        {
            form.Add(new StringContent(requestedOfficerIds[i].ToString()), $"load_charge_person[{i}][user_id]");
            form.Add(new StringContent("1"), $"load_charge_person[{i}][user_type]");
        }

        if (requestedPricingUserId is { } pricingId)
        {
            var i = requestedOfficerIds.Length;
            form.Add(new StringContent(pricingId.ToString()), $"load_charge_person[{i}][user_id]");
            form.Add(new StringContent("3"), $"load_charge_person[{i}][user_type]");
        }

        var response = await client.PostAsync("/api/v1/load", form);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();
    }

    /// <summary>Yükün operasyon yetkilileri, EKLEME SIRASINDA — sıra anlamlı.</summary>
    private async Task<List<long?>> OfficersOfAsync(long loadId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        return await db.LoadChargePeople.AsNoTracking()
            .Where(p => p.LoadId == (int)loadId && p.UserType == OperationOfficerType)
            .OrderBy(p => p.Id)
            .Select(p => (long?)p.UserId)
            .ToListAsync();
    }

    private async Task<long?> OfficerOfAsync(long loadId) =>
        (await OfficersOfAsync(loadId)).FirstOrDefault();

    private async Task<long?> ChargePersonAsync(long loadId, int userType)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        return await db.LoadChargePeople.AsNoTracking()
            .Where(p => p.LoadId == (int)loadId && p.UserType == userType)
            .OrderBy(p => p.Id)
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
    public async Task IstekteYetkiliYoksa_MusterinInkiYazilir()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountId = await CreateAccountAsync(admin);
        var officerId = await admin.CreateUserAsync($"yetkili-{Guid.NewGuid():N}@example.test");
        await LinkRepresentativeAsync(accountId, officerId, active: true);

        var loadId = await SaveOfferAsync(admin, accountId);

        (await OfficerOfAsync(loadId)).Should().Be(officerId);
    }

    /// <summary>
    /// "düzenlensin" — müşteriye tanımlı yetkili artık ÖN DOLDURUR, kilitlemez:
    /// formdan gelen seçim onun yerine yazılır.
    /// </summary>
    [Fact]
    public async Task ElleSecim_MusterininYetkilisiniEzer()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountId = await CreateAccountAsync(admin);
        var officerId = await admin.CreateUserAsync($"yetkili-{Guid.NewGuid():N}@example.test");
        var baskasi = await admin.CreateUserAsync($"baskasi-{Guid.NewGuid():N}@example.test");
        await LinkRepresentativeAsync(accountId, officerId, active: true);

        var loadId = await SaveOfferAsync(admin, accountId, null, baskasi);

        (await OfficersOfAsync(loadId)).Should().Equal(baskasi);
    }

    /// <summary>
    /// İKİ YETKİLİ, GÖNDERİLEN SIRADA. Sıra anlamlı: 1. yetkili Siber'de
    /// musteritemsilcisi, 2.'si operasyonyetkilisikod2 sütununa yazılıyor.
    /// </summary>
    [Fact]
    public async Task IkiYetkili_GonderilenSirayaGoreYazilir()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountId = await CreateAccountAsync(admin);
        var birinci = await admin.CreateUserAsync($"birinci-{Guid.NewGuid():N}@example.test");
        var ikinci = await admin.CreateUserAsync($"ikinci-{Guid.NewGuid():N}@example.test");

        var loadId = await SaveOfferAsync(admin, accountId, null, birinci, ikinci);

        (await OfficersOfAsync(loadId)).Should().Equal(birinci, ikinci);
    }

    /// <summary>İkiden fazlası yazılmaz — Siber'de yalnızca iki sütun var.</summary>
    [Fact]
    public async Task IkidenFazlaYetkili_IlkIkisiYazilir()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountId = await CreateAccountAsync(admin);
        var birinci = await admin.CreateUserAsync($"birinci-{Guid.NewGuid():N}@example.test");
        var ikinci = await admin.CreateUserAsync($"ikinci-{Guid.NewGuid():N}@example.test");
        var ucuncu = await admin.CreateUserAsync($"ucuncu-{Guid.NewGuid():N}@example.test");

        var loadId = await SaveOfferAsync(admin, accountId, null, birinci, ikinci, ucuncu);

        (await OfficersOfAsync(loadId)).Should().Equal(birinci, ikinci);
    }

    /// <summary>"yoksa manuel girilebilsin".</summary>
    [Fact]
    public async Task MusterininYetkilisiYoksa_ElleSecilenYazilir()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountId = await CreateAccountAsync(admin);
        var secilen = await admin.CreateUserAsync($"secilen-{Guid.NewGuid():N}@example.test");

        var loadId = await SaveOfferAsync(admin, accountId, null, secilen);

        (await OfficerOfAsync(loadId)).Should().Be(secilen);
    }

    [Fact]
    public async Task MusterininYetkilisiYokVeElleSecimYoksa_KaydedenYazilir()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountId = await CreateAccountAsync(admin);

        var loadId = await SaveOfferAsync(admin, accountId);

        (await OfficerOfAsync(loadId)).Should().Be(await AdminIdAsync());
    }

    /// <summary>
    /// Ayrılmış personel "tanımlı yetkili" sayılmaz; ön doldurma da ona bakmaz.
    /// </summary>
    [Fact]
    public async Task MusterininYetkilisiPasifse_ElleSecilenYazilir()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountId = await CreateAccountAsync(admin);
        var ayrilan = await admin.CreateUserAsync($"ayrilan-{Guid.NewGuid():N}@example.test");
        var secilen = await admin.CreateUserAsync($"secilen-{Guid.NewGuid():N}@example.test");
        await LinkRepresentativeAsync(accountId, ayrilan, active: false);

        var loadId = await SaveOfferAsync(admin, accountId, null, secilen);

        (await OfficerOfAsync(loadId)).Should().Be(secilen);
    }

    /// <summary>
    /// Var olmayan bir kimlik yazılmaz: Siber'e boş ad/kod gönderilir, yük
    /// dönüşümü "Operasyon yetkilisinin Siber karşılığı yok" ile düşerdi.
    /// </summary>
    [Fact]
    public async Task ElleSecilenKullaniciYoksa_KaydedeneDuser()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountId = await CreateAccountAsync(admin);

        var loadId = await SaveOfferAsync(admin, accountId, null, 999_999_999);

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

        data.GetProperty("operation_officers").GetArrayLength().Should().Be(0);
    }

    /// <summary>
    /// FİYATLANDIRAN (user_type=3) — gönderilmezse 1. OPERASYON YETKİLİSİ.
    ///
    /// Siber'in kendi verisinde fiyatlandıran, dolu 7.952 teklifin 5.798'inde
    /// (%73) zaten 1. operasyon yetkilisiyle aynı kişi.
    /// </summary>
    [Fact]
    public async Task Fiyatlandiran_GonderilmezseOperasyonYetkilisiYazilir()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountId = await CreateAccountAsync(admin);
        var yetkili = await admin.CreateUserAsync($"yetkili-{Guid.NewGuid():N}@example.test");

        var loadId = await SaveOfferAsync(admin, accountId, null, yetkili);

        (await ChargePersonAsync(loadId, PricingUserType)).Should().Be(yetkili);
    }

    /// <summary>Elle seçilen fiyatlandıran yetkiliden BAĞIMSIZ yazılır (%27).</summary>
    [Fact]
    public async Task Fiyatlandiran_ElleSecilirse_OYazilir()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountId = await CreateAccountAsync(admin);
        var yetkili = await admin.CreateUserAsync($"yetkili-{Guid.NewGuid():N}@example.test");
        var fiyatlandiran = await admin.CreateUserAsync($"fiyat-{Guid.NewGuid():N}@example.test");

        var loadId = await SaveOfferAsync(admin, accountId, fiyatlandiran, yetkili);

        (await ChargePersonAsync(loadId, PricingUserType)).Should().Be(fiyatlandiran);
        (await OfficerOfAsync(loadId)).Should().Be(yetkili);
    }

    /// <summary>
    /// SATIŞ TEMSİLCİSİ TEK KİŞİ. Siber'in ikinci sütunu (satistemsilcisi2kod)
    /// 19.561 teklifin 540'ında dolu (%2,8) — kullanılmıyor. Eskiden cariye
    /// tanımlı TÜM temsilciler yazılıyor ama Siber'e yalnızca ilki gidiyordu.
    /// </summary>
    [Fact]
    public async Task SatisTemsilcisi_CokKisiTanimliOlsaBileTekYazilir()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountId = await CreateAccountAsync(admin);

        for (var i = 0; i < 3; i++)
        {
            var userId = await admin.CreateUserAsync($"satis{i}-{Guid.NewGuid():N}@example.test");
            await LinkRepresentativeAsync(accountId, userId, active: true, userType: SalesRepType);
        }

        var loadId = await SaveOfferAsync(admin, accountId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        (await db.LoadChargePeople.AsNoTracking()
            .CountAsync(p => p.LoadId == (int)loadId && p.UserType == SalesRepType))
            .Should().Be(1);
    }

    /// <summary>
    /// Temsilci ucu EN FAZLA İKİ yetkili döner — teklif formunda iki alan var,
    /// fazlası ekranda görünmeyen ama "tanımlı" sanılan bir kişi bırakırdı.
    /// </summary>
    [Fact]
    public async Task TemsilciUcu_EnFazlaIkiYetkiliDondurur()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountId = await CreateAccountAsync(admin);

        for (var i = 0; i < 3; i++)
        {
            var userId = await admin.CreateUserAsync($"yetkili{i}-{Guid.NewGuid():N}@example.test");
            await LinkRepresentativeAsync(accountId, userId, active: true);
        }

        var response = await admin.GetAsync($"/api/v1/account/{accountId}/representatives");
        response.EnsureSuccessStatusCode();

        var data = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        data.GetProperty("operation_officers").GetArrayLength().Should().Be(2);
    }
}
