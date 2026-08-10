using Microsoft.AspNetCore.Mvc;
using OLS.Business.Common;

namespace OLS.API.Controllers;

/// <summary>
/// Tüm Front API controller'larının tabanı.
///
/// olsold'da her controller metodu yanıt zarfını elle kuruyordu
/// (<c>response()->json(['data' => ..., 'message' => __('general.…')])</c>).
/// Aynı zarfı burada tek yerden üretiyoruz ki şekil kaymasın.
/// </summary>
[ApiController]
[Route("api/v1")]
public abstract class ApiControllerBase : ControllerBase
{
    private ITranslator? _translator;

    /// <summary>Laravel'in <c>__('general.…')</c> karşılığı.</summary>
    protected ITranslator Translator =>
        _translator ??= HttpContext.RequestServices.GetRequiredService<ITranslator>();

    /// <summary>Sayfalama URL'lerinde kullanılacak mutlak yol (Laravel'in <c>path</c> alanı).</summary>
    protected string CurrentPath =>
        $"{Request.Scheme}://{Request.Host}{Request.Path}";

    protected IActionResult Ok(object? data, string messageKey) =>
        base.Ok(ApiResponse.Success(data, Translator.Get(messageKey)));

    protected IActionResult OkMessage(string messageKey) =>
        base.Ok(ApiResponse.Message(Translator.Get(messageKey)));

    /// <summary>olsold'daki <c>['errors' => ['...']]</c> 400 yanıtının karşılığı.</summary>
    protected IActionResult BadRequestError(string messageKey) =>
        BadRequest(ApiResponse.Error(Translator.Get(messageKey)));

    protected IActionResult NotFoundError(string messageKey = "Kayıt Bulunamadı") =>
        NotFound(ApiResponse.Error(Translator.Get(messageKey)));
}
