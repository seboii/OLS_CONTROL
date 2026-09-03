using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.DataAccess.Context;

namespace OLS.API.Controllers.Front;

/// <summary>
/// Sefer sürücüsü seçicisini besleyen SALT OKUNUR liste.
///
/// Genel <c>LookupControllerBase</c> kullanılmadı: o taban CRUD ve
/// <c>Code</c>/<c>GroupCode</c> alanları bekliyor; personel yerelde
/// açılmıyor, yalnızca Siber'den içe aktarılıyor (bkz.
/// <c>SiberImportService.ImportPersonnelAsync</c>).
///
/// Varsayılan olarak YALNIZCA sürücüler döner (canlıda 25 personelin 22'si);
/// <c>?driver=false</c> ile tamamı istenebilir.
/// </summary>
[Authorize]
[Route("api/v1/personnel")]
public sealed class PersonnelController : ApiControllerBase
{
    private readonly OlsDbContext _db;

    public PersonnelController(OlsDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? search,
        [FromQuery] bool driver = true,
        [FromQuery(Name = "per_page")] int? perPage = null,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        // Siber karşılığı olmayan satır listelenmez: seçilirse sefer kaydı
        // "Sürücü (Siber'de bulunamadı)" ile reddedilirdi.
        var query = _db.Personnel.AsNoTracking()
            .Where(p => p.SiberId != null && p.SiberId != "");

        if (driver)
            query = query.Where(p => p.IsDriver);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name != null && EF.Functions.ILike(p.Name, $"%{search}%"));

        var ordered = query
            .OrderBy(p => p.Name)
            .Select(p => new { id = p.Id, name = p.Name, is_driver = p.IsDriver });

        var result = await ordered.ToPagedOrListAsync(perPage, page, CurrentPath, cancellationToken);
        return Ok(result, "Kayıtlar");
    }
}
