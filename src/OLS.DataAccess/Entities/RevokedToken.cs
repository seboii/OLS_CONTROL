namespace OLS.DataAccess.Entities;

/// <summary>
/// Çıkış yapılmış (iptal edilmiş) JWT kayıtları.
///
/// olsold Laravel Passport kullanıyordu ve <c>logout</c> sunucu tarafında
/// <c>token()->revoke()</c> ile jetonu geçersiz kılıyordu. JWT durumsuz olduğu
/// için aynı davranışı korumak adına jetonun <c>jti</c> değerini saklıyoruz.
///
/// Bu tablo scaffold edilmedi; code-first migration ile eklendi.
/// </summary>
public partial class RevokedToken
{
    public long Id { get; set; }

    /// <summary>JWT'nin benzersiz kimliği (jti claim'i).</summary>
    public string Jti { get; set; } = null!;

    public long UserId { get; set; }

    /// <summary>Jetonun doğal sona erme zamanı. Süresi geçenler temizlenebilir.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Null ise jeton hâlâ geçerli; dolu ise çıkış yapılmış.</summary>
    public DateTime? RevokedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
