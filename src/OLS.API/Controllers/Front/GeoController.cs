using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.DataAccess.Context;

namespace OLS.API.Controllers.Front;

/// <summary>
/// Ülke / şehir / ilçe uçları — salt okunur (olsold'da da yalnızca all + single vardı).
///
/// Bu tablolar Guid anahtarlı olduğu için <see cref="LookupControllerBase{TEntity}"/>
/// (long anahtar varsayar) kullanılamıyor; bu yüzden açıkça yazıldı.
///
/// Veri kaynağı: üretimde bu üç tablo Siber'den içe aktarılıyor
/// (<c>sbr_ulke</c>, <c>sbr_sehir</c>, <c>sbr_ilce</c>) ve Siber'in kendi
/// GUID'leri birincil anahtar olarak kullanılıyor.
/// </summary>
[Authorize]
[Route("api/v1")]
public sealed class GeoController : ApiControllerBase
{
    private readonly OlsDbContext _db;

    public GeoController(OlsDbContext db) => _db = db;

    [HttpGet("country")]
    public async Task<IActionResult> Countries(
        [FromQuery] string? search,
        [FromQuery(Name = "per_page")] int? perPage,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Countries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => EF.Functions.ILike(c.Name!, $"%{search}%"));

        var ordered = query.OrderBy(c => c.Name);

        var result = await ordered.ToPagedOrListAsync(perPage, page, CurrentPath, cancellationToken);
        return Ok(result, "Kayıtlar");
    }

    [HttpGet("country/{id:guid}")]
    public async Task<IActionResult> Country(Guid id, CancellationToken cancellationToken)
    {
        var country = await _db.Countries.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return country is null ? NotFoundError() : Ok(country, "Kayıtlar");
    }

    [HttpGet("city")]
    public async Task<IActionResult> Cities(
        [FromQuery(Name = "country_id")] string? countryId,
        [FromQuery] string? search,
        [FromQuery(Name = "per_page")] int? perPage,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Cities.AsNoTracking().AsQueryable();

        // country_id metin olarak saklanıyor ve Siber GUID'leri büyük harf;
        // olsold da karşılaştırmadan önce strtoupper uyguluyordu.
        if (!string.IsNullOrWhiteSpace(countryId))
            query = query.Where(c => c.CountryId == countryId.ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => EF.Functions.ILike(c.Name!, $"%{search}%"));

        var ordered = query.OrderBy(c => c.Id);

        var result = await ordered.ToPagedOrListAsync(perPage, page, CurrentPath, cancellationToken);
        return Ok(result, "Kayıtlar");
    }

    [HttpGet("city/{id:guid}")]
    public async Task<IActionResult> City(Guid id, CancellationToken cancellationToken)
    {
        var city = await _db.Cities.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return city is null ? NotFoundError() : Ok(city, "Kayıtlar");
    }

    [HttpGet("district")]
    public async Task<IActionResult> Districts(
        [FromQuery(Name = "city_id")] string? cityId,
        [FromQuery] string? search,
        [FromQuery(Name = "per_page")] int? perPage,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Districts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(cityId))
            query = query.Where(d => d.CityId == cityId.ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => EF.Functions.ILike(d.Name!, $"%{search}%"));

        // olsold burada yalnızca id, city_id, name, slug seçiyordu.
        var ordered = query
            .OrderBy(d => d.Id)
            .Select(d => new { id = d.Id, city_id = d.CityId, name = d.Name, slug = d.Slug });

        var result = await ordered.ToPagedOrListAsync(perPage, page, CurrentPath, cancellationToken);
        return Ok(result, "Kayıtlar");
    }

    [HttpGet("district/{id:guid}")]
    public async Task<IActionResult> District(Guid id, CancellationToken cancellationToken)
    {
        var district = await _db.Districts.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        return district is null ? NotFoundError() : Ok(district, "Kayıtlar");
    }
}
