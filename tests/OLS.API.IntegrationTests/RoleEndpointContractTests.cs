using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace OLS.API.IntegrationTests;

/// <summary>
/// GET/PUT /api/v1/role, projedeki diğer tüm uçlardan farklı bir zarf kullanır:
/// çıplak <c>{ id, stats: { permission_data, user_name } }</c> — <c>{data,message}</c>
/// DEĞİL (bkz. RoleController.cs üstündeki not). Bu sözleşmeyi frontend (auth.tsx,
/// UsersPage.tsx) yanlış varsayınca sidebar'ın tamamen boş göründüğü gerçek bir hata
/// bu oturumda bulunup düzeltildi; bu testler o sözleşmeyi kilitler.
/// </summary>
[Collection("OlsApi")]
public sealed class RoleEndpointContractTests
{
    private readonly OlsApiFactory _factory;

    public RoleEndpointContractTests(OlsApiFactory factory) => _factory = factory;

    [Fact]
    public async Task GetRole_ForSelf_ReturnsBareIdStatsEnvelope_NotWrappedInDataMessage()
    {
        using var client = await _factory.CreateAdminClientAsync();
        var adminId = await GetAdminIdAsync(client);

        var response = await client.GetAsync($"/api/v1/role?id={adminId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Kritik: kök seviyede "data" ya da "message" alanı OLMAMALI.
        body.TryGetProperty("data", out _).Should().BeFalse(
            "role ucu diğer uçların {data,message} zarfını kullanmıyor");
        body.TryGetProperty("message", out _).Should().BeFalse();

        body.TryGetProperty("id", out var idProp).Should().BeTrue();
        idProp.GetInt64().Should().Be(adminId);

        body.TryGetProperty("stats", out var stats).Should().BeTrue();
        stats.GetProperty("user_name").GetString().Should().Contain("Sistem");
        stats.GetProperty("permission_data").GetArrayLength().Should().BeGreaterThan(0,
            "seed admin tüm sayfalarda tam yetkiyle bootstrap edilir");

        var firstRow = stats.GetProperty("permission_data")[0];
        firstRow.GetProperty("read").GetInt32().Should().Be(1);
        firstRow.GetProperty("create").GetInt32().Should().Be(1);
        firstRow.GetProperty("update").GetInt32().Should().Be(1);
        firstRow.GetProperty("delete").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetRole_ForAnotherUser_WithoutRoleManagementPermission_Returns403()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var lowPrivEmail = $"role-test-{Guid.NewGuid():N}@example.test";
        await admin.CreateUserAsync(lowPrivEmail);
        var lowPrivToken = await _factory.LoginAsync(lowPrivEmail, "Test!2026Pw");

        using var lowPrivClient = _factory.CreateAuthorizedClient(lowPrivToken);
        var adminId = await GetAdminIdAsync(admin);

        // Kendi id'si DEĞİL, admin'in id'si isteniyor -> role_management/read gerekir,
        // yeni kullanıcının hiçbir sayfada yetkisi yok -> 403 beklenir.
        var response = await lowPrivClient.GetAsync($"/api/v1/role?id={adminId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetRole_ForSelf_IsAlwaysAllowed_EvenWithoutRoleManagementPermission()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var email = $"role-self-{Guid.NewGuid():N}@example.test";
        var newUserId = await admin.CreateUserAsync(email);
        var token = await _factory.LoginAsync(email, "Test!2026Pw");
        using var client = _factory.CreateAuthorizedClient(token);

        var response = await client.GetAsync($"/api/v1/role?id={newUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        // Yeni kullanıcı: her sayfada sıfır yetkili satır var (UserService.CreateAsync),
        // yani liste BOŞ değil ama tüm bayraklar 0.
        var rows = body.GetProperty("stats").GetProperty("permission_data");
        rows.GetArrayLength().Should().BeGreaterThan(0);
        rows[0].GetProperty("read").GetInt32().Should().Be(0);
    }

    private static async Task<long> GetAdminIdAsync(HttpClient adminClient)
    {
        var response = await adminClient.GetAsync("/api/v1/auth");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("id").GetInt64();
    }
}
