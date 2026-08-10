using FluentAssertions;
using OLS.Business.Common;

namespace OLS.Business.Tests;

/// <summary>
/// LengthAwarePaginator, Laravel'in sayfalama JSON zarfının birebir karşılığı ve
/// frontend'in DataTable/Pagination bileşenleri buna doğrudan bağımlı (current_page,
/// data, total, per_page, from, to alan adları sabit sözleşme). Alan adı ya da
/// from/to/last_page hesaplama mantığı bozulursa sayfalama sessizce yanlış çalışır.
/// </summary>
public sealed class LengthAwarePaginatorTests
{
    [Fact]
    public void Create_WithItems_ComputesFromToAndLastPageCorrectly()
    {
        var items = new[] { "a", "b", "c" };

        var page = LengthAwarePaginator<string>.Create(items, total: 25, perPage: 3, currentPage: 2, path: "/api/v1/account");

        page.CurrentPage.Should().Be(2);
        page.PerPage.Should().Be(3);
        page.Total.Should().Be(25);
        page.LastPage.Should().Be(9); // ceil(25/3)
        page.From.Should().Be(4);     // (2-1)*3 + 1
        page.To.Should().Be(6);       // (2-1)*3 + 3 öğe
        page.Data.Should().Equal(items);
    }

    [Fact]
    public void Create_WithNoItems_FromAndToAreNull()
    {
        // Laravel boş sayfada from/to'yu null döner, 0 değil.
        var page = LengthAwarePaginator<string>.Create([], total: 0, perPage: 10, currentPage: 1, path: "/api/v1/account");

        page.From.Should().BeNull();
        page.To.Should().BeNull();
        page.LastPage.Should().Be(1);
        page.Total.Should().Be(0);
    }

    [Fact]
    public void Create_PerPageOrCurrentPageBelowOne_ClampsToOne()
    {
        var page = LengthAwarePaginator<string>.Create(["x"], total: 1, perPage: 0, currentPage: 0, path: "/p");

        page.PerPage.Should().Be(1);
        page.CurrentPage.Should().Be(1);
    }

    [Fact]
    public void Create_LastPage_HasNoNextPageUrl_FirstPage_HasNoPrevPageUrl()
    {
        var page = LengthAwarePaginator<string>.Create(["x"], total: 3, perPage: 1, currentPage: 3, path: "/p");

        page.NextPageUrl.Should().BeNull();
        page.PrevPageUrl.Should().Be("/p?page=2");
    }

    [Fact]
    public void Create_JsonPropertyNames_MatchLaravelPaginatorContract()
    {
        // Frontend'in api.ts tipindeki Paginated<T> alan adlarıyla birebir eşleşmeli.
        var page = LengthAwarePaginator<string>.Create(["x"], total: 1, perPage: 10, currentPage: 1, path: "/p");
        var json = System.Text.Json.JsonSerializer.Serialize(page);

        json.Should().Contain("\"current_page\"");
        json.Should().Contain("\"per_page\"");
        json.Should().Contain("\"last_page\"");
        json.Should().Contain("\"total\"");
        json.Should().Contain("\"data\"");
    }
}
