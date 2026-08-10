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
    /// </summary>
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

        for (var page = 1; page <= lastPage; page++)
        {
            links.Add(new PaginatorLink
            {
                Url = pageUrl(page),
                Label = page.ToString(),
                Active = page == currentPage,
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
