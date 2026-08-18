using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.Business.Services.Loads;
using OLS.DataAccess.Context;

namespace OLS.Business.Services.Expeditions;

/// <summary>
/// Sefer (pozisyon) modülü — okuma tarafı.
/// olsold: <c>Front\Expedition\ExpeditionController</c>
///
/// Siber'deki <c>skn_pozisyon</c> kayıtları buraya aktarılır.
/// </summary>
public interface IExpeditionService
{
    Task<object> ListAsync(ExpeditionListQuery query, CancellationToken cancellationToken = default);
    Task<ExpeditionDetailDto?> SingleAsync(long id, CancellationToken cancellationToken = default);
    Task<object> MovementsAsync(long expeditionId, CancellationToken cancellationToken = default);
}

public sealed record ExpeditionListQuery(
    string? Search, int? WorkTypeId, DateOnly? DateFrom, DateOnly? DateTo, int? PerPage, int Page, string Path);

public sealed class ExpeditionListItemDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("expedition_number")] public string? ExpeditionNumber { get; init; }
    [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; init; }

    [JsonPropertyName("work_type")] public NamedRefDto? WorkType { get; init; }
    [JsonPropertyName("expedition_type_id")] public NamedRefDto? ExpeditionTypeId { get; init; }
    [JsonPropertyName("status_id")] public NamedRefDto? StatusId { get; init; }
    [JsonPropertyName("department_id")] public NamedRefDto? DepartmentId { get; init; }
    [JsonPropertyName("romork_id")] public CarRefDto? RomorkId { get; init; }
    [JsonPropertyName("start_city_id")] public CityRefDto? StartCityId { get; init; }
    [JsonPropertyName("end_city_id")] public CityRefDto? EndCityId { get; init; }
}

public sealed class ExpeditionDetailDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("expedition_id")] public string? ExpeditionId { get; init; }
    [JsonPropertyName("expedition_number")] public string? ExpeditionNumber { get; init; }
    [JsonPropertyName("sefer_id")] public string? SeferId { get; init; }
    [JsonPropertyName("year_week")] public string? YearWeek { get; init; }
    [JsonPropertyName("registration_login_date")] public DateOnly? RegistrationLoginDate { get; init; }
    [JsonPropertyName("car_exit_date")] public DateOnly? CarExitDate { get; init; }
    [JsonPropertyName("release_date")] public DateOnly? ReleaseDate { get; init; }
    [JsonPropertyName("loading_date")] public DateOnly? LoadingDate { get; init; }
    [JsonPropertyName("return_date")] public DateOnly? ReturnDate { get; init; }
    [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; init; }

    [JsonPropertyName("work_type")] public NamedRefDto? WorkType { get; init; }
    [JsonPropertyName("expedition_type_id")] public NamedRefDto? ExpeditionTypeId { get; init; }
    [JsonPropertyName("status_id")] public NamedRefDto? StatusId { get; init; }
    [JsonPropertyName("department_id")] public NamedRefDto? DepartmentId { get; init; }
    [JsonPropertyName("romork_id")] public CarRefDto? RomorkId { get; init; }
    [JsonPropertyName("start_city_id")] public CityRefDto? StartCityId { get; init; }
    [JsonPropertyName("load_city_id")] public CityRefDto? LoadCityId { get; init; }
    [JsonPropertyName("end_city_id")] public CityRefDto? EndCityId { get; init; }
}

public sealed class CarRefDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("plate_number")] public string? PlateNumber { get; init; }
    [JsonPropertyName("siber_id")] public string? SiberId { get; init; }
}

public sealed class CityRefDto
{
    [JsonPropertyName("id")] public Guid Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
}

public sealed class ExpeditionService : IExpeditionService
{
    private readonly OlsDbContext _db;

    public ExpeditionService(OlsDbContext db) => _db = db;

    public async Task<object> ListAsync(
        ExpeditionListQuery query, CancellationToken cancellationToken = default)
    {
        var expeditions = _db.Expeditions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search}%";

            // olsold: sefer numarası VEYA römorkun plakası
            var matchingCarIds = _db.Cars
                .Where(c => EF.Functions.ILike(c.PlateNumber!, pattern))
                .Select(c => (int)c.Id);

            expeditions = expeditions.Where(e =>
                EF.Functions.ILike(e.ExpeditionNumber!, pattern) ||
                (e.RomorkId != null && matchingCarIds.Contains(e.RomorkId.Value)));
        }

        if (query.WorkTypeId is { } workTypeId)
            expeditions = expeditions.Where(e => e.WorkType == workTypeId);

        if (query.DateFrom is { } dateFrom)
        {
            var from = dateFrom.ToDateTime(TimeOnly.MinValue);
            expeditions = expeditions.Where(e => e.CreatedAt >= from);
        }

        if (query.DateTo is { } dateTo)
        {
            var to = dateTo.AddDays(1).ToDateTime(TimeOnly.MinValue);
            expeditions = expeditions.Where(e => e.CreatedAt < to);
        }

        var projected = expeditions
            .OrderByDescending(e => e.Id)
            .Select(e => new ExpeditionListItemDto
            {
                Id = e.Id,
                ExpeditionNumber = e.ExpeditionNumber,
                CreatedAt = e.CreatedAt,
                WorkType = _db.WorkTypes.Where(w => w.Id == e.WorkType)
                    .Select(w => new NamedRefDto { Id = w.Id, Name = w.Name, Code = w.Code })
                    .FirstOrDefault(),
                ExpeditionTypeId = _db.ExpeditionTypes.Where(t => t.Id == e.ExpeditionTypeId)
                    .Select(t => new NamedRefDto { Id = t.Id, Name = t.Name, Code = t.Code })
                    .FirstOrDefault(),
                StatusId = _db.ExpeditionStatuses.Where(s => s.Id == e.StatusId)
                    .Select(s => new NamedRefDto { Id = s.Id, Name = s.Name })
                    .FirstOrDefault(),
                DepartmentId = _db.Departments.Where(d => d.Id == e.DepartmentId)
                    .Select(d => new NamedRefDto { Id = d.Id, Name = d.Name })
                    .FirstOrDefault(),
                RomorkId = _db.Cars.Where(c => c.Id == e.RomorkId)
                    .Select(c => new CarRefDto { Id = c.Id, PlateNumber = c.PlateNumber, SiberId = c.SiberId })
                    .FirstOrDefault(),
                StartCityId = _db.Cities.Where(c => c.Id == e.StartCityId)
                    .Select(c => new CityRefDto { Id = c.Id, Name = c.Name })
                    .FirstOrDefault(),
                EndCityId = _db.Cities.Where(c => c.Id == e.EndCityId)
                    .Select(c => new CityRefDto { Id = c.Id, Name = c.Name })
                    .FirstOrDefault(),
            });

        return await projected.ToPagedOrListAsync(
            query.PerPage, query.Page, query.Path, cancellationToken);
    }

    public async Task<ExpeditionDetailDto?> SingleAsync(
        long id, CancellationToken cancellationToken = default)
    {
        var e = await _db.Expeditions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (e is null)
            return null;

        return new ExpeditionDetailDto
        {
            Id = e.Id,
            ExpeditionId = e.ExpeditionId,
            ExpeditionNumber = e.ExpeditionNumber,
            SeferId = e.SeferId,
            YearWeek = e.YearWeek,
            RegistrationLoginDate = e.RegistrationLoginDate,
            CarExitDate = e.CarExitDate,
            ReleaseDate = e.ReleaseDate,
            LoadingDate = e.LoadingDate,
            ReturnDate = e.ReturnDate,
            CreatedAt = e.CreatedAt,

            WorkType = await _db.WorkTypes.AsNoTracking().Where(w => w.Id == e.WorkType)
                .Select(w => new NamedRefDto { Id = w.Id, Name = w.Name, Code = w.Code })
                .FirstOrDefaultAsync(cancellationToken),
            ExpeditionTypeId = await _db.ExpeditionTypes.AsNoTracking().Where(t => t.Id == e.ExpeditionTypeId)
                .Select(t => new NamedRefDto { Id = t.Id, Name = t.Name, Code = t.Code })
                .FirstOrDefaultAsync(cancellationToken),
            StatusId = await _db.ExpeditionStatuses.AsNoTracking().Where(s => s.Id == e.StatusId)
                .Select(s => new NamedRefDto { Id = s.Id, Name = s.Name })
                .FirstOrDefaultAsync(cancellationToken),
            DepartmentId = await _db.Departments.AsNoTracking().Where(d => d.Id == e.DepartmentId)
                .Select(d => new NamedRefDto { Id = d.Id, Name = d.Name })
                .FirstOrDefaultAsync(cancellationToken),
            RomorkId = await _db.Cars.AsNoTracking().Where(c => c.Id == e.RomorkId)
                .Select(c => new CarRefDto { Id = c.Id, PlateNumber = c.PlateNumber, SiberId = c.SiberId })
                .FirstOrDefaultAsync(cancellationToken),
            StartCityId = await CityAsync(e.StartCityId, cancellationToken),
            LoadCityId = await CityAsync(e.LoadCityId, cancellationToken),
            EndCityId = await CityAsync(e.EndCityId, cancellationToken),
        };
    }

    /// <summary>olsold: GET /expedition/{id}/movements</summary>
    public async Task<object> MovementsAsync(
        long expeditionId, CancellationToken cancellationToken = default) =>
        await _db.ExpeditionMovements.AsNoTracking()
            .Where(m => m.ExpeditionId == expeditionId)
            .OrderByDescending(m => m.Id)
            .ToListAsync(cancellationToken);

    private async Task<CityRefDto?> CityAsync(Guid? id, CancellationToken cancellationToken) =>
        id is null
            ? null
            : await _db.Cities.AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new CityRefDto { Id = c.Id, Name = c.Name })
                .FirstOrDefaultAsync(cancellationToken);
}
