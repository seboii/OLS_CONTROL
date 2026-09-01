using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OLS.Business.Common;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;
using OLS.DataAccess.Siber;

namespace OLS.Business.Services.TransferData;

/// <summary>
/// Siber'deki İŞLEM (transactional) verisini yerel Postgres'te SÜREKLİ güncel tutar —
/// <see cref="OLS.Business.Services.TransferData.SiberImportService"/>'in tersi yön
/// (o yalnızca referans/tanım verisini, TEK SEFERLİK kurulum için taşır).
///
/// Salt-okunur: Siber'den yalnızca SELECT yapılır, hiçbir zaman yazılmaz. Yerel taraf
/// siber_id'ye göre upsert edilir (idempotent) — bu servis dakikalar/saatler arayla
/// güvenle tekrar tekrar çalıştırılabilir (bkz. <c>SiberSyncBackgroundService</c>).
///
/// Kapsam (bu sürüm): Teklif (skn_rezervasyon) + içerik (skn_rezervasyonyukkoli) +
/// mali kalem (skn_rezervasyontarife). Cari için mevcut
/// <see cref="ISiberImportService.ImportAccountsAsync"/> zaten yeterli, ayrıca
/// portlanmadı. Yük/Sefer sürekli senkronu KAPSAM DIŞI — sonraki adım.
/// </summary>
public interface ISiberSyncService
{
    /// <summary>Siber değişiklik günlüğü — bkz. gerçekleştirimdeki açıklama.</summary>
    Task<SiberImportSummary> SyncChangeLogsAsync(bool full = false, CancellationToken cancellationToken = default);

    Task<SiberImportSummary> SyncOffersAsync(CancellationToken cancellationToken = default);
    Task<SiberImportSummary> SyncOfferContentsAsync(CancellationToken cancellationToken = default);
    Task<SiberImportSummary> SyncOfferFinancialsAsync(CancellationToken cancellationToken = default);
    Task<SiberImportSummary> SyncLoadTransfersAsync(CancellationToken cancellationToken = default);
    Task<SiberImportSummary> SyncLoadTransferPackagesAsync(CancellationToken cancellationToken = default);
    Task<SiberImportSummary> SyncLoadTransferInvoiceItemsAsync(CancellationToken cancellationToken = default);
    Task<SiberImportSummary> SyncExpeditionsAsync(CancellationToken cancellationToken = default);
    Task<SiberImportSummary> SyncLoadTransferDocumentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// skn_yukaktarma — Sefer'e bağlı yükler. BULUNAN GERÇEK BOŞLUK: bu tablo hiçbir
    /// zaman sürekli senkrona eklenmemişti (SiberImportService.cs'deki "pull_skn_yukaktarma
    /// bilinçli olarak portlanmadı" notu, port SIFIRDAN test edilirken yazılmıştı —
    /// artık gerçek Siber'e bağlı çalıştığımız için o gerekçe geçersiz). Sonuç: canlıda
    /// doğrulandı, gerçek Siber'de olup yerelde eksik satırlar vardı — bir sefer
    /// açıldığında "Bağlı Yükler" sekmesi, veri Siber'de olsa bile boş görünüyordu.
    /// </summary>
    Task<SiberImportSummary> SyncExpeditionLoadMappingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// BULUNAN GERÇEK BOŞLUK: Müşteri (sbr_firma) — bkz. <see cref="SyncAccountsAsync"/>'in
    /// XML açıklaması. Aynı "yalnızca tek seferlik içeri alınmış" sorunu.
    /// </summary>
    Task<SiberImportSummary> SyncAccountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// BULUNAN GERÇEK BOŞLUK: Araç (skn_arac) — bkz. <see cref="SyncCarsAsync"/>'ın
    /// XML açıklaması. Aynı "yalnızca tek seferlik içeri alınmış" sorunu.
    /// </summary>
    Task<SiberImportSummary> SyncCarsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// BULUNAN GERÇEK BOŞLUK: Ödeme Şekli/Kap Cinsi/Para Birimi/Ülke gibi
    /// referans/tanım tabloları da yalnızca <see cref="ISiberImportService.ImportReferenceDataAsync"/>
    /// ile TEK SEFERLİK içeri alınmıştı. Canlıda doğrulandı, gerçek Siber'de
    /// yerelden ÇOK daha fazla seçenek vardı — ör. Ödeme Şekli 2→12, Kap Cinsi
    /// 9→123, Para Birimi 4→112, Ülke 173→195. Bu, kullanıcıların formlarda
    /// gerçekte var olan seçenekleri hiç görememesi anlamına geliyordu.
    ///
    /// Şehir (sbr_sehir) BİLİNÇLİ OLARAK bu sürekli turun DIŞINDA tutuldu —
    /// canlıda doğrulandı, o tablo aslında 116 ülkeye yayılmış 118.392 satırlık
    /// dünya çapında bir posta-kodu/mikro-yerleşim veritabanı, "şehir" listesi
    /// değil. <see cref="ISiberImportService.ImportReferenceDataAsync"/>'a
    /// <c>includeCities: false</c> ile çağrılır — bkz. o metodun XML açıklaması.
    /// </summary>
    Task<SiberImportSummary> SyncReferenceDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// sbr_firmatemsilci — cariye bağlı Satış Temsilcisi / Operasyon Yetkilisi.
    /// Teklif açarken müşteri seçilince Görevliler sekmesinin kendiliğinden
    /// dolmasını sağlar (bkz. AccountRepresentative).
    /// </summary>
    Task<SiberImportSummary> SyncAccountRepresentativesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// "Hızlı katman" için ucuz değişiklik sezgisi: 11 tablonun satır sayısını TEK
    /// gidiş-dönüşte okur (satır verisi hiç çekilmez). Siber'in bu tablolarda genel
    /// bir "son değişiklik tarihi" kolonu olmadığından (yalnızca sfy_modulkalem'de
    /// var — bkz. SiberSyncBackgroundService XML açıklaması) gerçek artımlı senkron
    /// kurulamıyor; bunun yerine satır SAYISINDAKİ değişim "yeni kayıt geldi"
    /// sezgisi olarak kullanılıyor. Var olan bir satırın alan güncellemesini
    /// YAKALAMAZ — onu yavaş katmandaki tam senkron karşılıyor.
    /// </summary>
    Task<SiberRowCounts> GetRowCountsAsync(CancellationToken cancellationToken = default);
}

/// <summary>Onbir senkron kaynağının anlık satır sayıları — bkz. <see cref="ISiberSyncService.GetRowCountsAsync"/>.</summary>
public sealed record SiberRowCounts(
    int Offers,
    int OfferContents,
    int OfferFinancials,
    int LoadTransfers,
    int LoadTransferPackages,
    int LoadTransferInvoiceItems,
    int Expeditions,
    int LoadTransferDocuments,
    int ExpeditionLoadMappings,
    int Accounts,
    int Cars);

public sealed class SiberSyncService : ISiberSyncService
{
    private readonly OlsDbContext _db;
    private readonly ISiberConnectionFactory _siber;
    private readonly ISiberImportService _import;
    private readonly ILogger<SiberSyncService> _logger;

    public SiberSyncService(
        OlsDbContext db,
        ISiberConnectionFactory siber,
        ISiberImportService import,
        ILogger<SiberSyncService> logger)
    {
        _db = db;
        _siber = siber;
        _import = import;
        _logger = logger;
    }

    private async Task<IDbConnection> OpenAsync(CancellationToken cancellationToken)
    {
        if (!_siber.IsConfigured)
            throw new InvalidOperationException("Siber bağlantısı yapılandırılmamış.");

        return await _siber.CreateOpenAsync(cancellationToken);
    }

    public async Task<SiberRowCounts> GetRowCountsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await OpenAsync(cancellationToken);

        // Tek gidiş-dönüşte 7 ucuz COUNT(*) — satır verisi hiç çekilmez, yalnızca
        // sayı. Hızlı katman bunu her ~15 saniyede bir çağırıp bir önceki sayıyla
        // karşılaştırır; sunucuya bindirdiği yük tam senkrona kıyasla ihmal
        // edilebilir düzeydedir.
        const string sql = """
            SELECT COUNT(*) FROM skn_rezervasyon;
            SELECT COUNT(*) FROM skn_rezervasyonyukkoli;
            SELECT COUNT(*) FROM skn_rezervasyontarife;
            SELECT COUNT(*) FROM skn_yuk;
            SELECT COUNT(*) FROM skn_yukkoli;
            SELECT COUNT(*) FROM sfy_modulkalem;
            SELECT COUNT(*) FROM skn_pozisyon;
            SELECT COUNT(*) FROM skn_yukevrak;
            SELECT COUNT(*) FROM skn_yukaktarma;
            SELECT COUNT(*) FROM sbr_firma;
            SELECT COUNT(*) FROM skn_arac;
            """;

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return new SiberRowCounts(
            Offers: await multi.ReadSingleAsync<int>(),
            OfferContents: await multi.ReadSingleAsync<int>(),
            OfferFinancials: await multi.ReadSingleAsync<int>(),
            LoadTransfers: await multi.ReadSingleAsync<int>(),
            LoadTransferPackages: await multi.ReadSingleAsync<int>(),
            LoadTransferInvoiceItems: await multi.ReadSingleAsync<int>(),
            Expeditions: await multi.ReadSingleAsync<int>(),
            LoadTransferDocuments: await multi.ReadSingleAsync<int>(),
            ExpeditionLoadMappings: await multi.ReadSingleAsync<int>(),
            Accounts: await multi.ReadSingleAsync<int>(),
            Cars: await multi.ReadSingleAsync<int>());
    }

    /// <summary>siber_id (string) → yerel id eşlemesi, tek sorguyla belleğe alınır.</summary>
    private static Dictionary<string, long> BySiberId<T>(
        IEnumerable<T> rows, Func<T, string?> siberId, Func<T, long> id)
    {
        var map = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var key = siberId(row);
            if (!string.IsNullOrWhiteSpace(key) && !map.ContainsKey(key))
                map[key] = id(row);
        }

        return map;
    }

    /// <summary>
    /// Bir senkron adımının "bu Siber kaydı yerelde var mı" sözlüğü — kendi
    /// upsert anahtarına (siber_id/yukid/yukkoliid/modulkalemid) göre TAM entity
    /// döner (BySiberId'nin aksine, güncelleme için tüm satıra ihtiyaç var).
    ///
    /// ToDictionaryAsync DEĞİL: canlıda doğrulandı, SQL Server'ın
    /// uniqueidentifier→VARCHAR CAST'i BÜYÜK harf üretiyor, ama yerelde farklı
    /// case'le kaydedilmiş satırlar da bulunabiliyor — case-sensitive eşleşme
    /// bunları hiç bulamayıp HER TAM SENKRONDA yeni bir kopya oluşturuyordu
    /// (canlıda ölçüldü: skn_pozisyon için ~8.700 kaydın ~4.300'ü, skn_rezervasyon
    /// için ~18.900'ün ~18.750'si ikiye katlanmıştı). Case-insensitive + "ilk
    /// eşleşen kazanır" bunu kalıcı önler; zaten var olan kopyalar ayrı bir
    /// temizlik betiğiyle (bkz. dup-cleanup) birleştirildi.
    /// </summary>
    private static async Task<Dictionary<string, T>> ExistingByKeyAsync<T>(
        IQueryable<T> query, Func<T, string?> key, CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in await query.ToListAsync(cancellationToken))
            if (key(row) is { } k) map.TryAdd(k, row);

        return map;
    }

    /// <summary>Kod (string) → yerel id eşlemesi — isturu/talimatgelissekli/istenenromorkcins/yukturkod gibi Siber'in ham kod sütunları için.</summary>
    private static Dictionary<string, long> ByCode<T>(
        IEnumerable<T> rows, Func<T, string?> code, Func<T, long> id)
    {
        var map = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var key = code(row);
            if (!string.IsNullOrWhiteSpace(key) && !map.ContainsKey(key))
                map[key] = id(row);
        }

        return map;
    }

    /// <summary>
    /// <see cref="ByCode{T}"/>'un Türkçe I/İ duyarsız sürümü — Siber'de kullanıcı adları
    /// NOKTASIZ I ile ("HEDIYE ARIDICI"), yerelde NOKTALI İ ile ("HEDİYE ARİDİCİ")
    /// yazılabiliyor. <c>OrdinalIgnoreCase</c> bunları farklı görüyor; canlıda doğrulandı,
    /// bu yüzden bazı yüklerin müşteri temsilcisi çözülemiyordu.
    /// </summary>
    private static Dictionary<string, long> ByTurkishName<T>(
        IEnumerable<T> rows, Func<T, string?> name, Func<T, long> id)
    {
        var map = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var key = name(row);
            if (string.IsNullOrWhiteSpace(key)) continue;
            var normalized = QueryableExtensions.NormalizeTurkish(key);
            if (!map.ContainsKey(normalized)) map[normalized] = id(row);
        }

        return map;
    }

    /// <summary>Sayısal kod → yerel id eşlemesi — CarType/CarOwner/CarStatusType gibi tinyint kod sütunları için.</summary>
    private static Dictionary<int, long> ByIntCode<T>(
        IEnumerable<T> rows, Func<T, int?> code, Func<T, long> id)
    {
        var map = new Dictionary<int, long>();
        foreach (var row in rows)
        {
            var key = code(row);
            if (key is { } k && !map.ContainsKey(k))
                map[k] = id(row);
        }

        return map;
    }

    // NOT: bu satır DTO'ları BİLİNÇLİ olarak "record(...)" pozisyonel yapıcı yerine
    // mutable class + property olarak tanımlanır — Dapper'ın çok parametreli (20+)
    // pozisyonel record constructor eşleştirmesi canlıda tutarsız davrandı (bazı satırlar
    // "uygun constructor yok" hatasıyla materialize edilemedi, bkz. bu değişikliğin
    // commit notu). Property tabanlı sınıflar Dapper'ın en güvenilir, standart yolu.

    /// <summary>
    /// Siber kullanıcı kodu → yerel kullanıcı kimliği. Kod ham hâliyle de
    /// saklandığı için eşleşmeyen kod (ayrılmış personel; 91 koddan 3'ü)
    /// bilgiyi kaybettirmez, yalnızca bağlantısız kalır.
    /// </summary>
    private async Task<Dictionary<string, long>> SiberUserCodeMapAsync(
        CancellationToken cancellationToken)
    {
        var rows = await _db.Users.AsNoTracking()
            .Where(u => u.SiberCode != null && u.SiberCode != "")
            .Select(u => new { u.Id, u.SiberCode })
            .ToListAsync(cancellationToken);

        var map = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
            map[row.SiberCode!.Trim()] = row.Id;

        return map;
    }

    /// <summary>
    /// Siber'in denetim alanlarını kayda işler.
    ///
    /// "Kim açtı" Siber'de üç tabloda da %100 dolu; "kim son dokundu" teklifte
    /// %81, yükte %85, seferde %30 dolu — bu yüzden güncelleme alanları boş
    /// gelirse MEVCUT değer korunur, null'a çekilmez.
    /// </summary>
    private static void ApplySiberAudit(
        string? insUser, DateTime? insTime, string? updUser, DateTime? updTime,
        IReadOnlyDictionary<string, long> userCodes,
        Action<string?, long?, DateTime?> setCreated,
        Action<string?, long?, DateTime?> setUpdated)
    {
        if (!string.IsNullOrWhiteSpace(insUser))
        {
            var code = insUser.Trim();
            setCreated(code, userCodes.TryGetValue(code, out var id) ? id : null, insTime);
        }
        else if (insTime is not null)
        {
            setCreated(null, null, insTime);
        }

        if (!string.IsNullOrWhiteSpace(updUser))
        {
            var code = updUser.Trim();
            setUpdated(code, userCodes.TryGetValue(code, out var id) ? id : null, updTime);
        }
    }

    /// <summary>
    /// Silme kontrolünün çalışması için Siber'den gelmesi gereken asgari oran.
    /// Altında kalırsa çekim eksik sayılır ve hiçbir kayıt işaretlenmez.
    /// </summary>
    private const double MinimumFetchRatioForDeletionCheck = 0.5;

    /// <summary>Siber <c>sbr_log.yapilanislem</c> silme kodu.</summary>
    private const short SiberDeleteOperation = 3;

    /// <summary>
    /// Silme kontrolü güvenli mi? Siber'den gelen kayıt sayısı, yerelde bilinen
    /// sayının yarısının altındaysa çekim EKSİK sayılır ve hiçbir kayıt
    /// işaretlenmez.
    ///
    /// Bu eşik olmadan, yarım dönen ya da hataya düşen tek bir çekim tüm tabloyu
    /// "Siber'de silinmiş" diye damgalar; kullanıcı ertesi gün listelerini boş
    /// bulurdu. Yanlış işaretlemenin bedeli, birkaç turluk gecikmenin bedelinden
    /// çok daha yüksek.
    /// </summary>
    public static bool ShouldSkipDeletionCheck(int fetchedCount, int localCount) =>
        localCount > 0 && fetchedCount < localCount * MinimumFetchRatioForDeletionCheck;


    /// <summary>
    /// Siber'den SİLİNMİŞ kayıtları işaretler.
    ///
    /// Uygulama dışında (doğrudan Siber ekranından) silinen bir yük/teklif/sefer,
    /// bu kontrol olmadan yerelde sonsuza kadar canlı görünüyordu. Ölçüm: 6 teklif,
    /// 27 yük ve 6 sefer bu durumdaydı.
    ///
    /// Kayıt SİLİNMEZ, yalnızca işaretlenir: bağlı finans kayıtları, evrak arşivi
    /// ve denetim izi korunmalı; ayrıca Siber'de yanlışlıkla silinip geri alınan
    /// bir kaydın yerel geçmişi de kaybolmamalı (kayıt yeniden görünürse işaret
    /// senkron sırasında temizleniyor).
    ///
    /// GÜVENLİK EŞİĞİ: Siber'den gelen küme, yerelde bilinen kayıt sayısının
    /// <see cref="MinimumFetchRatioForDeletionCheck"/> oranından azsa HİÇBİR ŞEY
    /// işaretlenmez. Yarım dönen ya da hataya düşen bir çekim, aksi hâlde tüm
    /// tabloyu "silindi" diye damgalardı — bu, sessizce en pahalı hata olurdu.
    /// </summary>
    private async Task<string?> MarkMissingAsDeletedAsync<TEntity>(
        string label,
        string siberTableName,
        IQueryable<TEntity> localRows,
        Func<TEntity, string?> keySelector,
        Func<TEntity, string?> labelSelector,
        Action<TEntity, DateTime?> setDeleted,
        Func<TEntity, DateTime?> getDeleted,
        Action<TEntity, string?, long?, DateTime?> setDeletedBy,
        IReadOnlyCollection<string> siberKeys,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var local = await localRows.ToListAsync(cancellationToken);
        if (local.Count == 0)
            return null;

        if (ShouldSkipDeletionCheck(siberKeys.Count, local.Count))
        {
            _logger.LogWarning(
                "{Label}: Siber'den {Fetched} kayıt geldi ama yerelde {Local} var — " +
                "silme kontrolü GÜVENLİK EŞİĞİ nedeniyle atlandı.",
                label, siberKeys.Count, local.Count);

            return $"{label}: silme kontrolü atlandı (Siber'den beklenenden az kayıt geldi).";
        }

        var present = new HashSet<string>(siberKeys, StringComparer.OrdinalIgnoreCase);
        var now = DateTime.Now;

        var missing = local
            .Select(e => new { Entity = e, Key = keySelector(e) })
            .Where(x => x.Key is not null && !present.Contains(x.Key) && getDeleted(x.Entity) is null)
            .ToList();

        if (missing.Count == 0)
            return null;

        // SILENI BUL: Siber kendi gunlugunde (sbr_log) silme kaydi tutuyor.
        // Varsa kimin ne zaman sildigi oradan gelir; yoksa yalnizca "fark
        // edildi" damgasi kalir (uygulamanin acip Siber ekranindan gecmemis
        // kayitlarda gunluk satiri olusmuyor).
        var missingKeys = missing.Select(x => x.Key!).ToList();

        var deletionLogs = await _db.SiberChangeLogs.AsNoTracking()
            .Where(l => l.TableName == siberTableName &&
                        l.Operation == SiberDeleteOperation &&
                        missingKeys.Contains(l.RecordId))
            .Select(l => new { l.RecordId, l.UserCode, l.UserId, l.ChangedAt })
            .ToListAsync(cancellationToken);

        var deletionByKey = deletionLogs
            .GroupBy(l => l.RecordId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.ChangedAt).First(),
                StringComparer.OrdinalIgnoreCase);

        var marked = 0;
        var withDeleter = 0;

        foreach (var item in missing)
        {
            setDeleted(item.Entity, now);

            deletionByKey.TryGetValue(item.Key!, out var log);

            if (log is not null)
            {
                setDeletedBy(item.Entity, log.UserCode, log.UserId, log.ChangedAt);
                withDeleter++;
            }

            // DENETIM KAYDINA DUS: silinme, yoneticinin izledigi akista
            // gorunmeli. Arka plan senkronunun HttpContext'i olmadigi icin
            // AuditSaveChangesInterceptor bunu yakalamiyor, satir elle yazilir.
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = log?.UserId,
                UserName = log?.UserCode ?? "Siber",
                Action = "deleted",
                EntityType = label,
                EntityId = item.Key,
                EntityLabel = labelSelector(item.Entity),
                CreatedAt = now,
            });

            marked++;
        }

        await _db.SaveChangesAsync(cancellationToken);

        if (withDeleter > 0)
            _logger.LogInformation(
                "{Label}: {Count} silinen kaydin {WithDeleter} tanesinde silen kullanici Siber gunlugunden bulundu.",
                label, marked, withDeleter);

        _logger.LogWarning(
            "{Label}: Siber'de bulunmayan {Count} kayıt 'silindi' olarak işaretlendi.",
            label, marked);

        return $"{label}: {marked} kayıt Siber'de bulunamadı, silindi olarak işaretlendi.";
    }


    /// <summary>
    /// Siber'in değişiklik günlüğü (<c>sbr_log</c>) — bir kaydın TAM işlem
    /// geçmişi: kim, ne zaman, hangi alanı, hangi değerden hangi değere.
    ///
    /// <c>insuser</c>/<c>upduser</c> yalnızca açan ve son dokunanı verir;
    /// aradaki her işlem burada. Kapsam üç operasyon tablosuyla sınırlı
    /// (yük 36.941, teklif 31.331, sefer 18.000 satır) — sbr_log'un tamamı
    /// 797.855 satır ve büyük kısmı bu uygulamanın göstermediği modüllere ait.
    ///
    /// Günlük satırları DEĞİŞMEZ: bir kez yazıldıktan sonra Siber tarafından
    /// güncellenmiyor. Bu yüzden yalnızca YENİ satırlar çekilir — yerelde en
    /// son görülen tarihten itibaren, bir gün geri örtüşmeyle (aynı gün içinde
    /// sonradan eklenen satırlar kaçmasın diye).
    /// </summary>
    public async Task<SiberImportSummary> SyncChangeLogsAsync(
        bool full = false, CancellationToken cancellationToken = default)
    {
        if (!_siber.IsConfigured)
            return SiberImportSummary.Empty;

        var since = full
            ? (DateTime?)null
            : await _db.SiberChangeLogs
                .OrderByDescending(l => l.ChangedAt)
                .Select(l => l.ChangedAt)
                .FirstOrDefaultAsync(cancellationToken);

        if (since is { } s)
            since = s.AddDays(-1);

        using var connection = await OpenAsync(cancellationToken);

        var rows = (await connection.QueryAsync<ChangeLogRow>(
            new CommandDefinition(
                """
                SELECT LOWER(CAST(logid AS VARCHAR(64)))          AS LogId,
                       LTRIM(RTRIM(tablename))                    AS TableName,
                       LOWER(CAST(tablerecordid AS VARCHAR(64)))  AS RecordId,
                       LTRIM(RTRIM(kullanici))                    AS UserCode,
                       tarih                                      AS ChangedAt,
                       yapilanislem                               AS Operation,
                       CAST(fieldlar AS NVARCHAR(MAX))            AS Fields,
                       CAST(oncekideger AS NVARCHAR(MAX))         AS OldValues,
                       CAST(sonrakideger AS NVARCHAR(MAX))        AS NewValues,
                       findfieldvalue                             AS RecordLabel,
                       LTRIM(RTRIM(islemmodul))                   AS Module
                FROM sbr_log
                WHERE tablename IN ('skn_yuk', 'skn_rezervasyon', 'skn_pozisyon')
                  AND (@Since IS NULL OR tarih >= @Since)
                """,
                new { Since = since },
                commandTimeout: 300,
                cancellationToken: cancellationToken))).ToList();

        if (rows.Count == 0)
            return SiberImportSummary.Empty;

        var existing = await _db.SiberChangeLogs
            .Where(l => since == null || l.ChangedAt >= since)
            .Select(l => l.SiberId)
            .ToListAsync(cancellationToken);

        var known = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
        var userCodes = await SiberUserCodeMapAsync(cancellationToken);

        var created = 0;
        var pending = 0;

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.LogId) || !known.Add(row.LogId))
                continue;

            var code = string.IsNullOrWhiteSpace(row.UserCode) ? null : row.UserCode.Trim();

            _db.SiberChangeLogs.Add(new SiberChangeLog
            {
                SiberId = row.LogId,
                TableName = row.TableName ?? string.Empty,
                RecordId = row.RecordId ?? string.Empty,
                UserCode = code,
                UserId = code is not null && userCodes.TryGetValue(code, out var id) ? id : null,
                ChangedAt = row.ChangedAt,
                Operation = row.Operation,
                Fields = StripNul(row.Fields),
                OldValues = StripNul(row.OldValues),
                NewValues = StripNul(row.NewValues),
                RecordLabel = Truncate(StripNul(row.RecordLabel), 510),
                Module = Truncate(StripNul(row.Module), 255),
                CreatedAt = DateTime.Now,
            });

            created++;

            if (++pending >= 5000)
            {
                await _db.SaveChangesAsync(cancellationToken);
                pending = 0;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new SiberImportSummary(created, 0, []);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    /// <summary>
    /// NUL baytlarını (0x00) atar.
    ///
    /// Siber'in günlük metinleri NUL içerebiliyor (sabit uzunluklu alanlardan
    /// gelen dolgu). PostgreSQL <c>text</c> sütunu bunu kabul etmiyor ve tüm
    /// toplu yazma
    /// "invalid byte sequence for encoding UTF8: 0x00" ile düşüyor —
    /// tek bir satır yüzünden 86 bin satırlık aktarım iptal oluyordu.
    /// </summary>
    private static string? StripNul(string? value) =>
        value is null ? null : value.Replace(" ", string.Empty);

    private sealed class ChangeLogRow
    {
        public string? LogId { get; set; }
        public string? TableName { get; set; }
        public string? RecordId { get; set; }
        public string? UserCode { get; set; }
        public DateTime? ChangedAt { get; set; }
        public short? Operation { get; set; }
        public string? Fields { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? RecordLabel { get; set; }
        public string? Module { get; set; }
    }

    private sealed class OfferRow
    {
        public string Rezervasyonid { get; set; } = string.Empty;
        public int? Rezervasyonno { get; set; }
        public string? Talimatgelissekli { get; set; }
        public string? Istenenromorkcins { get; set; }
        public string? Isturu { get; set; }
        public string? Yuklemetip { get; set; }
        public string? Yukturkod { get; set; }
        public DateTime? Pazarlamabildirimtarih { get; set; }
        public DateTime? Talimatgelistarih { get; set; }
        public DateTime? Gecerliliktarih { get; set; }
        /// <summary>Teklifin "Olumlu"ya çekildiği gün (bkz. Load.ApprovalDate).</summary>
        public DateTime? Onaytarih { get; set; }

        /// <summary>Teklifin ait olduğu Siber şirketi — görünürlük ayrımı.</summary>
        public string? Sirketid { get; set; }
        public string? Odemesekliid { get; set; }
        public int? Ontasimatarafimizdanyapilir { get; set; }
        public int? Sontasimatarafimizdanyapilir { get; set; }
        public string? Musteriid { get; set; }
        public string? Navlunfirmaid { get; set; }
        public string? Gondericiid { get; set; }
        public string? Aliciid { get; set; }
        public string? Durumid { get; set; }
        public string? Departmanid { get; set; }
        public string? Aciklama { get; set; }
        public string? Yuklemeulkeid { get; set; }
        public string? Bosaltmaulkeid { get; set; }
        public int? Calismasekli { get; set; }
        /// <summary>skn_rezervasyon.acenteid — senkronda eksikti (Siber'de 17 kayıtta dolu).</summary>
        public string? Acenteid { get; set; }

        /// <summary>
        /// skn_rezervasyon.yukid — teklifin DÖNÜŞTÜĞÜ yük. Senkronda hiç okunmuyordu:
        /// yük doğrudan Siber ekranından açıldığında yereldeki teklifin load_number'ı
        /// boş kalıyordu ve arayüz o teklife hâlâ "Yük Oluştur" öneriyordu (mükerrer
        /// yük riski). Canlıda doğrulandı: 25 teklif bu durumdaydı.
        /// </summary>
        public string? Yukid { get; set; }
    
        // Siber denetim izleri — kim açtı / kim son dokundu.
        public string? InsUser { get; set; }
        public DateTime? InsTime { get; set; }
        public string? UpdUser { get; set; }
        public DateTime? UpdTime { get; set; }
}

    /// <summary>
    /// olsold/TransferSiberService ile AYNI 18 kolon (bkz. SiberLoadRepository.
    /// FindRezervasyonAsync) — şema gerçek Siber'den bu oturumda doğrulandı. Filtre yok:
    /// Siber canlı büyüdükçe her çalıştırmada yeni/güncellenmiş rezervasyonlar yakalanır.
    /// </summary>
    public async Task<SiberImportSummary> SyncOffersAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await OpenAsync(cancellationToken);

        var rows = (await connection.QueryAsync<OfferRow>(
            new CommandDefinition(
                """
                SELECT CAST(rezervasyonid AS VARCHAR(64)) AS Rezervasyonid, rezervasyonno AS Rezervasyonno,
                       LTRIM(RTRIM(talimatgelissekli)) AS Talimatgelissekli,
                       LTRIM(RTRIM(istenenromorkcins)) AS Istenenromorkcins,
                       LTRIM(RTRIM(isturu)) AS Isturu, LTRIM(RTRIM(yuklemetip)) AS Yuklemetip,
                       LTRIM(RTRIM(yukturkod)) AS Yukturkod, pazarlamabildirimtarih AS Pazarlamabildirimtarih,
                       talimatgelistarih AS Talimatgelistarih, gecerliliktarih AS Gecerliliktarih,
                       onaytarih AS Onaytarih,
                       CAST(sirketid AS VARCHAR(64)) AS Sirketid,
                       CAST(odemesekliid AS VARCHAR(64)) AS Odemesekliid,
                       ontasimatarafimizdanyapilir AS Ontasimatarafimizdanyapilir,
                       sontasimatarafimizdanyapilir AS Sontasimatarafimizdanyapilir,
                       CAST(musteriid AS VARCHAR(64)) AS Musteriid, CAST(navlunfirmaid AS VARCHAR(64)) AS Navlunfirmaid,
                       CAST(gondericiid AS VARCHAR(64)) AS Gondericiid, CAST(aliciid AS VARCHAR(64)) AS Aliciid,
                       CAST(durumid AS VARCHAR(64)) AS Durumid, CAST(departmanid AS VARCHAR(64)) AS Departmanid,
                       LTRIM(RTRIM(CAST(aciklama AS NVARCHAR(MAX)))) AS Aciklama,
                       CAST(yuklemeulkeid AS VARCHAR(64)) AS Yuklemeulkeid, CAST(bosaltmaulkeid AS VARCHAR(64)) AS Bosaltmaulkeid,
                       calismasekli AS Calismasekli, CAST(acenteid AS VARCHAR(64)) AS Acenteid,
                       LOWER(CAST(yukid AS VARCHAR(64))) AS Yukid,
                       LTRIM(RTRIM(insuser)) AS InsUser, instime AS InsTime,
                       LTRIM(RTRIM(upduser)) AS UpdUser, updtime AS UpdTime
                FROM skn_rezervasyon
                """,
                cancellationToken: cancellationToken))).ToList();

        var workTypes = ByCode(await _db.WorkTypes.AsNoTracking().ToListAsync(cancellationToken), w => w.Code, w => w.Id);
        var userCodes = await SiberUserCodeMapAsync(cancellationToken);
        var instructions = ByCode(await _db.Instructions.AsNoTracking().ToListAsync(cancellationToken), i => i.Code, i => i.Id);
        var romorkTypes = ByCode(await _db.RomorkTypes.AsNoTracking().ToListAsync(cancellationToken), r => r.Code, r => r.Id);
        var loadingTypes = ByCode(await _db.LoadingTypes.AsNoTracking().ToListAsync(cancellationToken), t => t.Code, t => t.Id);
        var loadTransferTypes = ByCode(await _db.LoadTransferTypes.AsNoTracking().ToListAsync(cancellationToken), t => t.Code, t => t.Id);
        var paymentTypes = BySiberId(await _db.PaymentTypes.AsNoTracking().ToListAsync(cancellationToken), p => p.SiberId, p => p.Id);
        var statusTypes = BySiberId(await _db.StatusTypes.AsNoTracking().ToListAsync(cancellationToken), s => s.SiberId, s => s.Id);
        var departments = BySiberId(await _db.Departments.AsNoTracking().ToListAsync(cancellationToken), d => d.SiberId, d => d.Id);
        var accounts = BySiberId(await _db.Accounts.AsNoTracking().ToListAsync(cancellationToken), a => a.SiberId, a => a.Id);

        var existing = await ExistingByKeyAsync(
            _db.Loads.Where(l => l.SiberId != null).OrderBy(l => l.Id), l => l.SiberId, cancellationToken);

        // Teklif → Yük bağı: skn_rezervasyon.yukid'den yükün numarasına (yuknoisturu).
        // Teklifin load_number'ı bu numaradır; dolu olması "bu teklif zaten yüke
        // dönüştü" demektir ve arayüz o teklife "Yük Oluştur" önermez.
        var loadNumberByYukid = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in await _db.LoadTransfers.AsNoTracking()
                     .Where(t => t.LoadTransferId != null && t.LoadNumberWorkType != null)
                     .Select(t => new { t.LoadTransferId, t.LoadNumberWorkType })
                     .ToListAsync(cancellationToken))
        {
            loadNumberByYukid.TryAdd(t.LoadTransferId!, t.LoadNumberWorkType!);
        }

        var created = 0;
        var updated = 0;
        var errors = new List<string>();

        foreach (var row in rows)
        {
            try
            {
                var isNew = !existing.TryGetValue(row.Rezervasyonid, out var load);
                load ??= new Load { SiberId = row.Rezervasyonid, CreatedAt = DateTime.Now };

                load.ReservationNumber = row.Rezervasyonno?.ToString();
                load.WorkTypeId = row.Isturu is { } wt && workTypes.TryGetValue(wt, out var wtId) ? (int)wtId : load.WorkTypeId;
                load.LoadingTypeId = row.Yuklemetip is { } lt && loadingTypes.TryGetValue(lt, out var ltId) ? (int)ltId : load.LoadingTypeId;
                load.PaymentTypeId = row.Odemesekliid is { } pt && paymentTypes.TryGetValue(pt, out var ptId) ? (int)ptId : load.PaymentTypeId;
                load.StatusTypeId = row.Durumid is { } st && statusTypes.TryGetValue(st, out var stId) ? (int)stId : load.StatusTypeId;
                load.InstructionId = row.Talimatgelissekli is { } ins && instructions.TryGetValue(ins, out var insId) ? (int)insId : load.InstructionId;
                load.RomorkTypeId = row.Istenenromorkcins is { } rom && romorkTypes.TryGetValue(rom, out var romId) ? (int)romId : load.RomorkTypeId;
                load.LoadTransferTypeId = row.Yukturkod is { } ltt && loadTransferTypes.TryGetValue(ltt, out var lttId) ? (int)lttId : load.LoadTransferTypeId;
                load.DepartmentId = row.Departmanid is { } dep && departments.TryGetValue(dep, out var depId) ? (int)depId : load.DepartmentId;
                load.CustomerId = row.Musteriid is { } cu && accounts.TryGetValue(cu, out var cuId) ? (int)cuId : load.CustomerId;
                load.SenderId = row.Gondericiid is { } se && accounts.TryGetValue(se, out var seId) ? (int)seId : load.SenderId;
                load.ReceiverId = row.Aliciid is { } al && accounts.TryGetValue(al, out var alId) ? (int)alId : load.ReceiverId;
                load.CompanyPayFreightId = row.Navlunfirmaid is { } nf && accounts.TryGetValue(nf, out var nfId) ? (int)nfId : load.CompanyPayFreightId;
                load.AgentId = row.Acenteid is { } ac && accounts.TryGetValue(ac, out var acId) ? (int)acId : load.AgentId;
                // Yalnızca ATANIR, asla temizlenmez: Siber'de yukid boş olsa bile
                // yerelde dönüşüm yapılmış olabilir (bkz. ConvertOfferAsync) — üzerine
                // null yazmak "yükü yok" sanılmasına ve mükerrer yüke yol açardı.
                if (row.Yukid is { } yid && loadNumberByYukid.TryGetValue(yid, out var yukNo))
                    load.LoadNumber = yukNo;
                load.OfferDate = row.Talimatgelistarih is { } td ? DateOnly.FromDateTime(td) : load.OfferDate;
                load.OfferValidityDate = row.Gecerliliktarih is { } gd ? DateOnly.FromDateTime(gd) : load.OfferValidityDate;
                load.ApprovalDate = row.Onaytarih is { } od ? DateOnly.FromDateTime(od) : load.ApprovalDate;
                load.SiberCompanyId = row.Sirketid ?? load.SiberCompanyId;

                // Siber'de kayıt yeniden göründüyse silindi işareti kalkar.
                load.SiberDeletedAt = null;

                ApplySiberAudit(row.InsUser, row.InsTime, row.UpdUser, row.UpdTime, userCodes,
                    (code, id, at) => { load.SiberCreatedBy = code; load.SiberCreatedByUserId = id; load.SiberCreatedAt = at; },
                    (code, id, at) => { load.SiberUpdatedBy = code; load.SiberUpdatedByUserId = id; load.SiberUpdatedAt = at; });
                load.MarketingNotificationDate = row.Pazarlamabildirimtarih is { } pbd ? DateOnly.FromDateTime(pbd) : load.MarketingNotificationDate;
                load.Description = row.Aciklama;
                load.FrontTransportationByUs = row.Ontasimatarafimizdanyapilir ?? load.FrontTransportationByUs;
                load.FinalTransportationByUs = row.Sontasimatarafimizdanyapilir ?? load.FinalTransportationByUs;
                load.WayOfWorking = row.Calismasekli ?? load.WayOfWorking;
                // Yuklemeulkeid/Bosaltmaulkeid: cities/countries GUID'i doğrudan Siber
                // GUID'iyle aynı (bkz. proje kuralı) — parse başarısızsa dokunulmaz.
                if (row.Yuklemeulkeid is { } dc && Guid.TryParse(dc, out var dcGuid)) load.DepartureCountryId = dcGuid;
                if (row.Bosaltmaulkeid is { } tc && Guid.TryParse(tc, out var tcGuid)) load.TargetCountryId = tcGuid;
                load.TransferToSiber = 1;
                load.UpdatedAt = DateTime.Now;

                if (isNew)
                {
                    _db.Loads.Add(load);
                    created++;
                }
                else
                {
                    updated++;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{row.Rezervasyonid}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        var deletionNote = await MarkMissingAsDeletedAsync(
            "Teklif",
            "skn_rezervasyon",
            _db.Loads.Where(l => l.SiberId != null),
            l => l.SiberId,
            l => l.ReservationNumber,
            (l, at) => l.SiberDeletedAt = at,
            l => l.SiberDeletedAt,
            (l, code, id, at) => { l.SiberDeletedBy = code; l.SiberDeletedByUserId = id; l.SiberDeletedOn = at; },
            rows.Select(r => r.Rezervasyonid).ToList(),
            cancellationToken);

        return new SiberImportSummary(created, updated, errors)
        {
            Notes = deletionNote is null ? [] : [deletionNote],
        };
    }

    private sealed class OfferContentRow
    {
        public string Rezyukkoliid { get; set; } = string.Empty;
        public string Rezervasyonid { get; set; } = string.Empty;
        public int? Kapadet { get; set; }
        public decimal? En { get; set; }
        public decimal? Boy { get; set; }
        public decimal? Yukseklik { get; set; }
        public string? Malcinsid { get; set; }
        public string? Kapid { get; set; }
        public decimal? Hacim { get; set; }
        public decimal? Burutagirlik { get; set; }
        public decimal? Netagirlik { get; set; }
        public decimal? Lademetre { get; set; }
        public int? Istiflenemez { get; set; }
    }

    /// <summary>
    /// skn_rezervasyonyukkoli — WRITE tarafıyla (SiberReservationRepository) AYNI tablo,
    /// bu yüzden en/boy/yükseklik → width/height/length TERS eşlemesi burada da birebir
    /// korunur (yazma tarafındaki "DİKKAT" notuyla aynı sebep).
    /// </summary>
    public async Task<SiberImportSummary> SyncOfferContentsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await OpenAsync(cancellationToken);

        var rows = (await connection.QueryAsync<OfferContentRow>(
            new CommandDefinition(
                """
                SELECT CAST(rezyukkoliid AS VARCHAR(64)) AS Rezyukkoliid, CAST(rezervasyonid AS VARCHAR(64)) AS Rezervasyonid,
                       kapadet AS Kapadet, en AS En, boy AS Boy, yukseklik AS Yukseklik,
                       CAST(malcinsid AS VARCHAR(64)) AS Malcinsid, CAST(kapid AS VARCHAR(64)) AS Kapid,
                       hacim AS Hacim, burutagirlik AS Burutagirlik, netagirlik AS Netagirlik,
                       lademetre AS Lademetre, istiflenemez AS Istiflenemez
                FROM skn_rezervasyonyukkoli
                """,
                cancellationToken: cancellationToken))).ToList();

        var loadIds = BySiberId(await _db.Loads.AsNoTracking().Where(l => l.SiberId != null).ToListAsync(cancellationToken), l => l.SiberId, l => l.Id);
        var productTypes = BySiberId(await _db.ProductTypes.AsNoTracking().ToListAsync(cancellationToken), p => p.SiberId, p => p.Id);
        var caseTypes = BySiberId(await _db.CaseTypes.AsNoTracking().ToListAsync(cancellationToken), c => c.SiberId, c => c.Id);
        var existing = await ExistingByKeyAsync(
            _db.LoadContents.Where(c => c.SiberId != null).OrderBy(c => c.Id), c => c.SiberId, cancellationToken);

        var created = 0;
        var updated = 0;
        var skipped = 0;
        var errors = new List<string>();

        foreach (var row in rows)
        {
            try
            {
                if (!loadIds.TryGetValue(row.Rezervasyonid, out var loadId))
                {
                    skipped++;
                    continue;
                }

                var isNew = !existing.TryGetValue(row.Rezyukkoliid, out var content);
                content ??= new LoadContent { SiberId = row.Rezyukkoliid, LoadId = loadId, CreatedAt = DateTime.Now };

                content.LoadId = loadId;
                content.Quantity = row.Kapadet;
                // en/boy/yükseklik -> width/height/length TERS (kaynakla birebir, bkz. yazma tarafı notu).
                content.Width = row.En;
                content.Height = row.Boy;
                content.Length = row.Yukseklik;
                content.ProductTypeId = row.Malcinsid is { } mc && productTypes.TryGetValue(mc, out var mcId) ? (int)mcId : content.ProductTypeId;
                content.CaseTypeId = row.Kapid is { } kp && caseTypes.TryGetValue(kp, out var kpId) ? (int)kpId : content.CaseTypeId;
                content.Volume = row.Hacim;
                content.GrossWeight = row.Burutagirlik;
                content.NetWeight = row.Netagirlik;
                content.Lademeter = row.Lademetre;
                // Ters mantık: istiflenemez = 0 ise stackable = 1 (bkz. yazma tarafı notu).
                content.Stackable = row.Istiflenemez == 0 ? 1 : 0;
                content.UpdatedAt = DateTime.Now;

                if (isNew)
                {
                    _db.LoadContents.Add(content);
                    created++;
                }
                else
                {
                    updated++;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{row.Rezyukkoliid}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new SiberImportSummary(created, updated, errors) with
        {
            Errors = skipped > 0 ? [.. errors, $"{skipped} satır atlandı (yerel teklif bulunamadı)."] : errors,
        };
    }

    private sealed class OfferFinancialRow
    {
        public string Rezervasyontarifeid { get; set; } = string.Empty;
        public string Rezervasyonid { get; set; } = string.Empty;
        public decimal? Miktar { get; set; }
        public string? Alisdovizkod { get; set; }
        public decimal? Alisbirimtutar { get; set; }
        public decimal? Alistoplamtutar { get; set; }
        public string? Alisfirmaid { get; set; }
        public string? Satisdovizkod { get; set; }
        public decimal? Satisbirimtutar { get; set; }
        public decimal? Satistoplamtutar { get; set; }
        public string? Satisfirmaid { get; set; }
        public string? Kalemid { get; set; }
        public string? Tasimasekli { get; set; }
    }

    /// <summary>
    /// skn_rezervasyontarife — hem alış hem satış sütun grubunu okuyup hangisi doluysa
    /// onu kullanır (bu oturumda bulunan ve düzeltilen okuma-yönü hatasıyla aynı mantık,
    /// bkz. buysell tespiti).
    /// </summary>
    public async Task<SiberImportSummary> SyncOfferFinancialsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await OpenAsync(cancellationToken);

        var rows = (await connection.QueryAsync<OfferFinancialRow>(
            new CommandDefinition(
                """
                SELECT CAST(rezervasyontarifeid AS VARCHAR(64)) AS Rezervasyontarifeid,
                       CAST(rezervasyonid AS VARCHAR(64)) AS Rezervasyonid, miktar AS Miktar,
                       LTRIM(RTRIM(alisdovizkod)) AS Alisdovizkod, alisbirimtutar AS Alisbirimtutar,
                       alistoplamtutar AS Alistoplamtutar, CAST(alisfirmaid AS VARCHAR(64)) AS Alisfirmaid,
                       LTRIM(RTRIM(satisdovizkod)) AS Satisdovizkod, satisbirimtutar AS Satisbirimtutar,
                       satistoplamtutar AS Satistoplamtutar, CAST(satisfirmaid AS VARCHAR(64)) AS Satisfirmaid,
                       CAST(kalemid AS VARCHAR(64)) AS Kalemid, LTRIM(RTRIM(tasimasekli)) AS Tasimasekli
                FROM skn_rezervasyontarife
                """,
                cancellationToken: cancellationToken))).ToList();

        var loadIds = BySiberId(await _db.Loads.AsNoTracking().Where(l => l.SiberId != null).ToListAsync(cancellationToken), l => l.SiberId, l => l.Id);
        var financialItems = BySiberId(await _db.FinancialItems.AsNoTracking().ToListAsync(cancellationToken), f => f.SiberId, f => f.Id);
        var accounts = BySiberId(await _db.Accounts.AsNoTracking().ToListAsync(cancellationToken), a => a.SiberId, a => a.Id);
        var transportTypes = ByCode(await _db.TransportTypes.AsNoTracking().ToListAsync(cancellationToken), t => t.Code, t => t.Id);
        var currencies = ByCode(await _db.Currencies.AsNoTracking().ToListAsync(cancellationToken), c => c.Code, c => c.Id);
        var existing = await ExistingByKeyAsync(
            _db.LoadFinancialItems.Where(f => f.SiberId != null).OrderBy(f => f.Id), f => f.SiberId, cancellationToken);

        var created = 0;
        var updated = 0;
        var skipped = 0;
        var errors = new List<string>();

        foreach (var row in rows)
        {
            try
            {
                if (!loadIds.TryGetValue(row.Rezervasyonid, out var loadId))
                {
                    skipped++;
                    continue;
                }

                // Yön tespiti: hangi grup (alış/satış) doluysa o. İkisi de boşsa alış varsayılır.
                var satisDolu = (row.Satistoplamtutar ?? 0) != 0 || (row.Satisbirimtutar ?? 0) != 0 || row.Satisfirmaid is not null;
                var alisDolu = (row.Alistoplamtutar ?? 0) != 0 || (row.Alisbirimtutar ?? 0) != 0 || row.Alisfirmaid is not null;
                var isSatis = satisDolu && !alisDolu;

                var doviz = isSatis ? row.Satisdovizkod : row.Alisdovizkod;
                var birim = isSatis ? row.Satisbirimtutar : row.Alisbirimtutar;
                var toplam = isSatis ? row.Satistoplamtutar : row.Alistoplamtutar;
                var firma = isSatis ? row.Satisfirmaid : row.Alisfirmaid;

                var isNew = !existing.TryGetValue(row.Rezervasyontarifeid, out var item);
                item ??= new LoadFinancialItem { SiberId = row.Rezervasyontarifeid, LoadId = loadId, CreatedAt = DateTime.Now };

                item.LoadId = loadId;
                item.Buysell = isSatis ? 2 : 1;
                item.Quantity = row.Miktar.HasValue ? (int)Math.Round(row.Miktar.Value) : item.Quantity;
                item.Item = row.Kalemid is { } ki && financialItems.TryGetValue(ki, out var kiId) ? (int)kiId : item.Item;
                item.AccountId = firma is { } af && accounts.TryGetValue(af, out var afId) ? (int)afId : item.AccountId;
                item.TransportTypeId = row.Tasimasekli is { } ts && transportTypes.TryGetValue(ts, out var tsId) ? (int)tsId : item.TransportTypeId;
                item.NetPrice = birim;
                item.TotalPrice = toplam;
                item.Currency = doviz is { } cur && currencies.TryGetValue(cur, out var curId) ? (int)curId : item.Currency;
                item.UpdatedAt = DateTime.Now;

                if (isNew)
                {
                    _db.LoadFinancialItems.Add(item);
                    created++;
                }
                else
                {
                    updated++;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{row.Rezervasyontarifeid}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new SiberImportSummary(created, updated, errors) with
        {
            Errors = skipped > 0 ? [.. errors, $"{skipped} satır atlandı (yerel teklif bulunamadı)."] : errors,
        };
    }

    private sealed class LoadTransferRow
    {
        public string Yukid { get; set; } = string.Empty;
        public int? Yukno { get; set; }
        public string? Isturu { get; set; }
        public int? Durumid { get; set; }
        public string? Yuklemetip { get; set; }
        public string? Firmaid { get; set; }
        public string? Gondericiid { get; set; }
        public string? Aliciid { get; set; }
        public string? Odemesekliid { get; set; }
        public string? Talimatgelissekli { get; set; }
        public string? Istenenromorkcins { get; set; }
        public decimal? Toplamagirlik { get; set; }
        public decimal? Toplamhacim { get; set; }
        public decimal? Toplamlademetre { get; set; }
        public decimal? Ucretagirlik { get; set; }
        public string? Departmanid { get; set; }
        public string? Operasyondepartmanid { get; set; }
        public string? Yuknoisturu { get; set; }
        public string? Bagliyuknoisturu { get; set; }
        public decimal? Toplamkap { get; set; }
        public string? Yukturkod { get; set; }
        public string? Yuklemeulke { get; set; }
        public string? Bosaltmaulke { get; set; }
        public int? Calismasekli { get; set; }

        // BULUNAN GERÇEK BOŞLUK — aşağıdaki 18 alan sürekli senkronda HİÇ okunmuyordu
        // (bkz. SyncLoadTransfersAsync XML açıklaması). Eşlemeler olsold'un kendi ETL'inden
        // birebir alındı: TransferDataController.php satır ~754-796.
        public int? Bagliyukno { get; set; }
        public string? Musteritemsilcisiad { get; set; }
        public string? Musteritemsilcisi2ad { get; set; }
        public string? Bildirimyapankullanicikod { get; set; }
        public string? Satistemsilcisikod { get; set; }
        public int? Kamyonda { get; set; }
        public int? Kuyrukta { get; set; }
        public int? Cmrduzenlenecek { get; set; }
        public int? Fcrduzenlenecek { get; set; }
        public decimal? Toplamlademetrem3 { get; set; }
        public string? Yuklemekita { get; set; }
        public string? Bosaltmakita { get; set; }
        public string? Teslimsekil { get; set; }
        public int? Ontasimatarafimizdanyapilir { get; set; }
        public int? Sontasimatarafimizdanyapilir { get; set; }
        /// <summary>Yükün ait olduğu Siber şirketi — görünürlük ayrımı.</summary>
        public string? Sirketid { get; set; }

        public DateTime? Talimatgelistarihi { get; set; }
        public DateTime? Istenenvaristarihi { get; set; }
        public DateTime? Hazirolmatarih { get; set; }
        public DateTime? Musteridenalinistarih { get; set; }
    
        // Siber denetim izleri — kim açtı / kim son dokundu.
        public string? InsUser { get; set; }
        public DateTime? InsTime { get; set; }
        public string? UpdUser { get; set; }
        public DateTime? UpdTime { get; set; }
}

    /// <summary>
    /// skn_yuk — WRITE tarafıyla (SiberLoadRepository.InsertYukWithLockedNumberAsync) AYNI
    /// tablo/kolonlar. Kolon adlarındaki alt çizgi öneki (<c>_yuklemeulke</c>) Siber'in
    /// kendi şemasında böyle — kaynakla birebir korunur.
    ///
    /// BULUNAN GERÇEK BOŞLUK: bu metot eskiden skn_yuk'un yalnızca 24 kolonunu okuyordu;
    /// olsold'un kendi ETL'i (TransferDataController.php satır ~754-796) ise 42 alan
    /// eşliyordu. Eksik kalan 18 alan (teslim şekli, ön/son taşıma, talimat/istenen varış/
    /// hazır olma/müşteriden alınış tarihleri, müşteri temsilcisi 1-2, bildirim/satış
    /// temsilcisi kodu, kamyonda/kuyrukta/cmr/fcr, toplam lademetre m³, yükleme/boşaltma
    /// kıtası, bağlı yük no) canlıda doğrulandı: Siber'de DOLU olmalarına rağmen
    /// (ör. müşteriden alınış 7474/7933, müşteri temsilcisi 7913/7933 satırda dolu)
    /// sürekli senkronla oluşan kayıtların %100'ünde boştu — formda "Seçiniz" görünüyordu.
    /// Eski tek-seferlik aktarımdan gelen kayıtlarda dolu olduğu için sorun uzun süre
    /// fark edilmedi; yalnızca YENİ gelen yüklerde ortaya çıkıyordu.
    /// </summary>
    public async Task<SiberImportSummary> SyncLoadTransfersAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await OpenAsync(cancellationToken);

        var rows = (await connection.QueryAsync<LoadTransferRow>(
            new CommandDefinition(
                """
                SELECT LOWER(CAST(yukid AS VARCHAR(64))) AS Yukid, TRY_CAST(yukno AS INT) AS Yukno,
                       CAST(sirketid AS VARCHAR(64)) AS Sirketid,
                       LTRIM(RTRIM(isturu)) AS Isturu, TRY_CAST(durumid AS INT) AS Durumid,
                       LTRIM(RTRIM(yuklemetip)) AS Yuklemetip,
                       CAST(firmaid AS VARCHAR(64)) AS Firmaid, CAST(gondericiid AS VARCHAR(64)) AS Gondericiid,
                       CAST(aliciid AS VARCHAR(64)) AS Aliciid, CAST(odemesekliid AS VARCHAR(64)) AS Odemesekliid,
                       LTRIM(RTRIM(talimatgelissekli)) AS Talimatgelissekli,
                       LTRIM(RTRIM(istenenromorkcins)) AS Istenenromorkcins,
                       TRY_CAST(toplamagirlik AS DECIMAL(18,4)) AS Toplamagirlik,
                       TRY_CAST(toplamhacim AS DECIMAL(18,4)) AS Toplamhacim,
                       TRY_CAST(toplamlademetre AS DECIMAL(18,4)) AS Toplamlademetre,
                       TRY_CAST(ucretagirlik AS DECIMAL(18,4)) AS Ucretagirlik,
                       CAST(departmanid AS VARCHAR(64)) AS Departmanid,
                       CAST(operasyondepartmanid AS VARCHAR(64)) AS Operasyondepartmanid,
                       LTRIM(RTRIM(yuknoisturu)) AS Yuknoisturu, LTRIM(RTRIM(bagliyuknoisturu)) AS Bagliyuknoisturu,
                       TRY_CAST(toplamkap AS DECIMAL(18,4)) AS Toplamkap, LTRIM(RTRIM(yukturkod)) AS Yukturkod,
                       CAST(_yuklemeulke AS VARCHAR(64)) AS Yuklemeulke, CAST(_bosaltmaulke AS VARCHAR(64)) AS Bosaltmaulke,
                       TRY_CAST(calismasekli AS INT) AS Calismasekli,
                       TRY_CAST(bagliyukno AS INT) AS Bagliyukno,
                       LTRIM(RTRIM(musteritemsilcisiad)) AS Musteritemsilcisiad,
                       LTRIM(RTRIM(musteritemsilcisi2ad)) AS Musteritemsilcisi2ad,
                       LTRIM(RTRIM(bildirimyapankullanicikod)) AS Bildirimyapankullanicikod,
                       LTRIM(RTRIM(satistemsilcisikod)) AS Satistemsilcisikod,
                       TRY_CAST(kamyonda AS INT) AS Kamyonda, TRY_CAST(kuyrukta AS INT) AS Kuyrukta,
                       TRY_CAST(cmrduzenlenecek AS INT) AS Cmrduzenlenecek,
                       TRY_CAST(fcrduzenlenecek AS INT) AS Fcrduzenlenecek,
                       TRY_CAST(toplamlademetrem3 AS DECIMAL(18,4)) AS Toplamlademetrem3,
                       LTRIM(RTRIM(_yuklemekita)) AS Yuklemekita, LTRIM(RTRIM(_bosaltmakita)) AS Bosaltmakita,
                       LTRIM(RTRIM(teslimsekil)) AS Teslimsekil,
                       TRY_CAST(ontasimatarafimizdanyapilir AS INT) AS Ontasimatarafimizdanyapilir,
                       TRY_CAST(sontasimatarafimizdanyapilir AS INT) AS Sontasimatarafimizdanyapilir,
                       talimatgelistarihi AS Talimatgelistarihi, istenenvaristarihi AS Istenenvaristarihi,
                       hazirolmatarih AS Hazirolmatarih, musteridenalinistarih AS Musteridenalinistarih,
                       LTRIM(RTRIM(kayitgiren)) AS InsUser, kayitgiristarih AS InsTime,
                       LTRIM(RTRIM(upduser)) AS UpdUser, updtime AS UpdTime
                FROM skn_yuk
                """,
                cancellationToken: cancellationToken))).ToList();

        var workTypes = ByCode(await _db.WorkTypes.AsNoTracking().ToListAsync(cancellationToken), w => w.Code, w => w.Id);
        var userCodes = await SiberUserCodeMapAsync(cancellationToken);
        var instructions = ByCode(await _db.Instructions.AsNoTracking().ToListAsync(cancellationToken), i => i.Code, i => i.Id);
        var romorkTypes = ByCode(await _db.RomorkTypes.AsNoTracking().ToListAsync(cancellationToken), r => r.Code, r => r.Id);
        var loadingTypes = ByCode(await _db.LoadingTypes.AsNoTracking().ToListAsync(cancellationToken), t => t.Code, t => t.Id);
        var loadTransferTypes = ByCode(await _db.LoadTransferTypes.AsNoTracking().ToListAsync(cancellationToken), t => t.Code, t => t.Id);
        var paymentTypes = BySiberId(await _db.PaymentTypes.AsNoTracking().ToListAsync(cancellationToken), p => p.SiberId, p => p.Id);
        var departments = BySiberId(await _db.Departments.AsNoTracking().ToListAsync(cancellationToken), d => d.SiberId, d => d.Id);
        var accounts = BySiberId(await _db.Accounts.AsNoTracking().ToListAsync(cancellationToken), a => a.SiberId, a => a.Id);
        var loadStatusTypes = new Dictionary<int, long>();
        foreach (var s in await _db.LoadStatusTypes.AsNoTracking().ToListAsync(cancellationToken))
            if (s.LoadStatusId is { } code && !loadStatusTypes.ContainsKey(code)) loadStatusTypes[code] = s.Id;

        // olsold ETL: User::pluck('id','siber_name') ve User::pluck('id','siber_code') —
        // müşteri temsilcileri ADA göre, bildirim/satış temsilcisi KODA göre eşlenir.
        var users = await _db.Users.AsNoTracking().OrderBy(u => u.Id).ToListAsync(cancellationToken);
        var userBySiberName = ByTurkishName(users, u => u.SiberName, u => u.Id);
        var userBySiberCode = ByCode(users, u => u.SiberCode, u => u.Id);
        // olsold ETL: LoadTransferDeliveryMethod::pluck('id','edikod') — skn_yuk.teslimsekil
        // bir GUID değil, EDI kodudur (char).
        var deliveryMethodByEdikod = ByCode(
            await _db.LoadTransferDeliveryMethods.AsNoTracking().ToListAsync(cancellationToken),
            d => d.Edikod, d => d.Id);

        var existing = await ExistingByKeyAsync(
            _db.LoadTransfers.Where(t => t.LoadTransferId != null).OrderBy(t => t.Id), t => t.LoadTransferId, cancellationToken);

        var created = 0;
        var updated = 0;
        var errors = new List<string>();

        foreach (var row in rows)
        {
            try
            {
                var isNew = !existing.TryGetValue(row.Yukid, out var transfer);
                transfer ??= new LoadTransfer { LoadTransferId = row.Yukid, CreatedAt = DateTime.Now };

                // ANAHTARI HER TURDA TAZELE. Eskiden yalnızca eklemede yazılıyordu:
                // ExistingByKeyAsync büyük/küçük harf duyarsız olduğu için eski
                // kayıtlar bulunup güncelleniyor ama anahtarları ESKİ harf düzeninde
                // kalıyordu. Sonuç: C# tarafı eşleşiyor, PostgreSQL join'leri
                // (harfe duyarlı) eşleşmiyordu — koliler yüke bağlanamıyor,
                // Sefer ekranında kap/kilo boş görünüyordu (8021 kolinin yalnızca
                // 39'u eşleşiyordu). Artık iki taraf da küçük harf.
                transfer.LoadTransferId = row.Yukid;
                transfer.SiberCompanyId = row.Sirketid ?? transfer.SiberCompanyId;

                transfer.SiberDeletedAt = null;

                ApplySiberAudit(row.InsUser, row.InsTime, row.UpdUser, row.UpdTime, userCodes,
                    (code, id, at) => { transfer.SiberCreatedBy = code; transfer.SiberCreatedByUserId = id; transfer.SiberCreatedAt = at; },
                    (code, id, at) => { transfer.SiberUpdatedBy = code; transfer.SiberUpdatedByUserId = id; transfer.SiberUpdatedAt = at; });

                transfer.LoadNumber = row.Yukno?.ToString();
                // olsold ETL: 'connected_load_number' => $sqlLoad->bagliyukno (yukno DEĞİL —
                // burada eskiden yukno yazılıyordu, "bağlı yük" her zaman yükün kendisini
                // gösteriyordu).
                transfer.ConnectedLoadNumber = row.Bagliyukno?.ToString() ?? transfer.ConnectedLoadNumber;
                transfer.LoadNumberWorkType = row.Yuknoisturu;
                transfer.ConnectedLoadNumberWorkType = row.Bagliyuknoisturu;
                transfer.WorkType = row.Isturu is { } wt && workTypes.TryGetValue(wt, out var wtId) ? (int)wtId : transfer.WorkType;
                transfer.LoadStatusId = row.Durumid is { } ds && loadStatusTypes.TryGetValue(ds, out var dsId) ? (int)dsId : transfer.LoadStatusId;
                transfer.LoadTypeId = row.Yuklemetip is { } lt && loadingTypes.TryGetValue(lt, out var ltId) ? (int)ltId : transfer.LoadTypeId;
                transfer.CustomerId = row.Firmaid is { } cu && accounts.TryGetValue(cu, out var cuId) ? (int)cuId : transfer.CustomerId;
                transfer.SenderId = row.Gondericiid is { } se && accounts.TryGetValue(se, out var seId) ? (int)seId : transfer.SenderId;
                transfer.ReceiverId = row.Aliciid is { } al && accounts.TryGetValue(al, out var alId) ? (int)alId : transfer.ReceiverId;
                transfer.PaymentTypeId = row.Odemesekliid is { } pt && paymentTypes.TryGetValue(pt, out var ptId) ? (int)ptId : transfer.PaymentTypeId;
                transfer.InstructionId = row.Talimatgelissekli is { } ins && instructions.TryGetValue(ins, out var insId) ? (int)insId : transfer.InstructionId;
                transfer.RomorkTypeId = row.Istenenromorkcins is { } rom && romorkTypes.TryGetValue(rom, out var romId) ? (int)romId : transfer.RomorkTypeId;
                transfer.TotalGrossWeight = row.Toplamagirlik ?? transfer.TotalGrossWeight;
                transfer.TotalVolume = row.Toplamhacim ?? transfer.TotalVolume;
                transfer.TotalLademeter = row.Toplamlademetre ?? transfer.TotalLademeter;
                transfer.WeightFee = row.Ucretagirlik ?? transfer.WeightFee;
                transfer.DepartmentId = row.Departmanid is { } dep && departments.TryGetValue(dep, out var depId) ? (int)depId : transfer.DepartmentId;
                transfer.OperationDepartmentId = row.Operasyondepartmanid is { } odep && departments.TryGetValue(odep, out var odepId) ? (int)odepId : transfer.OperationDepartmentId;
                transfer.TotalCap = row.Toplamkap ?? transfer.TotalCap;
                transfer.LoadTransferTypeId = row.Yukturkod is { } ltt && loadTransferTypes.TryGetValue(ltt, out var lttId) ? (int)lttId : transfer.LoadTransferTypeId;
                // DepartureCountryId/TargetCountryId burada string (Load'daki gibi Guid DEĞİL) — olduğu gibi
                // saklanır. BULUNAN GERÇEK DURUM: gerçek Siber'de bu sütun (_yuklemeulke/_bosaltmaulke)
                // çoğunlukla bir GUID DEĞİL, düz ülke ADI (olsold LoadTransferController.php'nin update()
                // akışı böyle yazıyordu — bkz. LoadTransferService.CountryRefAsync'in GUID+ad çözümü).
                transfer.DepartureCountryId = row.Yuklemeulke ?? transfer.DepartureCountryId;
                transfer.TargetCountryId = row.Bosaltmaulke ?? transfer.TargetCountryId;
                transfer.WayOfWorking = row.Calismasekli ?? transfer.WayOfWorking;

                // BULUNAN GERÇEK BOŞLUK (bkz. sınıf/metot açıklaması): aşağıdaki alanlar
                // sürekli senkronda hiç okunmuyordu — bu yüzden Siber'de DOLU olmalarına
                // rağmen sürekli senkronla gelen her yeni Yük'te formda "Seçiniz"/boş
                // görünüyorlardı. Eşlemeler olsold ETL'iyle birebir (TransferDataController.php).
                transfer.CustomerRepresentativeName = row.Musteritemsilcisiad is { } mt
                    && userBySiberName.TryGetValue(QueryableExtensions.NormalizeTurkish(mt), out var mtId)
                    ? (int)mtId : transfer.CustomerRepresentativeName;
                transfer.SecondCustomerRepresentativeName = row.Musteritemsilcisi2ad is { } mt2
                    && userBySiberName.TryGetValue(QueryableExtensions.NormalizeTurkish(mt2), out var mt2Id)
                    ? (int)mt2Id : transfer.SecondCustomerRepresentativeName;
                transfer.UsercodeWithNotification = row.Bildirimyapankullanicikod is { } bk && userBySiberCode.TryGetValue(bk, out var bkId)
                    ? (int)bkId : transfer.UsercodeWithNotification;
                transfer.SalesRepCode = row.Satistemsilcisikod is { } sk && userBySiberCode.TryGetValue(sk, out var skId)
                    ? (int)skId : transfer.SalesRepCode;
                transfer.DeliveryMethodId = row.Teslimsekil is { } ts && deliveryMethodByEdikod.TryGetValue(ts, out var tsId)
                    ? (int)tsId : transfer.DeliveryMethodId;
                transfer.InTruck = row.Kamyonda ?? transfer.InTruck;
                transfer.InTail = row.Kuyrukta ?? transfer.InTail;
                transfer.CmrWaiting = row.Cmrduzenlenecek ?? transfer.CmrWaiting;
                transfer.FcrWaiting = row.Fcrduzenlenecek ?? transfer.FcrWaiting;
                transfer.TotalLademeterM3 = row.Toplamlademetrem3 ?? transfer.TotalLademeterM3;
                transfer.LoadingContinent = row.Yuklemekita ?? transfer.LoadingContinent;
                transfer.UnloadingContinent = row.Bosaltmakita ?? transfer.UnloadingContinent;
                transfer.FrontTransportationByUs = row.Ontasimatarafimizdanyapilir ?? transfer.FrontTransportationByUs;
                transfer.FinalTransportationByUs = row.Sontasimatarafimizdanyapilir ?? transfer.FinalTransportationByUs;
                transfer.InstructionArrivalDate = row.Talimatgelistarihi is { } tgt ? DateOnly.FromDateTime(tgt) : transfer.InstructionArrivalDate;
                transfer.RequestArrivalDate = row.Istenenvaristarihi is { } ivt ? DateOnly.FromDateTime(ivt) : transfer.RequestArrivalDate;
                transfer.ReadinessDate = row.Hazirolmatarih is { } hot ? DateOnly.FromDateTime(hot) : transfer.ReadinessDate;
                transfer.DateOfReceiptCustomer = row.Musteridenalinistarih is { } mat ? DateOnly.FromDateTime(mat) : transfer.DateOfReceiptCustomer;

                transfer.UpdatedAt = DateTime.Now;

                if (isNew)
                {
                    _db.LoadTransfers.Add(transfer);
                    created++;
                }
                else
                {
                    updated++;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{row.Yukid}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        var deletionNote = await MarkMissingAsDeletedAsync(
            "Yük",
            "skn_yuk",
            _db.LoadTransfers.Where(t => t.LoadTransferId != null),
            t => t.LoadTransferId,
            t => t.LoadNumberWorkType,
            (t, at) => t.SiberDeletedAt = at,
            t => t.SiberDeletedAt,
            (t, code, id, at) => { t.SiberDeletedBy = code; t.SiberDeletedByUserId = id; t.SiberDeletedOn = at; },
            rows.Select(r => r.Yukid).ToList(),
            cancellationToken);

        return new SiberImportSummary(created, updated, errors)
        {
            Notes = deletionNote is null ? [] : [deletionNote],
        };
    }

    private sealed class LoadTransferPackageRow
    {
        public string Yukkoliid { get; set; } = string.Empty;
        public string Yukid { get; set; } = string.Empty;
        public int? Kapadet { get; set; }
        public string? Kapid { get; set; }
        public decimal? En { get; set; }
        public decimal? Boy { get; set; }
        public decimal? Yukseklik { get; set; }
        public decimal? Hacim { get; set; }
        public decimal? Burutagirlik { get; set; }
        public decimal? Netagirlik { get; set; }
        public decimal? Lademetre { get; set; }
        public int? Istiflenemez { get; set; }
        public string? Malcinsid { get; set; }
    }

    /// <summary>
    /// skn_yukkoli — DİKKAT: bu tablodaki en/boy/yükseklik eşlemesi
    /// skn_rezervasyonyukkoli'den FARKLI (bkz. WritePackagesAsync): en→width (doğrudan),
    /// boy→length, yükseklik→height. kapid, <see cref="SyncOfferContentsAsync"/>'teki
    /// gibi yerel CaseTypes.Id'ye ÇÖZÜLEREK metin olarak yazılır (LoadTransferPackage.
    /// CaseTypeId zaten string) — BULUNAN GERÇEK BUG: bu metot önceden Siber GUID'ini
    /// hiç çözmeden doğrudan yazıyordu, bu yüzden paketlerin %88'inde Kap Tipi hiç
    /// görünmüyordu (int.TryParse bir GUID'i asla sayıya çeviremez). Düzeltildi.
    /// </summary>
    public async Task<SiberImportSummary> SyncLoadTransferPackagesAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await OpenAsync(cancellationToken);

        var rows = (await connection.QueryAsync<LoadTransferPackageRow>(
            new CommandDefinition(
                """
                SELECT CAST(yukkoliid AS VARCHAR(64)) AS Yukkoliid, LOWER(CAST(yukid AS VARCHAR(64))) AS Yukid,
                       TRY_CAST(kapadet AS INT) AS Kapadet, CAST(kapid AS VARCHAR(64)) AS Kapid,
                       TRY_CAST(en AS DECIMAL(18,4)) AS En, TRY_CAST(boy AS DECIMAL(18,4)) AS Boy,
                       TRY_CAST(yukseklik AS DECIMAL(18,4)) AS Yukseklik, TRY_CAST(hacim AS DECIMAL(18,4)) AS Hacim,
                       TRY_CAST(burutagirlik AS DECIMAL(18,4)) AS Burutagirlik,
                       TRY_CAST(netagirlik AS DECIMAL(18,4)) AS Netagirlik,
                       TRY_CAST(lademetre AS DECIMAL(18,4)) AS Lademetre, TRY_CAST(istiflenemez AS INT) AS Istiflenemez,
                       CAST(malcinsid AS VARCHAR(64)) AS Malcinsid
                FROM skn_yukkoli
                """,
                cancellationToken: cancellationToken))).ToList();

        var loadTransferIds = new HashSet<string>(
            (await _db.LoadTransfers.AsNoTracking().Where(t => t.LoadTransferId != null)
                .Select(t => t.LoadTransferId!).ToListAsync(cancellationToken)),
            StringComparer.OrdinalIgnoreCase);
        var productTypes = BySiberId(await _db.ProductTypes.AsNoTracking().ToListAsync(cancellationToken), p => p.SiberId, p => p.Id);
        var caseTypes = BySiberId(await _db.CaseTypes.AsNoTracking().ToListAsync(cancellationToken), c => c.SiberId, c => c.Id);
        var existing = await ExistingByKeyAsync(
            _db.LoadTransferPackages.Where(p => p.Yukkoliid != null).OrderBy(p => p.Id), p => p.Yukkoliid, cancellationToken);

        var created = 0;
        var updated = 0;
        var skipped = 0;
        var errors = new List<string>();

        foreach (var row in rows)
        {
            try
            {
                if (!loadTransferIds.Contains(row.Yukid))
                {
                    skipped++;
                    continue;
                }

                var isNew = !existing.TryGetValue(row.Yukkoliid, out var package);
                package ??= new LoadTransferPackage { Yukkoliid = row.Yukkoliid, LoadTransferId = row.Yukid, CreatedAt = DateTime.Now };

                package.LoadTransferId = row.Yukid;
                package.Quantity = row.Kapadet;
                // Kapid Siber'in kap-cinsi GUID'i — LoadTransferPackage.CaseTypeId ise
                // YEREL case_types.id'sini METİN olarak tutuyor (bkz. LoadTransferService
                // XML açıklaması). Ham Siber GUID'ini olduğu gibi yazmak (eski hâli) DTO'daki
                // int.TryParse eşlemesini sessizce başarısız kılıyordu — Kap Tipi hemen hemen
                // hiçbir paket için çözülemiyordu.
                package.CaseTypeId = row.Kapid is { } kt && caseTypes.TryGetValue(kt, out var ctId)
                    ? ctId.ToString()
                    : package.CaseTypeId;
                package.Width = row.En;
                package.Length = row.Boy;
                package.Height = row.Yukseklik;
                package.Volume = row.Hacim;
                package.GrossWeight = row.Burutagirlik;
                package.NetWeight = row.Netagirlik;
                package.Lademeter = row.Lademetre;
                package.Stackable = row.Istiflenemez;
                package.ProductTypeId = row.Malcinsid is { } mc && productTypes.TryGetValue(mc, out var mcId) ? (int)mcId : package.ProductTypeId;
                package.UpdatedAt = DateTime.Now;

                if (isNew)
                {
                    _db.LoadTransferPackages.Add(package);
                    created++;
                }
                else
                {
                    updated++;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{row.Yukkoliid}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new SiberImportSummary(created, updated, errors) with
        {
            Errors = skipped > 0 ? [.. errors, $"{skipped} satır atlandı (yerel yük bulunamadı)."] : errors,
        };
    }

    private sealed class YukEvrakRow
    {
        public string Evrakid { get; set; } = string.Empty;
        public string? Yukid { get; set; }
        public int? Sirano { get; set; }
        public string? Evrakno { get; set; }
        public DateTime? Tarih { get; set; }
        public int? Orjinaladet { get; set; }
        public int? Kopyaadet { get; set; }
        public string? Teslimalan { get; set; }
        public DateTime? Teslimtarih { get; set; }
        public string? Aciklama { get; set; }
    }

    /// <summary>
    /// skn_yukevrak — Evrak Takibi'nin okuma yönü (yazma: LoadTransferDocumentService).
    ///
    /// DİKKAT — evrakid GÜVENİLİR BİR ANAHTAR DEĞİL: gerçek Siber'de doğrulandı,
    /// 31.520 satırın yalnızca 20 FARKLI evrakid değeri var (her sirano için 2 sabit
    /// "şablon" GUID, binlerce satırda tekrarlanıyor — Siber'in eski uygulamasının bir
    /// veri girişi/aktarım kalıntısı, muhtemelen gerçek amaç değil). Asıl doğal anahtar
    /// (yukid, sirano) çifti: 31.520 satırın 31.484'ü bu çiftte benzersiz (%99.9) —
    /// yani gerçek iş modeli "yük başına, evrak türü başına bir çeklist satırı".
    /// Bu yüzden yerel eşleme evrakid ÜZERİNDEN DEĞİL, çözümlenmiş (LoadTransferId,
    /// EvrakTuruId) çifti üzerinden yapılıyor; Siber'in evrakid'i yalnızca bilgi
    /// amaçlı saklanıyor (Yukevrakid).
    ///
    /// Diğer Siber-yazan modüllerin aksine burada VERİ KAYBI riski var: Siber
    /// tarafında elle silinen bir evrak satırı, yerelde de silinmeli — bu yüzden
    /// (Paketler'in aksine) burada tam bir "Siber'de yok → yerelde de sil" adımı da
    /// var, aksi halde yerel taraf Siber'den bağımsızca büyüyüp gerçek çekilistiyle
    /// uyuşmaz hale gelirdi.
    /// </summary>
    public async Task<SiberImportSummary> SyncLoadTransferDocumentsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await OpenAsync(cancellationToken);

        var rows = (await connection.QueryAsync<YukEvrakRow>(
            new CommandDefinition(
                """
                SELECT CAST(evrakid AS VARCHAR(64)) AS Evrakid, LOWER(CAST(yukid AS VARCHAR(64))) AS Yukid,
                       TRY_CAST(sirano AS INT) AS Sirano, evrakno AS Evrakno, tarih AS Tarih,
                       TRY_CAST(orjinaladet AS INT) AS Orjinaladet, TRY_CAST(kopyaadet AS INT) AS Kopyaadet,
                       teslimalan AS Teslimalan, teslimtarih AS Teslimtarih, aciklama AS Aciklama
                FROM skn_yukevrak
                """,
                cancellationToken: cancellationToken))).ToList();

        // BySiberId (ToDictionaryAsync DEĞİL): gerçek Siber'de aynı yukid'yi paylaşan
        // birden fazla yerel LoadTransfer satırı bulunabiliyor (doğrulandı) —
        // ToDictionaryAsync bunda ArgumentException ile patlardı, BySiberId ise
        // (diğer senkronlarda olduğu gibi) sessizce ilk eşleşeni tutar. OrderBy(Id)
        // ŞART: sıralama olmadan "ilk eşleşen" turdan tura DEĞİŞEBİLİYOR (canlıda
        // doğrulandı) — bu da aynı belge için turdan tura farklı LoadTransferId'ye
        // bağlanıp eskisini "silinmiş" gösteren, sürekli çoğalan hayalet kayıtlara
        // yol açıyordu. Id ASC ile seçim artık her turda aynı (en düşük id kazanır).
        var transferIdBySiberYukId = BySiberId(
            await _db.LoadTransfers.AsNoTracking().Where(t => t.LoadTransferId != null)
                .OrderBy(t => t.Id).ToListAsync(cancellationToken),
            t => t.LoadTransferId, t => t.Id);

        var evrakTuruByCode = BySiberId(
            await _db.EvrakTurus.AsNoTracking().ToListAsync(cancellationToken), e => e.Code, e => e.Id);

        var existing = new Dictionary<(long LoadTransferId, long EvrakTuruId), LoadTransferDocument>();
        foreach (var d in await _db.LoadTransferDocuments.Where(d => d.EvrakTuruId != null).ToListAsync(cancellationToken))
            existing.TryAdd((d.LoadTransferId, d.EvrakTuruId!.Value), d); // aynı çiftten birden fazlaysa ilki tutulur, gerisi aşağıda "seen" dışı kalıp silinir

        var created = 0;
        var updated = 0;
        var skipped = 0;
        var untyped = 0;
        var errors = new List<string>();
        var seen = new HashSet<(long, long)>();

        foreach (var row in rows)
        {
            try
            {
                // İKİ AYRI SEBEP, İKİ AYRI SAYAÇ. Eskiden tek sayaçta toplanıp
                // "40 satır atlandı (yerel yük veya evrak türü bulunamadı)" diye
                // HATA olarak loglanıyordu — hangi sebep olduğu anlaşılmıyordu ve
                // her turda hata gibi görünüyordu.
                //
                // Canlıda incelendi: 40 satırın TAMAMI ikinci sebep, yani Siber'in
                // kendisinde sirano NULL. Bu satırlarda evrak no, tarih ve açıklama
                // da boş (yalnızca orijinal/kopya adedi dolu) — Siber tarafında
                // türü belirlenmemiş boş yer tutucular. Bunları atlamak DOĞRU
                // davranış; hata değil, kaynak verisinin durumu.
                //
                // Birinci sebep (yerel yük bulunamadı) ise GERÇEK bir sorundur —
                // ayrı sayılıp hata olarak raporlanmaya devam eder.
                if (row.Yukid is null || !transferIdBySiberYukId.TryGetValue(row.Yukid, out var loadTransferId))
                {
                    skipped++;
                    continue;
                }

                if (row.Sirano is not { } sirano || !evrakTuruByCode.TryGetValue(sirano.ToString(), out var evrakTuruId))
                {
                    untyped++;
                    continue;
                }

                var key = (loadTransferId, evrakTuruId);
                seen.Add(key);

                var isNew = !existing.TryGetValue(key, out var document);
                document ??= new LoadTransferDocument
                {
                    LoadTransferId = loadTransferId, EvrakTuruId = evrakTuruId, CreatedAt = DateTime.Now,
                };

                document.Yukevrakid = row.Evrakid;
                document.DocumentNumber = row.Evrakno;
                document.Date = row.Tarih is { } t ? DateOnly.FromDateTime(t) : null;
                document.OriginalCount = row.Orjinaladet;
                document.CopyCount = row.Kopyaadet;
                document.DeliveredTo = row.Teslimalan;
                document.DeliveredAt = row.Teslimtarih is { } dt ? DateOnly.FromDateTime(dt) : null;
                document.Note = row.Aciklama;
                document.UpdatedAt = DateTime.Now;

                if (isNew)
                {
                    _db.LoadTransferDocuments.Add(document);
                    existing[key] = document;
                    created++;
                }
                else
                {
                    updated++;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{row.Evrakid}: {ex.Message}");
            }
        }

        // Güvenlik freni: kaynak satır sayısı (rows) yereldeki mevcut satır sayısının
        // ÇOK altındaysa (ör. Siber bağlantısı yanlışlıkla farklı/boş bir veritabanına
        // işaret ediyorsa, ya da geçici bir bağlantı sorunu hata fırlatmak yerine boş
        // sonuç dönüyorsa) toplu silme adımı ATLANIR — canlıda gerçekten doğrulandı:
        // bağlantı yanlışlıkla sahte Siber'e yönlendiğinde bu adım TÜM yerel evrak
        // kayıtlarını (o an 31.480 satır) silmeye kalkışıyordu. %90 eşiği keyfi ama
        // kasıtlı: gerçek Siber'de satır sayısı zamanla yalnızca artar/yatay seyreder,
        // ani bir çöküş neredeyse her zaman yanlış hedef/bağlantı anlamına gelir.
        var deleted = new List<LoadTransferDocument>();
        if (rows.Count >= existing.Count * 0.9)
        {
            deleted = existing.Where(kv => !seen.Contains(kv.Key)).Select(kv => kv.Value).ToList();
            if (deleted.Count > 0)
                _db.LoadTransferDocuments.RemoveRange(deleted);
        }
        else if (existing.Count > 0)
        {
            errors.Add(
                $"Güvenlik freni: kaynakta {rows.Count} satır bulundu ama yerelde {existing.Count} kayıt var " +
                "— toplu silme adımı atlandı (yanlış/boş bağlantı olabilir).");
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new SiberImportSummary(created, updated, errors) with
        {
            Errors = skipped > 0
                ? [.. errors, $"{skipped} satır atlandı (yerel yük bulunamadı)."]
                : errors,
            // Türü olmayan satırlar HATA DEĞİL — Siber'de sirano boş, atlanmaları
            // doğru. Sayı yalnızca bilgi amaçlı notta taşınır.
            Notes = untyped > 0
                ? [$"{untyped} satırda Siber tarafında evrak türü (sirano) boş — atlandı."]
                : [],
        };
    }

    private sealed class InvoiceItemRow
    {
        public string Modulkalemid { get; set; } = string.Empty;
        public string? Modulid { get; set; }
        public string? Modulkod { get; set; }
        public string? Kalemid { get; set; }
        public string? Gc { get; set; }
        public string? Firmaid { get; set; }
        public decimal? Toplamtutar { get; set; }
        public string? Dovizkod { get; set; }
        public decimal? Birimfiyat { get; set; }
        public decimal? Miktar { get; set; }
        public string? YukNumarasi { get; set; }
    }

    /// <summary>
    /// sfy_modulkalem — WriteInvoiceItemsAsync ile aynı tablo. Burada (skn_rezervasyontarife'nin
    /// aksine) alış/satış tek sütun grubuyla, <c>gc</c> ('C'=alış/1, 'G'=satış/2) ile ayrılıyor —
    /// yön belirsizliği yok.
    ///
    /// İlgili yüke bağlantı DOLAYLI: sfy_modulkalem'de yükün kendi kimliği (yukid) yok,
    /// yalnızca modulid var. FindModulKayitAsync'in ("SELECT modulid FROM sfy_modulkayit
    /// WHERE ad = yük_numarası") tersini yaparak sfy_modulkayit'e JOIN edilip yük numarası
    /// (ad) geri okunur, sonra bu numara LoadTransfer.LoadNumberWorkType ile eşleştirilir.
    /// </summary>
    public async Task<SiberImportSummary> SyncLoadTransferInvoiceItemsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await OpenAsync(cancellationToken);

        var rows = (await connection.QueryAsync<InvoiceItemRow>(
            new CommandDefinition(
                """
                SELECT CAST(mk.modulkalemid AS VARCHAR(64)) AS Modulkalemid, CAST(mk.modulid AS VARCHAR(64)) AS Modulid,
                       LTRIM(RTRIM(mk.modulkod)) AS Modulkod, CAST(mk.kalemid AS VARCHAR(64)) AS Kalemid,
                       LTRIM(RTRIM(mk.gc)) AS Gc, CAST(mk.firmaid AS VARCHAR(64)) AS Firmaid,
                       TRY_CAST(mk.tutar AS DECIMAL(18,4)) AS Toplamtutar, LTRIM(RTRIM(mk.dovizkod)) AS Dovizkod,
                       TRY_CAST(mk.birimfiyat AS DECIMAL(18,4)) AS Birimfiyat,
                       TRY_CAST(mk.miktar AS DECIMAL(18,4)) AS Miktar, LTRIM(RTRIM(mky.ad)) AS YukNumarasi
                FROM sfy_modulkalem mk
                JOIN sfy_modulkayit mky ON mky.modulid = mk.modulid
                -- sfy_modulkayit.yer, mali kalemin HANGİ nesneye bağlı olduğunu söylüyor
                -- (YUK/SEFER/FIRMA/ARAC/PERSONEL/rzv — gerçek Siber'de doğrulandı). Bu
                -- filtre olmadan Sefer'e bağlı kalemler de çekiliyor ve "ad" bir Sefer
                -- numarası olduğu için hiçbir zaman bir Yük'e eşleşmiyordu — canlı Siber'de
                -- 7271 satırlık gereksiz "yerel yük bulunamadı" uyarısının kaynağı buydu.
                -- Sefer'in KENDİ mali kalemleri bu portun kapsamında değil (henüz).
                WHERE LTRIM(RTRIM(mky.yer)) = 'YUK'
                """,
                cancellationToken: cancellationToken))).ToList();

        var financialItems = BySiberId(await _db.FinancialItems.AsNoTracking().ToListAsync(cancellationToken), f => f.SiberId, f => f.Id);
        var accounts = BySiberId(await _db.Accounts.AsNoTracking().ToListAsync(cancellationToken), a => a.SiberId, a => a.Id);
        var currencies = ByCode(await _db.Currencies.AsNoTracking().ToListAsync(cancellationToken), c => c.Code, c => c.Id);
        var knownLoadNumbers = new HashSet<string>(
            (await _db.LoadTransfers.AsNoTracking().Where(t => t.LoadNumberWorkType != null)
                .Select(t => t.LoadNumberWorkType!).ToListAsync(cancellationToken)),
            StringComparer.OrdinalIgnoreCase);
        var existing = await ExistingByKeyAsync(
            _db.LoadTransferInvoiceItems.Where(i => i.Modulkalemid != null).OrderBy(i => i.Id), i => i.Modulkalemid, cancellationToken);

        var created = 0;
        var updated = 0;
        var skipped = 0;
        var errors = new List<string>();

        foreach (var row in rows)
        {
            try
            {
                if (row.YukNumarasi is null || !knownLoadNumbers.Contains(row.YukNumarasi))
                {
                    skipped++;
                    continue;
                }

                var isNew = !existing.TryGetValue(row.Modulkalemid, out var item);
                item ??= new LoadTransferInvoiceItem
                {
                    Modulkalemid = row.Modulkalemid, Status = "pending", CreatedAt = DateTime.Now,
                };

                item.Modulid = row.Modulid;
                item.Modulkod = row.Modulkod;
                item.ItemId = row.Kalemid is { } ki && financialItems.TryGetValue(ki, out var kiId) ? (int)kiId : item.ItemId;
                item.Buysell = row.Gc?.Trim().Equals("G", StringComparison.OrdinalIgnoreCase) == true ? "2" : "1";
                item.AccountId = row.Firmaid is { } af && accounts.TryGetValue(af, out var afId) ? (int)afId : item.AccountId;
                item.TotalPrice = row.Toplamtutar ?? item.TotalPrice;
                item.CurrencyCode = row.Dovizkod is { } cur && currencies.TryGetValue(cur, out var curId) ? (int)curId : item.CurrencyCode;
                item.NetPrice = row.Birimfiyat ?? item.NetPrice;
                item.Quantity = row.Miktar ?? item.Quantity;
                item.InsertName = row.YukNumarasi;
                item.UpdatedAt = DateTime.Now;

                if (isNew)
                {
                    _db.LoadTransferInvoiceItems.Add(item);
                    created++;
                }
                else
                {
                    updated++;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{row.Modulkalemid}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new SiberImportSummary(created, updated, errors) with
        {
            Errors = skipped > 0 ? [.. errors, $"{skipped} satır atlandı (yerel yük bulunamadı)."] : errors,
        };
    }

    private sealed class ExpeditionRow
    {
        public string Pozisyonid { get; set; } = string.Empty;
        public string? Seferno { get; set; }
        public string? Seferid { get; set; }
        public string? Isturu { get; set; }
        public int? Durumid { get; set; }
        public string? Hafta { get; set; }
        public string? Departmanid { get; set; }
        public DateTime? Kayitgiristarih { get; set; }
        public string? Seferturid { get; set; }
        public DateTime? Araccikistarih { get; set; }
        public DateTime? Cikistarih { get; set; }
        public DateTime? Donustarih { get; set; }
        public string? Baslangicsehirid { get; set; }
        public string? Yuklemesehirid { get; set; }
        public string? Bitissehirid { get; set; }

        // BULUNAN GERÇEK BOŞLUK: bu iki alan sürekli senkronda okunmuyordu — canlıda
        // doğrulandı, sürekli senkronla gelen seferlerin %42'sinde Römork, %100'ünde
        // Yükleme Tarihi boştu (eski aktarımdakilerde doluydu).
        public string? Romorkplakano { get; set; }
        public DateTime? Yuklemetarih { get; set; }

        /// <summary>
        /// Römorkun GERÇEK anahtarı. Araç bağı eskiden yalnızca plaka METNİYLE
        /// kuruluyordu; canlıda 21 plaka birden fazla skn_arac kaydında tekrarlıyor
        /// ve bu 188 seferi etkiliyordu — hangi araç kaydına bağlanacağı rastgeleydi
        /// (sözlükte ilk gelen kazanıyordu), dolayısıyla aracın tipi/sahibi/durumu
        /// yanlış görünebiliyordu. Artık önce bu kimlik denenir.
        /// </summary>
        public string? Romorkid { get; set; }

        /// <summary>Seferin ait olduğu Siber şirketi — görünürlük ayrımı.</summary>
        public string? Sirketid { get; set; }
    
        // Siber denetim izleri — kim açtı / kim son dokundu.
        public string? InsUser { get; set; }
        public DateTime? InsTime { get; set; }
        public string? UpdUser { get; set; }
        public DateTime? UpdTime { get; set; }
}

    /// <summary>
    /// skn_pozisyon + skn_sefer (LEFT JOIN seferid) — bu oturumun önceki ETL çalışmasında
    /// (Sefer/Rezervasyon aktarımı düzeltmeleri) doğrulanmış sorgu. Şehir/ülke GUID'leri
    /// projede doğrudan Siber GUID'iyle aynı tutulduğundan (bkz. proje kuralı) ayrıca
    /// eşleme gerekmez, sadece parse edilir.
    /// </summary>
    public async Task<SiberImportSummary> SyncExpeditionsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await OpenAsync(cancellationToken);

        var rows = (await connection.QueryAsync<ExpeditionRow>(
            new CommandDefinition(
                """
                SELECT CAST(p.pozisyonid AS VARCHAR(64)) AS Pozisyonid, LTRIM(RTRIM(ISNULL(p.seferno,''))) AS Seferno,
                       CAST(p.seferid AS VARCHAR(64)) AS Seferid, LTRIM(RTRIM(p.isturu)) AS Isturu,
                       TRY_CAST(p.durumid AS INT) AS Durumid, LTRIM(RTRIM(ISNULL(p.hafta,''))) AS Hafta,
                       CAST(p.departmanid AS VARCHAR(64)) AS Departmanid, p.kayitgiristarih AS Kayitgiristarih,
                       CAST(p.seferturid AS VARCHAR(64)) AS Seferturid, p.araccikistarih AS Araccikistarih,
                       s.cikistarih AS Cikistarih, s.donustarih AS Donustarih,
                       CAST(p.baslangicsehirid AS VARCHAR(64)) AS Baslangicsehirid,
                       CAST(p.yuklemesehirid AS VARCHAR(64)) AS Yuklemesehirid,
                       CAST(p.bitissehirid AS VARCHAR(64)) AS Bitissehirid,
                       LTRIM(RTRIM(p.romorkplakano)) AS Romorkplakano,
                       CAST(p.romorkid AS VARCHAR(64)) AS Romorkid,
                       CAST(p.sirketid AS VARCHAR(64)) AS Sirketid,
                       p.yuklemetarih AS Yuklemetarih,
                       LTRIM(RTRIM(p.kayitgiren)) AS InsUser, p.kayitgiristarih AS InsTime,
                       LTRIM(RTRIM(p.upduser)) AS UpdUser, p.updtime AS UpdTime
                FROM skn_pozisyon p
                LEFT JOIN skn_sefer s ON s.seferid = p.seferid
                """,
                cancellationToken: cancellationToken))).ToList();

        var workTypes = ByCode(await _db.WorkTypes.AsNoTracking().ToListAsync(cancellationToken), w => w.Code, w => w.Id);
        var userCodes = await SiberUserCodeMapAsync(cancellationToken);
        var departments = BySiberId(await _db.Departments.AsNoTracking().ToListAsync(cancellationToken), d => d.SiberId, d => d.Id);
        // DİKKAT: skn_pozisyon.seferturid gerçek Siber'de tinyint (10, 11, ...) — GUID
        // DEĞİL. ExpeditionType.SiberId (GUID) değil Code (sayısal kod) ile eşleşir.
        var expeditionTypes = ByCode(await _db.ExpeditionTypes.AsNoTracking().ToListAsync(cancellationToken), t => t.Code, t => t.Id);
        var expeditionStatuses = new Dictionary<int, long>();
        foreach (var s in await _db.ExpeditionStatuses.AsNoTracking().ToListAsync(cancellationToken))
            if (s.ExpeditionStatusId is { } code && !expeditionStatuses.ContainsKey(code)) expeditionStatuses[code] = s.Id;

        // ARAÇ BAĞI: önce Siber kimliği (romorkid), sonra plaka.
        //
        // Eskiden yalnızca plaka metni kullanılıyordu. skn_pozisyon'da romorkid
        // GERÇEKTEN var ve 4365 satırın TAMAMINDA dolu — üstelik her zaman geçerli
        // bir skn_arac kaydına çözülüyor. Plakayla eşleştirmenin sorunu şu: canlıda
        // 21 plaka birden fazla araç kaydında geçiyor ve 188 sefer bu plakalara ait;
        // sözlük bunlardan yalnızca birini tutabildiği için sefer yanlış araç kaydına
        // bağlanabiliyordu. Plaka eşlemesi yalnızca YEDEK olarak kalıyor (kimlik
        // yerelde bulunamazsa).
        var cars = await _db.Cars.AsNoTracking().OrderBy(c => c.Id).ToListAsync(cancellationToken);

        var carBySiberId = cars
            .Where(c => !string.IsNullOrWhiteSpace(c.SiberId))
            .GroupBy(c => c.SiberId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        var carByPlate = ByCode(cars, c => c.PlateNumber, c => c.Id);

        var existing = await ExistingByKeyAsync(
            _db.Expeditions.Where(e => e.ExpeditionId != null).OrderBy(e => e.Id), e => e.ExpeditionId, cancellationToken);

        var created = 0;
        var updated = 0;
        var errors = new List<string>();

        foreach (var row in rows)
        {
            try
            {
                var isNew = !existing.TryGetValue(row.Pozisyonid, out var expedition);
                expedition ??= new Expedition { ExpeditionId = row.Pozisyonid, CreatedAt = DateTime.Now };

                expedition.SiberCompanyId = row.Sirketid ?? expedition.SiberCompanyId;

                expedition.SiberDeletedAt = null;

                ApplySiberAudit(row.InsUser, row.InsTime, row.UpdUser, row.UpdTime, userCodes,
                    (code, id, at) => { expedition.SiberCreatedBy = code; expedition.SiberCreatedByUserId = id; expedition.SiberCreatedAt = at; },
                    (code, id, at) => { expedition.SiberUpdatedBy = code; expedition.SiberUpdatedByUserId = id; expedition.SiberUpdatedAt = at; });
                expedition.ExpeditionNumber = row.Seferno;
                expedition.SeferId = row.Seferid;
                expedition.WorkType = row.Isturu is { } wt && workTypes.TryGetValue(wt, out var wtId) ? (int)wtId : expedition.WorkType;
                expedition.StatusId = row.Durumid is { } ds && expeditionStatuses.TryGetValue(ds, out var dsId) ? (int)dsId : expedition.StatusId;
                expedition.YearWeek = row.Hafta;
                expedition.DepartmentId = row.Departmanid is { } dep && departments.TryGetValue(dep, out var depId) ? (int)depId : expedition.DepartmentId;
                expedition.RegistrationLoginDate = row.Kayitgiristarih is { } kg ? DateOnly.FromDateTime(kg) : expedition.RegistrationLoginDate;
                expedition.ExpeditionTypeId = row.Seferturid is { } st && expeditionTypes.TryGetValue(st, out var stId) ? (int)stId : expedition.ExpeditionTypeId;
                expedition.CarExitDate = row.Araccikistarih is { } ac ? DateOnly.FromDateTime(ac) : expedition.CarExitDate;
                expedition.ReleaseDate = row.Cikistarih is { } cs ? DateOnly.FromDateTime(cs) : expedition.ReleaseDate;
                expedition.ReturnDate = row.Donustarih is { } dn ? DateOnly.FromDateTime(dn) : expedition.ReturnDate;
                if (row.Baslangicsehirid is { } bs && Guid.TryParse(bs, out var bsGuid)) expedition.StartCityId = bsGuid;
                if (row.Yuklemesehirid is { } ys && Guid.TryParse(ys, out var ysGuid)) expedition.LoadCityId = ysGuid;
                if (row.Bitissehirid is { } bi && Guid.TryParse(bi, out var biGuid)) expedition.EndCityId = biGuid;
                if (row.Romorkid is { } romorkId && carBySiberId.TryGetValue(romorkId, out var byId))
                    expedition.RomorkId = (int)byId;
                else if (row.Romorkplakano is { } rp && carByPlate.TryGetValue(rp, out var rpId))
                    expedition.RomorkId = (int)rpId;
                expedition.LoadingDate = row.Yuklemetarih is { } yt ? DateOnly.FromDateTime(yt) : expedition.LoadingDate;
                expedition.UpdatedAt = DateTime.Now;

                if (isNew)
                {
                    _db.Expeditions.Add(expedition);
                    created++;
                }
                else
                {
                    updated++;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{row.Pozisyonid}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        var deletionNote = await MarkMissingAsDeletedAsync(
            "Sefer",
            "skn_pozisyon",
            _db.Expeditions.Where(e => e.ExpeditionId != null),
            e => e.ExpeditionId,
            e => e.ExpeditionNumber,
            (e, at) => e.SiberDeletedAt = at,
            e => e.SiberDeletedAt,
            (e, code, id, at) => { e.SiberDeletedBy = code; e.SiberDeletedByUserId = id; e.SiberDeletedOn = at; },
            rows.Select(r => r.Pozisyonid).ToList(),
            cancellationToken);

        return new SiberImportSummary(created, updated, errors)
        {
            Notes = deletionNote is null ? [] : [deletionNote],
        };
    }

    private sealed class YukAktarmaRow
    {
        public string Yukaktarmaid { get; set; } = string.Empty;
        public int? Yuklemebosaltma { get; set; }
        public string? Yukid { get; set; }
        public string? Pozisyonid { get; set; }
        public string? Romorkid { get; set; }
        public string? Yerid { get; set; }
        public DateTime? Tarih { get; set; }
    }

    /// <summary>
    /// skn_yukaktarma — bir Sefer'e bağlanan her Yük için tek satır. Anahtar
    /// (yukaktarmaid) canlıda doğrulandı: gerçekten benzersiz (skn_yukevrak'ın
    /// aksine burada evrakid tarzı bir "şablon id" sorunu yok).
    /// </summary>
    public async Task<SiberImportSummary> SyncExpeditionLoadMappingsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await OpenAsync(cancellationToken);

        var rows = (await connection.QueryAsync<YukAktarmaRow>(
            new CommandDefinition(
                """
                SELECT CAST(yukaktarmaid AS VARCHAR(64)) AS Yukaktarmaid,
                       TRY_CAST(yuklemebosaltma AS INT) AS Yuklemebosaltma,
                       LOWER(CAST(yukid AS VARCHAR(64))) AS Yukid,
                       CAST(pozisyonid AS VARCHAR(64)) AS Pozisyonid,
                       CAST(romorkid AS VARCHAR(64)) AS Romorkid,
                       CAST(yerid AS VARCHAR(64)) AS Yerid,
                       tarih AS Tarih
                FROM skn_yukaktarma
                """,
                cancellationToken: cancellationToken))).ToList();

        var expeditionIdByPozisyon = BySiberId(
            await _db.Expeditions.AsNoTracking().Where(e => e.ExpeditionId != null)
                .OrderBy(e => e.Id).ToListAsync(cancellationToken),
            e => e.ExpeditionId, e => e.Id);

        var loadTransferIdByYukid = BySiberId(
            await _db.LoadTransfers.AsNoTracking().Where(t => t.LoadTransferId != null)
                .OrderBy(t => t.Id).ToListAsync(cancellationToken),
            t => t.LoadTransferId, t => t.Id);

        var carIdByRomork = BySiberId(
            await _db.Cars.AsNoTracking().Where(c => c.SiberId != null).ToListAsync(cancellationToken),
            c => c.SiberId, c => c.Id);

        var existing = await ExistingByKeyAsync(
            _db.ExpeditionLoadMappings.Where(m => m.Yukaktarmaid != null).OrderBy(m => m.Id),
            m => m.Yukaktarmaid, cancellationToken);

        var created = 0;
        var updated = 0;
        var skipped = 0;
        var errors = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            try
            {
                if (row.Pozisyonid is null || !expeditionIdByPozisyon.TryGetValue(row.Pozisyonid, out var expeditionId))
                {
                    skipped++;
                    continue;
                }

                seen.Add(row.Yukaktarmaid);

                var isNew = !existing.TryGetValue(row.Yukaktarmaid, out var mapping);
                mapping ??= new ExpeditionLoadMapping { Yukaktarmaid = row.Yukaktarmaid, CreatedAt = DateTime.Now };

                mapping.ExpeditionId = expeditionId.ToString();
                mapping.UploadUnload = row.Yuklemebosaltma;
                mapping.LoadTransferId = row.Yukid is { } yukid && loadTransferIdByYukid.TryGetValue(yukid, out var ltId)
                    ? ltId.ToString() : mapping.LoadTransferId;
                mapping.RomorkId = row.Romorkid is { } rk && carIdByRomork.TryGetValue(rk, out var carId)
                    ? carId.ToString() : mapping.RomorkId;
                mapping.YerId = row.Yerid ?? mapping.YerId;
                mapping.Date = row.Tarih is { } t ? DateOnly.FromDateTime(t) : mapping.Date;
                mapping.UpdatedAt = DateTime.Now;

                if (isNew)
                {
                    _db.ExpeditionLoadMappings.Add(mapping);
                    created++;
                }
                else
                {
                    updated++;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{row.Yukaktarmaid}: {ex.Message}");
            }
        }

        // Güvenlik freni: bkz. SyncLoadTransferDocumentsAsync'deki aynı korumanın
        // gerekçesi — kaynak beklenmedik şekilde neredeyse boş dönerse toplu silme
        // adımı atlanır.
        var deleted = new List<ExpeditionLoadMapping>();
        if (rows.Count >= existing.Count * 0.9)
        {
            deleted = existing.Where(kv => !seen.Contains(kv.Key)).Select(kv => kv.Value).ToList();
            if (deleted.Count > 0)
                _db.ExpeditionLoadMappings.RemoveRange(deleted);
        }
        else if (existing.Count > 0)
        {
            errors.Add(
                $"Güvenlik freni: kaynakta {rows.Count} satır bulundu ama yerelde {existing.Count} kayıt var " +
                "— toplu silme adımı atlandı (yanlış/boş bağlantı olabilir).");
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new SiberImportSummary(created, updated, errors) with
        {
            Errors = skipped > 0 ? [.. errors, $"{skipped} satır atlandı (yerel sefer bulunamadı)."] : errors,
        };
    }

    private sealed class FirmaRow
    {
        public string Firmaid { get; set; } = string.Empty;
        public string? Ad { get; set; }
        public string? Adres1 { get; set; }
        public string? Telefon1 { get; set; }
        public string? Email { get; set; }
        public string? Vergidaire { get; set; }
        public string? Vergino { get; set; }
        public bool? Aktif { get; set; }
        public string? Muhasebekod { get; set; }
    }

    /// <summary>
    /// BULUNAN GERÇEK BOŞLUK: Müşteri (sbr_firma) yalnızca <see cref="ISiberImportService.ImportAccountsAsync"/>
    /// ile TEK SEFERLİK içeri alınmıştı — canlıda doğrulandı, gerçek Siber'de 10
    /// yeni cari vardı ki yerelde yoktu. Burada aynı mantık (isim/tip eşlemesi
    /// dahil) sürekli senkrona taşındı; <see cref="ISiberImportService.ImportAccountsAsync"/>
    /// dokunulmadan bırakıldı (zararsız — tekrar çalışsa da aynı satırları
    /// günceller, ilk kurulum betiği hâlâ ondan geçebilir).
    /// </summary>
    public async Task<SiberImportSummary> SyncAccountsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await OpenAsync(cancellationToken);

        var rows = (await connection.QueryAsync<FirmaRow>(
            new CommandDefinition(
                """
                SELECT CAST(f.firmaid AS VARCHAR(64)) AS Firmaid, f.ad AS Ad, f.adres1 AS Adres1,
                       f.telefon1 AS Telefon1, f.email AS Email, f.vergidaire AS Vergidaire,
                       f.vergino AS Vergino, TRY_CAST(f.aktif AS BIT) AS Aktif, m.muhasebekod AS Muhasebekod
                FROM sbr_firma f
                LEFT JOIN sfy_muhasebeentegrekodu m ON m.entegread = f.ad
                """,
                cancellationToken: cancellationToken))).ToList();

        var existing = await ExistingByKeyAsync(
            _db.Accounts.Where(a => a.SiberId != null).OrderBy(a => a.Id), a => a.SiberId, cancellationToken);

        var created = 0;
        var updated = 0;
        var unclassified = 0;
        var errors = new List<string>();

        foreach (var row in rows)
        {
            try
            {
                // olsold: 320* -> tedarikçi(2), 120* -> müşteri(1), yoksa hem müşteri hem gönderici/alıcı(3,4).
                //
                // BULUNAN GERÇEK HATA: kod bu üçünden hiçbirine uymuyorsa eskiden
                // TÜM SATIR atlanıyordu (continue) — yani firmanın adı, adresi,
                // telefonu, vergi bilgisi ve aktiflik durumu hiçbir senkronda
                // tazelenmiyordu. Canlıda ölçüldü: her turda 272 satır bu yüzden
                // düşüyor ve 249 firma bundan etkileniyor.
                //
                // Tip çıkarılamaması, firmanın güncellenmemesi için gerekçe DEĞİL:
                // sbr_firma'daki kayıt gerçek bir firmadır, yalnızca muhasebe kodu
                // müşteri/tedarikçi hesabı değildir (canlıda 740 gider, 329 diğer
                // ticari borç, 102 banka gibi tekdüzen hesap kodları çıkıyor — eşleme
                // firma ADI üzerinden yapıldığı için bu satırlara denk gelebiliyor).
                // Bu firmaların tip eşlemesi zaten yerelde mevcut (yalnızca 1 carinin
                // hiç tipi yok), o yüzden tipi TAHMİN ETMİYORUZ — alanları güncelleyip
                // tip eşlemesine dokunmuyoruz.
                int[] typeIds = row.Muhasebekod is null
                    ? [3, 4]
                    : row.Muhasebekod.StartsWith("320", StringComparison.Ordinal) ? [2]
                    : row.Muhasebekod.StartsWith("120", StringComparison.Ordinal) ? [1]
                    : [];

                if (typeIds.Length == 0)
                    unclassified++;

                var isNew = !existing.TryGetValue(row.Firmaid, out var account);
                account ??= new Account { SiberId = row.Firmaid, Discount = 0, CreatedAt = DateTime.Now };

                account.Name = row.Ad ?? account.Name;
                account.Address = row.Adres1 ?? account.Address;
                account.Phone = row.Telefon1 ?? account.Phone;
                account.Email = row.Email ?? account.Email;
                account.TaxOffice = row.Vergidaire ?? account.TaxOffice;
                account.TaxNumber = row.Vergino ?? account.TaxNumber;
                if (row.Muhasebekod is not null) account.AccountingCode = row.Muhasebekod;
                account.IsActive = row.Aktif ?? true;
                account.UpdatedAt = DateTime.Now;

                if (isNew)
                {
                    _db.Accounts.Add(account);
                    // Id, aşağıdaki AccountTypeMappings için hemen gerekiyor.
                    await _db.SaveChangesAsync(cancellationToken);
                    existing[row.Firmaid] = account;
                    created++;
                }
                else
                {
                    updated++;
                }

                var currentTypeIds = await _db.AccountTypeMappings.AsNoTracking()
                    .Where(m => m.AccountId == (int)account.Id)
                    .Select(m => m.AccountTypeId).ToListAsync(cancellationToken);

                foreach (var typeId in typeIds)
                    if (!currentTypeIds.Contains(typeId))
                        _db.AccountTypeMappings.Add(new AccountTypeMapping { AccountId = (int)account.Id, AccountTypeId = typeId });
            }
            catch (Exception ex)
            {
                errors.Add($"{row.Firmaid}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new SiberImportSummary(created, updated, errors) with
        {
            Errors = errors,
            // Artık "atlandı" DEĞİL ve hata da değil: firmanın alanları güncellendi,
            // yalnızca muhasebe kodundan cari tipi çıkarılamadı (kod 120/320 değil).
            // Bilgi amaçlı not — bkz. SiberImportSummary.Notes.
            Notes = unclassified > 0
                ? [$"{unclassified} satırın muhasebe kodundan cari tipi çıkarılamadı (alanlar güncellendi, tip eşlemesine dokunulmadı)."]
                : [],
        };
    }

    private sealed class AracRow
    {
        public string Aracid { get; set; } = string.Empty;
        public string? Plakano { get; set; }
        public int? Aractip { get; set; }
        public string? Romorkcins { get; set; }
        public int? Aracsahip { get; set; }
        public int? Aracdurum { get; set; }
        public string? Baglifirmaid { get; set; }
        public double? Km { get; set; }
        public int? Yici { get; set; }
        public int? Uluslararasi { get; set; }
        public double? En { get; set; }
        public double? Boy { get; set; }
        public double? Yukseklik { get; set; }
        public double? Kapasite { get; set; }
    }

    /// <summary>
    /// BULUNAN GERÇEK BOŞLUK: Araç (skn_arac) da yalnızca <see cref="ISiberImportService.ImportCarsAsync"/>
    /// ile TEK SEFERLİK ve yalnızca INSERT (hiç güncellemeden) içeri alınmıştı —
    /// canlıda doğrulandı, gerçek Siber'de 12 yeni araç vardı ki yerelde yoktu.
    /// Burada sürekli senkrona taşınırken diğer 10 kaynakla tutarlı olsun diye
    /// tam upsert'e (create+update) yükseltildi.
    /// </summary>
    public async Task<SiberImportSummary> SyncCarsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await OpenAsync(cancellationToken);

        var rows = (await connection.QueryAsync<AracRow>(
            new CommandDefinition(
                """
                SELECT CAST(aracid AS VARCHAR(64)) AS Aracid, plakano AS Plakano,
                       TRY_CAST(aractip AS INT) AS Aractip, romorkcins AS Romorkcins,
                       TRY_CAST(aracsahip AS INT) AS Aracsahip, TRY_CAST(aracdurum AS INT) AS Aracdurum,
                       CAST(baglifirmaid AS VARCHAR(64)) AS Baglifirmaid,
                       TRY_CAST(km AS FLOAT) AS Km, TRY_CAST(yici AS INT) AS Yici,
                       TRY_CAST(uluslararasi AS INT) AS Uluslararasi,
                       TRY_CAST(en AS FLOAT) AS En, TRY_CAST(boy AS FLOAT) AS Boy,
                       TRY_CAST(yukseklik AS FLOAT) AS Yukseklik, TRY_CAST(kapasite AS FLOAT) AS Kapasite
                FROM skn_arac
                """,
                cancellationToken: cancellationToken))).ToList();

        var carTypeByCode = ByIntCode(await _db.CarTypes.AsNoTracking().Where(x => x.Code != null).ToListAsync(cancellationToken), x => x.Code, x => x.Id);
        var romorkTypeByCode = ByCode(await _db.RomorkTypes.AsNoTracking().Where(x => x.Code != null).ToListAsync(cancellationToken), x => x.Code, x => x.Id);
        var carOwnerByCode = ByIntCode(await _db.CarOwners.AsNoTracking().Where(x => x.Code != null).ToListAsync(cancellationToken), x => x.Code, x => x.Id);
        var carStatusByCode = ByIntCode(await _db.CarStatusTypes.AsNoTracking().Where(x => x.Code != null).ToListAsync(cancellationToken), x => x.Code, x => x.Id);
        var accountIdBySiberId = BySiberId(await _db.Accounts.AsNoTracking().Where(x => x.SiberId != null).OrderBy(x => x.Id).ToListAsync(cancellationToken), x => x.SiberId, x => x.Id);

        var existing = await ExistingByKeyAsync(
            _db.Cars.Where(c => c.SiberId != null).OrderBy(c => c.Id), c => c.SiberId, cancellationToken);

        var created = 0;
        var updated = 0;
        var errors = new List<string>();

        foreach (var row in rows)
        {
            try
            {
                var isNew = !existing.TryGetValue(row.Aracid, out var car);
                car ??= new Car { SiberId = row.Aracid, CreatedAt = DateTime.Now };

                car.PlateNumber = row.Plakano ?? car.PlateNumber;
                car.CarType = row.Aractip is { } ct && carTypeByCode.TryGetValue(ct, out var carTypeId) ? (int)carTypeId : car.CarType;
                car.RomorkType = row.Romorkcins is { } rc && romorkTypeByCode.TryGetValue(rc, out var romorkId) ? (int)romorkId : car.RomorkType;
                car.VehicleOwner = row.Aracsahip is { } vo && carOwnerByCode.TryGetValue(vo, out var ownerId) ? (int)ownerId : car.VehicleOwner;
                car.VehicleStatus = row.Aracdurum is { } vs && carStatusByCode.TryGetValue(vs, out var statusId) ? (int)statusId : car.VehicleStatus;
                car.CustomerId = row.Baglifirmaid is { } bf && accountIdBySiberId.TryGetValue(bf, out var accountId)
                    ? accountId.ToString() : car.CustomerId;
                car.Km = row.Km ?? car.Km;
                car.InCountry = row.Yici ?? car.InCountry;
                car.International = row.Uluslararasi ?? car.International;
                car.Width = row.En ?? car.Width;
                car.Length = row.Boy ?? car.Length;
                car.Height = row.Yukseklik ?? car.Height;
                car.Capacity = row.Kapasite ?? car.Capacity;
                car.UpdatedAt = DateTime.Now;

                if (isNew)
                {
                    _db.Cars.Add(car);
                    created++;
                }
                else
                {
                    updated++;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{row.Aracid}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new SiberImportSummary(created, updated, errors);
    }

    /// <summary>Bkz. arayüzdeki XML açıklaması. Şehir hariç, mevcut ad-eşleşmeli upsert mantığı yeniden kullanılır.</summary>
    private sealed class FirmaTemsilciRow
    {
        public string Firmatemsilciid { get; set; } = string.Empty;
        public string? Firmaid { get; set; }
        public string? Kod { get; set; }
        public string? Ad { get; set; }
        public bool? Satistemsilcisi { get; set; }
        public bool? Operasyonyetkilisi { get; set; }
    }

    /// <summary>
    /// Cari ↔ görevli bağını Siber'den alır. Kişi Siber'de KOD ile (kullanıcının
    /// siber_code'u) belirtiliyor; kod tutmazsa ADA göre (Türkçe I/İ duyarsız)
    /// denenir — bkz. ByTurkishName.
    ///
    /// Siber'de tek satır hem satış temsilcisi hem operasyon yetkilisi olabiliyor
    /// (bayraklar bağımsız); bu durumda yerelde İKİ satır üretilir çünkü teklif
    /// formunda iki ayrı alan var (user_type 1 ve 2).
    /// </summary>
    public async Task<SiberImportSummary> SyncAccountRepresentativesAsync(
        CancellationToken cancellationToken = default)
    {
        using var connection = await OpenAsync(cancellationToken);

        var rows = (await connection.QueryAsync<FirmaTemsilciRow>(
            new CommandDefinition(
                """
                SELECT CAST(firmatemsilciid AS VARCHAR(64)) AS Firmatemsilciid,
                       CAST(firmaid AS VARCHAR(64)) AS Firmaid,
                       LTRIM(RTRIM(kod)) AS Kod, LTRIM(RTRIM(ad)) AS Ad,
                       satistemsilcisi AS Satistemsilcisi,
                       operasyonyetkilisi AS Operasyonyetkilisi
                FROM sbr_firmatemsilci
                """,
                cancellationToken: cancellationToken))).ToList();

        var accountBySiberId = BySiberId(
            await _db.Accounts.AsNoTracking().Where(a => a.SiberId != null).OrderBy(a => a.Id)
                .ToListAsync(cancellationToken), a => a.SiberId, a => a.Id);

        var users = await _db.Users.AsNoTracking().OrderBy(u => u.Id).ToListAsync(cancellationToken);
        var userByCode = ByCode(users, u => u.SiberCode, u => u.Id);
        var userByName = ByTurkishName(users, u => u.SiberName, u => u.Id);

        var existing = await ExistingByKeyAsync(
            _db.AccountRepresentatives.Where(r => r.SiberId != null).OrderBy(r => r.Id),
            // Aynı Siber satırı iki role açılabildiği için anahtar rolü de içerir.
            r => r.SiberId + "|" + r.UserType, cancellationToken);

        var created = 0;
        var updated = 0;
        var skipped = 0;
        var errors = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            try
            {
                if (row.Firmaid is null || !accountBySiberId.TryGetValue(row.Firmaid, out var accountId))
                {
                    skipped++;
                    continue;
                }

                long userId = 0;
                if (row.Kod is { } kod && userByCode.TryGetValue(kod, out var byKod)) userId = byKod;
                else if (row.Ad is { } ad && userByName.TryGetValue(QueryableExtensions.NormalizeTurkish(ad), out var byAd)) userId = byAd;

                if (userId == 0)
                {
                    skipped++;
                    continue;
                }

                // 1 = Operasyon Yetkilisi, 2 = Satış Temsilcisi (load_charge_people kodlaması).
                var roles = new List<int>();
                if (row.Operasyonyetkilisi == true) roles.Add(1);
                if (row.Satistemsilcisi == true) roles.Add(2);
                if (roles.Count == 0) { skipped++; continue; }

                foreach (var role in roles)
                {
                    var key = row.Firmatemsilciid + "|" + role;
                    seen.Add(key);

                    var isNew = !existing.TryGetValue(key, out var rep);
                    rep ??= new AccountRepresentative
                    {
                        SiberId = row.Firmatemsilciid, UserType = role, CreatedAt = DateTime.Now,
                    };

                    rep.AccountId = (int)accountId;
                    rep.UserId = (int)userId;
                    rep.UserType = role;
                    rep.UpdatedAt = DateTime.Now;

                    if (isNew)
                    {
                        _db.AccountRepresentatives.Add(rep);
                        existing[key] = rep;
                        created++;
                    }
                    else
                    {
                        updated++;
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{row.Firmatemsilciid}: {ex.Message}");
            }
        }

        // Siber'de kaldırılan bağ yerelde de kalkmalı — güvenlik freniyle
        // (bkz. SyncLoadTransferDocumentsAsync'teki aynı gerekçe).
        if (rows.Count >= existing.Count * 0.9)
        {
            var stale = existing.Where(kv => !seen.Contains(kv.Key)).Select(kv => kv.Value).ToList();
            if (stale.Count > 0) _db.AccountRepresentatives.RemoveRange(stale);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new SiberImportSummary(created, updated, errors) with
        {
            Errors = errors,
            // HATA DEĞİL. Canlıda incelendi: atlanan satırlar Siber'de engelle=1 ile
            // PASİFE ALINMIŞ çalışanlara işaret eden temsilci bağları (artı firmaid'i
            // boş 1 satır). Kullanıcı içe aktarımı bilinçli olarak engelle=0 filtresi
            // uyguladığı için bu kişiler yerelde yok — ve olmamalı da: pasif bir
            // çalışan yeni teklife satış temsilcisi olarak atanmamalı.
            // Etki ölçüldü: satış temsilcisi tanımlı 4093 firmanın 4085'inde bağ
            // kuruluyor, yalnızca 8 firma açıkta kalıyor.
            Notes = skipped > 0
                ? [$"{skipped} temsilci bağı atlandı (Siber'de pasif kullanıcı ya da firmasız satır)."]
                : [],
        };
    }

    public Task<SiberImportSummary> SyncReferenceDataAsync(CancellationToken cancellationToken = default) =>
        _import.ImportReferenceDataAsync(includeCities: false, cancellationToken: cancellationToken);
}
