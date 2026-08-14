using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OLS.API.Filters;
using OLS.API.Services;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.Loads;
using OLS.Business.Services.Website;

namespace OLS.API.Controllers.Front;

/// <summary>
/// Yük eki senkronu — <c>POST /api/v1/load/file/upload</c>.
/// olsold: <c>LoadController::fileUpload</c>
///
/// İstek gövdesi yükün dosya listesinin SON HÂLİdir: <c>files[i][id]</c>
/// taşıyanlar korunur, <c>files[i][file]</c> taşıyanlar yeni yüklenir,
/// listede geçmeyen mevcut dosyalar diskten ve veritabanından silinir.
/// </summary>
[Authorize]
[Route("api/v1/load/file")]
public sealed class LoadFileController : ApiControllerBase
{
    private readonly ILoadFileService _files;
    private readonly IFileStorage _storage;

    public LoadFileController(ILoadFileService files, IFileStorage storage)
    {
        _files = files;
        _storage = storage;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(CancellationToken cancellationToken)
    {
        var form = await Request.ReadFormAsync(cancellationToken);

        if (!long.TryParse(form["load_id"], out var loadId))
            return UnprocessableEntity(ApiResponse.ValidationErrors(
                new Dictionary<string, string[]> { ["load_id"] = [Translator.Get("Zorunlu Alan")] }));

        // Arayüz alanları files[0][id] / files[0][file] biçiminde gönderiyor;
        // model bağlayıcı bu iç içe diziyi çözemediği için form ham okunur.
        var keepIds = form.Keys
            .Where(k => k.StartsWith("files[", StringComparison.Ordinal) &&
                        k.EndsWith("][id]", StringComparison.Ordinal))
            .Select(k => form[k].ToString())
            .Where(v => long.TryParse(v, out _))
            .Select(long.Parse)
            .ToList();

        var uploaded = new List<NewLoadFile>();

        foreach (var file in form.Files)
        {
            if (!file.Name.StartsWith("files[", StringComparison.Ordinal))
                continue;

            var stored = await _storage.SaveDocumentAsync(file, cancellationToken);

            if (stored is null)
                continue;

            uploaded.Add(new NewLoadFile(
                stored, Path.GetExtension(file.FileName).TrimStart('.'), file.FileName));
        }

        var removed = await _files.SyncAsync(loadId, keepIds, uploaded, cancellationToken);

        foreach (var name in removed)
            _storage.Delete(name);

        return base.Ok(ApiResponse.Message("Kayıt Başarıyla Güncellendi"));
    }
}

/// <summary>
/// Yeni yetki sayfası tanımlama — <c>POST /api/v1/permission</c>.
/// olsold: <c>Front\Permission\PermissionController::save</c>
///
/// Geliştirici aracı: yeni modül eklendiğinde yetki sayfasını açar ve tüm
/// kullanıcılara dört hakkı da verir. Kaynakta yetki kontrolü YOKTU (yalnızca
/// giriş yapmış olmak yeterliydi) — ama bu uç TÜM kullanıcıların yetki setini
/// toplu değiştiren bir yan etki taşıyor; docs/API-PARITE-MATRISI.md bunun için
/// en azından <c>role_management</c>(create) planlamıştı, burada uygulanıyor.
/// </summary>
[Authorize]
[Route("api/v1/permission")]
public sealed class PermissionPageController : ApiControllerBase
{
    private readonly IPermissionPageService _pages;

    public PermissionPageController(IPermissionPageService pages)
    {
        _pages = pages;
    }

    [HttpPost]
    [RequiresPermission(PermissionAction.Create, "role_management")]
    public async Task<IActionResult> Save(
        [FromBody] PermissionPageRequest request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.PageName))
            errors["permission_page_name"] = ["Yetki adı zorunludur."];

        if (string.IsNullOrWhiteSpace(request.PageSlug))
            errors["permission_page_slug"] = ["Yetki slug zorunludur."];

        if (errors.Count > 0)
            return UnprocessableEntity(ApiResponse.ValidationErrors(errors));

        var result = await _pages.CreateAsync(request.PageName!, request.PageSlug!, cancellationToken);

        return result.Success
            ? base.Ok(new Dictionary<string, object?> { ["message"] = result.Message })
            : BadRequest(new Dictionary<string, object?> { ["message"] = result.Message });
    }

    public sealed class PermissionPageRequest
    {
        [JsonPropertyName("permission_page_name")] public string? PageName { get; set; }
        [JsonPropertyName("permission_page_slug")] public string? PageSlug { get; set; }
    }
}

/// <summary>
/// Müşteriye teklif e-postası — <c>POST /api/v1/offer_send_email</c>.
///
/// Gerçek gönderim varsayılan olarak KAPALI (<c>Mail:Enabled</c>);
/// gerekçesi <see cref="IOfferEmailService"/> açıklamasında.
/// </summary>
[Authorize]
[Route("api/v1/offer_send_email")]
public sealed class OfferEmailController : ApiControllerBase
{
    private readonly IOfferEmailService _email;

    public OfferEmailController(IOfferEmailService email)
    {
        _email = email;
    }

    [HttpPost]
    public async Task<IActionResult> Save(
        [FromBody] OfferEmailRequest request, CancellationToken cancellationToken)
    {
        if (request.Id is null or <= 0)
            return UnprocessableEntity(ApiResponse.ValidationErrors(
                new Dictionary<string, string[]> { ["id"] = [Translator.Get("Zorunlu Alan")] }));

        var result = await _email.SendAsync(request.Id.Value, cancellationToken);

        return base.Ok(new Dictionary<string, object?>
        {
            ["sent"] = result.Sent,
            ["recipient"] = result.Recipient,
            ["message"] = result.Message,
        });
    }

    public sealed class OfferEmailRequest
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
    }
}

/// <summary>
/// Tanıtım sitesi iletişim formu.
/// olsold: <c>Website\Contact\ContactFormController</c>
///
/// DİKKAT: bu rotalar <c>/api/v1</c> altında DEĞİL, doğrudan <c>/api</c>
/// altında; <c>POST</c> kimlik doğrulaması istemez (dış siteden gelir),
/// okuma uçları panelden çağrılır.
///
/// Yanıt zarfı da farklı: <c>{success, data}</c>.
/// </summary>
[Route("api/website/contact/form")]
public sealed class ContactFormController : ApiControllerBase
{
    private readonly IContactFormService _forms;

    public ContactFormController(IContactFormService forms)
    {
        _forms = forms;
    }

    [AllowAnonymous]
    [EnableRateLimiting("public-form")]
    [HttpPost]
    public async Task<IActionResult> Store(
        [FromBody] ContactFormRequest request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.FirstName))
            errors["first_name"] = [Translator.Get("Zorunlu Alan")];

        if (string.IsNullOrWhiteSpace(request.LastName))
            errors["last_name"] = [Translator.Get("Zorunlu Alan")];

        if (string.IsNullOrWhiteSpace(request.Email))
            errors["email"] = [Translator.Get("Zorunlu Alan")];

        if (string.IsNullOrWhiteSpace(request.Message))
            errors["message"] = [Translator.Get("Zorunlu Alan")];

        if (errors.Count > 0)
            return UnprocessableEntity(ApiResponse.ValidationErrors(errors));

        var created = await _forms.CreateAsync(
            new ContactFormInput
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                Message = request.Message,
            },
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new Dictionary<string, object?>
        {
            ["success"] = true,
            ["message"] = "Contact form submitted successfully",
            ["data"] = created,
        });
    }

    [RequiresPermission(PermissionAction.Read, "support_request_management")]
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _forms.ListAsync(search, page, CurrentPath, cancellationToken);

        return base.Ok(new Dictionary<string, object?>
        {
            ["success"] = true,
            ["data"] = result,
        });
    }

    [RequiresPermission(PermissionAction.Read, "support_request_management")]
    [HttpGet("{id:long}")]
    public async Task<IActionResult> Show(long id, CancellationToken cancellationToken)
    {
        var form = await _forms.ShowAsync(id, cancellationToken);

        if (form is null)
            return NotFound(new Dictionary<string, object?>
            {
                ["success"] = false,
                ["message"] = "Contact form not found or an error occurred",
            });

        return base.Ok(new Dictionary<string, object?>
        {
            ["success"] = true,
            ["data"] = form,
        });
    }

    [RequiresPermission(PermissionAction.Update, "support_request_management")]
    [HttpPatch("{id:long}/answered")]
    public async Task<IActionResult> UpdateAnswered(
        long id, [FromBody] AnsweredRequest request, CancellationToken cancellationToken)
    {
        if (request.IsAnswered is null)
            return UnprocessableEntity(ApiResponse.ValidationErrors(
                new Dictionary<string, string[]> { ["is_answered"] = [Translator.Get("Zorunlu Alan")] }));

        var updated = await _forms.SetAnsweredAsync(id, request.IsAnswered.Value, cancellationToken);

        if (updated is null)
            return NotFound(new Dictionary<string, object?>
            {
                ["success"] = false,
                ["message"] = "Contact form not found or an error occurred",
            });

        return base.Ok(new Dictionary<string, object?>
        {
            ["success"] = true,
            ["message"] = request.IsAnswered.Value
                ? "Form marked as answered"
                : "Form marked as not answered",
            ["data"] = updated,
        });
    }

    public sealed class ContactFormRequest
    {
        [JsonPropertyName("first_name")] public string? FirstName { get; set; }
        [JsonPropertyName("last_name")] public string? LastName { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("phone")] public string? Phone { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
    }

    public sealed class AnsweredRequest
    {
        [JsonPropertyName("is_answered")] public bool? IsAnswered { get; set; }
    }
}
