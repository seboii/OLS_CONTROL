namespace OLS.Business.Services.Authorization;

/// <summary>
/// Programın KULLANDIĞI yetki sayfalarının tek kaynağı.
///
/// Bir slug burada listeliyse kodda gerçekten kontrol ediliyor demektir:
/// ya bir <c>[RequiresPermission]</c> özniteliğinde, ya bir tanım
/// denetleyicisinin <c>PermissionSlug</c>'ında, ya menüde
/// (<c>Sidebar.NAV_ITEMS</c>), ya da doğrudan
/// <c>IPermissionService.HasPermissionAsync</c> çağrısında.
///
/// İKİ YERDE KULLANILIR ve ikisi de kritik:
///
/// 1. <c>DbSeeder</c> bu listeyi tohumlar. Eksik bir slug tohumlanmazsa
///    arayüzde o modül hiç görünmez (frontend bilinmeyen slug'ı REDDEDER).
///
/// 2. <c>PermissionPageService.DeleteAsync</c> bu listedekileri SİLDİRMEZ.
///    Sebep ters yönde ve daha tehlikeli: <c>PermissionService</c> bulunamayan
///    bir slug için <b>true</b> döner ("tanımsız sayfa serbest", olsold'un
///    RoleHelper davranışı). Yani <c>load_management</c> silinseydi yük ve
///    teklif uçları yetki kontrolü olmadan HERKESE açılırdı.
/// </summary>
public static class PermissionPages
{
    /// <summary>Sırası ekranda görünen sıradır.</summary>
    public static readonly (string Slug, string Name)[] All =
    [
        // AccountService.IsSuperAdminAsync bu slug'ı arar: Read=1 olan
        // kullanıcı tüm carileri görür. Seed edilmezse KİMSE süper admin olamaz.
        ("super_admin", "Süper Admin"),
        // Denetim kaydı sayfası — yalnızca Yönetim rolüne verilir (RoleCatalog).
        ("audit_log_management", "Denetim Kaydı"),
        ("account_management", "Cari Yönetimi"),
        ("account_type_management", "Müşteri Tipi Yönetimi"),
        ("load_management", "Yük/Teklif Yönetimi"),
        ("expedition_management", "Sefer Yönetimi"),
        ("invoice_management", "Fatura Yönetimi"),
        ("invoice_type_management", "Fatura Tipi/Durumu Yönetimi"),
        ("car_management", "Araç Yönetimi"),
        ("case_type_management", "Kap Tipi Yönetimi"),
        ("payment_management", "Ödeme Tipi Yönetimi"),
        ("transport_type_management", "Taşıma Tipi Yönetimi"),
        ("loading_type_management", "Yükleme Tipi Yönetimi"),
        ("work_type_management", "İş Tipi Yönetimi"),
        ("status_type_management", "Durum Tipi Yönetimi"),
        ("department_management", "Departman Yönetimi"),
        ("product_type_management", "Ürün Tipi Yönetimi"),
        ("financial_item_management", "Mali Kalem Yönetimi"),
        ("financial_item_type_management", "Mali Kalem Tipi Yönetimi"),
        ("movement_type_management", "Hareket Tipi Yönetimi"),
        ("currency_management", "Para Birimi Yönetimi"),
        ("user_management", "Kullanıcı Yönetimi"),
        ("role_management", "Rol/Yetki Yönetimi"),
        ("support_request_management", "Destek Talebi Yönetimi"),
        ("report_management", "Raporlama Yönetimi"),
        // Finans ve muhasebe AYRI sayfalar: operasyonun cari bakiyeyi
        // görmesi gerekir ama yevmiye defterini görmesi gerekmez.
        ("finance_management", "Finans Yönetimi"),
        ("accounting_management", "Muhasebe Yönetimi"),
    ];

    private static readonly HashSet<string> Slugs =
        All.Select(p => p.Slug).ToHashSet(StringComparer.Ordinal);

    /// <summary>Bu slug programın kullandığı bir sayfa mı (yani silinemez mi).</summary>
    public static bool IsUsedByProgram(string? slug) =>
        slug is not null && Slugs.Contains(slug);
}
