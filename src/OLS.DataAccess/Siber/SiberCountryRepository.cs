using Dapper;

namespace OLS.DataAccess.Siber;

/// <summary>
/// Bir Siber ülkesinin yük kaydına yazılan hâli: kimliği, ADI ve KITASI.
/// </summary>
public sealed record SiberCountryRow(string UlkeId, string? Name, string? Continent);

/// <summary>
/// Siber ülke tablosunu (<c>sbr_ulke</c>) okur.
///
/// NEDEN GEREKLİ: <c>skn_yuk</c>'ta ülke için FK'li bir kimlik sütunu YOKTUR —
/// canlıda doğrulandı, tablonun 400 sütunu arasında yalnızca çözülmüş metin
/// sütunları var: <c>_yuklemeulke</c> / <c>_bosaltmaulke</c> ülke ADINI,
/// <c>_yuklemekita</c> / <c>_bosaltmakita</c> ise KITA ADINI taşır. Dolu 7.486
/// satırın HİÇBİRİNDE GUID yok. Yani yükü Siber'e yazarken elimizdeki yerel
/// ülke kimliğini Siber'in kendi ADINA çevirmek zorundayız; ham GUID yazmak
/// kaydı Siber ekranında okunamaz hâle getiriyordu.
///
/// Kıta, ülkenin <c>kita</c> (tinyint) alanından <c>skn_sabittanim</c>'in
/// <c>KITA</c> grubuyla çözülür: 0 AFRİKA, 1 ASYA, 2 AVRUPA, 3 AMERİKA,
/// 4 AVUSTURALYA. Bu değerler Siber'in kendi tanım tablosundan okunur,
/// koda gömülmez.
/// </summary>
public interface ISiberCountryRepository
{
    /// <summary>
    /// Verilen Siber ülke kimlikleri için ad + kıta döner. Anahtar KÜÇÜK harfe
    /// çevrilmiş kimliktir (Siber <c>CAST</c>'i BÜYÜK, .NET küçük üretiyor).
    /// </summary>
    Task<IReadOnlyDictionary<string, SiberCountryRow>> GetAsync(
        IReadOnlyCollection<string> ulkeIds, CancellationToken cancellationToken = default);
}

public sealed class SiberCountryRepository : ISiberCountryRepository
{
    private readonly ISiberConnectionFactory _factory;

    public SiberCountryRepository(ISiberConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyDictionary<string, SiberCountryRow>> GetAsync(
        IReadOnlyCollection<string> ulkeIds, CancellationToken cancellationToken = default)
    {
        var ids = ulkeIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Where(id => Guid.TryParse(id, out _))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(Guid.Parse)
            .ToArray();

        if (ids.Length == 0)
            return new Dictionary<string, SiberCountryRow>(StringComparer.OrdinalIgnoreCase);

        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        // uniqueidentifier doğrudan string alana okunamıyor ("Object must implement
        // IConvertible"), bu yüzden CAST şart.
        var rows = await connection.QueryAsync<SiberCountryRow>(new CommandDefinition(
            """
            SELECT LOWER(CAST(u.ulkeid AS VARCHAR(64))) AS UlkeId,
                   CAST(u.ad AS NVARCHAR(200))          AS Name,
                   CAST(k.ad AS NVARCHAR(200))          AS Continent
            FROM sbr_ulke u
            LEFT JOIN skn_sabittanim k ON k.grupkod = 'KITA' AND k.kod = u.kita
            WHERE u.ulkeid IN @Ids
            """,
            new { Ids = ids },
            cancellationToken: cancellationToken));

        return rows.ToDictionary(r => r.UlkeId, r => r, StringComparer.OrdinalIgnoreCase);
    }
}
