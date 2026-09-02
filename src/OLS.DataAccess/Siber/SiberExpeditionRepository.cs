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
    Task<SiberSeferRef?> FindSeferAsync(
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

    /// <summary>
    /// Sefer güncellemesini Siber'e yansıtır. <paramref name="pozisyon"/>
    /// içindeki güzergâh ve tarih alanları da yazılır — eskiden yalnızca durum
    /// ve römork gidiyordu, kullanıcının girdiği şehirler/tarihler Siber'e
    /// HİÇ ulaşmıyordu.
    /// </summary>
    Task UpdatePozisyonAsync(SiberPozisyon pozisyon, CancellationToken cancellationToken = default);
}

/// <summary>Var olan bir seferin, yeniden kullanılabilirliğini belirleyen alanları.</summary>
public sealed class SiberSeferRef
{
    public string SeferId { get; init; } = string.Empty;
    public string? RomorkId { get; init; }
    public int? AracSahip { get; init; }

    /// <summary>
    /// Bu sefere, verilen iş türü ve römorkla yeni bir pozisyon eklenebilir mi?
    /// <c>skn_pozisyon_seferromorkkontrol_tr</c> yalnızca EX/IM (isturu 0,1) ve
    /// özmal/sözleşmeli kiralık (aracsahip 0,2) seferlerde römork eşitliği arar.
    /// </summary>
    public bool AcceptsPosition(string? isTuruCode, string? romorkId) =>
        !(isTuruCode is "0" or "1"
          && AracSahip is 0 or 2
          && !string.Equals(RomorkId ?? string.Empty, romorkId ?? string.Empty,
                            StringComparison.OrdinalIgnoreCase));
}

public sealed class SiberSefer
{
    /// <summary>Seferi açan kullanıcının şirketi; şube bundan türer.</summary>
    public string? SirketId { get; init; }

    public string SeferId { get; init; } = string.Empty;
    public int? AracSahip { get; init; }
    public int SeferNo { get; init; }
    public DateTime? CikisTarih { get; init; }
    public DateTime? DonusTarih { get; init; }
    public string Yil { get; init; } = string.Empty;

    /// <summary>
    /// Seferin römorku. ÖZMAL VE SÖZLEŞMELİ KİRALIK SEFERLERDE ZORUNLU:
    /// <c>skn_pozisyon_seferromorkkontrol_tr</c> trigger'ı, iş türü EX/IM (0,1)
    /// ve araç sahibi 0/2 olan seferlerde <c>skn_pozisyon.romorkid</c> ile
    /// <c>skn_sefer.romorkid</c>'nin AYNI olmasını şart koşuyor; aksi hâlde
    /// pozisyon INSERT'i "EX,IM Seferlerde sefer romork bilgisi pozisyondan
    /// farklı olamaz!" ile ROLLBACK ediliyordu. Siber'in kendi verisi de bunu
    /// doğruluyor: 154 özmal/sözleşmeli seferin 148'inde dolu, 2.874 kiralık
    /// seferin yalnızca 1'inde. Bu yüzden yalnızca 0/2 için yazılır — kiralık
    /// seferde birden çok römork olabildiği için orada boş bırakmak DOĞRU.
    /// </summary>
    public string? RomorkId { get; init; }
}

public sealed class SiberPozisyon
{
    /// <summary>Seferi açan kullanıcının şirketi; şube bundan türer.</summary>
    public string? SirketId { get; init; }

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

    /// <summary>
    /// GÜZERGÂH VE TARİHLER. Sefer formu bu yedi alanı zaten topluyordu ama
    /// HİÇBİRİ Siber'e gitmiyordu — kayıt Siber'de güzergâhsız ve tarihsiz
    /// açılıyordu. Siber'in kendi verisinde hepsi yoğun kullanılıyor (4.398
    /// pozisyonda: başlangıç şehri %72,9 · yükleme şehri %72,8 · bitiş şehri
    /// %72,0 · çıkış %73,1 · dönüş %72,1 · yükleme tarihi %76,9 · araç çıkış
    /// %69,0).
    ///
    /// Şehir kimlikleri <c>sbr_sehir</c>'in KENDİ kimlikleridir; başlangıç ve
    /// bitiş FK'lidir (<c>FK_skn_pozisyon_sbr_sehir…</c>), yani karşılığı
    /// olmayan değer INSERT'i düşürür.
    ///
    /// Tarihler ayrıca <c>skn_pozisyon_sefer_update</c> trigger'ını besliyor:
    /// hafta/ay/yıl alanları COALESCE(çıkış, araç çıkış, yükleme, kayıt) ile
    /// türetiliyor. Boş bırakıldığında sefer "kayıt tarihi" haftasına düşüyordu.
    /// </summary>
    public string? BaslangicSehirId { get; init; }
    public string? YuklemeSehirId { get; init; }
    public string? BitisSehirId { get; init; }
    public DateTime? CikisTarih { get; init; }
    public DateTime? DonusTarih { get; init; }
    public DateTime? YuklemeTarih { get; init; }
    public DateTime? AracCikisTarih { get; init; }
}

public sealed class SiberExpeditionRepository : ISiberExpeditionRepository
{
    /// <summary>
    /// BULUNAN GERÇEK HATA — Avrora kullanıcısı açtığı seferi GÖREMİYORDU.
    ///
    /// Şirket ve şube burada SABİT yazılıyordu (olsold'dan taşınmış): her sefer,
    /// kimin açtığından bağımsız olarak OLS şirketine ve OLS şubesine düşüyordu.
    /// Görünürlük kuralı şirket kapsamına dayandığı için (Avrora ekibi yalnızca
    /// Avrora kayıtlarını görür) Avrora kullanıcısının açtığı sefer kendi
    /// listesinde HİÇ görünmüyordu.
    ///
    /// Siber'in kendi verisi iki şirketi de kullanıyor: 4.120 pozisyon OLS,
    /// 279'u AVRORA — ve şube şirketle birebir örtüşüyor (4.120 / 279).
    /// Yük akışı bunu zaten doğru yapıyordu (bkz. DirectLoadService), sefer
    /// akışı yapmıyordu.
    /// </summary>
    private static string SirketIdOr(string? sirketId) =>
        string.IsNullOrWhiteSpace(sirketId) ? SiberLoadRepository.DefaultSirketId : sirketId;

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

    /// <summary>
    /// BULUNAN GERÇEK HATA — sefer oluşturma HER SEFERİNDE "beklenmedik hata"
    /// veriyordu. <c>skn_sefer.seferid</c> <c>uniqueidentifier</c>; Dapper bunu
    /// doğrudan <c>string?</c>'a okuyamıyor ve
    /// <c>InvalidCastException: Object must implement IConvertible</c> fırlatıyor
    /// (proje kuralı: uniqueidentifier okurken HER ZAMAN CAST). Sorgu satır
    /// DÖNMEDİĞİNDE null geldiği için hata yalnızca aranan sefer gerçekten
    /// VARKEN çıkıyordu — yani o yıl/araç sahibi için ilk seferden sonra her
    /// defasında.
    ///
    /// Artık yalnızca kimlik değil, seferin ARAÇ SAHİBİ ve RÖMORKU da dönüyor:
    /// çağıran, römork trigger'ının (bkz. <see cref="SiberSefer.RomorkId"/>)
    /// reddedeceği bir seferi yeniden kullanmamalı.
    /// </summary>
    public async Task<SiberSeferRef?> FindSeferAsync(
        string year, string? ownerAdditionalCode, int seferNo, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<SiberSeferRef>(new CommandDefinition(
            """
            SELECT TOP 1
                   CAST(seferid AS VARCHAR(64))  AS SeferId,
                   CAST(romorkid AS VARCHAR(64)) AS RomorkId,
                   aracsahip                     AS AracSahip
            FROM skn_sefer
            WHERE yil = @yil AND aracsahipad = @ad AND seferno = @no
            """,
            new { yil = year, ad = ownerAdditionalCode, no = seferNo },
            cancellationToken: cancellationToken));
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
                (seferid, sirketid, subeid, aracsahip, seferno, cikistarih, donustarih, yil, yici,
                 romorkid)
            VALUES
                (@SeferId, @SirketId, @SubeId, @AracSahip, @nextNo, @CikisTarih, @DonusTarih, @Yil, 0,
                 CASE WHEN @AracSahip IN (0, 2) THEN @RomorkId END);

            COMMIT TRANSACTION;

            SELECT @nextNo;
            """;

        return await connection.QuerySingleAsync<int>(sql, new
        {
            sefer.SeferId,
            SirketId = SirketIdOr(sefer.SirketId),
            SubeId = SiberLoadRepository.SubeIdFor(sefer.SirketId),
            sefer.AracSahip,
            sefer.CikisTarih, sefer.DonusTarih, sefer.Yil, sefer.RomorkId,
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
                 cektirmefirmaid, planlananbitistarih,
                 baslangicsehirid, yuklemesehirid, bitissehirid,
                 cikistarih, donustarih, yuklemetarih, araccikistarih)
            VALUES
                (@PozisyonId, @SeferId, @IsTuru, @SirketId, @SubeId, @Sirano, @DurumId, @RomorkId,
                 @Hafta, @DepartmanId, @KayitGirisTarih, @SeferTurId, @KayitGiren,
                 NULL, NULL,
                 @BaslangicSehirId, @YuklemeSehirId, @BitisSehirId,
                 @CikisTarih, @DonusTarih, @YuklemeTarih, @AracCikisTarih)
            """;

        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            pozisyon.PozisyonId, pozisyon.SeferId, pozisyon.IsTuru,
            SirketId = SirketIdOr(pozisyon.SirketId),
            SubeId = SiberLoadRepository.SubeIdFor(pozisyon.SirketId),
            pozisyon.Sirano, pozisyon.DurumId, pozisyon.RomorkId, pozisyon.Hafta,
            pozisyon.DepartmanId, pozisyon.KayitGirisTarih, pozisyon.SeferTurId, pozisyon.KayitGiren,
            pozisyon.BaslangicSehirId, pozisyon.YuklemeSehirId, pozisyon.BitisSehirId,
            pozisyon.CikisTarih, pozisyon.DonusTarih, pozisyon.YuklemeTarih, pozisyon.AracCikisTarih,
        }, cancellationToken: cancellationToken));
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

    /// <summary>
    /// Pozisyonun durumunu ve römorkunu günceller.
    ///
    /// RÖMORK DEĞİŞİNCE SEFER DE HİZALANIR. Aynı trigger
    /// (<c>skn_pozisyon_seferromorkkontrol_tr</c>) UPDATE'te de çalışıyor: EX/IM
    /// ve özmal/sözleşmeli kiralık bir seferde pozisyonun römorkunu değiştirmek,
    /// sefer römorku eski değerde kaldığı için reddediliyordu. Sefer önce
    /// güncelleniyor, pozisyon sonra — trigger pozisyon yazılırken bakıyor.
    ///
    /// YALNIZCA TEK POZİSYONLU SEFERDE hizalanır. Sefere başka pozisyonlar da
    /// bağlıysa seferin römorkunu değiştirmek onları da sessizce yanlış duruma
    /// düşürürdü; o durumda Siber'in kendi kuralı devrede kalır ve işlem
    /// anlaşılır bir mesajla reddedilir.
    /// </summary>
    public async Task UpdatePozisyonAsync(
        SiberPozisyon pozisyon, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE s SET s.romorkid = @RomorkId
            FROM skn_sefer s
            JOIN skn_pozisyon p ON p.seferid = s.seferid
            WHERE p.pozisyonid = @PozisyonId
              AND s.aracsahip IN (0, 2)
              AND p.isturu IN (0, 1)
              AND (SELECT COUNT(*) FROM skn_pozisyon x WHERE x.seferid = s.seferid) = 1;

            UPDATE skn_pozisyon SET
                durumid          = @DurumId,
                romorkid         = @RomorkId,
                isturu           = ISNULL(@IsTuru, isturu),
                departmanid      = ISNULL(@DepartmanId, departmanid),
                seferturid       = ISNULL(@SeferTurId, seferturid),
                baslangicsehirid = @BaslangicSehirId,
                yuklemesehirid   = @YuklemeSehirId,
                bitissehirid     = @BitisSehirId,
                cikistarih       = @CikisTarih,
                donustarih       = @DonusTarih,
                yuklemetarih     = @YuklemeTarih,
                araccikistarih   = @AracCikisTarih
            WHERE pozisyonid = @PozisyonId;
            """,
            new
            {
                pozisyon.PozisyonId, pozisyon.DurumId, pozisyon.RomorkId, pozisyon.IsTuru,
                pozisyon.DepartmanId, pozisyon.SeferTurId,
                pozisyon.BaslangicSehirId, pozisyon.YuklemeSehirId, pozisyon.BitisSehirId,
                pozisyon.CikisTarih, pozisyon.DonusTarih, pozisyon.YuklemeTarih,
                pozisyon.AracCikisTarih,
            },
            cancellationToken: cancellationToken));
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
