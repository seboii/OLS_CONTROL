using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLS.Business.Common;
using OLS.Business.Services.Loads;

namespace OLS.API.Controllers.Front;

/// <summary>
/// Navlun alış → satış kalem eşleşmeleri.
///
/// YETKİ ARANMAZ (yalnızca oturum): bu liste teklif, teklifsiz yük ve yük
/// detay formlarının üçünde birden kullanılıyor ve kayıt AÇMAK bu ekranlarda
/// zaten yetkiye bağlı değil (bkz. LoadController). Eşleşme listesi gizli bir
/// bilgi değil, kalem listesinin kendisi zaten görünür.
/// </summary>
[Authorize]
[Route("api/v1/financial_item_pair")]
public sealed class FinancialItemPairController : ApiControllerBase
{
    private readonly IFinancialItemPairService _pairs;

    public FinancialItemPairController(IFinancialItemPairService pairs) => _pairs = pairs;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        base.Ok(ApiResponse.Success(await _pairs.ListAsync(cancellationToken), "Kayıtlar"));
}
