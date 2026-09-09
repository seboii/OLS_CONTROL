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
        var carPayload = await TestCarHelper.RequiredCarFieldsAsync(admin);
        carPayload["plate_number"] = plate;
        carPayload["romork_type"] = romorkType;
        var carResponse = await admin.PostAsJsonAsync("/api/v1/car", carPayload);
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

    /// <summary>
    /// RÖMORK TİPİ ARTIK ENGELLEMEZ — BULUNAN GERÇEK HATA.
    ///
    /// Eskiden seferin aracıyla yükün römork tipi aynı değilse eşleme
    /// reddediliyordu ("Yük ile Araç romork tipi uyuşmuyor"). Bu kural Siber'in
    /// kendi verisiyle çelişiyordu ve sefere yük eklemeyi fiilen çalışmaz hâle
    /// getirmişti:
    ///
    ///   • Siber'deki 10.540 yük-sefer eşlemesinin yalnızca 3.633'ünde (%34)
    ///     tipler tutuyor, 6.777'sinde (%64) TUTMUYOR.
    ///   • 8.070 yükün 4.195'inde (%52) römork tipi zaten boş; boş değer hiçbir
    ///     tipe eşit olmadığı için bu yükler koşulsuz reddediliyordu.
    ///   • skn_yukaktarma üzerinde hiç tetikleyici yok — Siber böyle bir
    ///     eşleşme aramıyor.
    /// </summary>
    [Fact]
    public async Task SaveMapping_WithMismatchedRomorkType_Succeeds()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var (expeditionId, _) = await SeedExpeditionWithCarAsync(admin, romorkType: 7);
        var loadTransferId = await SeedLoadTransferAsync(romorkTypeId: 9);

        var saveResponse = await admin.PostAsJsonAsync("/api/v1/expedition_load_mapping", new
        {
            expedition_id = expeditionId,
            load_transfer_id = loadTransferId,
        });

        saveResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            await saveResponse.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// AYNI yükü AYNI sefere iki kez bağlamak anlamsız — tek gerçek kopya
    /// kuralı budur.
    /// </summary>
    [Fact]
    public async Task SaveMapping_SameLoadTwiceOnSameExpedition_ReturnsValidationError()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var (expeditionId, _) = await SeedExpeditionWithCarAsync(admin, romorkType: 7);
        var loadTransferId = await SeedLoadTransferAsync(romorkTypeId: 7);

        var first = await admin.PostAsJsonAsync("/api/v1/expedition_load_mapping", new
        {
            expedition_id = expeditionId,
            load_transfer_id = loadTransferId,
        });
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await admin.PostAsJsonAsync("/api/v1/expedition_load_mapping", new
        {
            expedition_id = expeditionId,
            load_transfer_id = loadTransferId,
        });

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await second.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").GetProperty("message")[0].GetString()
            .Should().Be("Bu yük zaten bu sefere bağlı");
    }

    /// <summary>
    /// BİR YÜK BİRDEN ÇOK SEFERE BAĞLANABİLİR. Siber'in kendi verisinde 143
    /// yük öyle ve bu her yıl tekrarlıyor; uygulama bunu "Bu Sefer Zaten
    /// Eklendi" diyerek engelliyordu.
    /// </summary>
    [Fact]
    public async Task SaveMapping_SameLoadOnAnotherExpedition_Succeeds()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var (firstExpedition, _) = await SeedExpeditionWithCarAsync(admin, romorkType: 7);
        var (secondExpedition, _) = await SeedExpeditionWithCarAsync(admin, romorkType: 7);
        var loadTransferId = await SeedLoadTransferAsync(romorkTypeId: 7);

        (await admin.PostAsJsonAsync("/api/v1/expedition_load_mapping", new
        {
            expedition_id = firstExpedition,
            load_transfer_id = loadTransferId,
        })).StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await admin.PostAsJsonAsync("/api/v1/expedition_load_mapping", new
        {
            expedition_id = secondExpedition,
            load_transfer_id = loadTransferId,
        });

        second.StatusCode.Should().Be(HttpStatusCode.OK,
            await second.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// Seçim listesi yalnızca BU sefere bağlı yükleri gizler; başka seferde
    /// bağlı olan yük listelenir ve kaç seferde bağlı olduğu bildirilir.
    /// </summary>
    [Fact]
    public async Task AvailableLoads_HidesOnlyLoadsOnThisExpedition()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var (firstExpedition, _) = await SeedExpeditionWithCarAsync(admin, romorkType: 7);
        var (secondExpedition, _) = await SeedExpeditionWithCarAsync(admin, romorkType: 7);
        var loadTransferId = await SeedLoadTransferAsync(romorkTypeId: 7);

        (await admin.PostAsJsonAsync("/api/v1/expedition_load_mapping", new
        {
            expedition_id = firstExpedition,
            load_transfer_id = loadTransferId,
        })).StatusCode.Should().Be(HttpStatusCode.OK);

        static async Task<List<JsonElement>> RowsAsync(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();
            var data = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            return (data.ValueKind == JsonValueKind.Object ? data.GetProperty("data") : data)
                .EnumerateArray().ToList();
        }

        var onFirst = await RowsAsync(await admin.GetAsync(
            $"/api/v1/expedition_load_mapping?expedition_id={firstExpedition}&per_page=200"));
        onFirst.Should().NotContain(r => r.GetProperty("id").GetInt64() == loadTransferId,
            "yük zaten BU sefere bağlı");

        var onSecond = await RowsAsync(await admin.GetAsync(
            $"/api/v1/expedition_load_mapping?expedition_id={secondExpedition}&per_page=200"));
        var row = onSecond.Should().ContainSingle(r => r.GetProperty("id").GetInt64() == loadTransferId)
            .Subject;
        row.GetProperty("other_expedition_count").GetInt32().Should().BeGreaterThan(0);
    }
}
