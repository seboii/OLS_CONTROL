using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.API.IntegrationTests;

/// <summary>
/// Fatura — Kalemler (load_transfer_invoice_maps) ve Dipnotlar (invoice_footers)
/// sekmelerinin backend sözleşmesini kilitler.
/// </summary>
[Collection("OlsApi")]
public sealed class InvoiceTests
{
    private readonly OlsApiFactory _factory;

    public InvoiceTests(OlsApiFactory factory) => _factory = factory;

    private static async Task<long> CreateAccountAsync(HttpClient admin)
    {
        var name = $"Fatura Test Cari {Guid.NewGuid():N}";
        using var form = new MultipartFormDataContent { { new StringContent(name), "name" } };
        var response = await admin.PostAsync("/api/v1/account", form);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();
    }

    private async Task<long> CreateInvoiceAsync(HttpClient admin, long accountId)
    {
        using var form = new MultipartFormDataContent
        {
            { new StringContent("1"), "box_type" },
            { new StringContent("0"), "commercial_type" },
            { new StringContent(accountId.ToString()), "account_id" },
            { new StringContent("1"), "invoice_type_id" },
            { new StringContent("2026-08-13"), "invoice_create_date" },
            { new StringContent("2026-09-13"), "invoice_execution_date" },
        };
        var response = await admin.PostAsync("/api/v1/invoice", form);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();
    }

    /// <summary>Canlıda bulunan gerçek bir hatanın regresyon testi (bkz. TESLIM-RAPORU.md).</summary>
    [Fact]
    public async Task CreateInvoice_WithoutExecutionDate_ReturnsValidationError()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var accountId = await CreateAccountAsync(admin);

        using var form = new MultipartFormDataContent
        {
            { new StringContent("1"), "box_type" },
            { new StringContent("0"), "commercial_type" },
            { new StringContent(accountId.ToString()), "account_id" },
            { new StringContent("1"), "invoice_type_id" },
            { new StringContent("2026-08-13"), "invoice_create_date" },
        };
        var response = await admin.PostAsync("/api/v1/invoice", form);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").GetProperty("invoice_execution_date").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateInvoice_WithItemMap_LinksItemAndFlipsItsStatus()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var accountId = await CreateAccountAsync(admin);
        var invoiceId = await CreateInvoiceAsync(admin, accountId);

        long loadTransferId;
        long invoiceItemId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

            var loadNumberWorkType = $"YUK-TEST-{Guid.NewGuid():N}";
            var transfer = new LoadTransfer
            {
                LoadTransferId = $"TEST-{Guid.NewGuid():N}",
                LoadNumberWorkType = loadNumberWorkType,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
            };
            db.LoadTransfers.Add(transfer);
            await db.SaveChangesAsync();
            loadTransferId = transfer.Id;

            // insert_name, LoadTransfer'ın load_number_work_type'ıyla EŞLEŞMELİ —
            // InvoiceItemService bu ikisini metin eşleşmesiyle bağlıyor (bkz. servis yorumu).
            var item = new LoadTransferInvoiceItem
            {
                InsertName = loadNumberWorkType,
                Buysell = "2", // satış -> güncelleme sonrası "invoice_issued" beklenir
                NetPrice = 1000m,
                TotalPrice = 1180m,
                Status = "pending",
                CreatedAt = DateTime.Now,
            };
            db.LoadTransferInvoiceItems.Add(item);
            await db.SaveChangesAsync();
            invoiceItemId = item.Id;
        }

        using var updateForm = new MultipartFormDataContent
        {
            { new StringContent("1"), "box_type" },
            { new StringContent("0"), "commercial_type" },
            { new StringContent(accountId.ToString()), "account_id" },
            { new StringContent("1"), "invoice_type_id" },
            { new StringContent("2026-08-13"), "invoice_create_date" },
            { new StringContent("2026-09-13"), "invoice_execution_date" },
            { new StringContent(invoiceId.ToString()), "id" },
            { new StringContent(loadTransferId.ToString()), "load_transfer_invoice_maps[0][load_transfer_id]" },
            { new StringContent(invoiceItemId.ToString()), "load_transfer_invoice_maps[0][invoice_item_id]" },
        };
        var updateResponse = await admin.PostAsync("/api/v1/invoice/update", updateForm);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = (await (await admin.GetAsync($"/api/v1/invoice/{invoiceId}"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        var maps = detail.GetProperty("load_transfer_invoice_maps");
        maps.GetArrayLength().Should().Be(1);
        maps[0].GetProperty("load_transfer_id").GetInt64().Should().Be(loadTransferId);
        maps[0].GetProperty("invoice_item_id").GetInt64().Should().Be(invoiceItemId);

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<OlsDbContext>();
        var refreshedItem = await db2.LoadTransferInvoiceItems.FindAsync(invoiceItemId);
        refreshedItem!.Status.Should().Be("invoice_issued");

        // Eşlemeler her güncellemede BAŞTAN kurulur — boş liste gönderilirse hepsi silinmeli.
        using var clearForm = new MultipartFormDataContent
        {
            { new StringContent("1"), "box_type" },
            { new StringContent("0"), "commercial_type" },
            { new StringContent(accountId.ToString()), "account_id" },
            { new StringContent("1"), "invoice_type_id" },
            { new StringContent("2026-08-13"), "invoice_create_date" },
            { new StringContent("2026-09-13"), "invoice_execution_date" },
            { new StringContent(invoiceId.ToString()), "id" },
        };
        var clearResponse = await admin.PostAsync("/api/v1/invoice/update", clearForm);
        clearResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterClear = (await (await admin.GetAsync($"/api/v1/invoice/{invoiceId}"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("load_transfer_invoice_maps");
        afterClear.GetArrayLength().Should().Be(0);
    }

    /// <summary>
    /// Regresyon: "Faturaya Kalem Ekle" seçicisi (frontend) önce status/buysell
    /// filtresi hiç göndermiyordu — bu yüzden zaten BAŞKA bir faturaya bağlı
    /// (status != pending) veya yanlış yönde (buysell uyuşmayan) kalemler de
    /// listede görünüyordu; bir kalemi iki kez faturalamak mümkündü. Backend
    /// (LoadTransferInvoiceItemController/Service) bu filtreleri zaten
    /// destekliyordu — burada uçtan uca doğruluğu kilitleniyor.
    /// </summary>
    [Fact]
    public async Task ListInvoiceItems_FiltersByStatusAndBuysell_ExcludesNonMatchingItems()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        long pendingSellId, pendingBuyId, issuedSellId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

            LoadTransferInvoiceItem NewItem(string buysell, string status) => new()
            {
                InsertName = $"TEST-{Guid.NewGuid():N}",
                Buysell = buysell,
                Status = status,
                NetPrice = 100m,
                TotalPrice = 118m,
                CreatedAt = DateTime.Now,
            };

            var pendingSell = NewItem("2", "pending");
            var pendingBuy = NewItem("1", "pending");
            var issuedSell = NewItem("2", "invoice_issued");
            db.LoadTransferInvoiceItems.AddRange(pendingSell, pendingBuy, issuedSell);
            await db.SaveChangesAsync();

            pendingSellId = pendingSell.Id;
            pendingBuyId = pendingBuy.Id;
            issuedSellId = issuedSell.Id;
        }

        var response = await admin.GetAsync("/api/v1/load_transfer_invoice_item?status=pending&buysell=2&per_page=100");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var ids = (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("data")
            .EnumerateArray()
            .Select(e => e.GetProperty("id").GetInt64())
            .ToList();

        ids.Should().Contain(pendingSellId, "Satış yönünde ve bekleyen kalem listede olmalı");
        ids.Should().NotContain(pendingBuyId, "yanlış yöndeki (Alış) kalem listede OLMAMALI");
        ids.Should().NotContain(issuedSellId, "zaten faturalanmış kalem listede OLMAMALI — aksi hâlde iki kez faturalanabilir");
    }

    [Fact]
    public async Task Footer_CreateThenDelete_RoundTripsCorrectly()
    {
        using var admin = await _factory.CreateAdminClientAsync();
        var accountId = await CreateAccountAsync(admin);
        var invoiceId = await CreateInvoiceAsync(admin, accountId);

        using var createForm = new MultipartFormDataContent
        {
            { new StringContent(invoiceId.ToString()), "invoice_id" },
            { new StringContent("IBAN: TR00 0000 0000 0000 0000 00"), "value" },
        };
        var createResponse = await admin.PostAsync("/api/v1/invoice/footer", createForm);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var footerId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();

        var listResponse = await admin.GetAsync($"/api/v1/invoice/footer?invoice_id={invoiceId}");
        var list = (await listResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        list.GetArrayLength().Should().Be(1);
        list[0].GetProperty("value").GetString().Should().Be("IBAN: TR00 0000 0000 0000 0000 00");

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/invoice/footer")
        {
            Content = JsonContent.Create(new { deletion_id = new[] { footerId } }),
        };
        var deleteResponse = await admin.SendAsync(deleteRequest);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterDelete = (await (await admin.GetAsync($"/api/v1/invoice/footer?invoice_id={invoiceId}"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        afterDelete.GetArrayLength().Should().Be(0);
    }
}
