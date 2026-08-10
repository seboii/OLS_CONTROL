using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;

namespace OLS.API.Middleware;

/// <summary>
/// olsold'da her controller metodu kendi try/catch(QueryException) bloğunu taşıyordu
/// ve 500 yanıtında ham SQL hata metnini <c>error</c> alanında istemciye gönderiyordu.
///
/// Burada aynı zarfı merkezî olarak üretiyoruz. Fark: ham hata metni yalnızca
/// Development ortamında gönderilir; üretimde loglanır, istemciye gitmez.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context, ITranslator translator)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                // Yanıt yazılmaya başlandıysa zarfı değiştiremeyiz; sadece logla.
                _logger.LogError(ex, "Yanıt başladıktan sonra istisna oluştu: {Path}", context.Request.Path);
                throw;
            }

            _logger.LogError(ex, "İşlenmemiş istisna: {Method} {Path}",
                context.Request.Method, context.Request.Path);

            var (status, message) = ex switch
            {
                KeyNotFoundException => (StatusCodes.Status404NotFound,
                    translator.Get("Kayıt Bulunamadı")),
                UnauthorizedAccessException => (StatusCodes.Status403Forbidden,
                    translator.Get("Yetkisiz Erişim")),
                DbUpdateException => (StatusCodes.Status500InternalServerError,
                    translator.Get("Form hataydı! Lütfen geliştiricinizle iletişime geçin.")),
                _ => (StatusCodes.Status500InternalServerError,
                    translator.Get("Beklenmeyen bir hata oluştu.")),
            };

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";

            var payload = ApiResponse.ServerError(
                message,
                _environment.IsDevelopment() ? ex.Message : null);

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
