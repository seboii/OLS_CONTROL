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
/// </summary>
public sealed class OlsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string MaintenanceConnectionString =
        "Host=localhost;Port=5443;Database=postgres;Username=postgres;Password=secret";

    private static readonly string TestDatabaseName =
        $"ols_scoped_inttest_{Guid.NewGuid():N}";

    private static string TestConnectionString =>
        $"Host=localhost;Port=5443;Database={TestDatabaseName};Username=postgres;Password=secret";

    private string? _adminToken;
    private readonly SemaphoreSlim _adminTokenLock = new(1, 1);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = TestConnectionString,
                // Siber (legacy MSSQL) bilinçli olarak tanımsız: testler hiçbir
                // gerçek dış sisteme bağlanmamalı.
                //
                // DİKKAT: Jwt:Key BURADA override EDİLMİYOR — appsettings.Development.json'daki
                // değer kullanılıyor. Sebep: Program.cs, Jwt:Key'i builder.Build() çağrısından
                // ÖNCE bir local değişkene okuyup AddJwtBearer'ın IssuerSigningKey'ine kapatıyor;
                // WebApplicationFactory'nin ConfigureAppConfiguration override'ı ise .Build() bir
                // DiagnosticListener olayıyla ele geçirildiğinde uygulanıyor — yani Program.cs'in
                // ERKEN okuduğu local değişkenden SONRA. Sonuç: JwtTokenService (login'de imzalama,
                // IConfiguration'ı DI üzerinden İSTEK ANINDA okuyor) override'ı görür, ama doğrulama
                // tarafı (erken yakalanan local değişken) GÖRMEZ — iki farklı anahtarla imzalanıp
                // doğrulanmış olur, her token 401 ile reddedilir. Burada override'ı KALDIRMAK,
                // Program.cs'e hiç dokunmadan iki tarafı da AYNI (gerçek dev) anahtara hizalar.
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
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();

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
