using FluentAssertions;
using OLS.DataAccess.Siber;

namespace OLS.DataAccess.Tests;

/// <summary>
/// SEFER SİLİNEMİYORDU — SİLME SIRASI VE KAPSAMI.
///
/// Silme yalnızca yük EŞLEMESİNİ (<c>skn_yukaktarma</c>) kaldırıyordu; oysa
/// yükün KENDİSİ de sefere doğrudan bağlı (<c>skn_yuk.pozisyonid</c>,
/// <c>FK_skn_yuk_skn_pozisyon</c>). Silme bu yüzden SQL hata 547 ile düşüyor,
/// kullanıcı genel hata mesajı görüyordu.
///
/// Canlıda pozisyona işaret eden satırı OLAN dört tablo var: skn_yukaktarma
/// (10.571), skn_yuk (7.779), skn_pozisyonbilgi (4.435) ve skn_pozisyonkmbilgi
/// (1.826). Diğer 42 yabancı anahtar boş.
///
/// Doğru sıra üretim veritabanında geri alınan bir denemeyle doğrulandı:
/// yük koparıldı (1), eşleme (1), bilgi (1), km (0) ve pozisyon (1) silindi.
/// </summary>
public sealed class ExpeditionDeleteSqlTests
{
    private static readonly string Sql = SiberExpeditionRepository.DeletePozisyonSql;

    /// <summary>
    /// YÜK SİLİNMEZ, KOPARILIR. Seferle birlikte yükün de gitmesi veri kaybı
    /// olurdu; Siber'in kendi ekranı da yükü serbest bırakıyor.
    /// </summary>
    [Fact]
    public void Yuk_SilinmezKoparilir()
    {
        Sql.Should().Contain("UPDATE skn_yuk");
        Sql.Should().Contain("pozisyonid = NULL");
        Sql.Should().NotContain("DELETE FROM skn_yuk WHERE");
    }

    /// <summary>
    /// Sefer numarasının metin kopyaları da temizlenir — biri kalırsa yük
    /// ekranda artık var olmayan bir sefer numarasını göstermeye devam eder.
    /// </summary>
    [Fact]
    public void SeferNumarasininMetinKopyalariTemizlenir()
    {
        Sql.Should().Contain("pozisyonidstr = NULL");
        Sql.Should().Contain("seferno = NULL");
    }

    /// <summary>Yabancı anahtarı dolu olan dört tablonun tamamı temizlenmeli.</summary>
    [Theory]
    [InlineData("skn_yukaktarma")]
    [InlineData("skn_pozisyonbilgi")]
    [InlineData("skn_pozisyonkmbilgi")]
    public void BagliAltKayitlar_Silinir(string tablo) =>
        Sql.Should().Contain($"DELETE FROM {tablo}");

    /// <summary>
    /// SIRA: yük koparma en başta, pozisyonun kendisi en sonda. Tersi hâlde
    /// yabancı anahtar yine düşer; ayrıca <c>skn_yuk_seferbagla</c>
    /// tetikleyicisi seferin navlun özetini pozisyon dururken hesaplamalı.
    /// </summary>
    [Fact]
    public void Sira_YukKoparmaOnce_PozisyonEnSon()
    {
        var yukKoparma = Sql.IndexOf("UPDATE skn_yuk", StringComparison.Ordinal);
        var aktarma = Sql.IndexOf("DELETE FROM skn_yukaktarma", StringComparison.Ordinal);
        var pozisyon = Sql.IndexOf("DELETE FROM skn_pozisyon ", StringComparison.Ordinal);

        yukKoparma.Should().BeGreaterThanOrEqualTo(0);
        aktarma.Should().BeGreaterThan(yukKoparma);
        pozisyon.Should().BeGreaterThan(aktarma);

        // Pozisyon gerçekten EN SON: kendisinden sonra başka bir adım kalmamalı.
        Sql[(pozisyon + 1)..].Should().NotContain("DELETE FROM");
        Sql[(pozisyon + 1)..].Should().NotContain("UPDATE ");
    }
}
