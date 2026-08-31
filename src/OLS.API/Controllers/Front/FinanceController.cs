using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLS.API.Filters;
using OLS.Business.Services.Authorization;
using OLS.Business.Common;
using OLS.Business.Services.Finance;

namespace OLS.API.Controllers.Front;

/// <summary>
/// Finans ekranları: cari bakiye/ekstre, fatura, tahsilat-ödeme.
///
/// Yetki <c>finance_management</c>; muhasebe defteri uçları (fiş, mizan, hesap
/// planı) AYRI bir sayfada (<c>accounting_management</c>) — operasyon ekibinin
/// cari bakiyeyi görmesi gerekirken yevmiye defterini görmesi gerekmiyor.
/// </summary>
[Authorize]
[Route("api/v1/finance")]
public sealed class FinanceController : ApiControllerBase
{
    private readonly ILedgerService _ledger;
    private readonly IFinanceDocumentService _documents;
    private readonly IFinanceInvoiceWriteService _invoiceWriter;

    public FinanceController(
        ILedgerService ledger,
        IFinanceDocumentService documents,
        IFinanceInvoiceWriteService invoiceWriter)
    {
        _ledger = ledger;
        _documents = documents;
        _invoiceWriter = invoiceWriter;
    }

    /// <summary>Cari bakiye listesi — bakiye fiş satırlarından hesaplanır.</summary>
    [HttpGet("balances")]
    [RequiresPermission(PermissionAction.Read, "finance_management")]
    public async Task<IActionResult> Balances(
        [FromQuery] string? search,
        [FromQuery(Name = "only_open")] bool onlyOpen,
        [FromQuery(Name = "per_page")] int? perPage,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _ledger.GetBalancesAsync(
            new LedgerBalanceQuery
            {
                Search = search,
                OnlyOpen = onlyOpen,
                PerPage = perPage,
                Page = page,
            },
            CurrentPath, cancellationToken);

        return Ok(result, "Kayıtlar Listelendi");
    }

    /// <summary>
    /// Cari ekstre. <c>from</c> verilirse açılış bakiyesi o tarihten ÖNCEKİ
    /// tüm hareketlerden hesaplanır; aksi hâlde ekstrenin kapanışı cari
    /// bakiyesiyle tutmaz.
    /// </summary>
    [HttpGet("balances/{accountId:long}/statement")]
    [RequiresPermission(PermissionAction.Read, "finance_management")]
    public async Task<IActionResult> Statement(
        long accountId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var statement = await _ledger.GetStatementAsync(
            new LedgerStatementQuery { AccountId = accountId, From = from, To = to },
            cancellationToken);

        return statement is null
            ? NotFoundError()
            : Ok(statement, "Kayıt Getirildi");
    }

    [HttpGet("invoices")]
    [RequiresPermission(PermissionAction.Read, "finance_management")]
    public async Task<IActionResult> Invoices(
        [FromQuery] string? search,
        [FromQuery] string? direction,
        [FromQuery(Name = "account_id")] long? accountId,
        [FromQuery(Name = "load_transfer_id")] long? loadTransferId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery(Name = "only_overdue")] bool onlyOverdue,
        [FromQuery(Name = "per_page")] int? perPage,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _documents.GetInvoicesAsync(
            new InvoiceQuery
            {
                Search = search,
                Direction = direction,
                AccountId = accountId,
                LoadTransferId = loadTransferId,
                From = from,
                To = to,
                OnlyOverdue = onlyOverdue,
                PerPage = perPage,
                Page = page,
            },
            CurrentPath, cancellationToken);

        return Ok(result, "Kayıtlar Listelendi");
    }

    [HttpGet("invoices/{id:long}")]
    [RequiresPermission(PermissionAction.Read, "finance_management")]
    public async Task<IActionResult> Invoice(long id, CancellationToken cancellationToken)
    {
        var invoice = await _documents.GetInvoiceAsync(id, cancellationToken);
        return invoice is null ? NotFoundError() : Ok(invoice, "Kayıt Getirildi");
    }

    /// <summary>
    /// Fatura açar. Kayıt ÖNCE Siber'e yazılır; oradaki yazma başarısız olursa
    /// yerelde de kayıt oluşmaz.
    /// </summary>
    [HttpPost("invoices")]
    [RequiresPermission(PermissionAction.Create, "finance_management")]
    public async Task<IActionResult> CreateInvoice(
        [FromBody] FinanceInvoiceCreateRequest request, CancellationToken cancellationToken)
    {
        var result = await _invoiceWriter.CreateAsync(request, cancellationToken);

        if (!result.Success)
            return BadRequest(ApiResponse.Error(result.Error ?? "Fatura açılamadı."));

        return Ok(new { id = result.Id, invoice_number = result.InvoiceNumber },
            "Kayıt Oluşturuldu");
    }

    /// <summary>Bir yükün faturaları — yük ekranındaki finans bölümü.</summary>
    [HttpGet("loads/{loadTransferId:long}/invoices")]
    [RequiresPermission(PermissionAction.Read, "finance_management")]
    public async Task<IActionResult> LoadInvoices(
        long loadTransferId, CancellationToken cancellationToken) =>
        Ok(await _documents.GetLoadInvoicesAsync(loadTransferId, cancellationToken),
            "Kayıtlar Listelendi");

    [HttpGet("payments")]
    [RequiresPermission(PermissionAction.Read, "finance_management")]
    public async Task<IActionResult> Payments(
        [FromQuery] string? search,
        [FromQuery(Name = "account_id")] long? accountId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery(Name = "per_page")] int? perPage,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _documents.GetPaymentsAsync(
            new PaymentQuery
            {
                Search = search,
                AccountId = accountId,
                From = from,
                To = to,
                PerPage = perPage,
                Page = page,
            },
            CurrentPath, cancellationToken);

        return Ok(result, "Kayıtlar Listelendi");
    }

    // ------------------------------------------------------------------
    // Muhasebe defteri — ayrı yetki
    // ------------------------------------------------------------------

    [HttpGet("vouchers")]
    [RequiresPermission(PermissionAction.Read, "accounting_management")]
    public async Task<IActionResult> Vouchers(
        [FromQuery] string? search,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery(Name = "per_page")] int? perPage,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _documents.GetVouchersAsync(
            new VoucherQuery
            {
                Search = search,
                From = from,
                To = to,
                PerPage = perPage,
                Page = page,
            },
            CurrentPath, cancellationToken);

        return Ok(result, "Kayıtlar Listelendi");
    }

    [HttpGet("vouchers/{id:long}")]
    [RequiresPermission(PermissionAction.Read, "accounting_management")]
    public async Task<IActionResult> Voucher(long id, CancellationToken cancellationToken)
    {
        var voucher = await _documents.GetVoucherAsync(id, cancellationToken);
        return voucher is null ? NotFoundError() : Ok(voucher, "Kayıt Getirildi");
    }

    /// <summary>Mizan — hesap koduna göre borç/alacak/bakiye.</summary>
    [HttpGet("trial_balance")]
    [RequiresPermission(PermissionAction.Read, "accounting_management")]
    public async Task<IActionResult> TrialBalance(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery(Name = "code_prefix")] string? codePrefix,
        [FromQuery] short? level,
        [FromQuery(Name = "per_page")] int? perPage,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _ledger.GetTrialBalanceAsync(
            new TrialBalanceQuery
            {
                From = from,
                To = to,
                CodePrefix = codePrefix,
                Level = level,
                PerPage = perPage,
                Page = page,
            },
            CurrentPath, cancellationToken);

        return Ok(result, "Kayıtlar Listelendi");
    }

    [HttpGet("accounting_plan")]
    [RequiresPermission(PermissionAction.Read, "accounting_management")]
    public async Task<IActionResult> AccountingPlan(
        [FromQuery] string? search,
        [FromQuery] short? level,
        [FromQuery(Name = "include_passive")] bool includePassive,
        [FromQuery(Name = "per_page")] int? perPage,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _documents.GetAccountingPlanAsync(
            new AccountingPlanQuery
            {
                Search = search,
                Level = level,
                IncludePassive = includePassive,
                PerPage = perPage,
                Page = page,
            },
            CurrentPath, cancellationToken);

        return Ok(result, "Kayıtlar Listelendi");
    }
}
