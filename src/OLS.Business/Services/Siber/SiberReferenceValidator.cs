using OLS.DataAccess.Siber;

namespace OLS.Business.Services.Siber;

/// <summary>Doğrulanacak tek bir seçim: ekrandaki adı, hedef tablosu, Siber kimliği.</summary>
public readonly record struct SiberReferenceCheck(
    string Label, SiberReferenceTable Table, string? SiberId);

/// <summary>
/// Siber'e yazılacak referansları YAZIMDAN ÖNCE doğrular.
///
/// NEDEN TEK SERVİS: teklif, yük ve sefer akışları aynı soruyu soruyor ama
/// eskiden üçü ayrı kod yazıyordu ve üçü farklı sıkılıktaydı — teklif beş
/// referansı kontrol ediyordu, yük yalnızca tanımları, sefer hiçbirini. Siber'e
/// yazım GERİ ALINAMIYOR: yazım yarıda kalırsa yerel işlem geri alınıyor ama
/// Siber'deki kayıt kalıyor. Bu yüzden kontrol tek yerde toplandı; yeni bir
/// Siber yazımı eklendiğinde buradan geçmesi yeterli.
///
/// İKİ KUSURU BİRDEN yakalar:
///   * <c>SiberId</c> hiç yok ya da GUID bile değil (taklit Siber'den kalan
///     "ref-yuklemetip-0" gibi değerler),
///   * GUID var ama Siber'de o kayıt yok / silinmiş.
///
/// İkincisi kuramsal değil: canlıda üç cari Siber ekranından silinmişti ve
/// yerelde listede duruyordu; yük açarken FK hatası veriyordu.
/// </summary>
public interface ISiberReferenceValidator
{
    /// <summary>
    /// Sorun varsa kullanıcıya gösterilecek Türkçe mesajı, yoksa <c>null</c> döner.
    /// <c>SiberId</c>'si null olan seçimler "seçilmemiş" sayılır ve atlanır —
    /// zorunluluk kontrolü ayrı bir iştir.
    /// </summary>
    Task<string?> ValidateAsync(
        IReadOnlyList<SiberReferenceCheck> checks, CancellationToken cancellationToken = default);
}

public sealed class SiberReferenceValidator : ISiberReferenceValidator
{
    private readonly ISiberReferenceRepository _references;

    public SiberReferenceValidator(ISiberReferenceRepository references) => _references = references;

    public async Task<string?> ValidateAsync(
        IReadOnlyList<SiberReferenceCheck> checks, CancellationToken cancellationToken = default)
    {
        var problems = new List<string>();

        // Seçilmiş ama Siber karşılığı hiç tanımlanmamış olanlar.
        problems.AddRange(checks
            .Where(c => c.SiberId is not null && string.IsNullOrWhiteSpace(c.SiberId))
            .Select(c => $"{c.Label} (Siber karşılığı tanımlı değil)"));

        // Tablo başına tek sorgu.
        foreach (var group in checks
            .Where(c => !string.IsNullOrWhiteSpace(c.SiberId))
            .GroupBy(c => c.Table))
        {
            var missing = await _references.FindMissingAsync(
                group.Key,
                group.Select(c => c.SiberId!).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                cancellationToken);

            if (missing.Count == 0)
                continue;

            problems.AddRange(group
                .Where(c => missing.Contains(c.SiberId!, StringComparer.OrdinalIgnoreCase))
                .Select(c => $"{c.Label} (Siber'de bulunamadı)"));
        }

        if (problems.Count == 0)
            return null;

        return $"Şu seçimlerin Siber'de karşılığı yok: {string.Join(", ", problems.Distinct())}. " +
               "Kayıt Siber'den silinmiş olabilir; listeyi yenileyip yeniden seçin.";
    }
}
