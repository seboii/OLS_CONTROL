using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.Business.Services.Expeditions;

/// <summary>
/// Sefer ve yük hareketleri.
/// olsold: <c>ExpeditionController::expeditionMovement*</c> +
/// <c>Front\LoadTransfer\LoadTransferMovementController</c>
///
/// İki tablo birbirine bağlı: bir SEFER hareketi kaydedildiğinde, o sefere
/// bağlı her yük için ayrıca bir YÜK hareketi üretilir
/// (<c>expedition_movement_id</c> ile eşlenir).
///
/// Bu iki modülün yanıt zarfı diğerlerinden FARKLI:
/// <c>{status, message, data, deleted_movements}</c> — silinmiş kayıtlar
/// (soft delete) ayrı bir anahtarla birlikte dönüyor. Arayüz buna bağlı
/// olduğu için korunmuştur.
/// </summary>
public interface IMovementService
{
    Task<MovementListDto<ExpeditionMovementDto>> ExpeditionMovementsAsync(
        long expeditionId, long? destinationId, long? expeditionStatusId,
        CancellationToken cancellationToken = default);

    Task<MovementResult<ExpeditionMovementDto>> SaveExpeditionMovementAsync(
        ExpeditionMovementInput input, long userId, CancellationToken cancellationToken = default);

    Task<bool> DeleteExpeditionMovementAsync(
        long expeditionId, long movementId, CancellationToken cancellationToken = default);

    Task<MovementListDto<LoadMovementDto>> LoadMovementsAsync(
        LoadMovementQuery query, CancellationToken cancellationToken = default);

    Task<LoadMovementDto?> SingleLoadMovementAsync(long id, CancellationToken cancellationToken = default);

    Task<MovementResult<LoadMovementDto>> SaveLoadMovementAsync(
        LoadMovementInput input, long userId, CancellationToken cancellationToken = default);

    Task<MovementResult<LoadMovementDto>> UpdateLoadMovementAsync(
        long id, LoadMovementInput input, CancellationToken cancellationToken = default);

    Task<bool> DeleteLoadMovementsAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default);
}

public sealed record LoadMovementQuery(
    long? LoadId, long? LoadTransferId, long? DestinationId, long? ExpeditionStatusId);

public sealed class ExpeditionMovementInput
{
    public long? ExpeditionId { get; set; }
    public long? DestinationId { get; set; }
    public long? ExpeditionStatusId { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
}

/// <summary>
/// Yük hareketi yükü <b>numarasıyla</b> alır (<c>load_number</c>), id ile değil.
/// Güncellemede alanlar KISMİ: yalnızca gönderilenler değişir.
/// </summary>
public sealed class LoadMovementInput
{
    public string? LoadNumber { get; set; }
    public long? LoadTransferId { get; set; }
    public long? DestinationId { get; set; }
    public long? ExpeditionStatusId { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }

    public bool HasLoadNumber { get; set; }
    public bool HasLoadTransferId { get; set; }
    public bool HasDestinationId { get; set; }
    public bool HasExpeditionStatusId { get; set; }
    public bool HasDescription { get; set; }
    public bool HasAddress { get; set; }
}

public sealed record MovementResult<T>(T? Data, string? NotFoundMessage, IDictionary<string, string[]>? Errors)
{
    public bool IsSuccess => Data is not null;
}

public sealed class MovementListDto<T>
{
    [JsonPropertyName("data")] public IReadOnlyList<T> Data { get; init; } = [];
    [JsonPropertyName("deleted_movements")] public IReadOnlyList<T> DeletedMovements { get; init; } = [];
}

public sealed class ExpeditionMovementDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("expedition_id")] public long? ExpeditionId { get; init; }
    [JsonPropertyName("destination_id")] public long DestinationId { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("user_id")] public long UserId { get; init; }
    [JsonPropertyName("address")] public string? Address { get; init; }
    [JsonPropertyName("expedition_status_id")] public long ExpeditionStatusId { get; init; }
    [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; init; }
    [JsonPropertyName("deleted_at")] public DateTime? DeletedAt { get; init; }

    [JsonPropertyName("expedition")] public MappedExpeditionDto? Expedition { get; init; }
    [JsonPropertyName("destination")] public MovementRefDto? Destination { get; init; }
    [JsonPropertyName("user")] public MovementRefDto? User { get; init; }
    [JsonPropertyName("expedition_status")] public MovementRefDto? ExpeditionStatus { get; init; }
}

public sealed class LoadMovementDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("load_id")] public long LoadId { get; init; }
    [JsonPropertyName("load_transfer_id")] public long? LoadTransferId { get; init; }
    [JsonPropertyName("destination_id")] public long DestinationId { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("user_id")] public long UserId { get; init; }
    [JsonPropertyName("address")] public string? Address { get; init; }
    [JsonPropertyName("expedition_status_id")] public long ExpeditionStatusId { get; init; }
    [JsonPropertyName("expedition_movement_id")] public long? ExpeditionMovementId { get; init; }
    [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; init; }
    [JsonPropertyName("deleted_at")] public DateTime? DeletedAt { get; init; }

    /// <summary>Kaynak ilişki adı <c>loadRelation</c> — sütunla çakışmasın diye.</summary>
    [JsonPropertyName("load_relation")] public MovementRefDto? LoadRelation { get; init; }
    [JsonPropertyName("load_transfer")] public MovementRefDto? LoadTransfer { get; init; }
    [JsonPropertyName("destination")] public MovementRefDto? Destination { get; init; }
    [JsonPropertyName("user")] public MovementRefDto? User { get; init; }
    [JsonPropertyName("expedition_status")] public MovementRefDto? ExpeditionStatus { get; init; }
    [JsonPropertyName("expedition_movement")] public ExpeditionMovementDto? ExpeditionMovement { get; init; }
}

public sealed class MovementRefDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
}

public sealed class MovementService : IMovementService
{
    private readonly OlsDbContext _db;
    private readonly IClock _clock;

    public MovementService(OlsDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    // ── Sefer hareketleri ───────────────────────────────────────────────────

    public async Task<MovementListDto<ExpeditionMovementDto>> ExpeditionMovementsAsync(
        long expeditionId, long? destinationId, long? expeditionStatusId,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ExpeditionMovements.AsNoTracking()
            .Where(m => m.DeletedAt == null && m.ExpeditionId == expeditionId);

        if (destinationId is { } destination)
            query = query.Where(m => m.DestinationId == destination);

        if (expeditionStatusId is { } status)
            query = query.Where(m => m.ExpeditionStatusId == status);

        var rows = await query
            .OrderByDescending(m => m.CreatedAt)
            .Select(ProjectExpeditionMovement())
            .ToListAsync(cancellationToken);

        // Kaynak silinmiş hareketleri SEFERE GÖRE SÜZMÜYOR — tüm silinmişler
        // dönüyor. Davranış korundu.
        var deleted = await _db.ExpeditionMovements.AsNoTracking()
            .Where(m => m.DeletedAt != null)
            .OrderByDescending(m => m.DeletedAt)
            .Select(ProjectExpeditionMovement())
            .ToListAsync(cancellationToken);

        return new MovementListDto<ExpeditionMovementDto> { Data = rows, DeletedMovements = deleted };
    }

    public async Task<MovementResult<ExpeditionMovementDto>> SaveExpeditionMovementAsync(
        ExpeditionMovementInput input, long userId, CancellationToken cancellationToken = default)
    {
        var expedition = await _db.Expeditions.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == input.ExpeditionId, cancellationToken);

        if (expedition is null)
            return new MovementResult<ExpeditionMovementDto>(
                null, "Belirtilen sefer numarasına ait kayıt bulunamadı", null);

        var now = _clock.Now;

        var movement = new ExpeditionMovement
        {
            ExpeditionId = expedition.Id,
            DestinationId = input.DestinationId!.Value,
            Description = input.Description,
            UserId = userId,
            Address = input.Address,
            ExpeditionStatusId = input.ExpeditionStatusId!.Value,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.ExpeditionMovements.Add(movement);
        await _db.SaveChangesAsync(cancellationToken);

        // Sefere bağlı her yük için ayrıca bir yük hareketi üretilir.
        var key = expedition.Id.ToString();

        var mappedTransferIds = await _db.ExpeditionLoadMappings.AsNoTracking()
            .Where(m => m.ExpeditionId == key)
            .Select(m => m.LoadTransferId)
            .ToListAsync(cancellationToken);

        var transferIds = mappedTransferIds
            .Where(id => long.TryParse(id, out _))
            .Select(long.Parse!)
            .Distinct()
            .ToList();

        if (transferIds.Count > 0)
        {
            var transfers = await _db.LoadTransfers.AsNoTracking()
                .Where(t => transferIds.Contains(t.Id))
                .Select(t => new { t.Id, t.LoadNumberWorkType })
                .ToListAsync(cancellationToken);

            var numbers = transfers
                .Where(t => t.LoadNumberWorkType != null)
                .Select(t => t.LoadNumberWorkType!)
                .Distinct()
                .ToList();

            var loads = await _db.Loads.AsNoTracking()
                .Where(l => l.LoadNumber != null && numbers.Contains(l.LoadNumber))
                .Select(l => new { l.Id, l.LoadNumber })
                .ToListAsync(cancellationToken);

            var loadByNumber = loads
                .GroupBy(l => l.LoadNumber!)
                .ToDictionary(g => g.Key, g => g.First().Id);

            foreach (var transfer in transfers)
            {
                // KAYNAK FARKI: olsold eşleşen yük yoksa null üzerinde ->id
                // okuyup 500 veriyordu. Port böyle satırları atlar.
                if (transfer.LoadNumberWorkType is null ||
                    !loadByNumber.TryGetValue(transfer.LoadNumberWorkType, out var loadId))
                    continue;

                _db.LoadTransferMovements.Add(new LoadTransferMovement
                {
                    LoadId = loadId,
                    LoadTransferId = transfer.Id,
                    DestinationId = movement.DestinationId,
                    ExpeditionStatusId = movement.ExpeditionStatusId,
                    Description = input.Description,
                    Address = input.Address,
                    UserId = userId,
                    ExpeditionMovementId = movement.Id,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        var dto = await _db.ExpeditionMovements.AsNoTracking()
            .Where(m => m.Id == movement.Id)
            .Select(ProjectExpeditionMovement())
            .FirstOrDefaultAsync(cancellationToken);

        return new MovementResult<ExpeditionMovementDto>(dto, null, null);
    }

    /// <summary>Soft delete — kaynak <c>SoftDeletes</c> kullanıyor.</summary>
    public async Task<bool> DeleteExpeditionMovementAsync(
        long expeditionId, long movementId, CancellationToken cancellationToken = default)
    {
        var movement = await _db.ExpeditionMovements
            .FirstOrDefaultAsync(
                m => m.ExpeditionId == expeditionId && m.Id == movementId && m.DeletedAt == null,
                cancellationToken);

        if (movement is null)
            return false;

        movement.DeletedAt = _clock.Now;
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }

    // ── Yük hareketleri ─────────────────────────────────────────────────────

    public async Task<MovementListDto<LoadMovementDto>> LoadMovementsAsync(
        LoadMovementQuery query, CancellationToken cancellationToken = default)
    {
        var movements = _db.LoadTransferMovements.AsNoTracking()
            .Where(m => m.DeletedAt == null);

        if (query.LoadId is { } loadId)
            movements = movements.Where(m => m.LoadId == loadId);

        if (query.LoadTransferId is { } transferId)
            movements = movements.Where(m => m.LoadTransferId == transferId);

        if (query.DestinationId is { } destinationId)
            movements = movements.Where(m => m.DestinationId == destinationId);

        if (query.ExpeditionStatusId is { } statusId)
            movements = movements.Where(m => m.ExpeditionStatusId == statusId);

        var rows = await movements
            .OrderByDescending(m => m.CreatedAt)
            .Select(ProjectLoadMovement())
            .ToListAsync(cancellationToken);

        var deleted = await _db.LoadTransferMovements.AsNoTracking()
            .Where(m => m.DeletedAt != null)
            .OrderByDescending(m => m.DeletedAt)
            .Select(ProjectLoadMovement())
            .ToListAsync(cancellationToken);

        return new MovementListDto<LoadMovementDto> { Data = rows, DeletedMovements = deleted };
    }

    public async Task<LoadMovementDto?> SingleLoadMovementAsync(
        long id, CancellationToken cancellationToken = default) =>
        await _db.LoadTransferMovements.AsNoTracking()
            .Where(m => m.Id == id && m.DeletedAt == null)
            .Select(ProjectLoadMovement())
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<MovementResult<LoadMovementDto>> SaveLoadMovementAsync(
        LoadMovementInput input, long userId, CancellationToken cancellationToken = default)
    {
        var load = await _db.Loads.AsNoTracking()
            .FirstOrDefaultAsync(l => l.LoadNumber == input.LoadNumber, cancellationToken);

        if (load is null)
            return new MovementResult<LoadMovementDto>(
                null, "Belirtilen yük numarasına ait kayıt bulunamadı", null);

        var now = _clock.Now;

        var movement = new LoadTransferMovement
        {
            LoadId = load.Id,
            LoadTransferId = input.LoadTransferId,
            DestinationId = input.DestinationId!.Value,
            Description = input.Description,
            UserId = userId,
            Address = input.Address,
            ExpeditionStatusId = input.ExpeditionStatusId!.Value,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.LoadTransferMovements.Add(movement);
        await _db.SaveChangesAsync(cancellationToken);

        return new MovementResult<LoadMovementDto>(
            await SingleLoadMovementAsync(movement.Id, cancellationToken), null, null);
    }

    public async Task<MovementResult<LoadMovementDto>> UpdateLoadMovementAsync(
        long id, LoadMovementInput input, CancellationToken cancellationToken = default)
    {
        var movement = await _db.LoadTransferMovements
            .FirstOrDefaultAsync(m => m.Id == id && m.DeletedAt == null, cancellationToken);

        if (movement is null)
            return new MovementResult<LoadMovementDto>(null, "Yük hareketi bulunamadı", null);

        // Kısmi güncelleme: yalnızca gönderilen alanlar değişir (kaynaktaki
        // $request->has() zinciriyle aynı).
        if (input.HasLoadNumber)
        {
            var load = await _db.Loads.AsNoTracking()
                .FirstOrDefaultAsync(l => l.LoadNumber == input.LoadNumber, cancellationToken);

            if (load is null)
                return new MovementResult<LoadMovementDto>(
                    null, "Belirtilen yük numarasına ait kayıt bulunamadı", null);

            movement.LoadId = load.Id;
        }

        if (input.HasLoadTransferId)
            movement.LoadTransferId = input.LoadTransferId;

        if (input.HasDestinationId && input.DestinationId is { } destination)
            movement.DestinationId = destination;

        if (input.HasDescription)
            movement.Description = input.Description;

        if (input.HasAddress)
            movement.Address = input.Address;

        if (input.HasExpeditionStatusId && input.ExpeditionStatusId is { } status)
            movement.ExpeditionStatusId = status;

        movement.UpdatedAt = _clock.Now;
        await _db.SaveChangesAsync(cancellationToken);

        return new MovementResult<LoadMovementDto>(
            await SingleLoadMovementAsync(id, cancellationToken), null, null);
    }

    public async Task<bool> DeleteLoadMovementsAsync(
        IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return false;

        var movements = await _db.LoadTransferMovements
            .Where(m => ids.Contains(m.Id) && m.DeletedAt == null)
            .ToListAsync(cancellationToken);

        if (movements.Count == 0)
            return false;

        var now = _clock.Now;

        foreach (var movement in movements)
            movement.DeletedAt = now;

        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }

    // ── İzdüşümler ──────────────────────────────────────────────────────────

    private System.Linq.Expressions.Expression<Func<ExpeditionMovement, ExpeditionMovementDto>>
        ProjectExpeditionMovement() =>
        m => new ExpeditionMovementDto
        {
            Id = m.Id,
            ExpeditionId = m.ExpeditionId,
            DestinationId = m.DestinationId,
            Description = m.Description,
            UserId = m.UserId,
            Address = m.Address,
            ExpeditionStatusId = m.ExpeditionStatusId,
            CreatedAt = m.CreatedAt,
            DeletedAt = m.DeletedAt,
            Expedition = _db.Expeditions.Where(e => e.Id == m.ExpeditionId)
                .Select(e => new MappedExpeditionDto { Id = e.Id, ExpeditionNumber = e.ExpeditionNumber })
                .FirstOrDefault(),
            Destination = _db.Destinations.Where(d => d.Id == m.DestinationId)
                .Select(d => new MovementRefDto { Id = d.Id, Name = d.Name })
                .FirstOrDefault(),
            User = _db.Users.Where(u => u.Id == m.UserId)
                .Select(u => new MovementRefDto { Id = u.Id, Name = u.Name })
                .FirstOrDefault(),
            ExpeditionStatus = _db.ExpeditionStatuses.Where(s => s.Id == m.ExpeditionStatusId)
                .Select(s => new MovementRefDto { Id = s.Id, Name = s.Name })
                .FirstOrDefault(),
        };

    private System.Linq.Expressions.Expression<Func<LoadTransferMovement, LoadMovementDto>>
        ProjectLoadMovement() =>
        m => new LoadMovementDto
        {
            Id = m.Id,
            LoadId = m.LoadId,
            LoadTransferId = m.LoadTransferId,
            DestinationId = m.DestinationId,
            Description = m.Description,
            UserId = m.UserId,
            Address = m.Address,
            ExpeditionStatusId = m.ExpeditionStatusId,
            ExpeditionMovementId = m.ExpeditionMovementId,
            CreatedAt = m.CreatedAt,
            DeletedAt = m.DeletedAt,
            LoadRelation = _db.Loads.Where(l => l.Id == m.LoadId)
                .Select(l => new MovementRefDto { Id = l.Id, Name = l.LoadNumber })
                .FirstOrDefault(),
            LoadTransfer = _db.LoadTransfers.Where(t => t.Id == m.LoadTransferId)
                .Select(t => new MovementRefDto { Id = t.Id, Name = t.LoadNumberWorkType })
                .FirstOrDefault(),
            Destination = _db.Destinations.Where(d => d.Id == m.DestinationId)
                .Select(d => new MovementRefDto { Id = d.Id, Name = d.Name })
                .FirstOrDefault(),
            User = _db.Users.Where(u => u.Id == m.UserId)
                .Select(u => new MovementRefDto { Id = u.Id, Name = u.Name })
                .FirstOrDefault(),
            ExpeditionStatus = _db.ExpeditionStatuses.Where(s => s.Id == m.ExpeditionStatusId)
                .Select(s => new MovementRefDto { Id = s.Id, Name = s.Name })
                .FirstOrDefault(),
            ExpeditionMovement = _db.ExpeditionMovements.Where(e => e.Id == m.ExpeditionMovementId)
                .Select(e => new ExpeditionMovementDto
                {
                    Id = e.Id,
                    ExpeditionId = e.ExpeditionId,
                    DestinationId = e.DestinationId,
                    Description = e.Description,
                    UserId = e.UserId,
                    Address = e.Address,
                    ExpeditionStatusId = e.ExpeditionStatusId,
                    CreatedAt = e.CreatedAt,
                    DeletedAt = e.DeletedAt,
                    Expedition = _db.Expeditions.Where(x => x.Id == e.ExpeditionId)
                        .Select(x => new MappedExpeditionDto { Id = x.Id, ExpeditionNumber = x.ExpeditionNumber })
                        .FirstOrDefault(),
                })
                .FirstOrDefault(),
        };
}
