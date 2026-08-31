using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.DataAccess.Context;

namespace OLS.Business.Services.Finance;

/// <summary>
/// Cari bakiye, cari ekstre ve mizan.
///
/// BAKİYE HER ZAMAN HESAPLANIR, saklanmaz. Kolonda tutulan bakiye ilk kaçan
/// senkron turunda sessizce yanlışa düşer ve bunu kimse fark etmez.
///
/// YAŞLANDIRMA YOK — bilinçli. Klasik yaşlandırma (0-30/30-60/60-90 gün)
/// hangi faturanın ödendiğini bilmeyi gerektirir; Siber'de <c>kapalifatura</c>
/// 38.425 faturanın 36.713'ünde boş, yani kapanma bilgisi TUTULMUYOR. Ödendi
/// varsayımıyla üretilen bir yaşlandırma tablosu doğru görünür ama yanlış olur.
/// Bunun yerine iki AYRI ve doğrulanabilir bilgi veriliyor: carinin net
/// bakiyesi (fiş satırlarından) ve vadesi geçmiş faturaların listesi.
/// </summary>
public interface ILedgerService
{
    Task<object> GetBalancesAsync(LedgerBalanceQuery query, string path, CancellationToken cancellationToken = default);

    Task<LedgerStatement?> GetStatementAsync(LedgerStatementQuery query, CancellationToken cancellationToken = default);

    Task<object> GetTrialBalanceAsync(TrialBalanceQuery query, string path, CancellationToken cancellationToken = default);
}

public sealed class LedgerBalanceQuery
{
    public string? Search { get; init; }
    /// <summary>true ise yalnızca bakiyesi sıfır olmayan cariler.</summary>
    public bool OnlyOpen { get; init; }
    public int? PerPage { get; init; }
    public int Page { get; init; } = 1;
}

public sealed class LedgerStatementQuery
{
    public long AccountId { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}

public sealed class TrialBalanceQuery
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    /// <summary>Hesap kodu ön eki ("120", "320"…) ile daraltma.</summary>
    public string? CodePrefix { get; init; }
    /// <summary>Yalnızca bu seviyedeki hesaplar (1-4).</summary>
    public short? Level { get; init; }
    public int? PerPage { get; init; }
    public int Page { get; init; } = 1;
}

public sealed record LedgerBalanceRow(
    long AccountId,
    string? AccountName,
    string? AccountCode,
    int MovementCount,
    decimal Debit,
    decimal Credit,
    decimal Balance,
    DateTime? LastMovementDate);

public sealed record LedgerStatementLine(
    long Id,
    DateTime? Date,
    string? VoucherNumber,
    string? AccountCode,
    string? DocumentNumber,
    string? Description,
    string? CurrencyCode,
    decimal? DebitFx,
    decimal? CreditFx,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance,
    DateTime? DueDate,
    string? SourceId);

public sealed record LedgerStatement(
    long AccountId,
    string? AccountName,
    DateTime? From,
    DateTime? To,
    decimal OpeningBalance,
    decimal Debit,
    decimal Credit,
    decimal ClosingBalance,
    IReadOnlyList<LedgerStatementLine> Lines,
    IReadOnlyList<LedgerOverdueInvoice> OverdueInvoices);

/// <summary>
/// Vadesi geçmiş fatura. ÖDENDİ/ÖDENMEDİ BİLGİSİ İÇERMEZ — Siber bunu
/// tutmuyor (bkz. <see cref="ILedgerService"/>). Yalnızca vadesi bugünden
/// eski olan faturaları listeler.
/// </summary>
public sealed record LedgerOverdueInvoice(
    long Id,
    string? InvoiceNumber,
    DateTime? InvoiceDate,
    DateTime? DueDate,
    int OverdueDays,
    string? CurrencyCode,
    decimal? TotalAmount,
    decimal? TotalAmountTl);

public sealed record TrialBalanceRow(
    string AccountCode,
    string? AccountName,
    short? Level,
    decimal Debit,
    decimal Credit,
    decimal Balance);

public sealed class LedgerService : ILedgerService
{
    private readonly OlsDbContext _db;
    private readonly ICompanyScope _companyScope;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public LedgerService(
        OlsDbContext db,
        ICompanyScope companyScope,
        ICurrentUser currentUser,
        IClock clock)
    {
        _db = db;
        _companyScope = companyScope;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<object> GetBalancesAsync(
        LedgerBalanceQuery query, string path, CancellationToken cancellationToken = default)
    {
        var lines = await ScopedLinesAsync(cancellationToken);

        var grouped = lines
            .Where(l => l.AccountId != null)
            .GroupBy(l => l.AccountId!.Value)
            .Select(g => new
            {
                AccountId = g.Key,
                MovementCount = g.Count(),
                Debit = g.Sum(l => l.Debit ?? 0m),
                Credit = g.Sum(l => l.Credit ?? 0m),
                LastMovementDate = g.Max(l => l.DocumentDate),
                // Cari hangi hesap kodunda çalışıyorsa o gösterilir; birden
                // fazlaysa (120 ve 320 birlikte) en çok kullanılan seçilir.
                AccountCode = g.GroupBy(l => l.AccountCode)
                    .OrderByDescending(x => x.Count())
                    .Select(x => x.Key)
                    .FirstOrDefault(),
            });

        if (query.OnlyOpen)
            grouped = grouped.Where(g => g.Debit - g.Credit != 0m);

        var joined = from g in grouped
                     join a in _db.Accounts.AsNoTracking() on g.AccountId equals a.Id
                     select new { g, a.Name };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = QueryableExtensions.NormalizeTurkish(query.Search);
            joined = joined.Where(x =>
                x.Name != null && x.Name.ToLower().Contains(term));
        }

        var rows = joined
            .OrderByDescending(x => x.g.Debit - x.g.Credit != 0m)
            .ThenByDescending(x => x.g.LastMovementDate)
            .Select(x => new LedgerBalanceRow(
                x.g.AccountId,
                x.Name,
                x.g.AccountCode,
                x.g.MovementCount,
                x.g.Debit,
                x.g.Credit,
                x.g.Debit - x.g.Credit,
                x.g.LastMovementDate));

        return await rows.ToPagedOrListAsync(query.PerPage, query.Page, path, cancellationToken);
    }

    public async Task<LedgerStatement?> GetStatementAsync(
        LedgerStatementQuery query, CancellationToken cancellationToken = default)
    {
        var account = await _db.Accounts.AsNoTracking()
            .Where(a => a.Id == query.AccountId)
            .Select(a => new { a.Id, a.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (account is null)
            return null;

        var lines = (await ScopedLinesAsync(cancellationToken))
            .Where(l => l.AccountId == query.AccountId);

        // AÇILIŞ BAKİYESİ: aralıktan ÖNCEKİ tüm hareketlerin neti. Ekstre
        // yalnızca aralığı gösterse de yürüyen bakiye buradan başlamalı,
        // aksi hâlde kapanış bakiyesi cari kartıyla tutmaz.
        var opening = 0m;
        if (query.From is { } from)
        {
            opening = await lines
                .Where(l => l.DocumentDate != null && l.DocumentDate < from)
                .SumAsync(l => (l.Debit ?? 0m) - (l.Credit ?? 0m), cancellationToken);
        }

        var ranged = lines;
        if (query.From is { } f)
            ranged = ranged.Where(l => l.DocumentDate == null || l.DocumentDate >= f);
        if (query.To is { } t)
            ranged = ranged.Where(l => l.DocumentDate == null || l.DocumentDate <= t);

        var raw = await ranged
            .OrderBy(l => l.DocumentDate)
            .ThenBy(l => l.FinanceVoucher.VoucherNumber)
            .ThenBy(l => l.LineNumber)
            .Select(l => new
            {
                l.Id,
                l.DocumentDate,
                VoucherNumber = l.FinanceVoucher.VoucherNumber,
                l.AccountCode,
                l.DocumentNumber,
                l.Description,
                l.CurrencyCode,
                l.DebitFx,
                l.CreditFx,
                l.Debit,
                l.Credit,
                l.DueDate,
                l.SourceId,
            })
            .ToListAsync(cancellationToken);

        var running = opening;
        var statementLines = new List<LedgerStatementLine>(raw.Count);

        foreach (var row in raw)
        {
            var debit = row.Debit ?? 0m;
            var credit = row.Credit ?? 0m;
            running += debit - credit;

            statementLines.Add(new LedgerStatementLine(
                row.Id,
                row.DocumentDate,
                row.VoucherNumber?.ToString(),
                row.AccountCode,
                row.DocumentNumber,
                row.Description,
                row.CurrencyCode,
                row.DebitFx,
                row.CreditFx,
                debit,
                credit,
                running,
                row.DueDate,
                row.SourceId));
        }

        return new LedgerStatement(
            account.Id,
            account.Name,
            query.From,
            query.To,
            opening,
            statementLines.Sum(l => l.Debit),
            statementLines.Sum(l => l.Credit),
            running,
            statementLines,
            await OverdueInvoicesAsync(query.AccountId, cancellationToken));
    }

    public async Task<object> GetTrialBalanceAsync(
        TrialBalanceQuery query, string path, CancellationToken cancellationToken = default)
    {
        var lines = await ScopedLinesAsync(cancellationToken);

        if (query.From is { } from)
            lines = lines.Where(l => l.DocumentDate != null && l.DocumentDate >= from);
        if (query.To is { } to)
            lines = lines.Where(l => l.DocumentDate != null && l.DocumentDate <= to);
        if (!string.IsNullOrWhiteSpace(query.CodePrefix))
            lines = lines.Where(l => l.AccountCode.StartsWith(query.CodePrefix));

        // TOPLAMA VERİTABANINDA, AD EŞLEŞMESİ BELLEKTE.
        // GroupBy + LeftJoin'i tek sorguda birleştirmek EF tarafından
        // çevrilemiyor. Toplama 214.954 satırı tarar, bu yüzden mutlaka
        // veritabanında kalmalı; sonuç ise hesap sayısıyla sınırlı (≤3.936),
        // dolayısıyla adları belleğe alıp eşleştirmek güvenli.
        var totals = await lines
            .GroupBy(l => l.AccountCode)
            .Select(g => new
            {
                AccountCode = g.Key,
                Debit = g.Sum(l => l.Debit ?? 0m),
                Credit = g.Sum(l => l.Credit ?? 0m),
            })
            .ToListAsync(cancellationToken);

        // Hesap adı METİN eşleşmesiyle gelir; Siber'de fiş satırından hesap
        // planına yabancı anahtar yok. Planda karşılığı olmayan kod (kapatılmış
        // hesap) adsız görünür ama tutarı kaybolmaz.
        var planRows = await _db.AccountingPlans.AsNoTracking()
            .Select(p => new { p.Code, p.Name, p.Level })
            .ToListAsync(cancellationToken);

        var planByCode = planRows
            .GroupBy(p => p.Code)
            .ToDictionary(g => g.Key, g => g.First());

        var rows = totals
            .Select(t =>
            {
                var plan = planByCode.GetValueOrDefault(t.AccountCode);
                return new TrialBalanceRow(
                    t.AccountCode, plan?.Name, plan?.Level,
                    t.Debit, t.Credit, t.Debit - t.Credit);
            })
            // Seviye süzgeci ancak ad eşleşmesinden SONRA uygulanabilir:
            // seviye bilgisi fiş satırında değil, hesap planında.
            .Where(r => query.Level == null || r.Level == query.Level)
            .OrderBy(r => r.AccountCode)
            .ToList();

        if (query.PerPage is not { } perPage || perPage < 1)
            return rows;

        var page = query.Page < 1 ? 1 : query.Page;

        return LengthAwarePaginator<TrialBalanceRow>.Create(
            rows.Skip((page - 1) * perPage).Take(perPage).ToList(),
            rows.Count, perPage, page, path);
    }

    private async Task<IReadOnlyList<LedgerOverdueInvoice>> OverdueInvoicesAsync(
        long accountId, CancellationToken cancellationToken)
    {
        var today = _clock.Now.Date;

        var rows = await _db.FinanceInvoices.AsNoTracking()
            .Where(i => i.AccountId == accountId && i.DueDate != null && i.DueDate < today)
            .OrderBy(i => i.DueDate)
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                i.InvoiceDate,
                i.DueDate,
                i.CurrencyCode,
                i.TotalAmount,
                i.TotalAmountTl,
            })
            .ToListAsync(cancellationToken);

        return rows.Select(i => new LedgerOverdueInvoice(
            i.Id, i.InvoiceNumber, i.InvoiceDate, i.DueDate,
            i.DueDate is { } due ? (int)(today - due.Date).TotalDays : 0,
            i.CurrencyCode, i.TotalAmount, i.TotalAmountTl)).ToList();
    }

    /// <summary>
    /// Şirket görünürlüğü uygulanmış fiş satırları — yük ve seferdekiyle aynı
    /// kural (bkz. CompanyScope). Şirketi boş satır herkese görünür: Siber'de
    /// şirket alanı doldurulmamış eski kayıtlar var, onları gizlemek bakiyeyi
    /// bozardı.
    /// </summary>
    private async Task<IQueryable<DataAccess.Entities.FinanceVoucherLine>> ScopedLinesAsync(
        CancellationToken cancellationToken)
    {
        var lines = _db.FinanceVoucherLines.AsNoTracking();

        var visibility = await _companyScope.ResolveAsync(_currentUser.Id, cancellationToken);
        if (visibility.SeesEverything)
            return lines;

        return visibility.OnlyCompanyId is { } only
            ? lines.Where(l => l.SiberCompanyId == only)
            : lines.Where(l => l.SiberCompanyId == null ||
                               l.SiberCompanyId != visibility.ExcludeCompanyId);
    }
}
