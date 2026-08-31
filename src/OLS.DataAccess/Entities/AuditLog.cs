namespace OLS.DataAccess.Entities;

/// <summary>
/// Denetim kaydı — kim, ne zaman, hangi kayıtta ne yaptı.
///
/// YALNIZCA KULLANICI EYLEMLERİ yazılır. Siber senkronu her turda on binlerce
/// satır güncelliyor (tek turda ~96.000 referans + ~38.000 mali kalem); bunlar
/// loglansaydı tablo günde milyonlarca satıra çıkar ve gerçek kullanıcı
/// hareketlerini görünmez hâle getirirdi. Ayrım basit ve güvenilir: senkron bir
/// arka plan servisi olarak çalıştığı için oturum açmış kullanıcısı YOKTUR
/// (<c>ICurrentUser.Id is null</c>), dolayısıyla interceptor onu atlar.
/// </summary>
public partial class AuditLog
{
    public long Id { get; set; }

    /// <summary>Eylemi yapan kullanıcı. Kullanıcı silinse bile kayıt kalsın diye FK yok.</summary>
    public long? UserId { get; set; }

    /// <summary>Kullanıcı adı ve e-postası, o ANDAKİ hâliyle (kullanıcı sonradan silinse de okunabilsin).</summary>
    public string? UserName { get; set; }

    /// <summary>created | updated | deleted</summary>
    public string Action { get; set; } = null!;

    /// <summary>Değişen kaydın türü — arayüzde "Yük", "Sefer", "Cari" gibi gösterilir.</summary>
    public string EntityType { get; set; } = null!;

    /// <summary>Değişen kaydın yerel kimliği.</summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Kaydın İNSAN TARAFINDAN ARANABİLİR etiketi: yük numarası, sefer numarası,
    /// cari adı, kullanıcı e-postası… Arama kutusu bu sütunda çalışır, çünkü
    /// kullanıcı "2600838TR" yazıp aradığında yerel id'yi bilmiyor.
    /// </summary>
    public string? EntityLabel { get; set; }

    /// <summary>Değişen alanların eski/yeni değerleri (JSON).</summary>
    public string? Changes { get; set; }

    /// <summary>İsteği yapan IP — aynı hesabın farklı yerlerden kullanımını ayırmak için.</summary>
    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; }
}
