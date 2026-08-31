using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class User
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string Surname { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public Guid? CountryId { get; set; }

    public Guid? PhoneCountryId { get; set; }

    public DateTime? EmailVerifiedAt { get; set; }

    public string? Password { get; set; }

    public string? Avatar { get; set; }

    public bool NotificationMail { get; set; }

    public bool NotificationSms { get; set; }

    public bool Status { get; set; }

    public string? SiberId { get; set; }

    public string? SiberName { get; set; }

    public string? SiberCode { get; set; }

    /// <summary>
    /// Uygulanmış yetki şablonu (bkz. <see cref="Role"/>). Yetki kontrolü hâlâ
    /// user_permissions satırlarını okur; bu alan yalnızca hangi şablonun
    /// uygulandığını kaydeder, böylece rol güncellenince kimlere yeniden
    /// uygulanacağı bilinir.
    /// </summary>
    public long? RoleId { get; set; }

    /// <summary>
    /// Siber'de hesap engelli mi (sky_kullanici.engelle). İşten ayrılmış çalışanlar
    /// burada true olur; senkron bu kullanıcıların <see cref="Status"/> alanını
    /// kapatır. Canlıda 131 yerel kullanıcının 81'i bu durumdaydı — hepsi ortak
    /// şifreyle giriş yapabiliyordu.
    /// </summary>
    public bool? SiberBlocked { get; set; }

    /// <summary>Siber'deki departman adı — rol ataması bunun üzerinden yapılır.</summary>
    public string? SiberDepartmentName { get; set; }

    /// <summary>
    /// Kullanıcının GÖRME kapsamı: hangi Siber şirketinin kayıtlarını görür.
    ///
    /// Rol DEĞİL bilinçli olarak ayrı bir alan: rol "ne YAPABİLİR" (yetki
    /// şablonu), bu ise "ne GÖREBİLİR" (veri filtresi). Rolle birleştirilseydi
    /// her rolün şirket başına kopyası gerekirdi (Satış-Avrora, Satış-OLS…).
    ///
    /// Dolu ise kullanıcı YALNIZCA o şirketin yük/seferlerini görür.
    /// Boş ise Avrora kayıtları HARİÇ hepsini görür. Süper admin her şeyi görür.
    /// </summary>
    public string? SiberCompanyId { get; set; }

    public string? EmailId { get; set; }

    public string? EmailPassword { get; set; }

    public bool WorkingTracking { get; set; }

    public string? PkdsId { get; set; }

    public string? RememberToken { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ExpeditionMovement> ExpeditionMovements { get; set; } = new List<ExpeditionMovement>();

    public virtual ICollection<LoadTransferMovement> LoadTransferMovements { get; set; } = new List<LoadTransferMovement>();

    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
