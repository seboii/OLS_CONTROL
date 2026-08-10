using Dapper;

namespace OLS.DataAccess.Siber;

/// <summary>
/// Rezervasyonu (teklifi) gerçek yüke dönüştürme — Siber'de "Operasyona Bildir".
/// olsold: <c>TransferSiberController::loadSave</c>
///
/// Akış tamamen Siber'in kendi saklı yordamlarına dayanır; kaynak bunları
/// SQL Profiler çıktısından birebir almış:
/// <list type="number">
/// <item><c>sbr_rezervasyon_onay_kontrol</c> — rezervasyon onay kontrolü</item>
/// <item><c>skn_rezervazyon_yukac</c> — yükü açar (yazım hatası Siber'de böyle)</item>
/// <item><c>skn_rezervasyonyukbildir_tarifeaktar</c> — tarifeleri yeni yüke taşır</item>
/// <item><c>sbr_log</c> — iki denetim kaydı</item>
/// </list>
/// Ayrıca yeni yükün <c>kredilimitkontroluyapildi</c> alanı sıfırlanır.
///
/// Parametreler Dapper ile bağlanır — kaynak bunları dize birleştirmeyle
/// gönderiyordu (SQL enjeksiyonuna açık).
/// </summary>
public interface ISiberLoadReleaseRepository
{
    bool IsConfigured { get; }

    /// <summary>Rezervasyonun Siber'deki karşılığı; karşılaştırma için okunur.</summary>
    Task<SiberRezervasyonSnapshot?> FindRezervasyonAsync(
        string reservationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rezervasyonu yüke dönüştürür ve oluşan yükün kimlik/numarasını döner.
    /// </summary>
    Task<SiberReleasedLoad?> ReleaseAsync(
        string reservationId, string? siberUserCode, CancellationToken cancellationToken = default);
}

/// <summary>Karşılaştırılan alanların tamamı; hepsi metin olarak okunur.</summary>
public sealed class SiberRezervasyonSnapshot
{
    public string? RezervasyonId { get; init; }
    public string? IstenenRomorkCins { get; init; }
    public string? IsTuru { get; init; }
    public string? TalimatGelisSekli { get; init; }
    public string? YuklemeTip { get; init; }
    public string? YukTurKod { get; init; }
    public string? OdemeSekliId { get; init; }
    public string? OnTasimaTarafimizdanYapilir { get; init; }
    public string? SonTasimaTarafimizdanYapilir { get; init; }
    public string? MusteriId { get; init; }
    public string? NavlunFirmaId { get; init; }
    public string? GondericiId { get; init; }
    public string? AliciId { get; init; }
    public string? DurumId { get; init; }
    public string? DepartmanId { get; init; }
    public string? YuklemeUlkeId { get; init; }
    public string? BosaltmaUlkeId { get; init; }
    public string? CalismaSekli { get; init; }
}

public sealed record SiberReleasedLoad(string YukId, string? LoadNumber);

public sealed class SiberLoadReleaseRepository : ISiberLoadReleaseRepository
{
    private readonly ISiberConnectionFactory _factory;

    public SiberLoadReleaseRepository(ISiberConnectionFactory factory) => _factory = factory;

    public bool IsConfigured => _factory.IsConfigured;

    public async Task<SiberRezervasyonSnapshot?> FindRezervasyonAsync(
        string reservationId, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        return await connection.QueryFirstOrDefaultAsync<SiberRezervasyonSnapshot>(
            """
            SELECT TOP 1
                rezervasyonid               AS RezervasyonId,
                istenenromorkcins           AS IstenenRomorkCins,
                isturu                      AS IsTuru,
                talimatgelissekli           AS TalimatGelisSekli,
                yuklemetip                  AS YuklemeTip,
                yukturkod                   AS YukTurKod,
                odemesekliid                AS OdemeSekliId,
                ontasimatarafimizdanyapilir AS OnTasimaTarafimizdanYapilir,
                sontasimatarafimizdanyapilir AS SonTasimaTarafimizdanYapilir,
                musteriid                   AS MusteriId,
                navlunfirmaid               AS NavlunFirmaId,
                gondericiid                 AS GondericiId,
                aliciid                     AS AliciId,
                durumid                     AS DurumId,
                departmanid                 AS DepartmanId,
                yuklemeulkeid               AS YuklemeUlkeId,
                bosaltmaulkeid              AS BosaltmaUlkeId,
                calismasekli                AS CalismaSekli
            FROM skn_rezervasyon
            WHERE rezervasyonid = @id
            """,
            new { id = reservationId });
    }

    public async Task<SiberReleasedLoad?> ReleaseAsync(
        string reservationId, string? siberUserCode, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        // 1. Rezervasyon onay kontrolü. Kaynak dönüş değerini kullanmıyor
        //    (kontrol bloğu yorumda) — davranış korundu.
        await connection.ExecuteAsync(
            """
            DECLARE @mesajsor bit
            SET @mesajsor = 1
            EXEC sbr_rezervasyon_onay_kontrol @tip, @id, @mesajsor OUT
            """,
            new { tip = "K", id = reservationId });

        // 2. Yükü aç.
        await connection.ExecuteAsync(
            "EXEC skn_rezervazyon_yukac @id",
            new { id = reservationId });

        // 3. Oluşan yükü oku.
        var released = await connection.QueryFirstOrDefaultAsync<SiberReleasedLoad>(
            """
            SELECT TOP 1 yukid AS YukId, yuknoisturu AS LoadNumber
            FROM dbo.skn_yuk WITH (NOLOCK)
            WHERE rezervasyonid = @id
            ORDER BY yukno DESC
            """,
            new { id = reservationId });

        if (released is null)
            return null;

        // 4. Kredi limit kontrolü bayrağı.
        await connection.ExecuteAsync(
            "UPDATE skn_yuk SET kredilimitkontroluyapildi = 0 WHERE yukid = @yukid",
            new { yukid = released.YukId });

        // 5. Tarife aktarımı.
        await connection.ExecuteAsync(
            "EXEC skn_rezervasyonyukbildir_tarifeaktar @rez, @yuk, @kullanici",
            new { rez = reservationId, yuk = released.YukId, kullanici = siberUserCode });

        // 6. Denetim kayıtları.
        await connection.ExecuteAsync(
            """
            INSERT dbo.sbr_log
                (kullanici, tablename, tablerecordid, mastertablerecordid,
                 yapilanislem, findfieldvalue, islemmodul)
            SELECT dbo.sbr_program_username(), 'skn_rezervasyon', rezervasyonid, NULL,
                   2, rezervasyonno, 'Operasyona Bildir :' + yuknoisturu
            FROM dbo.skn_rezervasyon_view WHERE rezervasyonid = @id
            """,
            new { id = reservationId });

        await connection.ExecuteAsync(
            """
            INSERT dbo.sbr_log
                (kullanici, tablename, tablerecordid, mastertablerecordid,
                 yapilanislem, findfieldvalue, islemmodul)
            SELECT dbo.sbr_program_username(), 'skn_yuk', yukid, NULL,
                   1, yuknoisturu, 'Operasyona Bildir :' + rezervasyonno
            FROM dbo.skn_yuk_liste_v2 WHERE yukid = @yukid
            """,
            new { yukid = released.YukId });

        return released;
    }
}
