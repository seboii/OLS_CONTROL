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

    /// <summary>Aynı yıl ve iş türü için sıradaki yük numarası (max + 1).</summary>
    Task<int> NextYukNoAsync(
        string? isTuru, string year, CancellationToken cancellationToken = default);

    Task<Guid> GenerateYukIdAsync(CancellationToken cancellationToken = default);
    Task<Guid> GenerateYukKoliIdAsync(CancellationToken cancellationToken = default);
    Task<Guid> GenerateModulKalemIdAsync(CancellationToken cancellationToken = default);

    /// <summary>Yük numarasına karşılık gelen modül kaydı (fatura kalemleri için).</summary>
    Task<SiberModulKayit?> FindModulKayitAsync(
        string loadNumberWorkType, CancellationToken cancellationToken = default);

    Task InsertYukAsync(SiberYuk yuk, CancellationToken cancellationToken = default);
    Task InsertYukKoliAsync(SiberYukKoli koli, CancellationToken cancellationToken = default);
    Task InsertModulKalemAsync(SiberModulKalem kalem, CancellationToken cancellationToken = default);

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
}

/// <summary>skn_rezervasyon'un doğrulamada kullanılan alanları.</summary>
public sealed class SiberRezervasyon
{
    public string? RezervasyonId { get; init; }
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

public sealed class SiberYuk
{
    public string YukId { get; init; } = string.Empty;

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

public sealed class SiberLoadRepository : ISiberLoadRepository
{
    private const string SirketId = "BA4888B1-A2B0-4142-B273-92481D932EAD";
    private const string SubeId = "69588E44-731B-46E5-83A4-A338816E2300";

    /// <summary>olsold'da sabit çarpanlar: ücret ağırlığı ve hacim hesabı için.</summary>
    public const decimal LademeterMultiplier = 1750m;
    public const decimal VolumeMultiplier = 333.33m;
    public const int DefaultCarHeight = 280;

    private readonly ISiberConnectionFactory _factory;

    public SiberLoadRepository(ISiberConnectionFactory factory) => _factory = factory;

    public bool IsConfigured => _factory.IsConfigured;

    public async Task<SiberRezervasyon?> FindRezervasyonAsync(
        string rezervasyonId, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        return await connection.QueryFirstOrDefaultAsync<SiberRezervasyon>(
            """
            SELECT rezervasyonid AS RezervasyonId, istenenromorkcins AS IstenenRomorkCins,
                   isturu AS IsTuru, musteriid AS MusteriId, gondericiid AS GondericiId,
                   aliciid AS AliciId, odemesekliid AS OdemeSekliId, durumid AS DurumId,
                   departmanid AS DepartmanId, talimatgelissekli AS TalimatGelisSekli,
                   yuklemetip AS YuklemeTip, yukturkod AS YukTurKod,
                   navlunfirmaid AS NavlunFirmaId, yuklemeulkeid AS YuklemeUlkeId,
                   bosaltmaulkeid AS BosaltmaUlkeId,
                   ontasimatarafimizdanyapilir AS OnTasimaTarafimizdanYapilir,
                   sontasimatarafimizdanyapilir AS SonTasimaTarafimizdanYapilir,
                   calismasekli AS CalismaSekli
            FROM skn_rezervasyon WHERE rezervasyonid = @id
            """,
            new { id = rezervasyonId });
    }

    public async Task<int> NextYukNoAsync(
        string? isTuru, string year, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        var max = await connection.ExecuteScalarAsync<int?>(
            "SELECT MAX(yukno) FROM skn_yuk WHERE isturu = @isturu AND yil = @yil",
            new { isturu = isTuru, yil = year });

        return (max ?? 0) + 1;
    }

    public Task<Guid> GenerateYukIdAsync(CancellationToken cancellationToken = default) =>
        GenerateUniqueAsync("skn_yuk", "yukid", cancellationToken);

    public Task<Guid> GenerateYukKoliIdAsync(CancellationToken cancellationToken = default) =>
        GenerateUniqueAsync("skn_yukkoli", "yukkoliid", cancellationToken);

    public Task<Guid> GenerateModulKalemIdAsync(CancellationToken cancellationToken = default) =>
        GenerateUniqueAsync("sfy_modulkalem", "modulkalemid", cancellationToken);

    public async Task<SiberModulKayit?> FindModulKayitAsync(
        string loadNumberWorkType, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        return await connection.QueryFirstOrDefaultAsync<SiberModulKayit>(
            "SELECT TOP 1 modulid AS ModulId, modulkod AS ModulKod FROM sfy_modulkayit WHERE ad = @ad",
            new { ad = loadNumberWorkType });
    }

    public async Task InsertYukAsync(SiberYuk yuk, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        // Sabit değerler (durumid=1, kamyonda/kuyrukta/cmr/fcr=0, araç yüksekliği,
        // lademetre ve hacim çarpanları, kıtalar) olsold'dan birebir taşındı.
        const string sql = """
            INSERT INTO skn_yuk
                (yukid, sirketid, subeid, yukno, isturu, bagliyukno, durumid, yuklemetip,
                 firmaid, gondericiid, aliciid, odemesekliid, kamyonda, kuyrukta,
                 cmrduzenlenecek, fcrduzenlenecek, talimatgelissekli, istenenromorkcins,
                 toplamagirlik, toplamhacim, toplamlademetre, ucretagirlik,
                 musteritemsilcisiad, departmanid, operasyondepartmanid, yuknoisturu,
                 kayitgiristarih, bagliyuknoisturu, toplamkap, kayitgiren, yil,
                 talimatgelistarihi, lademetrecarpan, hacimcarpan, aracyuksekligi,
                 yukturkod, _yuklemeulke, _bosaltmaulke, _yuklemekita, _bosaltmakita,
                 bildirimyapankullanicikod, satistemsilcisikod, calismasekli)
            VALUES
                (@YukId, @SirketId, @SubeId, @YukNo, @IsTuru, @YukNo, 1, @YuklemeTip,
                 @FirmaId, @GondericiId, @AliciId, @OdemeSekliId, 0, 0,
                 0, 0, @TalimatGelisSekli, @IstenenRomorkCins,
                 @ToplamAgirlik, @ToplamHacim, @ToplamLademetre, @UcretAgirlik,
                 @MusteriTemsilcisiAd, @DepartmanId, @DepartmanId, @YukNoIsTuru,
                 @KayitGirisTarih, @YukNoIsTuru, @ToplamKap, @KayitGiren, @Yil,
                 @TalimatGelisTarihi, @LademeterMultiplier, @VolumeMultiplier, @CarHeight,
                 @YukTurKod, @YuklemeUlke, @BosaltmaUlke, 'ASYA', 'ASYA',
                 @KayitGiren, @KayitGiren, @CalismaSekli)
            """;

        await connection.ExecuteAsync(sql, new
        {
            yuk.YukId, SirketId, SubeId, yuk.YukNo, yuk.IsTuru, yuk.YuklemeTip,
            yuk.FirmaId, yuk.GondericiId, yuk.AliciId, yuk.OdemeSekliId,
            yuk.TalimatGelisSekli, yuk.IstenenRomorkCins, yuk.ToplamAgirlik,
            yuk.ToplamHacim, yuk.ToplamLademetre, yuk.UcretAgirlik,
            yuk.MusteriTemsilcisiAd, yuk.DepartmanId, yuk.YukNoIsTuru,
            yuk.KayitGirisTarih, yuk.ToplamKap, yuk.KayitGiren, yuk.Yil,
            yuk.TalimatGelisTarihi, LademeterMultiplier, VolumeMultiplier,
            CarHeight = DefaultCarHeight, yuk.YukTurKod, yuk.YuklemeUlke,
            yuk.BosaltmaUlke, yuk.CalismaSekli,
        });
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
                aracyuksekligi     = @CarHeight
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
