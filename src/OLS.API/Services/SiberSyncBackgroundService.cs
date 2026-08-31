using OLS.Business.Services.TransferData;
using OLS.DataAccess.Siber;

namespace OLS.API.Services;

/// <summary>
/// Siber'deki İŞLEM verisini (Teklif/Yük/Sefer + alt kalemleri) yerel Postgres'te
/// güncel tutar — <see cref="ISiberSyncService"/>'in periyodik çağrılması.
///
/// Salt-okunur: yalnızca Siber'den SELECT yapar, hiçbir zaman yazmaz. Siber bağlantısı
/// yapılandırılmamışsa (<c>ConnectionStrings:Siber</c> boşsa) tamamen devre dışı kalır.
///
/// İKİ KATMANLI çalışır (kullanıcı isteği: "anlık" hissi versin ama gerçek/canlı
/// Siber sunucusuna tam tarama yükü bindirmesin):
///
///   1) HIZLI KATMAN (<c>Siber:FastCheckIntervalSeconds</c>, varsayılan 15 sn):
///      <see cref="ISiberSyncService.GetRowCountsAsync"/> ile 11 tablonun satır
///      SAYISINI tek ucuz sorguda okur (satır verisi hiç çekilmez). Bir tablonun
///      sayısı bir önceki turdan FARKLIYSA ("yeni kayıt geldi" sezgisi) o tabloyu
///      hemen tam senkronlar — yeni bir Teklif/Yük/Sefer birkaç saniye içinde
///      görünür.
///
///   2) YAVAŞ KATMAN (<c>Siber:SyncIntervalMinutes</c>, varsayılan 5 dk): tüm 11
///      tabloyu KOŞULSUZ tam senkronlar. Bunun nedeni: gerçek Siber şemasında
///      genel bir "son değişiklik tarihi" kolonu yok (yalnızca sfy_modulkalem'de
///      <c>kayitsondegisiklikarih</c> var, diğer 6 tabloda sadece iş-süreci
///      tarihleri var — ör. "onay tarihi") — bu yüzden VAR OLAN bir satırın alan
///      güncellemesi (satır sayısını değiştirmeyen bir değişiklik, ör. bir
///      teklifin müşterisi güncellenmesi) hızlı katmanın COUNT(*) sezgisiyle asla
///      yakalanamaz. Yavaş katman bunu birkaç dakika içinde telafi eder.
///
/// Gerçek artımlı senkron (yalnızca değişeni çek) bu yüzden kurulamadı — SQL
/// Server'ın Change Tracking/CDC özelliği bunu çözerdi ama bu, Siber veritabanının
/// AYARLARINI değiştirmeyi gerektirir (<c>ALTER DATABASE ... SET CHANGE_TRACKING</c>)
/// ve "Siber'e asla yazma" kuralına aykırı düşer.
/// </summary>
public sealed class SiberSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SiberSyncBackgroundService> _logger;
    private readonly TimeSpan _fullSyncInterval;
    private readonly TimeSpan _fastCheckInterval;

    private static readonly (string Label, Func<ISiberSyncService, CancellationToken, Task<SiberImportSummary>> Step, Func<SiberRowCounts, int> Count)[] Entities =
    [
        ("Teklif", (s, ct) => s.SyncOffersAsync(ct), c => c.Offers),
        ("Teklif içeriği", (s, ct) => s.SyncOfferContentsAsync(ct), c => c.OfferContents),
        ("Teklif mali kalemi", (s, ct) => s.SyncOfferFinancialsAsync(ct), c => c.OfferFinancials),
        ("Yük", (s, ct) => s.SyncLoadTransfersAsync(ct), c => c.LoadTransfers),
        ("Yük koli", (s, ct) => s.SyncLoadTransferPackagesAsync(ct), c => c.LoadTransferPackages),
        ("Yük mali kalemi", (s, ct) => s.SyncLoadTransferInvoiceItemsAsync(ct), c => c.LoadTransferInvoiceItems),
        ("Yük evrak", (s, ct) => s.SyncLoadTransferDocumentsAsync(ct), c => c.LoadTransferDocuments),
        ("Sefer", (s, ct) => s.SyncExpeditionsAsync(ct), c => c.Expeditions),
        ("Sefer-Yük eşleme", (s, ct) => s.SyncExpeditionLoadMappingsAsync(ct), c => c.ExpeditionLoadMappings),
        ("Müşteri", (s, ct) => s.SyncAccountsAsync(ct), c => c.Accounts),
        ("Araç", (s, ct) => s.SyncCarsAsync(ct), c => c.Cars),
        // Sabit sayı (0): bu adım onlarca farklı referans tablosunu (ödeme şekli,
        // kap cinsi, para birimi, ülke, ...) TEK seferde günceller — hiçbiri kendi
        // COUNT(*) sinyaliyle hızlı katmanı erkenden tetiklemez, yalnızca yavaş
        // katmanın koşulsuz turunda çalışır (referans verisi saniyeler içinde
        // görünmesi gereken bir şey değil, birkaç dakika içinde yeterli).
        ("Referans Verisi", (s, ct) => s.SyncReferenceDataAsync(ct), _ => 0),
        // Cari görevlileri (sbr_firmatemsilci) — referans verisi gibi yavaş katmanda.
        ("Cari Görevlileri", (s, ct) => s.SyncAccountRepresentativesAsync(ct), _ => 0),
    ];

    private SiberRowCounts? _lastCounts;
    private DateTime _lastFullSync = DateTime.MinValue;

    public SiberSyncBackgroundService(
        IServiceProvider services, IConfiguration configuration, ILogger<SiberSyncBackgroundService> logger)
    {
        _services = services;
        _logger = logger;

        var fullMinutes = configuration.GetValue<int?>("Siber:SyncIntervalMinutes") ?? 5;
        _fullSyncInterval = TimeSpan.FromMinutes(Math.Max(1, fullMinutes));

        var fastSeconds = configuration.GetValue<int?>("Siber:FastCheckIntervalSeconds") ?? 15;
        _fastCheckInterval = TimeSpan.FromSeconds(Math.Max(5, fastSeconds));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var scope = _services.CreateScope())
        {
            var factory = scope.ServiceProvider.GetRequiredService<ISiberConnectionFactory>();
            if (!factory.IsConfigured)
            {
                _logger.LogInformation(
                    "Siber bağlantısı yapılandırılmamış — periyodik senkron devre dışı.");
                return;
            }
        }

        _logger.LogInformation(
            "Siber periyodik senkron başladı (hızlı kontrol: {Fast}, tam senkron: {Full}).",
            _fastCheckInterval, _fullSyncInterval);

        using var timer = new PeriodicTimer(_fastCheckInterval);
        try
        {
            while (true)
            {
                if (DateTime.UtcNow - _lastFullSync >= _fullSyncInterval)
                    await RunFullSyncAsync(stoppingToken);
                else
                    await RunFastCheckAsync(stoppingToken);

                await timer.WaitForNextTickAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal kapanış — uygulama durduruluyor.
        }
    }

    /// <summary>Yavaş katman: koşulsuz, 7 tablonun tamamını tam senkronlar.</summary>
    private async Task RunFullSyncAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var sync = scope.ServiceProvider.GetRequiredService<ISiberSyncService>();

        foreach (var (label, step, _) in Entities)
            await RunStepAsync(label, step, sync, cancellationToken);

        _lastFullSync = DateTime.UtcNow;
        await RefreshCountsAsync(sync, cancellationToken);
    }

    /// <summary>
    /// Hızlı katman: yalnızca satır sayısı değişen tabloları hemen senkronlar.
    /// İlk çalıştırmada (henüz bir önceki tur yoksa) yalnızca temel sayıları kaydeder,
    /// hiçbir şey senkronlamaz — ilk gerçek veri zaten hemen sonraki tam senkronla gelir.
    /// </summary>
    private async Task RunFastCheckAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var sync = scope.ServiceProvider.GetRequiredService<ISiberSyncService>();

        SiberRowCounts current;
        try
        {
            current = await sync.GetRowCountsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Siber hızlı kontrol (satır sayısı) başarısız oldu.");
            return;
        }

        var previous = _lastCounts;
        _lastCounts = current;

        if (previous is null)
            return;

        var changed = Entities.Where(e => e.Count(previous) != e.Count(current)).ToList();
        if (changed.Count == 0)
            return;

        _logger.LogInformation(
            "Siber hızlı kontrol: {Count} tabloda satır sayısı değişti, anlık senkron tetikleniyor.",
            changed.Count);

        foreach (var (label, step, _) in changed)
            await RunStepAsync(label, step, sync, cancellationToken);

        // Az önce senkronlanan tabloların sayısını tazele — aksi hâlde bir sonraki
        // hızlı kontrol aynı değişikliği tekrar "yeni" sanıp gereksiz yeniden senkronlar.
        await RefreshCountsAsync(sync, cancellationToken);
    }

    private async Task RefreshCountsAsync(ISiberSyncService sync, CancellationToken cancellationToken)
    {
        try
        {
            _lastCounts = await sync.GetRowCountsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Siber satır sayıları tazelenemedi — bir sonraki hızlı kontrolde yeniden denenecek.");
            _lastCounts = null;
        }
    }

    private async Task RunStepAsync(
        string label,
        Func<ISiberSyncService, CancellationToken, Task<SiberImportSummary>> step,
        ISiberSyncService sync,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await step(sync, cancellationToken);
            if (result.Created > 0 || result.Updated > 0 || result.Errors.Count > 0)
            {
                _logger.LogInformation(
                    "Siber senkron [{Label}]: {Created} yeni, {Updated} güncellendi, {ErrorCount} hata.",
                    label, result.Created, result.Updated, result.Errors.Count);
            }
            foreach (var error in result.Errors.Take(5))
                _logger.LogWarning("Siber senkron [{Label}] hata: {Error}", label, error);

            // Notlar hata değildir (bkz. SiberImportSummary.Notes) — INF seviyesinde.
            foreach (var note in result.Notes.Take(5))
                _logger.LogInformation("Siber senkron [{Label}] not: {Note}", label, note);
        }
        catch (Exception ex)
        {
            // Bir adım patlarsa döngü durmaz — bir sonraki periyotta yeniden denenir.
            _logger.LogError(ex, "Siber senkron [{Label}] başarısız oldu.", label);
        }
    }
}
