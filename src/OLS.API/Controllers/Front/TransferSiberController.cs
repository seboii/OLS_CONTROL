using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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
    private readonly ILogger<TransferSiberController> _logger;

    public TransferSiberController(
        ITransferSiberService transfer, ILoadReleaseService release,
        ICurrentUser currentUser, ILogger<TransferSiberController> logger)
    {
        _transfer = transfer;
        _release = release;
        _currentUser = currentUser;
        _logger = logger;
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

    /// <summary>Siber hata metninin yalnızca ilk satırı — gerisi yığın/teknik detay.</summary>
    private static string FirstLine(string message)
    {
        var idx = message.IndexOfAny(NewLineChars);
        return (idx < 0 ? message : message[..idx]).Trim();
    }

    private static readonly char[] NewLineChars = ['\n', '\r'];

    [HttpPost]
    [RequiresPermission(PermissionAction.Update, "load_management")]
    public async Task<IActionResult> Transfer(
        [FromBody] TransferRequest request, CancellationToken cancellationToken)
    {
        if (request.Id == 0)
            return BadRequestError("Form hataydı! Lütfen geliştiricinizle iletişime geçin.");

        if (_currentUser.Id is not { } userId)
            return Unauthorized(ApiResponse.Error(Translator.Get("Yetkisiz Erişim")));

        // SON SAVUNMA HATTI: ön doğrulamalardan kaçan bir Siber kısıtı (FK, tetikleyici,
        // benzersiz indeks) INSERT sırasında patlarsa, kullanıcıya "beklenmeyen hata"
        // diyen çıplak bir 500 dönüyordu. Artık Siber'in kendi mesajı, ne yapılacağını
        // söyleyen bir cümleyle birlikte alan hatası olarak gösteriliyor.
        TransferSiberResult result;
        try
        {
            result = await _transfer.TransferOfferAsync(request.Id, userId, cancellationToken);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Teklif {LoadId} Siber'e aktarılamadı (SQL kısıtı).", request.Id);

            return BadRequest(ApiResponse.ValidationErrors(new Dictionary<string, string[]>
            {
                ["message"] =
                [
                    "Siber bu kaydı kabul etmedi. Formdaki seçimlerden biri Siber'de " +
                    $"tanımlı olmayabilir. Siber'in bildirdiği sebep: {FirstLine(ex.Message)}",
                ],
            }));
        }

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
