using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.DataAccess.Context;

namespace OLS.Business.Services.Finance;

/// <summary>
/// Fatura, tahsilat/ödeme, muhasebe fişi ve hesap planı okuma uçları.
///
/// Hepsinde şirket görünürlüğü uygulanır (bkz. CompanyScope): Avrora ekibi
/// yalnızca Avrora belgelerini görür, diğerleri Avrora belgelerini görmez.
/// Şirketi BOŞ kayıtlar herkese görünür — Siber'de bu alanı doldurulmamış eski
/// belgeler var ve onları gizlemek toplamları bozardı.
/// </summary>
public interface IFinanceDocumentService
{
    Task<object> GetInvoicesAsync(InvoiceQuery query, string path, CancellationToken cancellationToken = default);
    Task<InvoiceDetail?> GetInvoiceAsync(long id, CancellationToken cancellationToken = default);

    Task<object> GetPaymentsAsync(PaymentQuery query, string path, CancellationToken cancellationToken = default);

    Task<object> GetVouchersAsync(VoucherQuery query, string path, CancellationToken cancellationToken = default);
    Task<VoucherDetail?> GetVoucherAsync(long id, CancellationToken cancellationToken = default);

    Task<object> GetAccountingPlanAsync(AccountingPlanQuery query, string path, CancellationToken cancellationToken = default);

    /// <summary>Bir yükün faturaları — yük ekranındaki finans bölümü için.</summary>
    Task<IReadOnlyList<InvoiceListRow>> GetLoadInvoicesAsync(long loadTransferId, CancellationToken cancellationToken = default);
}

public sealed class InvoiceQuery
{
    public string? Search { get; init; }
    /// <summary>"C" gelir, "G" gider; boşsa ikisi de.</summary>
    public string? Direction { get; init; }
    public long? AccountId { get; init; }
    public long? LoadTransferId { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    /// <summary>true ise yalnızca vadesi geçmiş faturalar.</summary>
    public bool OnlyOverdue { get; init; }
    public int? PerPage { get; init; }
    public int Page { get; init; } = 1;
}

public sealed class PaymentQuery
{
    public string? Search { get; init; }
    public long? AccountId { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int? PerPage { get; init; }
    public int Page { get; init; } = 1;
}

public sealed class VoucherQuery
{
    public string? Search { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int? PerPage { get; init; }
    public int Page { get; init; } = 1;
}

public sealed class AccountingPlanQuery
{
    public string? Search { get; init; }
    public short? Level { get; init; }
    public bool IncludePassive { get; init; }
    public int? PerPage { get; init; }
    public int Page { get; init; } = 1;
}

public sealed record InvoiceListRow(
    long Id,
    string? Direction,
    string? InvoiceNumber,
    DateTime? InvoiceDate,
    DateTime? DueDate,
    long? AccountId,
    string? AccountName,
    string? CurrencyCode,
    decimal? TotalAmount,
    decimal? TotalAmountTl,
    long? LoadTransferId,
    string? LoadNumber,
    bool IsApproved);

public sealed record InvoiceLineRow(
    long Id,
    string? FinancialItemName,
    decimal? Quantity,
    decimal? UnitPrice,
    string? CurrencyCode,
    decimal? TaxRate,
    decimal? Amount,
    decimal? TaxAmount,
    decimal? AmountTl,
    string? Description);

public sealed record InvoiceDetail(
    long Id,
    string? Direction,
    string? InvoiceSeries,
    string? InvoiceNumber,
    DateTime? InvoiceDate,
    DateTime? DueDate,
    long? AccountId,
    string? AccountName,
    string? CurrencyCode,
    decimal? ExchangeRate,
    decimal? Amount,
    decimal? TaxAmount,
    decimal? TotalAmount,
    decimal? AmountTl,
    decimal? TaxAmountTl,
    decimal? TotalAmountTl,
    string? Description,
    string? DocumentNumber,
    string? ModuleCode,
    long? LoadTransferId,
    string? LoadNumber,
    bool IsApproved,
    DateTime? ApprovalDate,
    string? SiberCreatedBy,
    DateTime? SiberCreatedAt,
    IReadOnlyList<InvoiceLineRow> Lines);

public sealed record PaymentListRow(
    long Id,
    string? ReceiptNumber,
    DateTime? ReceiptDate,
    DateTime? DueDate,
    string? DebitName,
    string? DebitAccountCode,
    string? CreditName,
    string? CreditAccountCode,
    string? CurrencyCode,
    decimal? Amount,
    decimal? AmountTl,
    string? Description);

public sealed record VoucherListRow(
    long Id,
    short? VoucherType,
    DateTime? VoucherDate,
    int? VoucherNumber,
    int? JournalNumber,
    string? Description,
    int LineCount,
    decimal Debit,
    decimal Credit);

public sealed record VoucherLineRow(
    long Id,
    string? AccountCode,
    string? AccountName,
    long? AccountId,
    string? PartyName,
    decimal? Debit,
    decimal? Credit,
    string? CurrencyCode,
    decimal? DebitFx,
    decimal? CreditFx,
    string? Description,
    string? DocumentNumber,
    DateTime? DocumentDate);

public sealed record VoucherDetail(
    long Id,
    short? VoucherType,
    DateTime? VoucherDate,
    int? VoucherNumber,
    int? JournalNumber,
    string? Description,
    string? DocumentNumber,
    bool IsChecked,
    decimal Debit,
    decimal Credit,
    /// <summary>Borç ve alacak toplamı eşit değilse fiş dengesizdir.</summary>
    bool IsBalanced,
    IReadOnlyList<VoucherLineRow> Lines);

public sealed record AccountingPlanRow(
    long Id,
    string Code,
    string? Name,
    short? Level,
    bool IsPassive);

public sealed class FinanceDocumentService : IFinanceDocumentService
{
    private readonly OlsDbContext _db;
    private readonly ICompanyScope _companyScope;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public FinanceDocumentService(
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

    // ------------------------------------------------------------------
    // Fatura
    // ------------------------------------------------------------------
    public async Task<object> GetInvoicesAsync(
        InvoiceQuery query, string path, CancellationToken cancellationToken = default)
    {
        var invoices = await ScopedAsync(_db.FinanceInvoices.AsNoTracking(),
            i => i.SiberCompanyId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(query.Direction))
            invoices = invoices.Where(i => i.Direction == query.Direction);

        if (query.AccountId is { } accountId)
            invoices = invoices.Where(i => i.AccountId == accountId);

        if (query.LoadTransferId is { } transferId)
            invoices = invoices.Where(i => i.LoadTransferId == transferId);

        if (query.From is { } from)
            invoices = invoices.Where(i => i.InvoiceDate >= from);

        if (query.To is { } to)
            invoices = invoices.Where(i => i.InvoiceDate <= to);

        if (query.OnlyOverdue)
        {
            var today = _clock.Now.Date;
            invoices = invoices.Where(i => i.DueDate != null && i.DueDate < today);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = QueryableExtensions.NormalizeTurkish(query.Search);
            invoices = invoices.Where(i =>
                (i.InvoiceNumber != null && i.InvoiceNumber.ToLower().Contains(term)) ||
                (i.AccountName != null && i.AccountName.ToLower().Contains(term)) ||
                (i.DocumentNumber != null && i.DocumentNumber.ToLower().Contains(term)));
        }

        var rows = invoices
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .Select(i => new InvoiceListRow(
                i.Id, i.Direction, i.InvoiceNumber, i.InvoiceDate, i.DueDate,
                i.AccountId,
                i.Account != null ? i.Account.Name : i.AccountName,
                i.CurrencyCode, i.TotalAmount, i.TotalAmountTl,
                i.LoadTransferId,
                i.LoadTransfer != null ? i.LoadTransfer.LoadNumberWorkType : null,
                i.IsApproved));

        return await rows.ToPagedOrListAsync(query.PerPage, query.Page, path, cancellationToken);
    }

    public async Task<InvoiceDetail?> GetInvoiceAsync(
        long id, CancellationToken cancellationToken = default)
    {
        var invoices = await ScopedAsync(_db.FinanceInvoices.AsNoTracking(),
            i => i.SiberCompanyId, cancellationToken);

        var invoice = await invoices
            .Where(i => i.Id == id)
            .Select(i => new
            {
                i.Id, i.Direction, i.InvoiceSeries, i.InvoiceNumber, i.InvoiceDate, i.DueDate,
                i.AccountId,
                AccountName = i.Account != null ? i.Account.Name : i.AccountName,
                i.CurrencyCode, i.ExchangeRate,
                i.Amount, i.TaxAmount, i.TotalAmount,
                i.AmountTl, i.TaxAmountTl, i.TotalAmountTl,
                i.Description, i.DocumentNumber, i.ModuleCode, i.LoadTransferId,
                LoadNumber = i.LoadTransfer != null ? i.LoadTransfer.LoadNumberWorkType : null,
                i.IsApproved, i.ApprovalDate, i.SiberCreatedBy, i.SiberCreatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (invoice is null)
            return null;

        var lines = await _db.FinanceInvoiceLines.AsNoTracking()
            .Where(l => l.FinanceInvoiceId == id)
            .OrderBy(l => l.Id)
            .Select(l => new InvoiceLineRow(
                l.Id, l.FinancialItemName, l.Quantity, l.UnitPrice, l.CurrencyCode,
                l.TaxRate, l.Amount, l.TaxAmount, l.AmountTl, l.Description))
            .ToListAsync(cancellationToken);

        return new InvoiceDetail(
            invoice.Id, invoice.Direction, invoice.InvoiceSeries, invoice.InvoiceNumber,
            invoice.InvoiceDate, invoice.DueDate, invoice.AccountId, invoice.AccountName,
            invoice.CurrencyCode, invoice.ExchangeRate,
            invoice.Amount, invoice.TaxAmount, invoice.TotalAmount,
            invoice.AmountTl, invoice.TaxAmountTl, invoice.TotalAmountTl,
            invoice.Description, invoice.DocumentNumber, invoice.ModuleCode,
            invoice.LoadTransferId, invoice.LoadNumber,
            invoice.IsApproved, invoice.ApprovalDate,
            invoice.SiberCreatedBy, invoice.SiberCreatedAt,
            lines);
    }

    public async Task<IReadOnlyList<InvoiceListRow>> GetLoadInvoicesAsync(
        long loadTransferId, CancellationToken cancellationToken = default)
    {
        var invoices = await ScopedAsync(_db.FinanceInvoices.AsNoTracking(),
            i => i.SiberCompanyId, cancellationToken);

        return await invoices
            .Where(i => i.LoadTransferId == loadTransferId)
            .OrderByDescending(i => i.InvoiceDate)
            .Select(i => new InvoiceListRow(
                i.Id, i.Direction, i.InvoiceNumber, i.InvoiceDate, i.DueDate,
                i.AccountId,
                i.Account != null ? i.Account.Name : i.AccountName,
                i.CurrencyCode, i.TotalAmount, i.TotalAmountTl,
                i.LoadTransferId, null, i.IsApproved))
            .ToListAsync(cancellationToken);
    }

    // ------------------------------------------------------------------
    // Tahsilat / ödeme
    // ------------------------------------------------------------------
    public async Task<object> GetPaymentsAsync(
        PaymentQuery query, string path, CancellationToken cancellationToken = default)
    {
        var payments = await ScopedAsync(_db.FinancePayments.AsNoTracking(),
            p => p.SiberCompanyId, cancellationToken);

        // Cari filtresi İKİ TARAFI da kapsar: bir tahsilatta cari borç
        // tarafında, bir ödemede alacak tarafında durur.
        if (query.AccountId is { } accountId)
            payments = payments.Where(p =>
                p.DebitAccountId == accountId || p.CreditAccountId == accountId);

        if (query.From is { } from)
            payments = payments.Where(p => p.ReceiptDate >= from);

        if (query.To is { } to)
            payments = payments.Where(p => p.ReceiptDate <= to);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = QueryableExtensions.NormalizeTurkish(query.Search);
            payments = payments.Where(p =>
                (p.ReceiptNumber != null && p.ReceiptNumber.ToLower().Contains(term)) ||
                (p.DebitName != null && p.DebitName.ToLower().Contains(term)) ||
                (p.CreditName != null && p.CreditName.ToLower().Contains(term)));
        }

        var rows = payments
            .OrderByDescending(p => p.ReceiptDate)
            .ThenByDescending(p => p.Id)
            .Select(p => new PaymentListRow(
                p.Id, p.ReceiptNumber, p.ReceiptDate, p.DueDate,
                p.DebitName, p.DebitAccountCode,
                p.CreditName, p.CreditAccountCode,
                p.CurrencyCode, p.Amount, p.AmountTl, p.Description));

        return await rows.ToPagedOrListAsync(query.PerPage, query.Page, path, cancellationToken);
    }

    // ------------------------------------------------------------------
    // Muhasebe fişi
    // ------------------------------------------------------------------
    public async Task<object> GetVouchersAsync(
        VoucherQuery query, string path, CancellationToken cancellationToken = default)
    {
        var vouchers = await ScopedAsync(_db.FinanceVouchers.AsNoTracking(),
            v => v.SiberCompanyId, cancellationToken);

        if (query.From is { } from)
            vouchers = vouchers.Where(v => v.VoucherDate >= from);

        if (query.To is { } to)
            vouchers = vouchers.Where(v => v.VoucherDate <= to);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = QueryableExtensions.NormalizeTurkish(query.Search);
            vouchers = vouchers.Where(v =>
                (v.Description != null && v.Description.ToLower().Contains(term)) ||
                (v.DocumentNumber != null && v.DocumentNumber.ToLower().Contains(term)));
        }

        var rows = vouchers
            .OrderByDescending(v => v.VoucherDate)
            .ThenByDescending(v => v.VoucherNumber)
            .Select(v => new VoucherListRow(
                v.Id, v.VoucherType, v.VoucherDate, v.VoucherNumber, v.JournalNumber,
                v.Description,
                v.Lines.Count,
                v.Lines.Sum(l => l.Debit ?? 0m),
                v.Lines.Sum(l => l.Credit ?? 0m)));

        return await rows.ToPagedOrListAsync(query.PerPage, query.Page, path, cancellationToken);
    }

    public async Task<VoucherDetail?> GetVoucherAsync(
        long id, CancellationToken cancellationToken = default)
    {
        var vouchers = await ScopedAsync(_db.FinanceVouchers.AsNoTracking(),
            v => v.SiberCompanyId, cancellationToken);

        var voucher = await vouchers
            .Where(v => v.Id == id)
            .Select(v => new
            {
                v.Id, v.VoucherType, v.VoucherDate, v.VoucherNumber, v.JournalNumber,
                v.Description, v.DocumentNumber, v.IsChecked,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (voucher is null)
            return null;

        var lines = await _db.FinanceVoucherLines.AsNoTracking()
            .Where(l => l.FinanceVoucherId == id)
            .OrderBy(l => l.LineNumber)
            .Select(l => new
            {
                l.Id, l.AccountCode, l.AccountId,
                PartyName = l.Account != null ? l.Account.Name : null,
                l.Debit, l.Credit, l.CurrencyCode, l.DebitFx, l.CreditFx,
                l.Description, l.DocumentNumber, l.DocumentDate,
            })
            .ToListAsync(cancellationToken);

        // Hesap adı metin eşleşmesiyle; planda karşılığı yoksa boş kalır.
        var codes = lines.Select(l => l.AccountCode).Distinct().ToList();
        var names = await _db.AccountingPlans.AsNoTracking()
            .Where(p => codes.Contains(p.Code))
            .Select(p => new { p.Code, p.Name })
            .ToListAsync(cancellationToken);

        var nameByCode = names
            .GroupBy(n => n.Code)
            .ToDictionary(g => g.Key, g => g.First().Name);

        var lineRows = lines.Select(l => new VoucherLineRow(
            l.Id, l.AccountCode,
            nameByCode.GetValueOrDefault(l.AccountCode),
            l.AccountId, l.PartyName,
            l.Debit, l.Credit, l.CurrencyCode, l.DebitFx, l.CreditFx,
            l.Description, l.DocumentNumber, l.DocumentDate)).ToList();

        var debit = lineRows.Sum(l => l.Debit ?? 0m);
        var credit = lineRows.Sum(l => l.Credit ?? 0m);

        return new VoucherDetail(
            voucher.Id, voucher.VoucherType, voucher.VoucherDate, voucher.VoucherNumber,
            voucher.JournalNumber, voucher.Description, voucher.DocumentNumber,
            voucher.IsChecked, debit, credit, debit == credit, lineRows);
    }

    // ------------------------------------------------------------------
    // Hesap planı
    // ------------------------------------------------------------------
    public async Task<object> GetAccountingPlanAsync(
        AccountingPlanQuery query, string path, CancellationToken cancellationToken = default)
    {
        var plans = await ScopedAsync(_db.AccountingPlans.AsNoTracking(),
            p => p.SiberCompanyId, cancellationToken);

        if (!query.IncludePassive)
            plans = plans.Where(p => !p.IsPassive);

        if (query.Level is { } level)
            plans = plans.Where(p => p.Level == level);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = QueryableExtensions.NormalizeTurkish(query.Search);
            plans = plans.Where(p =>
                p.Code.ToLower().Contains(term) ||
                (p.Name != null && p.Name.ToLower().Contains(term)));
        }

        var rows = plans
            .OrderBy(p => p.Code)
            .Select(p => new AccountingPlanRow(p.Id, p.Code, p.Name, p.Level, p.IsPassive));

        return await rows.ToPagedOrListAsync(query.PerPage, query.Page, path, cancellationToken);
    }

    /// <summary>Şirket görünürlüğünü herhangi bir finans tablosuna uygular.</summary>
    private async Task<IQueryable<T>> ScopedAsync<T>(
        IQueryable<T> source,
        System.Linq.Expressions.Expression<Func<T, string?>> companySelector,
        CancellationToken cancellationToken)
    {
        var visibility = await _companyScope.ResolveAsync(_currentUser.Id, cancellationToken);
        if (visibility.SeesEverything)
            return source;

        var parameter = companySelector.Parameters[0];
        var body = companySelector.Body;

        System.Linq.Expressions.Expression predicate;

        if (visibility.OnlyCompanyId is { } only)
        {
            predicate = System.Linq.Expressions.Expression.Equal(
                body, System.Linq.Expressions.Expression.Constant(only, typeof(string)));
        }
        else
        {
            var isNull = System.Linq.Expressions.Expression.Equal(
                body, System.Linq.Expressions.Expression.Constant(null, typeof(string)));
            var notExcluded = System.Linq.Expressions.Expression.NotEqual(
                body,
                System.Linq.Expressions.Expression.Constant(visibility.ExcludeCompanyId, typeof(string)));
            predicate = System.Linq.Expressions.Expression.OrElse(isNull, notExcluded);
        }

        return source.Where(
            System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(predicate, parameter));
    }
}
