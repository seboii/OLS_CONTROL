namespace OLS.Business.Common;

/// <summary>
/// Laravel'in <c>now()</c> karşılığı.
///
/// NEDEN GEREKLİ: olsold <c>config/app.php</c> içinde
/// <c>'timezone' => 'Europe/Istanbul'</c> kullanıyor ve tüm <c>created_at</c> /
/// <c>updated_at</c> sütunları <c>timestamp WITHOUT time zone</c> tipinde —
/// yani veritabanında saat dilimi bilgisi olmayan İstanbul yerel saati duruyor.
///
/// .NET tarafında <c>DateTime.UtcNow</c> (Kind=Utc) bu sütunlara yazılamaz;
/// Npgsql "Cannot write DateTime with Kind=UTC to timestamp without time zone"
/// hatası verir. Bu yüzden Kind=Unspecified olan İstanbul saati üretiyoruz.
///
/// Kısacası: Laravel-dönemi tablolara yazarken <see cref="Now"/> kullanın.
/// Code-first eklenen tablolarda (ör. revoked_tokens, timestamptz) UTC serbest.
/// </summary>
public interface IClock
{
    /// <summary>Kind=Unspecified, Europe/Istanbul yerel saati.</summary>
    DateTime Now { get; }

    /// <summary>Gerçek UTC an — timestamptz sütunları için.</summary>
    DateTime UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    private static readonly TimeZoneInfo AppTimeZone = ResolveTimeZone();

    public DateTime Now => DateTime.SpecifyKind(
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, AppTimeZone),
        DateTimeKind.Unspecified);

    public DateTime UtcNow => DateTime.UtcNow;

    /// <summary>
    /// Windows ve Linux farklı saat dilimi kimlikleri kullanır. .NET 8+ ikisini de
    /// çözer, yine de her iki adı deniyoruz; bulunamazsa UTC'ye düşüyoruz.
    /// </summary>
    private static TimeZoneInfo ResolveTimeZone()
    {
        foreach (var id in new[] { "Europe/Istanbul", "Turkey Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // sıradakini dene
            }
            catch (InvalidTimeZoneException)
            {
                // sıradakini dene
            }
        }

        return TimeZoneInfo.Utc;
    }
}
