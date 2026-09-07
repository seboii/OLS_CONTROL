using FluentAssertions;
using OLS.Business.Common;

namespace OLS.Business.Tests;

/// <summary>
/// SAYFA BAĞLANTILARI PENCERELENİR.
///
/// Eskiden <c>links</c> dizisi HER sayfa için bir nesne üretiyordu ve üst sınır
/// yoktu: 19.528 teklif / 10 = yanıt başına 1.955 nesne (~150 KB), yani zarfın
/// frontend'in hiç okumadığı bir alanı asıl veriden kat kat büyüktü.
///
/// Laravel'in kendisi de listeyi pencereler (<c>UrlWindow</c>,
/// <c>onEachSide = 3</c>) ve araya "..." ayıracı koyar — sınırsız liste zaten
/// "birebir aynı zarf" hedefine uymuyordu.
/// </summary>
public sealed class PaginatorLinkWindowTests
{
    private static LengthAwarePaginator<int> Page(int total, int perPage, int currentPage) =>
        LengthAwarePaginator<int>.Create([1], total, perPage, currentPage, "/api/v1/test");

    private static string[] Labels(LengthAwarePaginator<int> paginator) =>
        paginator.Links.Select(l => l.Label).ToArray();

    [Fact]
    public void AzSayfaVarsa_HepsiListelenir()
    {
        var labels = Labels(Page(total: 50, perPage: 10, currentPage: 1));

        // 5 sayfa + önceki/sonraki
        labels.Should().Equal(
            "&laquo; Previous", "1", "2", "3", "4", "5", "Next &raquo;");
    }

    [Fact]
    public void CokSayfaVarsa_BasSonVeCevreGosterilir()
    {
        // 19.528 kayıt / 10 = 1.953 sayfa — düzeltmeden önce 1.955 nesne üretiyordu.
        var paginator = Page(total: 19_528, perPage: 10, currentPage: 900);
        var labels = Labels(paginator);

        labels.Should().Equal(
            "&laquo; Previous",
            "1", "2", "3",
            "...",
            "897", "898", "899", "900", "901", "902", "903",
            "...",
            "1951", "1952", "1953",
            "Next &raquo;");

        paginator.Links.Single(l => l.Label == "900").Active.Should().BeTrue();
        paginator.Links.Where(l => l.Label == "...").Should().OnlyContain(l => l.Url == null);
    }

    [Fact]
    public void IlkSayfada_BastaAyiracOlmaz()
    {
        var labels = Labels(Page(total: 19_528, perPage: 10, currentPage: 1));

        labels.Should().StartWith(new[] { "&laquo; Previous", "1", "2", "3", "4", "..." });
        labels.Should().EndWith(new[] { "1951", "1952", "1953", "Next &raquo;" });
    }

    [Fact]
    public void SonSayfada_SondaAyiracOlmaz()
    {
        var labels = Labels(Page(total: 19_528, perPage: 10, currentPage: 1953));

        labels.Should().StartWith(new[] { "&laquo; Previous", "1", "2", "3", "..." });
        labels.Should().EndWith(new[] { "1950", "1951", "1952", "1953", "Next &raquo;" });
    }

    /// <summary>
    /// Asıl kazanç: bağlantı sayısı artık sayfa sayısıyla BÜYÜMÜYOR.
    /// </summary>
    [Theory]
    [InlineData(19_528)]
    [InlineData(215_989)]
    [InlineData(1_000_000)]
    public void BaglantiSayisi_SayfaSayisiyla_Buyumez(int total)
    {
        Page(total, perPage: 10, currentPage: 5).Links.Count.Should().BeLessThan(20);
    }
}
