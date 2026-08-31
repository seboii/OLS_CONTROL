using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace OLS.API.IntegrationTests;

/// <summary>
/// Regresyon: AccountFormDrawer.vue'nin gerçek "Hesap Türü" alanı ÇOKLU seçim
/// (MultiSelect, multiple) — bir cari aynı anda birden fazla tipte olabilir (ör.
/// hem Müşteri hem Alıcı hem Gönderici). Port'un "Müşteri/Tedarikçi Tipi" alanı
/// yanlışlıkla TEKİL seçimdi: <c>account_type_mapping: v ? [v] : []</c> — bu,
/// zaten birden fazla tipi olan bir cariyi (canlı test verisinde GERÇEKTEN vardı:
/// "Test Lojistik A.Ş." → Müşteri+Alıcı+Gönderici) bu formdan kaydetmenin SESSİZCE
/// tek tipe düşürmesi anlamına geliyordu. Bu testler tam döngüyü (ekle→koru→
/// değiştir) kilitliyor.
/// </summary>
[Collection("OlsApi")]
public sealed class AccountFormTests
{
    private readonly OlsApiFactory _factory;

    public AccountFormTests(OlsApiFactory factory) => _factory = factory;

    private static async Task<long> CreateAccountAsync(HttpClient admin, string name, params int[] accountTypeIds)
    {
        using var form = await TestAccountHelper.MinimalAccountFormAsync(admin, name);
        foreach (var typeId in accountTypeIds)
            form.Add(new StringContent(typeId.ToString()), "account_type_mapping[]");

        var response = await admin.PostAsync("/api/v1/account", form);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();
    }

    private static async Task<int[]> GetAccountTypeIdsAsync(HttpClient admin, long accountId)
    {
        var response = await admin.GetAsync($"/api/v1/account/{accountId}");
        response.EnsureSuccessStatusCode();
        var data = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        return data.GetProperty("account_type_mapping_id").EnumerateArray()
            .Select(m => m.GetProperty("account_type_id").GetProperty("id").GetInt32())
            .OrderBy(id => id)
            .ToArray();
    }

    [Fact]
    public async Task CreateAccount_WithMultipleAccountTypes_PersistsAllOfThem()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountId = await CreateAccountAsync(admin, $"Çoklu Tip Cari {Guid.NewGuid():N}", 1, 3, 4);

        var typeIds = await GetAccountTypeIdsAsync(admin, accountId);
        typeIds.Should().BeEquivalentTo([1, 3, 4]);
    }

    /// <summary>
    /// Formu AÇIP dokunmadan tekrar Kaydet'e basmak (mevcut tüm tipleri aynen geri
    /// göndermek) hiçbirini SİLMEMELİ — bu, canlıda bulunan asıl hatanın senaryosu.
    /// </summary>
    [Fact]
    public async Task UpdateAccount_ResendingAllExistingTypes_KeepsAllOfThem()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var accountId = await CreateAccountAsync(admin, $"Koru Testi {Guid.NewGuid():N}", 1, 3, 4);

        using var updateForm = await TestAccountHelper.MinimalAccountFormAsync(
            admin, "Koru Testi Güncellendi", accountId);
        updateForm.Add(new StringContent("1"), "account_type_mapping[]");
        updateForm.Add(new StringContent("3"), "account_type_mapping[]");
        updateForm.Add(new StringContent("4"), "account_type_mapping[]");

        var updateResponse = await admin.PostAsync("/api/v1/account/update", updateForm);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var typeIds = await GetAccountTypeIdsAsync(admin, accountId);
        typeIds.Should().BeEquivalentTo([1, 3, 4], "hiçbir tip kaybolmamalı — bu tam da canlıda bulunan hatanın senaryosu");
    }

    /// <summary>Gönderilen küme değişince eşleme BAŞTAN kurulmalı — hem ekleme hem çıkarma çalışmalı.</summary>
    [Fact]
    public async Task UpdateAccount_WithDifferentTypeSet_ReplacesExistingMappings()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var accountId = await CreateAccountAsync(admin, $"Değiştir Testi {Guid.NewGuid():N}", 1, 3);

        using var updateForm = await TestAccountHelper.MinimalAccountFormAsync(
            admin, "Değiştir Testi Güncellendi", accountId);
        updateForm.Add(new StringContent("5"), "account_type_mapping[]");

        var updateResponse = await admin.PostAsync("/api/v1/account/update", updateForm);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var typeIds = await GetAccountTypeIdsAsync(admin, accountId);
        typeIds.Should().BeEquivalentTo([5]);
    }

    /// <summary>
    /// olsold <c>FrontAccountController\RequestSave</c>: <c>name</c>/<c>country_id</c>/
    /// <c>discount</c> üçü de zorunlu. Hedefte yalnızca <c>name</c> kontrol ediliyordu.
    /// </summary>
    [Fact]
    public async Task CreateAccount_WithoutCountryIdOrDiscount_Returns400WithBothFieldErrors()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        using var form = new MultipartFormDataContent
        {
            { new StringContent($"Eksik Alan Cari {Guid.NewGuid():N}"), "name" },
        };
        var response = await admin.PostAsync("/api/v1/account", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        errors.GetProperty("country_id")[0].GetString().Should().Be("Ülke seçimi yapılmalıdır");
        errors.GetProperty("discount")[0].GetString().Should().Be("İndirim oranı boş olamaz");
    }

    /// <summary>
    /// olsold <c>RequestUpdate</c>: <c>name</c> güncellemede de zorunlu ama hedefte
    /// yalnızca <c>id</c> kontrol ediliyordu — isim boş bırakılıp güncellenebiliyordu.
    /// </summary>
    [Fact]
    public async Task UpdateAccount_WithoutName_Returns400()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var accountId = await CreateAccountAsync(admin, $"İsim Testi {Guid.NewGuid():N}");

        using var updateForm = await TestAccountHelper.MinimalAccountFormAsync(admin, string.Empty, accountId);

        var response = await admin.PostAsync("/api/v1/account/update", updateForm);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        errors.GetProperty("name")[0].GetString().Should().Be("Adı boş olamaz");
    }

    [Fact]
    public async Task CreateAccount_WithDiscountExplicitlyZero_Succeeds()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        // olsold: discount=0 "required" kuralını GEÇER (Laravel'de 0 boş sayılmaz) —
        // yalnızca alan hiç GÖNDERİLMEZSE reddedilmeli.
        var accountId = await CreateAccountAsync(admin, $"Sıfır İndirim Cari {Guid.NewGuid():N}");

        var detail = await admin.GetAsync($"/api/v1/account/{accountId}");
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        (await detail.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("discount").GetInt32().Should().Be(0);
    }

    /// <summary>
    /// Görevli sekmesinde seçilen kişi, teklif formunun okuduğu
    /// <c>account_representatives</c> tablosuna SATIŞ TEMSİLCİSİ (user_type = 2)
    /// olarak yazılmalı.
    ///
    /// Gerçek hata buydu: yalnızca user_account_mappings (görünürlük) yazılıyordu,
    /// temsilci satırı hiç açılmıyordu. Sonuç, "Satış Temsilcisi müşteriye bağlı
    /// olsun" kuralının uygulamadan açılan carilerde hiç çalışmaması ve her
    /// teklifte operasyon yetkilisine düşülmesiydi.
    /// </summary>
    [Fact]
    public async Task CreateAccount_GorevliSecilince_SatisTemsilcisiKaydiAcilir()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var meResponse = await admin.GetAsync("/api/v1/auth");
        meResponse.EnsureSuccessStatusCode();
        var userId = (await meResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();

        var name = $"Temsilcili Cari {Guid.NewGuid():N}";
        using var form = await TestAccountHelper.MinimalAccountFormAsync(admin, name);
        form.Add(new StringContent("1"), "account_type_mapping[]");
        form.Add(new StringContent(userId.ToString()), "account_charge_person[]");

        var response = await admin.PostAsync("/api/v1/account", form);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var accountId = (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();

        var reps = await admin.GetAsync($"/api/v1/account/{accountId}/representatives");
        reps.EnsureSuccessStatusCode();

        var salesReps = (await reps.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("sales_reps").EnumerateArray()
            .Select(r => r.GetProperty("id").GetInt64())
            .ToArray();

        salesReps.Should().Contain(userId,
            "Görevli sekmesindeki kişi teklif formunda satış temsilcisi olarak görünmeli");
    }
}
