using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OLS.Business.Services.LoadTransfers;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;
using OLS.DataAccess.Siber;

namespace OLS.API.IntegrationTests;

/// <summary>
/// Evrak Takibi (skn_yukevrak) — LoadTransferDocumentService'in Siber yazma
/// tarafını, gerçek/sahte Siber'e HİÇ dokunmadan doğrular. <see cref="RecordingSiberLoadRepository"/>
/// yalnızca evrak metotlarını gerçekten uygular (bellekte kaydeder); diğer tüm
/// metotlar TransferSiberTests.cs'deki FakeSiberLoadRepository gibi
/// NotSupportedException fırlatır — bu testlerin yük dönüşüm makinesine hiç
/// dokunmaması gerektiğini kilitler.
/// </summary>
[Collection("OlsApi")]
public sealed class LoadTransferDocumentTests
{
    private readonly OlsApiFactory _factory;

    public LoadTransferDocumentTests(OlsApiFactory factory) => _factory = factory;

    private static async Task<(OlsDbContext Db, LoadTransfer Transfer, EvrakTuru EvrakTuru)> SeedAsync(OlsDbContext db)
    {
        var transfer = new LoadTransfer
        {
            LoadTransferId = Guid.NewGuid().ToString(), // skn_yuk.yukid (Siber)
            LoadNumberWorkType = "25I0001",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        var evrakTuru = new EvrakTuru { Name = "Konşimento", Code = "3", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now };
        db.AddRange(transfer, evrakTuru);
        await db.SaveChangesAsync();

        return (db, transfer, evrakTuru);
    }

    [Fact]
    public async Task SaveAsync_WithValidInput_InsertsLocallyAndPushesToSiber()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<OLS.Business.Common.IClock>();
        var (_, transfer, evrakTuru) = await SeedAsync(db);

        var siber = new RecordingSiberLoadRepository();
        var service = new LoadTransferDocumentService(db, siber, clock);

        var result = await service.SaveAsync(new LoadTransferDocumentInput
        {
            LoadTransferId = transfer.Id,
            EvrakTuruId = evrakTuru.Id,
            DocumentNumber = "KONS-001",
            OriginalCount = 3,
            CopyCount = 2,
        });

        result.IsSuccess.Should().BeTrue();
        result.Data!.EvrakTuruName.Should().Be("Konşimento");
        result.Data.DocumentNumber.Should().Be("KONS-001");

        siber.Inserted.Should().ContainSingle();
        siber.Inserted[0].YukId.Should().Be(transfer.LoadTransferId);
        siber.Inserted[0].Sirano.Should().Be(3);
        siber.Inserted[0].EvrakNo.Should().Be("KONS-001");

        var stored = await db.LoadTransferDocuments.SingleAsync(d => d.Id == result.Data.Id);
        stored.Yukevrakid.Should().Be(siber.Inserted[0].YukEvrakId);
    }

    [Fact]
    public async Task SaveAsync_WhenSiberNotConfigured_ReturnsErrorWithoutTouchingLocalDb()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<OLS.Business.Common.IClock>();
        var (_, transfer, evrakTuru) = await SeedAsync(db);

        var siber = new RecordingSiberLoadRepository(isConfigured: false);
        var service = new LoadTransferDocumentService(db, siber, clock);

        var result = await service.SaveAsync(new LoadTransferDocumentInput
        {
            LoadTransferId = transfer.Id,
            EvrakTuruId = evrakTuru.Id,
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Siber bağlantısı yapılandırılmamış.");
        siber.Inserted.Should().BeEmpty();
        (await db.LoadTransferDocuments.CountAsync(d => d.LoadTransferId == transfer.Id)).Should().Be(0);
    }

    [Fact]
    public async Task UpdateAsync_ThenDeleteAsync_RoundTripsThroughSiber()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<OLS.Business.Common.IClock>();
        var (_, transfer, evrakTuru) = await SeedAsync(db);

        var siber = new RecordingSiberLoadRepository();
        var service = new LoadTransferDocumentService(db, siber, clock);

        var created = await service.SaveAsync(new LoadTransferDocumentInput
        {
            LoadTransferId = transfer.Id,
            EvrakTuruId = evrakTuru.Id,
            OriginalCount = 1,
        });

        var updated = await service.UpdateAsync(created.Data!.Id, new LoadTransferDocumentInput
        {
            LoadTransferId = transfer.Id,
            EvrakTuruId = evrakTuru.Id,
            OriginalCount = 5,
            DeliveredTo = "Muhasebe",
        });

        updated.IsSuccess.Should().BeTrue();
        updated.Data!.OriginalCount.Should().Be(5);
        updated.Data.DeliveredTo.Should().Be("Muhasebe");
        siber.Updated.Should().ContainSingle(u => u.OrjinalAdet == 5 && u.TeslimAlan == "Muhasebe");

        var yukEvrakId = siber.Inserted[0].YukEvrakId;
        var deleted = await service.DeleteAsync(created.Data.Id);

        deleted.Should().BeTrue();
        siber.Deleted.Should().ContainSingle(id => id == yukEvrakId);
        (await db.LoadTransferDocuments.CountAsync(d => d.Id == created.Data.Id)).Should().Be(0);
    }

    private sealed class RecordingSiberLoadRepository : ISiberLoadRepository
    {
    // Doğrulama uçları testte kullanılmıyor; kalem eksikliği simüle edilmiyor.
    public Task<IReadOnlyList<string>> FindMissingKalemIdsAsync(
        IReadOnlyCollection<string> kalemIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>([]);

        public RecordingSiberLoadRepository(bool isConfigured = true) => IsConfigured = isConfigured;

        public bool IsConfigured { get; }

        public List<SiberYukEvrak> Inserted { get; } = [];
        public List<SiberYukEvrak> Updated { get; } = [];
        public List<string> Deleted { get; } = [];

        public Task<Guid> GenerateYukEvrakIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Guid.NewGuid());

        public Task InsertYukEvrakAsync(SiberYukEvrak evrak, CancellationToken cancellationToken = default)
        {
            Inserted.Add(evrak);
            return Task.CompletedTask;
        }

        public Task UpdateYukEvrakAsync(SiberYukEvrak evrak, CancellationToken cancellationToken = default)
        {
            Updated.Add(evrak);
            return Task.CompletedTask;
        }

        public Task DeleteYukEvrakAsync(string yukEvrakId, CancellationToken cancellationToken = default)
        {
            Deleted.Add(yukEvrakId);
            return Task.CompletedTask;
        }

        public Task<SiberRezervasyon?> FindRezervasyonAsync(string rezervasyonId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<Guid> GenerateYukIdAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<Guid> GenerateYukKoliIdAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<Guid> GenerateModulKalemIdAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<SiberModulKayit?> FindModulKayitAsync(string loadNumberWorkType, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<SiberYukNumberResult> InsertYukWithLockedNumberAsync(SiberYuk yuk, string year, string additionalCode, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task LinkRezervasyonToYukAsync(string rezervasyonId, string yukId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task InsertYukKoliAsync(SiberYukKoli koli, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task InsertModulKalemAsync(SiberModulKalem kalem, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task DeleteYukAsync(string yukId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task DeleteYukKoliAsync(string yukKoliId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task DeleteModulKalemAsync(string modulKalemId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task MoveYukCompanyAsync(string yukId, string sirketId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateYukAsync(SiberYuk yuk, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task UpdateYukKoliAsync(SiberYukKoli koli, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task UpdateModulKalemAsync(SiberModulKalem kalem, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
