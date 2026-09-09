using FluentAssertions;
using OLS.Business.Services.Roles;
using OLS.DataAccess.Common;

namespace OLS.Business.Tests;

/// <summary>
/// TÜRKÇE KATLAMA — kapsam ve ona bağlı anahtarlar.
///
/// Kural bir kez daralıp genişledi ve HER İKİ yönde de canlıda hata üretti:
/// dar kuralda ASCII anahtarlar ("yonetim") eşleşmedi ve Yönetim çalışanları
/// yanlış role düştü; sonra kural genişleyince aynı sözlük bu kez Türkçe
/// harfli anahtarlarla ("yönetim") eşleşmez oldu. Anahtarlar bu metodun
/// ÇIKTISI olduğu için ikisi birlikte test ediliyor.
///
/// Kapsamın genişlemesinin ölçülmüş karşılığı (canlı Siber):
/// görevli adı eşleşmesi skn_yuk'ta 7.850→7.985 ve 7.712→7.792,
/// skn_rezervasyon'da 17.091→17.457 — toplam 581 kayıt.
/// </summary>
public sealed class TurkishFoldTests
{
    [Theory]
    // Dar kuralın da yaptığı iş — bozulmadığı doğrulanıyor.
    [InlineData("İSTANBUL", "istanbul")]
    [InlineData("TAŞIMA", "tasima")]
    [InlineData("VıACHESLAVK", "viacheslavk")]
    // Kapsam genişlemesi: aksansız yazılmış Siber adı ile aksanlı yerel ad
    // artık aynı anahtara düşüyor.
    [InlineData("RÜSTEM FEYAZ", "rustem feyaz")]
    [InlineData("RUSTEM FEYAZ", "rustem feyaz")]
    [InlineData("HASAN ÇALIŞKAN", "hasan caliskan")]
    [InlineData("HASAN CALISKAN", "hasan caliskan")]
    [InlineData("GÜL TÜREDİ", "gul turedi")]
    [InlineData("GUL TUREDİ", "gul turedi")]
    [InlineData("BURAK KOCASAHİNOĞLU", "burak kocasahinoglu")]
    [InlineData("Öz Mal", "oz mal")]
    public void Normalize_KatlarVeKucultur(string girdi, string beklenen) =>
        TurkishFold.Normalize(girdi).Should().Be(beklenen);

    /// <summary>
    /// Katlama listesi konum konum eşleşmeli — PostgreSQL <c>translate()</c>
    /// argümanları böyle çalışıyor, uzunluklar tutmazsa sessizce yanlış harf
    /// üretir.
    /// </summary>
    [Fact]
    public void TranslateArgumanlari_AyniUzunlukta()
    {
        TurkishFold.From.Should().HaveLength(TurkishFold.To.Length);
        TurkishFold.From.Should().Contain("Ş").And.Contain("ğ");
    }

    /// <summary>
    /// Departman → rol anahtarları katlama ÇIKTISI olmak zorunda; biri Türkçe
    /// harf içeriyorsa o departman hiçbir zaman eşleşmez ve çalışanları sessizce
    /// Standart Kullanıcı rolüne düşer.
    /// </summary>
    [Theory]
    [InlineData("Yönetim")]
    [InlineData("YÖNETİM")]
    [InlineData("Satış & Pazarlama")]
    [InlineData("SATIŞ & PAZARLAMA")]
    [InlineData("İdari İşler")]
    [InlineData("İhracat Operasyon")]
    [InlineData("İTHALAT OPERASYON")]
    [InlineData("Transit Operasyon")]
    [InlineData("Muhasebe & Finans")]
    public void DepartmanAdi_RoleEslesir(string departman) =>
        RoleCatalog.DepartmentToRoleSlug.Should().ContainKey(TurkishFold.Normalize(departman));

    [Fact]
    public void RolAnahtarlari_KatlamaCiktisidir() =>
        RoleCatalog.DepartmentToRoleSlug.Keys.Should()
            .OnlyContain(k => k == TurkishFold.Normalize(k),
                "anahtarlar TurkishFold.Normalize çıktısı olmalı");

    /// <summary>SQL'e çevrilmek üzere var; .NET'ten çağrılırsa sessizce yanlış sonuç vermemeli.</summary>
    [Fact]
    public void Fold_NetTarafindaCagrilirsaPatlar() =>
        FluentActions.Invoking(() => TurkishFold.Fold("x")).Should().Throw<NotSupportedException>();
}
