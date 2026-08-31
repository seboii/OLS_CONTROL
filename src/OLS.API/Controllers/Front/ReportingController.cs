using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLS.API.Filters;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.Reporting;

namespace OLS.API.Controllers.Front;

/// <summary>
/// Kullanıcı bazlı KPI/raporlama ekranı — bkz. ReportingService açıklaması.
/// Dashboard'un aksine yetki denetimi VAR: burada kişi bazlı iş yükü/performans
/// verisi görünüyor, bu genel özet kartlarından farklı olarak herkese açık
/// olmamalı — diğer 8 modülle aynı <c>[RequiresPermission]</c> deseni kullanılıyor.
/// </summary>
[Authorize]
[Route("api/v1/reporting")]
public sealed class ReportingController : ApiControllerBase
{
    private readonly IReportingService _reporting;

    public ReportingController(IReportingService reporting) => _reporting = reporting;

    [HttpGet]
    [RequiresPermission(PermissionAction.Read, "report_management")]
    public async Task<IActionResult> Get(
        [FromQuery(Name = "date_from")] DateOnly? dateFrom,
        [FromQuery(Name = "date_to")] DateOnly? dateTo,
        CancellationToken cancellationToken)
    {
        var result = await _reporting.GetAsync(dateFrom, dateTo, cancellationToken);

        return Ok(result, "Kayıtlar");
    }

    [HttpGet("users/{userId:long}")]
    [RequiresPermission(PermissionAction.Read, "report_management")]
    public async Task<IActionResult> GetUserDetail(
        long userId,
        [FromQuery(Name = "date_from")] DateOnly? dateFrom,
        [FromQuery(Name = "date_to")] DateOnly? dateTo,
        CancellationToken cancellationToken)
    {
        var result = await _reporting.GetUserDetailAsync(userId, dateFrom, dateTo, cancellationToken);

        return result is null ? NotFoundError() : Ok(result, "Kayıtlar");
    }
}
