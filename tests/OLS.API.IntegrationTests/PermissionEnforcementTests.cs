using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace OLS.API.IntegrationTests;

/// <summary>
/// RequiresPermissionAttribute'un olsold'daki bilinçli davranış farkını gerçekten
/// uyguladığını doğrular: legacy'de yetki kontrolü etkisizdi (yorum satırı/süslü
/// parantezsiz if), burada yetkisiz istek gerçekten 403 alır.
/// </summary>
[Collection("OlsApi")]
public sealed class PermissionEnforcementTests
{
    private readonly OlsApiFactory _factory;

    public PermissionEnforcementTests(OlsApiFactory factory) => _factory = factory;

    /// <summary>
    /// KAYIT AÇMAK YETKİYE BAĞLI DEĞİL — bilinçli kural. Müşteri / araç /
    /// teklif / yük / sefer oluşturma uçlarında <c>create</c> izni aranmıyor;
    /// yetki sistemi bu sayfalarda okuma, güncelleme ve silme için duruyor.
    /// Eskiden bu test aynı isteğin 403 döndüğünü kilitliyordu.
    /// </summary>
    [Fact]
    public async Task CreateCar_AsFreshUserWithoutCarManagementPermission_IsAllowed()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var email = $"car-noperm-{Guid.NewGuid():N}@example.test";
        await admin.CreateUserAsync(email);
        var token = await _factory.LoginAsync(email, "Test!2026Pw");
        using var client = _factory.CreateAuthorizedClient(token);

        var carPayload = await TestCarHelper.RequiredCarFieldsAsync(admin);
        carPayload["plate_number"] = $"34 AC {Random.Shared.Next(1000, 9999)}";

        var response = await client.PostAsJsonAsync("/api/v1/car", carPayload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Oluşturma açıldı diye yetki kontrolü BÜTÜNÜYLE kalkmadı: aynı sayfada
    /// silme hâlâ yetki istiyor. Mekanizmanın çalışmaya devam ettiğini kilitler.
    /// </summary>
    [Fact]
    public async Task DeleteCar_AsFreshUserWithoutCarManagementPermission_Returns403()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var email = $"car-nodelete-{Guid.NewGuid():N}@example.test";
        await admin.CreateUserAsync(email);
        var token = await _factory.LoginAsync(email, "Test!2026Pw");
        using var client = _factory.CreateAuthorizedClient(token);

        // Silme ucu gövde bekliyor (DELETE /api/v1/car + deletion_id), ama yetki
        // süzgeci model bağlamadan ÖNCE çalıştığı için içerik önemsiz.
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/car")
        {
            Content = JsonContent.Create(new { deletion_id = new[] { 1 } }),
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateCar_AfterGrantingCreatePermission_Succeeds()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var email = $"car-withperm-{Guid.NewGuid():N}@example.test";
        var userId = await admin.CreateUserAsync(email);
        await admin.GrantPermissionAsync(userId, "car_management", "create");
        await admin.GrantPermissionAsync(userId, "car_management", "read");

        var token = await _factory.LoginAsync(email, "Test!2026Pw");
        using var client = _factory.CreateAuthorizedClient(token);

        var plate = $"34 GR {Random.Shared.Next(1000, 9999)}";
        var carPayload = await TestCarHelper.RequiredCarFieldsAsync(admin);
        carPayload["plate_number"] = plate;
        var createResponse = await client.PostAsJsonAsync("/api/v1/car", carPayload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // per_page verilmezse CarService de (AccountService gibi) ToPagedOrListAsync
        // üzerinden çıplak dizi döner, LengthAwarePaginator değil.
        var listResponse = await client.GetAsync($"/api/v1/car?search={Uri.EscapeDataString(plate)}&per_page=50");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("data").EnumerateArray()
            .Should().Contain(car => car.GetProperty("plate_number").GetString() == plate);
    }

    [Fact]
    public async Task CreateCar_WithoutPlateNumber_ReturnsValidationError_NotServerError()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.PostAsJsonAsync("/api/v1/car", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").GetProperty("plate_number").GetArrayLength().Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Regresyon: olsold: <c>Front\Permission\PermissionController::save</c> hiç yetki
    /// kontrolü yapmıyordu (yalnızca giriş yapmış olmak yeterliydi) — ama bu uç TÜM
    /// kullanıcıların yetki setini toplu değiştiren bir yan etki taşıyor
    /// (docs/API-PARITE-MATRISI.md bunun için en azından role_management(create)
    /// planlamıştı). docs/SECILI-MODUL-PARITE-MATRISI.md ile çelişen bu notu
    /// uzlaştırırken bulundu, burada kilitleniyor.
    /// </summary>
    [Fact]
    public async Task CreatePermissionPage_AsFreshUserWithoutRoleManagementPermission_Returns403()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var email = $"permpage-noperm-{Guid.NewGuid():N}@example.test";
        await admin.CreateUserAsync(email);
        var token = await _factory.LoginAsync(email, "Test!2026Pw");
        using var client = _factory.CreateAuthorizedClient(token);

        var response = await client.PostAsJsonAsync("/api/v1/permission", new
        {
            permission_page_name = $"Test Sayfası {Guid.NewGuid():N}",
            permission_page_slug = $"test_page_{Guid.NewGuid():N}",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreatePermissionPage_AfterGrantingRoleManagementCreatePermission_Succeeds()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var email = $"permpage-withperm-{Guid.NewGuid():N}@example.test";
        var userId = await admin.CreateUserAsync(email);
        await admin.GrantPermissionAsync(userId, "role_management", "create");

        var token = await _factory.LoginAsync(email, "Test!2026Pw");
        using var client = _factory.CreateAuthorizedClient(token);

        var response = await client.PostAsJsonAsync("/api/v1/permission", new
        {
            permission_page_name = $"Test Sayfası {Guid.NewGuid():N}",
            permission_page_slug = $"test_page_{Guid.NewGuid():N}",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnknownPermissionSlug_IsOpenByDefault_MatchingLegacyRoleHelperBehavior()
    {
        // PermissionService.HasPermissionAsync: slug user_permission_pages'te yoksa
        // serbest bırakılır (RoleHelper'ın olsold davranışı). Burada gerçekten
        // seed edilmemiş bir sayfaya karşı [RequiresPermission] kullanan bir uç
        // yok elimizde; bu yüzden servisi doğrudan DI konteynerinden çözüp test
        // ediyoruz (gerçek DB'ye karşı, gerçek sorgu).
        using var scope = _factory.Services.CreateScope();
        var permissionService = scope.ServiceProvider
            .GetRequiredService<OLS.Business.Services.Authorization.IPermissionService>();

        var allowed = await permissionService.HasPermissionAsync(
            userId: 999_999_999,
            pageSlug: $"tanimsiz-sayfa-{Guid.NewGuid():N}",
            action: OLS.Business.Services.Authorization.PermissionAction.Read);

        allowed.Should().BeTrue();
    }
}
