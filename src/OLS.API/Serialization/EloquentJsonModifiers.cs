using System.Collections;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using OLS.DataAccess.Entities;

namespace OLS.API.Serialization;

/// <summary>
/// Eloquent'in iki serileştirme davranışını System.Text.Json'a taşır.
///
/// 1) <c>$hidden</c> — modelde gizli işaretlenen alanlar JSON'a yazılmaz.
/// 2) Yüklenmemiş ilişkiler — Eloquent eager-load edilmemiş ilişkiyi çıktıya
///    hiç koymaz. EF'te navigation koleksiyonları boş liste olarak durur ve
///    varsayılan ayarla <c>"ordinos": []</c> gibi onlarca gereksiz alan üretir.
/// </summary>
public static class EloquentJsonModifiers
{
    /// <summary>
    /// Laravel model'lerindeki <c>protected $hidden</c> karşılığı.
    ///
    /// User: olsold'da <c>['password', 'remember_token']</c> gizliydi.
    /// <c>email_password</c> gizli DEĞİLDİ — yani olsold kullanıcının IMAP posta
    /// şifresini API yanıtında istemciye gönderiyordu. Bunu bilerek taşımıyoruz.
    /// </summary>
    private static readonly Dictionary<Type, HashSet<string>> Hidden = new()
    {
        [typeof(User)] =
        [
            nameof(User.Password),
            nameof(User.RememberToken),
            nameof(User.EmailPassword),
            // Dahili jeton tablosu; API yanıtında yeri yok. Login sırasında
            // EF fixup bu koleksiyonu doldurduğu için açıkça gizliyoruz.
            nameof(User.RevokedTokens),
        ],
    };

    public static void ApplyHiddenProperties(JsonTypeInfo typeInfo)
    {
        if (!Hidden.TryGetValue(typeInfo.Type, out var hiddenNames))
            return;

        foreach (var property in typeInfo.Properties)
        {
            // property.Name JSON adıdır (snake_case dönüşümünden sonra).
            // CLR adıyla eşleştirmek için AttributeProvider üzerinden PropertyInfo'ya bakıyoruz.
            var clrName = (property.AttributeProvider as PropertyInfo)?.Name;

            if (clrName is not null && hiddenNames.Contains(clrName))
                property.ShouldSerialize = static (_, _) => false;
        }
    }

    /// <summary>
    /// Boş navigation koleksiyonlarını çıktıdan düşürür; yalnızca gerçekten
    /// Include edilmiş (dolu) ilişkiler yazılır. Böylece yanıt şekli Eloquent'in
    /// <c>with(...)</c> davranışına yaklaşır.
    /// </summary>
    public static void SkipEmptyCollections(JsonTypeInfo typeInfo)
    {
        // Yalnızca entity tiplerine uygula; DTO ve zarf sınıflarına dokunma.
        if (typeInfo.Type.Namespace != typeof(User).Namespace)
            return;

        foreach (var property in typeInfo.Properties)
        {
            if (property.PropertyType == typeof(string) ||
                !typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
            {
                continue;
            }

            property.ShouldSerialize = static (_, value) =>
                value is ICollection { Count: > 0 };
        }
    }
}
