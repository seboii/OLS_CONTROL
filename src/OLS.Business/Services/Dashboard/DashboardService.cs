using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.Business.Seed;
using OLS.DataAccess.Context;

namespace OLS.Business.Services.Dashboard;

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default);
}

public sealed record DashboardDto
{
    public required DashboardMetricsDto Metrics { get; init; }
    public required IReadOnlyList<MonthlyPointDto> MonthlyShipments { get; init; }
    public required IReadOnlyList<DistributionSliceDto> WorkTypeDistribution { get; init; }
    public required IReadOnlyList<WeeklyPointDto> WeeklyCompletedTrips { get; init; }
    public required IReadOnlyList<ActivityItemDto> RecentActivity { get; init; }
    public required IReadOnlyList<UpcomingTripDto> UpcomingTrips { get; init; }
}

public sealed record DashboardMetricsDto
{
    public required int ActiveExpeditions { get; init; }
    public required int ActiveExpeditionsDelta { get; init; }
    public required int LoadTransfersThisMonth { get; init; }
    public required double LoadTransfersThisMonthChangePercent { get; init; }
    public required decimal RevenueThisMonth { get; init; }
    public required double RevenueThisMonthChangePercent { get; init; }
    public required int PendingQuotes { get; init; }
    public required int PendingQuotesDelta { get; init; }
    public required int ActiveCustomers { get; init; }
    public required int ActiveCustomersDelta { get; init; }

    /// <summary>
    /// olsold/olsnew'de "zamanında teslimat" veya "gecikme" izleyen bir alan YOK
    /// (expedition_statuses ve load_status_types bu HEDEF veritabanında boş seed
    /// edilmiş durumda). Bu yüzden mockup'taki "Teslim Oranı" (bir SLA/zamanlama
    /// kavramı ima eder) uydurulmadı; onun yerine GERÇEK verilerden hesaplanan bir
    /// vekil kullanılıyor: bu ayki seferlerden kaçının dönüş tarihi (return_date)
    /// girilmiş — yani fiilen tamamlanmış — olduğunun yüzdesi.
    /// </summary>
    public required double CompletionRatePercent { get; init; }
    public required double CompletionRateDeltaPoints { get; init; }
}

public sealed record MonthlyPointDto(string Month, int ShipmentCount, decimal Revenue);

public sealed record DistributionSliceDto(string Name, int Count, double Percent);

public sealed record WeeklyPointDto(string Day, int CompletedCount);

public sealed record ActivityItemDto(string Kind, string Text, string Sub, DateTime At);

public sealed record UpcomingTripDto(
    long Id, string? ExpeditionNumber, string? Route, string? PlateNumber, DateOnly? Date);

public sealed class DashboardService : IDashboardService
{
    private readonly OlsDbContext _db;
    private readonly IClock _clock;

    public DashboardService(OlsDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.Now;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var prevMonthStart = monthStart.AddMonths(-1);
        var weekStart = now.Date.AddDays(-(int)now.DayOfWeek + (now.DayOfWeek == DayOfWeek.Sunday ? -6 : 1));

        var metrics = await BuildMetricsAsync(now, monthStart, prevMonthStart, cancellationToken);
        var monthlyShipments = await BuildMonthlyShipmentsAsync(monthStart, cancellationToken);
        var workTypeDistribution = await BuildWorkTypeDistributionAsync(cancellationToken);
        var weeklyCompleted = await BuildWeeklyCompletedAsync(weekStart, cancellationToken);
        var recentActivity = await BuildRecentActivityAsync(cancellationToken);
        var upcomingTrips = await BuildUpcomingTripsAsync(now, cancellationToken);

        return new DashboardDto
        {
            Metrics = metrics,
            MonthlyShipments = monthlyShipments,
            WorkTypeDistribution = workTypeDistribution,
            WeeklyCompletedTrips = weeklyCompleted,
            RecentActivity = recentActivity,
            UpcomingTrips = upcomingTrips,
        };
    }

    private async Task<DashboardMetricsDto> BuildMetricsAsync(
        DateTime now, DateTime monthStart, DateTime prevMonthStart, CancellationToken ct)
    {
        var activeExpeditions = await _db.Expeditions.CountAsync(e => e.ReturnDate == null, ct);
        var activeExpeditionsLastMonth = await _db.Expeditions.CountAsync(
            e => e.ReturnDate == null && e.CreatedAt != null && e.CreatedAt < monthStart, ct);

        var loadTransfersThisMonth = await _db.LoadTransfers.CountAsync(
            l => l.CreatedAt != null && l.CreatedAt >= monthStart, ct);
        var loadTransfersLastMonth = await _db.LoadTransfers.CountAsync(
            l => l.CreatedAt != null && l.CreatedAt >= prevMonthStart && l.CreatedAt < monthStart, ct);

        var revenueThisMonth = await _db.Invoices
            .Where(i => i.InvoiceCreateDate >= monthStart)
            .SumAsync(i => (decimal?)i.PayableAmount, ct) ?? 0m;
        var revenueLastMonth = await _db.Invoices
            .Where(i => i.InvoiceCreateDate >= prevMonthStart && i.InvoiceCreateDate < monthStart)
            .SumAsync(i => (decimal?)i.PayableAmount, ct) ?? 0m;

        var offerStatusId = await _db.StatusTypes
            .Where(s => s.Number == StatusTypeCodes.Offer)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(ct);
        var pendingQuotes = offerStatusId is null
            ? 0
            : await _db.Loads.CountAsync(l => l.StatusTypeId == offerStatusId, ct);

        var activeCustomers = await _db.Accounts.CountAsync(ct);
        var activeCustomersLastMonth = await _db.Accounts.CountAsync(
            a => a.CreatedAt != null && a.CreatedAt < monthStart, ct);

        var expeditionsThisMonth = await _db.Expeditions
            .Where(e => e.CreatedAt != null && e.CreatedAt >= monthStart)
            .Select(e => new { e.ReturnDate })
            .ToListAsync(ct);
        var completionRate = expeditionsThisMonth.Count == 0
            ? 0
            : 100.0 * expeditionsThisMonth.Count(e => e.ReturnDate != null) / expeditionsThisMonth.Count;

        return new DashboardMetricsDto
        {
            ActiveExpeditions = activeExpeditions,
            ActiveExpeditionsDelta = activeExpeditions - activeExpeditionsLastMonth,
            LoadTransfersThisMonth = loadTransfersThisMonth,
            LoadTransfersThisMonthChangePercent = PercentChange(loadTransfersLastMonth, loadTransfersThisMonth),
            RevenueThisMonth = revenueThisMonth,
            RevenueThisMonthChangePercent = PercentChange((double)revenueLastMonth, (double)revenueThisMonth),
            PendingQuotes = pendingQuotes,
            PendingQuotesDelta = 0,
            ActiveCustomers = activeCustomers,
            ActiveCustomersDelta = activeCustomers - activeCustomersLastMonth,
            CompletionRatePercent = Math.Round(completionRate, 1),
            CompletionRateDeltaPoints = 0,
        };
    }

    private async Task<IReadOnlyList<MonthlyPointDto>> BuildMonthlyShipmentsAsync(
        DateTime monthStart, CancellationToken ct)
    {
        var points = new List<MonthlyPointDto>();

        for (var i = 5; i >= 0; i--)
        {
            var start = monthStart.AddMonths(-i);
            var end = start.AddMonths(1);

            var count = await _db.LoadTransfers.CountAsync(
                l => l.CreatedAt != null && l.CreatedAt >= start && l.CreatedAt < end, ct);
            var revenue = await _db.Invoices
                .Where(inv => inv.InvoiceCreateDate >= start && inv.InvoiceCreateDate < end)
                .SumAsync(inv => (decimal?)inv.PayableAmount, ct) ?? 0m;

            points.Add(new MonthlyPointDto(TurkishMonthAbbrev(start), count, revenue));
        }

        return points;
    }

    private async Task<IReadOnlyList<DistributionSliceDto>> BuildWorkTypeDistributionAsync(CancellationToken ct)
    {
        var total = await _db.LoadTransfers.CountAsync(l => l.WorkType != null, ct);

        if (total == 0)
            return [];

        var grouped = await _db.LoadTransfers
            .Where(l => l.WorkType != null)
            .GroupBy(l => l.WorkType)
            .Select(g => new { WorkTypeId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var workTypeNames = await _db.WorkTypes
            .Select(w => new { w.Id, w.Name })
            .ToDictionaryAsync(w => (int)w.Id, w => w.Name ?? "—", ct);

        return grouped
            .Select(g => new DistributionSliceDto(
                workTypeNames.GetValueOrDefault(g.WorkTypeId ?? 0, "Diğer"),
                g.Count,
                Math.Round(100.0 * g.Count / total, 1)))
            .OrderByDescending(s => s.Count)
            .ToList();
    }

    private async Task<IReadOnlyList<WeeklyPointDto>> BuildWeeklyCompletedAsync(DateTime weekStart, CancellationToken ct)
    {
        string[] dayLabels = ["Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz"];
        var weekStartDate = DateOnly.FromDateTime(weekStart);

        var completedThisWeek = await _db.Expeditions
            .Where(e => e.ReturnDate != null && e.ReturnDate >= weekStartDate && e.ReturnDate < weekStartDate.AddDays(7))
            .Select(e => e.ReturnDate!.Value)
            .ToListAsync(ct);

        var points = new List<WeeklyPointDto>();
        for (var i = 0; i < 7; i++)
        {
            var day = weekStartDate.AddDays(i);
            points.Add(new WeeklyPointDto(dayLabels[i], completedThisWeek.Count(d => d == day)));
        }

        return points;
    }

    private async Task<IReadOnlyList<ActivityItemDto>> BuildRecentActivityAsync(CancellationToken ct)
    {
        var activities = new List<ActivityItemDto>();

        var recentLoadTransfers = await _db.LoadTransfers
            .Where(l => l.CreatedAt != null)
            .OrderByDescending(l => l.CreatedAt)
            .Take(5)
            .Select(l => new { l.Id, l.LoadNumber, l.CreatedAt })
            .ToListAsync(ct);
        activities.AddRange(recentLoadTransfers.Select(l => new ActivityItemDto(
            "load_transfer", $"{l.LoadNumber ?? $"YUK-{l.Id}"} oluşturuldu", "Yük", l.CreatedAt!.Value)));

        var recentExpeditions = await _db.Expeditions
            .Where(e => e.CreatedAt != null)
            .OrderByDescending(e => e.CreatedAt)
            .Take(5)
            .Select(e => new { e.Id, e.ExpeditionNumber, e.CreatedAt })
            .ToListAsync(ct);
        activities.AddRange(recentExpeditions.Select(e => new ActivityItemDto(
            "expedition", $"{e.ExpeditionNumber ?? $"SEF-{e.Id}"} oluşturuldu", "Sefer", e.CreatedAt!.Value)));

        var recentInvoices = await _db.Invoices
            .Where(i => i.CreatedAt != null)
            .OrderByDescending(i => i.CreatedAt)
            .Take(5)
            .Select(i => new { i.Id, i.InvoiceId, i.PayableAmount, i.CreatedAt })
            .ToListAsync(ct);
        activities.AddRange(recentInvoices.Select(i => new ActivityItemDto(
            "invoice",
            $"{i.InvoiceId ?? $"FAT-{i.Id}"} oluşturuldu",
            i.PayableAmount is { } amt ? $"₺{amt:N0}" : "Fatura",
            i.CreatedAt!.Value)));

        var recentQuotes = await _db.Loads
            .Where(l => l.CreatedAt != null)
            .OrderByDescending(l => l.CreatedAt)
            .Take(5)
            .Select(l => new { l.Id, l.LoadNumber, l.CreatedAt })
            .ToListAsync(ct);
        activities.AddRange(recentQuotes.Select(l => new ActivityItemDto(
            "quote", $"{l.LoadNumber ?? $"T-{l.Id}"} teklifi oluşturuldu", "Teklif", l.CreatedAt!.Value)));

        return activities
            .OrderByDescending(a => a.At)
            .Take(6)
            .ToList();
    }

    private async Task<IReadOnlyList<UpcomingTripDto>> BuildUpcomingTripsAsync(DateTime now, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(now);

        var upcoming = await _db.Expeditions
            .Where(e => e.CarExitDate != null && e.CarExitDate >= today)
            .OrderBy(e => e.CarExitDate)
            .Take(5)
            .Select(e => new { e.Id, e.ExpeditionNumber, e.CarExitDate, e.RomorkId, e.StartCityId, e.EndCityId })
            .ToListAsync(ct);

        var carIds = upcoming.Where(e => e.RomorkId != null).Select(e => (long)e.RomorkId!.Value).ToList();
        var plates = await _db.Cars
            .Where(c => carIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.PlateNumber, ct);

        var cityIds = upcoming
            .SelectMany(e => new[] { e.StartCityId, e.EndCityId })
            .Where(id => id != null)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        var cityNames = await _db.Cities
            .Where(c => cityIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        return upcoming.Select(e =>
        {
            var start = e.StartCityId is { } sid ? cityNames.GetValueOrDefault(sid) : null;
            var end = e.EndCityId is { } eid ? cityNames.GetValueOrDefault(eid) : null;
            var route = start is not null && end is not null ? $"{start} → {end}" : null;
            var plate = e.RomorkId is { } rid ? plates.GetValueOrDefault(rid) : null;

            return new UpcomingTripDto(e.Id, e.ExpeditionNumber, route, plate, e.CarExitDate);
        }).ToList();
    }

    private static double PercentChange(double previous, double current)
    {
        if (previous == 0)
            return current == 0 ? 0 : 100;

        return Math.Round(100.0 * (current - previous) / previous, 1);
    }

    private static string TurkishMonthAbbrev(DateTime date) => date.Month switch
    {
        1 => "Oca", 2 => "Şub", 3 => "Mar", 4 => "Nis", 5 => "May", 6 => "Haz",
        7 => "Tem", 8 => "Ağu", 9 => "Eyl", 10 => "Eki", 11 => "Kas", 12 => "Ara",
        _ => date.Month.ToString(),
    };
}
