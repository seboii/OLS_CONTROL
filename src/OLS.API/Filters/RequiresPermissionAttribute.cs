using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;

namespace OLS.API.Filters;

/// <summary>
/// olsold'daki <c>RoleHelper::permission($crud, $page)</c> kontrolünün karşılığı.
/// Yetki modeli aynı kalır: <c>user_permission_pages.permission_page_slug</c> ×
/// <c>user_permissions</c> tablosundaki read/create/update/delete bayrakları.
///
/// Davranış farkı (bilinçli): olsold'da yetki yoksa <c>abort()</c> satırı yorumdaydı,
/// yani kontrol fiilen hiçbir şeyi engellemiyordu. Ayrıca süslü parantezsiz
/// <c>if</c> kullanımı yüzünden kontrol çoğu yerde yalnızca sonraki tek satırı
/// kapsıyordu. Burada yetki yoksa istek gerçekten 403 ile reddedilir.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class RequiresPermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly PermissionAction _action;
    private readonly string _pageSlug;

    public RequiresPermissionAttribute(PermissionAction action, string pageSlug)
    {
        _action = action;
        _pageSlug = pageSlug;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var services = context.HttpContext.RequestServices;
        var permissions = services.GetRequiredService<IPermissionService>();
        var translator = services.GetRequiredService<ITranslator>();
        var currentUser = services.GetRequiredService<ICurrentUser>();

        if (currentUser.Id is not { } userId)
        {
            context.Result = new JsonResult(ApiResponse.Error(translator.Get("Yetkisiz Erişim")))
            {
                StatusCode = StatusCodes.Status401Unauthorized,
            };
            return;
        }

        var allowed = await permissions.HasPermissionAsync(
            userId, _pageSlug, _action, context.HttpContext.RequestAborted);

        if (!allowed)
        {
            context.Result = new JsonResult(ApiResponse.Error(translator.Get("Yetkisiz Erişim")))
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
        }
    }
}
