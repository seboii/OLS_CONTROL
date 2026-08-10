using System.Collections.Concurrent;
using System.Text.Json;
using OLS.API.Middleware;
using OLS.Business.Common;

namespace OLS.API.Localization;

/// <summary>
/// <c>Resources/general.{locale}.json</c> dosyalarından çeviri okur.
/// Dosyalar olsold'un <c>resources/lang/{locale}/general.php</c> karşılığıdır.
/// Aktif dil <see cref="LocalizationMiddleware"/> tarafından belirlenir.
/// </summary>
public sealed class JsonTranslator : ITranslator
{
    private const string FallbackLocale = "tr";

    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _cache = new();
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _resourcePath;
    private readonly ILogger<JsonTranslator> _logger;

    public JsonTranslator(
        IHttpContextAccessor httpContextAccessor,
        IWebHostEnvironment environment,
        ILogger<JsonTranslator> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _resourcePath = Path.Combine(environment.ContentRootPath, "Resources");
    }

    public string Get(string key)
    {
        var locale = _httpContextAccessor.HttpContext?.Items[LocalizationMiddleware.LocaleItemKey] as string
                     ?? FallbackLocale;

        if (Load(locale).TryGetValue(key, out var value))
            return value;

        if (locale != FallbackLocale && Load(FallbackLocale).TryGetValue(key, out var fallback))
            return fallback;

        // Laravel çeviri bulunamadığında anahtarı olduğu gibi döndürür.
        return key;
    }

    private Dictionary<string, string> Load(string locale) =>
        _cache.GetOrAdd(locale, l =>
        {
            var file = Path.Combine(_resourcePath, $"general.{l}.json");

            if (!File.Exists(file))
            {
                _logger.LogWarning("Çeviri dosyası bulunamadı: {File}", file);
                return [];
            }

            try
            {
                var json = File.ReadAllText(file);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Çeviri dosyası okunamadı: {File}", file);
                return [];
            }
        });
}
