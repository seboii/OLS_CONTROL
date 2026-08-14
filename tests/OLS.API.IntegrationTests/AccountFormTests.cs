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
        using var form = new MultipartFormDataContent { { new StringContent(name), "name" } };
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

        using var updateForm = new MultipartFormDataContent
        {
            { new StringContent(accountId.ToString()), "id" },
            { new StringContent("Koru Testi Güncellendi"), "name" },
        };
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

        using var updateForm = new MultipartFormDataContent
        {
            { new StringContent(accountId.ToString()), "id" },
            { new StringContent("Değiştir Testi Güncellendi"), "name" },
        };
        updateForm.Add(new StringContent("5"), "account_type_mapping[]");

        var updateResponse = await admin.PostAsync("/api/v1/account/update", updateForm);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var typeIds = await GetAccountTypeIdsAsync(admin, accountId);
        typeIds.Should().BeEquivalentTo([5]);
    }
}
