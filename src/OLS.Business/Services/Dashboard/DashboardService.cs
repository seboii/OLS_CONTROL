using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.Business.Seed;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.Expeditions;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

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

    /// <summary>
    /// YOLDAKİ SEFERLER — çıkış yapmış, henüz boşaltılmamış.
    ///
    /// "Aktif sefer" ölçütü (boşaltılmamış) operasyoncu için yanıltıcı: canlıda
    /// 1.271 aktif seferin 1.255'i "10 - HAZIR" durumunda, yani açılmış ama hiç
    /// hareket etmemiş eski kayıtlar. Gerçekten yolda olan 16 sefer var ve
    /// operasyoncunun sabah baktığı sayı bu.
    ///
    /// Ölçüt SIRA NUMARASI: 15 (ÇIKIŞ YAPTI) ile 90 (BOŞALTILDI) arası. Kimlik
    /// yerine numara, çünkü durum satırlarının yerel kimlikleri ortamdan ortama
    /// değişebiliyor.
    /// </summary>
    public required int OnRoadExpeditions { get; init; }
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

    /// <summary>
    /// Bu hafta çıkan sefer sayısı ve geçen haftaya oranı (kullanıcı isteği:
    /// "haftalık toplam seferlerin oranını görsün").
    /// </summary>
    public required int ExpeditionsThisWeek { get; init; }
    public required int ExpeditionsLastWeek { get; init; }
    public required double ExpeditionsWeekChangePercent { get; init; }

    /// <summary>
    /// Kullanıcı PARA verilerini görebilir mi (<c>finance_management</c> okuma).
    ///
    /// Operasyonun para ile işi yok — yalnızca yükün mali kalemini girerken
    /// finansa dokunuyor (kullanıcı isteği). Karar SUNUCUDA veriliyor: arayüzün
    /// gizlemesine güvenmek, tutarı yine de göndermek demekti.
    /// </summary>
    public required bool CanSeeRevenue { get; init; }
}

public sealed record MonthlyPointDto(string Month, int ShipmentCount, decimal Revenue);

public sealed record DistributionSliceDto(string Name, int Count, double Percent);

public sealed record WeeklyPointDto(string Day, int CompletedCount);

public sealed record ActivityItemDto(string Kind, string Text, string Sub, DateTime At);

/// <summary>
/// Yaklaşan sefer satırı. Operasyoncunun bakışıyla: hangi sefer, nereden
/// nereye, hangi araç, KAÇ GÜN KALDI ve şu an hangi durumda.
///
/// <c>DaysLeft</c> ve <c>Status</c> sonradan eklendi: eski kart yalnızca
/// numara/güzergâh/plaka/tarih gösteriyordu ve "Beklemede" etiketi SABİTTİ —
/// yani seferin gerçek durumunu değil, sabit bir metni yazıyordu.
/// </summary>
public sealed record UpcomingTripDto(
    long Id, string? ExpeditionNumber, string? Route, string? PlateNumber, DateOnly? Date)
{
    public int? DaysLeft { get; init; }
    public string? Status { get; init; }
    public string? Driver { get; init; }
}

public sealed class DashboardService : IDashboardService
{
    private readonly OlsDbContext _db;
    private readonly IClock _clock;
    private readonly ICompanyScope _companyScope;
    private readonly ICurrentUser _currentUser;
    private readonly IPermissionService _permissions;

    public DashboardService(
        OlsDbContext db, IClock clock, ICompanyScope companyScope,
        ICurrentUser currentUser, IPermissionService permissions)
    {
        _db = db;
        _clock = clock;
        _companyScope = companyScope;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    /// <summary>
    /// Panelin saydigi kayit kumeleri.
    ///
    /// Panel eskiden <c>_db.Loads</c>/<c>_db.LoadTransfers</c>/<c>_db.Expeditions</c>
    /// uzerinden HAM sayiyordu; iki kural birden atlanmisti:
    ///   - SIRKET KAPSAMI: Avrora kullanicisi listede 762 yuk gorurken panelde
    ///     butun sirketlerin toplamini goruyordu.
    ///   - SILINMIS KAYIT: listeler <c>siber_deleted_at</c> damgali kayitlari
    ///     gizliyor, panel sayiyordu; ayni ekranda iki farkli toplam cikiyordu.
    ///
    /// Kaynaklar tek yerde kurulup alt yapicilara gecirilir ki yeni bir kart
    /// eklenirken filtre atlanmasin.
    /// </summary>
    private sealed record Sources(
        IQueryable<Load> Loads,
        IQueryable<LoadTransfer> LoadTransfers,
        IQueryable<Expedition> Expeditions);

    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var visibility = await _companyScope.ResolveAsync(_currentUser.Id, cancellationToken);
        var src = new Sources(
            _db.Loads.VisibleTo(visibility).Live(),
            _db.LoadTransfers.VisibleTo(visibility).Live(),
            _db.Expeditions.VisibleTo(visibility).Live());

        var now = _clock.Now;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var prevMonthStart = monthStart.AddMonths(-1);
        var weekStart = now.Date.AddDays(-(int)now.DayOfWeek + (now.DayOfWeek == DayOfWeek.Sunday ? -6 : 1));

        var metrics = await BuildMetricsAsync(src, now, monthStart, prevMonthStart, cancellationToken);
        var monthlyShipments = await BuildMonthlyShipmentsAsync(src, monthStart, cancellationToken);
        var workTypeDistribution = await BuildWorkTypeDistributionAsync(src, cancellationToken);
        var weeklyCompleted = await BuildWeeklyCompletedAsync(src, weekStart, cancellationToken);
        var recentActivity = await BuildRecentActivityAsync(cancellationToken);
        var upcomingTrips = await BuildUpcomingTripsAsync(src, now, cancellationToken);

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

    /// <summary>Siber'de "boşaltıldı" durumu (skn_pozisyondurum.pozisyondurumid=14);
    /// bu durumdaki sefer tamamlanmış sayılır (bkz. SiberExpeditionRepository.UnloadedStatusId
    /// - aynı sabit orada aktif-sefer kontrolü için kullanılıyor).</summary>
    private const int UnloadedStatusCode = 14;

    /// <summary>
    /// Sefer durumlarının SIRA numaraları (Siber'in kendi numaralandırması):
    /// 10 HAZIR · 15 ÇIKIŞ YAPTI · 20 SEFERE ATANMIŞ · 30 YÜKLEME İÇİN YOLDA ·
    /// 40 YÜKLENDİ · 50 YÜKLEME GÜMRÜĞÜNDE · 60 YURT İÇİ YOLDA ·
    /// 70 YURT DIŞI YOLDA · 80 BOŞALTMADA · 90 BOŞALTILDI.
    /// </summary>
    private const int OnRoadFromOrder = 15;
    private const int UnloadedOrder = 90;

    /// <summary>Haftanın başı (Pazartesi) — Pazar günü bir önceki haftaya sayılır.</summary>
    private static DateTime StartOfWeek(DateTime now) =>
        now.Date.AddDays(-(int)now.DayOfWeek + (now.DayOfWeek == DayOfWeek.Sunday ? -6 : 1));

    private async Task<DashboardMetricsDto> BuildMetricsAsync(
        Sources src, DateTime now, DateTime monthStart, DateTime prevMonthStart, CancellationToken ct)
    {
        var unloadedStatusId = await _db.ExpeditionStatuses
            .Where(s => s.ExpeditionStatusId == UnloadedStatusCode)
            .Select(s => (long?)s.Id)
            .FirstOrDefaultAsync(ct);

        // "Dönüşü girilmemiş" tek başına yanıltıcı - boşaltıldı olarak işaretlenmiş bir sefer
        // dönüş tarihi girilmemiş olsa bile artık aktif değildir (kullanıcı geri bildirimi).
        // "Boşaltıldı" durumu yerelde hiç tanımlı değilse (ör. Siber içe aktarımı hiç
        // yapılmamış taze bir ortam) eski dönüş-tarihi sinyaline düşülür.
        var activeExpeditions = unloadedStatusId is { } uid1
            ? await src.Expeditions.CountAsync(e => e.StatusId != uid1, ct)
            : await src.Expeditions.CountAsync(e => e.ReturnDate == null, ct);
        var activeExpeditionsLastMonth = unloadedStatusId is { } uid2
            ? await src.Expeditions.CountAsync(
                e => e.StatusId != uid2 && e.CreatedAt != null && e.CreatedAt < monthStart, ct)
            : await src.Expeditions.CountAsync(
                e => e.ReturnDate == null && e.CreatedAt != null && e.CreatedAt < monthStart, ct);

        var loadTransfersThisMonth = await src.LoadTransfers.CountAsync(
            l => l.CreatedAt != null && l.CreatedAt >= monthStart, ct);
        var loadTransfersLastMonth = await src.LoadTransfers.CountAsync(
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
            : await src.Loads.CountAsync(l => l.StatusTypeId == offerStatusId, ct);

        // CARI VE FATURA SIRKETE GORE SUZULEMEZ: accounts ve invoices tablolarinda
        // yerel sirket sutunu yok (sirket yalnizca Siber'e yazarken cozuluyor).
        // Silinmis cariler yine de elenir - liste de eliyor.
        var activeCustomers = await _db.Accounts.CountAsync(a => a.SiberDeletedAt == null, ct);
        var activeCustomersLastMonth = await _db.Accounts.CountAsync(
            a => a.SiberDeletedAt == null && a.CreatedAt != null && a.CreatedAt < monthStart, ct);

        var expeditionsThisMonth = await src.Expeditions
            .Where(e => e.CreatedAt != null && e.CreatedAt >= monthStart)
            .Select(e => new { e.ReturnDate, e.StatusId })
            .ToListAsync(ct);
        var completionRate = expeditionsThisMonth.Count == 0
            ? 0
            : 100.0 * expeditionsThisMonth.Count(e => unloadedStatusId is { } uid3 ? e.StatusId == uid3 : e.ReturnDate != null)
                / expeditionsThisMonth.Count;

        // YOLDAKİ SEFER — kural tek yerde (bkz. ExpeditionOnRoad).
        //
        // Yalnızca duruma bakmak YETMİYOR: Avrora sefer durumunu hiç
        // ilerletmiyor ve yolda olup olmadığı tarihlerden okunuyor. Bu yüzden
        // kart Avrora'da hep 0 gösteriyordu.
        var onRoadStatusIds = await _db.ExpeditionStatuses
            .Where(s => s.OrderNumber != null
                     && s.OrderNumber >= ExpeditionOnRoad.DepartedOrder
                     && s.OrderNumber < ExpeditionOnRoad.UnloadedOrder)
            .Select(s => s.Id)
            .ToListAsync(ct);

        var finishedStatusIds = await _db.ExpeditionStatuses
            .Where(s => s.OrderNumber != null && s.OrderNumber >= ExpeditionOnRoad.UnloadedOrder)
            .Select(s => s.Id)
            .ToListAsync(ct);

        var onRoadExpeditions = await src.Expeditions.CountAsync(
            ExpeditionOnRoad.Predicate(onRoadStatusIds, finishedStatusIds, DateOnly.FromDateTime(now)), ct);

        // HAFTALIK SEFER ORANI. Ölçüt çıkış tarihi: seferin fiilen o hafta
        // yola çıkıp çıkmadığını söyleyen tek alan (kayıt tarihi senkron
        // damgası taşıyabiliyor).
        var weekStart = StartOfWeek(now);
        var lastWeekStart = weekStart.AddDays(-7);
        var thisWeekEnd = weekStart.AddDays(7);

        var expeditionsThisWeek = await src.Expeditions.CountAsync(
            e => e.ReleaseDate != null
              && e.ReleaseDate >= DateOnly.FromDateTime(weekStart)
              && e.ReleaseDate < DateOnly.FromDateTime(thisWeekEnd), ct);

        var expeditionsLastWeek = await src.Expeditions.CountAsync(
            e => e.ReleaseDate != null
              && e.ReleaseDate >= DateOnly.FromDateTime(lastWeekStart)
              && e.ReleaseDate < DateOnly.FromDateTime(weekStart), ct);

        // PARA YETKİSİ. Operasyonun para ile işi yok; tutar, yetkisi olmayana
        // hiç GÖNDERİLMİYOR (gizlemek yetmez).
        var canSeeRevenue = _currentUser.Id is { } userId
            && await _permissions.HasPermissionAsync(
                userId, "finance_management", PermissionAction.Read, ct);

        return new DashboardMetricsDto
        {
            ActiveExpeditions = activeExpeditions,
            OnRoadExpeditions = onRoadExpeditions,
            ExpeditionsThisWeek = expeditionsThisWeek,
            ExpeditionsLastWeek = expeditionsLastWeek,
            ExpeditionsWeekChangePercent = PercentChange(expeditionsLastWeek, expeditionsThisWeek),
            CanSeeRevenue = canSeeRevenue,
            ActiveExpeditionsDelta = activeExpeditions - activeExpeditionsLastMonth,
            LoadTransfersThisMonth = loadTransfersThisMonth,
            LoadTransfersThisMonthChangePercent = PercentChange(loadTransfersLastMonth, loadTransfersThisMonth),
            RevenueThisMonth = canSeeRevenue ? revenueThisMonth : 0m,
            RevenueThisMonthChangePercent = canSeeRevenue
                ? PercentChange((double)revenueLastMonth, (double)revenueThisMonth)
                : 0,
            PendingQuotes = pendingQuotes,
            PendingQuotesDelta = 0,
            ActiveCustomers = activeCustomers,
            ActiveCustomersDelta = activeCustomers - activeCustomersLastMonth,
            CompletionRatePercent = Math.Round(completionRate, 1),
            CompletionRateDeltaPoints = 0,
        };
    }

    private async Task<IReadOnlyList<MonthlyPointDto>> BuildMonthlyShipmentsAsync(
        Sources src, DateTime monthStart, CancellationToken ct)
    {
        var points = new List<MonthlyPointDto>();

        for (var i = 5; i >= 0; i--)
        {
            var start = monthStart.AddMonths(-i);
            var end = start.AddMonths(1);

            var count = await src.LoadTransfers.CountAsync(
                l => l.CreatedAt != null && l.CreatedAt >= start && l.CreatedAt < end, ct);
            var revenue = await _db.Invoices
                .Where(inv => inv.InvoiceCreateDate >= start && inv.InvoiceCreateDate < end)
                .SumAsync(inv => (decimal?)inv.PayableAmount, ct) ?? 0m;

            points.Add(new MonthlyPointDto(TurkishMonthAbbrev(start), count, revenue));
        }

        return points;
    }

    private async Task<IReadOnlyList<DistributionSliceDto>> BuildWorkTypeDistributionAsync(
        Sources src, CancellationToken ct)
    {
        var total = await src.LoadTransfers.CountAsync(l => l.WorkType != null, ct);

        if (total == 0)
            return [];

        var grouped = await src.LoadTransfers
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

    private async Task<IReadOnlyList<WeeklyPointDto>> BuildWeeklyCompletedAsync(
        Sources src, DateTime weekStart, CancellationToken ct)
    {
        string[] dayLabels = ["Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz"];
        var weekStartDate = DateOnly.FromDateTime(weekStart);

        var completedThisWeek = await src.Expeditions
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

    /// <summary>
    /// SON AKTİVİTELER — GERÇEK KULLANICI HAREKETLERİ.
    ///
    /// BULUNAN GERÇEK HATA: liste yerel tabloların <c>created_at</c> alanından
    /// üretiliyordu. O alan, SENKRONLA gelen kayıtlarda kaydın açıldığı anı
    /// değil, senkron turunun saatini taşıyor — canlıda tek bir turda onlarca
    /// yük/teklif aynı saniyeye damgalanıyor. Sonuç: "Son Aktiviteler" gerçek
    /// hareketi değil, senkron gürültüsünü listeliyordu; üstelik kimin ne
    /// yaptığı hiç yazmıyordu.
    ///
    /// Doğru kaynak <c>siber_change_logs</c> (Siber'in kendi değişiklik
    /// günlüğünün aynası): 256.322 satır, kullanıcı KODU, kayıt etiketi ve
    /// işlem türüyle birlikte ve canlı ilerliyor.
    ///
    /// PARA MODÜLLERİ DIŞARIDA: operasyoncunun panelinde fatura/tahsilat
    /// hareketi işi değil (kullanıcı isteği). Yalnızca yük, teklif, sefer ve
    /// cari hareketleri gösteriliyor.
    /// </summary>
    private async Task<IReadOnlyList<ActivityItemDto>> BuildRecentActivityAsync(
        CancellationToken ct)
    {
        var rows = await _db.SiberChangeLogs.AsNoTracking()
            .Where(l => l.ChangedAt != null && ActivityTables.Contains(l.TableName))
            .OrderByDescending(l => l.ChangedAt)
            .Take(12)
            .Select(l => new
            {
                l.TableName,
                l.Operation,
                l.RecordLabel,
                l.UserCode,
                l.ChangedAt,
                UserName = _db.Users.Where(u => u.Id == l.UserId).Select(u => u.Name).FirstOrDefault(),
            })
            .ToListAsync(ct);

        return rows
            .Select(r => new ActivityItemDto(
                ActivityKind(r.TableName),
                $"{ActivityLabel(r.TableName)} {r.RecordLabel ?? "—"} {ActivityVerb(r.Operation)}",
                r.UserName ?? r.UserCode ?? "—",
                r.ChangedAt!.Value))
            .ToList();
    }

    /// <summary>
    /// Panelde gösterilen modüller. Fatura (<c>sfy_gelirgider</c>) ve tahsilat
    /// (<c>sfy_tahsilatodeme</c>) BİLEREK yok: operasyonun para hareketiyle işi
    /// yok, yalnızca yükün mali kalemini girerken finansa dokunuyor.
    /// </summary>
    private static readonly string[] ActivityTables =
        ["skn_yuk", "skn_rezervasyon", "skn_pozisyon", "sbr_firma"];

    private static string ActivityKind(string table) => table switch
    {
        "skn_yuk" => "load_transfer",
        "skn_rezervasyon" => "offer",
        "skn_pozisyon" => "expedition",
        _ => "account",
    };

    private static string ActivityLabel(string table) => table switch
    {
        "skn_yuk" => "Yük",
        "skn_rezervasyon" => "Teklif",
        "skn_pozisyon" => "Sefer",
        _ => "Cari",
    };

    /// <summary>Siber: 1 ekleme, 2 güncelleme, 3 silme.</summary>
    private static string ActivityVerb(short? operation) => operation switch
    {
        1 => "açıldı",
        3 => "silindi",
        _ => "güncellendi",
    };

    private async Task<IReadOnlyList<UpcomingTripDto>> BuildUpcomingTripsAsync(
        Sources src, DateTime now, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(now);

        // YAKLAŞAN SEFERİN TARİHİ TEK ALANDAN GELMİYOR.
        //
        // Eski ölçüt yalnızca araç çıkış tarihiydi (car_exit_date) ve canlıda
        // bugünden sonrası için 2 kayıt buluyordu — kart neredeyse hep boştu.
        // Operasyoncunun beklediği tarih hangisi doluysa odur: araç çıkışı,
        // yoksa sefer çıkışı, yoksa yükleme tarihi (sırasıyla 2 / 4 / 7 kayıt).
        var upcoming = await src.Expeditions
            .Where(e => (e.CarExitDate ?? e.ReleaseDate ?? e.LoadingDate) != null
                     && (e.CarExitDate ?? e.ReleaseDate ?? e.LoadingDate) >= today)
            .OrderBy(e => e.CarExitDate ?? e.ReleaseDate ?? e.LoadingDate)
            .Take(6)
            .Select(e => new
            {
                e.Id, e.ExpeditionNumber,
                CarExitDate = e.CarExitDate ?? e.ReleaseDate ?? e.LoadingDate,
                e.RomorkId, e.StartCityId, e.EndCityId,
                Status = _db.ExpeditionStatuses.Where(s => s.Id == e.StatusId).Select(s => s.Name).FirstOrDefault(),
                Driver = _db.Personnel.Where(p => p.Id == e.DriverId).Select(p => p.Name).FirstOrDefault(),
            })
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

            return new UpcomingTripDto(e.Id, e.ExpeditionNumber, route, plate, e.CarExitDate)
            {
                // Sabit "Beklemede" etiketi yerine seferin GERÇEK durumu ve
                // çıkışa kaç gün kaldığı.
                DaysLeft = e.CarExitDate is { } exit ? exit.DayNumber - today.DayNumber : null,
                Status = e.Status,
                Driver = e.Driver,
            };
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
