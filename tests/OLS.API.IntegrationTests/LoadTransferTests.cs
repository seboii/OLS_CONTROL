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
        using var accountForm = await TestAccountHelper.MinimalAccountFormAsync(admin, accountName);
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

    /// <summary>
    /// Regresyon: <c>LoadTransferDetailDto</c> önceden romork_type_id/
    /// instruction_id/delivery_method_id/load_transfer_type_id/way_of_working/
    /// front+final_transportation_by_us/departure+target_country_id/paketlerin
    /// case_type_id'sini HİÇ döndürmüyordu (yazma tarafı destekliyordu) — bu da
    /// formu AÇIP dokunmadan Kaydet'in bu 10 alanı sessizce boşaltmasına yol
    /// açıyordu (canlı Docker'da gerçek bir kayıtta doğrulandı). Bu test tam da
    /// bunu kilitler: hepsi set edilip GET'te dönüp dönmediği tek tek kontrol
    /// edilir — DTO'da biri eksik kalırsa bu test kırılır.
    /// </summary>
    [Fact]
    public async Task UpdateLoadTransfer_SetsAllPreviouslyMissingReadFields_AllRoundTripCorrectly()
    {
        var id = await SeedLoadTransferAsync();
        using var admin = await _factory.CreateAdminClientAsync();

        async Task<long> CreateLookupAsync(string path, string name)
        {
            var response = await admin.PostAsJsonAsync(path, new { name });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("data").GetProperty("id").GetInt64();
        }

        var romorkTypeId = await CreateLookupAsync("/api/v1/romork_type", $"Test Römork {Guid.NewGuid():N}");
        var instructionId = await CreateLookupAsync("/api/v1/instruction", $"Test Talimat {Guid.NewGuid():N}");
        var deliveryMethodId = await CreateLookupAsync("/api/v1/load_transfer_deliver_method", $"Test Teslimat {Guid.NewGuid():N}");
        var loadTransferTypeId = await CreateLookupAsync("/api/v1/load_transfer_type", $"Test Yük Türü {Guid.NewGuid():N}");
        var loadTypeId = await CreateLookupAsync("/api/v1/loading_type", $"Test Yük Tipi {Guid.NewGuid():N}");
        var caseTypeId = await CreateLookupAsync("/api/v1/case_type", $"Test Kap Tipi {Guid.NewGuid():N}");

        Guid departureCountryId, targetCountryId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
            var departure = new Country { Id = Guid.NewGuid(), Name = $"Test Ülke A {Guid.NewGuid():N}" };
            var target = new Country { Id = Guid.NewGuid(), Name = $"Test Ülke B {Guid.NewGuid():N}" };
            db.Countries.AddRange(departure, target);
            await db.SaveChangesAsync();
            departureCountryId = departure.Id;
            targetCountryId = target.Id;
        }

        var updateResponse = await admin.PostAsJsonAsync($"/api/v1/load_transfer/{id}", new
        {
            romork_type_id = romorkTypeId,
            instruction_id = instructionId,
            delivery_method_id = deliveryMethodId,
            load_transfer_type_id = loadTransferTypeId,
            load_type_id = loadTypeId,
            way_of_working = 1,
            front_transportation_by_us = 1,
            final_transportation_by_us = 0,
            departure_country_id = departureCountryId.ToString(),
            target_country_id = targetCountryId.ToString(),
            packages = new[]
            {
                new { quantity = 2, case_type_id = caseTypeId },
            },
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = (await (await admin.GetAsync($"/api/v1/load_transfer/{id}"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        detail.GetProperty("romork_type_id").GetProperty("id").GetInt64().Should().Be(romorkTypeId);
        detail.GetProperty("instruction_id").GetProperty("id").GetInt64().Should().Be(instructionId);
        detail.GetProperty("delivery_method_id").GetProperty("id").GetInt64().Should().Be(deliveryMethodId);
        detail.GetProperty("load_transfer_type_id").GetProperty("id").GetInt64().Should().Be(loadTransferTypeId);
        detail.GetProperty("load_type_id").GetProperty("id").GetInt64().Should().Be(loadTypeId);
        detail.GetProperty("way_of_working").GetInt32().Should().Be(1);
        detail.GetProperty("front_transportation_by_us").GetInt32().Should().Be(1);
        detail.GetProperty("final_transportation_by_us").GetInt32().Should().Be(0);
        detail.GetProperty("departure_country_id").GetProperty("id").GetGuid().Should().Be(departureCountryId);
        detail.GetProperty("target_country_id").GetProperty("id").GetGuid().Should().Be(targetCountryId);
        detail.GetProperty("load_transfer_package")[0].GetProperty("case_type_id").GetProperty("id").GetInt64().Should().Be(caseTypeId);

        // Kritik regresyon: formu AÇIP DOKUNMADAN tekrar Kaydet'e basmak (bu
        // alanları içeren tam gövdeyle geri göndermek) hiçbirini bozmamalı.
        var noOpUpdateResponse = await admin.PostAsJsonAsync($"/api/v1/load_transfer/{id}", new
        {
            romork_type_id = romorkTypeId,
            instruction_id = instructionId,
            delivery_method_id = deliveryMethodId,
            load_transfer_type_id = loadTransferTypeId,
            load_type_id = loadTypeId,
            way_of_working = 1,
            front_transportation_by_us = 1,
            final_transportation_by_us = 0,
            departure_country_id = departureCountryId.ToString(),
            target_country_id = targetCountryId.ToString(),
        });
        noOpUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var detailAfterNoOpSave = (await (await admin.GetAsync($"/api/v1/load_transfer/{id}"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        detailAfterNoOpSave.GetProperty("romork_type_id").GetProperty("id").GetInt64().Should().Be(romorkTypeId);
        detailAfterNoOpSave.GetProperty("way_of_working").GetInt32().Should().Be(1);
        detailAfterNoOpSave.GetProperty("target_country_id").GetProperty("id").GetGuid().Should().Be(targetCountryId);
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
