using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.Business.Services.Loads;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;
using OLS.DataAccess.Siber;

namespace OLS.Business.Services.Cars;

/// <summary>
/// Araç modülü. olsold: <c>Front\Car\CarController</c>
/// Siber tarafında <c>skn_arac</c> tablosuyla senkron çalışır.
/// </summary>
public interface ICarService
{
    Task<object> ListAsync(CarListQuery query, CancellationToken cancellationToken = default);
    Task<CarDto?> SingleAsync(long id, CancellationToken cancellationToken = default);
    Task<CarDto> CreateAsync(CarWriteModel model, CancellationToken cancellationToken = default);
    Task<CarDto?> UpdateAsync(CarWriteModel model, CancellationToken cancellationToken = default);
    Task DeleteAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default);
}

public sealed record CarListQuery(string? Search, int? PerPage, int Page, string Path);

public sealed class CarWriteModel
{
    public long? Id { get; init; }
    public string? PlateNumber { get; init; }
    public int? CarType { get; init; }
    public int? RomorkType { get; init; }
    public int? VehicleOwner { get; init; }
    public int? VehicleStatus { get; init; }
    public string? CustomerId { get; init; }
    public double? Km { get; init; }
    public double? Width { get; init; }
    public double? Length { get; init; }
    public double? Height { get; init; }
    public double? Capacity { get; init; }

    /// <summary>Siber'e "kayıt giren" olarak yazılır (Auth::user()->siber_code karşılığı).</summary>
    public string? CurrentUserSiberCode { get; init; }
}

/// <summary>
/// olsold ilişki adları snake_case'e çevrilince sütun adlarını ezer:
/// <c>car_type</c>, <c>romork_type</c>, <c>vehicle_owner</c>, <c>vehicle_status</c>
/// artık nesnedir. <c>customer</c> ise ayrı bir anahtar (sütun adı customer_id).
/// </summary>
public sealed class CarDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("siber_id")] public string? SiberId { get; init; }
    [JsonPropertyName("plate_number")] public string? PlateNumber { get; init; }
    [JsonPropertyName("customer_id")] public string? CustomerId { get; init; }
    [JsonPropertyName("km")] public double? Km { get; init; }
    [JsonPropertyName("in_country")] public int? InCountry { get; init; }
    [JsonPropertyName("international")] public int? International { get; init; }
    [JsonPropertyName("width")] public double? Width { get; init; }
    [JsonPropertyName("length")] public double? Length { get; init; }
    [JsonPropertyName("height")] public double? Height { get; init; }
    [JsonPropertyName("capacity")] public double? Capacity { get; init; }
    [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; init; }
    [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; init; }

    [JsonPropertyName("car_type")] public NamedRefDto? CarType { get; init; }
    [JsonPropertyName("romork_type")] public NamedRefDto? RomorkType { get; init; }
    [JsonPropertyName("vehicle_owner")] public NamedRefDto? VehicleOwner { get; init; }
    [JsonPropertyName("vehicle_status")] public NamedRefDto? VehicleStatus { get; init; }
    [JsonPropertyName("customer")] public NamedRefDto? Customer { get; init; }
}

public sealed class CarService : ICarService
{
    private readonly OlsDbContext _db;
    private readonly ISiberCarRepository _siber;
    private readonly IClock _clock;

    public CarService(OlsDbContext db, ISiberCarRepository siber, IClock clock)
    {
        _db = db;
        _siber = siber;
        _clock = clock;
    }

    public async Task<object> ListAsync(
        CarListQuery query, CancellationToken cancellationToken = default)
    {
        var cars = _db.Cars.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
            cars = cars.Where(c => EF.Functions.ILike(c.PlateNumber!, $"%{query.Search}%"));

        var projected = cars.OrderByDescending(c => c.Id).Select(Project());

        return await projected.ToPagedOrListAsync(
            query.PerPage, query.Page, query.Path, cancellationToken);
    }

    public async Task<CarDto?> SingleAsync(long id, CancellationToken cancellationToken = default) =>
        await _db.Cars.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(Project())
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<CarDto> CreateAsync(
        CarWriteModel model, CancellationToken cancellationToken = default)
    {
        var siberId = _siber.IsConfigured
            ? (await _siber.GenerateAracIdAsync(cancellationToken)).ToString()
            : Guid.NewGuid().ToString();

        var car = new Car
        {
            SiberId = siberId,
            PlateNumber = model.PlateNumber,
            CarType = model.CarType,
            RomorkType = model.RomorkType,
            VehicleOwner = model.VehicleOwner,
            VehicleStatus = model.VehicleStatus,
            CustomerId = model.CustomerId,
            Km = model.Km,
            // olsold bu iki alanı her zaman 1 yazıyor (formda yok).
            InCountry = 1,
            International = 1,
            Width = model.Width,
            Length = model.Length,
            Height = model.Height,
            Capacity = model.Capacity,
            CreatedAt = _clock.Now,
            UpdatedAt = _clock.Now,
        };

        _db.Cars.Add(car);
        await _db.SaveChangesAsync(cancellationToken);

        await SyncToSiberAsync(car, model.CurrentUserSiberCode, isNew: true, cancellationToken);

        return (await SingleAsync(car.Id, cancellationToken))!;
    }

    public async Task<CarDto?> UpdateAsync(
        CarWriteModel model, CancellationToken cancellationToken = default)
    {
        var id = model.Id ?? throw new ArgumentException("Id zorunlu", nameof(model));

        var car = await _db.Cars.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (car is null)
            return null;

        car.PlateNumber = model.PlateNumber;
        car.CarType = model.CarType;
        car.RomorkType = model.RomorkType;
        car.VehicleOwner = model.VehicleOwner;
        car.VehicleStatus = model.VehicleStatus;
        car.CustomerId = model.CustomerId;
        car.Km = model.Km;
        car.InCountry = 1;
        car.International = 1;
        car.Width = model.Width;
        car.Length = model.Length;
        car.Height = model.Height;
        car.Capacity = model.Capacity;
        car.UpdatedAt = _clock.Now;

        await _db.SaveChangesAsync(cancellationToken);

        await SyncToSiberAsync(car, model.CurrentUserSiberCode, isNew: false, cancellationToken);

        return await SingleAsync(car.Id, cancellationToken);
    }

    /// <summary>
    /// olsold araç silmede Siber'e dokunmuyordu (skn_arac satırı kalıyor).
    /// Aynı davranışı koruyoruz: Siber'de araca bağlı sefer geçmişi olabilir.
    /// </summary>
    public async Task DeleteAsync(
        IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        var cars = await _db.Cars.Where(c => ids.Contains(c.Id)).ToListAsync(cancellationToken);

        _db.Cars.RemoveRange(cars);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task SyncToSiberAsync(
        Car car, string? userSiberCode, bool isNew, CancellationToken cancellationToken)
    {
        if (!_siber.IsConfigured || car.SiberId is null)
            return;

        // Siber referansları kod değeriyle tutuyor; yerel id'den koda çeviriyoruz.
        var carTypeCode = await _db.CarTypes.Where(t => t.Id == car.CarType)
            .Select(t => t.Code).FirstOrDefaultAsync(cancellationToken);
        var romorkCode = await _db.RomorkTypes.Where(t => t.Id == car.RomorkType)
            .Select(t => t.Code).FirstOrDefaultAsync(cancellationToken);
        var ownerCode = await _db.CarOwners.Where(t => t.Id == car.VehicleOwner)
            .Select(t => t.Code).FirstOrDefaultAsync(cancellationToken);
        var statusCode = await _db.CarStatusTypes.Where(t => t.Id == car.VehicleStatus)
            .Select(t => t.Code).FirstOrDefaultAsync(cancellationToken);

        var arac = new SiberArac
        {
            AracId = car.SiberId,
            PlakaNo = car.PlateNumber,
            AracTip = carTypeCode,
            RomorkCins = romorkCode,
            AracSahip = ownerCode,
            AracDurum = statusCode,
            BagliFirmaId = car.CustomerId,
            Km = car.Km,
            En = car.Width,
            Boy = car.Length,
            Yukseklik = car.Height,
            Kapasite = car.Capacity,
            KayitGirisTarih = _clock.Now,
            KayitGiren = userSiberCode,
        };

        if (isNew)
            await _siber.InsertAracAsync(arac, cancellationToken);
        else
            await _siber.UpdateAracAsync(arac, cancellationToken);
    }

    private System.Linq.Expressions.Expression<Func<Car, CarDto>> Project() => c => new CarDto
    {
        Id = c.Id,
        SiberId = c.SiberId,
        PlateNumber = c.PlateNumber,
        CustomerId = c.CustomerId,
        Km = c.Km,
        InCountry = c.InCountry,
        International = c.International,
        Width = c.Width,
        Length = c.Length,
        Height = c.Height,
        Capacity = c.Capacity,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,

        CarType = _db.CarTypes.Where(t => t.Id == c.CarType)
            .Select(t => new NamedRefDto { Id = t.Id, Name = t.Name, SiberId = t.SiberId })
            .FirstOrDefault(),
        RomorkType = _db.RomorkTypes.Where(t => t.Id == c.RomorkType)
            .Select(t => new NamedRefDto { Id = t.Id, Name = t.Name, Code = t.Code, SiberId = t.SiberId })
            .FirstOrDefault(),
        VehicleOwner = _db.CarOwners.Where(t => t.Id == c.VehicleOwner)
            .Select(t => new NamedRefDto { Id = t.Id, Name = t.Name, SiberId = t.SiberId })
            .FirstOrDefault(),
        VehicleStatus = _db.CarStatusTypes.Where(t => t.Id == c.VehicleStatus)
            .Select(t => new NamedRefDto { Id = t.Id, Name = t.Name, SiberId = t.SiberId })
            .FirstOrDefault(),
        // cars.customer_id metin sütunu ve Siber firmaid'sini tutuyor.
        Customer = _db.Accounts.Where(a => a.SiberId == c.CustomerId)
            .Select(a => new NamedRefDto { Id = a.Id, Name = a.Name, SiberId = a.SiberId })
            .FirstOrDefault(),
    };
}
