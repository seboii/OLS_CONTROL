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

        using var form = await TestAccountHelper.MinimalAccountFormAsync(admin, accountName);
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

    /// <summary>
    /// Okuma yetkisi olan kullanıcı, kendisine cari ATANMAMIŞ olsa bile tüm
    /// carileri görür.
    ///
    /// Eskiden kural tersineydi (yalnızca user_account_mappings ile atanmış
    /// cariler görünürdü) ve bu test 0 bekliyordu. Ancak o eşleme tablosu
    /// canlıda hiç doldurulmuyordu: 7.443 carinin tamamı iki süper admin
    /// dışında kimseye görünmüyor, ekip müşteri listesini boş buluyordu.
    /// Kural kaldırıldı; görünürlük artık yetki sayfasına dayanıyor.
    /// </summary>
    [Fact]
    public async Task RegularUser_WithReadPermission_SeesAccountsWithoutMapping()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountName = $"Baska Musteri {Guid.NewGuid():N}";
        using var form = await TestAccountHelper.MinimalAccountFormAsync(admin, accountName);
        (await admin.PostAsync("/api/v1/account", form)).EnsureSuccessStatusCode();

        var email = $"account-noMapping-{Guid.NewGuid():N}@example.test";
        var userId = await admin.CreateUserAsync(email);
        await admin.GrantPermissionAsync(userId, "account_management", "read");
        // Bilinçli olarak user_account_mappings satırı EKLENMİYOR.

        var token = await _factory.LoginAsync(email, "Test!2026Pw");
        using var client = _factory.CreateAuthorizedClient(token);

        var response = await client.GetAsync(
            $"/api/v1/account?search={Uri.EscapeDataString(accountName)}&per_page=50");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Kendisine atanmamış cari de görünmeli.
        body.GetProperty("data").GetProperty("data").EnumerateArray()
            .Should().Contain(a => a.GetProperty("name").GetString() == accountName,
                "eşlemesi olmayan kullanıcı da carileri görebilmeli");
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

    /// <summary>
    /// BULUNAN GERÇEK HATA — CARİYE TIKLAYINCA HİÇBİR BİLGİ AÇILMIYORDU.
    ///
    /// Liste kuralı daha önce kaldırılmıştı ama DETAY ucu
    /// (<c>GET /api/v1/account/{id}</c>) olsold'dan devralınan nesne seviyesi
    /// kuralı hâlâ uyguluyordu: süper admin değilsen yalnızca
    /// <c>user_account_mappings</c> ile sana atanmış cariyi görebilirsin.
    ///
    /// O tablo canlıda 7.462 cariye karşılık TEK satır taşıyor; yani 48 aktif
    /// kullanıcının 46'sı için uç HER cariden 403 dönüyordu. Kullanıcının
    /// gördüğü: liste ve arama çalışıyor, satıra tıklayınca çekmece
    /// "Müşteri bilgileri yüklenemedi" deyip kapanıyor.
    /// </summary>
    [Fact]
    public async Task RegularUser_WithReadPermission_OpensAccountDetailWithoutMapping()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountName = $"Detay Musteri {Guid.NewGuid():N}"[..30];
        using var form = await TestAccountHelper.MinimalAccountFormAsync(admin, accountName);
        var created = await admin.PostAsync("/api/v1/account", form);
        created.EnsureSuccessStatusCode();
        var accountId = (await created.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();

        var email = $"account-detail-{Guid.NewGuid():N}@example.test";
        var userId = await admin.CreateUserAsync(email);
        await admin.GrantPermissionAsync(userId, "account_management", "read");
        // Bilinçli olarak user_account_mappings satırı EKLENMİYOR.

        var token = await _factory.LoginAsync(email, "Test!2026Pw");
        using var client = _factory.CreateAuthorizedClient(token);

        var response = await client.GetAsync($"/api/v1/account/{accountId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "okuma yetkisi olan kullanıcı cari detayını açabilmeli");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("name").GetString().Should().Be(accountName);
    }

    /// <summary>
    /// Aynı kural cari GEÇMİŞİ ucunda da vardı: geçmiş sekmesi 46 kullanıcıda
    /// hep boş dönüyordu. Yetkisi olan kullanıcı için uç artık 404 DEĞİL.
    /// </summary>
    [Fact]
    public async Task RegularUser_WithReadPermission_ReadsAccountHistoryWithoutMapping()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var accountName = $"Gecmis Musteri {Guid.NewGuid():N}"[..30];
        using var form = await TestAccountHelper.MinimalAccountFormAsync(admin, accountName);
        var created = await admin.PostAsync("/api/v1/account", form);
        created.EnsureSuccessStatusCode();
        var accountId = (await created.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();

        var email = $"account-history-{Guid.NewGuid():N}@example.test";
        var userId = await admin.CreateUserAsync(email);
        await admin.GrantPermissionAsync(userId, "account_management", "read");

        var token = await _factory.LoginAsync(email, "Test!2026Pw");
        using var client = _factory.CreateAuthorizedClient(token);

        var response = await client.GetAsync($"/api/v1/record_history/account/{accountId}/history");

        // Yeni açılan carinin Siber kimliği yoksa uç 404 döner — o ayrı bir
        // durum. Burada aranan, YETKİ yüzünden reddedilmemesi.
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
