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
            { new StringContent("1"), "load_content[0][product_type_id]" },
            { new StringContent("1"), "load_content[0][case_type_id]" },
            { new StringContent("15"), "load_content[0][quantity]" },
            { new StringContent("120"), "load_content[0][width]" },
            { new StringContent("100"), "load_content[0][height]" },
            { new StringContent("80"), "load_content[0][length]" },
            { new StringContent("250,5"), "load_content[0][gross_weight]" },
            { new StringContent("2,4"), "load_content[0][lademeter]" },
            { new StringContent("1"), "load_content[0][stackable]" },
            { new StringContent("1"), "load_financial_item[0][item]" },
            { new StringContent("1"), "load_financial_item[0][quantity]" },
            { new StringContent("1"), "load_financial_item[0][buysell]" },
            { new StringContent("1"), "load_financial_item[0][transport_type_id]" },
            { new StringContent("1"), "load_financial_item[0][order]" },
            { new StringContent("1.250,75"), "load_financial_item[0][net_price]" },
            { new StringContent("1.250,75"), "load_financial_item[0][total_price]" },
            { new StringContent("1"), "load_financial_item[0][currency]" },
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
        //
        // load_content ARTIK zorunlu değil (taslak mantığı — bkz. LoadController.
        // Validate XML açıklaması): boş içerikle de teklif kaydedilebilmeli, tam
        // doğrulama Sibere Aktar adımında yapılıyor. Bu yüzden yalnızca gerçekten
        // koşulsuz zorunlu kalan alanlar (customer_id) kontrol ediliyor.
        using var form = new MultipartFormDataContent { { new StringContent("test"), "description" } };
        var response = await admin.PostAsync("/api/v1/load", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").GetProperty("customer_id").GetArrayLength().Should().BeGreaterThan(0);
        body.GetProperty("errors").GetProperty("work_type_id").GetArrayLength().Should().BeGreaterThan(0);
    }

    /// <summary>
    /// olsold <c>LoadSave</c>: <c>load_content.*.product_type_id/case_type_id/quantity/
    /// width/height/length/gross_weight/lademeter/stackable</c> satır bazlı zorunlu —
    /// hedefte bu satır içi alanların HİÇBİRİ doğrulanmıyordu (yalnızca dizinin boş
    /// olmadığı kontrol ediliyordu).
    /// </summary>
    [Fact]
    public async Task CreateLoad_WithIncompleteContentRow_ReturnsIndexedFieldErrors()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var accountId = await CreateTestAccountAsync(admin, "İçerik Satırı Testi");

        using var form = RequiredFieldsForm(accountId);
        // load_content[0] zaten dolu — ikinci, EKSİK bir satır ekliyoruz.
        form.Add(new StringContent("2"), "load_content[1][quantity]");

        var response = await admin.PostAsync("/api/v1/load", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        foreach (var field in new[]
        {
            "product_type_id", "case_type_id", "width", "height", "length", "gross_weight", "lademeter", "stackable",
        })
            errors.TryGetProperty($"load_content.1.{field}", out _).Should().BeTrue($"satır 1'de '{field}' eksik");

        // Satır 0 tamdı — onda hata OLMAMALI.
        errors.TryGetProperty("load_content.0.product_type_id", out _).Should().BeFalse();
    }

    /// <summary>
    /// "Olumlu" teklif, Siber'e aktarılıp Yük'e dönüşecek tekliftir; bu yüzden
    /// dönüşümün ihtiyaç duyduğu alanlar Kaydet aşamasında zorunludur. Liste,
    /// Siber'in kendi rezervasyon ekranında KIRMIZI işaretli alanlardan alındı
    /// (kullanıcının paylaştığı ekran görüntüleri).
    ///
    /// Bir ara sürümde bu blok taslak mantığı için kaldırılmıştı; kullanıcı isteğiyle
    /// geri getirildi. "Henüz tamamlamadım" durumu artık KAYDETMEDEN, tarayıcıdaki
    /// otomatik taslakla karşılanıyor (bkz. frontend lib/autodraft.ts) — yani eksik
    /// bilgiyi "Olumlu" olarak veritabanına yazmaya gerek kalmadı.
    ///
    /// Acente ve Navlun Ödeyecek Firma Siber'de kırmızı OLMASINA rağmen bilinçli
    /// olarak zorunlu DEĞİL: gerçek veride Olumlu tekliflerin yalnızca %0,3'ünde ve
    /// %35'inde dolular, zorunlu tutmak mevcut iş akışını kilitlerdi.
    /// </summary>
    [Fact]
    public async Task CreateLoad_WithPositiveStatusAndMissingConditionalFields_ReturnsFieldErrors()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var accountId = await CreateTestAccountAsync(admin, "Olumlu Durum Testi");

        using var form = RequiredFieldsFormWithStatus(accountId, "5");
        var response = await admin.PostAsync("/api/v1/load", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            await response.Content.ReadAsStringAsync());

        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        foreach (var field in new[]
        {
            "sender_id", "receiver_id", "departure_country_id", "target_country_id",
            "instruction_id", "romork_type_id", "load_transfer_type_id", "way_of_working",
        })
            errors.TryGetProperty(field, out _).Should().BeTrue($"Olumlu teklifte '{field}' zorunlu");

        // Siber'de kirmizi ama bilincli olarak zorunlu DEGIL.
        errors.TryGetProperty("agent_id", out _).Should().BeFalse();
        errors.TryGetProperty("company_pay_freight_id", out _).Should().BeFalse();
    }

    /// <summary>
    /// olsold <c>LoadSave</c>: herhangi bir mali kalemde <c>net_price == 0</c> ise
    /// <c>load_financial_item.*.description</c> kuralı joker karakterle TÜM kalemlere
    /// uygulanır (yalnızca 0 fiyatlı satıra değil) — Laravel'in wildcard rule
    /// davranışının birebir taşınmış hâli.
    /// </summary>
    [Fact]
    public async Task CreateLoad_WithOneZeroPricedFinancialItem_RequiresDescriptionOnEveryRow()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var accountId = await CreateTestAccountAsync(admin, "Sıfır Fiyat Testi");
        var countryId = await FirstCountryIdAsync(admin);

        using var form = RequiredFieldsFormWithStatus(accountId, "5");
        form.Add(new StringContent(countryId), "departure_country_id");
        form.Add(new StringContent(countryId), "target_country_id");
        form.Add(new StringContent(accountId.ToString()), "sender_id");
        form.Add(new StringContent(accountId.ToString()), "receiver_id");
        form.Add(new StringContent("1"), "romork_type_id");
        form.Add(new StringContent("1"), "load_transfer_type_id");
        form.Add(new StringContent("1"), "way_of_working");
        form.Add(new StringContent("1"), "instruction_id");

        // Kalem 0: fiyatı sıfır, açıklaması YOK. Kalem 1: fiyatı normal, açıklaması da YOK
        // — olsold'un joker kuralı yüzünden İKİSİ de reddedilmeli.
        form.Add(new StringContent("1"), "load_financial_item[0][item]");
        form.Add(new StringContent("1"), "load_financial_item[0][quantity]");
        form.Add(new StringContent("1"), "load_financial_item[0][buysell]");
        form.Add(new StringContent("1"), "load_financial_item[0][transport_type_id]");
        form.Add(new StringContent("1"), "load_financial_item[0][order]");
        form.Add(new StringContent("0"), "load_financial_item[0][net_price]");
        form.Add(new StringContent("0"), "load_financial_item[0][total_price]");
        form.Add(new StringContent("1"), "load_financial_item[0][currency]");

        form.Add(new StringContent("1"), "load_financial_item[1][item]");
        form.Add(new StringContent("1"), "load_financial_item[1][quantity]");
        form.Add(new StringContent("1"), "load_financial_item[1][buysell]");
        form.Add(new StringContent("1"), "load_financial_item[1][transport_type_id]");
        form.Add(new StringContent("1"), "load_financial_item[1][order]");
        form.Add(new StringContent("100"), "load_financial_item[1][net_price]");
        form.Add(new StringContent("100"), "load_financial_item[1][total_price]");
        form.Add(new StringContent("1"), "load_financial_item[1][currency]");

        var response = await admin.PostAsync("/api/v1/load", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        errors.TryGetProperty("load_financial_item.0.description", out _).Should().BeTrue();
        errors.TryGetProperty("load_financial_item.1.description", out _).Should().BeTrue(
            "kaynakta net_price==0 kuralı joker karakterle TÜM satırlara uygulanır");
    }

    private static MultipartFormDataContent RequiredFieldsFormWithStatus(long accountId, string statusTypeId) => new()
    {
        { new StringContent("1"), "work_type_id" },
        { new StringContent("1"), "loading_type_id" },
        { new StringContent("1"), "payment_type_id" },
        { new StringContent(statusTypeId), "status_type_id" },
        { new StringContent("1"), "department_id" },
        { new StringContent(accountId.ToString()), "customer_id" },
        { new StringContent("2026-09-01"), "offer_date" },
        { new StringContent("2026-09-30"), "offer_validity_date" },
        { new StringContent("2026-09-01"), "marketing_notification_date" },
        { new StringContent("1"), "load_content[0][product_type_id]" },
        { new StringContent("1"), "load_content[0][case_type_id]" },
        { new StringContent("1"), "load_content[0][quantity]" },
        { new StringContent("100"), "load_content[0][width]" },
        { new StringContent("100"), "load_content[0][height]" },
        { new StringContent("100"), "load_content[0][length]" },
        { new StringContent("100"), "load_content[0][gross_weight]" },
        { new StringContent("1"), "load_content[0][lademeter]" },
        { new StringContent("1"), "load_content[0][stackable]" },
    };

    private static async Task<string> FirstCountryIdAsync(HttpClient admin)
    {
        var response = await admin.GetAsync("/api/v1/country");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").EnumerateArray().First().GetProperty("id").GetGuid().ToString();
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
        { new StringContent("1"), "load_content[0][product_type_id]" },
        { new StringContent("1"), "load_content[0][case_type_id]" },
        { new StringContent("1"), "load_content[0][quantity]" },
        { new StringContent("100"), "load_content[0][width]" },
        { new StringContent("100"), "load_content[0][height]" },
        { new StringContent("100"), "load_content[0][length]" },
        { new StringContent("100"), "load_content[0][gross_weight]" },
        { new StringContent("1"), "load_content[0][lademeter]" },
        { new StringContent("1"), "load_content[0][stackable]" },
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

    /// <summary>
    /// Kopyalanan teklif YENİ bir taslak olmalı: Siber kimlikleri, numaralar,
    /// durum ve onay bilgisi devredilmemeli.
    ///
    /// Bu alanların kopyalanması somut hasar verirdi — iki yerel teklif aynı
    /// Siber kaydını gösterir, biri kaydedilince diğerinin verisi ezilirdi.
    /// </summary>
    [Fact]
    public async Task DuplicateLoad_YeniTaslakUretir_SiberKimliginiTasimaz()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var accountId = await CreateTestAccountAsync(admin, "Kopya Test Cari");

        var sourceId = await CreateLoadWithNumberAsync(
            _factory, admin, accountId, $"KOPYA-{Guid.NewGuid():N}"[..20]);

        // Kaynağa Siber'e aktarılmış bir teklifin izlerini koy.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
            var source = await db.Loads.FirstAsync(l => l.Id == sourceId);
            source.SiberId = Guid.NewGuid().ToString();
            source.ReservationNumber = "2699999";
            source.TransferToSiber = 1;
            source.StatusTypeId = 5;                       // Olumlu
            source.ApprovalDate = new DateOnly(2026, 1, 15);
            source.RejectionReason = "eski gerekçe";
            await db.SaveChangesAsync();
        }

        var response = await admin.PostAsJsonAsync($"/api/v1/load/{sourceId}/duplicate", new { });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var copyId = (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();

        copyId.Should().NotBe(sourceId);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
            var copy = await db.Loads.AsNoTracking().FirstAsync(l => l.Id == copyId);
            var source = await db.Loads.AsNoTracking().FirstAsync(l => l.Id == sourceId);

            copy.CustomerId.Should().Be(source.CustomerId, "içerik kopyalanmalı");

            copy.SiberId.Should().BeNull("kopya Siber'e hiç gitmemiş olmalı");
            copy.ReservationNumber.Should().BeNull();
            copy.LoadNumber.Should().BeNull("yük numarası tek bir yüke aittir");
            copy.TransferToSiber.Should().Be(0);
            copy.StatusTypeId.Should().Be(4, "kopya 'Teklif' durumunda başlamalı");
            copy.ApprovalDate.Should().BeNull();
            copy.RejectionReason.Should().BeNull();

            // Kaynak hiç değişmemeli.
            source.SiberId.Should().NotBeNull();
            source.StatusTypeId.Should().Be(5);
        }
    }
}
