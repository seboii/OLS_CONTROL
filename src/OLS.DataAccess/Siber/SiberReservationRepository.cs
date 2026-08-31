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

    /// <summary>
    /// Yeni rezervasyonu, numarasını (max + 1) atomik biçimde atayarak INSERT eder ve
    /// atanan numarayı döner. Numara üretimiyle INSERT tek transaction+kilit altında
    /// yapılır — bkz. metodun XML açıklaması.
    /// </summary>
    Task<int> InsertRezervasyonWithLockedNumberAsync(
        SiberRezervasyonYaz rezervasyon, CancellationToken cancellationToken = default);

    Task UpdateRezervasyonAsync(SiberRezervasyonYaz rezervasyon, CancellationToken cancellationToken = default);

    /// <summary>
    /// Teklifi (rezervasyon) ve alt kayıtlarını Siber'den siler. Yük silinirken
    /// teklif de silindiği için gerekli — bkz. LoadTransferWriteService.DeleteAsync.
    /// </summary>
    Task DeleteRezervasyonAsync(string rezervasyonId, CancellationToken cancellationToken = default);

    Task<bool> YukKoliExistsAsync(string yukKoliId, CancellationToken cancellationToken = default);
    Task InsertRezervasyonYukKoliAsync(SiberRezervasyonYukKoli koli, CancellationToken cancellationToken = default);
    Task UpdateRezervasyonYukKoliAsync(SiberRezervasyonYukKoli koli, CancellationToken cancellationToken = default);

    Task<bool> TarifeExistsAsync(string tarifeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mali kalemin Siber'de gerçekten var olup olmadığı (<c>skn_kalem.kalemid</c>).
    /// Aktarım öncesi kontrol için: yoksa INSERT, FK_skn_rezervasyontarife_skn_kalem
    /// kısıtına takılıp işlenmemiş bir istisnaya ("beklenmeyen hata") dönüşüyordu.
    /// </summary>
    Task<bool> KalemExistsAsync(string kalemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Siber'e gönderilecek bir yabancı anahtarın hedef tabloda GERÇEKTEN var olup
    /// olmadığını söyler. Tablo/kolon adları çağıran koddaki SABİTLERDEN gelir
    /// (kullanıcı girdisi değildir) — bkz. SiberReferenceCheck.
    /// </summary>
    Task<bool> ReferenceExistsAsync(
        string table, string idColumn, string id, CancellationToken cancellationToken = default);

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
    /// <summary>
    /// Teklifin "Olumlu"ya çekildiği gün (skn_rezervasyon.onaytarih).
    /// Durum Olumlu değilken NULL yazılır.
    /// </summary>
    public DateTime? OnayTarih { get; init; }

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
    public string? KalemId { get; init; }
    public string? TasimaSekli { get; init; }

    /// <summary>
    /// Kalemin yönü: 1 = alış, 2 = satış. Siber'de tek tabloda İKİ AYRI sütun
    /// grubu var (alis*/satis*) ve yalnızca yöne karşılık gelen grup doldurulur;
    /// diğer grup 0/NULL bırakılır. Bkz. Siber Entegrasyon Raporu §5.1 adım 6.
    /// </summary>
    public int Buysell { get; init; }

    /// <summary>Yön ne olursa olsun aynı: döviz kodu, birim tutar, toplam tutar, cari.</summary>
    public string? DovizKod { get; init; }
    public decimal BirimTutar { get; init; }
    public decimal ToplamTutar { get; init; }
    public string? FirmaId { get; init; }
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

    /// <summary>
    /// olsold'da rezervasyon numarası kilitsiz <c>MAX(rezervasyonno) + 1</c> ile üretilir
    /// (Siber Entegrasyon Raporu risk #3): aynı anda iki "Sibere Aktar" çağrısı aynı
    /// numarayı okuyup ikisi de o numarayla INSERT deneyebilir. Burada numara üretimi
    /// ve INSERT <c>sp_getapplock</c> ile serileştirilmiş TEK transaction içinde
    /// yapılıyor — kilit adı sayacın kapsamıyla aynı: şirket + yıl
    /// ("skn_rezervasyon_no_{sirketid}_{yil}", bkz. gövdedeki numara kuralı
    /// açıklaması). <c>@LockOwner = 'Transaction'</c> kilidi COMMIT/ROLLBACK'te otomatik
    /// bırakır; bu, farklı bağlantılardan (uygulamanın birden fazla örneği olsa dahi)
    /// gelen eşzamanlı çağrıları da doğru sıraya sokar.
    /// </summary>
    public async Task<int> InsertRezervasyonWithLockedNumberAsync(
        SiberRezervasyonYaz r, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        const string sql = """
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;

            -- Kilit sayacın kapsamıyla AYNI olmalı (şirket+yıl): sayaç artık
            -- (sirketid, yil) bazında ilerlediği için genel bir kilit farklı
            -- şirket/yılları gereksiz yere sıraya sokardı.
            DECLARE @lockName NVARCHAR(100) =
                'skn_rezervasyon_no_' + CAST(@SirketId AS NVARCHAR(64)) + '_' + CAST(@Yil AS NVARCHAR(10));

            DECLARE @lockResult INT;
            EXEC @lockResult = sp_getapplock
                @Resource = @lockName, @LockMode = 'Exclusive',
                @LockOwner = 'Transaction', @LockTimeout = 15000;
            IF @lockResult < 0
            BEGIN
                ROLLBACK TRANSACTION;
                THROW 51000, 'Rezervasyon numarası kilidi alınamadı (zaman aşımı).', 1;
            END;

            -- Siber'in gerçek numara kuralı (18.937 kayıtta sıfır ihlalle doğrulandı):
            --     rezervasyonno = (yil % 100) * 100000 + rezervasyonnoint
            -- ve sayaç ŞİRKET + YIL bazında ilerler (benzersiz indeks de
            -- (sirketid, yil, rezervasyonno) üçlüsünde).
            --
            -- ASIL BELİRLEYİCİ ALAN rezervasyonnoint'tir: skn_rezervasyon üzerindeki
            -- [skn_rezervasyon_numaraupdate] tetikleyicisi, rezervasyonnoint her
            -- yazıldığında rezervasyonno'yu dbo.sbr_yukseferno_olustur ile YENİDEN
            -- ÜRETİR. Yani buradan gönderilen rezervasyonno'nun bir hükmü yok;
            -- doğru olması gereken rezervasyonnoint'tir.
            --
            -- BULUNAN GERÇEK HATA: eskiden sayaç, genel MAX(rezervasyonno)+1'den
            -- türetilip rezervasyonnoint = RIGHT(...,4) ile DÖRT haneye kırpılıyordu.
            -- 2026 sayacı 5 haneye çıkınca (15568) son 4 hane alınıp 5568 yazıldı,
            -- tetikleyici de numarayı 26|05568 = 2605568 olarak yeniden üretti —
            -- hem yanlış numara, hem de o numara zaten var olduğu için
            -- "duplicate key" hatası. Sayaç artık doğrudan rezervasyonnoint'ten,
            -- şirket+yıl kapsamında hesaplanıyor.
            DECLARE @nextNoInt INT = (
                SELECT ISNULL(MAX(rezervasyonnoint), 0)
                FROM skn_rezervasyon WHERE sirketid = @SirketId AND yil = @Yil) + 1;
            DECLARE @nextNo INT = (@Yil % 100) * 100000 + @nextNoInt;

            INSERT INTO skn_rezervasyon
                (rezervasyonid, sirketid, subeid, talimatgelissekli, rezervasyonno,
                 rezervasyonnoint, istenenromorkcins, isturu, yuklemetip, yukturkod,
                 pazarlamabildirimtarih, talimatgelistarih, gecerliliktarih, odemesekliid,
                 ontasimatarafimizdanyapilir, sontasimatarafimizdanyapilir, musteriid,
                 navlunfirmaid, gondericiid, aliciid, durumid, musteritemsilcisi,
                 satistemsilcisikod, departmanid, aciklama, yil, instime, insuser,
                 yuklemeulkeid, bosaltmaulkeid, calismasekli, onaytarih)
            VALUES
                (@RezervasyonId, @SirketId, @SubeId, @TalimatGelisSekli, @nextNo,
                 @nextNoInt, @IstenenRomorkCins, @IsTuru, @YuklemeTip, @YukTurKod,
                 @PazarlamaBildirimTarih, @TalimatGelisTarih, @GecerlilikTarih, @OdemeSekliId,
                 @OnTasimaTarafimizdanYapilir, @SonTasimaTarafimizdanYapilir, @MusteriId,
                 @NavlunFirmaId, @GondericiId, @AliciId, @DurumId, @MusteriTemsilcisi,
                 @SatisTemsilcisiKod, @DepartmanId, @Aciklama, @Yil, @InsTime, @InsUser,
                 @YuklemeUlkeId, @BosaltmaUlkeId, @CalismaSekli, @OnayTarih);

            COMMIT TRANSACTION;

            SELECT @nextNo;
            """;

        return await connection.QuerySingleAsync<int>(sql, Parameters(r));
    }

    public async Task DeleteRezervasyonAsync(
        string rezervasyonId, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        // Önce alt kayıtlar (koli + tarife), sonra rezervasyonun kendisi —
        // olsold LoadController.php satır 855-864 ile aynı sıra.
        await connection.ExecuteAsync("""
            DELETE FROM skn_rezervasyonyukkoli WHERE rezervasyonid = @id;
            DELETE FROM skn_rezervasyontarife  WHERE rezervasyonid = @id;
            DELETE FROM skn_rezervasyon        WHERE rezervasyonid = @id;
            """, new { id = rezervasyonId });
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
                calismasekli = @CalismaSekli, onaytarih = @OnayTarih
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

    public async Task<bool> ReferenceExistsAsync(
        string table, string idColumn, string id, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        var count = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM {table} WHERE {idColumn} = @id", new { id });

        return count > 0;
    }

    public async Task<bool> KalemExistsAsync(
        string kalemId, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM skn_kalem WHERE kalemid = @id", new { id = kalemId });

        return count > 0;
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

    /// <summary>
    /// Kalem SATIŞ ise (<c>buysell == 2</c>) satis* sütunları, aksi hâlde alis*
    /// sütunları doldurulur; karşı grup 0/NULL bırakılır. Siber Entegrasyon
    /// Raporu §5.1 adım 6. Daha önce yön dikkate alınmıyor, her kalem alış
    /// sütunlarına yazılıyordu — satış kalemleri Siber'de alış görünüyordu.
    /// KDV sütunları olsold'daki gibi sabit 0.
    /// </summary>
    public async Task InsertRezervasyonTarifeAsync(
        SiberRezervasyonTarife t, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        var sql = IsSale(t)
            ? """
              INSERT INTO skn_rezervasyontarife
                  (rezervasyontarifeid, rezervasyonid, tarih, miktar, satisdovizkod,
                   satisbirimtutar, satistoplamtutar, alistoplamtutar, kalemid, satisfirmaid,
                   tasimasekli, kdvoran, aliskdvoran)
              VALUES
                  (@RezervasyonTarifeId, @RezervasyonId, @Tarih, @Miktar, @DovizKod,
                   @BirimTutar, @ToplamTutar, 0, @KalemId, @FirmaId,
                   @TasimaSekli, 0, 0)
              """
            : """
              INSERT INTO skn_rezervasyontarife
                  (rezervasyontarifeid, rezervasyonid, tarih, miktar, alisdovizkod,
                   alisbirimtutar, alistoplamtutar, satistoplamtutar, kalemid, alisfirmaid,
                   tasimasekli, kdvoran, aliskdvoran)
              VALUES
                  (@RezervasyonTarifeId, @RezervasyonId, @Tarih, @Miktar, @DovizKod,
                   @BirimTutar, @ToplamTutar, 0, @KalemId, @FirmaId,
                   @TasimaSekli, 0, 0)
              """;

        await connection.ExecuteAsync(sql, t);
    }

    /// <summary>
    /// Yön değişmiş olabileceği için (kullanıcı kalemi Alış'tan Satış'a çevirdiyse)
    /// güncellemede KARŞI grup da sıfırlanır — aksi hâlde Siber'de kalemin hem alış
    /// hem satış tutarı dolu kalır.
    /// </summary>
    public async Task UpdateRezervasyonTarifeAsync(
        SiberRezervasyonTarife t, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        var sql = IsSale(t)
            ? """
              UPDATE skn_rezervasyontarife SET
                  tarih = @Tarih, miktar = @Miktar, satisdovizkod = @DovizKod,
                  satisbirimtutar = @BirimTutar, satistoplamtutar = @ToplamTutar,
                  satisfirmaid = @FirmaId,
                  alisdovizkod = NULL, alisbirimtutar = 0, alistoplamtutar = 0, alisfirmaid = NULL,
                  kalemid = @KalemId, tasimasekli = @TasimaSekli
              WHERE rezervasyontarifeid = @RezervasyonTarifeId
              """
            : """
              UPDATE skn_rezervasyontarife SET
                  tarih = @Tarih, miktar = @Miktar, alisdovizkod = @DovizKod,
                  alisbirimtutar = @BirimTutar, alistoplamtutar = @ToplamTutar,
                  alisfirmaid = @FirmaId,
                  satisdovizkod = NULL, satisbirimtutar = 0, satistoplamtutar = 0, satisfirmaid = NULL,
                  kalemid = @KalemId, tasimasekli = @TasimaSekli
              WHERE rezervasyontarifeid = @RezervasyonTarifeId
              """;

        await connection.ExecuteAsync(sql, t);
    }

    /// <summary>olsold buysell: 1 = alış, 2 = satış.</summary>
    private static bool IsSale(SiberRezervasyonTarife t) => t.Buysell == 2;

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
        r.YuklemeUlkeId, r.BosaltmaUlkeId, r.CalismaSekli, r.OnayTarih,
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
            -- uniqueidentifier -> string? okuması CAST ister (bkz. SiberLoadRepository).
            SELECT CAST(rezyukkoliid AS VARCHAR(64)) AS RezYukKoliId, kapadet AS KapAdet,
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
            SELECT CAST(rezervasyontarifeid AS VARCHAR(64)) AS RezervasyonTarifeId, miktar AS Miktar,
                   CAST(kalemid AS VARCHAR(64)) AS KalemId, tasimasekli AS TasimaSekli
            FROM skn_rezervasyontarife
            WHERE rezervasyonid = @id
            """,
            new { id = reservationId });

        return rows.ToList();
    }

}
