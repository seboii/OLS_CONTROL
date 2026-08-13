using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.API.IntegrationTests;

/// <summary>
/// Sefer - Bağlı Yükler (expedition_load_mapping) — bu oturumda eklenen
/// bağlama UI'sinin backend sözleşmesini ve BR-006/007 romork tipi eşleşme
/// kuralını kilitler.
///
/// Not: Bu akış (Teklif→Yük dönüşümünün tersine) Siber yapılandırmasına
/// BAĞIMLI DEĞİL — ExpeditionLoadMappingService.SaveAsync yalnızca
/// _siber.IsConfigured true ise Siber'e yazmayı DENER, ama PostgreSQL
/// tarafı her koşulda çalışır (bkz. servis içi yorum). Test ortamında
/// Siber bilinçli olarak yapılandırılmadığından burada GUID tabanlı
/// yerel kimlik üretimi devreye girer.
/// </summary>
[Collection("OlsApi")]
public sealed class ExpeditionLoadMappingTests
{
    private readonly OlsApiFactory _factory;

    public ExpeditionLoadMappingTests(OlsApiFactory factory) => _factory = factory;

    private async Task<(long ExpeditionId, long CarId)> SeedExpeditionWithCarAsync(
        HttpClient admin, int romorkType)
    {
        var plate = $"34 TST {Guid.NewGuid():N}".Substring(0, 12);
        var carResponse = await admin.PostAsJsonAsync("/api/v1/car", new { plate_number = plate, romork_type = romorkType });
        carResponse.EnsureSuccessStatusCode();
        var carId = (await carResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var expedition = new Expedition
        {
            ExpeditionNumber = $"SEF-TEST-{Guid.NewGuid():N}",
            RomorkId = (int)carId,
            StatusId = 1,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.Expeditions.Add(expedition);
        await db.SaveChangesAsync();

        return (expedition.Id, carId);
    }

    private async Task<long> SeedLoadTransferAsync(int? romorkTypeId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var transfer = new LoadTransfer
        {
            LoadTransferId = $"TEST-{Guid.NewGuid():N}",
            LoadNumber = $"YUK-TEST-{Guid.NewGuid():N}",
            LoadNumberWorkType = $"YUK-TEST-{Guid.NewGuid():N}",
            RomorkTypeId = romorkTypeId,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.LoadTransfers.Add(transfer);
        await db.SaveChangesAsync();

        return transfer.Id;
    }

    [Fact]
    public async Task SaveMapping_WithMatchingRomorkType_LinksLoadAndAppearsInDetail()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var (expeditionId, _) = await SeedExpeditionWithCarAsync(admin, romorkType: 7);
        var loadTransferId = await SeedLoadTransferAsync(romorkTypeId: 7);

        var saveResponse = await admin.PostAsJsonAsync("/api/v1/expedition_load_mapping", new
        {
            expedition_id = expeditionId,
            load_transfer_id = loadTransferId,
        });
        saveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var detailResponse = await admin.GetAsync($"/api/v1/expedition_load_mapping/{expeditionId}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detailBody = await detailResponse.Content.ReadFromJsonAsync<JsonElement>();

        var data = detailBody.GetProperty("data");
        data.GetArrayLength().Should().Be(1);
        data[0].GetProperty("load_transfer_id").GetProperty("id").GetInt64().Should().Be(loadTransferId);

        // total_expedition_values zarfın İÇİNDE değil, kökte yer alır (bkz. controller XML yorumu).
        detailBody.TryGetProperty("total_expedition_values", out var totals).Should().BeTrue();
        totals.GetProperty("total_quantity").GetDecimal().Should().Be(0);

        var mappingId = data[0].GetProperty("id").GetInt64();
        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/expedition_load_mapping")
        {
            Content = JsonContent.Create(new { deletion_id = new[] { mappingId } }),
        };
        var deleteResponse = await admin.SendAsync(deleteRequest);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterDelete = await (await admin.GetAsync($"/api/v1/expedition_load_mapping/{expeditionId}"))
            .Content.ReadFromJsonAsync<JsonElement>();
        afterDelete.GetProperty("data").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task SaveMapping_WithMismatchedRomorkType_ReturnsValidationError()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var (expeditionId, _) = await SeedExpeditionWithCarAsync(admin, romorkType: 7);
        var loadTransferId = await SeedLoadTransferAsync(romorkTypeId: 9);

        var saveResponse = await admin.PostAsJsonAsync("/api/v1/expedition_load_mapping", new
        {
            expedition_id = expeditionId,
            load_transfer_id = loadTransferId,
        });

        saveResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await saveResponse.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").GetProperty("message")[0].GetString()
            .Should().Be("Yük ile Araç romork tipi uyuşmuyor");
    }
}
