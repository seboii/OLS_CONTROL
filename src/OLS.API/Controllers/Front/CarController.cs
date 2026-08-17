using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OLS.API.Filters;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.Cars;
using OLS.DataAccess.Context;

namespace OLS.API.Controllers.Front;

/// <summary>
/// olsold: <c>Front\Car\CarController</c> — Araç yönetimi.
///
///   POST   /api/v1/car        save
///   PUT    /api/v1/car        update  (id gövdede)
///   DELETE /api/v1/car        delete  (gövdede deletion_id)
///   GET    /api/v1/car        all     (?search= plakada ILIKE)
///   GET    /api/v1/car/{id}   single
///
/// Yetki slug'ı: olsold <c>car_management</c> kullanıyordu ama kaynak kodda
/// "bu alan ekli degil calısmaz" notuyla itiraf edildiği üzere bu slug
/// olsold'un KENDİ <c>user_permission_pages</c>'inde tanımlı değildi (fiilen
/// herkese açıktı). Bu HEDEF'te düzeltildi: <c>car_management</c> gerçekten
/// seed ediliyor (bkz. DbSeeder.cs, "Araç Yönetimi"), bu yüzden aşağıdaki
/// <c>[RequiresPermission]</c> öznitelikleri gerçekten uygulanıyor.
/// </summary>
[Authorize]
[Route("api/v1/car")]
public sealed class CarController : ApiControllerBase
{
    private readonly ICarService _cars;
    private readonly OlsDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CarController(ICarService cars, OlsDbContext db, ICurrentUser currentUser)
    {
        _cars = cars;
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    [RequiresPermission(PermissionAction.Read, "car_management")]
    public async Task<IActionResult> All(
        [FromQuery] string? search,
        [FromQuery(Name = "per_page")] int? perPage,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _cars.ListAsync(
            new CarListQuery(search, perPage, page, CurrentPath), cancellationToken);

        return Ok(result, "Kayıtlar");
    }

    [HttpGet("{id:long}")]
    [RequiresPermission(PermissionAction.Read, "car_management")]
    public async Task<IActionResult> Single(long id, CancellationToken cancellationToken)
    {
        var car = await _cars.SingleAsync(id, cancellationToken);

        return car is null ? NotFoundError() : Ok(car, "Kayıtlar");
    }

    public sealed class CarRequest
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("plate_number")] public string? PlateNumber { get; set; }
        [JsonPropertyName("car_type")] public int? CarType { get; set; }
        [JsonPropertyName("romork_type")] public int? RomorkType { get; set; }
        [JsonPropertyName("vehicle_owner")] public int? VehicleOwner { get; set; }
        [JsonPropertyName("vehicle_status")] public int? VehicleStatus { get; set; }
        [JsonPropertyName("customer_id")] public string? CustomerId { get; set; }
        [JsonPropertyName("km")] public double? Km { get; set; }
        [JsonPropertyName("width")] public double? Width { get; set; }
        [JsonPropertyName("length")] public double? Length { get; set; }
        [JsonPropertyName("height")] public double? Height { get; set; }
        [JsonPropertyName("capacity")] public double? Capacity { get; set; }
    }

    [HttpPost]
    [RequiresPermission(PermissionAction.Create, "car_management")]
    public async Task<IActionResult> Save(
        [FromBody] CarRequest request, CancellationToken cancellationToken)
    {
        if (Validate(request) is { } errors)
            return BadRequest(ApiResponse.ValidationErrors(errors));

        var result = await _cars.CreateAsync(
            await ToModelAsync(request, cancellationToken), cancellationToken);

        return BuildSaveResponse(result, "Kayıt Başarılı");
    }

    [HttpPut]
    [HttpPost("update")]
    [RequiresPermission(PermissionAction.Update, "car_management")]
    public async Task<IActionResult> Update(
        [FromBody] CarRequest request, CancellationToken cancellationToken)
    {
        if (request.Id is null)
            return BadRequest(ApiResponse.ValidationErrors(new Dictionary<string, string[]>
            {
                ["id"] = [Translator.Get("Bu alan boş bırakılamaz")],
            }));

        if (Validate(request) is { } errors)
            return BadRequest(ApiResponse.ValidationErrors(errors));

        var result = await _cars.UpdateAsync(
            await ToModelAsync(request, cancellationToken), cancellationToken);

        return BuildSaveResponse(result, "Güncelleme Başarılı");
    }

    /// <summary>olsold: <c>CarSave</c>/<c>CarUpdate</c> FormRequest kuralları — ikisi de birebir aynı.</summary>
    private Dictionary<string, string[]>? Validate(CarRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.PlateNumber))
            errors["plate_number"] = [Translator.Get("Plaka numarası boş olamaz")];
        if (request.CarType is null)
            errors["car_type"] = [Translator.Get("Araç tipi boş olamaz")];
        if (request.RomorkType is null)
            errors["romork_type"] = [Translator.Get("Romork tipi boş olamaz")];
        if (request.VehicleOwner is null)
            errors["vehicle_owner"] = [Translator.Get("Araç sahibi boş olamaz")];
        if (request.VehicleStatus is null)
            errors["vehicle_status"] = [Translator.Get("Araç durumu boş olamaz")];
        if (string.IsNullOrWhiteSpace(request.CustomerId))
            errors["customer_id"] = [Translator.Get("Firma boş olamaz")];
        if (request.Km is null)
            errors["km"] = [Translator.Get("Km boş olamaz")];
        if (request.Width is null)
            errors["width"] = [Translator.Get("En boş olamaz")];
        if (request.Length is null)
            errors["length"] = [Translator.Get("Boy boş olamaz")];
        if (request.Height is null)
            errors["height"] = [Translator.Get("Yükseklik boş olamaz")];
        if (request.Capacity is null)
            errors["capacity"] = [Translator.Get("Kapasite hedef boş olamaz")];

        return errors.Count > 0 ? errors : null;
    }

    /// <summary>olsold: <c>plate_number.unique</c> — "Bu plaka numarası zaten kayıtlı".</summary>
    private IActionResult BuildSaveResponse(CarSaveResult result, string successKey)
    {
        if (result.PlateNumberTaken)
            return UnprocessableEntity(ApiResponse.ValidationErrors(new Dictionary<string, string[]>
            {
                ["plate_number"] = [Translator.Get("Bu plaka numarası zaten kayıtlı")],
            }));

        return result.Car is null ? NotFoundError() : Ok(result.Car, successKey);
    }

    [HttpDelete]
    [RequiresPermission(PermissionAction.Delete, "car_management")]
    public async Task<IActionResult> Delete(
        [FromBody] DeletionRequest request, CancellationToken cancellationToken)
    {
        if (request.DeletionId.Count == 0)
            return BadRequestError("Form hataydı! Lütfen geliştiricinizle iletişime geçin.");

        await _cars.DeleteAsync(request.DeletionId, cancellationToken);

        return OkMessage("Kayıt Başarıyla Silindi");
    }

    /// <summary>Siber'e "kayıt giren" olarak yazılacak kullanıcı kodunu ekler.</summary>
    private async Task<CarWriteModel> ToModelAsync(
        CarRequest request, CancellationToken cancellationToken)
    {
        string? siberCode = null;

        if (_currentUser.Id is { } userId)
        {
            siberCode = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.SiberCode)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new CarWriteModel
        {
            Id = request.Id,
            PlateNumber = request.PlateNumber,
            CarType = request.CarType,
            RomorkType = request.RomorkType,
            VehicleOwner = request.VehicleOwner,
            VehicleStatus = request.VehicleStatus,
            CustomerId = request.CustomerId,
            Km = request.Km,
            Width = request.Width,
            Length = request.Length,
            Height = request.Height,
            Capacity = request.Capacity,
            CurrentUserSiberCode = siberCode,
        };
    }
}
