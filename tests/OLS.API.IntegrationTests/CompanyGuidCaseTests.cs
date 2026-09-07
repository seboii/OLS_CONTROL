using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OLS.Business.Services.Authorization;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.API.IntegrationTests;

/// <summary>
/// ŞİRKET GUID'İ HARFE DUYARLI KARŞILAŞTIRILMAZ.
///
/// Canlıda bulunan hata: <c>siber_company_id</c> iki farklı yazımla birikmişti.
/// Teklif/yük/sefer BÜYÜK harf (senkron <c>CAST(...)</c> kullanıyor), bütün
/// finans tabloları KÜÇÜK harf (<c>SiberFinanceRepository</c> beş sorgunun
/// hepsinde <c>LOWER(...)</c> yazıyordu) — 343.059 satırın tamamı. Görünürlük
/// süzgeci ise BÜYÜK harfli sabitlerle <c>=</c> karşılaştırması yapıyor ve
/// PostgreSQL bu işleçte harfe DUYARLI.
///
/// Sonuç iki yönde birden yanlıştı: Avrora kullanıcısı kendi finans
/// kayıtlarının hiçbirini göremiyor, OLS kullanıcısı Avrora'nınkilerin
/// tamamını görüyordu.
///
/// Düzeltme üç katmanlı ve testler üçünü de kilitler:
///   1. veri BÜYÜK harfe çekildi (NormalizeSiberCompanyIdCase),
///   2. yazan uçlar kanonik yazımı üretiyor,
///   3. süzgeç <c>upper()</c> ile harfe duyarsız — ileride yeniden küçük harf
///      sızarsa görünürlük yine doğru kalsın.
/// </summary>
[Collection("OlsApi")]
public sealed class CompanyGuidCaseTests
{
    private readonly OlsApiFactory _factory;

    public CompanyGuidCaseTests(OlsApiFactory factory) => _factory = factory;

    /// <summary>Kayıt BİLEREK küçük harfle yazılır — düzeltilen hatanın kendisi.</summary>
    private async Task<string> SeedLowercaseAvroraTransferAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var number = $"HRF{Guid.NewGuid():N}"[..14];

        db.LoadTransfers.Add(new LoadTransfer
        {
            LoadTransferId = Guid.NewGuid().ToString(),
            LoadNumberWorkType = number,
            SiberCompanyId = CompanyScope.AvroraCompanyId.ToLowerInvariant(),
        });

        await db.SaveChangesAsync();

        return number;
    }

    private async Task<HttpClient> CreateClientAsync(string emailDomain)
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var email = $"harf-{Guid.NewGuid():N}{emailDomain}";
        var userId = await admin.CreateUserAsync(email);
        await admin.GrantPermissionAsync(userId, "load_management", "read");

        var token = await _factory.LoginAsync(email, "Test!2026Pw");
        return _factory.CreateAuthorizedClient(token);
    }

    private static async Task<int> TransferTotalAsync(HttpClient client, string number)
    {
        var response = await client.GetAsync($"/api/v1/load_transfer?search={number}&per_page=25");
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("total").GetInt32();
    }

    [Fact]
    public async Task KucukHarfliAvroraKaydi_AvroraKullanicisinaGorunur()
    {
        var number = await SeedLowercaseAvroraTransferAsync();
        using var client = await CreateClientAsync("@avroralog.com");

        (await TransferTotalAsync(client, number)).Should().Be(
            1, "kapsamlı kullanıcı kendi şirketinin kaydını yazım farkı yüzünden kaybetmemeli");
    }

    [Fact]
    public async Task KucukHarfliAvroraKaydi_DigerKullaniciyaGorunmez()
    {
        var number = await SeedLowercaseAvroraTransferAsync();
        using var client = await CreateClientAsync("@example.test");

        (await TransferTotalAsync(client, number)).Should().Be(
            0, "Avrora dışı kullanıcı, yazımı ne olursa olsun Avrora kaydını görmemeli");
    }

    /// <summary>
    /// Şirket seçicisinden küçük harfli bir GUID gelse bile kayıt kanonik
    /// yazımla açılır — sorunun tekrar üremesini engelleyen kural.
    /// </summary>
    [Fact]
    public async Task SirketSecimi_KucukHarfliGuidiKanonikYazimlaDoner()
    {
        using var scope = _factory.Services.CreateScope();
        var companyScope = scope.ServiceProvider.GetRequiredService<ICompanyScope>();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var adminId = await db.Users
            .Where(u => u.Email == "admin@ols-scoped.local")
            .Select(u => u.Id)
            .FirstAsync();

        var resolved = await companyScope.ResolveWriteCompanyAsync(
            adminId, CompanyScope.AvroraCompanyId.ToLowerInvariant());

        resolved.Should().Be(CompanyScope.AvroraCompanyId);
    }
}
