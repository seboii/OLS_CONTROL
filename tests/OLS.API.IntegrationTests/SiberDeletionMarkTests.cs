using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.API.IntegrationTests;

/// <summary>
/// Siber'de silinen kaydın davranışını kilitler.
///
/// Kural: kayıt yerelden SİLİNMEZ (bağlı finans kayıtları, evrak arşivi ve
/// denetim izi korunmalı) ama günlük listelerde GÖRÜNMEZ. Görünmesi için
/// açıkça istenmeli.
/// </summary>
[Collection("OlsApi")]
public sealed class SiberDeletionMarkTests
{
    private readonly OlsApiFactory _factory;

    public SiberDeletionMarkTests(OlsApiFactory factory) => _factory = factory;

    private async Task<(long Id, string Number)> SeedDeletedTransferAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var number = $"SIL{Guid.NewGuid():N}"[..14];

        var transfer = new LoadTransfer
        {
            LoadTransferId = Guid.NewGuid().ToString(),
            LoadNumberWorkType = number,
            SiberCreatedBy = "TESTKOD",
            SiberCreatedAt = new DateTime(2026, 5, 1, 9, 0, 0),
            SiberDeletedAt = new DateTime(2026, 8, 31, 12, 0, 0),
        };

        db.LoadTransfers.Add(transfer);
        await db.SaveChangesAsync();

        return (transfer.Id, number);
    }

    [Fact]
    public async Task SilinmisKayit_VarsayilanListedeGorunmez()
    {
        var (_, number) = await SeedDeletedTransferAsync();
        var admin = await _factory.CreateAdminClientAsync();

        var data = (await (await admin.GetAsync($"/api/v1/load_transfer?search={number}&per_page=25"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        data.GetProperty("total").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task SilinmisKayit_IstenirseListelenir()
    {
        var (_, number) = await SeedDeletedTransferAsync();
        var admin = await _factory.CreateAdminClientAsync();

        var data = (await (await admin.GetAsync(
                $"/api/v1/load_transfer?search={number}&include_deleted=true&per_page=25"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        data.GetProperty("total").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task SilinmisKayit_DetayiHalaAcilirVeIsaretiTasir()
    {
        var (id, _) = await SeedDeletedTransferAsync();
        var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.GetAsync($"/api/v1/load_transfer/{id}");
        response.EnsureSuccessStatusCode();

        // Detay kapanmaz: bağlı kayıtlara ve geçmişe erişim sürmeli.
        var audit = (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("siber_audit");

        audit.GetProperty("deleted_at").GetString().Should().NotBeNull();
        audit.GetProperty("created_by_code").GetString().Should().Be("TESTKOD");
    }

    [Fact]
    public async Task OnlyDeleted_YalnizcaSilinmisKayitlariDoner()
    {
        var (_, number) = await SeedDeletedTransferAsync();
        var admin = await _factory.CreateAdminClientAsync();

        // only_deleted, include_deleted'dan farklıdır: silinenleri normal
        // listeye katmaz, YALNIZCA onları getirir ("ne silinmiş?" sorusu).
        var data = (await (await admin.GetAsync(
                $"/api/v1/load_transfer?search={number}&only_deleted=true&per_page=25"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        data.GetProperty("total").GetInt32().Should().Be(1);
        data.GetProperty("data")[0].GetProperty("siber_deleted_at").GetString()
            .Should().NotBeNull();
    }

    [Fact]
    public async Task SilenBilgisi_VarsaDetaydaDoner()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var transfer = new LoadTransfer
        {
            LoadTransferId = Guid.NewGuid().ToString(),
            LoadNumberWorkType = $"SLN{Guid.NewGuid():N}"[..14],
            SiberDeletedAt = new DateTime(2026, 9, 1, 8, 34, 0),
            // Siber günlüğünden gelen gerçek silme; fark edilme anından FARKLI.
            SiberDeletedBy = "ASLIY",
            SiberDeletedOn = new DateTime(2026, 8, 19, 14, 42, 59),
        };
        db.LoadTransfers.Add(transfer);
        await db.SaveChangesAsync();

        var admin = await _factory.CreateAdminClientAsync();

        var audit = (await (await admin.GetAsync($"/api/v1/load_transfer/{transfer.Id}"))
            .Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("siber_audit");

        audit.GetProperty("deleted_by_code").GetString().Should().Be("ASLIY");
        audit.GetProperty("deleted_on").GetDateTime().Should().Be(new DateTime(2026, 8, 19, 14, 42, 59));
        // İki damga karıştırılmamalı: biri gerçek silme, diğeri fark edilme.
        audit.GetProperty("deleted_at").GetDateTime().Should().NotBe(
            audit.GetProperty("deleted_on").GetDateTime());
    }

    [Fact]
    public async Task SilinmemisKayit_IsaretsizDoner()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var transfer = new LoadTransfer
        {
            LoadTransferId = Guid.NewGuid().ToString(),
            LoadNumberWorkType = $"CANLI{Guid.NewGuid():N}"[..14],
        };
        db.LoadTransfers.Add(transfer);
        await db.SaveChangesAsync();

        var admin = await _factory.CreateAdminClientAsync();

        var audit = (await (await admin.GetAsync($"/api/v1/load_transfer/{transfer.Id}"))
            .Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("siber_audit");

        audit.ValueKind.Should().Be(JsonValueKind.Null);
    }
}
