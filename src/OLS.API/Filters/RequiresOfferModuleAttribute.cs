using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;

namespace OLS.API.Filters;

/// <summary>
/// Teklif modülünü kullanmayan şirketin isteğini reddeder.
///
/// Avrora teklifle çalışmıyor: yükü doğrudan Yükler ekranından açıyor. Arayüzde
/// Teklifler sekmesi gizli, ama gizli menü YETKİ DEĞİLDİR — adresi elle yazan
/// ya da ucu doğrudan çağıran istek de kapatılmalı. Karar tek yerde:
/// <see cref="ICompanyScope.ResolveCapabilitiesAsync"/>.
///
/// Yetki (<see cref="RequiresPermissionAttribute"/>) ile birlikte kullanılır ve
/// onun yerine GEÇMEZ: bu "şirket bu iş akışını kullanıyor mu", o "kullanıcının
/// bu sayfada hakkı var mı" sorusudur.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequiresOfferModuleAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var services = context.HttpContext.RequestServices;
        var scope = services.GetRequiredService<ICompanyScope>();
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

        var capabilities = await scope.ResolveCapabilitiesAsync(
            userId, context.HttpContext.RequestAborted);

        if (!capabilities.UsesOffers)
        {
            context.Result = new JsonResult(ApiResponse.Error(
                translator.Get("Şirketinizde teklif modülü kullanılmıyor.")))
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
        }
    }
}
