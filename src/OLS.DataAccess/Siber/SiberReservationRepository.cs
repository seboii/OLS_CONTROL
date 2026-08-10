using Dapper;

namespace OLS.DataAccess.Siber;

/// <summary>
/// Teklifin Siber'deki karşılığı: <c>skn_rezervasyon</c> ve alt tabloları
/// <c>skn_rezervasyonyukkoli</c> (içerik) ile <c>skn_rezervasyontarife</c>
/// (finansal kalem).
///
/// olsold: <c>Front\TransferSiber\TransferSiberController::save</c>
/// </summary>
public interface ISiberReservationRepository
{
    bool IsConfigured { get; }

    Task<Guid> GenerateRezervasyonIdAsync(CancellationToken cancellationToken = default);
    Task<Guid> GenerateYukKoliIdAsync(CancellationToken cancellationToken = default);
    Task<Guid> GenerateTarifeIdAsync(CancellationToken cancellationToken = default);

    /// <summary>Sıradaki rezervasyon numarası (max + 1).</summary>
    Task<int> NextRezervasyonNoAsync(CancellationToken cancellationToken = default);

    Task InsertRezervasyonAsync(SiberRezervasyonYaz rezervasyon, CancellationToken cancellationToken = default);
    Task UpdateRezervasyonAsync(SiberRezervasyonYaz rezervasyon, CancellationToken cancellationToken = default);

    Task<bool> YukKoliExistsAsync(string yukKoliId, CancellationToken cancellationToken = default);
    Task InsertRezervasyonYukKoliAsync(SiberRezervasyonYukKoli koli, CancellationToken cancellationToken = default);
    Task UpdateRezervasyonYukKoliAsync(SiberRezervasyonYukKoli koli, CancellationToken cancellationToken = default);

    Task<bool> TarifeExistsAsync(string tarifeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rezervasyonun Siber'deki koli satırları — <c>update_siber_id</c>
    /// geriye dönük eşlemesi için okunur.
    /// </summary>
    Task<IReadOnlyList<SiberRezervasyonKoliSatir>> ReadReservationPackagesAsync(
        string reservationId, CancellationToken cancellationToken = default);

    /// <summary>Rezervasyonun Siber'deki tarife satırları.</summary>
    Task<IReadOnlyList<SiberRezervasyonTarifeSatir>> ReadReservationTariffsAsync(
        string reservationId, CancellationToken cancellationToken = default);
    Task InsertRezervasyonTarifeAsync(SiberRezervasyonTarife tarife, CancellationToken cancellationToken = default);
    Task UpdateRezervasyonTarifeAsync(SiberRezervasyonTarife tarife, CancellationToken cancellationToken = default);
}

/// <summary>Eşleme için okunan koli satırı.</summary>
public sealed class SiberRezervasyonKoliSatir
{
    public string? RezYukKoliId { get; init; }
    public int? KapAdet { get; init; }
    public decimal? En { get; init; }
    public decimal? Boy { get; init; }
    public decimal? Yukseklik { get; init; }
}

/// <summary>Eşleme için okunan tarife satırı.</summary>
public sealed class SiberRezervasyonTarifeSatir
{
    public string? RezervasyonTarifeId { get; init; }
    public decimal? Miktar { get; init; }
    public string? KalemId { get; init; }
    public string? TasimaSekli { get; init; }
}

/// <summary>skn_rezervasyon yazma modeli.</summary>
public sealed class SiberRezervasyonYaz
{
    public string RezervasyonId { get; init; } = string.Empty;
    public int RezervasyonNo { get; init; }
    public string? TalimatGelisSekli { get; init; }
    public string? IstenenRomorkCins { get; init; }
    public string? IsTuru { get; init; }
    public string? YuklemeTip { get; init; }
    public string? YukTurKod { get; init; }
    public DateTime? PazarlamaBildirimTarih { get; init; }
    public DateTime? TalimatGelisTarih { get; init; }
    public DateTime? GecerlilikTarih { get; init; }
    public string? OdemeSekliId { get; init; }
    public int? OnTasimaTarafimizdanYapilir { get; init; }
    public int? SonTasimaTarafimizdanYapilir { get; init; }
    public string? MusteriId { get; init; }
    public string? NavlunFirmaId { get; init; }
    public string? GondericiId { get; init; }
    public string? AliciId { get; init; }
    public string? DurumId { get; init; }
    public string? MusteriTemsilcisi { get; init; }
    public string? SatisTemsilcisiKod { get; init; }
    public string? DepartmanId { get; init; }
    public string? Aciklama { get; init; }
    public int Yil { get; init; }
    public string? YuklemeUlkeId { get; init; }
    public string? BosaltmaUlkeId { get; init; }
    public int? CalismaSekli { get; init; }
    public DateTime InsTime { get; init; }
    public string? InsUser { get; init; }
}

public sealed class SiberRezervasyonYukKoli
{
    public string RezYukKoliId { get; init; } = string.Empty;
    public string RezervasyonId { get; init; } = string.Empty;
    public int KapAdet { get; init; }
    public decimal En { get; init; }
    public decimal Boy { get; init; }
    public decimal Yukseklik { get; init; }
    public string? MalCinsId { get; init; }
    public string? KapId { get; init; }
    public string? TurkceTanim { get; init; }
    public decimal Hacim { get; init; }
    public decimal BurutAgirlik { get; init; }
    public decimal NetAgirlik { get; init; }
    public decimal Lademetre { get; init; }

    /// <summary>Ters mantık: stackable = 1 ise istiflenemez = 0.</summary>
    public int Istiflenemez { get; init; }
}

public sealed class SiberRezervasyonTarife
{
    public string RezervasyonTarifeId { get; init; } = string.Empty;
    public string RezervasyonId { get; init; } = string.Empty;
    public DateTime Tarih { get; init; }
    public decimal Miktar { get; init; }
    public string? AlisDovizKod { get; init; }
    public decimal AlisBirimTutar { get; init; }
    public decimal AlisToplamTutar { get; init; }
    public string? KalemId { get; init; }
    public string? AlisFirmaId { get; init; }
    public string? TasimaSekli { get; init; }
}

public sealed class SiberReservationRepository : ISiberReservationRepository
{
    private const string SirketId = "BA4888B1-A2B0-4142-B273-92481D932EAD";
    private const string SubeId = "69588E44-731B-46E5-83A4-A338816E2300";

    private readonly ISiberConnectionFactory _factory;

    public SiberReservationRepository(ISiberConnectionFactory factory) => _factory = factory;

    public bool IsConfigured => _factory.IsConfigured;

    public Task<Guid> GenerateRezervasyonIdAsync(CancellationToken cancellationToken = default) =>
        GenerateUniqueAsync("skn_rezervasyon", "rezervasyonid", cancellationToken);

    public Task<Guid> GenerateYukKoliIdAsync(CancellationToken cancellationToken = default) =>
        GenerateUniqueAsync("skn_rezervasyonyukkoli", "rezyukkoliid", cancellationToken);

    public Task<Guid> GenerateTarifeIdAsync(CancellationToken cancellationToken = default) =>
        GenerateUniqueAsync("skn_rezervasyontarife", "rezervasyontarifeid", cancellationToken);

    public async Task<int> NextRezervasyonNoAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        var max = await connection.ExecuteScalarAsync<int?>(
            "SELECT MAX(rezervasyonno) FROM skn_rezervasyon");

        return (max ?? 0) + 1;
    }

    public async Task InsertRezervasyonAsync(
        SiberRezervasyonYaz r, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        const string sql = """
            INSERT INTO skn_rezervasyon
                (rezervasyonid, sirketid, subeid, talimatgelissekli, rezervasyonno,
                 rezervasyonnoint, istenenromorkcins, isturu, yuklemetip, yukturkod,
                 pazarlamabildirimtarih, talimatgelistarih, gecerliliktarih, odemesekliid,
                 ontasimatarafimizdanyapilir, sontasimatarafimizdanyapilir, musteriid,
                 navlunfirmaid, gondericiid, aliciid, durumid, musteritemsilcisi,
                 satistemsilcisikod, departmanid, aciklama, yil, instime, insuser,
                 yuklemeulkeid, bosaltmaulkeid, calismasekli)
            VALUES
                (@RezervasyonId, @SirketId, @SubeId, @TalimatGelisSekli, @RezervasyonNo,
                 @RezervasyonNoInt, @IstenenRomorkCins, @IsTuru, @YuklemeTip, @YukTurKod,
                 @PazarlamaBildirimTarih, @TalimatGelisTarih, @GecerlilikTarih, @OdemeSekliId,
                 @OnTasimaTarafimizdanYapilir, @SonTasimaTarafimizdanYapilir, @MusteriId,
                 @NavlunFirmaId, @GondericiId, @AliciId, @DurumId, @MusteriTemsilcisi,
                 @SatisTemsilcisiKod, @DepartmanId, @Aciklama, @Yil, @InsTime, @InsUser,
                 @YuklemeUlkeId, @BosaltmaUlkeId, @CalismaSekli)
            """;

        await connection.ExecuteAsync(sql, Parameters(r));
    }

    public async Task UpdateRezervasyonAsync(
        SiberRezervasyonYaz r, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        const string sql = """
            UPDATE skn_rezervasyon SET
                talimatgelissekli = @TalimatGelisSekli, istenenromorkcins = @IstenenRomorkCins,
                isturu = @IsTuru, yuklemetip = @YuklemeTip, yukturkod = @YukTurKod,
                pazarlamabildirimtarih = @PazarlamaBildirimTarih,
                talimatgelistarih = @TalimatGelisTarih, gecerliliktarih = @GecerlilikTarih,
                odemesekliid = @OdemeSekliId,
                ontasimatarafimizdanyapilir = @OnTasimaTarafimizdanYapilir,
                sontasimatarafimizdanyapilir = @SonTasimaTarafimizdanYapilir,
                musteriid = @MusteriId, navlunfirmaid = @NavlunFirmaId,
                gondericiid = @GondericiId, aliciid = @AliciId, durumid = @DurumId,
                musteritemsilcisi = @MusteriTemsilcisi, satistemsilcisikod = @SatisTemsilcisiKod,
                departmanid = @DepartmanId, aciklama = @Aciklama, yil = @Yil,
                yuklemeulkeid = @YuklemeUlkeId, bosaltmaulkeid = @BosaltmaUlkeId,
                calismasekli = @CalismaSekli
            WHERE rezervasyonid = @RezervasyonId
            """;

        await connection.ExecuteAsync(sql, Parameters(r));
    }

    public async Task<bool> YukKoliExistsAsync(
        string yukKoliId, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM skn_rezervasyonyukkoli WHERE rezyukkoliid = @id",
            new { id = yukKoliId });

        return count > 0;
    }

    public async Task InsertRezervasyonYukKoliAsync(
        SiberRezervasyonYukKoli k, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        const string sql = """
            INSERT INTO skn_rezervasyonyukkoli
                (rezyukkoliid, rezervasyonid, kapadet, en, boy, yukseklik, malcinsid,
                 kapid, turkcetanim, hacim, burutagirlik, netagirlik, lademetre, istiflenemez)
            VALUES
                (@RezYukKoliId, @RezervasyonId, @KapAdet, @En, @Boy, @Yukseklik, @MalCinsId,
                 @KapId, @TurkceTanim, @Hacim, @BurutAgirlik, @NetAgirlik, @Lademetre, @Istiflenemez)
            """;

        await connection.ExecuteAsync(sql, k);
    }

    public async Task UpdateRezervasyonYukKoliAsync(
        SiberRezervasyonYukKoli k, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        const string sql = """
            UPDATE skn_rezervasyonyukkoli SET
                kapadet = @KapAdet, en = @En, boy = @Boy, yukseklik = @Yukseklik,
                malcinsid = @MalCinsId, kapid = @KapId, turkcetanim = @TurkceTanim,
                hacim = @Hacim, burutagirlik = @BurutAgirlik, netagirlik = @NetAgirlik,
                lademetre = @Lademetre, istiflenemez = @Istiflenemez
            WHERE rezyukkoliid = @RezYukKoliId
            """;

        await connection.ExecuteAsync(sql, k);
    }

    public async Task<bool> TarifeExistsAsync(
        string tarifeId, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM skn_rezervasyontarife WHERE rezervasyontarifeid = @id",
            new { id = tarifeId });

        return count > 0;
    }

    public async Task InsertRezervasyonTarifeAsync(
        SiberRezervasyonTarife t, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        // satistoplamtutar / kdvoran / aliskdvoran olsold'da sabit 0.
        const string sql = """
            INSERT INTO skn_rezervasyontarife
                (rezervasyontarifeid, rezervasyonid, tarih, miktar, alisdovizkod,
                 alisbirimtutar, alistoplamtutar, satistoplamtutar, kalemid, alisfirmaid,
                 tasimasekli, kdvoran, aliskdvoran)
            VALUES
                (@RezervasyonTarifeId, @RezervasyonId, @Tarih, @Miktar, @AlisDovizKod,
                 @AlisBirimTutar, @AlisToplamTutar, 0, @KalemId, @AlisFirmaId,
                 @TasimaSekli, 0, 0)
            """;

        await connection.ExecuteAsync(sql, t);
    }

    public async Task UpdateRezervasyonTarifeAsync(
        SiberRezervasyonTarife t, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        const string sql = """
            UPDATE skn_rezervasyontarife SET
                tarih = @Tarih, miktar = @Miktar, alisdovizkod = @AlisDovizKod,
                alisbirimtutar = @AlisBirimTutar, alistoplamtutar = @AlisToplamTutar,
                kalemid = @KalemId, alisfirmaid = @AlisFirmaId, tasimasekli = @TasimaSekli
            WHERE rezervasyontarifeid = @RezervasyonTarifeId
            """;

        await connection.ExecuteAsync(sql, t);
    }

    private static object Parameters(SiberRezervasyonYaz r) => new
    {
        r.RezervasyonId, SirketId, SubeId, r.TalimatGelisSekli, r.RezervasyonNo,
        // olsold: rezervasyon numarasının son 4 hanesi ayrı sütunda tutuluyor.
        RezervasyonNoInt = r.RezervasyonNo.ToString()[^Math.Min(4, r.RezervasyonNo.ToString().Length)..],
        r.IstenenRomorkCins, r.IsTuru, r.YuklemeTip, r.YukTurKod,
        r.PazarlamaBildirimTarih, r.TalimatGelisTarih, r.GecerlilikTarih, r.OdemeSekliId,
        r.OnTasimaTarafimizdanYapilir, r.SonTasimaTarafimizdanYapilir, r.MusteriId,
        r.NavlunFirmaId, r.GondericiId, r.AliciId, r.DurumId, r.MusteriTemsilcisi,
        r.SatisTemsilcisiKod, r.DepartmanId, r.Aciklama, r.Yil, r.InsTime, r.InsUser,
        r.YuklemeUlkeId, r.BosaltmaUlkeId, r.CalismaSekli,
    };

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

    public async Task<IReadOnlyList<SiberRezervasyonKoliSatir>> ReadReservationPackagesAsync(
        string reservationId, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        var rows = await connection.QueryAsync<SiberRezervasyonKoliSatir>(
            """
            SELECT rezyukkoliid AS RezYukKoliId, kapadet AS KapAdet,
                   en AS En, boy AS Boy, yukseklik AS Yukseklik
            FROM skn_rezervasyonyukkoli
            WHERE rezervasyonid = @id
            """,
            new { id = reservationId });

        return rows.ToList();
    }

    public async Task<IReadOnlyList<SiberRezervasyonTarifeSatir>> ReadReservationTariffsAsync(
        string reservationId, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        var rows = await connection.QueryAsync<SiberRezervasyonTarifeSatir>(
            """
            SELECT rezervasyontarifeid AS RezervasyonTarifeId, miktar AS Miktar,
                   kalemid AS KalemId, tasimasekli AS TasimaSekli
            FROM skn_rezervasyontarife
            WHERE rezervasyonid = @id
            """,
            new { id = reservationId });

        return rows.ToList();
    }

}
