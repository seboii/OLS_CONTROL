using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.DataAccess.Context;

namespace OLS.Business.Services.Siber;

/// <summary>
/// YEREL şehir seçimini Siber'in <c>sbr_sehir.sehirid</c> değerine çevirir.
///
/// Ülkelerdeki tuzağın aynısı (bkz. <see cref="ISiberCountryResolver"/>):
/// <c>cities.id</c> Siber'in kimliğiyle AYNI OLMAK ZORUNDA DEĞİL. Bugün 104
/// şehrin 102'sinde tesadüfen aynı, 2'sinde farklı ("İzmir", "İSTANBUL") —
/// ve Siber'den yeni şehirler içe aktarıldıkça bu fark büyüyor, çünkü içe
/// aktarma yerel kimliği KORUYOR ve yeni satırlara kendi GUID'ini veriyor.
///
/// Sefer güzergâhı Siber'de FK'li (<c>baslangicsehirid</c>, <c>bitissehirid</c>),
/// yani yanlış kimlik INSERT'i düşürür — bu yüzden çeviri tek yerde.
/// </summary>
public interface ISiberCityResolver
{
    /// <summary>
    /// Verilen yerel şehir kimlikleri için Siber kimliğini döner. Anahtar,
    /// verilen değerin kendisidir (harfe duyarsız); çözülemeyen girdi
    /// sözlükte yer almaz.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> ResolveAsync(
        IReadOnlyCollection<Guid?> cityIds, CancellationToken cancellationToken = default);
}

public sealed class SiberCityResolver : ISiberCityResolver
{
    private readonly OlsDbContext _db;

    public SiberCityResolver(OlsDbContext db) => _db = db;

    public async Task<IReadOnlyDictionary<string, string>> ResolveAsync(
        IReadOnlyCollection<Guid?> cityIds, CancellationToken cancellationToken = default)
    {
        var ids = cityIds.Where(id => id is not null).Select(id => id!.Value).Distinct().ToList();

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (ids.Count == 0)
            return result;

        var rows = await _db.Cities.AsNoTracking()
            .Where(c => ids.Contains(c.Id) && c.SiberId != null && c.SiberId != "")
            .Select(c => new { c.Id, c.SiberId })
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
            result[row.Id.ToString()] = row.SiberId!;

        return result;
    }
}
