using System.Text.Json.Serialization;

namespace OLS.Business.Common;

/// <summary>
/// Bir kaydın Siber'deki izleri: kim açtı, kim en son dokundu, silindi mi.
///
/// Kullanıcı hem KOD hem AD olarak taşınır. Siber kullanıcıyı koduyla tutuyor
/// ve bu kodların 91'inden 3'ü yerel <c>users</c> tablosunda karşılık bulmuyor
/// (ayrılmış personel, "OLS" sistem hesabı). Ad boş kalsa bile kod gösterilerek
/// "kim yaptı" sorusu cevapsız kalmıyor.
///
/// "Kim açtı" Siber'de teklif/yük/sefer için %100 dolu; "kim son dokundu"
/// sırasıyla %81, %85 ve %30 dolu — arayüz bu alanların boş olabileceğini
/// varsaymalı.
/// </summary>
public sealed class SiberAuditDto
{
    [JsonPropertyName("created_by_code")] public string? CreatedByCode { get; init; }

    [JsonPropertyName("created_by_name")] public string? CreatedByName { get; init; }

    [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; init; }

    [JsonPropertyName("updated_by_code")] public string? UpdatedByCode { get; init; }

    [JsonPropertyName("updated_by_name")] public string? UpdatedByName { get; init; }

    [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; init; }

    /// <summary>Kaydın Siber'de bulunamadığının fark edildiği an; null ise kayıt duruyor.</summary>
    [JsonPropertyName("deleted_at")] public DateTime? DeletedAt { get; init; }

    /// <summary>
    /// Gösterilecek hiçbir iz yoksa NULL döner. Boş bir nesne döndürmek,
    /// arayüzde bilgi varmış gibi duran boş bir kutu bırakıyordu.
    /// </summary>
    public static SiberAuditDto? From(
        string? createdByCode, string? createdByName, DateTime? createdAt,
        string? updatedByCode, string? updatedByName, DateTime? updatedAt,
        DateTime? deletedAt)
    {
        var empty =
            string.IsNullOrWhiteSpace(createdByCode) &&
            string.IsNullOrWhiteSpace(createdByName) &&
            string.IsNullOrWhiteSpace(updatedByCode) &&
            string.IsNullOrWhiteSpace(updatedByName) &&
            createdAt is null && updatedAt is null && deletedAt is null;

        return empty ? null : new SiberAuditDto
        {
            CreatedByCode = createdByCode,
            CreatedByName = createdByName,
            CreatedAt = createdAt,
            UpdatedByCode = updatedByCode,
            UpdatedByName = updatedByName,
            UpdatedAt = updatedAt,
            DeletedAt = deletedAt,
        };
    }
}
