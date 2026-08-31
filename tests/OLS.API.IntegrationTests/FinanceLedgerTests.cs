using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.API.IntegrationTests;

/// <summary>
/// Cari ekstre ve mizanın ARİTMETİK sözleşmesini kilitler.
///
/// Bu modülde en pahalı hata sessiz olandır: bakiye doğru GÖRÜNÜR ama yanlıştır.
/// Bu yüzden testler tek tek alanları değil, bozulduğunda muhasebeyi yanlış
/// yapan değişmezleri doğrular:
///   * açılış + borç − alacak = kapanış,
///   * açılış bakiyesi aralıktan ÖNCEKİ hareketleri kapsar,
///   * ekstrenin son yürüyen bakiyesi kapanışa eşittir,
///   * bakiye listesi ile ekstre aynı sayıyı verir.
/// </summary>
[Collection("OlsApi")]
public sealed class FinanceLedgerTests
{
    private readonly OlsApiFactory _factory;

    public FinanceLedgerTests(OlsApiFactory factory) => _factory = factory;

    /// <summary>
    /// Bir cari ve ona bağlı üç fiş satırı kurar: biri aralıktan önce
    /// (açılış bakiyesini oluşturur), ikisi aralık içinde.
    /// </summary>
    private async Task<(long AccountId, string AccountCode)> SeedLedgerAsync()
    {
        // Hesap kodu HER TOHUMLAMADA benzersiz: mizan koda göre grupluyor ve
        // aynı koleksiyondaki diğer testler aynı kodu paylaşsaydı tutarlar
        // birikip birbirini bozardı.
        var accountCode = $"120 01 {Guid.NewGuid():N}"[..20];

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var account = new Account
        {
            Name = $"Ekstre Test Cari {Guid.NewGuid():N}",
            SiberId = Guid.NewGuid().ToString(),
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        var voucher = new FinanceVoucher
        {
            SiberId = Guid.NewGuid().ToString(),
            VoucherDate = new DateTime(2026, 1, 15),
            VoucherNumber = 1,
        };
        db.FinanceVouchers.Add(voucher);
        await db.SaveChangesAsync();

        db.FinanceVoucherLines.AddRange(
            // Aralıktan ÖNCE — açılış bakiyesine girer, satır listesine girmez.
            new FinanceVoucherLine
            {
                SiberId = Guid.NewGuid().ToString(),
                FinanceVoucherId = voucher.Id,
                AccountId = account.Id,
                AccountCode = accountCode,
                DocumentDate = new DateTime(2025, 6, 1),
                Debit = 1000m,
                Credit = 0m,
                LineNumber = 1,
            },
            new FinanceVoucherLine
            {
                SiberId = Guid.NewGuid().ToString(),
                FinanceVoucherId = voucher.Id,
                AccountId = account.Id,
                AccountCode = accountCode,
                DocumentDate = new DateTime(2026, 2, 1),
                Debit = 500m,
                Credit = 0m,
                LineNumber = 2,
            },
            new FinanceVoucherLine
            {
                SiberId = Guid.NewGuid().ToString(),
                FinanceVoucherId = voucher.Id,
                AccountId = account.Id,
                AccountCode = accountCode,
                DocumentDate = new DateTime(2026, 3, 1),
                Debit = 0m,
                Credit = 200m,
                LineNumber = 3,
            });

        await db.SaveChangesAsync();
        return (account.Id, accountCode);
    }

    [Fact]
    public async Task Statement_OpeningPlusMovements_EqualsClosing()
    {
        var (accountId, _) = await SeedLedgerAsync();
        var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.GetAsync(
            $"/api/v1/finance/balances/{accountId}/statement?from=2026-01-01");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        var opening = data.GetProperty("opening_balance").GetDecimal();
        var debit = data.GetProperty("debit").GetDecimal();
        var credit = data.GetProperty("credit").GetDecimal();
        var closing = data.GetProperty("closing_balance").GetDecimal();

        // 2025 hareketi aralığın dışında ama açılışta olmalı.
        opening.Should().Be(1000m);
        debit.Should().Be(500m);
        credit.Should().Be(200m);
        closing.Should().Be(opening + debit - credit);
        closing.Should().Be(1300m);
    }

    [Fact]
    public async Task Statement_LastRunningBalance_EqualsClosing()
    {
        var (accountId, _) = await SeedLedgerAsync();
        var admin = await _factory.CreateAdminClientAsync();

        var data = (await (await admin.GetAsync(
                $"/api/v1/finance/balances/{accountId}/statement?from=2026-01-01"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        var lines = data.GetProperty("lines").EnumerateArray().ToList();

        // Yalnızca aralık içindeki iki hareket listelenir.
        lines.Should().HaveCount(2);

        var lastRunning = lines[^1].GetProperty("running_balance").GetDecimal();
        lastRunning.Should().Be(data.GetProperty("closing_balance").GetDecimal());
    }

    [Fact]
    public async Task Statement_WithoutDateRange_OpeningIsZeroAndClosingIsFullBalance()
    {
        var (accountId, _) = await SeedLedgerAsync();
        var admin = await _factory.CreateAdminClientAsync();

        var data = (await (await admin.GetAsync(
                $"/api/v1/finance/balances/{accountId}/statement"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        // Tarih verilmezse açılış yoktur; kapanış carinin TÜM bakiyesidir.
        data.GetProperty("opening_balance").GetDecimal().Should().Be(0m);
        data.GetProperty("closing_balance").GetDecimal().Should().Be(1300m);
    }

    [Fact]
    public async Task BalanceList_And_Statement_AgreeOnTheSameNumber()
    {
        var (accountId, _) = await SeedLedgerAsync();
        var admin = await _factory.CreateAdminClientAsync();

        var listData = (await (await admin.GetAsync(
                "/api/v1/finance/balances?per_page=500"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("data");

        var row = listData.EnumerateArray()
            .First(r => r.GetProperty("account_id").GetInt64() == accountId);

        var statement = (await (await admin.GetAsync(
                $"/api/v1/finance/balances/{accountId}/statement"))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        // İki uç aynı kaynaktan farklı yollarla hesaplıyor; ayrışmaları
        // kullanıcıya çelişkili iki rakam gösterir.
        row.GetProperty("balance").GetDecimal()
            .Should().Be(statement.GetProperty("closing_balance").GetDecimal());
    }

    [Fact]
    public async Task TrialBalance_KeepsAccountsMissingFromThePlan()
    {
        var (_, accountCode) = await SeedLedgerAsync();
        var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.GetAsync(
            $"/api/v1/finance/trial_balance?code_prefix={Uri.EscapeDataString(accountCode)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var rows = (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").EnumerateArray().ToList();

        // Hesap planında karşılığı olmayan kod ADSIZ görünür ama TUTARI
        // kaybolmaz — mizandan tutar düşmesi denkliği sessizce bozardı.
        rows.Should().ContainSingle();
        rows[0].GetProperty("debit").GetDecimal().Should().Be(1500m);
        rows[0].GetProperty("credit").GetDecimal().Should().Be(200m);
        rows[0].GetProperty("balance").GetDecimal().Should().Be(1300m);
    }

    [Fact]
    public async Task FinanceEndpoints_RequirePermission()
    {
        var admin = await _factory.CreateAdminClientAsync();

        // Yetkisiz istemci: finans uçları kimliği doğrulanmamış erişime kapalı.
        using var anonymous = _factory.CreateClient();

        (await anonymous.GetAsync("/api/v1/finance/balances")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
        (await anonymous.GetAsync("/api/v1/finance/vouchers")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);

        (await admin.GetAsync("/api/v1/finance/balances")).StatusCode
            .Should().Be(HttpStatusCode.OK);
    }
}
