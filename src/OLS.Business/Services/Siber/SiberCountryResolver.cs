using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.DataAccess.Context;
using OLS.DataAccess.Siber;

namespace OLS.Business.Services.Siber;

/// <summary>
/// Bir ülkenin Siber'e yazılacak üç yüzü: kimliği (teklif), adı ve kıtası (yük).
/// </summary>
public sealed record SiberCountry(string? SiberId, string? Name, string? Continent);

/// <summary>
/// YEREL ülke seçimini Siber'in beklediği biçime çevirir.
///
/// ÜÇ AYRI TUZAK BİR ARADA:
///
/// 1. <b>Yerel kimlik ≠ Siber kimliği.</b> <c>countries.id</c> 197 satırın
///    171'inde <c>siber_id</c> ile aynı (eski aktarımda Siber GUID'i doğrudan PK
///    yapılmış), ama 26'sında DEĞİL — "TURKYE", "ÇEK CUMHURİYETİ", "BELARUS"...
///    Yerel kimliği Siber'e yazmak bu 26 ülkede yetim değer üretiyordu.
///
/// 2. <b><c>skn_yuk</c>'ta ülke kimliği sütunu YOK.</b> Yükte ülke yalnızca
///    <c>_yuklemeulke</c>/<c>_bosaltmaulke</c> metin sütunlarında, ÜLKE ADI
///    olarak tutuluyor (canlıda dolu 7.486 satırın hiçbirinde GUID yok).
///    Buraya GUID yazmak Siber ekranında okunamaz bir kayıt bırakıyordu.
///
/// 3. <b>Kıta ayrı bir sütun.</b> <c>_yuklemekita</c>/<c>_bosaltmakita</c>
///    de dolu (7.486/7.486) ve ülkeden türüyor; uygulama bunu sabit "ASYA"
///    yazıyordu, yani Avrupa yüklerinin tamamı yanlış kıtayla açılıyordu.
///
/// Çözüm tek yerde: girdi ne olursa olsun (yerel GUID, Siber GUID ya da düz
/// ülke adı — üçü de canlı veride mevcut) önce yerel <c>countries</c> satırına,
/// oradan <c>siber_id</c>'ye, oradan Siber'in kendi ad + kıta değerine inilir.
/// </summary>
public interface ISiberCountryResolver
{
    /// <summary>
    /// Girdi başına çözüm döner. Anahtar, verilen değerin KENDİSİDİR (kırpılmış,
    /// harfe duyarsız). Çözülemeyen girdi sözlükte yer almaz.
    /// </summary>
    Task<IReadOnlyDictionary<string, SiberCountry>> ResolveAsync(
        IReadOnlyCollection<string?> values, CancellationToken cancellationToken = default);

    Task<SiberCountry?> ResolveOneAsync(
        string? value, CancellationToken cancellationToken = default);
}

public sealed class SiberCountryResolver : ISiberCountryResolver
{
    private readonly OlsDbContext _db;
    private readonly ISiberCountryRepository _countries;

    public SiberCountryResolver(OlsDbContext db, ISiberCountryRepository countries)
    {
        _db = db;
        _countries = countries;
    }

    public async Task<SiberCountry?> ResolveOneAsync(
        string? value, CancellationToken cancellationToken = default)
    {
        var map = await ResolveAsync([value], cancellationToken);
        return value is not null && map.TryGetValue(value.Trim(), out var country) ? country : null;
    }

    public async Task<IReadOnlyDictionary<string, SiberCountry>> ResolveAsync(
        IReadOnlyCollection<string?> values, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, SiberCountry>(StringComparer.OrdinalIgnoreCase);

        var inputs = values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (inputs.Count == 0)
            return result;

        // Ülke tablosu 197 satır — tamamını çekip bellekte eşleştirmek, üç ayrı
        // eşleşme kuralını (yerel kimlik / Siber kimliği / ad) SQL'e taşımaktan
        // hem ucuz hem okunur.
        var rows = await _db.Countries.AsNoTracking()
            .Select(c => new { c.Id, c.SiberId, c.Name })
            .ToListAsync(cancellationToken);

        var byLocalId = rows
            .GroupBy(r => r.Id.ToString(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().SiberId, StringComparer.OrdinalIgnoreCase);

        var bySiberId = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.SiberId))
            .GroupBy(r => r.SiberId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().SiberId, StringComparer.OrdinalIgnoreCase);

        // Aynı ülkenin iki yerel satırı olabiliyor ("TÜRKİYE" ve "Türkiye" — ikisi
        // de aynı siber_id'yi taşıyor), bu yüzden ad anahtarında ilk satır alınır.
        var byName = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Name) && !string.IsNullOrWhiteSpace(r.SiberId))
            .GroupBy(r => QueryableExtensions.NormalizeTurkish(r.Name!.Trim()))
            .ToDictionary(g => g.Key, g => g.First().SiberId!);

        var siberIdByInput = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var input in inputs)
        {
            string? siberId = null;

            if (Guid.TryParse(input, out _))
            {
                if (byLocalId.TryGetValue(input, out var fromLocal) && !string.IsNullOrWhiteSpace(fromLocal))
                    siberId = fromLocal;
                else if (bySiberId.TryGetValue(input, out var fromSiber))
                    siberId = fromSiber;
                else
                    // Yerelde hiç karşılığı olmayan GUID: Siber'in kendi kimliği
                    // olabilir (senkron ham yazmış olabilir), Siber'e sorulur.
                    siberId = input;
            }
            else if (byName.TryGetValue(QueryableExtensions.NormalizeTurkish(input), out var fromName))
            {
                siberId = fromName;
            }

            if (siberId is not null)
                siberIdByInput[input] = siberId;
        }

        var siberRows = await _countries.GetAsync(
            siberIdByInput.Values.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            cancellationToken);

        foreach (var (input, siberId) in siberIdByInput)
        {
            if (siberRows.TryGetValue(siberId, out var row))
                result[input] = new SiberCountry(siberId, row.Name, row.Continent);
            else
                // Siber'de karşılığı yok: kimlik yine de dönülür ki çağıran
                // "seçilmiş ama Siber'de bulunamadı" diye uyarabilsin.
                result[input] = new SiberCountry(siberId, null, null);
        }

        return result;
    }
}
