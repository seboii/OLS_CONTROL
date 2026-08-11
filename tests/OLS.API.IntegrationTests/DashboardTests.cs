using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace OLS.API.IntegrationTests;

/// <summary>
/// Dashboard, kullanıcının "hazır tasarımda var, eklenmedi" düzeltmesi üzerine
/// sonradan kapsama girdi (bkz. TESLIM-RAPORU.md). Yalnızca [Authorize] — belirli
/// bir sayfa yetkisi gerektirmiyor, kendi özet ekranı.
/// </summary>
[Collection("OlsApi")]
public sealed class DashboardTests
{
    private readonly OlsApiFactory _factory;

    public DashboardTests(OlsApiFactory factory) => _factory = factory;

    [Fact]
    public async Task GetDashboard_WithoutToken_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDashboard_AsAuthenticatedUser_ReturnsRealAggregatesNotFakeData()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.GetAsync("/api/v1/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");

        // Zarf şekli diğer uçlarla aynı ({data,message}), Dashboard için özel bir
        // istisna yok (RoleController'ın aksine).
        body.GetProperty("message").GetString().Should().NotBeNullOrEmpty();

        var metrics = data.GetProperty("metrics");
        metrics.GetProperty("active_customers").GetInt32().Should().BeGreaterThanOrEqualTo(0);

        data.GetProperty("monthly_shipments").GetArrayLength().Should().Be(6, "son 6 ay gösterilir");
        data.GetProperty("weekly_completed_trips").GetArrayLength().Should().Be(7, "haftanın 7 günü gösterilir");

        // work_type_distribution / recent_activity / upcoming_trips veri yoksa BOŞ
        // DİZİ olmalı — sahte/örnek satırlarla doldurulmamalı.
        data.GetProperty("work_type_distribution").ValueKind.Should().Be(JsonValueKind.Array);
        data.GetProperty("recent_activity").ValueKind.Should().Be(JsonValueKind.Array);
        data.GetProperty("upcoming_trips").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetDashboard_ActiveCustomers_MatchesRealAccountCount()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountsResponse = await admin.GetAsync("/api/v1/account?per_page=1");
        var accountsBody = await accountsResponse.Content.ReadFromJsonAsync<JsonElement>();
        var realTotal = accountsBody.GetProperty("data").GetProperty("total").GetInt32();

        var dashboardResponse = await admin.GetAsync("/api/v1/dashboard");
        var dashboardBody = await dashboardResponse.Content.ReadFromJsonAsync<JsonElement>();
        var dashboardActiveCustomers = dashboardBody.GetProperty("data").GetProperty("metrics")
            .GetProperty("active_customers").GetInt32();

        // Dashboard sayısı, gerçek /api/v1/account uç noktasının döndürdüğü gerçek
        // toplamla BİREBİR eşleşmeli (super_admin olarak) — uydurma bir sayı değil.
        dashboardActiveCustomers.Should().Be(realTotal);
    }
}
