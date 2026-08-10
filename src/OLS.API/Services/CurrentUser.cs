using System.Security.Claims;
using OLS.Business.Services.Authorization;

namespace OLS.API.Services;

/// <summary>
/// JWT içindeki claim'lerden aktif kullanıcıyı okur.
/// olsold'daki <c>Auth::id()</c> / <c>Auth::user()</c> karşılığı.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public long? Id
    {
        get
        {
            var raw = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);
}
