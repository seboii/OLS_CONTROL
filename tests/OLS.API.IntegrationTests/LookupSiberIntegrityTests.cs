using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OLS.DataAccess.Context;

namespace OLS.API.IntegrationTests;

/// <summary>
/// Regresyon: teklifsiz yük açma "beklenmeyen bir hata oluştu" ile düşüyordu.
///
/// Sebep, formdaki açılır listelerin Siber'de KARŞILIĞI OLMAYAN seçenekler
/// içermesiydi. Listeler yerel tablolardan besleniyor ama kayıt Siber'e
/// yazılıyor; karşılığı olmayan bir seçenek INSERT'i düşürüyordu. En sert
/// olanı departman: <c>skn_yuk.departmanid</c> FK'li
/// (FK_skn_yuk_sbr_departman_departmanid), yani sahte departman seçimi
/// INSERT'i kesin olarak düşürüyordu.
///
/// Öksüz satırların kaynağı ikiliydi: taklit Siber'den (infra/docker/siber-mock)
/// gerçek veritabanına sızmış kayıtlar ve DbSeeder'ın karşılığı doğrulanmamış
/// başlangıç satırları ("Vadeli" ödeme tipi, "Parsiyel" yükleme tipi).
///
/// Bu testler HİÇBİR Siber import'u çalışmadan, yalnızca uygulamanın kendi
/// başlangıç seed'ine bakar: tohumlanan her seçeneğin bir <c>SiberId</c>'si
/// olmalı ve bu değer gerçek bir GUID olmalı. Taklit veriden gelen
/// "ref-yuklemetip-0" gibi değerler tam olarak bu ikinci koşulda yakalanır.
/// </summary>
[Collection("OlsApi")]
public sealed class LookupSiberIntegrityTests
{
    private readonly OlsApiFactory _factory;

    public LookupSiberIntegrityTests(OlsApiFactory factory) => _factory = factory;

    /// <summary>
    /// Tohumlanan her tanım satırı Siber'deki GERÇEK GUID'ini taşımalı.
    ///
    /// Beklenen değerler 192.168.1.101 üzerindeki gerçek sunucudan salt-okunur
    /// sorguyla alındı (<c>skn_sabittanim</c> / <c>sbr_departman</c> /
    /// <c>sbr_odemesekli</c>). Aynı veritabanını paylaşan başka testler kendi
    /// geçici satırlarını ekleyebildiği için "tabloda başka satır olmasın"
    /// denmiyor; tohumlanan satırın DOĞRU GUID ile var olduğu aranıyor.
    /// </summary>
    [Theory]
    [InlineData("Departman", "İdari İşler", "3416B6FC-2323-4471-B0AD-12B673317109")]
    [InlineData("Departman", "İhracat Operasyon", "D919053A-2CF0-4CB7-AD77-C487D312A71C")]
    [InlineData("Departman", "İthalat Operasyon", "4575BDF4-B72F-44D0-BFA9-7C63BBD913F5")]
    [InlineData("Departman", "Muhasebe & Finans", "CD95920F-12E3-48ED-821C-620A7442240E")]
    [InlineData("Departman", "Satış & Pazarlama", "C249E951-FB3F-4FF9-A1C4-EF0223A00B75")]
    [InlineData("Departman", "Transit Operasyon", "33289770-585F-4AFC-A007-C699CA8F7FBB")]
    [InlineData("Departman", "Yönetim", "DB3B6E91-B9D4-430B-BE96-AD5030EBC967")]
    [InlineData("İş türü", "İhracat", "1704A279-D076-4C38-B448-D8047FB6193D")]
    [InlineData("İş türü", "İthalat", "EA147918-3714-4DEF-A379-A44DF2233F7E")]
    [InlineData("İş türü", "Transit", "0A99104E-1523-44B4-A986-C8529DDEDA21")]
    [InlineData("İş türü", "Yurtiçi", "577D934A-BB8F-48DA-9322-1633CC1F5241")]
    [InlineData("Ödeme tipi", "Peşin", "97081C47-4F6A-4F37-9557-BC1CAC802106")]
    public async Task SeededLookup_CarriesRealSiberId(string kind, string name, string expectedSiberId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var ids = kind switch
        {
            "Departman" => await db.Departments.AsNoTracking()
                .Where(x => x.Name == name).Select(x => x.SiberId).ToListAsync(),
            "İş türü" => await db.WorkTypes.AsNoTracking()
                .Where(x => x.Name == name).Select(x => x.SiberId).ToListAsync(),
            "Ödeme tipi" => await db.PaymentTypes.AsNoTracking()
                .Where(x => x.Name == name).Select(x => x.SiberId).ToListAsync(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        ids.Select(x => x?.ToUpperInvariant()).Should().Contain(expectedSiberId,
            $"{kind} \"{name}\" Siber'de bu GUID ile duruyor; karşılığı olmayan " +
            "seçenek yük oluşturmayı düşürür");
    }

    /// <summary>
    /// Taklit Siber'den (infra/docker/siber-mock) sızmış kimlikler hiçbir tanım
    /// tablosunda bulunmamalı. Canlıda tam olarak bunlar vardı: departmanlarda
    /// <c>88888888-…</c>, ödeme tiplerinde <c>77777777-…</c>, yükleme tipinde
    /// GUID bile olmayan <c>ref-yuklemetip-0</c>.
    /// </summary>
    [Fact]
    public async Task Lookups_HaveNoMockSiberIds()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var suspects = new List<string>();

        void Check(string label, IEnumerable<(string Name, string? SiberId)> rows)
        {
            suspects.AddRange(rows
                .Where(r => r.SiberId is not null
                            && (r.SiberId.StartsWith("88888888-", StringComparison.Ordinal)
                                || r.SiberId.StartsWith("77777777-", StringComparison.Ordinal)
                                || r.SiberId.StartsWith("ref-", StringComparison.Ordinal)))
                .Select(r => $"{label}: {r.Name} ({r.SiberId})"));
        }

        Check("Departman", await db.Departments.AsNoTracking()
            .Select(x => new ValueTuple<string, string?>(x.Name, x.SiberId)).ToListAsync());
        Check("Ödeme tipi", await db.PaymentTypes.AsNoTracking()
            .Select(x => new ValueTuple<string, string?>(x.Name, x.SiberId)).ToListAsync());
        Check("Yükleme tipi", await db.LoadingTypes.AsNoTracking()
            .Select(x => new ValueTuple<string, string?>(x.Name, x.SiberId)).ToListAsync());
        Check("İş türü", await db.WorkTypes.AsNoTracking()
            .Select(x => new ValueTuple<string, string?>(x.Name, x.SiberId)).ToListAsync());

        suspects.Should().BeEmpty("taklit Siber verisi gerçek veritabanına bulaşmamalı");
    }

    /// <summary>
    /// Yükleme tipi listesi gerçek <c>skn_sabittanim</c>(YUKLEMETIP) ile birebir:
    /// GRUPAJ(0) / KOMPLE(1) / CO-LOAD(2). "Parsiyel" ile "grupaj" aynı şey
    /// (LTL / konsolide kısmi yük) ama Siber'in kullandığı ad GRUPAJ; ikisi
    /// birlikte tohumlandığında AYNI kodu ("0") taşıdıkları için senkron
    /// eşlemesi (ByCode, ilk gelen kazanır ve satır sırası garanti değil)
    /// Siber'in GRUPAJ kayıtlarını yerelde rastgele birine bağlıyordu.
    /// </summary>
    [Fact]
    public async Task SeededLoadingTypes_MatchSiberDomainExactly()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        // Yalnızca Siber karşılığı OLAN satırlara bakılır: aynı veritabanını
        // paylaşan başka testler kendi geçici satırlarını ekleyebiliyor.
        var siberBacked = await db.LoadingTypes.AsNoTracking()
            .Where(x => x.SiberId != null && x.SiberId != "")
            .Select(x => new { x.Name, x.Code, x.SiberId })
            .ToListAsync();

        siberBacked.Select(x => x.SiberId!.ToUpperInvariant()).Should().BeEquivalentTo(
        [
            "6F8B8B0E-357E-446B-99AC-E365E70AABED", // GRUPAJ  (kod 0)
            "DDA7585E-B003-4594-A261-131C046F6031", // KOMPLE  (kod 1)
            "3456324E-2FDF-4D50-AB3A-29A6F218DFA7", // CO-LOAD (kod 2)
        ]);

        siberBacked.Select(x => x.Code).Should().OnlyHaveUniqueItems(
            "aynı kod iki satırda olursa senkron eşlemesi (ByCode) belirsizleşir");
    }
}
