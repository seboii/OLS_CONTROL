using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace OLS.API.IntegrationTests;

/// <summary>
/// OLS ve Avrora bu noktada İKİ AYRI ŞİRKET: yük açma yolları birbirini dışlar.
///
///   • Avrora teklif kullanmıyor. Teklifler ekranı hiç görünmez ve uçları
///     kapalıdır; yük doğrudan Yükler ekranından açılır.
///   • OLS teklifle çalışır. Her yük bir teklifin dönüşümü olduğu için
///     teklifsiz yük açma düğmesi ve ucu yoktur.
///
/// Kural yetkiyle ifade EDİLEMİYOR: Teklifler ve Yükler ekranları aynı yetki
/// sayfasını (load_management) paylaşıyor, dolayısıyla Teklifler'i yetkiyle
/// kapatmak Yükler'i de kapatırdı. Karar tek yerde:
/// <c>CompanyScope.ResolveCapabilitiesAsync</c>.
///
/// Kapsam Avrora'ya iki yoldan çözülür: <c>users.siber_company_id</c> ya da
/// e-posta alan adı. Buradaki testler alan adı yolunu kullanıyor.
/// </summary>
[Collection("OlsApi")]
public sealed class CompanyCapabilityTests
{
    private readonly OlsApiFactory _factory;

    public CompanyCapabilityTests(OlsApiFactory factory) => _factory = factory;

    private async Task<HttpClient> UserClientAsync(string email)
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var userId = await admin.CreateUserAsync(email);
        await admin.GrantPermissionAsync(userId, "load_management", "read");
        await admin.GrantPermissionAsync(userId, "load_management", "create");

        var token = await _factory.LoginAsync(email, "Test!2026Pw");
        return _factory.CreateAuthorizedClient(token);
    }

    private static async Task<(bool UsesOffers, bool CanDirect)> CapabilitiesAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/capabilities");
        response.EnsureSuccessStatusCode();

        var data = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        return (data.GetProperty("uses_offers").GetBoolean(),
                data.GetProperty("can_create_direct_load").GetBoolean());
    }

    [Fact]
    public async Task AvroraUser_HasNoOfferModule_ButCanCreateLoadDirectly()
    {
        using var client = await UserClientAsync(
            $"teklifsiz-{Guid.NewGuid():N}@avroralog.com");

        var capabilities = await CapabilitiesAsync(client);
        capabilities.UsesOffers.Should().BeFalse("Avrora teklif kullanmıyor");
        capabilities.CanDirect.Should().BeTrue("yükü doğrudan açıyor");

        // Menüde sekme gizli olsa da uç kapalı olmalı: gizli menü yetki değildir.
        (await client.GetAsync("/api/v1/load")).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);

        var allowed = await client.GetAsync("/api/v1/load_transfer/direct/allowed");
        allowed.EnsureSuccessStatusCode();
        (await allowed.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("allowed").GetBoolean()
            .Should().BeTrue();
    }

    [Fact]
    public async Task OlsUser_HasOfferModule_ButCannotCreateLoadDirectly()
    {
        using var client = await UserClientAsync(
            $"teklifli-{Guid.NewGuid():N}@example.test");

        var capabilities = await CapabilitiesAsync(client);
        capabilities.UsesOffers.Should().BeTrue("OLS teklifle çalışıyor");
        capabilities.CanDirect.Should().BeFalse("her yük bir teklifin dönüşümü");

        (await client.GetAsync("/api/v1/load")).StatusCode
            .Should().Be(HttpStatusCode.OK);

        var allowed = await client.GetAsync("/api/v1/load_transfer/direct/allowed");
        allowed.EnsureSuccessStatusCode();
        (await allowed.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("allowed").GetBoolean()
            .Should().BeFalse();
    }

    /// <summary>
    /// Süper admin iki şirketi de yönetiyor; ikisine de erişir. Aksi hâlde
    /// yönetim, Avrora'nın yükünü teklifsiz açamaz ya da OLS'in tekliflerini
    /// göremez duruma düşerdi.
    /// </summary>
    [Fact]
    public async Task SuperAdmin_HasBothPaths()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var capabilities = await CapabilitiesAsync(admin);
        capabilities.UsesOffers.Should().BeTrue();
        capabilities.CanDirect.Should().BeTrue();

        (await admin.GetAsync("/api/v1/load")).StatusCode
            .Should().Be(HttpStatusCode.OK);
    }
}
