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
///
/// Ayrıca olsold <c>CarSave</c>/<c>CarUpdate</c>: <c>plate_number</c> hem
/// zorunlu hem benzersiz olmalı; <c>car_type/romork_type/vehicle_owner/
/// vehicle_status/customer_id/km/width/length/height/capacity</c> de
/// zorunlu — hedefte bu alanların hiçbirinin doğrulanmadığı bulundu ve
/// burada kilitleniyor.
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

        var createResponse = await admin.PostAsJsonAsync("/api/v1/car", await ValidCarPayloadAsync(admin,
            plateNumber: $"TEST-{Guid.NewGuid():N}"[..12], customerId: siberId));
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

    [Fact]
    public async Task CreateCar_WithMissingRequiredFields_Returns400WithAllFieldErrors()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.PostAsJsonAsync("/api/v1/car", new { });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errors = body.GetProperty("errors");
        foreach (var field in new[]
        {
            "plate_number", "car_type", "romork_type", "vehicle_owner", "vehicle_status",
            "customer_id", "km", "width", "length", "height", "capacity",
        })
            errors.TryGetProperty(field, out _).Should().BeTrue($"'{field}' olsold'da zorunlu");
    }

    [Fact]
    public async Task CreateCar_WithDuplicatePlateNumber_Returns422AndDoesNotCreateSecondRow()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var plate = $"DUP-{Guid.NewGuid():N}"[..12];

        var first = await admin.PostAsJsonAsync("/api/v1/car", await ValidCarPayloadAsync(admin, plate));
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await admin.PostAsJsonAsync("/api/v1/car", await ValidCarPayloadAsync(admin, plate));
        second.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await second.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").GetProperty("plate_number")[0].GetString()
            .Should().Be("Bu plaka numarası zaten kayıtlı");

        var listResponse = await admin.GetAsync($"/api/v1/car?search={Uri.EscapeDataString(plate)}&per_page=50");
        var listResults = (await listResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("data");
        listResults.GetArrayLength().Should().Be(1, "ikinci kayıt reddedildiği için tabloya girmemeli");
    }

    [Fact]
    public async Task UpdateCar_KeepingOwnPlateNumber_Succeeds()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var plate = $"SELF-{Guid.NewGuid():N}"[..12];

        var payload = await ValidCarPayloadAsync(admin, plate);
        var createResponse = await admin.PostAsJsonAsync("/api/v1/car", payload);
        var carId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();

        var updateResponse = await admin.PutAsJsonAsync("/api/v1/car",
            MergeId(payload, carId));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "kendi plakasıyla güncelleme kendine-çakışma sayılmamalı");
    }

    [Fact]
    public async Task UpdateCar_ToAnotherCarsPlateNumber_Returns422()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var plateA = $"A-{Guid.NewGuid():N}"[..12];
        var plateB = $"B-{Guid.NewGuid():N}"[..12];

        await admin.PostAsJsonAsync("/api/v1/car", await ValidCarPayloadAsync(admin, plateA));
        var createB = await admin.PostAsJsonAsync("/api/v1/car", await ValidCarPayloadAsync(admin, plateB));
        var carBId = (await createB.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();

        var updatePayload = await ValidCarPayloadAsync(admin, plateA);
        var updateResponse = await admin.PutAsJsonAsync("/api/v1/car", MergeId(updatePayload, carBId));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private static async Task<Dictionary<string, object?>> ValidCarPayloadAsync(
        HttpClient admin, string plateNumber, string? customerId = null)
    {
        var payload = await TestCarHelper.RequiredCarFieldsAsync(admin);
        payload["plate_number"] = plateNumber;
        if (customerId is not null)
            payload["customer_id"] = customerId;

        return payload;
    }

    private static Dictionary<string, object?> MergeId(Dictionary<string, object?> payload, long id)
    {
        var copy = new Dictionary<string, object?>(payload) { ["id"] = id };
        return copy;
    }
}
