using System.Net.Http.Json;
using System.Text.Json;

namespace OLS.API.IntegrationTests;

/// <summary>
/// "Sıfır yetkili yeni kullanıcı" senaryolarını kurmak için ortak yardımcılar.
/// UserService.CreateAsync her yeni kullanıcı için TÜM yetki sayfalarında
/// sıfır-değerli (read=create=update=delete=0) birer user_permissions satırı
/// açar (bkz. UserService.cs:143-158) — bu yüzden bir sayfaya yetki VERMEK için
/// önce GET /api/v1/role ile o sayfanın satır id'sini bulup PUT ile güncellemek
/// gerekir; UpdateAsync var olmayan bir satırı INSERT etmez (bkz. UserPermissionService).
/// </summary>
public static class TestUserHelper
{
    public static async Task<long> CreateUserAsync(
        this HttpClient adminClient, string email, string password = "Test!2026Pw")
    {
        var countryId = await FirstCountryIdAsync(adminClient);

        using var form = new MultipartFormDataContent
        {
            { new StringContent("Test"), "name" },
            { new StringContent("Kullanici"), "surname" },
            { new StringContent(email), "email" },
            { new StringContent(password), "password" },
            { new StringContent($"5{Random.Shared.NextInt64(100_000_000, 999_999_999)}"), "phone" },
            { new StringContent(countryId), "phone_country_id" },
        };

        var response = await adminClient.PostAsync("/api/v1/user", form);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("id").GetInt64();
    }

    /// <summary>olsold: UserSave'de <c>phone_country_id</c> zorunlu — herhangi bir ülke yeter.</summary>
    private static async Task<string> FirstCountryIdAsync(HttpClient adminClient)
    {
        var response = await adminClient.GetAsync("/api/v1/country");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").EnumerateArray().First()
            .GetProperty("id").GetGuid().ToString();
    }

    public static async Task<JsonElement> GetPermissionDataAsync(this HttpClient client, long userId)
    {
        var response = await client.GetAsync($"/api/v1/role?id={userId}");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("stats").GetProperty("permission_data");
    }

    /// <summary>Belirli bir modül sayfasında tek bir CRUD bayrağını 1'e çeker.</summary>
    public static async Task GrantPermissionAsync(
        this HttpClient adminClient, long userId, string pageSlug, string crud)
    {
        var rows = await adminClient.GetPermissionDataAsync(userId);

        var row = rows.EnumerateArray()
            .FirstOrDefault(r => r.GetProperty("permission_page_slug").GetString() == pageSlug);

        if (row.ValueKind == JsonValueKind.Undefined)
            throw new InvalidOperationException(
                $"'{pageSlug}' slug'lı yetki sayfası bulunamadı — DbSeeder'da seed edilmemiş olabilir.");

        var rowId = row.GetProperty("id").GetInt64();

        var response = await adminClient.PutAsJsonAsync("/api/v1/role", new
        {
            crud,
            is_data = 1,
            permission_page_id = rowId,
        });

        response.EnsureSuccessStatusCode();
    }
}
