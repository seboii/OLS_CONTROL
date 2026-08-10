namespace OLS.API.Middleware;

/// <summary>
/// olsold'daki <c>App\Http\Middleware\Localization</c> karşılığı:
/// <c>X-localization</c> header'ı varsa küçük harfe çevirip aktif dil yapar,
/// yoksa "tr" kullanır.
/// </summary>
public sealed class LocalizationMiddleware
{
    internal const string LocaleItemKey = "__ols_locale";
    private const string HeaderName = "X-localization";
    private const string DefaultLocale = "tr";

    /// <summary>
    /// Desteklenen diller. olsold'da bu kontrol yoktu; header'dan gelen
    /// herhangi bir değer doğrudan app()->setLocale()'e veriliyordu.
    /// Beklenmeyen değerlerin dosya yolu olarak kullanılmasını engellemek için
    /// beyaz liste ekledik.
    /// </summary>
    private static readonly HashSet<string> Supported =
        new(StringComparer.OrdinalIgnoreCase) { "tr", "en", "de", "fr", "nl" };

    private readonly RequestDelegate _next;

    public LocalizationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var locale = DefaultLocale;

        if (context.Request.Headers.TryGetValue(HeaderName, out var header))
        {
            var requested = header.ToString().ToLowerInvariant();
            if (Supported.Contains(requested))
                locale = requested;
        }

        context.Items[LocaleItemKey] = locale;

        await _next(context);
    }
}
