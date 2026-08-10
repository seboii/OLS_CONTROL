using Dapper;

namespace OLS.DataAccess.Siber;

/// <summary>
/// Siber tarafındaki araç tablosu (<c>skn_arac</c>).
/// olsold: <c>Front\Car\CarController</c> içindeki ham sqlsrv sorguları.
/// </summary>
public interface ISiberCarRepository
{
    bool IsConfigured { get; }

    /// <summary>Çakışmayan bir aracid üretir (olsold'daki do/while UUID döngüsü).</summary>
    Task<Guid> GenerateAracIdAsync(CancellationToken cancellationToken = default);

    Task InsertAracAsync(SiberArac arac, CancellationToken cancellationToken = default);
    Task UpdateAracAsync(SiberArac arac, CancellationToken cancellationToken = default);
}

/// <summary>skn_arac satırı. Alan adları Siber sütun adlarıdır.</summary>
public sealed class SiberArac
{
    public string AracId { get; init; } = string.Empty;
    public string? PlakaNo { get; init; }

    /// <summary>Referans tablolarının Siber "kod" değerleri (int).</summary>
    public int? AracTip { get; init; }
    public string? RomorkCins { get; init; }
    public int? AracSahip { get; init; }
    public int? AracDurum { get; init; }

    public string? BagliFirmaId { get; init; }
    public double? Km { get; init; }
    public double? En { get; init; }
    public double? Boy { get; init; }
    public double? Yukseklik { get; init; }
    public double? Kapasite { get; init; }
    public DateTime KayitGirisTarih { get; init; }
    public string? KayitGiren { get; init; }
}

public sealed class SiberCarRepository : ISiberCarRepository
{
    /// <summary>olsold'da sabit kodluydu (CarController::save).</summary>
    private const string SirketId = "BA4888B1-A2B0-4142-B273-92481D932EAD";
    private const string GrupSirketId = "7C62AB49-B7EC-435E-81DF-9BE2E57C59E4";

    private readonly ISiberConnectionFactory _factory;

    public SiberCarRepository(ISiberConnectionFactory factory) => _factory = factory;

    public bool IsConfigured => _factory.IsConfigured;

    public async Task<Guid> GenerateAracIdAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        while (true)
        {
            var candidate = Guid.NewGuid();

            var exists = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM skn_arac WHERE aracid = @aracid",
                new { aracid = candidate.ToString() });

            if (exists == 0)
                return candidate;
        }
    }

    public async Task InsertAracAsync(SiberArac arac, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        // yici / uluslararasi / aractur sabitleri olsold'dan birebir taşındı.
        const string sql = """
            INSERT INTO skn_arac
                (aracid, sirketid, grupsirketid, plakano, aractip, romorkcins, aracsahip,
                 aracdurum, baglifirmaid, km, yici, uluslararasi, en, boy, yukseklik,
                 kapasite, kayitgiristarih, kayitgiren, aractur)
            VALUES
                (@AracId, @SirketId, @GrupSirketId, @PlakaNo, @AracTip, @RomorkCins, @AracSahip,
                 @AracDurum, @BagliFirmaId, @Km, 1, 1, @En, @Boy, @Yukseklik,
                 @Kapasite, @KayitGirisTarih, @KayitGiren, 1)
            """;

        await connection.ExecuteAsync(sql, new
        {
            arac.AracId, SirketId, GrupSirketId, arac.PlakaNo, arac.AracTip, arac.RomorkCins,
            arac.AracSahip, arac.AracDurum, arac.BagliFirmaId, arac.Km, arac.En, arac.Boy,
            arac.Yukseklik, arac.Kapasite, arac.KayitGirisTarih, arac.KayitGiren,
        });
    }

    public async Task UpdateAracAsync(SiberArac arac, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        const string sql = """
            UPDATE skn_arac SET
                plakano = @PlakaNo, aractip = @AracTip, romorkcins = @RomorkCins,
                aracsahip = @AracSahip, aracdurum = @AracDurum, baglifirmaid = @BagliFirmaId,
                km = @Km, yici = 1, uluslararasi = 1, en = @En, boy = @Boy,
                yukseklik = @Yukseklik, kapasite = @Kapasite,
                kayitgiristarih = @KayitGirisTarih, kayitgiren = @KayitGiren, aractur = 1
            WHERE aracid = @AracId
            """;

        await connection.ExecuteAsync(sql, arac);
    }
}
