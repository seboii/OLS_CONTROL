using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLS.Business.Services.Dashboard;

namespace OLS.API.Controllers.Front;

/// <summary>
/// Hazır tasarımda (docs/tasarım) bulunan, önceki bir kapsam kararında dışarıda
/// bırakılan Dashboard ekranı için — kullanıcı bu tasarımı gösterip eklenmesini
/// istedi. olsold'un ayrı, kapsamlı Reports/Goals modülünün portu DEĞİL; yalnızca
/// bu 8 modülün zaten var olan verilerinden GERÇEK toplamlar/oranlar hesaplayan
/// hafif bir özet. Sayfa-yetkisi gerektirmiyor (kendi özet ekranı, olsold'da da
/// karşılığı yok — tasarımda da yetkiye göre gizlenen bir öğe yok).
/// </summary>
[Authorize]
[Route("api/v1/dashboard")]
public sealed class DashboardController : ApiControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _dashboard.GetAsync(cancellationToken);

        return Ok(result, "Kayıtlar");
    }
}
