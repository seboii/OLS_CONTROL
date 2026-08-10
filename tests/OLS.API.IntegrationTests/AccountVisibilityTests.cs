using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace OLS.API.IntegrationTests;

/// <summary>
/// Regresyon testi: bu oturumda canlı olarak bulunup düzeltilen gerçek bir hatayı
/// kilitler. AccountService.IsSuperAdminAsync, "super_admin" slug'lı ve Read=1 olan
/// bir user_permission_pages satırı arar. Bu sayfa seed edilmediğinde HİÇBİR
/// kullanıcı (admin dahil) süper admin sayılmıyordu ve account_management/read
/// yetkisi olan ama user_account_mappings eşlemesi olmayan bir kullanıcı, var olan
/// hiçbir cariyi göremiyordu — yeni oluşturulan cariler dahil (bkz. DbSeeder.cs
/// "super_admin" sayfası ve bu değişikliği ekleyen commit).
/// </summary>
[Collection("OlsApi")]
public sealed class AccountVisibilityTests
{
    private readonly OlsApiFactory _factory;

    public AccountVisibilityTests(OlsApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Admin_IsSuperAdmin_AndSeesAccountsWithoutExplicitMapping()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var accountName = $"Test Lojistik {Guid.NewGuid():N}";

        using var form = new MultipartFormDataContent { { new StringContent(accountName), "name" } };
        var createResponse = await admin.PostAsync("/api/v1/account", form);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Admin hiçbir user_account_mappings satırı olmadan (süper admin olduğu için)
        // yeni oluşturduğu cariyi listede görmeli.
        // NOT: per_page verilmezse AccountService.ListAsync -> ToPagedOrListAsync
        // LengthAwarePaginator DEĞİL, çıplak dizi döner (olsold'un
        // "$request->has('per_page') ? paginate() : get()" birebir karşılığı,
        // 13 serviste ortak — bkz. QueryableExtensions.ToPagedOrListAsync).
        var listResponse = await admin.GetAsync(
            $"/api/v1/account?search={Uri.EscapeDataString(accountName)}&per_page=50");
        var body = await listResponse.Content.ReadFromJsonAsync<JsonElement>();

        body.GetProperty("data").GetProperty("total").GetInt32().Should().BeGreaterThanOrEqualTo(1);
        body.GetProperty("data").GetProperty("data").EnumerateArray()
            .Should().Contain(a => a.GetProperty("name").GetString() == accountName);
    }

    [Fact]
    public async Task RegularUser_WithReadPermissionButNoAccountMapping_SeesNoAccounts()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        // Görünür bir kontrol noktası: admin'e göre en az bir cari var (üstteki test
        // veya bu testin kendisi tarafından oluşturulmuş olabilir; miktar önemli değil,
        // önemli olan aşağıdaki kullanıcının 0 görmesi).
        var accountName = $"Baska Musteri {Guid.NewGuid():N}";
        using var form = new MultipartFormDataContent { { new StringContent(accountName), "name" } };
        (await admin.PostAsync("/api/v1/account", form)).EnsureSuccessStatusCode();

        var email = $"account-noMapping-{Guid.NewGuid():N}@example.test";
        var userId = await admin.CreateUserAsync(email);
        await admin.GrantPermissionAsync(userId, "account_management", "read");
        // Bilinçli olarak user_account_mappings satırı EKLENMİYOR.

        var token = await _factory.LoginAsync(email, "Test!2026Pw");
        using var client = _factory.CreateAuthorizedClient(token);

        var response = await client.GetAsync("/api/v1/account?per_page=50");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("total").GetInt32().Should().Be(0,
            "eşlemesiz kullanıcı süper admin olmadığı sürece hiçbir cari görmemeli");
    }

    [Fact]
    public async Task RegularUser_WithoutAccountManagementPermission_Returns403OnList()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var email = $"account-noperm-{Guid.NewGuid():N}@example.test";
        await admin.CreateUserAsync(email);
        var token = await _factory.LoginAsync(email, "Test!2026Pw");
        using var client = _factory.CreateAuthorizedClient(token);

        var response = await client.GetAsync("/api/v1/account");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
