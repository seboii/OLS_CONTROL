using Microsoft.EntityFrameworkCore;
using OLS.DataAccess.Context;

namespace OLS.Business.Services.Auditing;

/// <summary>
/// Bir kaydın TAM işlem geçmişi — Siber'in kendi değişiklik günlüğünden
/// (<c>sbr_log</c>) türetilir.
///
/// Kayıt üzerindeki <c>insuser</c>/<c>upduser</c> alanları yalnızca iki noktayı
/// verir (açan ve en son dokunan). Bu servis aradaki her işlemi, hangi alanın
/// hangi değerden hangi değere geçtiğiyle birlikte listeler.
/// </summary>
public interface IRecordHistoryService
{
    /// <summary>
    /// Geçmişi getirir. <paramref name="siberRecordId"/> kaydın SİBER kimliğidir
    /// (yükid / rezervasyonid / pozisyonid).
    /// </summary>
    Task<IReadOnlyList<RecordHistoryEntry>> GetAsync(
        string tableName, string siberRecordId, CancellationToken cancellationToken = default);
}

public sealed record RecordFieldChange(string Field, string? OldValue, string? NewValue);

public sealed record RecordHistoryEntry(
    long Id,
    DateTime? ChangedAt,
    string? UserCode,
    string? UserName,
    short? Operation,
    string OperationLabel,
    string? Module,
    IReadOnlyList<RecordFieldChange> Changes,
    /// <summary>
    /// Alan adları ile değer listeleri satır sayısı bakımından eşleşmediğinde
    /// true. Bu durumda <see cref="Changes"/> boş bırakılır — yanlış eşleşmiş
    /// bir "önceki → sonraki" çifti göstermek, hiç göstermemekten kötü.
    /// </summary>
    bool ChangesUnparsed,
    IReadOnlyList<string> ChangedFieldNames);

public sealed class RecordHistoryService : IRecordHistoryService
{
    /// <summary>Siber <c>yapilanislem</c> kodları.</summary>
    private const short OperationInsert = 1;
    private const short OperationUpdate = 2;
    private const short OperationDelete = 3;

    private readonly OlsDbContext _db;

    public RecordHistoryService(OlsDbContext db) => _db = db;

    public async Task<IReadOnlyList<RecordHistoryEntry>> GetAsync(
        string tableName, string siberRecordId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(siberRecordId))
            return [];

        var key = siberRecordId.Trim().ToLowerInvariant();

        var rows = await _db.SiberChangeLogs.AsNoTracking()
            .Where(l => l.TableName == tableName && l.RecordId == key)
            .OrderByDescending(l => l.ChangedAt)
            .Select(l => new
            {
                l.Id,
                l.ChangedAt,
                l.UserCode,
                UserName = l.User != null ? l.User.Name : null,
                l.Operation,
                l.Module,
                l.Fields,
                l.OldValues,
                l.NewValues,
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r =>
        {
            var (changes, unparsed, fieldNames) = Diff(r.Operation, r.Fields, r.OldValues, r.NewValues);

            return new RecordHistoryEntry(
                r.Id, r.ChangedAt, r.UserCode, r.UserName, r.Operation,
                Label(r.Operation), r.Module, changes, unparsed, fieldNames);
        }).ToList();
    }

    private static string Label(short? operation) => operation switch
    {
        OperationInsert => "Oluşturdu",
        OperationUpdate => "Güncelledi",
        OperationDelete => "Sildi",
        _ => "İşlem",
    };

    /// <summary>
    /// Alan adlarını ve değer listelerini eşleştirir.
    ///
    /// Üç metin de satır sonuyla ayrılmış ve KONUM KONUM eşleşen listelerdir.
    /// Canlıda 3.000 örneğin 2.984'ünde hizalama tutuyor; tutmayan %0,5 çok
    /// satırlı bir metin alanı (açıklama gibi) içeriyor ve o kayıtlarda konum
    /// eşleşmesi kayıyor. Bu yüzden satır sayıları önce doğrulanır; tutmuyorsa
    /// değer eşleştirmesi YAPILMAZ, yalnızca değişen alan adları gösterilir.
    ///
    /// Güncellemede yalnızca gerçekten DEĞİŞEN alanlar döner: Siber her
    /// güncellemede kaydın tüm izlenen alanlarını yazıyor ve değişmeyenleri de
    /// listelemek geçmişi okunmaz hâle getiriyor.
    /// </summary>
    private static (IReadOnlyList<RecordFieldChange> Changes, bool Unparsed, IReadOnlyList<string> FieldNames) Diff(
        short? operation, string? fields, string? oldValues, string? newValues)
    {
        var fieldList = Split(fields);
        if (fieldList.Count == 0)
            return ([], false, []);

        var oldList = Split(oldValues);
        var newList = Split(newValues);

        var aligned = operation switch
        {
            OperationInsert => newList.Count == fieldList.Count,
            OperationDelete => oldList.Count == fieldList.Count,
            _ => oldList.Count == fieldList.Count && newList.Count == fieldList.Count,
        };

        if (!aligned)
            return ([], true, fieldList);

        var changes = new List<RecordFieldChange>();

        for (var i = 0; i < fieldList.Count; i++)
        {
            var before = operation == OperationInsert ? null : Value(oldList, i);
            var after = operation == OperationDelete ? null : Value(newList, i);

            // Güncellemede değişmeyen alan atlanır.
            if (operation == OperationUpdate && string.Equals(before, after, StringComparison.Ordinal))
                continue;

            if (before is null && after is null)
                continue;

            changes.Add(new RecordFieldChange(fieldList[i], before, after));
        }

        return (changes, false, fieldList);
    }

    private static string? Value(IReadOnlyList<string> values, int index)
    {
        if (index >= values.Count)
            return null;

        var value = values[index].Trim();
        return value.Length == 0 ? null : value;
    }

    /// <summary>
    /// Satırlara böler. BOŞ SATIRLAR KORUNUR — değeri boş olan alan da bir satır
    /// işgal ediyor ve atlanırsa sonraki tüm alanların eşleşmesi kayar.
    /// </summary>
    private static List<string> Split(string? text) =>
        string.IsNullOrEmpty(text)
            ? []
            : text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').ToList();
}
