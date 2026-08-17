using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OLS.DataAccess.Context;

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
        using var accountForm = await TestAccountHelper.MinimalAccountFormAsync(admin, accountName);
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

    private static MultipartFormDataContent RequiredFieldsForm(long accountId) => new()
    {
        { new StringContent("1"), "work_type_id" },
        { new StringContent("1"), "loading_type_id" },
        { new StringContent("1"), "payment_type_id" },
        { new StringContent("4"), "status_type_id" },
        { new StringContent("1"), "department_id" },
        { new StringContent(accountId.ToString()), "customer_id" },
        { new StringContent("2026-09-01"), "offer_date" },
        { new StringContent("2026-09-30"), "offer_validity_date" },
        { new StringContent("2026-09-01"), "marketing_notification_date" },
        { new StringContent("1"), "load_content[0][quantity]" },
    };

    /// <summary>
    /// Bu oturumda BULUNAN gerçek bir hatanın regresyon testi: <c>LoadWriteService.
    /// UpdateAsync</c>, listeden çıkarılan bir dosyanın veritabanı satırını siliyordu
    /// ama FİZİKSEL dosyayı diskte bırakıyordu (canlı Docker'da bir dosya yükleyip
    /// kaldırarak bulundu — `docker exec` ile diskte yetim dosya doğrulandı). Kök
    /// neden: <c>OLS.Business</c>, <c>IFileStorage</c>'a (API katmanı) erişemiyor;
    /// silme çağrısını controller'a bırakan bir sonuç tipi eksikti. Düzeltme:
    /// <c>LoadUpdateResult.RemovedFileNames</c> eklendi, <c>LoadController.Update</c>
    /// bu isimler için <c>IFileStorage.Delete</c> çağırıyor.
    /// </summary>
    [Fact]
    public async Task UpdateLoad_RemovingAFile_DeletesBothDatabaseRowAndPhysicalFile()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountName = $"Dosya Test Cari {Guid.NewGuid():N}";
        using var accountForm = await TestAccountHelper.MinimalAccountFormAsync(admin, accountName);
        var accountResponse = await admin.PostAsync("/api/v1/account", accountForm);
        accountResponse.EnsureSuccessStatusCode();
        var accountId = (await accountResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();

        using var createForm = RequiredFieldsForm(accountId);
        var fileBytes = new ByteArrayContent(Encoding.UTF8.GetBytes("dosya silme regresyon testi - " + Guid.NewGuid()));
        fileBytes.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        createForm.Add(fileBytes, "files", "regresyon-test.txt");

        var createResponse = await admin.PostAsync("/api/v1/load", createForm);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loadId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();

        var afterCreate = (await (await admin.GetAsync($"/api/v1/load/{loadId}"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("load_file");
        afterCreate.GetArrayLength().Should().Be(1);
        var storedFileName = afterCreate[0].GetProperty("file").GetString();
        storedFileName.Should().NotBeNullOrEmpty();

        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var storageRoot = config["Storage:PublicPath"] ?? "/app/storage/app/public";
        var storedPath = Path.Combine(storageRoot, storedFileName!);

        // Sağlık kontrolü: dosya gerçekten diske yazılmış mı (yoksa aşağıdaki "silindi"
        // iddiası anlamsız olur — hiç var olmayan bir şeyin yokluğunu kanıtlamak olurdu).
        File.Exists(storedPath).Should().BeTrue("dosya gerçekten diske yazılmış olmalı");

        // Güncelleme: existing_file_ids GÖNDERİLMEZ -> dosya listeden çıkarılmış demektir.
        using var updateForm = RequiredFieldsForm(accountId);
        var updateResponse = await admin.PostAsync($"/api/v1/load/{loadId}", updateForm);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterUpdate = (await (await admin.GetAsync($"/api/v1/load/{loadId}"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("load_file");
        afterUpdate.GetArrayLength().Should().Be(0);

        File.Exists(storedPath).Should().BeFalse("fiziksel dosya da silinmiş olmalı, yalnızca DB satırı değil");
    }

    /// <summary>
    /// Bu oturumda BULUNAN gerçek bir bulgu: olsold'da <c>load_number</c> doluysa
    /// (yani teklif zaten Yük'e dönüştürülmüşse) <c>LoadController::update</c>
    /// "Yük oluşturulmuş kayıt güncellenemez" diyerek reddediyordu — bu portta
    /// hiç uygulanmıyordu. Sonuç: dönüştürülmüş bir teklifi düzenlemek, zaten
    /// Siber'e senkronlanmış Yük'ü sessizce senkron-dışı bırakıyor, üstelik her
    /// düzenleme TÜM alt kayıtları (içerik/mali kalem/hareket/görevli/e-posta)
    /// silip yeniden yazıyordu. <c>load_number</c> normalde yalnızca Teklif→Yük
    /// dönüşümüyle dolar (<c>LoadTransferWriteService.ConvertOfferAsync</c>) —
    /// burada test kurulumunu basitleştirmek için doğrudan DB'ye yazılıyor
    /// (kilit mantığı yalnızca alanın dolu/boş olduğuna bakıyor, dönüşüm
    /// akışının kendisine değil).
    /// </summary>
    private static async Task<long> CreateLoadWithNumberAsync(
        OlsApiFactory factory, HttpClient admin, long accountId, string loadNumber)
    {
        using var createForm = RequiredFieldsForm(accountId);
        var createResponse = await admin.PostAsync("/api/v1/load", createForm);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loadId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var load = await db.Loads.FirstAsync(l => l.Id == loadId);
        load.LoadNumber = loadNumber;
        await db.SaveChangesAsync();

        return loadId;
    }

    private static async Task<long> CreateTestAccountAsync(HttpClient admin, string namePrefix)
    {
        using var accountForm = await TestAccountHelper.MinimalAccountFormAsync(
            admin, $"{namePrefix} {Guid.NewGuid():N}");
        var accountResponse = await admin.PostAsync("/api/v1/account", accountForm);
        accountResponse.EnsureSuccessStatusCode();
        return (await accountResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();
    }

    [Fact]
    public async Task UpdateLoad_AfterLoadNumberAssigned_IsRejectedAndLeavesDataUntouched()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var accountId = await CreateTestAccountAsync(admin, "Kilit Test Cari");
        var loadId = await CreateLoadWithNumberAsync(_factory, admin, accountId, $"YUK-{Guid.NewGuid():N}"[..10]);

        using var updateForm = RequiredFieldsForm(accountId);
        updateForm.Add(new StringContent("Değiştirilmeye çalışılan açıklama"), "description");
        var response = await admin.PostAsync($"/api/v1/load/{loadId}", updateForm);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").GetProperty("message")[0].GetString()
            .Should().Be("Yük oluşturulmuş kayıt güncellenemez");

        var detail = (await (await admin.GetAsync($"/api/v1/load/{loadId}"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        detail.GetProperty("description").GetString().Should().NotBe("Değiştirilmeye çalışılan açıklama");
    }

    [Fact]
    public async Task UpdateTimeOut_AfterLoadNumberAssigned_IsRejected()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var accountId = await CreateTestAccountAsync(admin, "Kilit TimeOut Cari");
        var loadId = await CreateLoadWithNumberAsync(_factory, admin, accountId, $"YUK-{Guid.NewGuid():N}"[..10]);

        var response = await admin.PostAsJsonAsync("/api/v1/load/updateTimeOut", new { id = loadId });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").GetProperty("message")[0].GetString()
            .Should().Be("Yük oluşturulmuş kayıt güncellenemez");
    }

    [Fact]
    public async Task DeleteLoadContent_AfterLoadNumberAssigned_IsRejectedAndKeepsRow()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var accountId = await CreateTestAccountAsync(admin, "Kilit İçerik Cari");
        var loadId = await CreateLoadWithNumberAsync(_factory, admin, accountId, $"YUK-{Guid.NewGuid():N}"[..10]);

        var detail = (await (await admin.GetAsync($"/api/v1/load/{loadId}"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        var contentId = detail.GetProperty("load_content")[0].GetProperty("id").GetInt64();

        var response = await admin.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/load/load_content")
        {
            Content = JsonContent.Create(new { deletion_id = new[] { contentId } }),
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").GetProperty("message")[0].GetString()
            .Should().Be("Yük oluşturulmuş kayıt silinemez");

        var afterAttempt = (await (await admin.GetAsync($"/api/v1/load/{loadId}"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("load_content");
        afterAttempt.GetArrayLength().Should().Be(1, "kilitli bir Yük'ün alt satırı silinmemeli");
    }
}
