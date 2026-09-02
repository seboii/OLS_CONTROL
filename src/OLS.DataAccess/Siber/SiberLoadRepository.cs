using Dapper;

namespace OLS.DataAccess.Siber;


/// <summary>
/// Siber tarafındaki yük tabloları: <c>skn_yuk</c>, <c>skn_yukkoli</c>,
/// <c>sfy_modulkalem</c>, <c>sfy_modulkayit</c> ve doğrulama için
/// <c>skn_rezervasyon</c>.
///
/// olsold: <c>Front\LoadTransfer\LoadTransferController</c>
/// </summary>
public interface ISiberLoadRepository
{
    bool IsConfigured { get; }

    /// <summary>Teklifin Siber'deki rezervasyon kaydını okur (doğrulama için).</summary>
    Task<SiberRezervasyon?> FindRezervasyonAsync(
        string rezervasyonId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verilen mali kalem kimliklerinden Siber'de BULUNMAYANLARI döner.
    ///
    /// Yük Siber'e yazıldıktan SONRA kalem yazımı patlarsa, Siber'deki yük
    /// geri alınamıyor (yerel işlem geri alınsa bile) ve kullanıcı "hata"
    /// görürken kayıt Siber'de duruyor. Bu yüzden kalemler yazmadan ÖNCE
    /// doğrulanır.
    /// </summary>
    Task<IReadOnlyList<string>> FindMissingKalemIdsAsync(
        IReadOnlyCollection<string> kalemIds, CancellationToken cancellationToken = default);


    Task<Guid> GenerateYukIdAsync(CancellationToken cancellationToken = default);
    Task<Guid> GenerateYukKoliIdAsync(CancellationToken cancellationToken = default);
    Task<Guid> GenerateModulKalemIdAsync(CancellationToken cancellationToken = default);

    /// <summary>Yük numarasına karşılık gelen modül kaydı (fatura kalemleri için).</summary>
    Task<SiberModulKayit?> FindModulKayitAsync(
        string loadNumberWorkType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Yük numarasını (aynı yıl+iş türü için max + 1) atomik biçimde atayarak
    /// <c>skn_yuk</c> INSERT'ini yapar, atanan numarayı ve biçimlendirilmiş
    /// (<c>yy00000EK</c>) hâlini döner. Numara üretimiyle INSERT tek transaction+kilit
    /// altında yapılır — bkz. metodun XML açıklaması.
    /// </summary>
    Task<SiberYukNumberResult> InsertYukWithLockedNumberAsync(
        SiberYuk yuk, string year, string additionalCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Teklifi (rezervasyonu) yeni açılan yüke bağlar:
    /// <c>UPDATE skn_rezervasyon SET yukid = ... WHERE rezervasyonid = ...</c>
    ///
    /// Siber'in kendi ekranları teklifin yükünü BU sütundan bulur; yazılmazsa
    /// <c>skn_yuk</c>'ta satır oluşsa bile teklif Siber tarafında "yüksüz" görünür
    /// ve yük numarası teklif üzerinde görünmez. Bkz. Siber Entegrasyon Raporu
    /// §6.2 adım 8.
    /// </summary>
    Task LinkRezervasyonToYukAsync(
        string rezervasyonId, string yukId, CancellationToken cancellationToken = default);
    Task InsertYukKoliAsync(SiberYukKoli koli, CancellationToken cancellationToken = default);
    Task InsertModulKalemAsync(SiberModulKalem kalem, CancellationToken cancellationToken = default);

    /// <summary>Yükü ve alt kayıtlarını Siber'den siler — bkz. uygulamadaki açıklama.</summary>
    Task DeleteYukAsync(string yukId, CancellationToken cancellationToken = default);
    Task DeleteYukKoliAsync(string yukKoliId, CancellationToken cancellationToken = default);
    Task DeleteModulKalemAsync(string modulKalemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Yük aktarma düzenlendiğinde Siber tarafını da günceller.
    /// Yük numarası, iş türü ve kimlikler DEĞİŞMEZ — olsold da bu alanları
    /// güncelleme listesinden çıkarmış (yorum satırı yapmış).
    /// </summary>
    Task UpdateYukAsync(SiberYuk yuk, CancellationToken cancellationToken = default);

    Task UpdateYukKoliAsync(SiberYukKoli koli, CancellationToken cancellationToken = default);
    Task UpdateModulKalemAsync(SiberModulKalem kalem, CancellationToken cancellationToken = default);

    /// <summary>
    /// Yük evrak takibi (<c>skn_yukevrak</c>) — gerçek dosya DEĞİL, fiziksel evrak
    /// çeklisti (tür + orijinal/kopya adedi + teslim bilgisi). Gerçek Siber'de
    /// doğrulandı: <c>evrakid</c> hiçbir tabloya referans vermiyor (rastgele
    /// üretiliyor), asıl tür kimliği <c>sirano</c> (bkz. EvrakTuru.Code).
    /// </summary>
    Task<Guid> GenerateYukEvrakIdAsync(CancellationToken cancellationToken = default);
    Task InsertYukEvrakAsync(SiberYukEvrak evrak, CancellationToken cancellationToken = default);
    Task UpdateYukEvrakAsync(SiberYukEvrak evrak, CancellationToken cancellationToken = default);
    Task DeleteYukEvrakAsync(string yukEvrakId, CancellationToken cancellationToken = default);
}

/// <summary>skn_rezervasyon'un doğrulamada kullanılan alanları.</summary>
public sealed class SiberRezervasyon
{
    public string? RezervasyonId { get; init; }

    /// <summary>
    /// Teklifin DONUSTUGU yuk. Dolu ise teklif zaten yuke cevrilmistir — mukerrer
    /// yuk acilmasini engellemek icin ConvertOfferAsync bunu kontrol eder.
    /// </summary>
    public string? YukId { get; init; }
    public string? IstenenRomorkCins { get; init; }
    public string? IsTuru { get; init; }
    public string? MusteriId { get; init; }
    public string? GondericiId { get; init; }
    public string? AliciId { get; init; }
    public string? OdemeSekliId { get; init; }
    public string? DurumId { get; init; }
    public string? DepartmanId { get; init; }
    public string? TalimatGelisSekli { get; init; }
    public string? YuklemeTip { get; init; }
    public string? YukTurKod { get; init; }
    public string? NavlunFirmaId { get; init; }
    public string? YuklemeUlkeId { get; init; }
    public string? BosaltmaUlkeId { get; init; }
    public int? OnTasimaTarafimizdanYapilir { get; init; }
    public int? SonTasimaTarafimizdanYapilir { get; init; }
    public int? CalismaSekli { get; init; }
}

public sealed class SiberModulKayit
{
    public string? ModulId { get; init; }
    public string? ModulKod { get; init; }
}

/// <summary>
/// <see cref="ISiberLoadRepository.InsertYukWithLockedNumberAsync"/> çıktısı: atanan
/// sayısal yük numarası ve biçimlendirilmiş (<c>yy00000EK</c>) hâli — ikincisi hem
/// <c>skn_yuk.yuknoisturu</c>'ya hem yerel <c>LoadTransfer.LoadNumberWorkType</c>/
/// <c>Load.LoadNumber</c>'a yazılır.
/// </summary>
public sealed class SiberYukNumberResult
{
    public int YukNo { get; init; }
    public string LoadNumberWorkType { get; init; } = string.Empty;
}

public sealed class SiberYuk
{
    public string YukId { get; init; } = string.Empty;

    /// <summary>
    /// Yükü DOĞURAN teklif (skn_rezervasyon.rezervasyonid) — TERS bağ.
    ///
    /// BULUNAN GERÇEK BOŞLUK: dönüşümde yalnızca ileri yön yazılıyordu
    /// (skn_rezervasyon.yukid, bkz. LinkRezervasyonToYukAsync). Siber'in
    /// rezervasyon ekranı bağlı yükü BU sütundan okuduğu için, bizim açtığımız
    /// yükler teklifin üzerinde görünmüyordu. Canlıda doğrulandı: Siber'in kendi
    /// yüklerinin 3673'ünde bu alan dolu, bizimkilerde NULL'dı.
    /// </summary>
    public string? RezervasyonId { get; init; }

    /// <summary>
    /// Yükün yazılacağı Siber şirketi. Boş bırakılırsa OLS
    /// (bkz. SiberLoadRepository.DefaultSirketId).
    /// </summary>
    public string? SirketId { get; init; }

    /// <summary>
    /// Yük durumu. Ekleme sırasında sabit 1 yazılır (yeni yük "sipariş"),
    /// güncellemede yerel <c>load_status_id</c>'den gelir.
    /// </summary>
    public string? DurumId { get; init; }

    public int YukNo { get; init; }
    public string? IsTuru { get; init; }
    public string? YuklemeTip { get; init; }
    public string? FirmaId { get; init; }
    public string? GondericiId { get; init; }
    public string? AliciId { get; init; }
    public string? OdemeSekliId { get; init; }
    public string? TalimatGelisSekli { get; init; }
    public string? IstenenRomorkCins { get; init; }
    public decimal? ToplamAgirlik { get; init; }
    public decimal? ToplamHacim { get; init; }
    public decimal? ToplamLademetre { get; init; }
    public decimal? UcretAgirlik { get; init; }
    public string? MusteriTemsilcisiAd { get; init; }
    public string? DepartmanId { get; init; }
    public string? YukNoIsTuru { get; init; }
    public decimal? ToplamKap { get; init; }
    public string? KayitGiren { get; init; }
    public string Yil { get; init; } = string.Empty;
    public DateTime? TalimatGelisTarihi { get; init; }
    public string? YukTurKod { get; init; }
    public string? YuklemeUlke { get; init; }
    public string? BosaltmaUlke { get; init; }
    public int? CalismaSekli { get; init; }
    public DateTime KayitGirisTarih { get; init; }

    /// <summary>
    /// BULUNAN GERÇEK BOŞLUK: bu 6 alan Genel Bilgiler'de düzenlenebilir ve yerelde
    /// doğru kaydediliyordu, ama Siber'e HİÇ gönderilmiyordu — kaynakta
    /// (LoadTransferController.php update(), satır 726-731) hepsi canlı/aktif
    /// yazılıyor, hiçbiri yorum satırı değil. Yalnızca UPDATE'te kullanılır
    /// (ekleme/dönüşüm sırasında olsold da bunları göndermiyor).
    /// </summary>
    public string? TeslimSekil { get; init; }
    public int? OnTasimaTarafimizdanYapilir { get; init; }
    public int? SonTasimaTarafimizdanYapilir { get; init; }

    /// <summary>
    /// Kaynakta AYNI kaynak alan (<c>request_arrival_date</c>) hem
    /// <see cref="TalimatGelisTarihi"/>'ye (talimatgelistarihi) HEM buna
    /// (istenenvaristarihi) yazılıyor — iki farklı Siber sütununa yinelenen
    /// yazma, kaynakta da böyle (satır 713 ve 729).
    /// </summary>
    public DateTime? IstenenVarisTarihi { get; init; }
    public DateTime? HazirOlmaTarih { get; init; }
    public DateTime? MusteridenAlinisTarih { get; init; }
}

public sealed class SiberYukKoli
{
    public string YukKoliId { get; init; } = string.Empty;
    public string YukId { get; init; } = string.Empty;
    public int? KapAdet { get; init; }
    public string? KapId { get; init; }
    public decimal? En { get; init; }
    public decimal? Boy { get; init; }
    public decimal? Yukseklik { get; init; }
    public decimal? Hacim { get; init; }
    public decimal? BurutAgirlik { get; init; }
    public decimal? NetAgirlik { get; init; }
    public decimal? Lademetre { get; init; }
    public int? Istiflenemez { get; init; }
    public string? MalCinsId { get; init; }
}

public sealed class SiberModulKalem
{
    public string ModulKalemId { get; init; } = string.Empty;
    public string? ModulId { get; init; }
    public string? ModulKod { get; init; }
    public string? KalemId { get; init; }

    /// <summary>'C' alış, 'G' satış.</summary>
    public string Gc { get; init; } = "C";

    public string? FirmaId { get; init; }
    public decimal? ToplamTutar { get; init; }
    public string? DovizKod { get; init; }
    public decimal? BirimFiyat { get; init; }
    public decimal? Miktar { get; init; }
    public decimal? Tutar { get; init; }
    public DateTime KayitGirisTarih { get; init; }
    public string? KayitGiren { get; init; }
}

/// <summary>
/// skn_yukevrak: fiziksel evrak çeklisti (gerçek dosya değil — bkz. sirano/evrakad
/// için 10 sabit tür: 1=Navlun Faturası, 2=Invoice, 3=Konşimento, 4=CMR,
/// 5=Mal Faturası, 6=ATR-1, 7=Packing List, 8=Sağlık Sertifikası, 9=Çeki Listesi,
/// 10=Menşei Şehadetnamesi).
/// </summary>
public sealed class SiberYukEvrak
{
    public string YukEvrakId { get; init; } = string.Empty;
    public string YukId { get; init; } = string.Empty;
    public int Sirano { get; init; }
    public string? EvrakAd { get; init; }
    public string? EvrakNo { get; init; }
    public DateTime? Tarih { get; init; }
    public int? OrjinalAdet { get; init; }
    public int? KopyaAdet { get; init; }
    public string? TeslimAlan { get; init; }
    public DateTime? TeslimTarih { get; init; }
    public string? Aciklama { get; init; }
}

public sealed class SiberLoadRepository : ISiberLoadRepository
{
    /// <summary>
    /// Varsayılan şirket: OLS DIŞ TİCARET. Siber'de iki şirket var (sbr_sirket) ve
    /// teklif akışından doğan yükler her zaman OLS'e yazılıyor. Teklifsiz yük açan
    /// Avrora kullanıcısı için <see cref="SiberYuk.SirketId"/> ile değiştirilebilir —
    /// aksi hâlde Avrora'nın açtığı yük OLS'e düşer ve görünürlük ayrımı daha
    /// doğduğu anda bozulurdu.
    /// </summary>
    public const string DefaultSirketId = "BA4888B1-A2B0-4142-B273-92481D932EAD";
    private const string SubeId = "69588E44-731B-46E5-83A4-A338816E2300";

    /// <summary>olsold'da sabit çarpanlar: ücret ağırlığı ve hacim hesabı için.</summary>
    public const decimal LademeterMultiplier = 1750m;
    public const decimal VolumeMultiplier = 333.33m;
    public const int DefaultCarHeight = 280;

    private readonly ISiberConnectionFactory _factory;

    public SiberLoadRepository(ISiberConnectionFactory factory) => _factory = factory;

    public bool IsConfigured => _factory.IsConfigured;

    public async Task<IReadOnlyList<string>> FindMissingKalemIdsAsync(
        IReadOnlyCollection<string> kalemIds, CancellationToken cancellationToken = default)
    {
        var candidates = kalemIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (candidates.Count == 0)
            return [];

        // Biçimi GUID olmayan kimlik zaten Siber'de olamaz; sorguya sokmadan
        // eksik sayılır (uniqueidentifier dönüşümü aksi hâlde hata verirdi).
        var parsable = candidates.Where(id => Guid.TryParse(id, out _)).ToList();
        var missing = candidates.Except(parsable, StringComparer.OrdinalIgnoreCase).ToList();

        if (parsable.Count == 0)
            return missing;

        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        var found = (await connection.QueryAsync<string>(new CommandDefinition(
            """
            SELECT LOWER(CAST(kalemid AS VARCHAR(64)))
            FROM skn_kalem
            WHERE kalemid IN @Ids
            """,
            new { Ids = parsable.Select(Guid.Parse).ToArray() },
            cancellationToken: cancellationToken))).ToHashSet(StringComparer.OrdinalIgnoreCase);

        missing.AddRange(parsable.Where(id => !found.Contains(id)));
        return missing;
    }

    public async Task<SiberRezervasyon?> FindRezervasyonAsync(
        string rezervasyonId, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        return await connection.QueryFirstOrDefaultAsync<SiberRezervasyon>(
            """
            -- DİKKAT: uniqueidentifier kolonlar C# tarafında string? olarak okunuyor;
            -- Dapper Guid→string dönüşümünü yapamaz ("Object must implement
            -- IConvertible") ve satır materialize edilemez. Bu yüzden GUID kolonların
            -- HEPSİ açıkça VARCHAR'a CAST edilir — projede yerleşik kural (bkz.
            -- SiberSyncService'teki aynı desen).
            SELECT CAST(rezervasyonid AS VARCHAR(64)) AS RezervasyonId,
                   istenenromorkcins AS IstenenRomorkCins,
                   isturu AS IsTuru,
                   CAST(musteriid AS VARCHAR(64)) AS MusteriId,
                   CAST(gondericiid AS VARCHAR(64)) AS GondericiId,
                   CAST(aliciid AS VARCHAR(64)) AS AliciId,
                   CAST(odemesekliid AS VARCHAR(64)) AS OdemeSekliId,
                   CAST(durumid AS VARCHAR(64)) AS DurumId,
                   CAST(departmanid AS VARCHAR(64)) AS DepartmanId,
                   talimatgelissekli AS TalimatGelisSekli,
                   yuklemetip AS YuklemeTip, yukturkod AS YukTurKod,
                   CAST(navlunfirmaid AS VARCHAR(64)) AS NavlunFirmaId,
                   CAST(yuklemeulkeid AS VARCHAR(64)) AS YuklemeUlkeId,
                   CAST(bosaltmaulkeid AS VARCHAR(64)) AS BosaltmaUlkeId,
                   ontasimatarafimizdanyapilir AS OnTasimaTarafimizdanYapilir,
                   sontasimatarafimizdanyapilir AS SonTasimaTarafimizdanYapilir,
                   calismasekli AS CalismaSekli,
                   CAST(yukid AS VARCHAR(64)) AS YukId
            FROM skn_rezervasyon WHERE rezervasyonid = @id
            """,
            new { id = rezervasyonId });
    }

    public Task<Guid> GenerateYukIdAsync(CancellationToken cancellationToken = default) =>
        GenerateUniqueAsync("skn_yuk", "yukid", cancellationToken);

    public Task<Guid> GenerateYukKoliIdAsync(CancellationToken cancellationToken = default) =>
        GenerateUniqueAsync("skn_yukkoli", "yukkoliid", cancellationToken);

    public Task<Guid> GenerateModulKalemIdAsync(CancellationToken cancellationToken = default) =>
        GenerateUniqueAsync("sfy_modulkalem", "modulkalemid", cancellationToken);

    public Task<Guid> GenerateYukEvrakIdAsync(CancellationToken cancellationToken = default) =>
        GenerateUniqueAsync("skn_yukevrak", "evrakid", cancellationToken);

    public async Task<SiberModulKayit?> FindModulKayitAsync(
        string loadNumberWorkType, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        return await connection.QueryFirstOrDefaultAsync<SiberModulKayit>(
            // modulid uniqueidentifier — string? olarak okunduğu için CAST şart
            // (bkz. FindRezervasyonAsync'teki aynı gerekçe).
            "SELECT TOP 1 CAST(modulid AS VARCHAR(64)) AS ModulId, modulkod AS ModulKod FROM sfy_modulkayit WHERE ad = @ad",
            new { ad = loadNumberWorkType });
    }

    /// <summary>
    /// olsold'da yük numarası kilitsiz <c>MAX(yukno) + 1</c> ile üretilir, üstelik AYNI
    /// sorgu ikinci kez (sayısal ve biçimlendirilmiş numara için ayrı ayrı) çalıştırılır
    /// (Siber Entegrasyon Raporu §6.1, risk #3): aynı iş türü+yıl için iki eşzamanlı
    /// "Yüke Dönüştür" çağrısı aynı numarayı alabilir, hatta TEK çağrı içinde iki sorgu
    /// arasına başka bir kayıt girerse sayısal/biçimlendirilmiş numara birbirinden
    /// sapabilir. Burada numara üretimi (tek sorgu, iki değer de aynı okumadan türetilir)
    /// ve INSERT <c>sp_getapplock</c> ile serileştirilmiş TEK transaction içinde yapılır.
    /// Kilit adı iş türü+yıla göre kapsamlı (<c>skn_yuk_no:{isturu}:{yil}</c>) — farklı iş
    /// türü/yıl kombinasyonları birbirini bloklamaz, sayaç zaten ayrı. <c>@LockOwner =
    /// 'Transaction'</c> kilidi COMMIT/ROLLBACK'te otomatik bırakır; farklı bağlantılardan
    /// (uygulamanın birden fazla örneği olsa dahi) gelen eşzamanlı çağrıları da
    /// serileştirir.
    /// </summary>
    public async Task<SiberYukNumberResult> InsertYukWithLockedNumberAsync(
        SiberYuk yuk, string year, string additionalCode, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        // Sabit değerler (durumid=1, kamyonda/kuyrukta/cmr/fcr=0, araç yüksekliği,
        // lademetre ve hacim çarpanları, kıtalar) olsold'dan birebir taşındı.
        const string sql = """
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;

            DECLARE @lockResult INT;
            EXEC @lockResult = sp_getapplock
                @Resource = @LockResource, @LockMode = 'Exclusive',
                @LockOwner = 'Transaction', @LockTimeout = 15000;
            IF @lockResult < 0
            BEGIN
                ROLLBACK TRANSACTION;
                THROW 51000, 'Yük numarası kilidi alınamadı (zaman aşımı).', 1;
            END;

            DECLARE @nextNo INT = (SELECT ISNULL(MAX(yukno), 0) FROM skn_yuk WHERE isturu = @IsTuru AND yil = @Yil) + 1;
            DECLARE @loadNumberWorkType NVARCHAR(32) = @Yil + RIGHT('00000' + CAST(@nextNo AS VARCHAR(10)), 5) + @AdditionalCode;

            INSERT INTO skn_yuk
                (yukid, sirketid, subeid, yukno, isturu, bagliyukno, durumid, yuklemetip,
                 firmaid, gondericiid, aliciid, odemesekliid, kamyonda, kuyrukta,
                 cmrduzenlenecek, fcrduzenlenecek, talimatgelissekli, istenenromorkcins,
                 toplamagirlik, toplamhacim, toplamlademetre, ucretagirlik,
                 musteritemsilcisiad, departmanid, operasyondepartmanid, yuknoisturu,
                 kayitgiristarih, bagliyuknoisturu, toplamkap, kayitgiren, yil,
                 talimatgelistarihi, lademetrecarpan, hacimcarpan, aracyuksekligi,
                 yukturkod, _yuklemeulke, _bosaltmaulke, _yuklemekita, _bosaltmakita,
                 bildirimyapankullanicikod, satistemsilcisikod, calismasekli,
                 rezervasyonid)
            VALUES
                (@YukId, @SirketId, @SubeId, @nextNo, @IsTuru, @nextNo, 1, @YuklemeTip,
                 @FirmaId, @GondericiId, @AliciId, @OdemeSekliId, 0, 0,
                 0, 0, @TalimatGelisSekli, @IstenenRomorkCins,
                 @ToplamAgirlik, @ToplamHacim, @ToplamLademetre, @UcretAgirlik,
                 @MusteriTemsilcisiAd, @DepartmanId, @DepartmanId, @loadNumberWorkType,
                 @KayitGirisTarih, @loadNumberWorkType, @ToplamKap, @KayitGiren, @Yil,
                 @TalimatGelisTarihi, @LademeterMultiplier, @VolumeMultiplier, @CarHeight,
                 @YukTurKod, @YuklemeUlke, @BosaltmaUlke, 'ASYA', 'ASYA',
                 @KayitGiren, @KayitGiren, @CalismaSekli,
                 @RezervasyonId);

            COMMIT TRANSACTION;

            SELECT @nextNo AS YukNo, @loadNumberWorkType AS LoadNumberWorkType;
            """;

        return await connection.QuerySingleAsync<SiberYukNumberResult>(sql, new
        {
            yuk.YukId, SirketId = yuk.SirketId ?? DefaultSirketId, SubeId,
            yuk.IsTuru, Yil = year, AdditionalCode = additionalCode,
            LockResource = $"skn_yuk_no:{yuk.IsTuru}:{year}",
            yuk.YuklemeTip, yuk.FirmaId, yuk.GondericiId, yuk.AliciId, yuk.OdemeSekliId,
            yuk.TalimatGelisSekli, yuk.IstenenRomorkCins, yuk.ToplamAgirlik,
            yuk.ToplamHacim, yuk.ToplamLademetre, yuk.UcretAgirlik,
            yuk.MusteriTemsilcisiAd, yuk.DepartmanId,
            yuk.KayitGirisTarih, yuk.ToplamKap, yuk.KayitGiren,
            yuk.TalimatGelisTarihi, LademeterMultiplier, VolumeMultiplier,
            CarHeight = DefaultCarHeight, yuk.YukTurKod, yuk.YuklemeUlke,
            yuk.BosaltmaUlke, yuk.CalismaSekli, yuk.RezervasyonId,
        });
    }

    public async Task LinkRezervasyonToYukAsync(
        string rezervasyonId, string yukId, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        await connection.ExecuteAsync(
            "UPDATE skn_rezervasyon SET yukid = @yukId WHERE rezervasyonid = @rezervasyonId",
            new { yukId, rezervasyonId });
    }

    public async Task InsertYukKoliAsync(
        SiberYukKoli koli, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        const string sql = """
            INSERT INTO skn_yukkoli
                (yukkoliid, yukid, kapadet, kapid, en, boy, yukseklik, hacim,
                 burutagirlik, netagirlik, lademetre, istiflenemez, malcinsid)
            VALUES
                (@YukKoliId, @YukId, @KapAdet, @KapId, @En, @Boy, @Yukseklik, @Hacim,
                 @BurutAgirlik, @NetAgirlik, @Lademetre, @Istiflenemez, @MalCinsId)
            """;

        await connection.ExecuteAsync(sql, koli);
    }

    public async Task InsertModulKalemAsync(
        SiberModulKalem kalem, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        const string sql = """
            INSERT INTO sfy_modulkalem
                (modulkalemid, modulid, modulkod, kalemid, gc, firmaid, toplamtutar,
                 dovizkod, birimfiyat, miktar, kdvtutar, kdvoran, tutar, subeid,
                 kayitgiristarih, kayitgiren, rezervasyondanaktarildi)
            VALUES
                (@ModulKalemId, @ModulId, @ModulKod, @KalemId, @Gc, @FirmaId, @ToplamTutar,
                 @DovizKod, @BirimFiyat, @Miktar, 0, 0, @Tutar, @SubeId,
                 @KayitGirisTarih, @KayitGiren, 1)
            """;

        await connection.ExecuteAsync(sql, new
        {
            kalem.ModulKalemId, kalem.ModulId, kalem.ModulKod, kalem.KalemId, kalem.Gc,
            kalem.FirmaId, kalem.ToplamTutar, kalem.DovizKod, kalem.BirimFiyat,
            kalem.Miktar, kalem.Tutar, SubeId, kalem.KayitGirisTarih, kalem.KayitGiren,
        });
    }

    public async Task InsertYukEvrakAsync(
        SiberYukEvrak evrak, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        const string sql = """
            INSERT INTO skn_yukevrak
                (evrakid, yukid, sirano, evrakad, evrakno, tarih, orjinaladet, kopyaadet,
                 teslimalan, teslimtarih, aciklama)
            VALUES
                (@YukEvrakId, @YukId, @Sirano, @EvrakAd, @EvrakNo, @Tarih, @OrjinalAdet,
                 @KopyaAdet, @TeslimAlan, @TeslimTarih, @Aciklama)
            """;

        await connection.ExecuteAsync(sql, evrak);
    }

    public async Task UpdateYukAsync(SiberYuk yuk, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        // yukid/yukno/isturu güncellenmez: kayıt kimliği ve numarası sabittir.
        const string sql = """
            UPDATE skn_yuk SET
                durumid            = @DurumId,
                yuklemetip         = @YuklemeTip,
                firmaid            = @FirmaId,
                gondericiid        = @GondericiId,
                aliciid            = @AliciId,
                odemesekliid       = @OdemeSekliId,
                talimatgelissekli  = @TalimatGelisSekli,
                istenenromorkcins  = @IstenenRomorkCins,
                toplamagirlik      = @ToplamAgirlik,
                toplamhacim        = @ToplamHacim,
                toplamlademetre    = @ToplamLademetre,
                ucretagirlik       = @UcretAgirlik,
                toplamkap          = @ToplamKap,
                musteritemsilcisiad= @MusteriTemsilcisiAd,
                departmanid        = @DepartmanId,
                operasyondepartmanid = @DepartmanId,
                talimatgelistarihi = @TalimatGelisTarihi,
                _yuklemeulke       = @YuklemeUlke,
                _bosaltmaulke      = @BosaltmaUlke,
                calismasekli       = @CalismaSekli,
                aracyuksekligi     = @CarHeight,
                teslimsekil                  = @TeslimSekil,
                ontasimatarafimizdanyapilir  = @OnTasimaTarafimizdanYapilir,
                sontasimatarafimizdanyapilir = @SonTasimaTarafimizdanYapilir,
                istenenvaristarihi           = @IstenenVarisTarihi,
                hazirolmatarih                = @HazirOlmaTarih,
                musteridenalinistarih        = @MusteridenAlinisTarih
            WHERE yukid = @YukId
            """;

        await connection.ExecuteAsync(sql, new
        {
            yuk.YukId, yuk.DurumId, yuk.YuklemeTip, yuk.FirmaId, yuk.GondericiId,
            yuk.AliciId, yuk.OdemeSekliId, yuk.TalimatGelisSekli, yuk.IstenenRomorkCins,
            yuk.ToplamAgirlik, yuk.ToplamHacim, yuk.ToplamLademetre, yuk.UcretAgirlik,
            yuk.ToplamKap, yuk.MusteriTemsilcisiAd, yuk.DepartmanId,
            yuk.TalimatGelisTarihi, yuk.YuklemeUlke, yuk.BosaltmaUlke, yuk.CalismaSekli,
            CarHeight = DefaultCarHeight,
            yuk.TeslimSekil, yuk.OnTasimaTarafimizdanYapilir, yuk.SonTasimaTarafimizdanYapilir,
            yuk.IstenenVarisTarihi, yuk.HazirOlmaTarih, yuk.MusteridenAlinisTarih,
        });
    }

    public async Task UpdateYukKoliAsync(
        SiberYukKoli koli, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        const string sql = """
            UPDATE skn_yukkoli SET
                kapadet      = @KapAdet,
                kapid        = @KapId,
                en           = @En,
                boy          = @Boy,
                yukseklik    = @Yukseklik,
                hacim        = @Hacim,
                burutagirlik = @BurutAgirlik,
                netagirlik   = @NetAgirlik,
                lademetre    = @Lademetre,
                istiflenemez = @Istiflenemez,
                malcinsid    = @MalCinsId
            WHERE yukkoliid = @YukKoliId
            """;

        await connection.ExecuteAsync(sql, koli);
    }

    public async Task UpdateModulKalemAsync(
        SiberModulKalem kalem, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        const string sql = """
            UPDATE sfy_modulkalem SET
                kalemid     = @KalemId,
                gc          = @Gc,
                firmaid     = @FirmaId,
                toplamtutar = @ToplamTutar,
                dovizkod    = @DovizKod,
                birimfiyat  = @BirimFiyat,
                miktar      = @Miktar,
                tutar       = @Tutar
            WHERE modulkalemid = @ModulKalemId
            """;

        await connection.ExecuteAsync(sql, new
        {
            kalem.ModulKalemId, kalem.KalemId, kalem.Gc, kalem.FirmaId,
            kalem.ToplamTutar, kalem.DovizKod, kalem.BirimFiyat, kalem.Miktar, kalem.Tutar,
        });
    }

    public async Task UpdateYukEvrakAsync(
        SiberYukEvrak evrak, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        const string sql = """
            UPDATE skn_yukevrak SET
                sirano      = @Sirano,
                evrakad     = @EvrakAd,
                evrakno     = @EvrakNo,
                tarih       = @Tarih,
                orjinaladet = @OrjinalAdet,
                kopyaadet   = @KopyaAdet,
                teslimalan  = @TeslimAlan,
                teslimtarih = @TeslimTarih,
                aciklama    = @Aciklama
            WHERE evrakid = @YukEvrakId
            """;

        await connection.ExecuteAsync(sql, evrak);
    }

    /// <summary>
    /// Yükü Siber'den tamamen siler: önce alt kayıtlar (koli + mali kalem +
    /// evrak + sefer eşlemesi), sonra skn_yuk satırı.
    ///
    /// Siber'den de silmek ŞART: yalnızca yerelden silinirse periyodik senkron
    /// (SyncLoadTransfersAsync) bir sonraki turda satırı Siber'den geri getirir —
    /// yani yerel silme kalıcı olmaz. Ayrıca teklifin skn_rezervasyon.yukid
    /// bağlantısı da temizlenir, aksi hâlde teklif silinmiş bir yüke işaret eder.
    /// </summary>
    public async Task DeleteYukAsync(string yukId, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        await connection.ExecuteAsync("""
            -- Mali kalemler (gelir/gider) ÖNCE silinmeli: skn_yuk üzerindeki Siber
            -- tetikleyicisi, bağlı sfy_modulkalem satırı kaldıysa silmeyi reddediyor
            -- ("Bu Yüke Ait Bekleyen Gelir/Gider kayıtları bulunmaktadır"). Bağlantı
            -- dolaylı: sfy_modulkalem -> sfy_modulkayit(ad = yük numarası).
            DELETE mk
            FROM sfy_modulkalem mk
            JOIN sfy_modulkayit mky ON mky.modulid = mk.modulid
            JOIN skn_yuk y ON LTRIM(RTRIM(mky.ad)) = LTRIM(RTRIM(y.yuknoisturu))
            WHERE y.yukid = @id AND LTRIM(RTRIM(mky.yer)) = 'YUK';

            DELETE FROM skn_yukkoli   WHERE yukid = @id;
            DELETE FROM skn_yukevrak  WHERE yukid = @id;
            DELETE FROM skn_yukaktarma WHERE yukid = @id;
            UPDATE skn_rezervasyon SET yukid = NULL WHERE yukid = @id;
            DELETE FROM skn_yuk       WHERE yukid = @id;
            """, new { id = yukId });
    }

    public async Task DeleteYukKoliAsync(
        string yukKoliId, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        await connection.ExecuteAsync(
            "DELETE FROM skn_yukkoli WHERE yukkoliid = @id", new { id = yukKoliId });
    }

    public async Task DeleteModulKalemAsync(
        string modulKalemId, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        await connection.ExecuteAsync(
            "DELETE FROM sfy_modulkalem WHERE modulkalemid = @id", new { id = modulKalemId });
    }

    public async Task DeleteYukEvrakAsync(
        string yukEvrakId, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        await connection.ExecuteAsync(
            "DELETE FROM skn_yukevrak WHERE evrakid = @id", new { id = yukEvrakId });
    }

    private async Task<Guid> GenerateUniqueAsync(
        string table, string column, CancellationToken cancellationToken)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        while (true)
        {
            var candidate = Guid.NewGuid();

            var exists = await connection.ExecuteScalarAsync<int>(
                $"SELECT COUNT(1) FROM {table} WHERE {column} = @value",
                new { value = candidate.ToString() });

            if (exists == 0)
                return candidate;
        }
    }
}
