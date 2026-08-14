using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace OLS.API.IntegrationTests;

/// <summary>
/// Regresyon: UserFormDrawer.vue'nin avatar/PDKS/Ülke Kodu alanları ve UserRole.vue'nin
/// "Tümünü Seç" (select-all) sütun başlığı — backend (UserController/RoleController) zaten
/// destekliyordu ama UsersPage.tsx bu alanları hiç okumuyor/göndermiyordu; PDKS/telefon
/// ülkesi/avatar sadece POST /api/v1/user/update DEĞİL GET /api/v1/user/{id}'den de
/// hidratlanmalıydı (liste satırı bu alanları taşımıyor). Bu testler her iki sözleşmeyi
/// kilitliyor.
/// </summary>
[Collection("OlsApi")]
public sealed class UserFormTests
{
    private readonly OlsApiFactory _factory;

    public UserFormTests(OlsApiFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateAndUpdateUser_WithAvatarPkdsAndPhoneCountry_RoundTripsAllFields()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var countryResponse = await admin.GetAsync("/api/v1/country");
        countryResponse.EnsureSuccessStatusCode();
        var countries = (await countryResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        var country = countries.EnumerateArray().First();
        var countryId = country.GetProperty("id").GetGuid();

        var pngBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
        var email = $"user-form-{Guid.NewGuid():N}@example.test";

        using var createForm = new MultipartFormDataContent
        {
            { new StringContent("Form"), "name" },
            { new StringContent("Testi"), "surname" },
            { new StringContent(email), "email" },
            { new StringContent("Test!2026Pw"), "password" },
            { new StringContent("PDKS-0001"), "pkds_id" },
            { new StringContent(countryId.ToString()), "phone_country_id" },
        };
        var avatarContent = new ByteArrayContent(pngBytes);
        avatarContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        createForm.Add(avatarContent, "avatar", "avatar-test.png");

        var createResponse = await admin.PostAsync("/api/v1/user", createForm);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var userId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();

        var afterCreate = await GetUserAsync(admin, userId);
        afterCreate.GetProperty("pkds_id").GetString().Should().Be("PDKS-0001");
        afterCreate.GetProperty("phone_country_id").GetProperty("id").GetGuid().Should().Be(countryId);
        afterCreate.GetProperty("avatar").GetString().Should().NotBeNullOrEmpty(
            "GET /api/v1/user/{id} liste satırından farklı olarak avatar'ı da döndürmeli");

        using var updateForm = new MultipartFormDataContent
        {
            { new StringContent(userId.ToString()), "id" },
            { new StringContent("Form"), "name" },
            { new StringContent("Testi"), "surname" },
            { new StringContent(email), "email" },
            { new StringContent("PDKS-0002"), "pkds_id" },
            { new StringContent("1"), "avatar_remove" },
        };
        var updateResponse = await admin.PostAsync("/api/v1/user/update", updateForm);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterUpdate = await GetUserAsync(admin, userId);
        afterUpdate.GetProperty("pkds_id").GetString().Should().Be("PDKS-0002",
            "PDKS Numarası güncellenebilmeli");
        afterUpdate.GetProperty("avatar").ValueKind.Should().Be(JsonValueKind.Null,
            "avatar_remove=1 gönderildiğinde avatar temizlenmeli");
    }

    /// <summary>
    /// UserRole.vue'nin "Tümünü Seç" sütun başlığı: bir sütun (ör. Görüntüle) için tek
    /// tıkla o kullanıcının TÜM sayfa satırlarını günceller. RoleController zaten
    /// permission_page_id boş + user_id doluysa tüm satırları güncelliyordu (bkz.
    /// UserPermissionService.UpdateAsync) — burada yalnızca HEDEFLENEN crud sütununun
    /// değiştiğini, diğer sütunların dokunulmadan kaldığını kilitliyoruz (naif bir
    /// implementasyon yanlışlıkla dört sütunu da 1'e çekebilirdi).
    /// </summary>
    [Fact]
    public async Task UpdateRole_WithUserIdAndNoPermissionPageId_UpdatesOnlyTargetedCrudForEveryRow()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var email = $"role-selectall-{Guid.NewGuid():N}@example.test";
        var userId = await admin.CreateUserAsync(email);

        var initialRows = await admin.GetPermissionDataAsync(userId);
        var rowCount = initialRows.GetArrayLength();
        rowCount.Should().BeGreaterThan(0, "yeni kullanıcı tüm sayfalarda sıfır-değerli satırlarla açılır");
        initialRows.EnumerateArray().Should().OnlyContain(r => r.GetProperty("read").GetInt32() == 0);

        var selectAllReadResponse = await admin.PutAsJsonAsync("/api/v1/role", new
        {
            crud = "read",
            is_data = 1,
            user_id = userId,
        });
        selectAllReadResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterRead = await admin.GetPermissionDataAsync(userId);
        afterRead.GetArrayLength().Should().Be(rowCount, "satır sayısı değişmemeli, sadece bayraklar");
        afterRead.EnumerateArray().Should().OnlyContain(r => r.GetProperty("read").GetInt32() == 1);
        afterRead.EnumerateArray().Should().OnlyContain(r => r.GetProperty("create").GetInt32() == 0,
            "yalnızca 'read' sütunu hedeflendi — 'create' dokunulmamış kalmalı");

        var selectAllUpdateResponse = await admin.PutAsJsonAsync("/api/v1/role", new
        {
            crud = "update",
            is_data = 1,
            user_id = userId,
        });
        selectAllUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterUpdate = await admin.GetPermissionDataAsync(userId);
        afterUpdate.EnumerateArray().Should().OnlyContain(r => r.GetProperty("read").GetInt32() == 1,
            "önceki 'Tümünü Seç' sonucu korunmalı");
        afterUpdate.EnumerateArray().Should().OnlyContain(r => r.GetProperty("update").GetInt32() == 1);
        afterUpdate.EnumerateArray().Should().OnlyContain(r => r.GetProperty("create").GetInt32() == 0);
        afterUpdate.EnumerateArray().Should().OnlyContain(r => r.GetProperty("delete").GetInt32() == 0);
    }

    private static async Task<JsonElement> GetUserAsync(HttpClient admin, long id)
    {
        var response = await admin.GetAsync($"/api/v1/user/{id}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
    }
}
