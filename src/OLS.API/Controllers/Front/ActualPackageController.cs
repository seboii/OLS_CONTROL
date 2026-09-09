using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLS.API.Filters;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.LoadTransfers;

namespace OLS.API.Controllers.Front;

/// <summary>
/// GERÇEK KOLİ BİLGİLERİ — Siber'de <c>skn_yukkolidepo</c>.
///
/// Yük ekranındaki koli bölümü iki set taşıyor: beyan edilen "Yük Koli
/// Bilgileri" ve depoda ölçülen "Gerçek Koli Bilgileri". Aradaki
/// "Gerçek Koli Bilgilerine Aktar" düğmesi <c>copy</c> ucuna karşılık geliyor.
///
/// YETKİ: koli satırları yükün ALT KAYDI; proje kuralı gereği alt kayıtlar
/// yetkili kalıyor (bkz. LoadController — yalnızca ana kayıt AÇMA yetkiden
/// bağımsız). Bu yüzden load_management üzerinden okuma/güncelleme aranıyor.
/// </summary>
[Authorize]
[Route("api/v1/load_transfer/{loadTransferId:long}/actual_package")]
public sealed class ActualPackageController : ApiControllerBase
{
    private readonly IActualPackageService _packages;

    public ActualPackageController(IActualPackageService packages) => _packages = packages;

    [HttpGet]
    [RequiresPermission(PermissionAction.Read, "load_management")]
    public async Task<IActionResult> All(long loadTransferId, CancellationToken cancellationToken) =>
        base.Ok(ApiResponse.Success(
            await _packages.ListAsync(loadTransferId, cancellationToken), "Kayıtlar"));

    public sealed class CopyRequest
    {
        /// <summary>
        /// Gerçek koli satırları zaten doluysa üzerine yazılsın mı? Varsayılan
        /// HAYIR: o satırlar depoda elle düzeltilmiş olabilir.
        /// </summary>
        public bool ReplaceExisting { get; set; }
    }

    /// <summary>Siber'deki "Gerçek Koli Bilgilerine Aktar" düğmesi.</summary>
    [HttpPost("copy")]
    [RequiresPermission(PermissionAction.Update, "load_management")]
    public async Task<IActionResult> Copy(
        long loadTransferId, [FromBody] CopyRequest? request, CancellationToken cancellationToken)
    {
        var result = await _packages.CopyFromDeclaredAsync(
            loadTransferId, request?.ReplaceExisting ?? false, cancellationToken);

        return result.IsSuccess
            ? base.Ok(ApiResponse.Success(new { affected = result.Affected }, "Gerçek koli bilgilerine aktarıldı"))
            : BadRequestError(result.ErrorMessage!);
    }

    public sealed class SaveRequest
    {
        public List<ActualPackageInput> Packages { get; set; } = [];
    }

    [HttpPost]
    [RequiresPermission(PermissionAction.Update, "load_management")]
    public async Task<IActionResult> Save(
        long loadTransferId, [FromBody] SaveRequest request, CancellationToken cancellationToken)
    {
        var result = await _packages.SaveAsync(loadTransferId, request.Packages, cancellationToken);

        return result.IsSuccess
            ? base.Ok(ApiResponse.Success(new { affected = result.Affected }, "Kaydedildi"))
            : BadRequestError(result.ErrorMessage!);
    }

    public sealed class DeleteRequest
    {
        public List<long> DeletionId { get; set; } = [];
    }

    [HttpDelete]
    [RequiresPermission(PermissionAction.Delete, "load_management")]
    public async Task<IActionResult> Delete(
        long loadTransferId, [FromBody] DeleteRequest request, CancellationToken cancellationToken)
    {
        var result = await _packages.DeleteAsync(request.DeletionId, cancellationToken);

        return result.IsSuccess
            ? base.Ok(ApiResponse.Success(new { affected = result.Affected }, "Silindi"))
            : BadRequestError(result.ErrorMessage!);
    }
}
