using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLS.API.Filters;
using OLS.API.Services;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.Loads;
using OLS.Business.Services.TransferSiber;

namespace OLS.API.Controllers.Front;

/// <summary>
/// olsold: <c>Front\Load\LoadController</c> — Yük / teklif yönetimi.
///
/// Dikkat: <c>Load</c> hem yükü hem TEKLİFİ temsil eder
/// (<c>status_type_id = 4</c> teklif demektir).
///
///   GET    /api/v1/load                     all
///   GET    /api/v1/load/{id}                single
///   POST   /api/v1/load                     save
///   POST   /api/v1/load/{id}                update
///   DELETE /api/v1/load                     delete (gövdede deletion_id)
///   DELETE /api/v1/load/load_content        içerik satırı silme
///   DELETE /api/v1/load/load_financial_item finansal kalem silme
///   POST   /api/v1/load/updateTimeOut       updateTimeOut
///
/// saveAi (AI çıktısından teklif oluşturma) ve Siber rezervasyon senkronu
/// (transfer_to_siber) henüz portlanmadı.
/// </summary>
[Authorize]
[Route("api/v1/load")]
public sealed class LoadController : ApiControllerBase
{
    /// <summary>status_types tablosundaki "Olumsuz" satırı — bkz. LoadWriteService.</summary>
    private const int NegativeStatusTypeId = 1;

    /// <summary>status_types tablosundaki "Olumlu" satırı — Yük'e dönüşebilen tek durum.</summary>
    private const int PositiveStatusTypeId = 5;

    private readonly ILoadService _loads;
    private readonly ILoadWriteService _write;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _files;
    private readonly ILoadArchivePublisher _archive;
    private readonly ITransferSiberService _transfer;
    private readonly ILogger<LoadController> _logger;

    public LoadController(
        ILoadService loads,
        ILoadWriteService write,
        ICurrentUser currentUser,
        IFileStorage files,
        ITransferSiberService transfer,
        ILoadArchivePublisher archive,
        ILogger<LoadController> logger)
    {
        _loads = loads;
        _write = write;
        _currentUser = currentUser;
        _files = files;
        _transfer = transfer;
        _archive = archive;
        _logger = logger;
    }

    [HttpGet]
    [RequiresPermission(PermissionAction.Read, "load_management")]
    public async Task<IActionResult> All(
        [FromQuery] string? search,
        [FromQuery(Name = "status_type_id")] int? statusTypeId,
        [FromQuery] int? timeout,
        [FromQuery(Name = "date_from")] DateOnly? dateFrom,
        [FromQuery(Name = "date_to")] DateOnly? dateTo,
        [FromQuery(Name = "per_page")] int? perPage,
        [FromQuery] int page = 1,
        [FromQuery(Name = "customer_id")] int? customerId = null,
        [FromQuery(Name = "sender_id")] int? senderId = null,
        [FromQuery(Name = "receiver_id")] int? receiverId = null,
        [FromQuery(Name = "agent_id")] int? agentId = null,
        [FromQuery(Name = "assigned_user_id")] int? assignedUserId = null,
        [FromQuery(Name = "work_type_id")] int? workTypeId = null,
        [FromQuery(Name = "is_draft")] int? isDraft = null,
        /// <summary>Siber'den silinmiş kayıtları da listeler.</summary>
        [FromQuery(Name = "include_deleted")] bool includeDeleted = false,
        /// <summary>Yalnizca Siber'den silinmis kayitlari listeler.</summary>
        [FromQuery(Name = "only_deleted")] bool onlyDeleted = false,
        CancellationToken cancellationToken = default)
    {
        if (_currentUser.Id is not { } userId)
            return Unauthorized(ApiResponse.Error(Translator.Get("Yetkisiz Erişim")));

        var result = await _loads.ListAsync(
            new LoadListQuery(
                userId, search, statusTypeId, timeout is 1, dateFrom, dateTo, perPage, page, CurrentPath,
                customerId, senderId, receiverId, agentId, assignedUserId, workTypeId, isDraft is 1,
                includeDeleted, onlyDeleted),
            cancellationToken);

        return Ok(result, "Kayıtlar");
    }

    [HttpGet("{id:long}")]
    [RequiresPermission(PermissionAction.Read, "load_management")]
    public async Task<IActionResult> Single(long id, CancellationToken cancellationToken)
    {
        var load = await _loads.SingleAsync(id, cancellationToken);

        return load is null
            ? NotFoundError()
            : Ok(load, "Kayıtlar");
    }

    /// <summary>
    /// olsold: <c>POST /load/saveAi</c> — AI çıktısından teklif oluşturur.
    ///
    /// OpenAI çağrısı arayüzde yapılır (<c>Offer.vue</c>); bu uç yalnızca
    /// hazır JSON'u alıp kaydeder, dış servise dokunmaz.
    ///
    /// AI id değil <b>ad</b> döndürdüğü için iş tipi / ödeme tipi / ülke /
    /// ürün tipi / para birimi adla eşlenir. Eşleşmeyen alanlar boş kalır ve
    /// yanıtta <c>eslesmeyen</c> listesinde bildirilir — kaynak sessizce
    /// geçiyordu, kullanıcı hangi alanın boş kaldığını göremiyordu.
    /// </summary>
    [HttpPost("saveAi")]
    [RequiresPermission(PermissionAction.Create, "load_management")]
    public async Task<IActionResult> SaveAi(
        [FromBody] LoadAiRequest request,
        [FromServices] ILoadAiImportService ai,
        CancellationToken cancellationToken)
    {
        var result = await ai.CreateAsync(request, cancellationToken);

        return base.Ok(ApiResponse.Success(
            new { id = result.LoadId, eslesmeyen = result.Unresolved },
            "Kayıt Başarılı"));
    }

    /// <summary>
    /// Teklifi kopyalar. Kopya YENİ bir taslaktır: Siber kimlikleri, numaralar,
    /// durum ve onay bilgisi devredilmez (bkz. LoadService.DuplicateAsync).
    /// </summary>
    [HttpPost("{id:long}/duplicate")]
    [RequiresPermission(PermissionAction.Create, "load_management")]
    public async Task<IActionResult> Duplicate(long id, CancellationToken cancellationToken)
    {
        var copyId = await _loads.DuplicateAsync(id, cancellationToken);

        if (copyId is null)
            return NotFoundError();

        return base.Ok(ApiResponse.Success(new { id = copyId.Value }, "Teklif kopyalandı"));
    }

    [HttpDelete]
    [RequiresPermission(PermissionAction.Delete, "load_management")]
    public async Task<IActionResult> Delete(
        [FromBody] DeletionRequest request, CancellationToken cancellationToken)
    {
        if (request.DeletionId.Count == 0)
            return BadRequestError("Form hataydı! Lütfen geliştiricinizle iletişime geçin.");

        var result = await _loads.DeleteAsync(request.DeletionId, cancellationToken);

        // olsold'un mesajı birebir korunuyor (frontend bu metni gösteriyor).
        if (!result.Success)
            return BadRequest(ApiResponse.ValidationErrors(new Dictionary<string, string[]>
            {
                ["message"] = ["Yük oluşturulmuş kayıt silinemez"],
            }));

        return OkMessage("Kayıt Başarıyla Silindi");
    }

    public sealed class UpdateTimeOutRequest
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
    }

    [HttpPost("updateTimeOut")]
    [RequiresPermission(PermissionAction.Update, "load_management")]
    public async Task<IActionResult> UpdateTimeOut(
        [FromBody] UpdateTimeOutRequest request, CancellationToken cancellationToken)
    {
        var status = await _loads.UpdateTimeOutAsync(request.Id, cancellationToken);

        return status switch
        {
            LoadTimeOutUpdateStatus.Success => OkMessage("Güncelleme Başarılı"),
            LoadTimeOutUpdateStatus.Locked => BadRequest(ApiResponse.ValidationErrors(new Dictionary<string, string[]>
            {
                ["message"] = ["Yük oluşturulmuş kayıt güncellenemez"],
            })),
            _ => NotFoundError(),
        };
    }

    [HttpPost]
    [RequiresPermission(PermissionAction.Create, "load_management")]
    public async Task<IActionResult> Save(
        [FromForm] LoadFormRequest form, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
            return Unauthorized(ApiResponse.Error(Translator.Get("Yetkisiz Erişim")));

        if (Validate(form) is { } errors)
            return BadRequest(ApiResponse.ValidationErrors(errors));

        if (await ValidateReferencesAsync(form, cancellationToken) is { } refErrors)
            return BadRequest(ApiResponse.ValidationErrors(refErrors));

        var (uploaded, fileContents) = await SaveFilesAsync(form, cancellationToken);
        var id = await _write.CreateAsync(form.ToModel(userId, uploaded), cancellationToken);

        var siberWarning = await PushToSiberAsync(id, userId, cancellationToken);

        // Arşiv Siber'e aktarımdan SONRA: rezervasyon kimliği o adımda oluşuyor.
        await PushFilesToArchiveAsync(id, fileContents, cancellationToken);

        var load = await _loads.SingleAsync(id, cancellationToken);
        return Ok(load, siberWarning ?? "Kayıt Başarılı");
    }

    /// <summary>
    /// Teklifi kaydedilir kaydedilmez Siber'e açar (kullanıcı isteği: "direkt Sibere
    /// kayıt açmak istiyorum"). Rezervasyon numarasını Siber'in kendi sayacı üretir
    /// (<c>MAX(rezervasyonno)+1</c>, uygulama kilidi altında — bkz.
    /// SiberReservationRepository.InsertRezervasyonWithLockedNumberAsync), böylece
    /// numarası olmayan teklif kalmaz.
    ///
    /// Siber'e yazma BAŞARISIZ olursa yerel kayıt geri alınmaz: kullanıcının emeği
    /// kaybolmasın diye teklif yerelde durur, uyarı mesajı döner ve kart üzerindeki
    /// "Siber'e Aktar" düğmesiyle elle yeniden denenebilir. Siber bağlantısı
    /// yapılandırılmamışsa sessizce atlanır (yerel/test ortamı).
    /// </summary>
    private async Task<string?> PushToSiberAsync(
        long loadId, long userId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _transfer.TransferOfferAsync(loadId, userId, cancellationToken);

            return result.IsSuccess
                ? null
                : $"Kayıt yerelde oluşturuldu, ancak Siber'e aktarılamadı: {result.ErrorMessage}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Teklif {LoadId} kaydedildi ama Siber'e aktarılamadı.", loadId);
            return "Kayıt yerelde oluşturuldu, ancak Siber'e aktarılamadı. " +
                   "Kart üzerindeki \"Siber'e Aktar\" düğmesiyle yeniden deneyebilirsiniz.";
        }
    }

    /// <summary>olsold rotası: <c>POST /api/v1/load/{id?}</c> güncelleme içindi.</summary>
    [HttpPost("{id:long}")]
    [RequiresPermission(PermissionAction.Update, "load_management")]
    public async Task<IActionResult> Update(
        long id, [FromForm] LoadFormRequest form, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
            return Unauthorized(ApiResponse.Error(Translator.Get("Yetkisiz Erişim")));

        if (Validate(form) is { } errors)
            return BadRequest(ApiResponse.ValidationErrors(errors));

        if (await ValidateReferencesAsync(form, cancellationToken) is { } refErrors)
            return BadRequest(ApiResponse.ValidationErrors(refErrors));

        var (uploaded, fileContents) = await SaveFilesAsync(form, cancellationToken);

        var model = form.ToModel(userId, uploaded) with { Id = id };
        var result = await _write.UpdateAsync(model, cancellationToken);

        // olsold'un mesajı birebir korunuyor (frontend bu metni gösteriyor).
        if (result.IsLocked)
            return BadRequest(ApiResponse.ValidationErrors(new Dictionary<string, string[]>
            {
                ["message"] = ["Yük oluşturulmuş kayıt güncellenemez"],
            }));

        if (result.Id is null)
            return NotFoundError();

        // Kaydedilen kaldırılmış dosyaların DB satırı LoadWriteService'te silindi;
        // fiziksel dosya OLS.Business'ın erişemediği IFileStorage'a bağımlı olduğu
        // için burada, çağıran katmanda silinir (bkz. LoadUpdateResult).
        foreach (var name in result.RemovedFileNames)
            _files.Delete(name);

        // Kaydet = Siber'e yansıt (bkz. PushToSiberAsync). TransferOfferAsync
        // idempotent: Siber'de kayıt varsa günceller, yoksa açıp numara atar —
        // yani ilk kaydında Siber'e ulaşılamamış teklifler burada telafi edilir.
        var siberWarning = await PushToSiberAsync(result.Id.Value, userId, cancellationToken);

        await PushFilesToArchiveAsync(result.Id.Value, fileContents, cancellationToken);

        var load = await _loads.SingleAsync(result.Id.Value, cancellationToken);
        return Ok(load, siberWarning ?? "Güncelleme Başarılı");
    }

    [HttpDelete("load_content")]
    [RequiresPermission(PermissionAction.Delete, "load_management")]
    public async Task<IActionResult> DeleteContents(
        [FromBody] DeletionRequest request, CancellationToken cancellationToken)
    {
        if (request.DeletionId.Count == 0)
            return BadRequestError("Form hataydı! Lütfen geliştiricinizle iletişime geçin.");

        var result = await _write.DeleteContentsAsync(request.DeletionId, cancellationToken);
        if (!result.Success)
            return BadRequest(ApiResponse.ValidationErrors(new Dictionary<string, string[]>
            {
                ["message"] = ["Yük oluşturulmuş kayıt silinemez"],
            }));

        return OkMessage("Kayıt Başarıyla Silindi");
    }

    [HttpDelete("load_financial_item")]
    [RequiresPermission(PermissionAction.Delete, "load_management")]
    public async Task<IActionResult> DeleteFinancialItems(
        [FromBody] DeletionRequest request, CancellationToken cancellationToken)
    {
        if (request.DeletionId.Count == 0)
            return BadRequestError("Form hataydı! Lütfen geliştiricinizle iletişime geçin.");

        var result = await _write.DeleteFinancialItemsAsync(request.DeletionId, cancellationToken);
        if (!result.Success)
            return BadRequest(ApiResponse.ValidationErrors(new Dictionary<string, string[]>
            {
                ["message"] = ["Yük oluşturulmuş kayıt silinemez"],
            }));

        return OkMessage("Kayıt Başarıyla Silindi");
    }

    /// <summary>olsold: <c>LoadSave</c>/<c>LoadUpdate</c> FormRequest kuralları — ikisi de birebir aynı.</summary>
    /// <summary>
    /// Taslak mantığı: bir Teklif, tüm ayrıntılar (içerik/mali kalem/güzergah/
    /// gönderici-alıcı) doldurulmadan da Kaydet'le kaydedilip sonra kaldığı
    /// yerden devam edilebilir olmalı — bu yüzden burası artık yalnızca EN
    /// TEMEL alanları ve (varsa) girilmiş satırların kendi içindeki
    /// tutarlılığını zorunlu tutuyor. Teklifin Siber'e aktarılabilecek kadar
    /// TAM olup olmadığı — içerik/mali kalem boş olamaz, güzergah/taraf dolu
    /// olmalı vb. — zaten <see cref="OLS.Business.Services.TransferSiber.
    /// TransferSiberService"/>'in "Sibere Aktar" adımında (ValidateRequired)
    /// ayrı ayrı, açık mesajlarla kontrol ediliyor; burada tekrar etmiyoruz.
    /// </summary>
    /// <summary>
    /// Formdaki referans seçimleri hâlâ geçerli mi? Açılır listeler sayfa açılışında
    /// bir kez yüklendiği için, arada kaldırılan bir kayıt eski sekmede seçilebiliyor
    /// ve veritabanına ARTIK OLMAYAN bir id yazılıyordu; hata çok sonra, aktarım
    /// sırasında ve yanıltıcı bir metinle ("Departman boş olamaz") çıkıyordu.
    /// </summary>
    private async Task<Dictionary<string, string[]>?> ValidateReferencesAsync(
        LoadFormRequest form, CancellationToken cancellationToken)
    {
        var errors = await _loads.ValidateReferencesAsync(
            new LoadReferenceIds(
                form.DepartmentId, form.PaymentTypeId, form.StatusTypeId, form.WorkTypeId,
                form.LoadingTypeId, form.LoadTransferTypeId, form.InstructionId, form.RomorkTypeId,
                form.CustomerId, form.SenderId, form.ReceiverId, form.AgentId, form.CompanyPayFreightId),
            cancellationToken);

        return errors.Count > 0 ? errors : null;
    }

    private Dictionary<string, string[]>? Validate(LoadFormRequest form)
    {
        var errors = new Dictionary<string, string[]>();
        var required = Translator.Get("Bu alan boş bırakılamaz");

        if (form.WorkTypeId is null) errors["work_type_id"] = [required];
        if (form.LoadingTypeId is null) errors["loading_type_id"] = [required];
        if (form.PaymentTypeId is null) errors["payment_type_id"] = [required];
        if (form.StatusTypeId is null) errors["status_type_id"] = [required];
        if (form.OfferDate is null) errors["offer_date"] = [required];
        if (form.OfferValidityDate is null) errors["offer_validity_date"] = [required];
        if (form.MarketingNotificationDate is null) errors["marketing_notification_date"] = [required];
        if (form.CustomerId is null) errors["customer_id"] = [required];
        if (form.DepartmentId is null) errors["department_id"] = [required];

        // Olumsuz teklifin gerekçesi zorunlu: raporlamada tekliflerin NEDEN
        // kaybedildiğini görebilmek için (kullanıcı isteği). Diğer durumlarda
        // gerekçe hiç saklanmaz — bkz. LoadWriteService.NormalizeRejectionReason.
        if (form.StatusTypeId == NegativeStatusTypeId && string.IsNullOrWhiteSpace(form.RejectionReason))
            errors["rejection_reason"] = [Translator.Get("Olumsuz teklif için gerekçe zorunludur")];

        // "Olumlu" teklif = Siber'e aktarılıp Yük'e dönüşecek teklif; bu yüzden
        // dönüşüm için gereken alanlar burada zorunlu tutulur. Liste, Siber'in
        // kendi rezervasyon ekranında KIRMIZI işaretli alanlardan alındı
        // (kullanıcının paylaştığı ekran görüntüleri).
        //
        // Siber'de kırmızı OLDUĞU HÂLDE burada zorunlu tutulmayan iki alan var —
        // Acente ve Navlun Ödeyecek Firma: gerçek veride Olumlu tekliflerin
        // yalnızca %0,3'ünde (11/4114) ve %35'inde dolular, zorunlu yapmak
        // mevcut iş akışını kilitlerdi. Kullanıcı onay verirse eklenebilir.
        if (form.StatusTypeId == PositiveStatusTypeId)
        {
            if (form.SenderId is null) errors["sender_id"] = [required];
            if (form.ReceiverId is null) errors["receiver_id"] = [required];
            if (form.DepartureCountryId is null) errors["departure_country_id"] = [required];
            if (form.TargetCountryId is null) errors["target_country_id"] = [required];
            if (form.InstructionId is null) errors["instruction_id"] = [required];
            if (form.RomorkTypeId is null) errors["romork_type_id"] = [required];
            if (form.LoadTransferTypeId is null) errors["load_transfer_type_id"] = [required];
            if (form.WayOfWorking is null) errors["way_of_working"] = [required];
        }

        for (var i = 0; i < form.LoadContent.Count; i++)
        {
            var c = form.LoadContent[i];
            if (c.ProductTypeId is null) errors[$"load_content.{i}.product_type_id"] = [required];
            if (c.CaseTypeId is null) errors[$"load_content.{i}.case_type_id"] = [required];
            if (string.IsNullOrWhiteSpace(c.Quantity)) errors[$"load_content.{i}.quantity"] = [required];
            if (string.IsNullOrWhiteSpace(c.Width)) errors[$"load_content.{i}.width"] = [required];
            if (string.IsNullOrWhiteSpace(c.Height)) errors[$"load_content.{i}.height"] = [required];
            if (string.IsNullOrWhiteSpace(c.Length)) errors[$"load_content.{i}.length"] = [required];
            if (string.IsNullOrWhiteSpace(c.GrossWeight)) errors[$"load_content.{i}.gross_weight"] = [required];
            if (string.IsNullOrWhiteSpace(c.Lademeter)) errors[$"load_content.{i}.lademeter"] = [required];
            if (c.Stackable is null) errors[$"load_content.{i}.stackable"] = [required];
        }

        if (form.LoadFinancialItem.Count > 0)
        {
            // olsold: HERHANGİ bir kalemde net_price == 0 ise açıklama kuralı TÜM
            // kalemlere joker karakterle uygulanır (Laravel'in load_financial_item.*.
            // description davranışı — yalnızca 0 fiyatlı satıra değil, hepsine).
            var anyZeroNetPrice = form.LoadFinancialItem
                .Any(f => TurkishDecimal.Parse(f.NetPrice) == 0);
            var zeroPriceMessage = Translator.Get("Kalem tutarı 0 olduğu için açıklama zorunludur.");

            for (var i = 0; i < form.LoadFinancialItem.Count; i++)
            {
                var f = form.LoadFinancialItem[i];
                if (f.Buysell is null) errors[$"load_financial_item.{i}.buysell"] = [required];
                if (f.Item is null) errors[$"load_financial_item.{i}.item"] = [required];
                if (string.IsNullOrWhiteSpace(f.Quantity)) errors[$"load_financial_item.{i}.quantity"] = [required];
                if (f.TransportTypeId is null) errors[$"load_financial_item.{i}.transport_type_id"] = [required];
                if (f.Order is null) errors[$"load_financial_item.{i}.order"] = [required];
                if (string.IsNullOrWhiteSpace(f.NetPrice)) errors[$"load_financial_item.{i}.net_price"] = [required];
                if (string.IsNullOrWhiteSpace(f.TotalPrice)) errors[$"load_financial_item.{i}.total_price"] = [required];
                if (f.Currency is null) errors[$"load_financial_item.{i}.currency"] = [required];
                if (anyZeroNetPrice && string.IsNullOrWhiteSpace(f.Description))
                    errors[$"load_financial_item.{i}.description"] = [zeroPriceMessage];
            }
        }

        return errors.Count > 0 ? errors : null;
    }

    /// <summary>
    /// Yüklenen dosyaları diske yazar VE içeriklerini Siber arşivine gönderilmek
    /// üzere döner. İçerik burada okunur çünkü istek gövdesi yanıt üretilmeden
    /// önce kapanıyor.
    /// </summary>
    private async Task<(List<UploadedFile> Files, List<(string Name, byte[] Content)> Contents)>
        SaveFilesAsync(LoadFormRequest form, CancellationToken cancellationToken)
    {
        var uploaded = new List<UploadedFile>();
        var contents = new List<(string, byte[])>();

        foreach (var file in form.Files)
        {
            var stored = await _files.SaveDocumentAsync(file, cancellationToken);
            if (stored is null)
                continue;

            uploaded.Add(new UploadedFile(
                stored, Path.GetExtension(file.FileName).TrimStart('.'), file.FileName));

            using var buffer = new MemoryStream();
            await using (var source = file.OpenReadStream())
                await source.CopyToAsync(buffer, cancellationToken);

            contents.Add((file.FileName, buffer.ToArray()));
        }

        return (uploaded, contents);
    }

    /// <summary>
    /// Teklif eklerini Siber arşivine gönderir. Siber'e aktarımdan SONRA
    /// çağrılmalı: yeni teklifin siber_id'si o adımda oluşuyor ve arşiv kaydı
    /// ona bağlanıyor (bkz. LoadArchivePublisher).
    /// </summary>
    private async Task PushFilesToArchiveAsync(
        long loadId, IReadOnlyList<(string Name, byte[] Content)> contents,
        CancellationToken cancellationToken)
    {
        if (contents.Count == 0)
            return;

        var written = await _archive.PushAsync(loadId, null, contents, cancellationToken);

        if (written < contents.Count)
        {
            _logger.LogWarning(
                "Teklif {LoadId}: Siber arşivine {Basarili}/{Toplam} dosya yazılabildi.",
                loadId, written, contents.Count);
        }
    }
}
