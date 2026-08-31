using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLS.Business.Services.Finance;
using OLS.Business.Services.TransferData;

namespace OLS.API.Controllers.Front;

/// <summary>
/// Muhasebe/finans verisini Siber'den çeker.
///
/// Uçlar AYRI tutuldu: hesap planı 3.938 satır, fiş satırları 214.954 satır —
/// bunları tek çağrıda toplamak isteğin zaman aşımına düşmesine yol açıyordu.
/// Sıralama önemli: fişler carilere, faturalar hem cariye hem yüke bağlanıyor,
/// bu yüzden cari/yük senkronunun önce koşmuş olması gerekir.
/// </summary>
[Authorize]
[Route("api/v1/finance_sync")]
public sealed class FinanceSyncController : ApiControllerBase
{
    private readonly IFinanceSyncService _sync;

    public FinanceSyncController(IFinanceSyncService sync) => _sync = sync;

    [HttpGet("accounting_plan")]
    public async Task<IActionResult> AccountingPlan(CancellationToken cancellationToken) =>
        Result(await _sync.SyncAccountingPlanAsync(cancellationToken));

    /// <param name="full">Tüm geçmişi yeniden çeker (geri dolum/onarım).</param>
    [HttpGet("vouchers")]
    public async Task<IActionResult> Vouchers(
        [FromQuery] bool full, CancellationToken cancellationToken) =>
        Result(await _sync.SyncVouchersAsync(full, cancellationToken));

    /// <inheritdoc cref="Vouchers"/>
    [HttpGet("invoices")]
    public async Task<IActionResult> Invoices(
        [FromQuery] bool full, CancellationToken cancellationToken) =>
        Result(await _sync.SyncInvoicesAsync(full, cancellationToken));

    /// <inheritdoc cref="Vouchers"/>
    [HttpGet("payments")]
    public async Task<IActionResult> Payments(
        [FromQuery] bool full, CancellationToken cancellationToken) =>
        Result(await _sync.SyncPaymentsAsync(full, cancellationToken));

    private IActionResult Result(SiberImportSummary summary) => base.Ok(new Dictionary<string, object?>
    {
        ["message"] = "Kayıtlar başarıyla eşlendi",
        ["created"] = summary.Created,
        ["updated"] = summary.Updated,
        ["notes"] = summary.Notes,
        ["errors"] = summary.Errors,
    });
}
