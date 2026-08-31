using System.Data;
using Dapper;

namespace OLS.DataAccess.Siber;

/// <summary>
/// Uygulamadan açılan faturayı Siber'e (<c>sfy_gelirgider</c> +
/// <c>sfy_gelirgiderdetay</c>) yazar.
///
/// NUMARA ÜRETİMİ — yalnızca GELİR faturasında. Canlı veriden çıkarıldı:
/// gelir faturaları seri koduyla (DKT, UA, OGM, ASL…) numaralanıyor ve sayaç
/// (seri, YIL) çiftinde ilerliyor — UA serisi 2025'te 4→55, 2026'da 1→84.
/// Gider faturasında ise seri BOŞ ve numara tedarikçinin kendi fatura
/// numarasıdır; üretilmez, kullanıcıdan alınır.
///
/// Numara üretimi ve INSERT tek transaction içinde <c>sp_getapplock</c> ile
/// serileştirilir. Kilitsiz MAX+1 bu projede daha önce rezervasyon, yük ve
/// sefer numaralarında yarış durumu üretmişti.
///
/// ŞUBE ŞİRKETE BAĞLI: OLS ve AVRORA ayrı <c>subeid</c> kullanıyor. Projedeki
/// diğer yazıcılar OLS şubesini sabit yazıyor; finans Avrora'yı da kapsadığı
/// için burada şirkete göre seçilir.
///
/// <c>faturaintid</c> ve <c>kayitgiris_sirano</c> IDENTITY sütunlarıdır —
/// SQL Server üretir, INSERT'e yazılmaz.
/// </summary>
public interface ISiberInvoiceWriter
{
    bool IsConfigured { get; }

    Task<SiberInvoiceWriteResult> InsertAsync(
        SiberInvoiceInsert invoice, CancellationToken cancellationToken = default);
}

public sealed class SiberInvoiceInsert
{
    /// <summary>"C" gelir, "G" gider.</summary>
    public string Direction { get; init; } = "C";

    public string SirketId { get; init; } = string.Empty;

    /// <summary>Cari (sbr_firma.firmaid).</summary>
    public string FirmaId { get; init; } = string.Empty;

    public string? FirmaAd { get; init; }

    /// <summary>Gelir faturasında zorunlu; gider faturasında boş bırakılır.</summary>
    public string? SeriNo { get; init; }

    /// <summary>Gider faturasında tedarikçinin fatura numarası; gelirde yok sayılır.</summary>
    public string? FaturaNo { get; init; }

    public DateTime FaturaTarihi { get; init; }
    public DateTime? VadeTarihi { get; init; }

    public string DovizKod { get; init; } = "TL ";
    public double DovizKur { get; init; } = 1;

    public string? Aciklama { get; init; }
    public string? BelgeNo { get; init; }

    /// <summary>Bağlı yük/sefer kaydının Siber kimliği.</summary>
    public string? ModulId { get; init; }

    /// <summary>Yük iş türüne göre 0401-0404, sefer 0405.</summary>
    public string? ModulKod { get; init; }

    public string? KayitGiren { get; init; }

    public IReadOnlyList<SiberInvoiceLineInsert> Lines { get; init; } = [];
}

public sealed class SiberInvoiceLineInsert
{
    /// <summary>Mali kalem tanımı (skn_kalem.kalemid).</summary>
    public string KalemId { get; init; } = string.Empty;

    public double Miktar { get; init; } = 1;
    public double BirimFiyat { get; init; }
    public double KdvOran { get; init; }
    public string? Aciklama { get; init; }
}

public sealed record SiberInvoiceWriteResult(string GelirGiderId, string? FaturaNo);

public sealed class SiberInvoiceWriter : ISiberInvoiceWriter
{
    /// <summary>OLS şirketinin şubesi.</summary>
    private const string OlsSirketId = "ba4888b1-a2b0-4142-b273-92481d932ead";
    private const string OlsSubeId = "69588E44-731B-46E5-83A4-A338816E2300";

    /// <summary>AVRORA şirketinin şubesi.</summary>
    private const string AvroraSirketId = "46258a01-8d77-4f87-aaf5-6b331dedd8a7";
    private const string AvroraSubeId = "D019AE6E-3E81-47FF-8194-03C259C67013";

    private readonly ISiberConnectionFactory _factory;

    public SiberInvoiceWriter(ISiberConnectionFactory factory) => _factory = factory;

    public bool IsConfigured => _factory.IsConfigured;

    public async Task<SiberInvoiceWriteResult> InsertAsync(
        SiberInvoiceInsert invoice, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Siber bağlantısı yapılandırılmamış.");

        if (!Guid.TryParse(invoice.SirketId, out var sirketId))
            throw new ArgumentException("Şirket kimliği geçersiz.", nameof(invoice));

        if (!Guid.TryParse(invoice.FirmaId, out var firmaId))
            throw new ArgumentException("Cari kimliği geçersiz.", nameof(invoice));

        var isIncome = string.Equals(invoice.Direction, "C", StringComparison.OrdinalIgnoreCase);

        if (isIncome && string.IsNullOrWhiteSpace(invoice.SeriNo))
            throw new ArgumentException("Gelir faturası için seri kodu zorunlu.", nameof(invoice));

        if (!isIncome && string.IsNullOrWhiteSpace(invoice.FaturaNo))
            throw new ArgumentException("Gider faturası için fatura numarası zorunlu.", nameof(invoice));

        if (invoice.Lines.Count == 0)
            throw new ArgumentException("Fatura en az bir kalem içermeli.", nameof(invoice));

        var gelirGiderId = Guid.NewGuid();
        var subeId = Guid.Parse(BranchFor(invoice.SirketId));

        // Tutarlar burada hesaplanır, istemciden GELMEZ: istemcinin gönderdiği
        // toplam ile satırların toplamı ayrışırsa muhasebe kaydı sessizce yanlış
        // olur.
        var lines = invoice.Lines.Select((l, index) => new
        {
            DetayId = Guid.NewGuid(),
            KalemId = Guid.TryParse(l.KalemId, out var k) ? k : Guid.Empty,
            l.Miktar,
            l.BirimFiyat,
            l.KdvOran,
            l.Aciklama,
            Tutar = Math.Round(l.Miktar * l.BirimFiyat, 2),
            KdvTutar = Math.Round(l.Miktar * l.BirimFiyat * l.KdvOran / 100d, 2),
            Sira = index,
        }).ToList();

        var tutar = Math.Round(lines.Sum(l => l.Tutar), 2);
        var kdvTutar = Math.Round(lines.Sum(l => l.KdvTutar), 2);
        var toplam = Math.Round(tutar + kdvTutar, 2);
        var kur = invoice.DovizKur <= 0 ? 1d : invoice.DovizKur;

        using var connection = await _factory.CreateOpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            var faturaNo = invoice.FaturaNo;

            if (isIncome)
            {
                faturaNo = (await connection.QuerySingleAsync<int>(new CommandDefinition("""
                    DECLARE @lockResult INT;
                    EXEC @lockResult = sp_getapplock
                        @Resource = @LockResource, @LockMode = 'Exclusive',
                        @LockOwner = 'Transaction', @LockTimeout = 15000;
                    IF @lockResult < 0
                        THROW 51000, 'Fatura numarası kilidi alınamadı (zaman aşımı).', 1;

                    -- Sayaç (seri, yıl) çiftinde ilerler; bkz. sınıf açıklaması.
                    SELECT ISNULL(MAX(TRY_CAST(faturano AS BIGINT)), 0) + 1
                    FROM sfy_gelirgider
                    WHERE LTRIM(RTRIM(ISNULL(faturaserino,''))) = @SeriNo
                      AND YEAR(faturatarihi) = @Yil
                      AND LTRIM(RTRIM(ISNULL(gc,''))) = 'C';
                    """,
                    new
                    {
                        SeriNo = invoice.SeriNo!.Trim(),
                        Yil = invoice.FaturaTarihi.Year,
                        LockResource = $"sfy_gelirgider_no:{invoice.SeriNo!.Trim()}:{invoice.FaturaTarihi.Year}",
                    },
                    transaction, cancellationToken: cancellationToken))).ToString();
            }

            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO sfy_gelirgider
                    (gelirgiderid, sirketid, subeid, gc, faturaserino, faturano,
                     faturatarihi, vadetarihi, firmaid, firmaad,
                     dovizkod, dovizkur, tutar, kdvtutar, toplamtutar,
                     tutartl, kdvtutartl, toplamtutartl,
                     aciklama, belgeno, modulid, modulkod,
                     kayitgiristarih, kayitgiren)
                VALUES
                    (@GelirGiderId, @SirketId, @SubeId, @Gc, @SeriNo, @FaturaNo,
                     @FaturaTarihi, @VadeTarihi, @FirmaId, @FirmaAd,
                     @DovizKod, @DovizKur, @Tutar, @KdvTutar, @Toplam,
                     @TutarTl, @KdvTutarTl, @ToplamTl,
                     @Aciklama, @BelgeNo, @ModulId, @ModulKod,
                     @KayitGirisTarih, @KayitGiren);
                """,
                new
                {
                    GelirGiderId = gelirGiderId,
                    SirketId = sirketId,
                    SubeId = subeId,
                    Gc = isIncome ? "C" : "G",
                    SeriNo = isIncome ? invoice.SeriNo!.Trim() : null,
                    FaturaNo = faturaNo,
                    invoice.FaturaTarihi,
                    invoice.VadeTarihi,
                    FirmaId = firmaId,
                    invoice.FirmaAd,
                    invoice.DovizKod,
                    DovizKur = kur,
                    Tutar = tutar,
                    KdvTutar = kdvTutar,
                    Toplam = toplam,
                    // TL karşılıkları kurla çarpılarak yazılır; Siber raporları
                    // bu sütunlardan okuyor ve boş bırakılırsa fatura raporlarda
                    // sıfır tutarla görünüyor.
                    TutarTl = Math.Round(tutar * kur, 2),
                    KdvTutarTl = Math.Round(kdvTutar * kur, 2),
                    ToplamTl = Math.Round(toplam * kur, 2),
                    invoice.Aciklama,
                    invoice.BelgeNo,
                    ModulId = Guid.TryParse(invoice.ModulId, out var m) ? m : (Guid?)null,
                    invoice.ModulKod,
                    KayitGirisTarih = DateTime.Now,
                    invoice.KayitGiren,
                },
                transaction, cancellationToken: cancellationToken));

            foreach (var line in lines)
            {
                await connection.ExecuteAsync(new CommandDefinition("""
                    INSERT INTO sfy_gelirgiderdetay
                        (gelirgiderdetayid, gelirgiderid, gelirgider, firmaid, kalemid,
                         dovizkod, dovizkur, kdvoran, miktar, birimfiyat,
                         tutar, kdvtutar, tutartl, kdvtutartl,
                         aciklama, modulid, modulkod)
                    VALUES
                        (@DetayId, @GelirGiderId, @GelirGider, @FirmaId, @KalemId,
                         @DovizKod, @DovizKur, @KdvOran, @Miktar, @BirimFiyat,
                         @Tutar, @KdvTutar, @TutarTl, @KdvTutarTl,
                         @Aciklama, @ModulId, @ModulKod);
                    """,
                    new
                    {
                        line.DetayId,
                        GelirGiderId = gelirGiderId,
                        GelirGider = isIncome,
                        FirmaId = firmaId,
                        line.KalemId,
                        invoice.DovizKod,
                        DovizKur = kur,
                        line.KdvOran,
                        line.Miktar,
                        line.BirimFiyat,
                        line.Tutar,
                        line.KdvTutar,
                        TutarTl = Math.Round(line.Tutar * kur, 2),
                        KdvTutarTl = Math.Round(line.KdvTutar * kur, 2),
                        line.Aciklama,
                        ModulId = Guid.TryParse(invoice.ModulId, out var lm) ? lm : (Guid?)null,
                        invoice.ModulKod,
                    },
                    transaction, cancellationToken: cancellationToken));
            }

            transaction.Commit();

            return new SiberInvoiceWriteResult(
                gelirGiderId.ToString().ToLowerInvariant(), faturaNo);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static string BranchFor(string sirketId) =>
        string.Equals(sirketId, AvroraSirketId, StringComparison.OrdinalIgnoreCase)
            ? AvroraSubeId
            : OlsSubeId;

    /// <summary>Şirketi bilinmeyen çağrılar için OLS varsayılanı.</summary>
    public static string DefaultSirketId => OlsSirketId;
}
