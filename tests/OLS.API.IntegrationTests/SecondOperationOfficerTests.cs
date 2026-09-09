using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OLS.Business.Services.TransferSiber;
using OLS.DataAccess.Context;
using OLS.DataAccess.Siber;

namespace OLS.API.IntegrationTests;

/// <summary>
/// 2. OPERASYON YETKİLİSİ SİBER'E YAZILIYOR MU?
///
/// Siber'in teklif kaydında yetkili için İKİ sütun var ve biçimleri FARKLI:
///   • <c>musteritemsilcisi</c>      → 1. yetkilinin ADI  (sky_kullanici.ad)
///   • <c>operasyonyetkilisikod2</c> → 2. yetkilinin KODU (sky_kullanici.kod)
///
/// Canlıda 19.613 rezervasyonun 17.135'inde ikinci sütun dolu ve 17.124'ü bir
/// kullanıcı koduyla eşleşiyor — yani alan gerçekten kullanılıyor.
///
/// Bu testler zincirin TAMAMINI yürütür: form iki yetkili gönderir →
/// load_charge_people'a SIRAYLA iki satır yazılır → aktarım 2. yetkilinin
/// KODUNU doğru sütuna koyar.
/// </summary>
[Collection("OlsApi")]
public sealed class SecondOperationOfficerTests
{
    private readonly OlsApiFactory _factory;

    public SecondOperationOfficerTests(OlsApiFactory factory) => _factory = factory;

    /// <summary>Siber kodu/adı olan kullanıcı — yetkili olarak seçilebilsin.</summary>
    private async Task<long> AddSiberUserAsync(string name, string code)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var user = new DataAccess.Entities.User
        {
            Name = name,
            Surname = "-",
            Email = $"{code.ToLowerInvariant()}-{Guid.NewGuid():N}@example.test",
            Password = "x",
            SiberName = name,
            SiberCode = code,
            SiberId = Guid.NewGuid().ToString(),
            Status = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    private async Task<long> SaveOfferAsync(HttpClient admin, params long[] officerIds)
    {
        using var accountForm = await TestAccountHelper.MinimalAccountFormAsync(
            admin, $"OY2 Testi {Guid.NewGuid():N}"[..26]);
        var accountResponse = await admin.PostAsync("/api/v1/account", accountForm);
        accountResponse.EnsureSuccessStatusCode();
        var accountId = (await accountResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();

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

        for (var i = 0; i < officerIds.Length; i++)
        {
            form.Add(new StringContent(officerIds[i].ToString()), $"load_charge_person[{i}][user_id]");
            form.Add(new StringContent("1"), $"load_charge_person[{i}][user_type]");
        }

        var response = await admin.PostAsync("/api/v1/load", form);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();
    }

    [Fact]
    public async Task IkiYetkiliKaydedilir_SiberdeIkinciSutunaKODYazilir()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var birinci = await AddSiberUserAsync("MAKSIM MOROZOV", $"MAKS{Random.Shared.Next(1000, 9999)}");
        var ikinci = await AddSiberUserAsync("HEDIYE ARIDICI", $"HEDI{Random.Shared.Next(1000, 9999)}");

        var loadId = await SaveOfferAsync(admin, birinci, ikinci);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<OLS.Business.Common.IClock>();

        // İKİ SATIR, SIRAYLA yazılmış olmalı — sıra Siber'de hangi sütuna
        // gideceğini belirliyor.
        var officers = await db.LoadChargePeople.AsNoTracking()
            .Where(p => p.LoadId == (int)loadId && p.UserType == 1)
            .OrderBy(p => p.Id).Select(p => (long)p.UserId!).ToListAsync();

        officers.Should().Equal(birinci, ikinci);

        var load = await db.Loads.FirstAsync(l => l.Id == loadId);
        load.SiberId = Guid.NewGuid().ToString();
        load.TransferToSiber = 1;
        await db.SaveChangesAsync();

        var siber = new RecordingReservationRepository();
        var service = new TransferSiberService(
            db, siber, clock, new PassThroughReferenceValidator(), new FakeSiberCountryResolver(db));

        var result = await service.TransferOfferAsync(loadId, currentUserId: 1);

        result.IsSuccess.Should().BeTrue(result.ErrorMessage);
        siber.Written.Should().NotBeNull();

        var ikinciKod = await db.Users.Where(u => u.Id == ikinci)
            .Select(u => u.SiberCode).FirstAsync();
        var birinciAd = await db.Users.Where(u => u.Id == birinci)
            .Select(u => u.SiberName).FirstAsync();

        // 1. yetkili AD, 2. yetkili KOD — biçimler farklı, karıştırılmamalı.
        siber.Written!.MusteriTemsilcisi.Should().Be(birinciAd);
        siber.Written!.OperasyonYetkilisiKod2.Should().Be(ikinciKod);
    }

    /// <summary>
    /// Tek yetkili seçilirse ikinci sütun BOŞ gider — UPDATE tarafındaki ISNULL
    /// sayesinde Siber'de zaten dolu olan değer silinmez.
    /// </summary>
    [Fact]
    public async Task TekYetkiliVarsa_IkinciSutunBosGider()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var birinci = await AddSiberUserAsync("UMUT AKBAS", $"UMUT{Random.Shared.Next(1000, 9999)}");

        var loadId = await SaveOfferAsync(admin, birinci);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<OLS.Business.Common.IClock>();

        var load = await db.Loads.FirstAsync(l => l.Id == loadId);
        load.SiberId = Guid.NewGuid().ToString();
        load.TransferToSiber = 1;
        await db.SaveChangesAsync();

        var siber = new RecordingReservationRepository();
        var service = new TransferSiberService(
            db, siber, clock, new PassThroughReferenceValidator(), new FakeSiberCountryResolver(db));

        await service.TransferOfferAsync(loadId, currentUserId: 1);

        siber.Written!.OperasyonYetkilisiKod2.Should().BeNull();
    }
}

/// <summary>Siber'e gidecek rezervasyonu yakalar; hiçbir G/Ç yapmaz.</summary>
internal sealed class RecordingReservationRepository : ISiberReservationRepository
{
    public SiberRezervasyonYaz? Written { get; private set; }

    public bool IsConfigured => true;

    public Task<Guid> GenerateRezervasyonIdAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Guid.NewGuid());

    public Task<int> InsertRezervasyonWithLockedNumberAsync(
        SiberRezervasyonYaz rezervasyon, CancellationToken cancellationToken = default)
    {
        Written = rezervasyon;
        return Task.FromResult(2600001);
    }

    public Task UpdateRezervasyonAsync(
        SiberRezervasyonYaz rezervasyon, CancellationToken cancellationToken = default)
    {
        Written = rezervasyon;
        return Task.CompletedTask;
    }

    public Task<Guid> GenerateYukKoliIdAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Guid.NewGuid());
    public Task<Guid> GenerateTarifeIdAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Guid.NewGuid());
    public Task DeleteRezervasyonAsync(string rezervasyonId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
    public Task<bool> YukKoliExistsAsync(string yukKoliId, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
    public Task InsertRezervasyonYukKoliAsync(SiberRezervasyonYukKoli koli, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
    public Task UpdateRezervasyonYukKoliAsync(SiberRezervasyonYukKoli koli, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
    public Task<bool> TarifeExistsAsync(string tarifeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
    public Task<bool> KalemExistsAsync(string kalemId, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
    public Task<bool> ReferenceExistsAsync(
        string table, string idColumn, string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
    public Task InsertRezervasyonTarifeAsync(SiberRezervasyonTarife tarife, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
    public Task UpdateRezervasyonTarifeAsync(SiberRezervasyonTarife tarife, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
    public Task<IReadOnlyList<SiberRezervasyonKoliSatir>> ReadReservationPackagesAsync(
        string rezervasyonId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SiberRezervasyonKoliSatir>>([]);
    public Task<IReadOnlyList<SiberRezervasyonTarifeSatir>> ReadReservationTariffsAsync(
        string rezervasyonId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SiberRezervasyonTarifeSatir>>([]);
}
