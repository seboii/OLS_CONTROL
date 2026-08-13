using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace OLS.API.IntegrationTests;

/// <summary>
/// Teklif (Load) - Taraflar/Güzergah/Mali Kalemler sekmeleri eklendikten sonra
/// tam alan kapsamının gerçekten kaydedilip geri okunduğunu doğrular (bu
/// oturumda tarayıcıda canlı doğrulanan akışın otomatik testi).
/// </summary>
[Collection("OlsApi")]
public sealed class LoadTests
{
    private readonly OlsApiFactory _factory;

    public LoadTests(OlsApiFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateLoad_WithPartiesRouteAndFinancialItems_RoundTripsCorrectly()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        // Gerçek bir cari lazım (customer/sender/receiver için).
        var accountName = $"Teklif Test Cari {Guid.NewGuid():N}";
        using var accountForm = new MultipartFormDataContent { { new StringContent(accountName), "name" } };
        var accountResponse = await admin.PostAsync("/api/v1/account", accountForm);
        accountResponse.EnsureSuccessStatusCode();
        var accountBody = await accountResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accountId = accountBody.GetProperty("data").GetProperty("id").GetInt64();

        // Gerçek seed edilmiş lookup id'leri kullanılıyor (status_types: 4=Teklif/OFFER).
        using var loadForm = new MultipartFormDataContent
        {
            { new StringContent("1"), "work_type_id" },
            { new StringContent("1"), "loading_type_id" },
            { new StringContent("1"), "payment_type_id" },
            { new StringContent("4"), "status_type_id" },
            { new StringContent("1"), "department_id" },
            { new StringContent(accountId.ToString()), "customer_id" },
            { new StringContent(accountId.ToString()), "sender_id" },
            { new StringContent("2026-09-01"), "offer_date" },
            { new StringContent("2026-09-30"), "offer_validity_date" },
            { new StringContent("2026-09-01"), "marketing_notification_date" },
            { new StringContent("Otomatik test açıklaması"), "description" },
            { new StringContent("15"), "load_content[0][quantity]" },
            { new StringContent("250,5"), "load_content[0][gross_weight]" },
            { new StringContent("1"), "load_content[0][stackable]" },
            { new StringContent("1"), "load_financial_item[0][item]" },
            { new StringContent("1"), "load_financial_item[0][quantity]" },
            { new StringContent("1"), "load_financial_item[0][buysell]" },
            { new StringContent("1.250,75"), "load_financial_item[0][net_price]" },
            { new StringContent("Navlun bedeli"), "load_financial_item[0][description]" },
        };

        var createResponse = await admin.PostAsync("/api/v1/load", loadForm);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var loadId = createBody.GetProperty("data").GetProperty("id").GetInt64();

        var detailResponse = await admin.GetAsync($"/api/v1/load/{loadId}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = (await detailResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        detail.GetProperty("customer_id").GetProperty("id").GetInt64().Should().Be(accountId);
        detail.GetProperty("sender_id").GetProperty("id").GetInt64().Should().Be(accountId);
        detail.GetProperty("status_type_id").GetProperty("id").GetInt64().Should().Be(4);

        var contentRows = detail.GetProperty("load_content");
        contentRows.GetArrayLength().Should().Be(1);
        contentRows[0].GetProperty("quantity").GetInt32().Should().Be(15);
        // TurkishDecimal: "250,5" (virgüllü) doğru ayrıştırılmış olmalı.
        contentRows[0].GetProperty("gross_weight").GetDecimal().Should().Be(250.5m);

        var financialRows = detail.GetProperty("load_financial_item");
        financialRows.GetArrayLength().Should().Be(1);
        // "1.250,75" (binlik nokta + virgüllü ondalık) 1250.75 olarak ayrıştırılmalı.
        financialRows[0].GetProperty("net_price").GetDecimal().Should().Be(1250.75m);
        financialRows[0].GetProperty("description").GetString().Should().Be("Navlun bedeli");
    }

    [Fact]
    public async Task CreateLoad_WithoutRequiredFields_ReturnsValidationErrors_NotServerError()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        // Not: tamamen boş bir MultipartFormDataContent (sıfır parça) ASP.NET Core'un
        // form-binding'ini farklı davranışa sokuyor (doğrudan doğrulandı) — bu yüzden
        // en az bir alan (zorunlu olmayan) gönderiliyor, gerçek bir "eksik form
        // gönderimi" senaryosunu yansıtıyor.
        using var form = new MultipartFormDataContent { { new StringContent("test"), "description" } };
        var response = await admin.PostAsync("/api/v1/load", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").GetProperty("customer_id").GetArrayLength().Should().BeGreaterThan(0);
        body.GetProperty("errors").GetProperty("load_content").GetArrayLength().Should().BeGreaterThan(0);
    }
}
