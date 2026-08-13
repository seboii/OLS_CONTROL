using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OLS.Business.Services.LoadTransfers;
using OLS.Business.Services.TransferSiber;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;
using OLS.DataAccess.Siber;

namespace OLS.API.IntegrationTests;

/// <summary>
/// Teklif→Yük dönüşüm zincirinin iki adımı (transfer_to_siber, load_transfer) için
/// BR-002/003/004/005 iş kurallarını ve Siber-yapılandırılmamış davranışlarını kilitler.
///
/// Bu test ortamında <c>ConnectionStrings:Siber</c> bilinçli olarak tanımsız (bkz.
/// OlsApiFactory) — bu yüzden HTTP üzerinden çağrılan `transfer_to_siber`/`load_transfer`
/// uçları her zaman "yapılandırılmamış" hatasıyla ERKEN dönerler ve BR kuralları hiç
/// çalışmaz. Bu, tam olarak canlı Docker'da bulunan (ama Siber kimlik eşlemesi eksik
/// olduğu için tamamlanamayan) durumun aynısı — bkz. TESLIM-RAPORU.md §8. BR kurallarının
/// kendisini test etmek için ilgili servisler DI'dan değil, sahte (fake) bir
/// ISiberLoadRepository/ISiberReservationRepository (IsConfigured=true, hiçbir Siber
/// G/Ç'sine ULAŞILMAMASI beklenir) ile DOĞRUDAN örnekleniyor.
/// </summary>
[Collection("OlsApi")]
public sealed class TransferSiberTests
{
    private readonly OlsApiFactory _factory;

    public TransferSiberTests(OlsApiFactory factory) => _factory = factory;

    // ── HTTP üzerinden: gerçek "Siber yapılandırılmamış" davranışı ──────────────

    [Fact]
    public async Task TransferToSiber_WhenSiberNotConfigured_ReturnsBadRequestWithMessage()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.PostAsJsonAsync("/api/v1/transfer_to_siber", new { id = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").GetProperty("message")[0].GetString()
            .Should().Be("Siber bağlantısı yapılandırılmamış.");
    }

    /// <summary>Bu, TESLIM-RAPORU.md §8'de bahsedilen, o ana dek testsiz kalan "Siber-503" davranışı.</summary>
    [Fact]
    public async Task LoadSave_WhenSiberNotConfigured_ReturnsServiceUnavailable()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.PostAsJsonAsync(
            "/api/v1/transfer_to_siber/loadSave", new { id = "some-siber-reservation-id" });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task LoadSave_WithoutId_ReturnsValidationError_BeforeCheckingSiberConfiguration()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.PostAsJsonAsync("/api/v1/transfer_to_siber/loadSave", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Doğrudan servis örneklemesiyle: BR kurallarının kendisi ─────────────────

    [Fact]
    public async Task TransferOfferAsync_WhenLoadAlreadyHasLoadNumber_ReturnsError()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<OLS.Business.Common.IClock>();

        var load = new Load
        {
            TransferToSiber = 1,
            SiberId = Guid.NewGuid().ToString(),
            LoadNumber = "26I0001", // zaten yüke dönüşmüş
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.Loads.Add(load);
        await db.SaveChangesAsync();

        var service = new TransferSiberService(db, new FakeSiberReservationRepository(isConfigured: true), clock);

        var result = await service.TransferOfferAsync(load.Id, currentUserId: 1);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Yük oluşturulmuş teklifde güncelleme yapamazsınız.");
    }

    /// <summary>
    /// Canlı Docker'da bulunan gerçek davranışın (bkz. TESLIM-RAPORU.md §8 "Siber kimlik
    /// eşleşmesi kısıtı") doğrudan servis seviyesinde regresyon testi: payment_types
    /// tablosunda hiçbir satırın siber_id'si dolu değilse (bu ortamda GERÇEKTEN böyle),
    /// transfer_to_siber HER ZAMAN bu mesajla reddeder.
    /// </summary>
    [Fact]
    public async Task TransferOfferAsync_WithoutPaymentType_ReturnsPaymentTypeRequiredError()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<OLS.Business.Common.IClock>();

        var load = new Load
        {
            TransferToSiber = 0,
            PaymentTypeId = null,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.Loads.Add(load);
        await db.SaveChangesAsync();

        var service = new TransferSiberService(db, new FakeSiberReservationRepository(isConfigured: true), clock);

        var result = await service.TransferOfferAsync(load.Id, currentUserId: 1);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Ödeme şekli boş olamaz");
    }

    [Fact]
    public async Task ConvertOfferAsync_WhenLoadAlreadyConverted_ReturnsBR002Error()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<OLS.Business.Common.IClock>();

        var siberId = Guid.NewGuid().ToString();
        var load = new Load
        {
            SiberId = siberId,
            TransferToSiber = 1,
            LoadNumber = "26I0002", // BR-002: zaten oluşmuş
            StatusTypeId = 5,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.Loads.Add(load);
        await db.SaveChangesAsync();

        var service = new LoadTransferWriteService(db, new FakeSiberLoadRepository(isConfigured: true), clock);

        var result = await service.ConvertOfferAsync(siberId, currentUserId: 1);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Bu yük zaten oluşturuldu");
    }

    [Fact]
    public async Task ConvertOfferAsync_WhenStatusNotApproved_ReturnsBR003Error()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<OLS.Business.Common.IClock>();

        var siberId = Guid.NewGuid().ToString();
        var load = new Load
        {
            SiberId = siberId,
            TransferToSiber = 1,
            LoadNumber = null,
            StatusTypeId = 4, // Teklif (OFFER) - Olumlu (5) değil
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.Loads.Add(load);
        await db.SaveChangesAsync();

        var service = new LoadTransferWriteService(db, new FakeSiberLoadRepository(isConfigured: true), clock);

        var result = await service.ConvertOfferAsync(siberId, currentUserId: 1);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Yük durumu Olumlu değil");
    }

    [Fact]
    public async Task ConvertOfferAsync_WhenNotTransferredToSiber_ReturnsBR004Error()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<OLS.Business.Common.IClock>();

        var siberId = Guid.NewGuid().ToString();
        var load = new Load
        {
            SiberId = siberId,
            TransferToSiber = 0, // BR-004: henüz Siber'e aktarılmamış
            LoadNumber = null,
            StatusTypeId = 5,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.Loads.Add(load);
        await db.SaveChangesAsync();

        var service = new LoadTransferWriteService(db, new FakeSiberLoadRepository(isConfigured: true), clock);

        var result = await service.ConvertOfferAsync(siberId, currentUserId: 1);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Önce Teklif Oluşturun");
    }
}

/// <summary>
/// Test dublörü: BR-002/003/004/005 kontrollerinin hepsi gerçek Siber G/Ç'sinden ÖNCE
/// çalışır — bu yüzden aşağıdaki metotların hiçbiri bu testlerde ÇAĞRILMAMALI. Çağrılırsa
/// (kontrollerden biri beklenenden geç devreye giriyorsa) test NotSupportedException ile
/// gürültülü şekilde başarısız olur, sessizce yanlış geçmez.
/// </summary>
internal sealed class FakeSiberLoadRepository : ISiberLoadRepository
{
    public FakeSiberLoadRepository(bool isConfigured) => IsConfigured = isConfigured;

    public bool IsConfigured { get; }

    public Task<SiberRezervasyon?> FindRezervasyonAsync(string rezervasyonId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<int> NextYukNoAsync(string? isTuru, string year, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<Guid> GenerateYukIdAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<Guid> GenerateYukKoliIdAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<Guid> GenerateModulKalemIdAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<SiberModulKayit?> FindModulKayitAsync(string loadNumberWorkType, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task InsertYukAsync(SiberYuk yuk, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task InsertYukKoliAsync(SiberYukKoli koli, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task InsertModulKalemAsync(SiberModulKalem kalem, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task DeleteYukKoliAsync(string yukKoliId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task DeleteModulKalemAsync(string modulKalemId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task UpdateYukAsync(SiberYuk yuk, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task UpdateYukKoliAsync(SiberYukKoli koli, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task UpdateModulKalemAsync(SiberModulKalem kalem, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

internal sealed class FakeSiberReservationRepository : ISiberReservationRepository
{
    public FakeSiberReservationRepository(bool isConfigured) => IsConfigured = isConfigured;

    public bool IsConfigured { get; }

    public Task<Guid> GenerateRezervasyonIdAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<Guid> GenerateYukKoliIdAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<Guid> GenerateTarifeIdAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<int> NextRezervasyonNoAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task InsertRezervasyonAsync(SiberRezervasyonYaz rezervasyon, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task UpdateRezervasyonAsync(SiberRezervasyonYaz rezervasyon, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<bool> YukKoliExistsAsync(string yukKoliId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task InsertRezervasyonYukKoliAsync(SiberRezervasyonYukKoli koli, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task UpdateRezervasyonYukKoliAsync(SiberRezervasyonYukKoli koli, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<bool> TarifeExistsAsync(string tarifeId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<IReadOnlyList<SiberRezervasyonKoliSatir>> ReadReservationPackagesAsync(string reservationId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<IReadOnlyList<SiberRezervasyonTarifeSatir>> ReadReservationTariffsAsync(string reservationId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task InsertRezervasyonTarifeAsync(SiberRezervasyonTarife tarife, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task UpdateRezervasyonTarifeAsync(SiberRezervasyonTarife tarife, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
