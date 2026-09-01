using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLS.Business.Services.TransferData;

namespace OLS.API.Controllers.Front;

/// <summary>
/// olsold: <c>Front\TransferData\TransferDataController</c> — Siber referans
/// verisini yerel Postgres'e aktarır. Geliştirici/kurulum aracı: kaynakta da
/// yalnızca <c>auth:api</c> ile korunuyor, ayrı bir izin sayfası yok (bkz.
/// PermissionPageController — aynı sınıf "geliştirici aracı" deseni).
///
/// KAPSAM: yalnızca referans/tanım verisi + boş lookup tabloları portlandı.
/// Geçmiş işlem verisi taşıma (pullLoad, pull_expdition, ...) ve Uyumsoft/
/// Reports uçları BİLİNÇLİ OLARAK yok — bkz. SiberImportService.cs sınıf yorumu.
/// </summary>
[Authorize]
[Route("api/v1/transfer_data")]
public sealed class TransferDataController : ApiControllerBase
{
    private readonly ISiberImportService _import;
    private readonly ISiberSyncService _sync;

    public TransferDataController(ISiberImportService import, ISiberSyncService sync)
    {
        _import = import;
        _sync = sync;
    }

    [HttpPost]
    public async Task<IActionResult> Save(CancellationToken cancellationToken) =>
        Result(await _import.ImportReferenceDataAsync(cancellationToken: cancellationToken));

    [HttpGet("getSiberAccount")]
    public async Task<IActionResult> GetSiberAccount(CancellationToken cancellationToken) =>
        Result(await _import.ImportAccountsAsync(cancellationToken));

    [HttpGet("getLoadStatus")]
    public async Task<IActionResult> GetLoadStatus(CancellationToken cancellationToken) =>
        Result(await _import.ImportLoadStatusTypesAsync(cancellationToken));

    [HttpGet("getExpeditionType")]
    public async Task<IActionResult> GetExpeditionType(CancellationToken cancellationToken) =>
        Result(await _import.ImportExpeditionTypesAsync(cancellationToken));

    [HttpGet("getExpeditionStatus")]
    public async Task<IActionResult> GetExpeditionStatus(CancellationToken cancellationToken) =>
        Result(await _import.ImportExpeditionStatusesAsync(cancellationToken));

    [HttpGet("getCarType")]
    public async Task<IActionResult> GetCarType(CancellationToken cancellationToken) =>
        Result(await _import.ImportCarTypesAsync(cancellationToken));

    [HttpGet("getCarStatus")]
    public async Task<IActionResult> GetCarStatus(CancellationToken cancellationToken) =>
        Result(await _import.ImportCarStatusTypesAsync(cancellationToken));

    [HttpGet("getCarOwner")]
    public async Task<IActionResult> GetCarOwner(CancellationToken cancellationToken) =>
        Result(await _import.ImportCarOwnersAsync(cancellationToken));

    [HttpGet("getLoadTrasnferDeliveryMethod")]
    public async Task<IActionResult> GetLoadTransferDeliveryMethod(CancellationToken cancellationToken) =>
        Result(await _import.ImportDeliveryMethodsAsync(cancellationToken));

    [HttpGet("getCar")]
    public async Task<IActionResult> GetCar(CancellationToken cancellationToken) =>
        Result(await _import.ImportCarsAsync(cancellationToken));

    /// <summary>
    /// Siber değişiklik günlüğü. <c>full=true</c> tüm geçmişi çeker (ilk dolum).
    /// </summary>
    [HttpGet("change_logs")]
    public async Task<IActionResult> ChangeLogs(
        [FromQuery] bool full, CancellationToken cancellationToken) =>
        Result(await _sync.SyncChangeLogsAsync(full, cancellationToken));

    private IActionResult Result(SiberImportSummary summary) => base.Ok(new Dictionary<string, object?>
    {
        ["message"] = "Kayıtlar başarıyla eşlendi",
        ["created"] = summary.Created,
        ["updated"] = summary.Updated,
        ["errors"] = summary.Errors,
    });
}
