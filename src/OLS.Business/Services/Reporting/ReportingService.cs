using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.DataAccess.Context;

namespace OLS.Business.Services.Reporting;

/// <summary>
/// Kullanıcı bazlı KPI/raporlama ekranı — daha önceki bir kapsam kararında
/// dışarıda bırakılan bir modüldü (bkz. DashboardController), kullanıcı
/// isteğiyle eklendi. olsold'da doğrudan bir karşılığı yok.
///
/// Kişi bazlı sayaçlar, gerçek veri modelindeki tek bağlantı noktalarından
/// hesaplanır:
///   - Teklif: <c>load_charge_people</c> (bir kullanıcı hem Operasyon Yetkilisi
///     hem Satış Temsilcisi olarak aynı teklife atanmış olabilir — bu yüzden
///     DISTINCT load_id sayılır, çift saymayı önler).
///   - Yük: <c>load_transfers.usercode_with_notification</c> (tek "Görevli" alanı).
///   - Sefer Hareketi: <c>expedition_movements.user_id</c> (kim hangi sefer
///     durumunu/hareketini işlemiş).
///   - Sorumlu Müşteri: <c>user_account_mappings</c> (AccountService'in görünürlük
///     kontrolünde kullandığı aynı "sorumlu" eşlemesi) — bu GÜNCEL bir atama,
///     zamanlanmış bir olay değil, bu yüzden dönem filtresinden ETKİLENMEZ.
///
/// Dönem filtresi (<c>dateFrom</c>/<c>dateTo</c>) — ikisi de boşsa ("Tüm
/// Zamanlar") KPI kartları ve tablo SINIRSIZDIR (tüm geçmiş); yalnızca trend
/// grafiği (aksi hâlde anlamsız/aşırı kalabalık olurdu) son 12 ayı gösterir.
/// </summary>
public interface IReportingService
{
    Task<ReportingDto> GetAsync(
        DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default);

    /// <summary>Bir kullanıcının ayrıntı çekmecesi — özet sayılar + son Teklif/Yük/Sefer Hareketi listeleri.</summary>
    Task<UserReportDetailDto?> GetUserDetailAsync(
        long userId, DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default);
}

public sealed record ReportingDto
{
    public required ReportingKpiDto Kpi { get; init; }
    public required string TrendGranularity { get; init; }
    public required IReadOnlyList<TrendPointDto> Trend { get; init; }
    public required IReadOnlyList<UserReportRowDto> Users { get; init; }
}

public sealed record ReportingKpiDto
{
    public required int TotalOffers { get; init; }
    public required int TotalLoads { get; init; }
    public required int TotalExpeditions { get; init; }
    public required decimal TotalInvoiceAmount { get; init; }
    public required int TotalAccounts { get; init; }
    /// <summary>Pasif hesaplar sayılmaz — bkz. UserService.ListAsync.</summary>
    public required int TotalUsers { get; init; }
    public required decimal ExpectedIncomeTry { get; init; }
    public required decimal ExpectedExpenseTry { get; init; }
    public required decimal RealizedIncomeTry { get; init; }
    public required decimal RealizedExpenseTry { get; init; }
}

public sealed record TrendPointDto(DateOnly Bucket, int OfferCount, int LoadCount);

public sealed record UserReportRowDto
{
    public required long UserId { get; init; }
    public string? Name { get; init; }
    public string? Surname { get; init; }
    public string? Email { get; init; }
    public string? Avatar { get; init; }
    public required int OfferCount { get; init; }
    public required int LoadCount { get; init; }
    public required int ExpeditionMovementCount { get; init; }
    public required int AccountCount { get; init; }
}

public sealed record UserReportDetailDto
{
    public required UserReportRowDto Summary { get; init; }
    public required IReadOnlyList<UserActivityRowDto> RecentOffers { get; init; }
    public required IReadOnlyList<UserActivityRowDto> RecentLoads { get; init; }
    public required IReadOnlyList<UserMovementRowDto> RecentMovements { get; init; }
    public required IReadOnlyList<UserAccountRowDto> Accounts { get; init; }
}

public sealed record UserActivityRowDto(
    long Id, string? Number, string? CustomerName, DateTime? CreatedAt, string? StatusName);

public sealed record UserMovementRowDto(
    long Id, string? ExpeditionNumber, string? DestinationName, string? StatusName, DateTime? CreatedAt);

public sealed record UserAccountRowDto(long Id, string? Name);

public sealed class ReportingService : IReportingService
{
    private readonly OlsDbContext _db;
    private readonly IClock _clock;

    public ReportingService(OlsDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<ReportingDto> GetAsync(
        DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(_clock.Now);

        DateTime? rangeStart = dateFrom?.ToDateTime(TimeOnly.MinValue);
        DateTime? rangeEndExclusive = dateTo?.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var offersQuery = _db.Loads.AsQueryable();
        if (rangeStart is { } os) offersQuery = offersQuery.Where(l => l.CreatedAt >= os);
        if (rangeEndExclusive is { } oe) offersQuery = offersQuery.Where(l => l.CreatedAt < oe);

        var loadsQuery = _db.LoadTransfers.AsQueryable();
        if (rangeStart is { } ls) loadsQuery = loadsQuery.Where(t => t.CreatedAt >= ls);
        if (rangeEndExclusive is { } le) loadsQuery = loadsQuery.Where(t => t.CreatedAt < le);

        var expeditionsQuery = _db.Expeditions.AsQueryable();
        if (rangeStart is { } es) expeditionsQuery = expeditionsQuery.Where(e => e.CreatedAt >= es);
        if (rangeEndExclusive is { } ee) expeditionsQuery = expeditionsQuery.Where(e => e.CreatedAt < ee);

        var invoicesQuery = _db.Invoices.AsQueryable();
        if (rangeStart is { } isv) invoicesQuery = invoicesQuery.Where(i => i.InvoiceCreateDate >= isv);
        if (rangeEndExclusive is { } ie) invoicesQuery = invoicesQuery.Where(i => i.InvoiceCreateDate < ie);

        var movementsQuery = _db.ExpeditionMovements.Where(m => m.DeletedAt == null);
        if (rangeStart is { } ms) movementsQuery = movementsQuery.Where(m => m.CreatedAt >= ms);
        if (rangeEndExclusive is { } me) movementsQuery = movementsQuery.Where(m => m.CreatedAt < me);

        // Siber'in kendi maliyet/ciro muhasebesi (sbr_kzgelirgider) - document_date
        // zaten DateOnly olduğundan diğer sorgulardaki DateTime aralık dönüşümüne gerek yok.
        var financeQuery = _db.ExpeditionFinanceRecords.AsQueryable();
        if (dateFrom is { } ff) financeQuery = financeQuery.Where(f => f.DocumentDate >= ff);
        if (dateTo is { } ft) financeQuery = financeQuery.Where(f => f.DocumentDate <= ft);

        var totalOffers = await offersQuery.CountAsync(cancellationToken);
        var totalLoads = await loadsQuery.CountAsync(cancellationToken);
        var totalExpeditions = await expeditionsQuery.CountAsync(cancellationToken);
        var totalInvoiceAmount = await invoicesQuery.SumAsync(i => (decimal?)i.PayableAmount, cancellationToken) ?? 0m;
        var totalAccounts = await _db.Accounts.CountAsync(cancellationToken);
        var expectedIncome = await financeQuery.SumAsync(f => (decimal?)f.ExpectedIncomeTry, cancellationToken) ?? 0m;
        var expectedExpense = await financeQuery.SumAsync(f => (decimal?)f.ExpectedExpenseTry, cancellationToken) ?? 0m;
        var realizedIncome = await financeQuery.SumAsync(f => (decimal?)f.RealizedIncomeTry, cancellationToken) ?? 0m;
        var realizedExpense = await financeQuery.SumAsync(f => (decimal?)f.RealizedExpenseTry, cancellationToken) ?? 0m;

        var users = await _db.Users.AsNoTracking()
            .Where(u => u.DeletedAt == null && u.Status)
            .OrderBy(u => u.Name).ThenBy(u => u.Surname)
            .Select(u => new { u.Id, u.Name, u.Surname, u.Email, u.Avatar })
            .ToListAsync(cancellationToken);

        // olsold: aynı kullanıcı bir teklife hem Operasyon Yetkilisi hem Satış
        // Temsilcisi olarak atanabiliyor (bkz. LoadWriteService — hiç görevli
        // seçilmezse oturum açan kullanıcı her iki role de otomatik ekleniyor).
        // DISTINCT load_id, bu tekliflerin iki kez sayılmasını önler.
        var offerIdsInRange = offersQuery.Select(l => (int)l.Id);
        var offerCountsRaw = await _db.LoadChargePeople
            .Where(p => p.UserId != null && p.LoadId != null && offerIdsInRange.Contains(p.LoadId.Value))
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Select(p => p.LoadId).Distinct().Count() })
            .ToListAsync(cancellationToken);
        var offerDict = offerCountsRaw
            .Where(x => x.UserId.HasValue)
            .ToDictionary(x => (long)x.UserId!.Value, x => x.Count);

        var loadCountsRaw = await loadsQuery
            .Where(t => t.UsercodeWithNotification != null)
            .GroupBy(t => t.UsercodeWithNotification)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var loadDict = loadCountsRaw
            .Where(x => x.UserId.HasValue)
            .ToDictionary(x => (long)x.UserId!.Value, x => x.Count);

        var movementCountsRaw = await movementsQuery
            .GroupBy(m => m.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var movementDict = movementCountsRaw.ToDictionary(x => x.UserId, x => x.Count);

        var accountCountsRaw = await _db.UserAccountMappings
            .GroupBy(m => m.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var accountDict = accountCountsRaw.ToDictionary(x => (long)x.UserId, x => x.Count);

        var rows = users.Select(u => new UserReportRowDto
        {
            UserId = u.Id,
            Name = u.Name,
            Surname = u.Surname,
            Email = u.Email,
            Avatar = u.Avatar,
            OfferCount = offerDict.GetValueOrDefault(u.Id),
            LoadCount = loadDict.GetValueOrDefault(u.Id),
            ExpeditionMovementCount = movementDict.GetValueOrDefault(u.Id),
            AccountCount = accountDict.GetValueOrDefault(u.Id),
        }).ToList();

        var (granularity, trend) = await BuildTrendAsync(dateFrom, dateTo, today, cancellationToken);

        return new ReportingDto
        {
            Kpi = new ReportingKpiDto
            {
                TotalOffers = totalOffers,
                TotalLoads = totalLoads,
                TotalExpeditions = totalExpeditions,
                TotalInvoiceAmount = totalInvoiceAmount,
                TotalAccounts = totalAccounts,
                TotalUsers = users.Count,
                ExpectedIncomeTry = expectedIncome,
                ExpectedExpenseTry = expectedExpense,
                RealizedIncomeTry = realizedIncome,
                RealizedExpenseTry = realizedExpense,
            },
            TrendGranularity = granularity,
            Trend = trend,
            Users = rows,
        };
    }

    public async Task<UserReportDetailDto?> GetUserDetailAsync(
        long userId, DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId && u.DeletedAt == null)
            .Select(u => new { u.Id, u.Name, u.Surname, u.Email, u.Avatar })
            .FirstOrDefaultAsync(cancellationToken);
        if (user is null)
            return null;

        DateTime? rangeStart = dateFrom?.ToDateTime(TimeOnly.MinValue);
        DateTime? rangeEndExclusive = dateTo?.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var offersQuery = _db.Loads.AsQueryable();
        if (rangeStart is { } os) offersQuery = offersQuery.Where(l => l.CreatedAt >= os);
        if (rangeEndExclusive is { } oe) offersQuery = offersQuery.Where(l => l.CreatedAt < oe);

        var loadsQuery = _db.LoadTransfers.AsQueryable();
        if (rangeStart is { } ls) loadsQuery = loadsQuery.Where(t => t.CreatedAt >= ls);
        if (rangeEndExclusive is { } le) loadsQuery = loadsQuery.Where(t => t.CreatedAt < le);

        var movementsQuery = _db.ExpeditionMovements.Where(m => m.DeletedAt == null && m.UserId == userId);
        if (rangeStart is { } ms) movementsQuery = movementsQuery.Where(m => m.CreatedAt >= ms);
        if (rangeEndExclusive is { } me) movementsQuery = movementsQuery.Where(m => m.CreatedAt < me);

        var userIdInt = (int)userId;

        // olsold: aynı DISTINCT load_id mantığı burada da geçerli - bkz. GetAsync.
        var offerIdsForUser = await _db.LoadChargePeople
            .Where(p => p.UserId == userIdInt && p.LoadId != null)
            .Select(p => p.LoadId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var offersForUserQuery = offersQuery.Where(l => offerIdsForUser.Contains((int)l.Id));
        var loadsForUserQuery = loadsQuery.Where(t => t.UsercodeWithNotification == userIdInt);

        var offerCount = await offersForUserQuery.CountAsync(cancellationToken);
        var loadCount = await loadsForUserQuery.CountAsync(cancellationToken);
        var movementCount = await movementsQuery.CountAsync(cancellationToken);
        var accountCount = await _db.UserAccountMappings.CountAsync(m => m.UserId == userIdInt, cancellationToken);

        var recentOffers = await offersForUserQuery
            .OrderByDescending(l => l.CreatedAt)
            .Take(20)
            .Select(l => new UserActivityRowDto(
                l.Id,
                l.ReservationNumber,
                _db.Accounts.Where(a => a.Id == l.CustomerId).Select(a => a.Name).FirstOrDefault(),
                l.CreatedAt,
                _db.StatusTypes.Where(s => s.Id == l.StatusTypeId).Select(s => s.Name).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        var recentLoads = await loadsForUserQuery
            .OrderByDescending(t => t.CreatedAt)
            .Take(20)
            .Select(t => new UserActivityRowDto(
                t.Id,
                t.LoadNumberWorkType,
                _db.Accounts.Where(a => a.Id == t.CustomerId).Select(a => a.Name).FirstOrDefault(),
                t.CreatedAt,
                _db.LoadStatusTypes.Where(s => s.Id == t.LoadStatusId).Select(s => s.Name).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        var recentMovements = await movementsQuery
            .OrderByDescending(m => m.CreatedAt)
            .Take(20)
            .Select(m => new UserMovementRowDto(
                m.Id,
                _db.Expeditions.Where(e => e.Id == m.ExpeditionId).Select(e => e.ExpeditionNumber).FirstOrDefault(),
                _db.Destinations.Where(d => d.Id == m.DestinationId).Select(d => d.Name).FirstOrDefault(),
                _db.ExpeditionStatuses.Where(s => s.Id == m.ExpeditionStatusId).Select(s => s.Name).FirstOrDefault(),
                m.CreatedAt))
            .ToListAsync(cancellationToken);

        // user_account_mappings şu an boş (bkz. sınıf açıklaması) - hazır, veri geldiğinde dolacak.
        var accounts = await _db.UserAccountMappings
            .Where(m => m.UserId == userIdInt)
            .OrderBy(m => m.Id)
            .Take(20)
            .Select(m => new UserAccountRowDto(
                m.AccountId,
                _db.Accounts.Where(a => a.Id == m.AccountId).Select(a => a.Name).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new UserReportDetailDto
        {
            Summary = new UserReportRowDto
            {
                UserId = user.Id,
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email,
                Avatar = user.Avatar,
                OfferCount = offerCount,
                LoadCount = loadCount,
                ExpeditionMovementCount = movementCount,
                AccountCount = accountCount,
            },
            RecentOffers = recentOffers,
            RecentLoads = recentLoads,
            RecentMovements = recentMovements,
            Accounts = accounts,
        };
    }

    private async Task<(string Granularity, IReadOnlyList<TrendPointDto> Points)> BuildTrendAsync(
        DateOnly? dateFrom, DateOnly? dateTo, DateOnly today, CancellationToken cancellationToken)
    {
        DateOnly from;
        DateOnly to;

        if (dateFrom is null && dateTo is null)
        {
            to = today;
            var monthsAgo = today.AddMonths(-11);
            from = new DateOnly(monthsAgo.Year, monthsAgo.Month, 1);
        }
        else
        {
            to = dateTo ?? today;
            from = dateFrom ?? to.AddYears(-1);
            if (from > to)
                (from, to) = (to, from);
        }

        var spanDays = to.DayNumber - from.DayNumber + 1;
        var isAllTime = dateFrom is null && dateTo is null;

        List<(DateOnly Start, DateOnly EndExclusive)> buckets = [];
        string granularity;

        if (isAllTime || spanDays > 180)
        {
            granularity = "month";
            var cursor = new DateOnly(from.Year, from.Month, 1);
            while (cursor <= to)
            {
                var next = cursor.AddMonths(1);
                buckets.Add((cursor, next));
                cursor = next;
            }
        }
        else if (spanDays > 31)
        {
            granularity = "week";
            var cursor = from;
            var toExclusive = to.AddDays(1);
            while (cursor < toExclusive)
            {
                var next = cursor.AddDays(7);
                if (next > toExclusive)
                    next = toExclusive;
                buckets.Add((cursor, next));
                cursor = next;
            }
        }
        else
        {
            granularity = "day";
            var cursor = from;
            while (cursor <= to)
            {
                var next = cursor.AddDays(1);
                buckets.Add((cursor, next));
                cursor = next;
            }
        }

        var points = new List<TrendPointDto>(buckets.Count);
        foreach (var (start, endExclusive) in buckets)
        {
            var startDt = start.ToDateTime(TimeOnly.MinValue);
            var endDt = endExclusive.ToDateTime(TimeOnly.MinValue);
            var offerCount = await _db.Loads.CountAsync(l => l.CreatedAt >= startDt && l.CreatedAt < endDt, cancellationToken);
            var loadCount = await _db.LoadTransfers.CountAsync(t => t.CreatedAt >= startDt && t.CreatedAt < endDt, cancellationToken);
            points.Add(new TrendPointDto(start, offerCount, loadCount));
        }

        return (granularity, points);
    }
}
