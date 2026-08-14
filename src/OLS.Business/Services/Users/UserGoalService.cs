using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.Business.Services.Users;

/// <summary>
/// olsold: <c>Front\UserGoal\UserGoalController</c> (<c>/api/v1/user_goal</c>) —
/// Kullanıcılar formunun "Hedefler" sekmesinin (<c>UserTarget.vue</c>) veri
/// kaynağı; genel Reports/Hedef-ciro modülünden AYRI, bu formun görsel/işlevsel
/// bir parçası (bkz. docs/SECILI-MODUL-PARITE-MATRISI.md §7 "İstisnai kapsam-içi
/// bağımlılık").
///
/// Kaynakta <c>delete()</c>'in yetki kontrolü YORUM SATIRINDAYDI (üstelik yanlış
/// slug'la, <c>transport_type_management</c>) — fiilen herkese açıktı. Burada
/// controller katmanında gerçek <c>user_management</c> yetkisi uygulanıyor.
///
/// Kaynağın <c>all()</c> yanıtı her satıra bir <c>total_price_sum</c> (o kullanıcının
/// Teklif-durumundaki, Satış yönlü mali kalemlerinin toplamı) ekliyor ama
/// <c>UserTarget.vue</c> bu alanı HİÇ RENDER ETMİYOR (ölü alan, doğrulandı) —
/// burada taşınmadı; kaynağın gerçekten kullanılan davranışı (hedef CRUD +
/// tarih aralığı çakışma kontrolü) birebir korunuyor.
/// </summary>
public interface IUserGoalService
{
    Task<IReadOnlyList<UserGoalDto>> ListAsync(int userId, CancellationToken cancellationToken = default);

    Task<UserGoalDto?> SingleAsync(long id, CancellationToken cancellationToken = default);

    Task<UserGoalResult> CreateAsync(UserGoalInput input, CancellationToken cancellationToken = default);

    Task<UserGoalResult> UpdateAsync(long id, UserGoalInput input, CancellationToken cancellationToken = default);

    Task DeleteAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default);
}

public sealed class UserGoalInput
{
    public int UserId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal GoalPrice { get; set; }
}

public sealed record UserGoalResult(bool Success, string? Error, UserGoalDto? Data)
{
    public static UserGoalResult Ok(UserGoalDto data) => new(true, null, data);
    public static UserGoalResult Fail(string error) => new(false, error, null);
}

public sealed class UserGoalDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("user_id")] public int? UserId { get; init; }
    [JsonPropertyName("start_date")] public DateOnly? StartDate { get; init; }
    [JsonPropertyName("end_date")] public DateOnly? EndDate { get; init; }
    [JsonPropertyName("goal_price")] public decimal GoalPrice { get; init; }
}

public sealed class UserGoalService : IUserGoalService
{
    private const string DateRangeConflictError = "Bu tarih aralığında zaten bir kayıt bulunmaktadır.";

    private readonly OlsDbContext _db;
    private readonly IClock _clock;

    public UserGoalService(OlsDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<UserGoalDto>> ListAsync(
        int userId, CancellationToken cancellationToken = default)
    {
        return await _db.UserGoals
            .AsNoTracking()
            .Where(g => g.UserId == userId)
            .OrderByDescending(g => g.Id)
            .Select(Project())
            .ToListAsync(cancellationToken);
    }

    public async Task<UserGoalDto?> SingleAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _db.UserGoals
            .AsNoTracking()
            .Where(g => g.Id == id)
            .Select(Project())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UserGoalResult> CreateAsync(
        UserGoalInput input, CancellationToken cancellationToken = default)
    {
        if (await HasOverlapAsync(input.UserId, input.StartDate, input.EndDate, excludeId: null, cancellationToken))
            return UserGoalResult.Fail(DateRangeConflictError);

        var now = _clock.Now;
        var goal = new UserGoal
        {
            UserId = input.UserId,
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            GoalPrice = input.GoalPrice,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.UserGoals.Add(goal);
        await _db.SaveChangesAsync(cancellationToken);

        return UserGoalResult.Ok(ToDto(goal));
    }

    public async Task<UserGoalResult> UpdateAsync(
        long id, UserGoalInput input, CancellationToken cancellationToken = default)
    {
        var goal = await _db.UserGoals.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        if (goal is null)
            return UserGoalResult.Fail("Kayıt bulunamadı.");

        if (await HasOverlapAsync(input.UserId, input.StartDate, input.EndDate, excludeId: id, cancellationToken))
            return UserGoalResult.Fail(DateRangeConflictError);

        goal.UserId = input.UserId;
        goal.StartDate = input.StartDate;
        goal.EndDate = input.EndDate;
        goal.GoalPrice = input.GoalPrice;
        goal.UpdatedAt = _clock.Now;

        await _db.SaveChangesAsync(cancellationToken);

        return UserGoalResult.Ok(ToDto(goal));
    }

    public async Task DeleteAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        await _db.UserGoals.Where(g => ids.Contains(g.Id)).ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>
    /// Kaynağın çakışma kuralı birebir: yeni aralığın başlangıcı VEYA bitişi mevcut
    /// bir kaydın içine düşüyorsa, ya da yeni aralık mevcut bir kaydı tamamen
    /// kapsıyorsa çakışma sayılır.
    /// </summary>
    private async Task<bool> HasOverlapAsync(
        int userId, DateOnly? startDate, DateOnly? endDate, long? excludeId, CancellationToken cancellationToken)
    {
        if (startDate is null || endDate is null)
            return false;

        var query = _db.UserGoals.Where(g => g.UserId == userId);
        if (excludeId is { } id)
            query = query.Where(g => g.Id != id);

        return await query.AnyAsync(g =>
            (g.StartDate >= startDate && g.StartDate <= endDate) ||
            (g.EndDate >= startDate && g.EndDate <= endDate) ||
            (g.StartDate <= startDate && g.EndDate >= endDate),
            cancellationToken);
    }

    private static UserGoalDto ToDto(UserGoal goal) => new()
    {
        Id = goal.Id,
        UserId = goal.UserId,
        StartDate = goal.StartDate,
        EndDate = goal.EndDate,
        GoalPrice = goal.GoalPrice,
    };

    private static System.Linq.Expressions.Expression<Func<UserGoal, UserGoalDto>> Project() =>
        g => new UserGoalDto
        {
            Id = g.Id,
            UserId = g.UserId,
            StartDate = g.StartDate,
            EndDate = g.EndDate,
            GoalPrice = g.GoalPrice,
        };
}
