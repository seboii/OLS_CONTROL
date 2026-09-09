using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OLS.Business.Common;
using OLS.Business.Services.Expeditions;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;
using OLS.DataAccess.Siber;

namespace OLS.API.IntegrationTests;

/// <summary>
/// SEFERE YÜK BAĞLANAMIYORDU — ŞEHRİN SİBER KİMLİĞİ ÇEVRİLMİYORDU.
///
/// <c>skn_yukaktarma.yerid</c> Siber'de <c>sbr_sehir</c>'e yabancı anahtarlı
/// (<c>FK_skn_yukaktarma_sbr_sehir</c>). Kod buraya
/// <c>expeditions.start_city_id</c>'yi, yani YEREL şehir kimliğini ham olarak
/// yazıyordu. Yerel <c>cities.id</c> ile Siber'in <c>sehirid</c>'si 351 şehrin
/// 248'inde FARKLI — dolayısıyla INSERT yabancı anahtar ihlaliyle düşüyor,
/// işlem geri alınıyor ve yük sefere HİÇ bağlanmıyordu.
///
/// Üretim veritabanında geri alınan bir denemeyle doğrulandı:
///   • yerel kimlikle → "The INSERT statement conflicted with the FOREIGN KEY
///     constraint FK_skn_yukaktarma_sbr_sehir"
///   • Siber kimliğiyle → başarılı
///
/// Etki: başlangıç şehri olan 3.222 seferin 324'ünde yazım kesin düşüyordu;
/// kalanlarda yerel kimlik tesadüfen Siber'inkiyle aynı olduğu için
/// çalışıyordu. Hata bu yüzden "bazen" değil ŞEHRE BAĞLI çıkıyordu.
///
/// ENTEGRASYON TESTİ TEK BAŞINA YAKALAYAMAZDI: test ortamında Siber bağlantısı
/// tanımsız (<c>IsConfigured=false</c>) ve Siber yazımı hiç çalışmıyor. Bu
/// yüzden servis, kaydı YAKALAYAN sahte bir depo ile doğrudan örnekleniyor.
/// </summary>
[Collection("OlsApi")]
public sealed class ExpeditionMappingCityIdTests
{
    private readonly OlsApiFactory _factory;

    public ExpeditionMappingCityIdTests(OlsApiFactory factory) => _factory = factory;

    [Fact]
    public async Task SefereYukBaglaninca_SehrinSiberKimligiYazilir()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        // cities.country_id NOT NULL — var olan bir ülkeye bağlanıyor.
        var countryId = await db.Countries.AsNoTracking()
            .OrderBy(c => c.Id).Select(c => c.Id).FirstAsync();

        // Yerel kimliği Siber kimliğinden FARKLI bir şehir — canlıda 351
        // şehrin 248'i böyle.
        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = $"TEST ŞEHİR {Guid.NewGuid():N}"[..24],
            CountryId = countryId.ToString(),
            SiberId = Guid.NewGuid().ToString().ToUpperInvariant(),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.Cities.Add(city);

        var car = new Car
        {
            PlateNumber = $"34 TST {Random.Shared.Next(1000, 9999)}",
            SiberId = Guid.NewGuid().ToString(),
            RomorkType = 7,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.Cars.Add(car);
        await db.SaveChangesAsync();

        var expedition = new Expedition
        {
            ExpeditionNumber = $"SEF-CITY-{Guid.NewGuid():N}",
            ExpeditionId = Guid.NewGuid().ToString(),
            RomorkId = (int)car.Id,
            StartCityId = city.Id,
            StatusId = 1,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.Expeditions.Add(expedition);

        var transfer = new LoadTransfer
        {
            LoadTransferId = Guid.NewGuid().ToString(),
            LoadNumber = $"YUK-CITY-{Guid.NewGuid():N}"[..20],
            LoadNumberWorkType = $"YUK-CITY-{Guid.NewGuid():N}"[..20],
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.LoadTransfers.Add(transfer);
        await db.SaveChangesAsync();

        var siber = new RecordingMappingRepository();
        var service = new ExpeditionLoadMappingService(
            db, siber, scope.ServiceProvider.GetRequiredService<ISiberArchiveRepository>(), clock);

        var result = await service.SaveAsync(expedition.Id, transfer.Id);

        result.IsSuccess.Should().BeTrue(result.ErrorMessage);
        siber.Written.Should().NotBeNull();

        // ASIL İDDİA: Siber'e ŞEHRİN SİBER KİMLİĞİ gider, yerel kimlik DEĞİL.
        siber.Written!.YerId.Should().Be(city.SiberId);
        siber.Written!.YerId.Should().NotBe(city.Id.ToString());
    }

    /// <summary>
    /// Şehrin Siber karşılığı yoksa null yazılır — sütun nullable, tek zorunlu
    /// alanlar yukaktarmaid ve yukid. Çözülemeyen kimliği ham yazmak yabancı
    /// anahtarı yine düşürürdü.
    /// </summary>
    [Fact]
    public async Task SehrinSiberKarsiligiYoksa_NullYazilir()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        // cities.country_id NOT NULL — var olan bir ülkeye bağlanıyor.
        var countryId = await db.Countries.AsNoTracking()
            .OrderBy(c => c.Id).Select(c => c.Id).FirstAsync();

        var city = new City
        {
            Id = Guid.NewGuid(),
            Name = $"SIBERSIZ {Guid.NewGuid():N}"[..24],
            CountryId = countryId.ToString(),
            SiberId = null,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.Cities.Add(city);

        var car = new Car
        {
            PlateNumber = $"34 TST {Random.Shared.Next(1000, 9999)}",
            SiberId = Guid.NewGuid().ToString(),
            RomorkType = 7,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.Cars.Add(car);
        await db.SaveChangesAsync();

        var expedition = new Expedition
        {
            ExpeditionNumber = $"SEF-NOCITY-{Guid.NewGuid():N}",
            ExpeditionId = Guid.NewGuid().ToString(),
            RomorkId = (int)car.Id,
            StartCityId = city.Id,
            StatusId = 1,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.Expeditions.Add(expedition);

        var transfer = new LoadTransfer
        {
            LoadTransferId = Guid.NewGuid().ToString(),
            LoadNumber = $"YUK-NOCITY-{Guid.NewGuid():N}"[..20],
            LoadNumberWorkType = $"YUK-NOCITY-{Guid.NewGuid():N}"[..20],
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.LoadTransfers.Add(transfer);
        await db.SaveChangesAsync();

        var siber = new RecordingMappingRepository();
        var service = new ExpeditionLoadMappingService(
            db, siber, scope.ServiceProvider.GetRequiredService<ISiberArchiveRepository>(), clock);

        var result = await service.SaveAsync(expedition.Id, transfer.Id);

        result.IsSuccess.Should().BeTrue(result.ErrorMessage);
        siber.Written!.YerId.Should().BeNull();
    }
}

/// <summary>Siber'e gidecek eşleme kaydını yakalar; hiçbir G/Ç yapmaz.</summary>
internal sealed class RecordingMappingRepository : ISiberLoadMappingRepository
{
    public SiberYukAktarma? Written { get; private set; }

    public bool IsConfigured => true;

    public Task<Guid> GenerateYukAktarmaIdAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Guid.NewGuid());

    public Task InsertYukAktarmaAsync(SiberYukAktarma mapping, CancellationToken cancellationToken = default)
    {
        Written = mapping;
        return Task.CompletedTask;
    }

    public Task UpdateYukAktarmaAsync(SiberYukAktarma mapping, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task DeleteYukAktarmaAsync(string yukAktarmaId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task SetYukPozisyonAsync(
        string yukId, string? pozisyonId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
