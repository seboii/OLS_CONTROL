using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OLS.Business.Services.Siber;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;
using OLS.DataAccess.Siber;

namespace OLS.API.IntegrationTests;

/// <summary>
/// Regresyon: yük Siber'e ÜLKE ADIYLA yazılır, ülke GUID'iyle değil.
///
/// Canlıda doğrulanan üç gerçek:
///
///   * <c>skn_yuk</c>'ta ülke için kimlik sütunu YOK (400 sütun tarandı).
///     Ülke yalnızca <c>_yuklemeulke</c>/<c>_bosaltmaulke</c> metin sütunlarında,
///     ADIYLA duruyor — dolu 7.486 satırın hiçbirinde GUID yok. Uygulama buraya
///     yerel GUID yazıyordu, yani Siber'deki her yeni yük okunamaz bir ülke
///     değeriyle açılıyordu.
///   * Kıta ayrı sütun (<c>_yuklemekita</c>), ülkeden türüyor ve uygulama sabit
///     "ASYA" yazıyordu; canlıdaki 7.486 yükün 5.793'ü AVRUPA.
///   * Yerel <c>countries.id</c> Siber'in <c>ulkeid</c>'si DEĞİL: 197 ülkenin
///     171'inde tesadüfen aynı, 26'sında farklı.
///
/// Çözümleyici bu yüzden üç girdi biçimini de tanımalı: yerel kimlik, Siber
/// kimliği ve düz ülke adı (yerel ayna, senkronda ADI saklıyor).
/// </summary>
[Collection("OlsApi")]
public sealed class SiberCountryResolverTests
{
    private const string SiberUlkeId = "9F1C0A11-0000-4000-8000-000000000001";
    private const string CountryName = "TESTONYA";

    private readonly OlsApiFactory _factory;

    public SiberCountryResolverTests(OlsApiFactory factory) => _factory = factory;

    /// <summary>Siber'i taklit eden depo: tek ülke tanır, adı ve kıtasıyla.</summary>
    private sealed class FakeCountryRepository : ISiberCountryRepository
    {
        public Task<IReadOnlyDictionary<string, SiberCountryRow>> GetAsync(
            IReadOnlyCollection<string> ulkeIds, CancellationToken cancellationToken = default)
        {
            var map = new Dictionary<string, SiberCountryRow>(StringComparer.OrdinalIgnoreCase);

            foreach (var id in ulkeIds.Where(i => string.Equals(i, SiberUlkeId, StringComparison.OrdinalIgnoreCase)))
                map[id] = new SiberCountryRow(id, "TESTONYA CUMHURİYETİ", "AVRUPA");

            return Task.FromResult<IReadOnlyDictionary<string, SiberCountryRow>>(map);
        }
    }

    [Fact]
    public async Task Resolve_MapsLocalIdSiberIdAndName_ToSiberNameAndContinent()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        // Yerel kimliği Siber kimliğinden FARKLI bir ülke — canlıdaki 26 ülkenin
        // durumu. Ayrıca aynı Siber ülkesinin ikinci bir yerel satırı ("TÜRKİYE"
        // ve "Türkiye" canlıda tam olarak böyle) çözümü bozmamalı.
        var localId = Guid.NewGuid();
        db.Countries.AddRange(
            new Country { Id = localId, Name = CountryName, SiberId = SiberUlkeId },
            new Country { Id = Guid.NewGuid(), Name = CountryName + " (kopya)", SiberId = SiberUlkeId });
        await db.SaveChangesAsync();

        try
        {
            var resolver = new SiberCountryResolver(db, new FakeCountryRepository());

            var map = await resolver.ResolveAsync(
                [localId.ToString(), SiberUlkeId, CountryName, "hicbiryerde-yok"]);

            foreach (var input in new[] { localId.ToString(), SiberUlkeId, CountryName })
            {
                map.Should().ContainKey(input);
                map[input].SiberId.Should().BeEquivalentTo(SiberUlkeId);
                map[input].Name.Should().Be("TESTONYA CUMHURİYETİ",
                    "Siber'in _yuklemeulke sütunu ülke ADINI ister, GUID'i değil");
                map[input].Continent.Should().Be("AVRUPA",
                    "_yuklemekita ülkeden türer, sabit ASYA değildir");
            }

            map.Should().NotContainKey("hicbiryerde-yok");
        }
        finally
        {
            db.Countries.RemoveRange(
                await db.Countries.Where(c => c.Name!.StartsWith(CountryName)).ToListAsync());
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Siber'de karşılığı bulunamayan ülkede ad ve kıta boş döner ama KİMLİK
    /// dolu kalır — çağıran akış bunu "seçilmiş ama Siber'de yok" diye
    /// raporlayabilsin diye (bkz. SiberReferenceTable.Ulke).
    /// </summary>
    [Fact]
    public async Task Resolve_KeepsSiberId_WhenCountryMissingInSiber()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var localId = Guid.NewGuid();
        var orphanSiberId = "9F1C0A11-0000-4000-8000-0000000000FF";
        db.Countries.Add(new Country { Id = localId, Name = CountryName + " ÖKSÜZ", SiberId = orphanSiberId });
        await db.SaveChangesAsync();

        try
        {
            var resolver = new SiberCountryResolver(db, new FakeCountryRepository());

            var map = await resolver.ResolveAsync([localId.ToString()]);

            map[localId.ToString()].SiberId.Should().BeEquivalentTo(orphanSiberId);
            map[localId.ToString()].Name.Should().BeNull();
            map[localId.ToString()].Continent.Should().BeNull();
        }
        finally
        {
            db.Countries.RemoveRange(
                await db.Countries.Where(c => c.Name!.StartsWith(CountryName)).ToListAsync());
            await db.SaveChangesAsync();
        }
    }
}
