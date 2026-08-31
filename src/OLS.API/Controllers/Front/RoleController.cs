using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.API.Filters;
using OLS.Business.Services.Roles;

namespace OLS.API.Controllers.Front;

/// <summary>
/// olsold: <c>Front\Role\UserPermissionController</c> — GET/PUT /api/v1/role
///
/// Yanıt zarfı diğer uçlardan FARKLI: <c>{ id, stats: { permission_data, user_name } }</c>.
/// Frontend <c>data_store.js</c> içinde <c>res.data.stats.permission_data</c> okuyor,
/// bu yüzden şekil değiştirilemez.
/// </summary>
[Authorize]
public sealed class RoleController : ApiControllerBase
{
    private readonly IUserPermissionService _permissions;
    private readonly IPermissionService _permissionCheck;
    private readonly ICurrentUser _currentUser;
    private readonly IRoleService _roles;

    public RoleController(
        IUserPermissionService permissions,
        IPermissionService permissionCheck,
        ICurrentUser currentUser,
        IRoleService roles)
    {
        _permissions = permissions;
        _permissionCheck = permissionCheck;
        _currentUser = currentUser;
        _roles = roles;
    }

    /// <summary>Tanımlı rolleri ve her rolün kullanıcı sayısını döndürür.</summary>
    [HttpGet("roles")]
    [RequiresPermission(PermissionAction.Read, "role_management")]
    public async Task<IActionResult> Roles(CancellationToken cancellationToken) =>
        Ok(await _roles.ListAsync(cancellationToken), "Kayıtlar");

    /// <summary>
    /// Kullanıcıya rol uygular — rolün şablonu user_permissions satırlarına yazılır.
    /// </summary>
    [HttpPost("roles/assign")]
    [RequiresPermission(PermissionAction.Update, "role_management")]
    public async Task<IActionResult> AssignRole(
        [FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
    {
        if (request.UserId is not { } userId || request.RoleId is not { } roleId)
            return BadRequestError("Kullanıcı ve rol zorunludur.");

        return await _roles.AssignAsync(userId, roleId, cancellationToken)
            ? base.Ok(ApiResponse.Message("Rol uygulandı"))
            : NotFoundError();
    }

    /// <summary>
    /// Kullanıcının ŞİRKET GÖRME KAPSAMINI ayarlar (AVRORA / OLS).
    ///
    /// Rolden ayrı tutuluyor: rol "ne yapabilir", kapsam "ne görebilir".
    /// Boş gönderilirse kapsam temizlenir — kullanıcı Avrora dışındaki her şeyi
    /// görür (varsayılan davranış, bkz. CompanyScope).
    /// </summary>
    [HttpPost("roles/company-scope")]
    [RequiresPermission(PermissionAction.Update, "role_management")]
    public async Task<IActionResult> SetCompanyScope(
        [FromBody] CompanyScopeRequest request, CancellationToken cancellationToken)
    {
        if (request.UserId is not { } userId)
            return BadRequestError("Kullanıcı zorunludur.");

        return await _roles.SetCompanyScopeAsync(userId, request.CompanyId, cancellationToken)
            ? base.Ok(ApiResponse.Message("Şirket kapsamı güncellendi"))
            : NotFoundError();
    }

    /// <summary>
    /// Siber departmanlarına göre TÜM kullanıcılara rol uygular ve Siber'de
    /// engelli olanları pasife alır. Toplu bakım ucu.
    /// </summary>
    [HttpPost("roles/apply-from-siber")]
    [RequiresPermission(PermissionAction.Update, "role_management")]
    public async Task<IActionResult> ApplyFromSiber(CancellationToken cancellationToken) =>
        Ok(await _roles.ApplyFromSiberAsync(cancellationToken), "Roller uygulandı");

    [HttpGet("role")]
    public async Task<IActionResult> Single([FromQuery] long id, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } currentUserId)
            return Unauthorized(ApiResponse.Error(Translator.Get("Yetkisiz Erişim")));

        // olsold kuralı: kendi yetkilerini herkes okuyabilir; başkasınınki için
        // role_management/read gerekir. Frontend her sayfa geçişinde kendi
        // id'siyle çağırdığı için self-read şart.
        if (id != currentUserId)
        {
            var allowed = await _permissionCheck.HasPermissionAsync(
                currentUserId, "role_management", PermissionAction.Read, cancellationToken);

            if (!allowed)
                return StatusCode(StatusCodes.Status403Forbidden,
                    ApiResponse.Error(Translator.Get("Yetkisiz Erişim")));
        }

        var result = await _permissions.GetAsync(id, cancellationToken);

        if (result is null)
            return NotFoundError();

        return base.Ok(new RoleEnvelope
        {
            Id = id,
            Stats = new RoleStats
            {
                PermissionData = result.PermissionData,
                UserName = result.UserName,
            },
        });
    }

    public sealed class UpdateRoleRequest
    {
        [JsonPropertyName("crud")]
        public string? Crud { get; set; }

        [JsonPropertyName("is_data")]
        public short IsData { get; set; }

        /// <summary>Tek satır güncellemesi için user_permissions.id</summary>
        [JsonPropertyName("permission_page_id")]
        public long? PermissionPageId { get; set; }

        /// <summary>permission_page_id yoksa bu kullanıcının tüm satırları güncellenir.</summary>
        [JsonPropertyName("user_id")]
        public long? UserId { get; set; }
    }

    [HttpPut("role")]
    public async Task<IActionResult> Update(
        [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } currentUserId)
            return Unauthorized(ApiResponse.Error(Translator.Get("Yetkisiz Erişim")));

        // olsold'da yetki kontrolü süslü parantezsiz bir if içindeydi: yetki yoksa
        // hiçbir şey yapılmadan yine {"result":"success"} dönüyordu (sessiz no-op).
        // Burada gerçekten 403 veriyoruz.
        var allowed = await _permissionCheck.HasPermissionAsync(
            currentUserId, "role_management", PermissionAction.Update, cancellationToken);

        if (!allowed)
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse.Error(Translator.Get("Yetkisiz Erişim")));

        var ok = await _permissions.UpdateAsync(
            request.Crud ?? string.Empty,
            request.IsData,
            request.PermissionPageId,
            request.UserId,
            cancellationToken);

        if (!ok)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Message(
                Translator.Get("Form hataydı! Lütfen geliştiricinizle iletişime geçin.")));

        return base.Ok(new UpdateResult { Result = "success" });
    }

    private sealed class RoleEnvelope
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }

        [JsonPropertyName("stats")]
        public RoleStats Stats { get; init; } = new();
    }

    private sealed class RoleStats
    {
        [JsonPropertyName("permission_data")]
        public IReadOnlyList<PermissionRow> PermissionData { get; init; } = [];

        [JsonPropertyName("user_name")]
        public string UserName { get; init; } = string.Empty;
    }

    private sealed class UpdateResult
    {
        [JsonPropertyName("result")]
        public string Result { get; init; } = string.Empty;
    }
}

public sealed class AssignRoleRequest
{
    [JsonPropertyName("user_id")] public long? UserId { get; set; }
    [JsonPropertyName("role_id")] public long? RoleId { get; set; }
}

public sealed class CompanyScopeRequest
{
    [JsonPropertyName("user_id")] public long? UserId { get; set; }
    /// <summary>Boş/null = kapsam yok (Avrora hariç her şey).</summary>
    [JsonPropertyName("company_id")] public string? CompanyId { get; set; }
}
