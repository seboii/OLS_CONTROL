using Dapper;

namespace OLS.DataAccess.Siber;

/// <summary>
/// Siber tarafındaki yük–sefer eşleme tablosu (<c>skn_yukaktarma</c>) ve buna
/// bağlı <c>skn_yuk.pozisyonid</c> güncellemeleri.
/// olsold: <c>Front\ExpeditionLoadMapping\ExpeditionLoadMappingController</c>
///
/// Bir yük bir sefere eklendiğinde iki yazma olur: eşleme satırı eklenir ve
/// yükün pozisyon (sefer) alanı doldurulur. Silmede ikisi de geri alınır.
/// </summary>
public interface ISiberLoadMappingRepository
{
    bool IsConfigured { get; }

    /// <summary>Çakışmayan yeni bir <c>yukaktarmaid</c> üretir.</summary>
    Task<Guid> GenerateYukAktarmaIdAsync(CancellationToken cancellationToken = default);

    Task InsertYukAktarmaAsync(SiberYukAktarma mapping, CancellationToken cancellationToken = default);

    Task UpdateYukAktarmaAsync(SiberYukAktarma mapping, CancellationToken cancellationToken = default);

    Task DeleteYukAktarmaAsync(string yukAktarmaId, CancellationToken cancellationToken = default);

    /// <summary>Yükün bağlı olduğu pozisyonu (seferi) ayarlar; <c>null</c> ile bağı koparır.</summary>
    Task SetYukPozisyonAsync(
        string yukId, string? pozisyonId, CancellationToken cancellationToken = default);
}

public sealed class SiberYukAktarma
{
    public string YukAktarmaId { get; init; } = string.Empty;
    public int YuklemeBosaltma { get; init; } = 1;
    public string? YukId { get; init; }
    public string? PozisyonId { get; init; }
    public string? RomorkId { get; init; }
    public string? YerId { get; init; }
    public DateTime? Tarih { get; init; }
}

public sealed class SiberLoadMappingRepository : ISiberLoadMappingRepository
{
    private readonly ISiberConnectionFactory _factory;

    public SiberLoadMappingRepository(ISiberConnectionFactory factory) => _factory = factory;

    public bool IsConfigured => _factory.IsConfigured;

    public async Task<Guid> GenerateYukAktarmaIdAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        // Kaynak do/while ile çakışma kontrol ediyor; aynı davranış.
        while (true)
        {
            var candidate = Guid.NewGuid();

            var exists = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM skn_yukaktarma WHERE yukaktarmaid = @id",
                new { id = candidate.ToString() });

            if (exists == 0)
                return candidate;
        }
    }

    public async Task InsertYukAktarmaAsync(
        SiberYukAktarma mapping, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        await connection.ExecuteAsync(
            """
            INSERT INTO skn_yukaktarma
                (yukaktarmaid, yuklemebosaltma, yukid, pozisyonid, romorkid, yerid, tarih)
            VALUES
                (@YukAktarmaId, @YuklemeBosaltma, @YukId, @PozisyonId, @RomorkId, @YerId, @Tarih)
            """,
            mapping);
    }

    public async Task UpdateYukAktarmaAsync(
        SiberYukAktarma mapping, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        await connection.ExecuteAsync(
            """
            UPDATE skn_yukaktarma SET
                yuklemebosaltma = @YuklemeBosaltma,
                yukid           = @YukId,
                pozisyonid      = @PozisyonId,
                romorkid        = @RomorkId,
                yerid           = @YerId,
                tarih           = @Tarih
            WHERE yukaktarmaid = @YukAktarmaId
            """,
            mapping);
    }

    public async Task DeleteYukAktarmaAsync(
        string yukAktarmaId, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        await connection.ExecuteAsync(
            "DELETE FROM skn_yukaktarma WHERE yukaktarmaid = @id",
            new { id = yukAktarmaId });
    }

    public async Task SetYukPozisyonAsync(
        string yukId, string? pozisyonId, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        await connection.ExecuteAsync(
            "UPDATE skn_yuk SET pozisyonid = @pozisyonid WHERE yukid = @yukid",
            new { pozisyonid = pozisyonId, yukid = yukId });
    }
}
