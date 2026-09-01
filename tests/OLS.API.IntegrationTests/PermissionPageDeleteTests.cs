using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace OLS.API.IntegrationTests;

/// <summary>
/// Yetki sayfası silme ve onun EN ÖNEMLİ kuralı: programın kullandığı sayfa
/// silinemez.
///
/// Sezgiye ters olan nokta şu — bir yetki sayfasını silmek o modülü kilitlemez,
/// HERKESE AÇAR. <c>PermissionService.HasPermissionAsync</c> bulunamayan slug
/// için <b>true</b> döner ("tanımsız sayfa serbest", olsold RoleHelper
/// davranışı). <c>load_management</c> silinseydi yük ve teklif uçları yetki
/// kontrolü olmadan açılırdı.
///
/// Silme ucu, canlıda takılı kalan gerçek bir artık yüzünden eklendi: sayfa
/// AÇMA ucu vardı ama SİLME yoktu, bu yüzden "test_sayfa_canli" 130
/// kullanıcının 48'inde yetki satırıyla birlikte ekranda duruyordu.
/// </summary>
[Collection("OlsApi")]
public sealed class PermissionPageDeleteTests
{
    private readonly OlsApiFactory _factory;

    public PermissionPageDeleteTests(OlsApiFactory factory) => _factory = factory;

    [Fact]
    public async Task StrayPage_CanBeCreatedAndDeleted()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var slug = $"gecici_sayfa_{Guid.NewGuid():N}";

        var created = await admin.PostAsJsonAsync("/api/v1/permission", new
        {
            permission_page_name = "Geçici Sayfa",
            permission_page_slug = slug,
        });
        created.EnsureSuccessStatusCode();

        var deleted = await admin.DeleteAsync($"/api/v1/permission/{slug}");
        deleted.EnsureSuccessStatusCode();

        // Sayfa artık yetki listesinde görünmemeli.
        var rows = await admin.GetPermissionDataAsync(1);
        rows.EnumerateArray()
            .Select(r => r.GetProperty("permission_page_slug").GetString())
            .Should().NotContain(slug);
    }

    [Theory]
    [InlineData("load_management")]
    [InlineData("super_admin")]
    [InlineData("account_management")]
    public async Task ProgramPage_CannotBeDeleted(string slug)
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.DeleteAsync($"/api/v1/permission/{slug}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "programın kullandığı sayfayı silmek modülü kilitlemez, herkese açar");

        // Sayfa yerinde durmalı.
        var rows = await admin.GetPermissionDataAsync(1);
        rows.EnumerateArray()
            .Select(r => r.GetProperty("permission_page_slug").GetString())
            .Should().Contain(slug);
    }

    /// <summary>
    /// Programın kullandığı HER sayfa yetki ekranında bulunmalı. Eksiği menüde
    /// kayıp modül demek: arayüz bilinmeyen slug'ı reddediyor, yani seed
    /// edilmemiş bir sayfanın modülü kimseye görünmez.
    ///
    /// Ters yön ("fazladan sayfa olmasın") BURADA doğrulanamaz: entegrasyon
    /// testleri veritabanını paylaşıyor ve PermissionEnforcementTests kendi
    /// geçici sayfalarını açıyor. Fazlalık koruması silme testlerinde:
    /// programa ait olmayan sayfa silinebilir, ait olan silinemez.
    /// </summary>
    [Fact]
    public async Task EveryProgramPage_IsSeeded()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var rows = await admin.GetPermissionDataAsync(1);

        var seeded = rows.EnumerateArray()
            .Select(r => r.GetProperty("permission_page_slug").GetString())
            .ToList();

        var expected = OLS.Business.Services.Authorization.PermissionPages.All
            .Select(p => p.Slug);

        seeded.Should().Contain(expected);
    }
}
