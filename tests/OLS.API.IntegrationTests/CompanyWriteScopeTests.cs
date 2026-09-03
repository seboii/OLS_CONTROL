using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OLS.Business.Services.Authorization;

namespace OLS.API.IntegrationTests;

/// <summary>
/// YENİ KAYIT HANGİ ŞİRKETE YAZILIR.
///
/// Yük, sefer, araç ve cari açma akışlarının hepsi
/// <c>ICompanyScope.ResolveWriteCompanyAsync</c>'ten geçer. Kural üç kademeli:
///
///   1. Tek şirkete bağlı kullanıcı (Avrora ekibi) DAİMA kendi şirketine yazar;
///      istekle gelen değer YOK SAYILIR. Aksi hâlde kullanıcı, görünürlük
///      kuralı gereği kendi listesinde göremeyeceği bir kayıt açabilirdi.
///   2. İki şirketi de gören kullanıcı (süper admin) seçebilir.
///   3. Kalan herkes OLS.
///
/// Canlıda bulunan hatanın kilidi: sefer/yük deposu şirketi SABİT yazıyordu ve
/// Avrora kullanıcısının açtığı kayıt OLS'e düşüp kendi listesinden
/// kayboluyordu.
/// </summary>
[Collection("OlsApi")]
public sealed class CompanyWriteScopeTests
{
    private const string Avrora = "46258A01-8D77-4F87-AAF5-6B331DEDD8A7";
    private const string Ols = "BA4888B1-A2B0-4142-B273-92481D932EAD";

    private readonly OlsApiFactory _factory;

    public CompanyWriteScopeTests(OlsApiFactory factory) => _factory = factory;

    private async Task<long> CreateUserAsync(string email)
    {
        using var admin = await _factory.CreateAdminClientAsync();
        return await admin.CreateUserAsync(email);
    }

    private async Task<string> ResolveAsync(long userId, string? requested)
    {
        using var scope = _factory.Services.CreateScope();
        var companyScope = scope.ServiceProvider.GetRequiredService<ICompanyScope>();
        return await companyScope.ResolveWriteCompanyAsync(userId, requested);
    }

    /// <summary>
    /// Avrora kullanıcısı OLS'i İSTESE BİLE kendi şirketine yazar — sunucu
    /// istemciden gelen şirkete güvenmez.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData(Ols)]
    [InlineData("gecersiz-deger")]
    public async Task ScopedUser_AlwaysWritesToOwnCompany(string? requested)
    {
        var userId = await CreateUserAsync($"kapsam-{Guid.NewGuid():N}@avroralog.com");

        (await ResolveAsync(userId, requested)).Should().BeEquivalentTo(Avrora);
    }

    /// <summary>Kapsamı olmayan kullanıcı OLS'e yazar ve seçim yapamaz.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData(Avrora)]
    public async Task UnscopedUser_AlwaysWritesToOls(string? requested)
    {
        var userId = await CreateUserAsync($"kapsamsiz-{Guid.NewGuid():N}@example.test");

        (await ResolveAsync(userId, requested)).Should().BeEquivalentTo(Ols);
    }

    /// <summary>
    /// İki şirketi de gören kullanıcı seçebilir; tanımsız değer OLS'e düşer.
    /// </summary>
    [Fact]
    public async Task SuperAdmin_CanChooseCompany()
    {
        using var scope = _factory.Services.CreateScope();
        var companyScope = scope.ServiceProvider.GetRequiredService<ICompanyScope>();

        using var db = _factory.Services.CreateScope();
        var context = db.ServiceProvider.GetRequiredService<OLS.DataAccess.Context.OlsDbContext>();
        var adminId = await context.Users
            .Where(u => u.Email == "admin@ols-scoped.local")
            .Select(u => u.Id)
            .FirstAsync();

        (await companyScope.ResolveWriteCompanyAsync(adminId, Avrora)).Should().BeEquivalentTo(Avrora);
        (await companyScope.ResolveWriteCompanyAsync(adminId, Ols)).Should().BeEquivalentTo(Ols);
        (await companyScope.ResolveWriteCompanyAsync(adminId, "yok-boyle-sirket"))
            .Should().BeEquivalentTo(Ols, "tanımsız değer sessizce varsayılana düşer");
        (await companyScope.ResolveWriteCompanyAsync(adminId, null)).Should().BeEquivalentTo(Ols);
    }

    /// <summary>
    /// Arayüz seçiciyi <c>can_choose_company</c> ile açıp kapatıyor; uç bunu
    /// doğru raporlamalı, yoksa süper admin seçim ekranını hiç görmez.
    /// </summary>
    [Fact]
    public async Task Capabilities_ReportsCompanyChoice()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.GetAsync("/api/v1/capabilities");
        response.EnsureSuccessStatusCode();

        var data = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        data.GetProperty("can_choose_company").GetBoolean()
            .Should().BeTrue("süper admin iki şirketi de yönetiyor");
        data.GetProperty("companies").GetArrayLength()
            .Should().Be(2, "sbr_sirket'te tam olarak iki şirket var");
    }

    /// <summary>
    /// MEVCUT KAYDI TAŞIMA da aynı kapıdan geçer: yük ve sefer güncelleme
    /// akışları şirketi yalnızca <c>CanChooseCompanyAsync</c> true iken
    /// değiştirir. Kapsamlı kullanıcıda false olmalı, yoksa her kaydetmesi
    /// kaydı sessizce kendi şirketine çekerdi.
    /// </summary>
    [Fact]
    public async Task OnlyUsersSeeingBothCompanies_MayMoveRecords()
    {
        using var scope = _factory.Services.CreateScope();
        var companyScope = scope.ServiceProvider.GetRequiredService<ICompanyScope>();
        var context = scope.ServiceProvider.GetRequiredService<OLS.DataAccess.Context.OlsDbContext>();

        var adminId = await context.Users
            .Where(u => u.Email == "admin@ols-scoped.local")
            .Select(u => u.Id)
            .FirstAsync();

        (await companyScope.CanChooseCompanyAsync(adminId))
            .Should().BeTrue("süper admin kaydı iki şirket arasında taşıyabilir");

        var scopedId = await CreateUserAsync($"tasima-{Guid.NewGuid():N}@avroralog.com");
        (await companyScope.CanChooseCompanyAsync(scopedId))
            .Should().BeFalse("kapsamlı kullanıcı kaydı taşıyamaz");

        var plainId = await CreateUserAsync($"tasima-{Guid.NewGuid():N}@example.test");
        (await companyScope.CanChooseCompanyAsync(plainId))
            .Should().BeFalse("kapsamsız kullanıcı da taşıyamaz");
    }

    [Fact]
    public async Task Capabilities_HidesCompanyChoice_ForScopedUser()
    {
        var email = $"kapsam-yetenek-{Guid.NewGuid():N}@avroralog.com";
        await CreateUserAsync(email);

        var token = await _factory.LoginAsync(email, "Test!2026Pw");
        using var client = _factory.CreateAuthorizedClient(token);

        var response = await client.GetAsync("/api/v1/capabilities");
        response.EnsureSuccessStatusCode();

        var data = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        data.GetProperty("can_choose_company").GetBoolean()
            .Should().BeFalse("Avrora kullanıcısı kendi şirketine yazar, seçemez");
        data.GetProperty("companies").GetArrayLength().Should().Be(1);
    }
}
