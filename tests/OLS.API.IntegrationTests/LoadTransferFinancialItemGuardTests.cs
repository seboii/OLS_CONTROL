using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.LoadTransfers;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.API.IntegrationTests;

/// <summary>
/// YÜK DETAYINDA "BEKLENMEYEN BİR HATA OLUŞTU" — KALEMSİZ FİNANS SATIRI.
///
/// <c>sfy_modulkalem.kalemid</c> gerçek Siber'de NOT NULL. Kalemi seçilmemiş
/// (ya da yerel kalemin <c>siber_id</c>'si olmayan) bir finans satırı,
/// güncelleme sırasında Siber'e null <c>kalemid</c> göndermeye çalışıyor;
/// SQL Server "Cannot insert the value NULL" ile düşürüyor ve bu, iş kuralı
/// hatası (RAISERROR 50000) OLMADIĞI için kullanıcıya genel
/// "Beklenmeyen bir hata oluştu." mesajı olarak dönüyordu — hangi satırın
/// sorunlu olduğuna dair hiçbir ipucu vermeden, üstelik yerel kayıt o sırada
/// çoktan yazılmış hâlde.
///
/// Teklif → yük dönüşümü bu kontrolü zaten yapıyordu
/// (<c>LoadTransferWriteService.ValidateRequired</c>); yük detay ekranı
/// atlıyordu. Kontrol artık Siber'e DOKUNMADAN ÖNCE yapılıyor.
/// </summary>
[Collection("OlsApi")]
public sealed class LoadTransferFinancialItemGuardTests
{
    private readonly OlsApiFactory _factory;

    public LoadTransferFinancialItemGuardTests(OlsApiFactory factory) => _factory = factory;

    private static async Task<LoadTransfer> AddTransferAsync(OlsDbContext db)
    {
        var transfer = new LoadTransfer
        {
            LoadTransferId = Guid.NewGuid().ToString(),
            LoadNumber = Random.Shared.Next(100_000, 999_999).ToString(),
            LoadNumberWorkType = $"26{Random.Shared.Next(10_000, 99_999)}EX",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };

        db.LoadTransfers.Add(transfer);
        await db.SaveChangesAsync();

        return transfer;
    }

    private LoadTransferUpdateService CreateService(
        IServiceScope scope, OlsDbContext db, RecordingSiberLoadRepository siber) =>
        new(db, siber, new FakeSiberCountryResolver(db),
            scope.ServiceProvider.GetRequiredService<ICompanyScope>(),
            scope.ServiceProvider.GetRequiredService<IClock>());

    [Fact]
    public async Task KalemiBosFinansSatiri_AcikHataDoner()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var transfer = await AddTransferAsync(db);
        var siber = new RecordingSiberLoadRepository();

        var result = await CreateService(scope, db, siber).UpdateAsync(
            new LoadTransferUpdateRequest
            {
                Id = transfer.Id,
                InvoiceItems = [new LoadTransferUpdateRequest.InvoiceItemInput
                {
                    ItemId = null,
                    Buysell = "1",
                    Quantity = 1,
                    NetPrice = 100,
                    TotalPrice = 100,
                }],
            },
            currentUserId: 1);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Kalem boş olamaz");

        // SİBER'E HİÇ DOKUNULMADI: doğrulama yazımdan önce.
        siber.Written.Should().BeNull();
    }

    /// <summary>
    /// Kalem seçilmiş ama yerel kaydın Siber karşılığı yoksa da yazım
    /// düşerdi. Mesaj kalemin ADINI taşıyor — kullanıcı hangi satırı
    /// düzelteceğini bilsin.
    /// </summary>
    [Fact]
    public async Task SiberKarsiligiOlmayanKalem_AdiylaBildirilir()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var item = new FinancialItem
        {
            Name = $"SİBERSİZ KALEM {Guid.NewGuid():N}"[..28],
            SiberId = null,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.FinancialItems.Add(item);
        await db.SaveChangesAsync();

        var transfer = await AddTransferAsync(db);
        var siber = new RecordingSiberLoadRepository();

        var result = await CreateService(scope, db, siber).UpdateAsync(
            new LoadTransferUpdateRequest
            {
                Id = transfer.Id,
                InvoiceItems = [new LoadTransferUpdateRequest.InvoiceItemInput
                {
                    ItemId = (int)item.Id,
                    Buysell = "1",
                    Quantity = 1,
                    NetPrice = 100,
                    TotalPrice = 100,
                }],
            },
            currentUserId: 1);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(item.Name);
        siber.Written.Should().BeNull();
    }

    /// <summary>
    /// Siber karşılığı olan kalem geçer — kontrol yalnızca gerçekten yazımı
    /// düşürecek satırları durduruyor, çalışan akışı değil.
    /// </summary>
    [Fact]
    public async Task SiberKarsiligiOlanKalem_Gecer()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var item = new FinancialItem
        {
            Name = $"KALEM {Guid.NewGuid():N}"[..24],
            SiberId = Guid.NewGuid().ToString().ToUpperInvariant(),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.FinancialItems.Add(item);
        await db.SaveChangesAsync();

        var transfer = await AddTransferAsync(db);
        var siber = new RecordingSiberLoadRepository();

        var result = await CreateService(scope, db, siber).UpdateAsync(
            new LoadTransferUpdateRequest
            {
                Id = transfer.Id,
                InvoiceItems = [new LoadTransferUpdateRequest.InvoiceItemInput
                {
                    ItemId = (int)item.Id,
                    Buysell = "1",
                    Quantity = 1,
                    NetPrice = 100,
                    TotalPrice = 100,
                }],
            },
            currentUserId: 1);

        result.IsSuccess.Should().BeTrue(result.ErrorMessage);
        siber.Written.Should().NotBeNull();
    }
}
