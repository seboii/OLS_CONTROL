using Dapper;

namespace OLS.DataAccess.Siber;

/// <summary>
/// Siber'in muhasebe/finans tablolarını (sfy_*) OKUR.
///
/// SENKRON STRATEJİSİ — kayan tarih penceresi, ekleme damgası değil.
/// Siber güncellemeleri damgalamıyor: <c>sfy_fisdetay.updtime</c> 214.954
/// satırın yalnızca 19'unda dolu. Ekleme zamanına göre artımlı çekmek, sonradan
/// DÜZELTİLEN kayıtları sessizce atlardı. Bu yüzden sorgular iş tarihine
/// (fiş tarihi / fatura tarihi / makbuz tarihi) göre filtrelenir ve senkron her
/// turda son N ayı yeniden çeker; düzeltmeler pratikte yakın geçmişte oluyor.
///
/// GUID: Siber CAST(x AS VARCHAR) ile BÜYÜK harf döndürür, .NET küçük harf
/// üretir ve PostgreSQL karşılaştırması harfe duyarlıdır. Tüm kimlikler
/// sorguda LOWER() ile küçültülür. Ayrıca uniqueidentifier doğrudan string'e
/// okunamaz ("Object must implement IConvertible") — CAST zorunlu.
/// </summary>
public interface ISiberFinanceRepository
{
    bool IsConfigured { get; }

    Task<IReadOnlyList<SiberAccountingPlanRow>> GetAccountingPlanAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SiberInvoiceRow>> GetInvoicesAsync(
        DateTime? since, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SiberInvoiceLineRow>> GetInvoiceLinesAsync(
        DateTime? since, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SiberPaymentRow>> GetPaymentsAsync(
        DateTime? since, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SiberVoucherRow>> GetVouchersAsync(
        DateTime? since, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SiberVoucherLineRow>> GetVoucherLinesAsync(
        DateTime? since, CancellationToken cancellationToken = default);
}

public sealed class SiberAccountingPlanRow
{
    public string HesapPlanId { get; init; } = string.Empty;
    public string? SirketId { get; init; }
    public string? HesapKod { get; init; }
    public string? Ad { get; init; }
    public string? Ad2 { get; init; }
    public byte? Seviye { get; init; }
    public bool? Pasif { get; init; }
}

public sealed class SiberInvoiceRow
{
    public string GelirGiderId { get; init; } = string.Empty;
    public string? SirketId { get; init; }
    public string? Gc { get; init; }
    public string? FaturaSeriNo { get; init; }
    public string? FaturaNo { get; init; }
    public DateTime? FaturaTarihi { get; init; }
    public DateTime? VadeTarihi { get; init; }
    public string? FirmaId { get; init; }
    public string? FirmaAd { get; init; }
    public string? DovizKod { get; init; }
    public double? DovizKur { get; init; }
    public double? Tutar { get; init; }
    public double? KdvTutar { get; init; }
    public double? ToplamTutar { get; init; }
    public double? TutarTl { get; init; }
    public double? KdvTutarTl { get; init; }
    public double? ToplamTutarTl { get; init; }
    public string? Aciklama { get; init; }
    public string? ModulId { get; init; }
    public string? ModulKod { get; init; }
    public string? BelgeNo { get; init; }
    public bool? Onay { get; init; }
    public DateTime? OnayTarih { get; init; }
    public DateTime? KayitGirisTarih { get; init; }
    public string? KayitGiren { get; init; }
}

public sealed class SiberInvoiceLineRow
{
    public string GelirGiderDetayId { get; init; } = string.Empty;
    public string GelirGiderId { get; init; } = string.Empty;
    public string? KalemId { get; init; }
    public string? KalemAd { get; init; }
    public double? Miktar { get; init; }
    public double? BirimFiyat { get; init; }
    public string? DovizKod { get; init; }
    public double? DovizKur { get; init; }
    public double? KdvOran { get; init; }
    public double? Tutar { get; init; }
    public double? KdvTutar { get; init; }
    public double? TutarTl { get; init; }
    public double? KdvTutarTl { get; init; }
    public string? Aciklama { get; init; }
    public string? BelgeNo { get; init; }
    public DateTime? BelgeTarih { get; init; }
}

public sealed class SiberPaymentRow
{
    public string TahsilatOdemeId { get; init; } = string.Empty;
    public string? SirketId { get; init; }
    public string? MakbuzNo { get; init; }
    public DateTime? MakbuzTarih { get; init; }
    public DateTime? VadeTarih { get; init; }
    public int? IslemTur { get; init; }
    public string? BorcId { get; init; }
    public string? BorcAd { get; init; }
    public string? BorcHesapKod { get; init; }
    public string? AlacakId { get; init; }
    public string? AlacakAd { get; init; }
    public string? AlacakHesapKod { get; init; }
    public string? DovizKod { get; init; }
    public double? DovizKur { get; init; }
    public double? Tutar { get; init; }
    public double? TutarTl { get; init; }
    public string? Aciklama { get; init; }
    public string? ModulId { get; init; }
    public string? ModulKod { get; init; }
    public DateTime? KayitGirisTarih { get; init; }
    public string? KayitGiren { get; init; }
}

public sealed class SiberVoucherRow
{
    public string FisId { get; init; } = string.Empty;
    public string? SirketId { get; init; }
    public byte? FisTur { get; init; }
    public DateTime? FisTarih { get; init; }
    public int? FisNo { get; init; }
    public int? YevmiyeNo { get; init; }
    public string? Aciklama { get; init; }
    public string? DovizTur { get; init; }
    public string? MuhasebeBelgeNo { get; init; }
    public DateTime? MuhasebeBelgeTarih { get; init; }
    public bool? KontrolEdildi { get; init; }
    public DateTime? KayitGirisTarih { get; init; }
    public string? InsUser { get; init; }
}

public sealed class SiberVoucherLineRow
{
    public string FisDetayId { get; init; } = string.Empty;
    public string FisId { get; init; } = string.Empty;
    public string? SirketId { get; init; }
    public string? HesapKod { get; init; }
    public double? Borc { get; init; }
    public double? Alacak { get; init; }
    public double? BorcDoviz { get; init; }
    public double? AlacakDoviz { get; init; }
    public string? DovizTur { get; init; }
    public double? DovizKur { get; init; }
    public string? Aciklama { get; init; }
    public string? KartoteksId { get; init; }
    public string? EntegreId { get; init; }
    public string? BelgeNo { get; init; }
    public DateTime? BelgeTarih { get; init; }
    public DateTime? VadeTarih { get; init; }
    public long? SiraNo { get; init; }
}

public sealed class SiberFinanceRepository : ISiberFinanceRepository
{
    private readonly ISiberConnectionFactory _factory;

    public SiberFinanceRepository(ISiberConnectionFactory factory) => _factory = factory;

    public bool IsConfigured => _factory.IsConfigured;

    public async Task<IReadOnlyList<SiberAccountingPlanRow>> GetAccountingPlanAsync(
        CancellationToken cancellationToken = default)
    {
        // Hesap planı 3.938 satır ve tarih sütunu yok — her turda tamamı çekilir.
        const string sql = """
            SELECT LOWER(CAST(hesapplanid AS VARCHAR(64))) AS HesapPlanId,
                   LOWER(CAST(sirketid    AS VARCHAR(64))) AS SirketId,
                   LTRIM(RTRIM(hesapkod))                  AS HesapKod,
                   ad AS Ad, ad2 AS Ad2, seviye AS Seviye, pasif AS Pasif
            FROM sfy_hesapplan
            WHERE LTRIM(RTRIM(ISNULL(hesapkod,''))) <> ''
            """;

        return await QueryAsync<SiberAccountingPlanRow>(sql, null, cancellationToken);
    }

    public async Task<IReadOnlyList<SiberInvoiceRow>> GetInvoicesAsync(
        DateTime? since, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT LOWER(CAST(g.gelirgiderid AS VARCHAR(64))) AS GelirGiderId,
                   LOWER(CAST(g.sirketid     AS VARCHAR(64))) AS SirketId,
                   LTRIM(RTRIM(g.gc))            AS Gc,
                   g.faturaserino                AS FaturaSeriNo,
                   g.faturano                    AS FaturaNo,
                   g.faturatarihi                AS FaturaTarihi,
                   g.vadetarihi                  AS VadeTarihi,
                   LOWER(CAST(g.firmaid AS VARCHAR(64))) AS FirmaId,
                   g.firmaad                     AS FirmaAd,
                   LTRIM(RTRIM(g.dovizkod))      AS DovizKod,
                   g.dovizkur                    AS DovizKur,
                   g.tutar                       AS Tutar,
                   g.kdvtutar                    AS KdvTutar,
                   g.toplamtutar                 AS ToplamTutar,
                   g.tutartl                     AS TutarTl,
                   g.kdvtutartl                  AS KdvTutarTl,
                   g.toplamtutartl               AS ToplamTutarTl,
                   g.aciklama                    AS Aciklama,
                   LOWER(CAST(g.modulid AS VARCHAR(64))) AS ModulId,
                   LTRIM(RTRIM(g.modulkod))      AS ModulKod,
                   g.belgeno                     AS BelgeNo,
                   g.onay                        AS Onay,
                   g.onaytarih                   AS OnayTarih,
                   g.kayitgiristarih             AS KayitGirisTarih,
                   g.kayitgiren                  AS KayitGiren
            FROM sfy_gelirgider g
            WHERE (@Since IS NULL OR g.faturatarihi >= @Since)
            """;

        return await QueryAsync<SiberInvoiceRow>(sql, since, cancellationToken);
    }

    public async Task<IReadOnlyList<SiberInvoiceLineRow>> GetInvoiceLinesAsync(
        DateTime? since, CancellationToken cancellationToken = default)
    {
        // Satırın kendi tarihi güvenilir değil; pencere BAŞLIĞIN fatura
        // tarihinden uygulanır ki satır ile başlık aynı kümede kalsın.
        const string sql = """
            SELECT LOWER(CAST(d.gelirgiderdetayid AS VARCHAR(64))) AS GelirGiderDetayId,
                   LOWER(CAST(d.gelirgiderid      AS VARCHAR(64))) AS GelirGiderId,
                   LOWER(CAST(d.kalemid           AS VARCHAR(64))) AS KalemId,
                   d.kalemyabanciad AS KalemAd,
                   d.miktar         AS Miktar,
                   d.birimfiyat     AS BirimFiyat,
                   LTRIM(RTRIM(d.dovizkod)) AS DovizKod,
                   d.dovizkur       AS DovizKur,
                   d.kdvoran        AS KdvOran,
                   d.tutar          AS Tutar,
                   d.kdvtutar       AS KdvTutar,
                   d.tutartl        AS TutarTl,
                   d.kdvtutartl     AS KdvTutarTl,
                   d.aciklama       AS Aciklama,
                   d.belgeno        AS BelgeNo,
                   d.belgetarih     AS BelgeTarih
            FROM sfy_gelirgiderdetay d
            JOIN sfy_gelirgider g ON g.gelirgiderid = d.gelirgiderid
            WHERE (@Since IS NULL OR g.faturatarihi >= @Since)
            """;

        return await QueryAsync<SiberInvoiceLineRow>(sql, since, cancellationToken);
    }

    public async Task<IReadOnlyList<SiberPaymentRow>> GetPaymentsAsync(
        DateTime? since, CancellationToken cancellationToken = default)
    {
        // ceksenetno/cekbanka taşınmıyor: 29.007 kaydın hiçbirinde dolu değil.
        const string sql = """
            SELECT LOWER(CAST(t.tahsilatodemeid AS VARCHAR(64))) AS TahsilatOdemeId,
                   LOWER(CAST(t.sirketid        AS VARCHAR(64))) AS SirketId,
                   t.makbuzno    AS MakbuzNo,
                   t.makbuztarih AS MakbuzTarih,
                   t.vadetarih   AS VadeTarih,
                   t.islemtur    AS IslemTur,
                   LOWER(CAST(t.borcid   AS VARCHAR(64))) AS BorcId,
                   t.borcad      AS BorcAd,
                   LTRIM(RTRIM(t.borchesapkod)) AS BorcHesapKod,
                   LOWER(CAST(t.alacakid AS VARCHAR(64))) AS AlacakId,
                   t.alacakad    AS AlacakAd,
                   LTRIM(RTRIM(t.alacakhesapkod)) AS AlacakHesapKod,
                   LTRIM(RTRIM(t.dovizkod)) AS DovizKod,
                   t.dovizkur    AS DovizKur,
                   t.tutar       AS Tutar,
                   t.tutartl     AS TutarTl,
                   t.aciklama    AS Aciklama,
                   LOWER(CAST(t.modulid AS VARCHAR(64))) AS ModulId,
                   LTRIM(RTRIM(t.modulkod)) AS ModulKod,
                   t.kayitgiristarih AS KayitGirisTarih,
                   t.kayitgiren  AS KayitGiren
            FROM sfy_tahsilatodeme t
            WHERE (@Since IS NULL OR t.makbuztarih >= @Since)
            """;

        return await QueryAsync<SiberPaymentRow>(sql, since, cancellationToken);
    }

    public async Task<IReadOnlyList<SiberVoucherRow>> GetVouchersAsync(
        DateTime? since, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT LOWER(CAST(f.fisid    AS VARCHAR(64))) AS FisId,
                   LOWER(CAST(f.sirketid AS VARCHAR(64))) AS SirketId,
                   f.fistur     AS FisTur,
                   f.fistarih   AS FisTarih,
                   f.fisno      AS FisNo,
                   f.yevmiyeno  AS YevmiyeNo,
                   f.aciklama   AS Aciklama,
                   LTRIM(RTRIM(f.doviztur)) AS DovizTur,
                   f.muhasebebelgeno    AS MuhasebeBelgeNo,
                   f.muhasebebelgetarih AS MuhasebeBelgeTarih,
                   f.kontroledildi      AS KontrolEdildi,
                   f.kayitgiristarih    AS KayitGirisTarih,
                   f.insuser            AS InsUser
            FROM sfy_fis f
            WHERE (@Since IS NULL OR f.fistarih >= @Since)
            """;

        return await QueryAsync<SiberVoucherRow>(sql, since, cancellationToken);
    }

    public async Task<IReadOnlyList<SiberVoucherLineRow>> GetVoucherLinesAsync(
        DateTime? since, CancellationToken cancellationToken = default)
    {
        // Cari ekstrenin kaynağı. kartoteksid = cari, entegreid = kaynak belge
        // (fatura ya da tahsilat). Pencere fişin tarihinden uygulanır.
        const string sql = """
            SELECT LOWER(CAST(d.fisdetayid AS VARCHAR(64))) AS FisDetayId,
                   LOWER(CAST(d.fisid      AS VARCHAR(64))) AS FisId,
                   LOWER(CAST(d.sirketid   AS VARCHAR(64))) AS SirketId,
                   LTRIM(RTRIM(d.hesapkod)) AS HesapKod,
                   d.borc        AS Borc,
                   d.alacak      AS Alacak,
                   d.borcdoviz   AS BorcDoviz,
                   d.alacakdoviz AS AlacakDoviz,
                   LTRIM(RTRIM(d.doviztur)) AS DovizTur,
                   d.dovizkur    AS DovizKur,
                   d.aciklama    AS Aciklama,
                   LOWER(CAST(d.kartoteksid AS VARCHAR(64))) AS KartoteksId,
                   LOWER(CAST(d.entegreid   AS VARCHAR(64))) AS EntegreId,
                   d.belgeno     AS BelgeNo,
                   d.belgetarih  AS BelgeTarih,
                   d.vadetarih   AS VadeTarih,
                   d.sirano      AS SiraNo
            FROM sfy_fisdetay d
            JOIN sfy_fis f ON f.fisid = d.fisid
            WHERE (@Since IS NULL OR f.fistarih >= @Since)
            """;

        return await QueryAsync<SiberVoucherLineRow>(sql, since, cancellationToken);
    }

    private async Task<IReadOnlyList<T>> QueryAsync<T>(
        string sql, DateTime? since, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            return [];

        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        var rows = await connection.QueryAsync<T>(new CommandDefinition(
            sql, new { Since = since },
            commandTimeout: 300,
            cancellationToken: cancellationToken));

        return rows.ToList();
    }
}
