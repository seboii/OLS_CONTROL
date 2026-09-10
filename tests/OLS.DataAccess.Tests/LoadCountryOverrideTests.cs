using FluentAssertions;
using OLS.DataAccess.Siber;

namespace OLS.DataAccess.Tests;

/// <summary>
/// SEÇİLEN ÜLKE YÜKTE GEÇERLİ OLMALI.
///
/// Siber'de <c>skn_yuk_yuklemebosaltma_update</c> tetikleyicisi var:
/// <c>gondericiid</c> ya da <c>aliciid</c> SET listesinde geçtiği anda
/// <c>_yuklemeulke</c>, <c>_bosaltmaulke</c> ve kıta sütunlarını
/// <c>skn_yuk_bilgi_yukbosulke_V2</c> ile yeniden hesaplıyor — kaynağı
/// gönderici/alıcı FİRMANIN kayıtlı adresi. Uygulamanın yazdığı ülke aynı
/// işlemde eziliyordu.
///
/// Canlı örnek: teklif TÜRKİYE → ÇEK CUMHURİYETİ seçilmiş, gönderici ve alıcı
/// aynı Danimarka firması, yük Siber'de DANİMARKA → DANİMARKA açılmış.
/// Ölçüm: teklifden gelen 3.782 yükün 3.113'ünde (%82) yükün ülkesi gönderici
/// firmanın ülkesi; teklifte seçilenle aynı olan yalnızca 1.806.
///
/// ÜRETİMDE GERİ ALINAN DENEYLE DOĞRULANDI:
///   • yalnızca ülke/kıta sütunlarına dokunan UPDATE  → değer KALIYOR
///   • aynı UPDATE gondericiid'yi de listelerse       → tetikleyici EZİYOR
///
/// Bu yüzden ülke, ana yazımdan SONRA ayrı bir ifadeyle geri yazılıyor ve o
/// ifade gönderici/alıcı sütunlarına ASLA dokunmamalı — testin asıl konusu bu.
/// </summary>
public sealed class LoadCountryOverrideTests
{
    private static readonly string Sql = SiberLoadRepository.RestoreCountrySql;

    /// <summary>
    /// TETİKLEYİCİYİ UYANDIRMAMALI. Bu iki sütundan biri SET listesine girerse
    /// ülke yeniden firmadan türetilir ve düzeltme hiçbir işe yaramaz.
    /// </summary>
    [Theory]
    [InlineData("gondericiid")]
    [InlineData("aliciid")]
    public void GondericiVeAlici_SetListesindeOlmamali(string sutun) =>
        Sql.Should().NotContain(sutun);

    [Theory]
    [InlineData("_yuklemeulke")]
    [InlineData("_bosaltmaulke")]
    [InlineData("_yuklemekita")]
    [InlineData("_bosaltmakita")]
    public void UlkeVeKitaSutunlari_GeriYazilir(string sutun) =>
        Sql.Should().Contain(sutun);

    /// <summary>
    /// ELİMİZDE DEĞER YOKSA SİBER'İNKİ KORUNUR: yükün gönderici/alıcısı yoksa
    /// tetikleyici zaten çalışmıyor ve tek kaynak bizim yazdığımız oluyor;
    /// düz atama o durumda dolu bir alanı boşaltırdı.
    /// </summary>
    [Fact]
    public void BosDegerMevcuduSilmez() =>
        Sql.Should().Contain("ISNULL(@YuklemeUlke,  _yuklemeulke)");

    /// <summary>Tek bir yüke uygulanmalı — anahtarsız UPDATE tüm tabloyu ezerdi.</summary>
    [Fact]
    public void TekYukeUygulanir() => Sql.Should().Contain("WHERE yukid = @YukId");
}
