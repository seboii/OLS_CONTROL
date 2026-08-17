using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.API.IntegrationTests;

/// <summary>
/// olsold <c>expeditionSave</c>/<c>expeditionUpdate</c>: <c>romork_id/expedition_type/
/// work_type/department_id</c> her ikisinde de zorunlu; <c>expedition_status_id</c>
/// yalnızca Update'te zorunlu; <c>expedition_status_id==8</c> ise tarihler+3 şehir de
/// zorunlu olur; <c>return_date</c>/<c>loading_date</c> her zaman <c>release_date</c>'ten
/// küçük olamaz. HEDEF'te bu doğrulamaların HİÇBİRİ uygulanmıyordu.
/// </summary>
[Collection("OlsApi")]
public sealed class ExpeditionTests
{
    private readonly OlsApiFactory _factory;

    public ExpeditionTests(OlsApiFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateExpedition_WithoutRequiredFields_ReturnsValidationErrors()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.PostAsJsonAsync("/api/v1/expedition", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        foreach (var field in new[] { "romork_id", "expedition_type", "work_type", "department_id" })
            errors.TryGetProperty(field, out _).Should().BeTrue($"'{field}' oluşturmada zorunlu");
    }

    [Fact]
    public async Task UpdateExpedition_WithoutRequiredFields_ReturnsValidationErrorsIncludingStatus()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var expeditionId = await CreateExpeditionAsync(admin);

        var response = await admin.PutAsJsonAsync("/api/v1/expedition", new { id = expeditionId });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        foreach (var field in new[] { "romork_id", "expedition_type", "work_type", "department_id", "expedition_status_id" })
            errors.TryGetProperty(field, out _).Should().BeTrue($"'{field}' güncellemede zorunlu");
    }

    /// <summary>olsold: <c>expedition_status_id == 8</c> ise tarihler + 3 şehir zorunlu olur.</summary>
    [Fact]
    public async Task UpdateExpedition_WithStatus8AndMissingConditionalFields_ReturnsAllConditionalErrors()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var expeditionId = await CreateExpeditionAsync(admin);
        var (romorkId, expeditionType, workType, departmentId) = await LookupIdsAsync(admin);

        var response = await admin.PutAsJsonAsync("/api/v1/expedition", new
        {
            id = expeditionId,
            romork_id = romorkId,
            expedition_type_id = expeditionType,
            work_type = workType,
            department_id = departmentId,
            expedition_status_id = 8,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        foreach (var field in new[]
        {
            "car_exit_date", "release_date", "return_date", "loading_date",
            "start_city_id", "load_city_id", "end_city_id",
        })
            errors.TryGetProperty(field, out _).Should().BeTrue($"'{field}' expedition_status_id=8 iken zorunlu");
    }

    /// <summary>olsold: durumdan bağımsız — release_date doluysa return_date ondan küçük olamaz.</summary>
    [Fact]
    public async Task UpdateExpedition_WithReturnDateBeforeReleaseDate_ReturnsDateOrderError()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var expeditionId = await CreateExpeditionAsync(admin);
        var (romorkId, expeditionType, workType, departmentId) = await LookupIdsAsync(admin);

        var response = await admin.PutAsJsonAsync("/api/v1/expedition", new
        {
            id = expeditionId,
            romork_id = romorkId,
            expedition_type_id = expeditionType,
            work_type = workType,
            department_id = departmentId,
            expedition_status_id = 1,
            release_date = "2026-09-10",
            return_date = "2026-09-01",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        errors.GetProperty("return_date")[0].GetString()
            .Should().Be("Bitiş Tarihi Başlangıç tarihinden küçük olamaz");
    }

    [Fact]
    public async Task UpdateExpedition_WithStatus8AndFullValidData_Succeeds()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var expeditionId = await CreateExpeditionAsync(admin);
        var (romorkId, expeditionType, workType, departmentId) = await LookupIdsAsync(admin);
        var cityId = await FirstCityIdAsync(admin);
        await EnsureExpeditionStatusEightAsync();

        var response = await admin.PutAsJsonAsync("/api/v1/expedition", new
        {
            id = expeditionId,
            romork_id = romorkId,
            expedition_type_id = expeditionType,
            work_type = workType,
            department_id = departmentId,
            expedition_status_id = 8,
            car_exit_date = "2026-09-01",
            release_date = "2026-09-01",
            return_date = "2026-09-10",
            loading_date = "2026-09-02",
            start_city_id = cityId,
            load_city_id = cityId,
            end_city_id = cityId,
        });

        var debugBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, debugBody);
    }

    /// <summary>
    /// olsold: <c>ExpeditionWriteService.CreateAsync</c> gerçek Siber'e yazıyor
    /// (<c>_siber.IsConfigured</c> değilse HER ZAMAN reddeder) — test ortamında
    /// Siber bilinçli olarak yapılandırılmadığından (bkz. ExpeditionLoadMappingTests)
    /// <c>POST /api/v1/expedition</c> ile gerçek bir sefer oluşturmak mümkün değil.
    /// Update uç noktasının doğrulama sözleşmesini test edebilmek için satırı
    /// doğrudan DbContext ile (gerçek EF Core, gerçek Postgres) kuruyoruz.
    /// </summary>
    private async Task<long> CreateExpeditionAsync(HttpClient admin)
    {
        var (romorkId, expeditionType, workType, departmentId) = await LookupIdsAsync(admin);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var expedition = new Expedition
        {
            ExpeditionNumber = $"SEF-TEST-{Guid.NewGuid():N}",
            RomorkId = (int)romorkId,
            ExpeditionTypeId = expeditionType,
            WorkType = workType,
            DepartmentId = departmentId,
            StatusId = 1,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.Expeditions.Add(expedition);
        await db.SaveChangesAsync();

        return expedition.Id;
    }

    private async Task<(long RomorkId, int ExpeditionType, int WorkType, int DepartmentId)> LookupIdsAsync(
        HttpClient admin)
    {
        var carPayload = await TestCarHelper.RequiredCarFieldsAsync(admin);
        carPayload["plate_number"] = $"SEF-{Guid.NewGuid():N}"[..12];
        var carResponse = await admin.PostAsJsonAsync("/api/v1/car", carPayload);
        carResponse.EnsureSuccessStatusCode();
        var romorkId = (await carResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();

        // olsold'da expedition_types tablosu için (expedition_statuses gibi) hiçbir
        // migration/seeder INSERT'i yok — hem kaynakta hem hedefte boş, gerçek
        // dağıtımda admin ekranından doldurulması bekleniyor. Testin kendi satırını
        // doğrudan DbContext ile ekliyoruz (üründe seed EDİLMİYOR, bilinçli olarak).
        var expeditionType = await EnsureExpeditionTypeAsync();
        var workType = await TestCarHelper.FirstLookupIdAsync(admin, "work_type");
        var departmentId = await TestCarHelper.FirstLookupIdAsync(admin, "department");

        return (romorkId, expeditionType, workType, departmentId);
    }

    private async Task<int> EnsureExpeditionTypeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var existing = await db.ExpeditionTypes.AsNoTracking().Select(t => t.Id).FirstOrDefaultAsync();
        if (existing != 0)
            return (int)existing;

        var type = new ExpeditionType { Name = $"Test Sefer Tipi {Guid.NewGuid():N}" };
        db.ExpeditionTypes.Add(type);
        await db.SaveChangesAsync();
        return (int)type.Id;
    }

    /// <summary>
    /// olsold'un <c>expedition_status_id == 8</c> koşulu belirli bir ID'yi hedefliyor —
    /// <c>expedition_statuses</c> boş olduğundan (bkz. yukarıdaki not) ID sütunu
    /// "generated by default as identity" olsa da EF Core'un yüksek seviye <c>Add</c>'i
    /// istemci tarafında verilen ID'yi INSERT'e dahil etmiyor; bu yüzden satır ham
    /// SQL ile (Postgres "by default" identity açık değer kabul eder) ekleniyor.
    /// </summary>
    private async Task EnsureExpeditionStatusEightAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO expedition_statuses (id, name) VALUES (8, 'Test Durum 8') ON CONFLICT (id) DO NOTHING");
    }

    /// <summary>
    /// olsold'da (expedition_types gibi) <c>cities</c> için de hiçbir seed INSERT'i
    /// yok — hem kaynakta hem hedefte boş, gerçek dağıtımda admin ekranından
    /// doldurulması bekleniyor. Testin kendi satırını doğrudan DbContext ile ekliyoruz.
    /// </summary>
    private async Task<string> FirstCityIdAsync(HttpClient admin)
    {
        var response = await admin.GetAsync("/api/v1/city");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");
        if (data.GetArrayLength() > 0)
            return data[0].GetProperty("id").GetGuid().ToString();

        var countryResponse = await admin.GetAsync("/api/v1/country");
        countryResponse.EnsureSuccessStatusCode();
        var countryId = (await countryResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").EnumerateArray().First().GetProperty("id").GetGuid();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var city = new City
        {
            Id = Guid.NewGuid(),
            CountryId = countryId.ToString().ToUpperInvariant(),
            Name = $"Test Şehir {Guid.NewGuid():N}",
        };
        db.Cities.Add(city);
        await db.SaveChangesAsync();
        return city.Id.ToString();
    }
}
