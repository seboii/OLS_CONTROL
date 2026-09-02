using Dapper;

namespace OLS.DataAccess.Siber;

/// <summary>
/// Yazımdan önce doğrulanabilen Siber referans tabloları.
///
/// Tablo ve kolon adları SQL'e dizge olarak giriyor; bu yüzden serbest metin
/// DEĞİL, kapalı bir liste. Yeni bir Siber yazımı eklenirken dokunulan her
/// referans buraya da eklenmeli.
/// </summary>
public enum SiberReferenceTable
{
    /// <summary>
    /// <c>skn_sabittanim</c> — iş türü, yükleme tipi, yük türü, talimat geliş
    /// şekli ve römork cinsi hepsi bu tek tabloda <c>grupkod</c> ile ayrılıyor.
    /// </summary>
    SabitTanim,

    /// <summary><c>sbr_departman</c> — teklif, yük ve seferde FK'li.</summary>
    Departman,

    /// <summary><c>sbr_odemesekli</c>.</summary>
    OdemeSekli,

    /// <summary>
    /// <c>sbr_firma</c> — müşteri / gönderici / alıcı / acente.
    /// <c>skn_yuk</c>'ta üçü de FK'li.
    /// </summary>
    Firma,

    /// <summary>
    /// <c>skn_arac</c> — sefer aracı. <c>skn_pozisyon.romorkid</c> hem FK'li hem
    /// NOT NULL, yani karşılığı olmayan araç INSERT'i kesin düşürür.
    /// </summary>
    Arac,

    /// <summary><c>skn_kapcins</c> — koli kap tipi, <c>skn_yukkoli.kapid</c> FK'li.</summary>
    KapCins,

    /// <summary><c>skn_kalem</c> — mali kalem.</summary>
    Kalem,
}

/// <summary>
/// Siber'de bir referans kaydının GERÇEKTEN var olup olmadığını sorar.
///
/// NEDEN AYRI BİR DEPO: aynı soruyu üç akış da soruyor (teklif, yük, sefer) ve
/// eskiden üçü ayrı ayrı, farklı sıkılıkta soruyordu — teklif beş referansı,
/// yük yalnızca tanımları, sefer hiçbirini. Siber'e yazım GERİ ALINAMADIĞI için
/// bu fark doğrudan riskti: yazım yarıda kalınca yerel işlem geri alınıyor ama
/// Siber'deki kayıt kalıyor.
/// </summary>
public interface ISiberReferenceRepository
{
    /// <summary>
    /// Verilen kimliklerden Siber'de BULUNMAYANLARI döner. Tablo başına tek
    /// sorgu; boş liste "hepsi var" demektir.
    /// </summary>
    Task<IReadOnlyList<string>> FindMissingAsync(
        SiberReferenceTable table, IReadOnlyCollection<string> ids,
        CancellationToken cancellationToken = default);
}

public sealed class SiberReferenceRepository : ISiberReferenceRepository
{
    private readonly ISiberConnectionFactory _factory;

    public SiberReferenceRepository(ISiberConnectionFactory factory) => _factory = factory;

    private static (string Table, string KeyColumn) Target(SiberReferenceTable table) => table switch
    {
        SiberReferenceTable.SabitTanim => ("skn_sabittanim", "sabittanimid"),
        SiberReferenceTable.Departman => ("sbr_departman", "departmanid"),
        SiberReferenceTable.OdemeSekli => ("sbr_odemesekli", "odemesekliid"),
        SiberReferenceTable.Firma => ("sbr_firma", "firmaid"),
        SiberReferenceTable.Arac => ("skn_arac", "aracid"),
        SiberReferenceTable.KapCins => ("skn_kapcins", "kapcinsid"),
        SiberReferenceTable.Kalem => ("skn_kalem", "kalemid"),
        _ => throw new ArgumentOutOfRangeException(nameof(table)),
    };

    public async Task<IReadOnlyList<string>> FindMissingAsync(
        SiberReferenceTable table, IReadOnlyCollection<string> ids,
        CancellationToken cancellationToken = default)
    {
        var (tableName, keyColumn) = Target(table);

        var candidates = ids
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (candidates.Count == 0)
            return [];

        // Biçimi GUID olmayan kimlik Siber'de olamaz; sorguya sokulmadan eksik
        // sayılır (uniqueidentifier dönüşümü aksi hâlde hata verirdi). Taklit
        // Siber'den kalan "ref-yuklemetip-0" gibi değerler tam olarak buraya düşer.
        var parsable = candidates.Where(id => Guid.TryParse(id, out _)).ToList();
        var missing = candidates.Except(parsable, StringComparer.OrdinalIgnoreCase).ToList();

        if (parsable.Count == 0)
            return missing;

        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        // tableName/keyColumn yukarıdaki kapalı listeden geliyor, kullanıcı girdisi değil.
        var found = (await connection.QueryAsync<string>(new CommandDefinition(
            $"""
            SELECT LOWER(CAST({keyColumn} AS VARCHAR(64)))
            FROM {tableName}
            WHERE {keyColumn} IN @Ids
            """,
            new { Ids = parsable.Select(Guid.Parse).ToArray() },
            cancellationToken: cancellationToken))).ToHashSet(StringComparer.OrdinalIgnoreCase);

        missing.AddRange(parsable.Where(id => !found.Contains(id)));
        return missing;
    }
}
