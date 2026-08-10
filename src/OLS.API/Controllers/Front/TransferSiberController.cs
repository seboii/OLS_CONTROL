using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLS.API.Filters;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.TransferSiber;

namespace OLS.API.Controllers.Front;

/// <summary>
/// olsold: <c>Front\TransferSiber\TransferSiberController</c>
///
/// <c>POST /api/v1/transfer_to_siber</c> — teklifi Siber'e rezervasyon olarak yazar.
/// Frontend'de "Sibere Aktar" düğmesi bunu çağırır
/// (<c>OfferFormDrawer.vue</c> ve <c>LoadFormDrawer.vue</c> → <c>SEND_LOAD_SIBER</c>).
///
/// Kaynaktaki <c>loadSave</c> ucu PORTLANMADI: Siber'in saklı yordamlarını
/// (<c>skn_rezervasyonyukbildir_tarifeaktar</c> vb.) çağıran alternatif bir
/// dönüşüm yolu ve frontend tarafından kullanılmıyor. Dönüşüm için
/// <c>POST /api/v1/load_transfer</c> kullanılıyor.
/// </summary>
[Authorize]
[Route("api/v1/transfer_to_siber")]
public sealed class TransferSiberController : ApiControllerBase
{
    private readonly ITransferSiberService _transfer;
    private readonly ILoadReleaseService _release;
    private readonly ICurrentUser _currentUser;

    public TransferSiberController(
        ITransferSiberService transfer, ILoadReleaseService release, ICurrentUser currentUser)
    {
        _transfer = transfer;
        _release = release;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Teklifi (rezervasyonu) gerçek yüke dönüştürür — Siber'de
    /// "Operasyona Bildir". Gövdeden gelen <c>id</c> yerel kayıt id'si DEĞİL,
    /// <b>Siber rezervasyon kimliğidir</b> (<c>loads.siber_id</c>).
    /// </summary>
    [HttpPost("loadSave")]
    [RequiresPermission(PermissionAction.Update, "load_management")]
    public async Task<IActionResult> LoadSave(
        [FromBody] LoadReleaseRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
            return BadRequest(ApiResponse.ValidationErrors(new Dictionary<string, string[]>
            {
                ["message"] = [Translator.Get("Zorunlu Alan")],
            }));

        if (!_release.IsConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse.ServerError(
                "Siber bağlantısı yapılandırılmamış.",
                "ConnectionStrings:Siber tanımlı değil."));

        var result = await _release.ReleaseAsync(request.Id, _currentUser.Id, cancellationToken);

        if (!result.Success)
            return BadRequest(ApiResponse.ValidationErrors(new Dictionary<string, string[]>
            {
                ["message"] = [result.Message],
            }));

        return base.Ok(new Dictionary<string, object?>
        {
            ["success"] = true,
            ["message"] = result.Message,
            ["yuk_id"] = result.YukId,
            ["yuk_no"] = result.LoadNumber,
        });
    }

    public sealed class LoadReleaseRequest
    {
        /// <summary>Siber rezervasyon kimliği (<c>loads.siber_id</c>).</summary>
        [JsonPropertyName("id")] public string? Id { get; set; }
    }

    public sealed class TransferRequest
    {
        /// <summary>Teklifin yerel id'si (loads.id).</summary>
        [JsonPropertyName("id")]
        public long Id { get; set; }
    }

    [HttpPost]
    [RequiresPermission(PermissionAction.Update, "load_management")]
    public async Task<IActionResult> Transfer(
        [FromBody] TransferRequest request, CancellationToken cancellationToken)
    {
        if (request.Id == 0)
            return BadRequestError("Form hataydı! Lütfen geliştiricinizle iletişime geçin.");

        if (_currentUser.Id is not { } userId)
            return Unauthorized(ApiResponse.Error(Translator.Get("Yetkisiz Erişim")));

        var result = await _transfer.TransferOfferAsync(request.Id, userId, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse.ValidationErrors(new Dictionary<string, string[]>
            {
                ["message"] = [result.ErrorMessage!],
            }));

        // olsold yanıt şekli birebir korunuyor.
        return base.Ok(new TransferResponse
        {
            Message = "Sibere aktarım tamamlandı.",
            SiberId = result.SiberId,
            ReservationNumber = result.ReservationNumber,
        });
    }

    private sealed class TransferResponse
    {
        [JsonPropertyName("message")] public string Message { get; init; } = string.Empty;
        [JsonPropertyName("siber_id")] public string? SiberId { get; init; }
        [JsonPropertyName("reservation_number")] public int? ReservationNumber { get; init; }
    }
}
