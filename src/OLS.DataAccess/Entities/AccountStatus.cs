using System;

namespace OLS.DataAccess.Entities;

/// <summary>
/// FİRMA DURUMU — Siber'de <c>sbr_firmadurum</c>.
///
/// Siber firmayı ikiye ayırıyor ve bu ayrım fiilen kullanılıyor:
/// <b>CARİ FİRMALAR</b> (4.250 firma) ve <b>DİĞER FİRMALAR</b> (3.212).
/// Uygulama bu alanı hiç sormuyor, <c>sbr_firma.firmadurumid</c>'ye SABİT
/// olarak "CARİ FİRMALAR" yazıyordu — yani programdan açılan her firma Siber'de
/// cari olarak görünüyordu.
///
/// Tablo Siber'den aynalanır, yerelde satır AÇILMAZ: seçenek listesinin
/// Siber'de karşılığı olmayan bir satır taşıması, kaydetme anında sessiz veri
/// bozulmasına dönüşürdü.
/// </summary>
public partial class AccountStatus
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    /// <summary>Siber karşılığı (<c>sbr_firmadurum.firmadurumid</c>).</summary>
    public string? SiberId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
