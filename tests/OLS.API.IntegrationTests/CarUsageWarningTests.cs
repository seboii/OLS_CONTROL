using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OLS.Business.Services.Expeditions;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.API.IntegrationTests;

/// <summary>
/// PLAKA UYARISI — "BAĞLI OLMADIĞI HÂLDE BAĞLI DİYORDU".
///
/// Sefer formunda römork seçilince, o plakanın başka bir AÇIK seferde bağlı
/// olup olmadığı sorulur. Kontrol tamamlanmış seferleri (90 - BOŞALTILDI)
/// zaten dışarıda bırakıyordu ama <b>Siber'den silinmiş</b> seferleri
/// bırakmıyordu.
///
/// Canlı örnek: 26OZ0100EX'e 34 NBV 524 seçilince uyarı 26OZ0087EX'i
/// gösteriyordu — o sefer 2026-09-01'de Siber ekranından silinmişti, yani
/// plaka gerçekte boştaydı. Silinmiş olduğu hâlde uyarı üretebilecek 5 sefer
/// ölçüldü.
/// </summary>
[Collection("OlsApi")]
public sealed class CarUsageWarningTests
{
    private readonly OlsApiFactory _factory;

    public CarUsageWarningTests(OlsApiFactory factory) => _factory = factory;

    /// <summary>Açık bir durum (BOŞALTILDI'dan önce) — sıra numarası 90'dan küçük.</summary>
    private static async Task<long> OpenStatusIdAsync(OlsDbContext db)
    {
        var status = await db.ExpeditionStatuses.AsNoTracking()
            .Where(s => s.OrderNumber != null && s.OrderNumber < 90)
            .OrderBy(s => s.Id).FirstOrDefaultAsync();

        if (status is not null)
            return status.Id;

        var fresh = new ExpeditionStatus
        {
            Name = "10 - HAZIR",
            OrderNumber = 10,
            ExpeditionStatusId = 1,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };

        db.ExpeditionStatuses.Add(fresh);
        await db.SaveChangesAsync();
        return fresh.Id;
    }

    private static async Task<Car> AddCarAsync(OlsDbContext db)
    {
        var car = new Car
        {
            PlateNumber = $"34 NBV {Random.Shared.Next(100, 999)}",
            SiberId = Guid.NewGuid().ToString(),
            RomorkType = 15,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };

        db.Cars.Add(car);
        await db.SaveChangesAsync();
        return car;
    }

    private static async Task<Expedition> AddExpeditionAsync(
        OlsDbContext db, Car car, long statusId, DateTime? deletedAt)
    {
        var expedition = new Expedition
        {
            ExpeditionNumber = $"26OZ{Random.Shared.Next(1000, 9999)}EX",
            ExpeditionId = Guid.NewGuid().ToString(),
            RomorkId = (int)car.Id,
            StatusId = (int)statusId,
            SiberDeletedAt = deletedAt,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };

        db.Expeditions.Add(expedition);
        await db.SaveChangesAsync();
        return expedition;
    }

    [Fact]
    public async Task SilinmisSefer_PlakayiDoluGostermez()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var expeditions = scope.ServiceProvider.GetRequiredService<IExpeditionService>();

        var car = await AddCarAsync(db);
        var statusId = await OpenStatusIdAsync(db);

        // Siber'den SİLİNMİŞ sefer — plakayı işgal ETMEZ.
        await AddExpeditionAsync(db, car, statusId, deletedAt: DateTime.Now);
        var current = await AddExpeditionAsync(db, car, statusId, deletedAt: null);

        var usage = await expeditions.CarUsageAsync(car.Id, current.Id);

        usage.Should().BeEmpty("silinmiş sefer plakayı bağlı göstermemeli");
    }

    /// <summary>
    /// Silinmemiş AÇIK bir sefer hâlâ uyarı üretmeli — düzeltme kontrolü
    /// tamamen kapatmadı.
    /// </summary>
    [Fact]
    public async Task SilinmemisAcikSefer_UyariUretir()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var expeditions = scope.ServiceProvider.GetRequiredService<IExpeditionService>();

        var car = await AddCarAsync(db);
        var statusId = await OpenStatusIdAsync(db);

        var other = await AddExpeditionAsync(db, car, statusId, deletedAt: null);
        var current = await AddExpeditionAsync(db, car, statusId, deletedAt: null);

        var usage = await expeditions.CarUsageAsync(car.Id, current.Id);

        usage.Should().ContainSingle();
        usage[0].Id.Should().Be(other.Id);
    }

    /// <summary>
    /// Tamamlanmış sefer (90 - BOŞALTILDI) uyarı üretmez: bir römorkun
    /// geçmişte onlarca seferi olması normal — canlıda 34 NBV 524 plakası 25
    /// sefere bağlıydı ve 24'ü bitmişti.
    /// </summary>
    [Fact]
    public async Task TamamlanmisSefer_UyariUretmez()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var expeditions = scope.ServiceProvider.GetRequiredService<IExpeditionService>();

        var car = await AddCarAsync(db);

        var done = await db.ExpeditionStatuses.AsNoTracking()
            .Where(s => s.OrderNumber != null && s.OrderNumber >= 90)
            .OrderBy(s => s.Id).FirstOrDefaultAsync();

        if (done is null)
        {
            done = new ExpeditionStatus
            {
                Name = "90 - BOŞALTILDI",
                OrderNumber = 90,
                ExpeditionStatusId = 14,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
            };
            db.ExpeditionStatuses.Add(done);
            await db.SaveChangesAsync();
        }

        await AddExpeditionAsync(db, car, done.Id, deletedAt: null);
        var current = await AddExpeditionAsync(db, car, await OpenStatusIdAsync(db), deletedAt: null);

        var usage = await expeditions.CarUsageAsync(car.Id, current.Id);

        usage.Should().BeEmpty();
    }
}
