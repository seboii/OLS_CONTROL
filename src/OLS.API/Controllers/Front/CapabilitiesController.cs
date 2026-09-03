using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;

namespace OLS.API.Controllers.Front;

/// <summary>
/// Oturumdaki kullanıcının ŞİRKETİNE bağlı açık/kapalı iş akışları.
///
/// Yetkiden (<c>/api/v1/role</c>) ayrı bir soru: yetki "bu kullanıcının bu
/// sayfada hakkı var mı", burası "bu şirket bu iş akışını kullanıyor mu".
/// İkisi aynı slug'a sıkıştırılamıyor çünkü Teklifler ve Yükler ekranları
/// AYNI yetki sayfasını (<c>load_management</c>) paylaşıyor — Teklifler'i
/// yetkiyle gizlemek Yükler'i de gizlerdi.
///
/// Arayüz menüyü ve düğmeleri buna göre kuruyor; uçlar ayrıca
/// <see cref="OLS.API.Filters.RequiresOfferModuleAttribute"/> ile korunuyor.
/// </summary>
[Authorize]
[Route("api/v1/capabilities")]
public sealed class CapabilitiesController : ApiControllerBase
{
    private readonly ICompanyScope _scope;
    private readonly ICurrentUser _currentUser;

    public CapabilitiesController(ICompanyScope scope, ICurrentUser currentUser)
    {
        _scope = scope;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var capabilities = await _scope.ResolveCapabilitiesAsync(
            _currentUser.Id, cancellationToken);

        // Seçilebilir şirketler: tek şirkete bağlı kullanıcıda tek öge döner ve
        // arayüz seçiciyi hiç göstermez. Yalnızca iki şirketi de gören kullanıcı
        // (süper admin) kaydın hangi şirkete açılacağını seçebilir.
        var companies = await _scope.ListWritableCompaniesAsync(
            _currentUser.Id, cancellationToken);

        return base.Ok(ApiResponse.Success(
            new
            {
                uses_offers = capabilities.UsesOffers,
                can_create_direct_load = capabilities.CanCreateDirectLoad,
                can_choose_company = companies.Count > 1,
                companies = companies.Select(c => new { id = c.Id, name = c.Name }),
            },
            "Kayıtlar"));
    }
}
