using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLS.API.Filters;
using OLS.Business.Services.Auditing;
using OLS.Business.Services.Authorization;

namespace OLS.API.Controllers.Front;

/// <summary>
/// Denetim kaydı — YALNIZCA yöneticiye açık.
///
/// Yetki <c>audit_log_management</c> sayfasıyla korunuyor ve bu sayfa rol
/// kataloğunda SADECE Yönetim rolüne verilmiştir (bkz. RoleCatalog). Kimin ne
/// yaptığını görmek, yetki yönetimiyle aynı hassasiyette bir bilgidir.
/// </summary>
[Authorize]
public sealed class AuditLogController : ApiControllerBase
{
    private readonly IAuditLogService _audit;

    public AuditLogController(IAuditLogService audit) => _audit = audit;

    /// <summary>
    /// Denetim kayıtları. <c>after_id</c> verilirse yalnızca ondan SONRAKİ
    /// kayıtlar döner — arayüzün anlık takip döngüsü bunu kullanır.
    /// </summary>
    [HttpGet("audit_log")]
    [RequiresPermission(PermissionAction.Read, "audit_log_management")]
    public async Task<IActionResult> All(
        [FromQuery] string? search,
        [FromQuery(Name = "entity_type")] string? entityType,
        [FromQuery(Name = "user_id")] long? userId,
        [FromQuery(Name = "entity_label")] string? entityLabel,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery(Name = "after_id")] long? afterId,
        [FromQuery(Name = "per_page")] int? perPage,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _audit.ListAsync(
            new AuditLogQuery(search, entityType, userId, entityLabel,
                from, to, afterId, perPage, page, CurrentPath),
            cancellationToken);

        return Ok(result, "Kayıtlar");
    }

    /// <summary>Arama kutusunun önerileri: yük numarası, sefer numarası, kullanıcı, cari.</summary>
    [HttpGet("audit_log/targets")]
    [RequiresPermission(PermissionAction.Read, "audit_log_management")]
    public async Task<IActionResult> Targets(
        [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(await _audit.TargetsAsync(search, cancellationToken), "Kayıtlar");
}
