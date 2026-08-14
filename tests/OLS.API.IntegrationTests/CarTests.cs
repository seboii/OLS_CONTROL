using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace OLS.API.IntegrationTests;

/// <summary>
/// Regresyon: Araç formunun "Kiralanan Firma" alanı olsold'da hiç yoktu
/// (CarItem arayüzü <c>customer</c> alanını taşıyordu ama hiçbir yerde
/// okunmuyor/gönderilmiyordu). Ayrıca <c>cars.customer_id</c> yerel Account
/// id'si DEĞİL, cari'nin Siber id'sini tutuyor (CarService.SingleAsync'in
/// <c>Accounts.Where(a =&gt; a.SiberId == c.CustomerId)</c> eşleşmesinden
/// görülüyor) — bu test hem alanın var olduğunu hem SiberId eşleşmesinin
/// doğru çalıştığını kilitliyor.
/// </summary>
[Collection("OlsApi")]
public sealed class CarTests
{
    private readonly OlsApiFactory _factory;

    public CarTests(OlsApiFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateCar_WithCustomerSiberId_ResolvesBackToTheSameAccountOnRead()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountName = $"Araç Test Cari {Guid.NewGuid():N}";
        using var accountForm = new MultipartFormDataContent { { new StringContent(accountName), "name" } };
        var accountResponse = await admin.PostAsync("/api/v1/account", accountForm);
        accountResponse.EnsureSuccessStatusCode();
        var account = (await accountResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        var accountId = account.GetProperty("id").GetInt64();
        var siberId = account.GetProperty("siber_id").GetString();

        siberId.Should().NotBeNullOrWhiteSpace("her cari, manuel oluşturulsa bile bir Siber id alır");

        var createResponse = await admin.PostAsJsonAsync("/api/v1/car", new
        {
            plate_number = $"TEST-{Guid.NewGuid():N}"[..12],
            customer_id = siberId,
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var carId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();

        var detailResponse = await admin.GetAsync($"/api/v1/car/{carId}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = (await detailResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        var customer = detail.GetProperty("customer");
        customer.GetProperty("id").GetInt64().Should().Be(accountId);
        customer.GetProperty("siber_id").GetString().Should().Be(siberId);

        // Cariyi arama uçundan (picker'ın kullandığı) da siber_id ile bulunabilmeli.
        var searchResponse = await admin.GetAsync($"/api/v1/account?search={Uri.EscapeDataString(accountName)}&per_page=10");
        var searchResults = (await searchResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("data");
        searchResults.EnumerateArray().Should().Contain(a =>
            a.GetProperty("id").GetInt64() == accountId &&
            a.GetProperty("siber_id").GetString() == siberId);
    }
}
