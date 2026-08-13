using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.API.IntegrationTests;

/// <summary>
/// Yük (LoadTransfer) düzenleme akışı — bu oturumda eklenen ilk gerçek
/// düzenleme UI'sinin backend sözleşmesini kilitler.
///
/// LoadTransfer kayıtları normalde YALNIZCA Siber'e aktarılmış bir teklifin
/// dönüştürülmesiyle oluşur (ConvertOffer) — bu da gerçek Siber-mock'a
/// bağımlı, testte kurulması pahalı bir zincir. Bunun yerine, tam olarak
/// ConvertOffer'ın kendisinin test edildiği bir senaryo DEĞİL burası —
/// yalnızca UPDATE uç noktasının sözleşmesini doğrulamak için, o ön koşulu
/// doğrudan DbContext ile (gerçek EF Core, gerçek Postgres) kuruyoruz.
/// </summary>
[Collection("OlsApi")]
public sealed class LoadTransferTests
{
    private readonly OlsApiFactory _factory;

    public LoadTransferTests(OlsApiFactory factory) => _factory = factory;

    private async Task<long> SeedLoadTransferAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var entity = new LoadTransfer
        {
            // load_transfer_packages / load_transfer_invoice_items bu Siber
            // kimliği (metin) üzerinden bağlanıyor, yerel id üzerinden DEĞİL
            // (bkz. LoadTransferService/LoadTransferUpdateService) — null
            // bırakılırsa aynı anda çalışan başka testlerin/kayıtların
            // null-LoadTransferId satırlarıyla çakışır. Benzersiz bir değer
            // vermek testi izole eder.
            LoadTransferId = $"TEST-{Guid.NewGuid():N}",
            LoadNumber = $"YUK-TEST-{Guid.NewGuid():N}",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.LoadTransfers.Add(entity);
        await db.SaveChangesAsync();
        return entity.Id;
    }

    [Fact]
    public async Task UpdateLoadTransfer_WithCoreFieldsAndPackages_RoundTripsCorrectly()
    {
        var id = await SeedLoadTransferAsync();
        using var admin = await _factory.CreateAdminClientAsync();

        var accountName = $"Yük Test Cari {Guid.NewGuid():N}";
        using var accountForm = new MultipartFormDataContent { { new StringContent(accountName), "name" } };
        var accountResponse = await admin.PostAsync("/api/v1/account", accountForm);
        accountResponse.EnsureSuccessStatusCode();
        var accountId = (await accountResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();

        var updateResponse = await admin.PostAsJsonAsync($"/api/v1/load_transfer/{id}", new
        {
            customer_id = accountId,
            total_gross_weight = 340.25m,
            total_volume = 12.5m,
            packages = new[]
            {
                new { quantity = 4, gross_weight = 85.0m, stackable = 1 },
            },
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var detailResponse = await admin.GetAsync($"/api/v1/load_transfer/{id}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = (await detailResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        detail.GetProperty("customer_id").GetProperty("id").GetInt64().Should().Be(accountId);
        detail.GetProperty("total_gross_weight").GetDecimal().Should().Be(340.25m);

        var packages = detail.GetProperty("load_transfer_package");
        packages.GetArrayLength().Should().Be(1);
        packages[0].GetProperty("quantity").GetInt32().Should().Be(4);
    }

    [Fact]
    public async Task DeletePackage_RemovesItFromSubsequentRead()
    {
        var id = await SeedLoadTransferAsync();
        using var admin = await _factory.CreateAdminClientAsync();

        await admin.PostAsJsonAsync($"/api/v1/load_transfer/{id}", new
        {
            packages = new[] { new { quantity = 1 } },
        });

        var afterCreate = (await (await admin.GetAsync($"/api/v1/load_transfer/{id}"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("load_transfer_package");
        afterCreate.GetArrayLength().Should().Be(1);
        var packageId = afterCreate[0].GetProperty("id").GetInt64();

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/load_transfer/load_transfer_package")
        {
            Content = JsonContent.Create(new { deletion_id = new[] { packageId } }),
        };
        var deleteResponse = await admin.SendAsync(deleteRequest);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterDelete = (await (await admin.GetAsync($"/api/v1/load_transfer/{id}"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("load_transfer_package");
        afterDelete.GetArrayLength().Should().Be(0);
    }
}
