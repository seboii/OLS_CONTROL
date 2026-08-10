using Serilog.Context;

namespace OLS.API.Middleware;

/// <summary>
/// Her isteğe bir correlation id atar (istemci <c>X-Correlation-ID</c> gönderdiyse
/// onu kullanır, yoksa üretir), yanıt header'ında geri döner ve Serilog
/// LogContext'ine ekler. olsnew'de bu yoktu (bkz. 09-DevOps-ve-Operasyon.md,
/// OPS bulguları) — üretim sorunlarını istek bazında izlemek için eklendi.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing)
            && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString("n");

        context.Items[HeaderName] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
