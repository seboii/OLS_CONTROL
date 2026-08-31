using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.DataAccess.Context;

namespace OLS.Business.Services.Auditing;

public interface IAuditLogService
{
    Task<object> ListAsync(AuditLogQuery query, CancellationToken cancellationToken = default);

    /// <summary>Arama kutusunda seçilebilecek kayıtlar (yük/sefer/kullanıcı/cari).</summary>
    Task<IReadOnlyList<AuditTargetDto>> TargetsAsync(
        string? search, CancellationToken cancellationToken = default);
}

public sealed record AuditLogQuery(
    string? Search, string? EntityType, long? UserId, string? EntityLabel,
    DateOnly? From, DateOnly? To, long? AfterId, int? PerPage, int Page, string Path);

public sealed class AuditLogDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("user_id")] public long? UserId { get; init; }
    [JsonPropertyName("user_name")] public string? UserName { get; init; }
    [JsonPropertyName("action")] public string Action { get; init; } = null!;
    [JsonPropertyName("entity_type")] public string EntityType { get; init; } = null!;
    [JsonPropertyName("entity_id")] public string? EntityId { get; init; }
    [JsonPropertyName("entity_label")] public string? EntityLabel { get; init; }
    [JsonPropertyName("changes")] public string? Changes { get; init; }
    [JsonPropertyName("ip_address")] public string? IpAddress { get; init; }
    [JsonPropertyName("created_at")] public DateTime CreatedAt { get; init; }
}

/// <summary>Arama kutusunun önerdiği hedef: bir yük, sefer, cari ya da kullanıcı.</summary>
public sealed class AuditTargetDto
{
    [JsonPropertyName("label")] public string Label { get; init; } = null!;
    [JsonPropertyName("type")] public string Type { get; init; } = null!;
    [JsonPropertyName("hint")] public string? Hint { get; init; }
}

public sealed class AuditLogService : IAuditLogService
{
    private const int TargetLimit = 8;

    private readonly OlsDbContext _db;

    public AuditLogService(OlsDbContext db) => _db = db;

    public async Task<object> ListAsync(
        AuditLogQuery query, CancellationToken cancellationToken = default)
    {
        var logs = _db.AuditLogs.AsNoTracking();

        // ANLIK TAKİP: arayüz elindeki en büyük id'yi gönderiyor, yalnızca ondan
        // sonrakiler dönüyor. Tüm listeyi tekrar çekip karşılaştırmaktan çok daha
        // ucuz ve "yeni satır geldi" animasyonunu mümkün kılıyor.
        if (query.AfterId is { } afterId)
            logs = logs.Where(l => l.Id > afterId);

        if (!string.IsNullOrWhiteSpace(query.EntityType))
            logs = logs.Where(l => l.EntityType == query.EntityType);

        if (query.UserId is { } userId)
            logs = logs.Where(l => l.UserId == userId);

        // Arama kutusundan bir hedef SEÇİLDİĞİNDE tam eşleşme kullanılır:
        // "2600838TR" seçildiyse "2600838TR1" karışmasın.
        if (!string.IsNullOrWhiteSpace(query.EntityLabel))
            logs = logs.Where(l => l.EntityLabel == query.EntityLabel);
        else if (!string.IsNullOrWhiteSpace(query.Search))
            logs = logs
                .WhereILike(l => l.EntityLabel, query.Search)
                .Union(_db.AuditLogs.AsNoTracking().WhereILike(l => l.UserName, query.Search));

        if (query.From is { } from)
            logs = logs.Where(l => l.CreatedAt >= from.ToDateTime(TimeOnly.MinValue));

        if (query.To is { } to)
            logs = logs.Where(l => l.CreatedAt <= to.ToDateTime(TimeOnly.MaxValue));

        var projected = logs
            .OrderByDescending(l => l.Id)
            .Select(l => new AuditLogDto
            {
                Id = l.Id,
                UserId = l.UserId,
                UserName = l.UserName,
                Action = l.Action,
                EntityType = l.EntityType,
                EntityId = l.EntityId,
                EntityLabel = l.EntityLabel,
                Changes = l.Changes,
                IpAddress = l.IpAddress,
                CreatedAt = l.CreatedAt,
            });

        return await projected.ToPagedOrListAsync(
            query.PerPage, query.Page, query.Path, cancellationToken);
    }

    public async Task<IReadOnlyList<AuditTargetDto>> TargetsAsync(
        string? search, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(search) || search.Trim().Length < 2)
            return [];

        var targets = new List<AuditTargetDto>();

        // Hedefler DENETİM KAYDINDAN değil, asıl tablolardan aranır: kullanıcı
        // henüz hiç işlem yapılmamış bir yükü de seçip "bu yükte ne olmuş"
        // diyebilmeli (cevap "hiçbir şey" olsa bile).
        targets.AddRange(await _db.LoadTransfers.AsNoTracking()
            .WhereILike(t => t.LoadNumberWorkType, search)
            .OrderByDescending(t => t.Id)
            .Take(TargetLimit)
            .Select(t => new AuditTargetDto
            {
                Label = t.LoadNumberWorkType!, Type = "Yük", Hint = t.LoadNumber,
            })
            .ToListAsync(cancellationToken));

        targets.AddRange(await _db.Expeditions.AsNoTracking()
            .WhereILike(e => e.ExpeditionNumber, search)
            .OrderByDescending(e => e.Id)
            .Take(TargetLimit)
            .Select(e => new AuditTargetDto
            {
                Label = e.ExpeditionNumber!, Type = "Sefer", Hint = e.YearWeek,
            })
            .ToListAsync(cancellationToken));

        targets.AddRange(await _db.Users.AsNoTracking()
            .WhereILike(u => u.Email, search)
            .OrderBy(u => u.Id)
            .Take(TargetLimit)
            .Select(u => new AuditTargetDto
            {
                Label = u.Email!, Type = "Kullanıcı", Hint = u.Name + " " + u.Surname,
            })
            .ToListAsync(cancellationToken));

        targets.AddRange(await _db.Accounts.AsNoTracking()
            .WhereILike(a => a.Name, search)
            .OrderBy(a => a.Id)
            .Take(TargetLimit)
            .Select(a => new AuditTargetDto { Label = a.Name!, Type = "Cari", Hint = null })
            .ToListAsync(cancellationToken));

        return targets;
    }
}
