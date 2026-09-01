using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OLS.API.Filters;
using OLS.Business.Services.Auditing;
using OLS.Business.Services.Accounts;
using OLS.Business.Services.Authorization;
using OLS.DataAccess.Context;

namespace OLS.API.Controllers.Front;

/// <summary>
/// Teklif, yük, sefer, fatura, tahsilat ve cari kayıtlarının İŞLEM GEÇMİŞİ.
///
/// Kaynak Siber'in kendi değişiklik günlüğü (<c>sbr_log</c>): kaydın üzerindeki
/// <c>insuser</c>/<c>upduser</c> alanları yalnızca açan ve son dokunanı verir,
/// aradaki her işlem bu günlükte.
///
/// Yetkiler kendi modülüyle aynı: yükün geçmişini görmek yükü görmekle aynı
/// hassasiyette. Şirket görünürlüğü de uygulanır — Avrora kaydının geçmişi
/// başkasına açılmamalı.
/// </summary>
[Authorize]
[Route("api/v1")]
public sealed class RecordHistoryController : ApiControllerBase
{
    private const string OfferTable = "skn_rezervasyon";
    private const string LoadTable = "skn_yuk";
    private const string ExpeditionTable = "skn_pozisyon";
    private const string InvoiceTable = "sfy_gelirgider";
    private const string PaymentTable = "sfy_tahsilatodeme";
    private const string AccountTable = "sbr_firma";

    private readonly IRecordHistoryService _history;
    private readonly OlsDbContext _db;
    private readonly ICompanyScope _companyScope;
    private readonly ICurrentUser _currentUser;
    private readonly IAccountService _accounts;

    public RecordHistoryController(
        IRecordHistoryService history,
        OlsDbContext db,
        ICompanyScope companyScope,
        ICurrentUser currentUser,
        IAccountService accounts)
    {
        _history = history;
        _db = db;
        _companyScope = companyScope;
        _currentUser = currentUser;
        _accounts = accounts;
    }

    [HttpGet("load/{id:long}/history")]
    [RequiresPermission(PermissionAction.Read, "load_management")]
    public async Task<IActionResult> Offer(long id, CancellationToken cancellationToken)
    {
        var record = await _db.Loads.AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => new { l.SiberId, l.SiberCompanyId })
            .FirstOrDefaultAsync(cancellationToken);

        return await ResultAsync(OfferTable, record?.SiberId, record?.SiberCompanyId,
            record is not null, cancellationToken);
    }

    [HttpGet("load_transfer/{id:long}/history")]
    [RequiresPermission(PermissionAction.Read, "load_management")]
    public async Task<IActionResult> Load(long id, CancellationToken cancellationToken)
    {
        var record = await _db.LoadTransfers.AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new { SiberId = t.LoadTransferId, t.SiberCompanyId })
            .FirstOrDefaultAsync(cancellationToken);

        return await ResultAsync(LoadTable, record?.SiberId, record?.SiberCompanyId,
            record is not null, cancellationToken);
    }

    [HttpGet("expedition/{id:long}/history")]
    [RequiresPermission(PermissionAction.Read, "expedition_management")]
    public async Task<IActionResult> Expedition(long id, CancellationToken cancellationToken)
    {
        var record = await _db.Expeditions.AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new { SiberId = e.ExpeditionId, e.SiberCompanyId })
            .FirstOrDefaultAsync(cancellationToken);

        return await ResultAsync(ExpeditionTable, record?.SiberId, record?.SiberCompanyId,
            record is not null, cancellationToken);
    }

    [HttpGet("finance/invoices/{id:long}/history")]
    [RequiresPermission(PermissionAction.Read, "finance_management")]
    public async Task<IActionResult> Invoice(long id, CancellationToken cancellationToken)
    {
        var record = await _db.FinanceInvoices.AsNoTracking()
            .Where(i => i.Id == id)
            .Select(i => new { i.SiberId, i.SiberCompanyId })
            .FirstOrDefaultAsync(cancellationToken);

        return await ResultAsync(InvoiceTable, record?.SiberId, record?.SiberCompanyId,
            record is not null, cancellationToken);
    }

    [HttpGet("finance/payments/{id:long}/history")]
    [RequiresPermission(PermissionAction.Read, "finance_management")]
    public async Task<IActionResult> Payment(long id, CancellationToken cancellationToken)
    {
        var record = await _db.FinancePayments.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new { p.SiberId, p.SiberCompanyId })
            .FirstOrDefaultAsync(cancellationToken);

        return await ResultAsync(PaymentTable, record?.SiberId, record?.SiberCompanyId,
            record is not null, cancellationToken);
    }

    /// <summary>
    /// Cari geçmişi. Cari kayıtlarında şirket alanı yok; erişim, cari
    /// listesindeki NESNE SEVİYESİ kuralla sınırlanır (süper admin tümünü,
    /// diğerleri yalnızca kendisine atanmış carileri görür).
    /// </summary>
    [HttpGet("account/{id:long}/history")]
    [RequiresPermission(PermissionAction.Read, "account_management")]
    public async Task<IActionResult> Account(long id, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
            return NotFoundError();

        var siberId = await _db.Accounts.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => a.SiberId)
            .FirstOrDefaultAsync(cancellationToken);

        if (siberId is null)
            return NotFoundError();

        if (!await _accounts.IsVisibleToUserAsync(userId, id, cancellationToken))
            return NotFoundError();

        return Ok(await _history.GetAsync(AccountTable, siberId, cancellationToken),
            "Kayıtlar Listelendi");
    }

    private async Task<IActionResult> ResultAsync(
        string tableName, string? siberId, string? companyId, bool exists,
        CancellationToken cancellationToken)
    {
        if (!exists)
            return NotFoundError();

        var visibility = await _companyScope.ResolveAsync(_currentUser.Id, cancellationToken);
        if (!visibility.Allows(companyId))
            return NotFoundError();

        // Siber karşılığı olmayan kayıt (yalnızca yerelde açılmış) için geçmiş
        // yoktur; hata değil, boş liste.
        if (string.IsNullOrWhiteSpace(siberId))
            return Ok(Array.Empty<object>(), "Kayıtlar Listelendi");

        return Ok(await _history.GetAsync(tableName, siberId, cancellationToken),
            "Kayıtlar Listelendi");
    }
}
