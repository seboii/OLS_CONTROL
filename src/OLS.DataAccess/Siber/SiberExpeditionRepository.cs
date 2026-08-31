using Dapper;

namespace OLS.DataAccess.Siber;

/// <summary>
/// Siber tarafındaki sefer/pozisyon tabloları (<c>skn_sefer</c>, <c>skn_pozisyon</c>).
/// olsold: <c>Front\Expedition\ExpeditionController</c>
///
/// Sefer numarası üretimi Siber'de yapılır ve <c>max() + 1</c> mantığına dayanır.
/// Bu desen eşzamanlı iki kayıtta çakışabilir; kaynak koddaki davranış korunmuştur
/// ancak üretimde bir sayaç tablosuna taşınması önerilir.
/// </summary>
public interface ISiberExpeditionRepository
{
    bool IsConfigured { get; }

    /// <summary>Araç halihazırda kapanmamış bir seferde mi? (durumid 14 = boşaltıldı)</summary>
    Task<bool> IsCarOnActiveTripAsync(string carSiberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aynı yıl / iş türü / araç sahipliği için en son pozisyonun sefer numarasını verir.
    /// Sıradaki numara bu değerin içindeki sayıdan türetilir.
    /// </summary>
    Task<string?> FindLastSeferNoAsync(
        string year, string? workTypeCode, int carOwnerFlag, CancellationToken cancellationToken = default);

    /// <summary>Yıl + araç sahibi kodu + sefer numarası ile mevcut seferi bulur.</summary>
    Task<string?> FindSeferIdAsync(
        string year, string? ownerAdditionalCode, int seferNo, CancellationToken cancellationToken = default);

    Task<Guid> GeneratePozisyonIdAsync(CancellationToken cancellationToken = default);
    Task<Guid> GenerateSeferIdAsync(CancellationToken cancellationToken = default);

    Task<int> NextSiranoAsync(string seferId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Sefer numarasını ÜRETİR ve skn_sefer satırını TEK, KİLİTLİ işlemde ekler.
    ///
    /// Eski hâli iki ayrı adımdı (NextSeferNoAsync + InsertSeferAsync) ve numarayı
    /// <c>MAX(seferno) WHERE yil = @yil</c> ile buluyordu — yani YALNIZCA yıla göre.
    /// Canlıda doğrulandı: seferno sayacı (yıl, ARAÇ SAHİBİ) çiftine göre ayrı
    /// ilerliyor. 2026'da kiralık (KR) 1→516, özmal (OZ) 0→91. Yıl bazlı MAX her
    /// durumda 516 döndüğü için ÖZMAL bir araçla açılan sefer 92 yerine 517
    /// numarasını alıyor, sefer numarası 26OZ0092.. yerine 26OZ0517.. çıkıyor ve
    /// özmal sayacı kalıcı olarak bozuluyordu. Kiralıkta tesadüfen doğru
    /// çalıştığı için fark edilmemişti.
    ///
    /// Ayrıca kilitsiz MAX+1 iki eşzamanlı sefer açılışında aynı numarayı
    /// üretebiliyordu (rezervasyon ve yük numarasında düzeltilen aynı yarış
    /// durumu); numara üretimi ve INSERT artık sp_getapplock ile serileştirilmiş
    /// tek transaction içinde.
    /// </summary>
    Task<int> InsertSeferWithLockedNumberAsync(
        SiberSefer sefer, CancellationToken cancellationToken = default);


    Task InsertPozisyonAsync(SiberPozisyon pozisyon, CancellationToken cancellationToken = default);

    /// <summary>Eklenen pozisyonun Siber tarafından üretilen sefer numarasını okur.</summary>
    Task<string?> ReadPozisyonSeferNoAsync(string pozisyonId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Seferi (pozisyon) ve bağlı yük eşlemelerini Siber'den siler. Yalnızca yerelden
    /// silmek yetmiyordu: periyodik senkron kaydı bir sonraki turda skn_pozisyon'dan
    /// geri getiriyordu (bkz. LoadTransferWriteService.DeleteAsync'teki aynı not).
    /// </summary>
    Task DeletePozisyonAsync(string pozisyonId, CancellationToken cancellationToken = default);

    Task UpdatePozisyonAsync(
        string pozisyonId, int? durumId, string? romorkId, CancellationToken cancellationToken = default);
}

public sealed class SiberSefer
{
    public string SeferId { get; init; } = string.Empty;
    public int? AracSahip { get; init; }
    public int SeferNo { get; init; }
    public DateTime? CikisTarih { get; init; }
    public DateTime? DonusTarih { get; init; }
    public string Yil { get; init; } = string.Empty;
}

public sealed class SiberPozisyon
{
    public string PozisyonId { get; init; } = string.Empty;
    public string SeferId { get; init; } = string.Empty;
    public string? IsTuru { get; init; }
    public int Sirano { get; init; }
    public int DurumId { get; init; }
    public string? RomorkId { get; init; }
    public string? Hafta { get; init; }
    public string? DepartmanId { get; init; }
    public DateTime KayitGirisTarih { get; init; }

    /// <summary>expedition_types.code metin sütunu; olsold da kodu doğrudan yazıyordu.</summary>
    public string? SeferTurId { get; init; }

    public string? KayitGiren { get; init; }
}

public sealed class SiberExpeditionRepository : ISiberExpeditionRepository
{
    /// <summary>olsold'da sabit kodlu şirket/şube kimlikleri.</summary>
    private const string SirketId = "BA4888B1-A2B0-4142-B273-92481D932EAD";
    private const string SubeId = "69588E44-731B-46E5-83A4-A338816E2300";

    /// <summary>Siber'de "boşaltıldı" durumu; bu durumdaki sefer aktif sayılmaz.</summary>
    private const int UnloadedStatusId = 14;

    private readonly ISiberConnectionFactory _factory;

    public SiberExpeditionRepository(ISiberConnectionFactory factory) => _factory = factory;

    public bool IsConfigured => _factory.IsConfigured;

    public async Task<bool> IsCarOnActiveTripAsync(
        string carSiberId, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM skn_pozisyon WHERE romorkid = @romorkid AND durumid <> @unloaded",
            new { romorkid = carSiberId, unloaded = UnloadedStatusId });

        return count > 0;
    }

    public async Task<string?> FindLastSeferNoAsync(
        string year, string? workTypeCode, int carOwnerFlag, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<string?>(
            """
            SELECT TOP 1 seferno FROM skn_pozisyon
            WHERE haftayil = @year AND isturu = @isturu AND romorkaracsahip = @sahip
            ORDER BY id DESC
            """,
            new { year, isturu = workTypeCode, sahip = carOwnerFlag });
    }

    public async Task<string?> FindSeferIdAsync(
        string year, string? ownerAdditionalCode, int seferNo, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<string?>(
            "SELECT TOP 1 seferid FROM skn_sefer WHERE yil = @yil AND aracsahipad = @ad AND seferno = @no",
            new { yil = year, ad = ownerAdditionalCode, no = seferNo });
    }

    public Task<Guid> GeneratePozisyonIdAsync(CancellationToken cancellationToken = default) =>
        GenerateUniqueAsync("skn_pozisyon", "pozisyonid", cancellationToken);

    public Task<Guid> GenerateSeferIdAsync(CancellationToken cancellationToken = default) =>
        GenerateUniqueAsync("skn_sefer", "seferid", cancellationToken);

    public async Task<int> NextSiranoAsync(string seferId, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        var max = await connection.ExecuteScalarAsync<int?>(
            "SELECT MAX(sirano) FROM skn_pozisyon WHERE seferid = @seferid",
            new { seferid = seferId });

        return (max ?? 0) + 1;
    }

    public async Task<int> InsertSeferWithLockedNumberAsync(
        SiberSefer sefer, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

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
                THROW 51000, 'Sefer numarası kilidi alınamadı (zaman aşımı).', 1;
            END;

            -- Sayaç (yıl, araç sahibi) çiftine göre ayrı ilerler; bkz. arayüz açıklaması.
            DECLARE @nextNo INT = (
                SELECT ISNULL(MAX(seferno), 0)
                FROM skn_sefer
                WHERE yil = @Yil AND aracsahip = @AracSahip) + 1;

            INSERT INTO skn_sefer
                (seferid, sirketid, subeid, aracsahip, seferno, cikistarih, donustarih, yil, yici)
            VALUES
                (@SeferId, @SirketId, @SubeId, @AracSahip, @nextNo, @CikisTarih, @DonusTarih, @Yil, 0);

            COMMIT TRANSACTION;

            SELECT @nextNo;
            """;

        return await connection.QuerySingleAsync<int>(sql, new
        {
            sefer.SeferId, SirketId, SubeId, sefer.AracSahip,
            sefer.CikisTarih, sefer.DonusTarih, sefer.Yil,
            LockResource = $"skn_sefer_no:{sefer.Yil}:{sefer.AracSahip}",
        });
    }

    public async Task InsertPozisyonAsync(
        SiberPozisyon pozisyon, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        // seferno sütunu kasten yazılmıyor: Siber tarafında üretiliyor ve
        // ekleme sonrası geri okunuyor (olsold da böyle yapıyordu).
        const string sql = """
            INSERT INTO skn_pozisyon
                (pozisyonid, seferid, isturu, sirketid, subeid, sirano, durumid, romorkid,
                 hafta, departmanid, kayitgiristarih, seferturid, kayitgiren,
                 cektirmefirmaid, planlananbitistarih)
            VALUES
                (@PozisyonId, @SeferId, @IsTuru, @SirketId, @SubeId, @Sirano, @DurumId, @RomorkId,
                 @Hafta, @DepartmanId, @KayitGirisTarih, @SeferTurId, @KayitGiren,
                 NULL, NULL)
            """;

        await connection.ExecuteAsync(sql, new
        {
            pozisyon.PozisyonId, pozisyon.SeferId, pozisyon.IsTuru, SirketId, SubeId,
            pozisyon.Sirano, pozisyon.DurumId, pozisyon.RomorkId, pozisyon.Hafta,
            pozisyon.DepartmanId, pozisyon.KayitGirisTarih, pozisyon.SeferTurId, pozisyon.KayitGiren,
        });
    }

    public async Task<string?> ReadPozisyonSeferNoAsync(
        string pozisyonId, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<string?>(
            "SELECT TOP 1 seferno FROM skn_pozisyon WHERE pozisyonid = @id",
            new { id = pozisyonId });
    }

    public async Task DeletePozisyonAsync(
        string pozisyonId, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        // Önce sefere bağlı yük eşlemeleri, sonra pozisyonun kendisi.
        await connection.ExecuteAsync("""
            DELETE FROM skn_yukaktarma WHERE pozisyonid = @id;
            DELETE FROM skn_pozisyon   WHERE pozisyonid = @id;
            """, new { id = pozisyonId });
    }

    public async Task UpdatePozisyonAsync(
        string pozisyonId, int? durumId, string? romorkId, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        await connection.ExecuteAsync(
            "UPDATE skn_pozisyon SET durumid = @durumid, romorkid = @romorkid WHERE pozisyonid = @id",
            new { durumid = durumId, romorkid = romorkId, id = pozisyonId });
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
