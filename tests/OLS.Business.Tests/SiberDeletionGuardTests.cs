using FluentAssertions;
using OLS.Business.Services.TransferData;

namespace OLS.Business.Tests;

/// <summary>
/// Siber'den silinen kayıtları işaretleyen kontrolün GÜVENLİK EŞİĞİ.
///
/// Bu eşik bozulursa hata sessiz ve yıkıcı olur: yarım dönen tek bir Siber
/// çekimi, yereldeki tüm yük/teklif/sefer kayıtlarını "silinmiş" diye
/// damgalar ve kullanıcı listelerini boş bulur. Testler bu yüzden eşiğin iki
/// yönünü de kilitler — eksik çekimde ATLAMALI, tam çekimde ÇALIŞMALI.
/// </summary>
public sealed class SiberDeletionGuardTests
{
    [Theory]
    // Siber'den hiç kayıt gelmemesi en tehlikeli durum: bağlantı koptuğunda
    // ya da sorgu boş döndüğünde tüm tablo silinmiş sayılırdı.
    [InlineData(0, 8000)]
    [InlineData(1, 8000)]
    [InlineData(3999, 8000)]
    public void EksikCekimde_KontrolAtlanir(int fetched, int local)
    {
        SiberSyncService.ShouldSkipDeletionCheck(fetched, local).Should().BeTrue();
    }

    [Theory]
    // Eşik tam yarıda açılır; gerçek senaryoda Siber'den yerelden AZ kayıt
    // gelmesi normaldir (silinenler kadar fark) ve kontrol çalışmalıdır.
    [InlineData(4000, 8000)]
    [InlineData(7974, 8001)]
    [InlineData(8001, 8001)]
    public void YeterliCekimde_KontrolCalisir(int fetched, int local)
    {
        SiberSyncService.ShouldSkipDeletionCheck(fetched, local).Should().BeFalse();
    }

    [Fact]
    public void YereldeKayitYoksa_KontrolEnglenmez()
    {
        // İlk kurulumda yerel tablo boş; işaretlenecek kayıt olmadığı için
        // eşiğin devreye girmesine gerek yok.
        SiberSyncService.ShouldSkipDeletionCheck(0, 0).Should().BeFalse();
    }

    [Fact]
    public void SiberdenFazlaKayitGelirse_KontrolCalisir()
    {
        // Yerelde henüz olmayan yeni kayıtlar geldiğinde de kontrol çalışmalı;
        // bu tur hiçbir şeyi silinmiş saymaz, yalnızca yeni kayıtları ekler.
        SiberSyncService.ShouldSkipDeletionCheck(9000, 8000).Should().BeFalse();
    }
}
