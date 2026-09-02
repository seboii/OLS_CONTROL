using System.Text.Json;
using Microsoft.Data.SqlClient;
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

                // SİBER'İN KENDİ İŞ KURALI MESAJI KULLANICIYA GÖSTERİLİR.
                // Siber, iş kurallarını trigger'larda RAISERROR ile uyguluyor ve
                // mesajlar zaten Türkçe ve anlaşılır ("EX,IM Seferlerde sefer
                // romork bilgisi pozisyondan farklı olamaz!"). Bunları
                // "Beklenmeyen bir hata oluştu." ile örtmek, kullanıcıya
                // düzeltebileceği bir sorunu ARIZA gibi gösteriyordu. Yalnızca
                // RAISERROR ile üretilenler (hata numarası 50000) geçirilir;
                // bağlantı/FK gibi teknik hatalar genel mesajda kalır.
                SqlException { Number: SiberRuleErrorNumber } sql
                    => (StatusCodes.Status422UnprocessableEntity, SiberRuleMessage(sql)),

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

    /// <summary>SQL Server'da RAISERROR ile üretilen kullanıcı hatalarının numarası.</summary>
    private const int SiberRuleErrorNumber = 50000;

    /// <summary>
    /// Siber mesajlarının sonunda tetikleyicinin adı bir işaretle duruyor
    /// ("... olamaz! #skn_pozisyon_seferromorkkontrol_tr"). Kullanıcıya
    /// gösterilmeden önce bu teknik kuyruk atılır; log'da tam metin duruyor.
    /// </summary>
    private static string SiberRuleMessage(SqlException exception)
    {
        var text = exception.Message.Split('#')[0].Trim();

        return string.IsNullOrWhiteSpace(text)
            ? "Siber bu kaydı kabul etmedi."
            : text;
    }
}
