using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OLS.API.IntegrationTests;

/// <summary>
/// Profil (Hesabım) — avatar yükleme/kaldırma ve BR-012 (mevcut şifre kontrolü).
/// Bu oturumda bulundu: avatar hiç frontend'de gösterilmiyordu/yüklenemiyordu (backend
/// zaten doğruydu) — TopBar/UsersPage/ProfilePage'e eklendi, canlıda doğrulandı. Burada
/// backend sözleşmesi kilitleniyor.
/// </summary>
[Collection("OlsApi")]
public sealed class ProfileTests
{
    private readonly OlsApiFactory _factory;

    public ProfileTests(OlsApiFactory factory) => _factory = factory;

    [Fact]
    public async Task UpdateGeneral_WithAvatar_ThenRemovingIt_DeletesPhysicalFile()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        // 1x1 kırmızı PNG.
        var pngBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

        using var uploadForm = new MultipartFormDataContent { { new StringContent("Sistem"), "name" } };
        var avatarContent = new ByteArrayContent(pngBytes);
        avatarContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        uploadForm.Add(avatarContent, "avatar", "avatar-test.png");

        var uploadResponse = await admin.PostAsync("/api/v1/profile/general/update", uploadForm);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var uploadBody = await uploadResponse.Content.ReadFromJsonAsync<JsonElement>();
        var storedName = uploadBody.GetProperty("data").GetProperty("avatar").GetString();
        storedName.Should().NotBeNullOrEmpty();

        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var storageRoot = config["Storage:PublicPath"] ?? "/app/storage/app/public";
        var storedPath = Path.Combine(storageRoot, storedName!);
        File.Exists(storedPath).Should().BeTrue("avatar gerçekten diske yazılmış olmalı");

        using var removeForm = new MultipartFormDataContent
        {
            { new StringContent("Sistem"), "name" },
            { new StringContent("1"), "avatar_remove" },
        };
        var removeResponse = await admin.PostAsync("/api/v1/profile/general/update", removeForm);
        removeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var removeBody = await removeResponse.Content.ReadFromJsonAsync<JsonElement>();
        removeBody.GetProperty("data").GetProperty("avatar").ValueKind.Should()
            .BeOneOf(JsonValueKind.Null, JsonValueKind.String);
        var afterRemove = removeBody.GetProperty("data").GetProperty("avatar");
        (afterRemove.ValueKind == JsonValueKind.Null || string.IsNullOrEmpty(afterRemove.GetString()))
            .Should().BeTrue();

        File.Exists(storedPath).Should().BeFalse("kaldırma fiziksel dosyayı da silmeli");
    }

    [Fact]
    public async Task UpdatePassword_WithWrongCurrentPassword_ReturnsError()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var email = $"profile-pw-{Guid.NewGuid():N}@example.test";
        await admin.CreateUserAsync(email);
        var token = await _factory.LoginAsync(email, "Test!2026Pw");
        using var client = _factory.CreateAuthorizedClient(token);

        using var form = new MultipartFormDataContent
        {
            { new StringContent("YanlisSifre!123"), "current_password" },
            { new StringContent("YeniSifre!456"), "new_password" },
            { new StringContent("YeniSifre!456"), "new_password_confirmation" },
        };
        var response = await client.PostAsync("/api/v1/profile/password/update", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Be("Geçerli şifre yanlış.");
    }

    [Fact]
    public async Task UpdatePassword_WithCorrectCurrentPassword_ActuallyChangesIt()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var email = $"profile-pw-ok-{Guid.NewGuid():N}@example.test";
        await admin.CreateUserAsync(email);
        var token = await _factory.LoginAsync(email, "Test!2026Pw");
        using var client = _factory.CreateAuthorizedClient(token);

        using var form = new MultipartFormDataContent
        {
            { new StringContent("Test!2026Pw"), "current_password" },
            { new StringContent("YeniSifre!789"), "new_password" },
            { new StringContent("YeniSifre!789"), "new_password_confirmation" },
        };
        var response = await client.PostAsync("/api/v1/profile/password/update", form);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Yalnızca 200 dönmesi yeterli değil — şifre GERÇEKTEN değişmiş olmalı.
        // LoginAsync'in kendi 429 yeniden deneme mantığı var (bkz. OlsApiFactory), bu
        // yüzden ham bir ikinci istek yerine onu kullanıyoruz.
        var newToken = await _factory.LoginAsync(email, "YeniSifre!789");
        newToken.Should().NotBeNullOrEmpty();
    }
}
