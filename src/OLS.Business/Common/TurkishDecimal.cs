using System.Globalization;

namespace OLS.Business.Common;

/// <summary>
/// Türkçe ondalık girişini sayıya çevirir.
///
/// Frontend sayısal alanları FormData ile METİN olarak gönderiyor ve kullanıcı
/// Türkçe klavyede virgül kullanıyor. ASP.NET'in varsayılan model bağlayıcısı
/// invariant kültürle çalıştığı için "32,5" değerini 325 olarak okur — yani
/// hacim, lademetre, ağırlık gibi alanlar 10 kat büyür.
///
/// olsold tarafında da benzer bir sorun vardı: fiyatlarda yalnızca
/// <c>str_replace(',', '.')</c> yapılıyor, binlik ayraçlı giriş ("1.850,75")
/// "1.850.75" olup PHP'de 1.85'e düşüyordu.
///
/// Bu yüzden sayısal form alanları <c>string</c> olarak alınır ve burada
/// ayrıştırılır: son ayraç ondalık kabul edilir, öncekiler binlik ayraç sayılır.
/// </summary>
public static class TurkishDecimal
{
    /// <summary>"1234.56", "1234,56", "1.234,56" ve "1,234.56" kabul edilir.</summary>
    public static decimal? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var text = value.Trim();

        var lastComma = text.LastIndexOf(',');
        var lastDot = text.LastIndexOf('.');
        var separatorIndex = Math.Max(lastComma, lastDot);

        string normalized;

        if (separatorIndex < 0)
        {
            normalized = text;
        }
        else
        {
            var integerPart = text[..separatorIndex]
                .Replace(".", string.Empty)
                .Replace(",", string.Empty);

            var fractionPart = text[(separatorIndex + 1)..];

            // Tek ayraç ve ardından tam 3 hane varsa bu binlik ayraçtır ("1.250").
            var separatorCount = text.Count(c => c is '.' or ',');

            normalized = fractionPart.Length == 3 && separatorCount == 1
                ? integerPart + fractionPart
                : $"{integerPart}.{fractionPart}";
        }

        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    /// <summary>Tam sayı alanları için (adet vb.).</summary>
    public static int? ParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return int.TryParse(value.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : (int?)Parse(value);
    }
}
