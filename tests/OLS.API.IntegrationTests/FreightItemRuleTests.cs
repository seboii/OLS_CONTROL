using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.API.IntegrationTests;

/// <summary>
/// NAVLUN KURALLARI (kullanıcı isteği):
///   1. "Olumlu" teklifte en az bir NAVLUN kalemi zorunlu.
///   2. Navlun alış kaleminin karşılığı olan satış kalemi tanımlı olmalı ki
///      arayüz satış satırını kendiliğinden açabilsin (%15 kâr ile).
///
/// EŞLEŞME SİBER'DE YOK: <c>skn_kalem.alisfaturaiskalemid</c> 47.192 kalemin
/// tamamında boş, <c>skn_kalemdefault</c> boş. Bu yüzden eşleşme yerel
/// <c>financial_item_pairs</c> tablosunda tanımlanıyor.
///
/// ADDAN TÜRETİLEMEZ: kara navlununun gelir karşılığı "KARA NAVLUN GELİRİ"
/// DEĞİL "KARA NAVLUN HİZMET BEDELİ"dir ve o ad Siber'de hiç yoktur — canlıda
/// en çok kullanılan çift (1.519 yük) tam olarak budur.
/// </summary>
[Collection("OlsApi")]
public sealed class FreightItemRuleTests
{
    private readonly OlsApiFactory _factory;

    public FreightItemRuleTests(OlsApiFactory factory) => _factory = factory;

    /// <summary>Tohumlanan demo navlun çiftinin alış/satış kalem kimlikleri.</summary>
    private async Task<(long PurchaseId, long SaleId)> FreightPairAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var pair = await db.FinancialItemPairs.AsNoTracking().OrderBy(p => p.Id).FirstAsync();

        return (pair.PurchaseItemId, pair.SaleItemId);
    }

    private async Task<long> NonFreightItemIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        return await db.FinancialItems.AsNoTracking()
            .Where(f => !f.IsFreight).OrderBy(f => f.Id).Select(f => f.Id).FirstAsync();
    }

    /// <summary>
    /// "Olumlu" teklif Siber'e aktarılıp yüke dönüşecek kayıttır; bu yüzden
    /// dönüşüm için gereken alanların hepsi doludur (bkz. LoadController).
    /// </summary>
    private static MultipartFormDataContent PositiveOfferForm(
        long accountId, string countryId, long financialItemId) =>
        new()
        {
            { new StringContent("1"), "work_type_id" },
            { new StringContent("1"), "loading_type_id" },
            { new StringContent("1"), "payment_type_id" },
            { new StringContent("5"), "status_type_id" },
            { new StringContent("1"), "department_id" },
            { new StringContent(accountId.ToString()), "customer_id" },
            { new StringContent(accountId.ToString()), "sender_id" },
            { new StringContent(accountId.ToString()), "receiver_id" },
            { new StringContent(countryId), "departure_country_id" },
            { new StringContent(countryId), "target_country_id" },
            { new StringContent("1"), "romork_type_id" },
            { new StringContent("1"), "load_transfer_type_id" },
            { new StringContent("1"), "way_of_working" },
            { new StringContent("1"), "instruction_id" },
            { new StringContent("2026-09-01"), "offer_date" },
            { new StringContent("2026-09-30"), "offer_validity_date" },
            { new StringContent("2026-09-01"), "marketing_notification_date" },
            { new StringContent(financialItemId.ToString()), "load_financial_item[0][item]" },
            { new StringContent("1"), "load_financial_item[0][quantity]" },
            { new StringContent("1"), "load_financial_item[0][buysell]" },
            { new StringContent("1"), "load_financial_item[0][transport_type_id]" },
            { new StringContent("1"), "load_financial_item[0][order]" },
            { new StringContent("100"), "load_financial_item[0][net_price]" },
            { new StringContent("100"), "load_financial_item[0][total_price]" },
            { new StringContent("1"), "load_financial_item[0][currency]" },
        };

    private async Task<(long AccountId, string CountryId)> ReferencesAsync(HttpClient admin)
    {
        using var accountForm = await TestAccountHelper.MinimalAccountFormAsync(
            admin, $"Navlun Testi {Guid.NewGuid():N}"[..28]);

        var accountResponse = await admin.PostAsync("/api/v1/account", accountForm);
        accountResponse.EnsureSuccessStatusCode();
        var accountId = (await accountResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();

        var countryResponse = await admin.GetAsync("/api/v1/country");
        countryResponse.EnsureSuccessStatusCode();
        var countryId = (await countryResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").EnumerateArray().First().GetProperty("id").GetGuid().ToString();

        return (accountId, countryId);
    }

    [Fact]
    public async Task OlumluTeklif_NavlunKalemiYoksa_Reddedilir()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var (accountId, countryId) = await ReferencesAsync(admin);

        using var form = PositiveOfferForm(accountId, countryId, await NonFreightItemIdAsync());

        var response = await admin.PostAsync("/api/v1/load", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").GetProperty("load_financial_item")[0].GetString()
            .Should().Contain("navlun");
    }

    [Fact]
    public async Task OlumluTeklif_NavlunKalemiVarsa_Kaydedilir()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var (accountId, countryId) = await ReferencesAsync(admin);
        var (purchaseId, _) = await FreightPairAsync();

        using var form = PositiveOfferForm(accountId, countryId, purchaseId);

        var response = await admin.PostAsync("/api/v1/load", form);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            await response.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// Kural YALNIZCA Olumlu'da işler: teklif aşamasındaki kayıtlarda finans
    /// henüz oluşmamış olabilir, zorunlu tutmak günlük işi kilitlerdi.
    /// </summary>
    [Fact]
    public async Task TeklifDurumunda_NavlunAranmaz()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var (accountId, _) = await ReferencesAsync(admin);

        using var form = new MultipartFormDataContent
        {
            { new StringContent("1"), "work_type_id" },
            { new StringContent("1"), "loading_type_id" },
            { new StringContent("1"), "payment_type_id" },
            { new StringContent("4"), "status_type_id" },
            { new StringContent("1"), "department_id" },
            { new StringContent(accountId.ToString()), "customer_id" },
            { new StringContent("2026-09-01"), "offer_date" },
            { new StringContent("2026-09-30"), "offer_validity_date" },
            { new StringContent("2026-09-01"), "marketing_notification_date" },
        };

        var response = await admin.PostAsync("/api/v1/load", form);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task EslesmeUcu_AlisKaleminiSatisKalemineBaglar()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var (purchaseId, saleId) = await FreightPairAsync();

        var response = await admin.GetAsync("/api/v1/financial_item_pair");
        response.EnsureSuccessStatusCode();

        var pairs = (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").EnumerateArray().ToList();

        pairs.Should().NotBeEmpty();

        var pair = pairs.Single(p => p.GetProperty("purchase_item_id").GetInt64() == purchaseId);

        pair.GetProperty("sale_item_id").GetInt64().Should().Be(saleId);
        // %15 varsayılan kâr oranı — arayüz satış fiyatını bununla hesaplıyor.
        pair.GetProperty("markup_percent").GetDecimal().Should().Be(15m);
    }

    /// <summary>
    /// TESLİM ŞEKLİ ve DÖVİZ TÜRÜ ARTIK TEKLİFTE TOPLANIYOR.
    ///
    /// Kullanıcı kuralı: yük açılırken doldurulabilen, yükün durumunu
    /// etkilemeyen ve teklif aşamasında zaten belli olan alanlar teklifte
    /// sorulsun; yük açıldıktan sonra tek tek girilmesi gerekmesin. Dönüşüm bu
    /// ikisini yüke taşıyor (bkz. LoadTransferWriteService).
    ///
    /// Siber'in rezervasyon tablosunda karşılıkları FİİLEN YOK — teslimsekil
    /// 19.613 kaydın yalnızca 4'ünde dolu, düz bir dovizkod sütunu hiç yok —
    /// bu yüzden alanlar yerelde tutuluyor ve Siber'e YÜK olarak yazılıyor
    /// (skn_yuk.teslimsekil / skn_yuk.dovizkod, canlıda %76 ve %82 dolu).
    /// </summary>
    [Fact]
    public async Task Teklif_TeslimSekliVeDovizTuruSaklanir()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var (accountId, _) = await ReferencesAsync(admin);

        long deliveryMethodId;
        long currencyId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
            // Teslim şekli listesi tohumlanmıyor (Siber'den içe aktarılıyor);
            // test kendi satırını açıyor.
            var deliveryMethod = await db.LoadTransferDeliveryMethods
                .OrderBy(d => d.Id).FirstOrDefaultAsync();

            if (deliveryMethod is null)
            {
                deliveryMethod = new LoadTransferDeliveryMethod
                {
                    Name = "Fabrika çıkışında teslim",
                    Edikod = "EXW",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                };
                db.LoadTransferDeliveryMethods.Add(deliveryMethod);
                await db.SaveChangesAsync();
            }

            deliveryMethodId = deliveryMethod.Id;
            currencyId = await db.Currencies.AsNoTracking()
                .OrderBy(c => c.Id).Select(c => c.Id).FirstAsync();
        }

        using var form = new MultipartFormDataContent
        {
            { new StringContent("1"), "work_type_id" },
            { new StringContent("1"), "loading_type_id" },
            { new StringContent("1"), "payment_type_id" },
            { new StringContent("4"), "status_type_id" },
            { new StringContent("1"), "department_id" },
            { new StringContent(accountId.ToString()), "customer_id" },
            { new StringContent("2026-09-01"), "offer_date" },
            { new StringContent("2026-09-30"), "offer_validity_date" },
            { new StringContent("2026-09-01"), "marketing_notification_date" },
            { new StringContent(deliveryMethodId.ToString()), "delivery_method_id" },
            { new StringContent(currencyId.ToString()), "currency_id" },
        };

        var response = await admin.PostAsync("/api/v1/load", form);
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            await response.Content.ReadAsStringAsync());

        var loadId = (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();

        var detail = (await (await admin.GetAsync($"/api/v1/load/{loadId}"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        detail.GetProperty("delivery_method_id").GetProperty("id").GetInt64()
            .Should().Be(deliveryMethodId);
        detail.GetProperty("currency_id").GetProperty("id").GetInt64()
            .Should().Be(currencyId);
    }

    /// <summary>
    /// Bir alış kalemi TEK satış kalemine eşlenir; aksi hâlde otomatik satır
    /// hangi kalemi açacağını bilemezdi (benzersiz dizin bunu garanti ediyor).
    /// </summary>
    [Fact]
    public async Task EslesmeUcu_AlisKalemiTekrarlanmaz()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.GetAsync("/api/v1/financial_item_pair");
        response.EnsureSuccessStatusCode();

        var purchaseIds = (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").EnumerateArray()
            .Select(p => p.GetProperty("purchase_item_id").GetInt64())
            .ToList();

        purchaseIds.Should().OnlyHaveUniqueItems();
    }
}
