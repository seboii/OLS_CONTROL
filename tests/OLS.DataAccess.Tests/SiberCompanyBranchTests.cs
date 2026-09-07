using FluentAssertions;
using OLS.DataAccess.Siber;

namespace OLS.DataAccess.Tests;

/// <summary>
/// ŞUBE ŞİRKETİ TAKİP EDER.
///
/// <c>SiberLoadRepository.SubeIdFor</c> tek kaynak: yük INSERT'i, sefer/pozisyon
/// INSERT'i ve <c>sfy_modulkalem</c> yazımı hep buradan geçiyor. Eskiden şube
/// SABİT yazılıyordu ve Avrora yükü OLS şubesine düşüyordu; Siber'in kendi
/// verisinde şube şirketle birebir örtüşüyor (4.120 OLS / 279 AVRORA).
///
/// Bu proje çözümde kayıtlıydı ama İÇİNDE HİÇ TEST YOKTU — her test turunda
/// "No test is available in OLS.DataAccess.Tests.dll" uyarısı üretiyordu.
/// Veritabanı gerektirmeyen bu eşleme, katmanın doğal birim testi.
/// </summary>
public sealed class SiberCompanyBranchTests
{
    [Fact]
    public void AvroraSirketi_AvroraSubesineDuser()
    {
        SiberLoadRepository.SubeIdFor(SiberLoadRepository.AvroraSirketId)
            .Should().Be(SiberLoadRepository.AvroraSubeId);
    }

    [Fact]
    public void OlsSirketi_OlsSubesineDuser()
    {
        SiberLoadRepository.SubeIdFor(SiberLoadRepository.DefaultSirketId)
            .Should().Be(SiberLoadRepository.DefaultSubeId);
    }

    /// <summary>
    /// Şirketi bilinmeyen ya da hiç gelmeyen kayıt OLS şubesine düşer —
    /// <c>skn_pozisyon.subeid</c> NOT NULL, boş bırakılamaz.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("tanimsiz-sirket")]
    public void BilinmeyenSirket_OlsSubesineDuser(string? sirketId)
    {
        SiberLoadRepository.SubeIdFor(sirketId).Should().Be(SiberLoadRepository.DefaultSubeId);
    }

    /// <summary>
    /// HARF DUYARSIZ OLMALI. <c>siber_company_id</c> yerelde bir dönem küçük
    /// harfle birikmişti (finans tablolarının 343.059 satırının tamamı);
    /// eşleme harfe duyarlı olsaydı böyle bir değer sessizce OLS şubesine
    /// düşer, Avrora kaydı yanlış şubeye yazılırdı.
    /// </summary>
    [Fact]
    public void KucukHarfliAvroraGuidi_YineAvroraSubesineDuser()
    {
        SiberLoadRepository.SubeIdFor(SiberLoadRepository.AvroraSirketId.ToLowerInvariant())
            .Should().Be(SiberLoadRepository.AvroraSubeId);
    }

    /// <summary>
    /// Dört kimlik de <c>sbr_sirket</c> / <c>sbr_sube</c> değerleri; sabit
    /// olarak duruyorlar çünkü Siber'e bağlanılamadığında da yazma yolunun
    /// çalışması gerekiyor. Değerleri kilitlemek, birinin yanlışlıkla
    /// düzenlenmesini yakalar.
    /// </summary>
    [Fact]
    public void SirketVeSubeKimlikleri_Degismez()
    {
        SiberLoadRepository.DefaultSirketId.Should().Be("BA4888B1-A2B0-4142-B273-92481D932EAD");
        SiberLoadRepository.AvroraSirketId.Should().Be("46258A01-8D77-4F87-AAF5-6B331DEDD8A7");
        SiberLoadRepository.DefaultSubeId.Should().Be("69588E44-731B-46E5-83A4-A338816E2300");
        SiberLoadRepository.AvroraSubeId.Should().Be("D019AE6E-3E81-47FF-8194-03C259C67013");
    }
}
