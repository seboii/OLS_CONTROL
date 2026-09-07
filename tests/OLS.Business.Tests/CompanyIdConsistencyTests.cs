using FluentAssertions;
using OLS.Business.Services.Authorization;
using OLS.DataAccess.Siber;

namespace OLS.Business.Tests;

/// <summary>
/// AYNI İKİ ŞİRKET KİMLİĞİ İKİ KATMANDA AYRI AYRI TANIMLI.
///
/// <c>CompanyScope</c> (Business) görünürlük ve yazma kararını,
/// <c>SiberLoadRepository</c> (DataAccess) ise Siber'e yazılan
/// <c>sirketid</c>/<c>subeid</c> değerlerini bu sabitlerden alıyor. İkisi
/// birleştirilmedi çünkü DataAccess, Business'a bağımlı olamaz.
///
/// Bedeli: biri düzeltilip diğeri unutulursa uygulama kaydı bir şirkete
/// yazıp başka bir şirkete göre süzer — ve hata ekranda "kayıt kayboldu"
/// olarak görünür, hiçbir istisna atmaz. Bu test iki tanımın ayrışmasını
/// derleme değil TEST zamanında yakalar.
/// </summary>
public sealed class CompanyIdConsistencyTests
{
    [Fact]
    public void OlsKimligi_IkiKatmandaAyni()
    {
        CompanyScope.OlsCompanyId.Should().Be(SiberLoadRepository.DefaultSirketId);
    }

    [Fact]
    public void AvroraKimligi_IkiKatmandaAyni()
    {
        CompanyScope.AvroraCompanyId.Should().Be(SiberLoadRepository.AvroraSirketId);
    }

    /// <summary>
    /// Şirket seçicisinin listesi de aynı iki kimlikten oluşmalı: liste
    /// <c>sbr_sirket</c>'ten çekilmiyor (Siber'e bağlanılamadığında da
    /// seçicinin çalışması gerekiyor), yani ayrışma sessiz kalırdı.
    /// </summary>
    [Fact]
    public void SecilebilirSirketler_TamOlarakBuIki()
    {
        CompanyScope.Companies.Select(c => c.Id).Should().BeEquivalentTo(
            [CompanyScope.OlsCompanyId, CompanyScope.AvroraCompanyId]);
    }

    /// <summary>
    /// Kimlikler BÜYÜK harfle durmalı: görünürlük süzgeci sütunu
    /// <c>upper()</c> ile karşılaştırıyor ve karşılaştırmanın sabit tarafı
    /// zaten büyütülmüş olarak gidiyor. Küçük harfli bir sabit, canlıda
    /// düzeltilen harf duyarlılığı hatasını geri getirirdi.
    /// </summary>
    [Fact]
    public void Kimlikler_BuyukHarfli()
    {
        CompanyScope.OlsCompanyId.Should().Be(CompanyScope.OlsCompanyId.ToUpperInvariant());
        CompanyScope.AvroraCompanyId.Should().Be(CompanyScope.AvroraCompanyId.ToUpperInvariant());
    }
}
