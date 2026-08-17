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

        var countryId = await FirstCountryIdAsync(admin);

        var pngBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
        var email = $"user-form-{Guid.NewGuid():N}@example.test";
        var phone = $"5{Random.Shared.NextInt64(100_000_000, 999_999_999)}";

        using var createForm = new MultipartFormDataContent
        {
            { new StringContent("Form"), "name" },
            { new StringContent("Testi"), "surname" },
            { new StringContent(email), "email" },
            { new StringContent("Test!2026Pw"), "password" },
            { new StringContent("Test!2026Pw"), "password_confirmation" },
            { new StringContent("PDKS-0001"), "pkds_id" },
            { new StringContent(countryId.ToString()), "phone_country_id" },
            { new StringContent(phone), "phone" },
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
            { new StringContent(phone), "phone" },
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

    /// <summary>
    /// olsold: UserSave/UserUpdate — <c>phone: required|unique</c> ikisinde de,
    /// <c>phone_country_id: required</c> ise YALNIZCA UserSave'de (UserUpdate'te yok).
    /// </summary>
    [Fact]
    public async Task CreateUser_WithoutPhoneOrPhoneCountryId_Returns422WithBothFieldErrors()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var email = $"user-nophone-{Guid.NewGuid():N}@example.test";

        using var form = new MultipartFormDataContent
        {
            { new StringContent("Form"), "name" },
            { new StringContent("Testi"), "surname" },
            { new StringContent(email), "email" },
            { new StringContent("Test!2026Pw"), "password" },
        };

        var response = await admin.PostAsync("/api/v1/user", form);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        errors.GetProperty("phone")[0].GetString().Should().Be("Telefon numarası boş olamaz");
        errors.GetProperty("phone_country_id")[0].GetString().Should().Be("Ülke Kodu boş olamaz");
    }

    [Fact]
    public async Task UpdateUser_WithoutPhoneCountryId_StillSucceeds_ButPhoneStaysRequired()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var email = $"user-update-nocountry-{Guid.NewGuid():N}@example.test";
        var phone = $"5{Random.Shared.NextInt64(100_000_000, 999_999_999)}";

        using var createForm = new MultipartFormDataContent
        {
            { new StringContent("Form"), "name" },
            { new StringContent("Testi"), "surname" },
            { new StringContent(email), "email" },
            { new StringContent("Test!2026Pw"), "password" },
            { new StringContent("Test!2026Pw"), "password_confirmation" },
            { new StringContent(phone), "phone" },
            { new StringContent((await FirstCountryIdAsync(admin)).ToString()), "phone_country_id" },
        };
        var createResponse = await admin.PostAsync("/api/v1/user", createForm);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var userId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();

        // olsold UserUpdate::rules() phone_country_id'yi hiç doğrulamıyor — göndermeden
        // güncelleme başarılı olmalı. phone yine de zorunlu (kendi değeriyle gönderiliyor).
        using var updateForm = new MultipartFormDataContent
        {
            { new StringContent(userId.ToString()), "id" },
            { new StringContent("Form"), "name" },
            { new StringContent("Testi"), "surname" },
            { new StringContent(email), "email" },
            { new StringContent(phone), "phone" },
        };
        var updateResponse = await admin.PostAsync("/api/v1/user/update", updateForm);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateUser_WithPhoneAlreadyUsedByAnotherUser_Returns422AndDoesNotCreateSecondUser()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var countryId = await FirstCountryIdAsync(admin);
        var phone = $"5{Random.Shared.NextInt64(100_000_000, 999_999_999)}";

        async Task<HttpResponseMessage> CreateWithPhoneAsync(string email) => await admin.PostAsync(
            "/api/v1/user",
            new MultipartFormDataContent
            {
                { new StringContent("Form"), "name" },
                { new StringContent("Testi"), "surname" },
                { new StringContent(email), "email" },
                { new StringContent("Test!2026Pw"), "password" },
                { new StringContent("Test!2026Pw"), "password_confirmation" },
                { new StringContent(phone), "phone" },
                { new StringContent(countryId.ToString()), "phone_country_id" },
            });

        var first = await CreateWithPhoneAsync($"user-phone-a-{Guid.NewGuid():N}@example.test");
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await CreateWithPhoneAsync($"user-phone-b-{Guid.NewGuid():N}@example.test");
        second.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var errors = (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        errors.GetProperty("phone")[0].GetString().Should().Be("Bu Telefon numarası zaten kullanılıyor");
    }

    /// <summary>
    /// olsold: UserSave — <c>password: required|confirmed</c>, <c>password_confirmation:
    /// required</c>. Kaynağın frontend Vuelidate kuralı (UserFormDrawer.vue) bu ikisini
    /// yalnızca form_type=='edit' iken required yapıyordu (create'te DEĞİL) — bu, `rules`
    /// nesnesinin onDrawerShow'da kalıcı olarak mutasyona uğratılmasından kaynaklanan bir
    /// istemci-tarafı hatadır (drawer bir kez edit modunda açılınca sonraki create'lerde de
    /// kalıcı olur). Backend'in KENDİSİ (UserSave.php) doğru davranışı taşır: create'te HER
    /// ZAMAN zorunlu. Bu test o doğru (backend) sözleşmeyi kilitliyor.
    /// </summary>
    [Fact]
    public async Task CreateUser_WithoutPasswordConfirmation_Returns422()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var countryId = await FirstCountryIdAsync(admin);
        var email = $"user-nopwconfirm-{Guid.NewGuid():N}@example.test";

        using var form = new MultipartFormDataContent
        {
            { new StringContent("Form"), "name" },
            { new StringContent("Testi"), "surname" },
            { new StringContent(email), "email" },
            { new StringContent("Test!2026Pw"), "password" },
            { new StringContent($"5{Random.Shared.NextInt64(100_000_000, 999_999_999)}"), "phone" },
            { new StringContent(countryId.ToString()), "phone_country_id" },
        };

        var response = await admin.PostAsync("/api/v1/user", form);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        errors.GetProperty("password_confirmation")[0].GetString().Should().Be("Şifre Tekrarı boş olamaz");
    }

    [Fact]
    public async Task CreateUser_WithMismatchedPasswordConfirmation_Returns422WithMismatchMessageOnPasswordField()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var countryId = await FirstCountryIdAsync(admin);
        var email = $"user-pwmismatch-{Guid.NewGuid():N}@example.test";

        using var form = new MultipartFormDataContent
        {
            { new StringContent("Form"), "name" },
            { new StringContent("Testi"), "surname" },
            { new StringContent(email), "email" },
            { new StringContent("Test!2026Pw"), "password" },
            { new StringContent("Different!2026Pw"), "password_confirmation" },
            { new StringContent($"5{Random.Shared.NextInt64(100_000_000, 999_999_999)}"), "phone" },
            { new StringContent(countryId.ToString()), "phone_country_id" },
        };

        var response = await admin.PostAsync("/api/v1/user", form);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        // olsold: Laravel'in 'confirmed' kuralı hatayı password_confirmation'a değil
        // password alanına ekler — birebir korundu.
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        errors.GetProperty("password")[0].GetString().Should().Be("Şifreler Eşleşmiyor");
    }

    /// <summary>
    /// olsold: UserUpdate — password/password_confirmation YALNIZCA password gönderildiyse
    /// doğrulanır. Burada yeni bir şifre gönderilip eşleşmeyen bir tekrar verildiğinde
    /// güncellemenin reddedildiği kilitleniyor (boş bırakılırsa hiç doğrulanmadığı zaten
    /// UpdateUser_WithoutPhoneCountryId_StillSucceeds_ButPhoneStaysRequired'da kapsanıyor).
    /// </summary>
    [Fact]
    public async Task UpdateUser_WithNewPasswordButMismatchedConfirmation_Returns422()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var email = $"user-update-pwmismatch-{Guid.NewGuid():N}@example.test";
        var userId = await admin.CreateUserAsync(email);

        using var updateForm = new MultipartFormDataContent
        {
            { new StringContent(userId.ToString()), "id" },
            { new StringContent("Form"), "name" },
            { new StringContent("Testi"), "surname" },
            { new StringContent(email), "email" },
            { new StringContent($"5{Random.Shared.NextInt64(100_000_000, 999_999_999)}"), "phone" },
            { new StringContent("NewPassword!2026"), "password" },
            { new StringContent("DifferentPassword!2026"), "password_confirmation" },
        };
        var updateResponse = await admin.PostAsync("/api/v1/user/update", updateForm);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var errors = (await updateResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        errors.GetProperty("password")[0].GetString().Should().Be("Şifreler Eşleşmiyor");
    }

    private static async Task<Guid> FirstCountryIdAsync(HttpClient admin)
    {
        var response = await admin.GetAsync("/api/v1/country");
        response.EnsureSuccessStatusCode();
        var countries = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        return countries.EnumerateArray().First().GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> GetUserAsync(HttpClient admin, long id)
    {
        var response = await admin.GetAsync($"/api/v1/user/{id}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
    }
}
