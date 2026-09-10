using FluentAssertions;
using OLS.Business.Services.Expeditions;
using OLS.DataAccess.Entities;

namespace OLS.Business.Tests;

/// <summary>
/// "YOLDAKİ SEFER" KURALI — İKİ ŞİRKET İKİ FARKLI ŞEKİLDE ÇALIŞIYOR.
///
/// BULUNAN GERÇEK HATA: kural yalnızca sefer DURUMUNA bakıyordu ve Avrora'da
/// hiçbir sefer göstermiyordu. Canlı ölçüm sebebi net söylüyor:
///
/// • OLS durumu ilerletiyor — 16 sefer ÇIKIŞ YAPTI / YÜKLENDİ / YURT DIŞI
///   YOLDA / BOŞALTMADA durumlarında.
/// • AVRORA durumu HİÇ ilerletmiyor — 285 seferin 260'ı BOŞALTILDI, 25'i
///   HAZIR, arada başka durum YOK. Yolda olup olmadığı TARİHLERDEN okunuyor.
///
/// Kullanıcının bildirdiği "özmal araçlar yolda ama gözükmüyor" tam olarak
/// buydu. Ölçüm sonrası: AVRORA 0 → 5, OLS 16 → 21.
/// </summary>
public sealed class ExpeditionOnRoadTests
{
    private static readonly DateOnly Bugun = new(2026, 9, 10);

    private const long YoldaDurum = 5;
    private const long BosaltildiDurum = 9;
    private const long HazirDurum = 1;

    private static readonly long[] Yolda = [YoldaDurum];
    private static readonly long[] Bitmis = [BosaltildiDurum];

    private static bool YoldaMi(Expedition e) =>
        ExpeditionOnRoad.Predicate(Yolda, Bitmis, Bugun).Compile()(e);

    /// <summary>OLS yolu: durum ilerlemiş, tarihlere hiç bakılmıyor.</summary>
    [Fact]
    public void DurumuYoldaOlan_TarihOlmasaBileYolda()
    {
        var sefer = new Expedition { StatusId = (int)YoldaDurum };

        YoldaMi(sefer).Should().BeTrue();
    }

    /// <summary>
    /// AVRORA yolu: durum "HAZIR"da kalmış ama çıkmış ve dönmemiş.
    /// Eski kural bunu kaçırıyordu.
    /// </summary>
    [Fact]
    public void DurumIlerlememisAmaCikmisDonmemis_Yolda()
    {
        var sefer = new Expedition
        {
            StatusId = (int)HazirDurum,
            ReleaseDate = Bugun.AddDays(-6),
            ReturnDate = null,
        };

        YoldaMi(sefer).Should().BeTrue();
    }

    /// <summary>Dönüşü GELECEKTE olan sefer hâlâ yolda.</summary>
    [Fact]
    public void DonusuGelecekte_Yolda()
    {
        var sefer = new Expedition
        {
            StatusId = (int)HazirDurum,
            ReleaseDate = Bugun,
            ReturnDate = Bugun.AddDays(14),
        };

        YoldaMi(sefer).Should().BeTrue();
    }

    [Fact]
    public void DonusuGecmiste_YoldaDegil()
    {
        var sefer = new Expedition
        {
            StatusId = (int)HazirDurum,
            ReleaseDate = Bugun.AddDays(-8),
            ReturnDate = Bugun.AddDays(-2),
        };

        YoldaMi(sefer).Should().BeFalse();
    }

    /// <summary>
    /// TERK EDİLMİŞ KAYIT YOLDA SAYILMAZ. Sınırsız bırakılınca OLS'te 40 sefer
    /// çıkıyor ve 35'i yıllar önce çıkmış, dönüşü hiç girilmemiş kayıtlar.
    /// Sınır keyfi değil: tamamlanmış 3.559 seferin süresi ortanca 4 gün,
    /// %95'i 37 gün, %99'u 94 gün.
    /// </summary>
    [Fact]
    public void CokEskiCikis_YoldaSayilmaz()
    {
        var sefer = new Expedition
        {
            StatusId = (int)HazirDurum,
            ReleaseDate = Bugun.AddDays(-(ExpeditionOnRoad.RecentDepartureDays + 1)),
            ReturnDate = null,
        };

        YoldaMi(sefer).Should().BeFalse();
    }

    /// <summary>Sınırın tam üstündeki sefer hâlâ yolda — sınır dâhil.</summary>
    [Fact]
    public void SinirinTamUstu_Yolda()
    {
        var sefer = new Expedition
        {
            StatusId = (int)HazirDurum,
            ReleaseDate = Bugun.AddDays(-ExpeditionOnRoad.RecentDepartureDays),
            ReturnDate = null,
        };

        YoldaMi(sefer).Should().BeTrue();
    }

    /// <summary>
    /// BOŞALTILMIŞ SEFER, dönüşü girilmemiş olsa bile yolda değil — tarih
    /// kuralı bitmiş durumları dışarıda bırakmalı.
    /// </summary>
    [Fact]
    public void BosaltilmisAmaDonusuGirilmemis_YoldaDegil()
    {
        var sefer = new Expedition
        {
            StatusId = (int)BosaltildiDurum,
            ReleaseDate = Bugun.AddDays(-3),
            ReturnDate = null,
        };

        YoldaMi(sefer).Should().BeFalse();
    }

    /// <summary>Henüz çıkmamış (çıkış tarihi gelecekte) sefer yolda değil.</summary>
    [Fact]
    public void CikisiGelecekte_YoldaDegil()
    {
        var sefer = new Expedition
        {
            StatusId = (int)HazirDurum,
            ReleaseDate = Bugun.AddDays(3),
            ReturnDate = null,
        };

        YoldaMi(sefer).Should().BeFalse();
    }

    /// <summary>Hiç tarihi olmayan ve durumu ilerlememiş sefer yolda değil.</summary>
    [Fact]
    public void TarihsizVeDurumsuz_YoldaDegil() =>
        YoldaMi(new Expedition { StatusId = (int)HazirDurum }).Should().BeFalse();
}
