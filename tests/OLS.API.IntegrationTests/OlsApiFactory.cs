using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace OLS.API.IntegrationTests;

/// <summary>
/// Tam ASP.NET Core pipeline'ı (JWT auth, [RequiresPermission] filtreleri, EF Core
/// migrasyonları, DbSeeder) — Program.cs'in kendi başlangıç kodu üzerinden çalışır,
/// hiçbir katman mock'lanmaz.
///
/// NOT — Testcontainers.PostgreSql BİLİNÇLİ OLARAK KULLANILMIYOR: bu geliştirme
/// makinesinde (Docker Desktop + WSL2) Testcontainers'ın dinamik host portları,
/// gerçek "docker compose" Postgres konteynerine (ols-scoped-postgres, 5443) yanlış
/// yönlendiriliyor — Testcontainers "taze" bir konteyner oluşturduğunu bildiriyor
/// (her seferinde farklı container id ve farklı host portu), ama içeriden
/// pg_postmaster_start_time() sorgulandığında HER SEFERİNDE ols-scoped-postgres'in
/// gerçek başlangıç zamanı dönüyor ve "ols_scoped" (test'in istediği "ols_scoped_test"
/// DEĞİL) veritabanına bağlanılıyor. Bu ortamda bu şekilde doğrulandı (bkz.
/// TEST-RAPORU.md "Bilinen Ortam Sorunları"). Bu yüzden burada, AYNI (doğrulanmış
/// çalışan) Postgres'e, ayrı/izole bir veritabanı adıyla bağlanılıyor — gerçek
/// migrasyon + gerçek seed + gerçek HTTP pipeline'ı korunuyor, yalnızca izolasyon
/// mekanizması değişiyor.
///
/// NOT 2 — İZOLASYON ÇEVRE DEĞİŞKENİYLE YAPILIYOR, ConfigureAppConfiguration İLE DEĞİL:
/// OLS.DataAccess/DependencyInjection.cs'teki AddDataAccess, "ConnectionStrings:Postgres"'i
/// AddDbContext'in options lambda'sı İÇİNDE değil, lambda'dan ÖNCE bir yerel değişkene
/// okuyup lambda'yı bu değişkenin closure'ıyla kaydediyor — bu okuma Program.cs'in üst
/// seviye kodu çalışırken, yani builder.Build() ÇAĞRILMADAN ÖNCE olur. WebApplicationFactory'nin
/// ConfigureAppConfiguration override'ı ise .Build() bir DiagnosticListener olayıyla
/// yakalandığında devreye girer — yani bu erken okumadan SONRA. Sonuç: ConfigureAppConfiguration
/// ile verilen bir "ConnectionStrings:Postgres" override'ı SESSİZCE YOK SAYILIR ve uygulama
/// appsettings.Development.json'daki GERÇEK dev veritabanına (ols_scoped) bağlanmaya devam
/// eder — Jwt:Key'de yaşanan sorunla birebir aynı kök neden (bkz. NOT 3), ama bu kez fark
/// edilmesi çok daha geç oldu: testler "geçiyor" göründü çünkü gerçek dev admin zaten
/// (ayrı bir düzeltmeyle) doğru şifreyle çalışıyordu — sessizce gerçek dev veritabanına
/// yazıp okuyorlardı. Ortam değişkenleri ise WebApplication.CreateBuilder(args)'ın KENDİ
/// normal yapılandırma zincirinin bir parçasıdır (appsettings + env var'lar hep birlikte,
/// Program.cs'in ilk satırında) — bu yüzden erken okuma da bu değeri GÖRÜR. Çözüm burada:
/// override'ı ConfigureAppConfiguration yerine, host hiç kurulmadan önce
/// Environment.SetEnvironmentVariable ile veriyoruz.
/// </summary>
public sealed class OlsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Bağlantı bilgisi ORTAM DEĞİŞKENİNDEN okunur, varsayılanı localhost:5443.
    //
    // Testler iki yerden çalışabiliyor:
    //   * ana bilgisayardan  -> localhost:5443 (dev override'ı bu portu açar)
    //   * Docker içinden     -> postgres:5432  (hiç port açmaya gerek yok)
    //
    // Varsayılanların korunması şart: sabit kodlanmış hâliyle çalışan mevcut
    // geliştirici akışı (dotnet test) hiçbir ayar yapmadan çalışmaya devam etsin.
    private static string DbHost =>
        Environment.GetEnvironmentVariable("TEST_DB_HOST") ?? "localhost";

    private static string DbPort =>
        Environment.GetEnvironmentVariable("TEST_DB_PORT") ?? "5443";

    private static string DbUser =>
        Environment.GetEnvironmentVariable("TEST_DB_USERNAME") ?? "postgres";

    private static string DbPassword =>
        Environment.GetEnvironmentVariable("TEST_DB_PASSWORD") ?? "secret";

    /// <summary>
    /// Test veritabanını AÇIP SİLMEK için kullanılan bağlantı. Bakım işlemleri
    /// silinecek veritabanına bağlıyken yapılamaz, bu yüzden "postgres"e bağlanır.
    /// </summary>
    private static string MaintenanceConnectionString =>
        $"Host={DbHost};Port={DbPort};Database=postgres;Username={DbUser};Password={DbPassword}";

    private static readonly string TestDatabaseName =
        $"ols_scoped_inttest_{Guid.NewGuid():N}";

    private static string TestConnectionString =>
        $"Host={DbHost};Port={DbPort};Database={TestDatabaseName};Username={DbUser};Password={DbPassword}";

    private string? _adminToken;
    private readonly SemaphoreSlim _adminTokenLock = new(1, 1);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // DİKKAT: Jwt:Key BURADA override EDİLMİYOR — appsettings.Development.json'daki
        // değer kullanılıyor. AddDataAccess ile birebir aynı erken-okuma tuzağı Program.cs'in
        // Jwt:Key okuyan satırında da var (bkz. yukarıdaki sınıf yorumu NOT 2); JwtTokenService
        // (imzalama, DI üzerinden İSTEK ANINDA okur) override'ı görür ama doğrulama tarafı
        // (erken yakalanan yerel değişken) görmez — iki farklı anahtar, her jeton 401.
        // Override'ı KALDIRMAK Program.cs'e dokunmadan iki tarafı da AYNI (gerçek dev)
        // anahtara hizalıyor. Jwt:Key'in aksine ConnectionStrings:Postgres için "hiç override
        // etme" seçeneği YOK (gerçek dev veritabanını kirletmemek gerekiyor) — o yüzden onun
        // için ortam değişkeni yolu kullanılıyor, bkz. InitializeAsync.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seed:AdminEmail"] = "admin@ols-scoped.local",
                ["Seed:AdminPassword"] = "ChangeMe!Dev1",
            });
        });
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await using var connection = new NpgsqlConnection(MaintenanceConnectionString);
        await connection.OpenAsync();

        await using (var create = connection.CreateCommand())
        {
            create.CommandText = $"CREATE DATABASE \"{TestDatabaseName}\"";
            await create.ExecuteNonQueryAsync();
        }

        // Program.cs, ConnectionStrings:Postgres'i builder.Build()'dan ÖNCE bir yerel
        // değişkene okuyor (bkz. sınıf yorumu NOT 2) — bu yüzden ConfigureAppConfiguration
        // ile verilen bir override'ı asla göremez. Ortam değişkeni ise
        // WebApplication.CreateBuilder(args)'ın kendi ilk yapılandırma taramasının bir
        // parçası olduğundan bu erken okumadan da GÖRÜLÜR. Bu satır, bu fixture'dan ilk kez
        // host kurulmadan (ilk CreateClient/Services erişiminden) ÖNCE, InitializeAsync
        // içinde (xUnit'in ilk testten önce garanti ettiği tek yer) çalıştırılmalı.
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", TestConnectionString);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", null);

        await using var connection = new NpgsqlConnection(MaintenanceConnectionString);
        await connection.OpenAsync();

        await using var drop = connection.CreateCommand();
        // WITH (FORCE): Npgsql bağlantı havuzu testler bitince bile bağlantıyı hemen
        // kapatmayabilir; FORCE aktif bağlantı varken de DROP'un başarılı olmasını sağlar.
        drop.CommandText = $"DROP DATABASE IF EXISTS \"{TestDatabaseName}\" WITH (FORCE)";
        await drop.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Seed edilen dev admin ile giriş yapar ve jeton döner. Rate limiter'ın "auth"
    /// politikası dakikada 10 istekle sınırlı olduğundan jeton bir kez alınıp
    /// tüm testler arasında paylaşılır (her test kendi login isteği atmaz).
    /// </summary>
    public async Task<string> GetAdminTokenAsync()
    {
        if (_adminToken is not null)
            return _adminToken;

        await _adminTokenLock.WaitAsync();
        try
        {
            _adminToken ??= await LoginAsync("admin@ols-scoped.local", "ChangeMe!Dev1");
            return _adminToken;
        }
        finally
        {
            _adminTokenLock.Release();
        }
    }

    public async Task<string> LoginAsync(string email, string password)
    {
        using var client = CreateClient();

        // "auth" politikası SABİT PENCERELİ ve dakikada 10 istekle sınırlı (SEC-009);
        // bu koleksiyondaki testlerin toplam gerçek login sayısı pencereye göre bazen
        // taşabiliyor. Sabit pencere olduğu için kısa aralıklarla yeniden denemek işe
        // yaramaz (pencere sınırına rastlamadıkça hep 429 döner) — pencere uzunluğunu
        // (60sn) aşan TEK bir bekleyiş, bir sonraki pencereye geçişi garanti eder.
        // Limiti gevşetmiyoruz, testi ona dayanıklı yapıyoruz.
        var response = await client.PostAsJsonAsync("/api/v1/login", new { email, password });

        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            await Task.Delay(TimeSpan.FromSeconds(65));
            response = await client.PostAsJsonAsync("/api/v1/login", new { email, password });
        }

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("token").GetString()
            ?? throw new InvalidOperationException("Giriş yanıtında token yok.");
    }

    public HttpClient CreateAuthorizedClient(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task<HttpClient> CreateAdminClientAsync()
        => CreateAuthorizedClient(await GetAdminTokenAsync());
}
