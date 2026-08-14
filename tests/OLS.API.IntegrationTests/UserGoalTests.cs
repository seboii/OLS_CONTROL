using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace OLS.API.IntegrationTests;

/// <summary>
/// Regresyon: Kullanıcılar formunun "Hedefler" sekmesi (UserTarget.vue →
/// api/v1/user_goal) hiç portlanmamıştı — bkz. docs/SECILI-MODUL-PARITE-MATRISI.md
/// §7 satır 134 ("istisnai kapsam-içi bağımlılık": genel Reports/Hedef-ciro
/// modülünden AYRI, UserFormDrawer'ın görsel/işlevsel bir parçası). Kaynakta
/// delete() yetki kontrolü YORUM SATIRINDAYDI (yanlış slug'la) — burada
/// user_management altında gerçek CRUD yetkisi uygulanıyor; bu testler hem CRUD'u
/// hem kaynağın tarih-aralığı çakışma kuralını hem yetki gerekliliğini kilitliyor.
/// </summary>
[Collection("OlsApi")]
public sealed class UserGoalTests
{
    private readonly OlsApiFactory _factory;

    public UserGoalTests(OlsApiFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateListAndDeleteGoal_RoundTripsCorrectly()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var userId = await admin.CreateUserAsync($"goal-{Guid.NewGuid():N}@example.test");

        var createResponse = await admin.PostAsJsonAsync("/api/v1/user_goal", new
        {
            user_id = userId,
            start_date = "2026-08-01",
            end_date = "2026-08-31",
            goal_price = 50000.50m,
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        var goalId = created.GetProperty("id").GetInt64();
        created.GetProperty("goal_price").GetDecimal().Should().Be(50000.50m);

        var listResponse = await admin.GetAsync($"/api/v1/user_goal?user_id={userId}");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = (await listResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        list.EnumerateArray().Should().ContainSingle(g => g.GetProperty("id").GetInt64() == goalId);

        var deleteResponse = await admin.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/user_goal")
        {
            Content = JsonContent.Create(new { deletion_id = new[] { goalId } }),
        });
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterDelete = (await (await admin.GetAsync($"/api/v1/user_goal?user_id={userId}"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        afterDelete.EnumerateArray().Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateGoal_ChangesAmountAndDateRange()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var userId = await admin.CreateUserAsync($"goal-upd-{Guid.NewGuid():N}@example.test");
        var goalId = await CreateGoalAsync(admin, userId, "2026-08-01", "2026-08-31", 10000m);

        var updateResponse = await admin.PutAsJsonAsync("/api/v1/user_goal", new
        {
            id = goalId,
            user_id = userId,
            start_date = "2026-09-01",
            end_date = "2026-09-30",
            goal_price = 20000m,
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await updateResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        updated.GetProperty("goal_price").GetDecimal().Should().Be(20000m);
        updated.GetProperty("start_date").GetString().Should().Be("2026-09-01");
    }

    /// <summary>Kaynağın kuralı: aynı kullanıcı için çakışan tarih aralığıyla ikinci bir hedef eklenemez.</summary>
    [Fact]
    public async Task CreateGoal_WithOverlappingDateRange_IsRejected()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var userId = await admin.CreateUserAsync($"goal-overlap-{Guid.NewGuid():N}@example.test");
        await CreateGoalAsync(admin, userId, "2026-08-01", "2026-08-31", 10000m);

        var overlapResponse = await admin.PostAsJsonAsync("/api/v1/user_goal", new
        {
            user_id = userId,
            start_date = "2026-08-15",
            end_date = "2026-09-15",
            goal_price = 5000m,
        });

        overlapResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var list = (await (await admin.GetAsync($"/api/v1/user_goal?user_id={userId}"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        list.GetArrayLength().Should().Be(1, "çakışan kayıt eklenmemeli");
    }

    /// <summary>Farklı (çakışmayan) bir aralıkla ikinci hedef eklenebilmeli.</summary>
    [Fact]
    public async Task CreateGoal_WithNonOverlappingDateRange_Succeeds()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var userId = await admin.CreateUserAsync($"goal-nooverlap-{Guid.NewGuid():N}@example.test");
        await CreateGoalAsync(admin, userId, "2026-08-01", "2026-08-31", 10000m);

        var secondResponse = await admin.PostAsJsonAsync("/api/v1/user_goal", new
        {
            user_id = userId,
            start_date = "2026-09-01",
            end_date = "2026-09-30",
            goal_price = 15000m,
        });
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = (await (await admin.GetAsync($"/api/v1/user_goal?user_id={userId}"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        list.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task List_ScopesStrictlyToRequestedUser()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var userA = await admin.CreateUserAsync($"goal-a-{Guid.NewGuid():N}@example.test");
        var userB = await admin.CreateUserAsync($"goal-b-{Guid.NewGuid():N}@example.test");
        await CreateGoalAsync(admin, userA, "2026-08-01", "2026-08-31", 1000m);
        await CreateGoalAsync(admin, userB, "2026-08-01", "2026-08-31", 2000m);

        var listA = (await (await admin.GetAsync($"/api/v1/user_goal?user_id={userA}"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        listA.GetArrayLength().Should().Be(1);
        listA[0].GetProperty("user_id").GetInt64().Should().Be(userA);
    }

    [Fact]
    public async Task Endpoints_WithoutUserManagementPermission_Return403()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var lowPrivEmail = $"goal-lowpriv-{Guid.NewGuid():N}@example.test";
        var lowPrivId = await admin.CreateUserAsync(lowPrivEmail);
        var token = await _factory.LoginAsync(lowPrivEmail, "Test!2026Pw");
        using var lowPriv = _factory.CreateAuthorizedClient(token);

        var listResponse = await lowPriv.GetAsync($"/api/v1/user_goal?user_id={lowPrivId}");
        listResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var createResponse = await lowPriv.PostAsJsonAsync("/api/v1/user_goal", new
        {
            user_id = lowPrivId,
            start_date = "2026-08-01",
            end_date = "2026-08-31",
            goal_price = 1000m,
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static async Task<long> CreateGoalAsync(
        HttpClient admin, long userId, string startDate, string endDate, decimal goalPrice)
    {
        var response = await admin.PostAsJsonAsync("/api/v1/user_goal", new
        {
            user_id = userId,
            start_date = startDate,
            end_date = endDate,
            goal_price = goalPrice,
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();
    }
}
