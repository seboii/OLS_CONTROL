using System.Text.Json.Serialization;

namespace OLS.Business.Common;

/// <summary>
/// olsold'un yanıt zarfları. Laravel controller'ları üç şekil üretiyordu ve
/// frontend (data_store.js + useGetFirstError) bunlara göre yazılmış:
///
///   başarı : { "data": ..., "message": "..." }
///   doğrulama hatası : { "errors": { "alan": ["mesaj"] } }        -> 400/422
///   sunucu hatası    : { "message": "...", "error": "..." }       -> 500
///
/// Zarf şekli frontend sözleşmesidir; değiştirmeden önce data_store.js'e bakın.
/// </summary>
public static class ApiResponse
{
    public static object Success(object? data, string message) =>
        new SuccessEnvelope { Data = data, Message = message };

    /// <summary>Sadece mesaj dönen uçlar (delete, update gibi) için.</summary>
    public static object Message(string message) =>
        new MessageEnvelope { Message = message };

    /// <summary>
    /// Alan bazlı doğrulama hataları. Laravel'in <c>$validator->errors()</c>
    /// çıktısıyla aynı şekil: alan adı -> mesaj dizisi.
    /// </summary>
    public static object ValidationErrors(IDictionary<string, string[]> errors) =>
        new ErrorsEnvelope { Errors = errors };

    /// <summary>
    /// Alan adı olmayan tekil hata. olsold'da
    /// <c>['errors' => ['Sefer Bulunamadı']]</c> şeklinde kullanılıyordu.
    /// </summary>
    public static object Error(params string[] messages) =>
        new FlatErrorsEnvelope { Errors = messages };

    /// <summary>Yakalanan istisnalar için. olsold QueryException bloklarının karşılığı.</summary>
    public static object ServerError(string message, string? error) =>
        new ServerErrorEnvelope { Message = message, Error = error };

    private sealed class SuccessEnvelope
    {
        [JsonPropertyName("data")]
        public object? Data { get; init; }

        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;
    }

    private sealed class MessageEnvelope
    {
        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;
    }

    private sealed class ErrorsEnvelope
    {
        [JsonPropertyName("errors")]
        public IDictionary<string, string[]> Errors { get; init; } =
            new Dictionary<string, string[]>();
    }

    private sealed class FlatErrorsEnvelope
    {
        [JsonPropertyName("errors")]
        public IReadOnlyList<string> Errors { get; init; } = [];
    }

    private sealed class ServerErrorEnvelope
    {
        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;

        [JsonPropertyName("error")]
        public string? Error { get; init; }
    }
}
