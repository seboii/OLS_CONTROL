using System.Text.Json.Serialization;

namespace OLS.Business.Common;

/// <summary>
/// Laravel'in <c>LengthAwarePaginator</c> JSON çıktısının birebir karşılığı.
///
/// Frontend buna doğrudan bağımlı: <c>resources/js/composables/index.js</c> içinde
/// <c>state.items = response.data.data.data</c> ve <c>state.meta = response.data.data</c>
/// yapılıyor; <c>DatatableAjax.vue</c> ise <c>meta.total</c>, <c>meta.per_page</c> ve
/// <c>meta.current_page</c> alanlarını okuyor. Alan adları bu yüzden değiştirilemez.
/// </summary>
public sealed class LengthAwarePaginator<T>
{
    [JsonPropertyName("current_page")]
    public int CurrentPage { get; init; }

    /// <summary>Sayfadaki kayıtlar. Laravel'de de iç anahtar adı "data".</summary>
    [JsonPropertyName("data")]
    public IReadOnlyList<T> Data { get; init; } = [];

    [JsonPropertyName("first_page_url")]
    public string? FirstPageUrl { get; init; }

    /// <summary>Sayfadaki ilk kaydın genel sırası. Sayfa boşsa null.</summary>
    [JsonPropertyName("from")]
    public int? From { get; init; }

    [JsonPropertyName("last_page")]
    public int LastPage { get; init; }

    [JsonPropertyName("last_page_url")]
    public string? LastPageUrl { get; init; }

    [JsonPropertyName("links")]
    public IReadOnlyList<PaginatorLink> Links { get; init; } = [];

    [JsonPropertyName("next_page_url")]
    public string? NextPageUrl { get; init; }

    [JsonPropertyName("path")]
    public string? Path { get; init; }

    [JsonPropertyName("per_page")]
    public int PerPage { get; init; }

    [JsonPropertyName("prev_page_url")]
    public string? PrevPageUrl { get; init; }

    /// <summary>Sayfadaki son kaydın genel sırası. Sayfa boşsa null.</summary>
    [JsonPropertyName("to")]
    public int? To { get; init; }

    [JsonPropertyName("total")]
    public int Total { get; init; }

    public static LengthAwarePaginator<T> Create(
        IReadOnlyList<T> items,
        int total,
        int perPage,
        int currentPage,
        string path)
    {
        if (perPage < 1) perPage = 1;
        if (currentPage < 1) currentPage = 1;

        var lastPage = total == 0 ? 1 : (int)Math.Ceiling(total / (double)perPage);

        string PageUrl(int page) => $"{path}?page={page}";

        // Laravel sayfa boşken from/to alanlarını null döndürür.
        int? from = items.Count == 0 ? null : ((currentPage - 1) * perPage) + 1;
        int? to = items.Count == 0 ? null : ((currentPage - 1) * perPage) + items.Count;

        return new LengthAwarePaginator<T>
        {
            CurrentPage = currentPage,
            Data = items,
            FirstPageUrl = PageUrl(1),
            From = from,
            LastPage = lastPage,
            LastPageUrl = PageUrl(lastPage),
            Links = BuildLinks(currentPage, lastPage, PageUrl),
            NextPageUrl = currentPage < lastPage ? PageUrl(currentPage + 1) : null,
            Path = path,
            PerPage = perPage,
            PrevPageUrl = currentPage > 1 ? PageUrl(currentPage - 1) : null,
            To = to,
            Total = total,
        };
    }

    /// <summary>
    /// Laravel'in "&laquo; Previous" / sayfa numaraları / "Next &raquo;" bağlantı dizisi.
    /// Frontend şu an kullanmıyor ama zarfın birebir aynı kalması için üretiliyor.
    ///
    /// PENCERELENİR — eskiden HER sayfa için bir nesne üretiliyordu ve üst sınır
    /// yoktu: 19.528 teklif / 10 = yanıt başına 1.955 bağlantı nesnesi, yaklaşık
    /// 150 KB. Yani zarfın kullanılmayan bir alanı, asıl veriden (10 satır) kat
    /// kat büyüktü.
    ///
    /// Laravel'in kendisi de bu listeyi pencereler (<c>UrlWindow</c>,
    /// <c>onEachSide = 3</c>) ve araya "..." ayıracı koyar; yani "birebir aynı
    /// zarf" hedefine sınırsız liste zaten UYMUYORDU. Buradaki kural aynı:
    /// az sayıda sayfa varsa hepsi, çoksa ilk/son sayfa + geçerli sayfanın
    /// çevresi.
    /// </summary>
    private const int OnEachSide = 3;

    /// <summary>Bunun altında pencereleme yapılmaz, tüm sayfalar listelenir (Laravel ile aynı eşik).</summary>
    private const int SmallSliderLimit = (OnEachSide * 2) + 8;

    private static List<PaginatorLink> BuildLinks(int currentPage, int lastPage, Func<int, string> pageUrl)
    {
        var links = new List<PaginatorLink>
        {
            new()
            {
                Url = currentPage > 1 ? pageUrl(currentPage - 1) : null,
                Label = "&laquo; Previous",
                Active = false,
            },
        };

        foreach (var page in PageWindow(currentPage, lastPage))
        {
            links.Add(page is null
                // Laravel'in ayıracı: url'siz, "..." etiketli satır.
                ? new PaginatorLink { Url = null, Label = "...", Active = false }
                : new PaginatorLink
                {
                    Url = pageUrl(page.Value),
                    Label = page.Value.ToString(),
                    Active = page.Value == currentPage,
                });
        }

        links.Add(new PaginatorLink
        {
            Url = currentPage < lastPage ? pageUrl(currentPage + 1) : null,
            Label = "Next &raquo;",
            Active = false,
        });

        return links;
    }

    /// <summary>Gösterilecek sayfa numaraları; <c>null</c> ögesi "..." ayıracıdır.</summary>
    private static IEnumerable<int?> PageWindow(int currentPage, int lastPage)
    {
        if (lastPage < SmallSliderLimit)
        {
            for (var page = 1; page <= lastPage; page++)
                yield return page;

            yield break;
        }

        var windowStart = Math.Max(1, currentPage - OnEachSide);
        var windowEnd = Math.Min(lastPage, currentPage + OnEachSide);

        // Baş: ilk sayfalar. Pencere zaten başa değiyorsa ayıraç konmaz.
        var headEnd = Math.Min(OnEachSide, windowStart - 1);
        for (var page = 1; page <= headEnd; page++)
            yield return page;

        if (windowStart > headEnd + 1)
            yield return null;

        for (var page = windowStart; page <= windowEnd; page++)
            yield return page;

        var tailStart = Math.Max(windowEnd + 1, lastPage - OnEachSide + 1);

        if (tailStart > windowEnd + 1)
            yield return null;

        for (var page = tailStart; page <= lastPage; page++)
            yield return page;
    }
}

public sealed class PaginatorLink
{
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    [JsonPropertyName("active")]
    public bool Active { get; init; }
}
