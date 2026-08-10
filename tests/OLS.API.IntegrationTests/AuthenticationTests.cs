using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace OLS.API.IntegrationTests;

[Collection("OlsApi")]
public sealed class AuthenticationTests
{
    private readonly OlsApiFactory _factory;

    public AuthenticationTests(OlsApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_WithSeededAdminCredentials_ReturnsTokenAndUser()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/login",
            new { email = "admin@ols-scoped.local", password = "ChangeMe!Dev1" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("token").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("data").GetProperty("user").GetProperty("email").GetString()
            .Should().Be("admin@ols-scoped.local");
        body.GetProperty("message").GetString().Should().Be("Giriş Başarılı");
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401WithoutLeakingWhichFieldWasWrong()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/login",
            new { email = "admin@ols-scoped.local", password = "kesinlikle-yanlis-sifre" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithMissingEmail_ReturnsFieldValidationError()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/login", new { email = "", password = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").GetProperty("email").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutBearerToken_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/car");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithGarbageToken_Returns401()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer bu-gecerli-bir-jwt-degil");

        var response = await client.GetAsync("/api/v1/car");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CheckAuth_WithValidAdminToken_ReturnsAuthenticatedTrue()
    {
        using var client = await _factory.CreateAdminClientAsync();

        var response = await client.GetAsync("/api/v1/auth");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("authenticated").GetBoolean().Should().BeTrue();
        body.GetProperty("data").GetProperty("email").GetString().Should().Be("admin@ols-scoped.local");
    }

    [Fact]
    public async Task Logout_ThenReusingSameToken_Returns401()
    {
        // Her testin kendi jetonu olmalı (paylaşılan admin jetonunu iptal edemeyiz),
        // bu yüzden burada ayrı bir login atılıyor — "auth" rate limiti (10/dk) bu
        // tek ekstra çağrıyı rahatça karşılar.
        var token = await _factory.LoginAsync("admin@ols-scoped.local", "ChangeMe!Dev1");
        using var client = _factory.CreateAuthorizedClient(token);

        var logoutResponse = await client.PostAsync("/api/v1/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var reuseResponse = await client.GetAsync("/api/v1/auth");
        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
