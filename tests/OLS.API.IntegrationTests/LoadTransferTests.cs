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

        var uniqueSuffix = Guid.NewGuid().ToString("N");
        var entity = new LoadTransfer
        {
            // load_transfer_packages / load_transfer_invoice_items bu Siber
            // kimliği (metin) üzerinden bağlanıyor, yerel id üzerinden DEĞİL
            // (bkz. LoadTransferService/LoadTransferUpdateService) — null
            // bırakılırsa aynı anda çalışan başka testlerin/kayıtların
            // null-LoadTransferId satırlarıyla çakışır. Benzersiz bir değer
            // vermek testi izole eder. load_transfer_invoice_items özelinde
            // eşleme insert_name == load_number_work_type üzerinden yapılıyor
            // (bkz. UpsertInvoiceItemsAsync) — o da benzersiz olmalı, aksi hâlde
            // aynı anda çalışan testlerin finans kalemleri birbirine karışır.
            LoadTransferId = $"TEST-{uniqueSuffix}",
            LoadNumber = $"YUK-TEST-{uniqueSuffix}",
            LoadNumberWorkType = $"YUK-TEST-{uniqueSuffix}",
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

        // total_gross_weight/total_volume/total_lademeter artık istekten kabul
        // edilmiyor — LoadTransferController.php satır 874-894'teki gibi paket
        // satırlarından sunucu tarafında yeniden hesaplanıyor (bkz.
        // LoadTransferUpdateService.RecomputeTotalsFromPackagesAsync).
        var updateResponse = await admin.PostAsJsonAsync($"/api/v1/load_transfer/{id}", new
        {
            customer_id = accountId,
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
        detail.GetProperty("total_gross_weight").GetDecimal().Should().Be(85.0m);

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

    /// <summary>
    /// Regresyon: Paketler sekmesinde <c>width/length/height/stackable</c> yazma tarafı
    /// destekliyordu ama form bu 4 alanı hiç RENDER etmiyordu; Finans sekmesinde
    /// <c>status</c> (olsold: pending/invoice_received/invoice_issued) hem formda hem
    /// yazma tarafında (backend her zaman "pending" sabitliyordu) tamamen eksikti.
    /// </summary>
    [Fact]
    public async Task UpdateLoadTransfer_WithFullPackageDimensionsAndInvoiceItemStatus_RoundTripsCorrectly()
    {
        var id = await SeedLoadTransferAsync();
        using var admin = await _factory.CreateAdminClientAsync();

        var updateResponse = await admin.PostAsJsonAsync($"/api/v1/load_transfer/{id}", new
        {
            packages = new[]
            {
                new { quantity = 3, width = 120.5m, length = 240m, height = 80m, stackable = 0 },
            },
            invoice_items = new[]
            {
                new { buysell = "2", quantity = 1, net_price = 100m, total_price = 100m, status = "invoice_received" },
            },
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = (await (await admin.GetAsync($"/api/v1/load_transfer/{id}"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        var package = detail.GetProperty("load_transfer_package")[0];
        package.GetProperty("width").GetDecimal().Should().Be(120.5m);
        package.GetProperty("length").GetDecimal().Should().Be(240m);
        package.GetProperty("height").GetDecimal().Should().Be(80m);
        package.GetProperty("stackable").GetInt32().Should().Be(0);

        var invoiceItem = detail.GetProperty("load_transfer_invoice_item")[0];
        invoiceItem.GetProperty("status").GetString().Should().Be("invoice_received");
    }

    /// <summary>olsold: <c>$item['status'] ?? 'pending'</c> — göndermezse varsayılan "pending".</summary>
    [Fact]
    public async Task UpdateLoadTransfer_WithInvoiceItemWithoutStatus_DefaultsToPending()
    {
        var id = await SeedLoadTransferAsync();
        using var admin = await _factory.CreateAdminClientAsync();

        var updateResponse = await admin.PostAsJsonAsync($"/api/v1/load_transfer/{id}", new
        {
            invoice_items = new[] { new { buysell = "1", quantity = 1 } },
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = (await (await admin.GetAsync($"/api/v1/load_transfer/{id}"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        detail.GetProperty("load_transfer_invoice_item")[0].GetProperty("status").GetString().Should().Be("pending");
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

    /// <summary>
    /// Yük silinince FİNANS KALEMLERİ de silinmeli.
    ///
    /// Gerçek hata buydu: kalemler yüke insert_name = yük numarası metin
    /// eşleşmesiyle bağlı ve silmede temizlenmiyordu. Yük numarası MAX(yukno)+1
    /// ile üretildiği için silinen numara BİR SONRAKİ yüke yeniden veriliyor ve
    /// yeni yük, ölü yükün kalemlerini miras alıyordu — kullanıcı hiç girmediği
    /// "GÜMRÜKLEME GELİRİ" satırlarını görüyordu.
    /// </summary>
    [Fact]
    public async Task DeleteLoadTransfer_FinansKalemleriniDeSiler()
    {
        var id = await SeedLoadTransferAsync();

        string loadNumberWorkType;
        string siberYukId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
            var transfer = await db.LoadTransfers.FindAsync(id);
            loadNumberWorkType = transfer!.LoadNumberWorkType!;
            siberYukId = transfer.LoadTransferId!;

            db.LoadTransferInvoiceItems.Add(new LoadTransferInvoiceItem
            {
                InsertName = loadNumberWorkType,
                Buysell = "1",
                TotalPrice = 500m,
                Status = "pending",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
            });
            db.LoadTransferPackages.Add(new LoadTransferPackage
            {
                LoadTransferId = transfer.LoadTransferId,
                Quantity = 3,
            });
            await db.SaveChangesAsync();
        }

        using var admin = await _factory.CreateAdminClientAsync();
        var response = await admin.SendAsync(new HttpRequestMessage(
            HttpMethod.Delete, "/api/v1/load_transfer")
        {
            Content = JsonContent.Create(new { deletion_id = new[] { id } }),
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

            db.LoadTransferInvoiceItems.Count(i => i.InsertName == loadNumberWorkType)
                .Should().Be(0, "yük silinince finans kalemleri de gitmeli — aksi hâlde " +
                                "yeniden kullanılan yük numarası ölü kalemleri miras alır");

            db.LoadTransferPackages.Count(p => p.LoadTransferId == siberYukId)
                .Should().Be(0, "koliler de yükle birlikte silinmeli");
        }
    }
}
