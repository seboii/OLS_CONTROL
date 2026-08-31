using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OLS.API.IntegrationTests;

/// <summary>
/// Regresyon: "Yeni Teklif" oluştururken Acente seçicisi ve Alış tarafındaki Kalem
/// seçicisi "Sonuç bulunamadı" gösteriyordu — mock Siber'in şeması bu iki alanı
/// (financial_items.type, Acente tipinde bir cari) hiç taşımadığından, Siber
/// import'u tek başına yeterli değildi (bkz. TESLIM-RAPORU.md, DbSeeder.
/// SeedDemoConvenienceDataAsync). Bu testler, hiçbir Siber import'u ÇALIŞMADAN,
/// yalnızca uygulamanın kendi başlangıç seed'iyle (OlsApiFactory her testte
/// gerçek Program.cs başlangıcını taze bir veritabanına karşı çalıştırır) bu iki
/// seçicinin dolu geldiğini doğrular.
/// </summary>
[Collection("OlsApi")]
public sealed class DbSeederTests
{
    private readonly OlsApiFactory _factory;

    public DbSeederTests(OlsApiFactory factory) => _factory = factory;

    [Fact]
    public async Task FreshDatabase_HasAtLeastOneAcenteAccount()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.GetAsync("/api/v1/account?account_type_id=5");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");

        data.GetArrayLength().Should().BeGreaterThan(0,
            "Teklif'in Taraflar sekmesindeki Acente seçicisi taze bir kurulumda boş görünmemeli");
    }

    [Fact]
    public async Task FreshDatabase_HasAtLeastOneBuyAndOneSellFinancialItem()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var buyResponse = await admin.GetAsync("/api/v1/financial_item?type=1");
        var sellResponse = await admin.GetAsync("/api/v1/financial_item?type=2");
        buyResponse.EnsureSuccessStatusCode();
        sellResponse.EnsureSuccessStatusCode();

        var buyData = (await buyResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data");
        var sellData = (await sellResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data");

        buyData.GetArrayLength().Should().BeGreaterThan(0,
            "Mali Kalemler'de Alış seçiliyken Kalem seçicisi taze bir kurulumda boş görünmemeli");
        sellData.GetArrayLength().Should().BeGreaterThan(0,
            "Mali Kalemler'de Satış seçiliyken Kalem seçicisi taze bir kurulumda boş görünmemeli");
    }

    /// <summary>
    /// Seed ikinci kez (aynı veritabanına) çalışsa da tekrar hesap üretmemeli.
    ///
    /// NOT — mutlak sayı yerine ÖNCESİ/SONRASI farkı ölçülüyor: bu koleksiyondaki
    /// diğer testler ([Collection("OlsApi")] AYNI veritabanını paylaşıyor) kendi
    /// amaçları için Acente tipinde başka hesaplar oluşturabilir (bkz.
    /// AccountFormTests) — bu testin asıl iddiası "ikinci seed çağrısı SIFIR yeni
    /// kayıt eklemeli", "toplam sayı tam olarak 1'dir" değil.
    /// </summary>
    [Fact]
    public async Task RunningSeedTwice_DoesNotCreateDuplicateAcenteAccount()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OLS.DataAccess.Context.OlsDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<OLS.Business.Services.Authentication.IPasswordHasher>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<OlsApiFactory>>();
        var defaultPassword = scope.ServiceProvider
            .GetRequiredService<OLS.Business.Services.Authentication.IDefaultUserPassword>();
        var roleService = scope.ServiceProvider
            .GetRequiredService<OLS.Business.Services.Roles.IRoleService>();

        async Task<int> AcenteAccountCountAsync()
        {
            var acenteTypeId = await db.AccountTypes
                .Where(t => t.Name == "Acente")
                .Select(t => (int?)t.Id)
                .FirstOrDefaultAsync();

            return await db.AccountTypeMappings
                .Where(m => m.AccountTypeId == acenteTypeId)
                .CountAsync();
        }

        await OLS.Business.Seed.DbSeeder.SeedAsync(db, hasher, defaultPassword, roleService, configuration, environment, logger);
        var countAfterFirstSeed = await AcenteAccountCountAsync();

        await OLS.Business.Seed.DbSeeder.SeedAsync(db, hasher, defaultPassword, roleService, configuration, environment, logger);
        var countAfterSecondSeed = await AcenteAccountCountAsync();

        countAfterSecondSeed.Should().Be(countAfterFirstSeed,
            "ikinci seed çağrısı ek bir Acente hesabı üretmemeli — DIĞER testlerin kendi hesapları etkilenmeden");
    }

    [Fact]
    public async Task Seed_SifresizKullaniciyaVarsayilanSifreAtar_MevcutSifreyiEzmez()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OLS.DataAccess.Context.OlsDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<OLS.Business.Services.Authentication.IPasswordHasher>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<OlsApiFactory>>();
        var defaultPassword = scope.ServiceProvider
            .GetRequiredService<OLS.Business.Services.Authentication.IDefaultUserPassword>();
        var roleService = scope.ServiceProvider
            .GetRequiredService<OLS.Business.Services.Roles.IRoleService>();

        // Siber içe aktarımının ürettiği hâl: şifresiz kullanıcı.
        var sifresiz = new OLS.DataAccess.Entities.User
        {
            Name = "Sifresiz", Surname = "Kullanici",
            Email = $"sifresiz-{Guid.NewGuid():N}@ols.local", Status = true,
        };
        var mevcutHash = hasher.Hash("KendiSifresi!42");
        var sifreli = new OLS.DataAccess.Entities.User
        {
            Name = "Sifreli", Surname = "Kullanici",
            Email = $"sifreli-{Guid.NewGuid():N}@ols.local", Status = true,
            Password = mevcutHash,
        };
        db.Users.AddRange(sifresiz, sifreli);
        await db.SaveChangesAsync();

        await OLS.Business.Seed.DbSeeder.SeedAsync(
            db, hasher, defaultPassword, roleService, configuration, environment, logger);

        sifresiz.Password.Should().NotBeNullOrEmpty("şifresiz kullanıcı giriş yapabilmeli");
        hasher.Verify(
            OLS.Business.Services.Authentication.DefaultUserPassword.DevelopmentDefault,
            sifresiz.Password!)
            .Should().BeTrue("varsayılan şifre atanmalı");

        sifreli.Password.Should().Be(mevcutHash,
            "seed mevcut şifreleri ezmemeli — ResetAllPasswords kapalıyken");
    }
}
