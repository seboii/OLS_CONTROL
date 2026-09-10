using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OLS.API.Controllers.Front;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.LoadTransfers;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.API.IntegrationTests;

/// <summary>
/// MALİ KALEM TEK TARAFA YAZILIR.
///
/// BULUNAN GERÇEK HATA: her kalem İKİ kez yazılıyordu — alış (Siber
/// <c>GC='C'</c>) ve satış (<c>GC='G'</c>), aynı kalem ve aynı fiyatla.
/// Kullanıcının ekranda gördüğü: "KARA NAVLUN GİDERİ" hem Alış hem Satış
/// hareketlerinde çıkıyordu. Gider alışta olmalı; satış tarafında ise onun
/// eşleşen GELİR kalemi ("KARA NAVLUN HİZMET BEDELİ") olmalı — o satırı navlun
/// eşleşmesi %15 zamla kendisi açıyor.
///
/// Siber'in kendi verisi tek taraflı: yüke bağlı 38.492 mali kalem satırının
/// 30.298'i C, 8.194'ü G ve aynı modülde aynı kalemin iki tarafta birden
/// olduğu yalnızca 28 satır (%0,07) — o 28'i de bu uygulama üretmişti.
/// </summary>
[Collection("OlsApi")]
public sealed class FinancialItemSideTests
{
    private readonly OlsApiFactory _factory;

    public FinancialItemSideTests(OlsApiFactory factory) => _factory = factory;

    private static async Task<FinancialItem> AddItemAsync(OlsDbContext db, string name)
    {
        var item = new FinancialItem
        {
            Name = $"{name} {Guid.NewGuid():N}"[..28],
            SiberId = Guid.NewGuid().ToString().ToUpperInvariant(),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };

        db.FinancialItems.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    /// <summary>
    /// Formun gönderdiği taraf ne ise Siber'e o yazılır; karşı tarafa İKİNCİ
    /// bir satır AÇILMAZ.
    /// </summary>
    [Theory]
    [InlineData("1", "C")]
    [InlineData("2", "G")]
    public async Task TeklifsizYuk_KalemYalnizcaKendiTarafinaYazilir(string buysell, string beklenenGc)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var item = await AddItemAsync(db, "KARA NAVLUN GİDERİ");
        var siber = new RecordingDirectLoadRepository();

        var model = await DirectLoadModelHelper.BuildAsync(db, financialItems:
            [new DirectLoadFinancialItem(item.Id, null, null, 1000m, 1m, null, buysell)]);

        var service = new DirectLoadService(
            db, siber,
            scope.ServiceProvider.GetRequiredService<ICompanyScope>(),
            scope.ServiceProvider.GetRequiredService<IPermissionService>(),
            scope.ServiceProvider.GetRequiredService<IClock>(),
            new PassThroughReferenceValidator(),
            new FakeSiberCountryResolver(db));

        var result = await service.CreateAsync(model, currentUserId: 1);

        result.IsSuccess.Should().BeTrue(result.ErrorMessage);

        var written = await db.LoadTransferInvoiceItems.AsNoTracking()
            .Where(i => i.ItemId == (int)item.Id)
            .ToListAsync();

        written.Should().ContainSingle("kalem yalnızca kendi tarafına yazılmalı");
        written[0].Buysell.Should().Be(buysell);

        siber.Kalemler.Should().ContainSingle();
        siber.Kalemler[0].Gc.Should().Be(beklenenGc);
    }

    /// <summary>
    /// Taraf hiç gönderilmezse ALIŞ yazılır: Siber'de 38.492 satırın 30.298'i C,
    /// yani baskın taraf alış.
    /// </summary>
    [Fact]
    public async Task TarafGonderilmezse_AlisaYazilir()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var item = await AddItemAsync(db, "GÜMRÜKLEME");
        var siber = new RecordingDirectLoadRepository();

        var model = await DirectLoadModelHelper.BuildAsync(db, financialItems:
            [new DirectLoadFinancialItem(item.Id, null, null, 500m, 1m, null)]);

        var service = new DirectLoadService(
            db, siber,
            scope.ServiceProvider.GetRequiredService<ICompanyScope>(),
            scope.ServiceProvider.GetRequiredService<IPermissionService>(),
            scope.ServiceProvider.GetRequiredService<IClock>(),
            new PassThroughReferenceValidator(),
            new FakeSiberCountryResolver(db));

        (await service.CreateAsync(model, currentUserId: 1)).IsSuccess.Should().BeTrue();

        var written = await db.LoadTransferInvoiceItems.AsNoTracking()
            .Where(i => i.ItemId == (int)item.Id).ToListAsync();

        written.Should().ContainSingle();
        written[0].Buysell.Should().Be("1");
    }

    /// <summary>İstek DTO'su tarafı modele taşımalı — zincirin ilk halkası.</summary>
    [Fact]
    public void Istek_TarafiModeleTasir()
    {
        var request = new DirectLoadRequest
        {
            FinancialItems =
            [
                new DirectLoadFinancialItemRequest { ItemId = 7, NetPrice = 10m, Quantity = 1m, Buysell = "2" },
            ],
        };

        request.ToModel().FinancialItems.Should().ContainSingle()
            .Which.Buysell.Should().Be("2");
    }
}
