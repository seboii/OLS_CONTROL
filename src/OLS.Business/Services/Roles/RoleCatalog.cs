namespace OLS.Business.Services.Roles;

/// <summary>Bir yetki sayfası üzerindeki CRUD şablonu.</summary>
public readonly record struct PagePermission(string Slug, bool Read, bool Create, bool Update, bool Delete)
{
    public static PagePermission ReadOnly(string slug) => new(slug, true, false, false, false);
    public static PagePermission Full(string slug) => new(slug, true, true, true, true);
    public static PagePermission ReadWrite(string slug) => new(slug, true, true, true, false);
}

public sealed record RoleDefinition(
    string Slug, string Name, string Description, bool IsDefault, IReadOnlyList<PagePermission> Pages);

/// <summary>
/// Rol kataloğu — Siber'deki DEPARTMANLARDAN türetilmiştir. <see cref="RoleDefinition.Slug"/>
/// değeri departman adının normalize edilmiş hâlidir; kullanıcıya rol atanırken
/// sky_kullanici.departmanid → sbr_departman.ad üzerinden eşleşir
/// (bkz. <c>UserRoleAssignmentService</c>).
///
/// Kullanıcı kararları (bu katalog onlara göre kuruldu):
///   • Raporlama YALNIZCA Yönetim rolünde açık.
///   • Kullanıcı/Rol yönetimi ve süper admin yalnızca Yönetim'de.
///   • Normal kullanıcıda görünmesi istenenler: Müşteri (cari), Teklif/Yük,
///     Sefer, Fatura, Araç.
///
/// TANIM SAYFALARI neden her rolde okunabilir: Araç formundaki "Sahiplik Durumu",
/// Sefer formundaki "Departman", Yük formundaki "Kap Tipi" gibi açılır listeler
/// bu sayfaların yetkisiyle korunuyor. Okuma yetkisi verilmezse uç 403 dönüyor ve
/// arayüz listeyi SESSİZCE boş gösteriyor — canlıda tam olarak bu yaşandı
/// ("bu seçenekler yok ki"). Bu yüzden tanım sayfaları tüm rollerde en az okuma.
/// </summary>
public static class RoleCatalog
{
    /// <summary>Ana iş modülleri.</summary>
    public const string Account = "account_management";
    public const string Load = "load_management";
    public const string Expedition = "expedition_management";
    public const string Invoice = "invoice_management";
    public const string Car = "car_management";
    public const string Support = "support_request_management";

    /// <summary>Yalnızca Yönetim'e açık olanlar.</summary>
    public const string Report = "report_management";
    public const string UserManagement = "user_management";
    public const string RoleManagement = "role_management";
    public const string SuperAdmin = "super_admin";

    /// <summary>Denetim kaydı — kimin ne yaptığı. Yalnızca Yönetim rolünde.</summary>
    public const string AuditLog = "audit_log_management";

    /// <summary>Cari bakiye/ekstre, fatura, tahsilat-ödeme.</summary>
    public const string Finance = "finance_management";

    /// <summary>Yevmiye fişi, mizan, hesap planı — defter ekranları.</summary>
    public const string Accounting = "accounting_management";

    /// <summary>Açılır listeleri besleyen tanım sayfaları — her rolde okunabilir.</summary>
    public static readonly string[] DefinitionPages =
    [
        "account_type_management", "invoice_type_management", "case_type_management",
        "payment_management", "transport_type_management", "loading_type_management",
        "work_type_management", "status_type_management", "department_management",
        "product_type_management", "financial_item_management", "financial_item_type_management",
        "movement_type_management", "currency_management",
    ];

    private static IReadOnlyList<PagePermission> Compose(params PagePermission[] explicitPages)
    {
        var pages = new List<PagePermission>(explicitPages);
        var named = explicitPages.Select(p => p.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var slug in DefinitionPages)
            if (!named.Contains(slug))
                pages.Add(PagePermission.ReadOnly(slug));

        return pages;
    }

    public static IReadOnlyList<RoleDefinition> All { get; } =
    [
        new("yonetim", "Yönetim",
            "Tüm modüller, raporlama ve kullanıcı/rol yönetimi.", false,
            ManagementPages()),

        new("satis-pazarlama", "Satış & Pazarlama",
            "Cari ve teklif/yük üzerinde tam yetki; sefer, fatura ve araç okunur.", false,
            Compose(
                PagePermission.Full(Account), PagePermission.Full(Load),
                PagePermission.ReadOnly(Expedition), PagePermission.ReadOnly(Invoice),
                PagePermission.ReadOnly(Finance),
                PagePermission.ReadOnly(Car), PagePermission.Full(Support))),

        new("ihracat-operasyon", "İhracat Operasyon",
            "Yük ve sefer üzerinde tam yetki; cari ve fatura okunur.", false,
            OperationsPages()),

        new("ithalat-operasyon", "İthalat Operasyon",
            "Yük ve sefer üzerinde tam yetki; cari ve fatura okunur.", false,
            OperationsPages()),

        new("transit-operasyon", "Transit Operasyon",
            "Yük ve sefer üzerinde tam yetki; cari ve fatura okunur.", false,
            OperationsPages()),

        new("muhasebe-finans", "Muhasebe & Finans",
            "Fatura üzerinde tam yetki; cari güncellenebilir, yük/sefer okunur.", false,
            Compose(
                PagePermission.Full(Invoice), PagePermission.Full(Account),
                PagePermission.Full(Finance), PagePermission.Full(Accounting),
                PagePermission.ReadOnly(Load), PagePermission.ReadOnly(Expedition),
                PagePermission.ReadOnly(Car), PagePermission.Full(Support))),

        new("idari-isler", "İdari İşler",
            "Araç üzerinde tam yetki; diğer iş modülleri okunur.", false,
            Compose(
                PagePermission.Full(Car), PagePermission.Full(Account),
                PagePermission.ReadOnly(Load), PagePermission.ReadOnly(Expedition),
                PagePermission.ReadOnly(Invoice), PagePermission.Full(Support))),

        // Departmanı olmayan kullanıcılar buraya düşer. Yalnızca okuma:
        // yetkisiz bırakmak ekranları tamamen boş gösterirdi, yazma vermek ise
        // hangi işi yaptığı bilinmeyen 26 kullanıcıya fazla yetki olurdu.
        new("standart", "Standart Kullanıcı",
            "Departmanı tanımlı olmayan kullanıcılar. İş modülleri yalnızca okunur.", true,
            Compose(
                // Departmanı bilinmeyen kullanıcı da müşteri açabilmeli, ama
                // SİLME hakkı verilmiyor: kimin hangi işi yaptığı belli değil.
                PagePermission.ReadWrite(Account), PagePermission.ReadOnly(Load),
                PagePermission.ReadOnly(Expedition), PagePermission.ReadOnly(Invoice),
                PagePermission.ReadOnly(Car), PagePermission.Full(Support))),
    ];

    /// <summary>Yönetim: her sayfada tam yetki.</summary>
    private static IReadOnlyList<PagePermission> ManagementPages()
    {
        var pages = new List<PagePermission>
        {
            PagePermission.Full(SuperAdmin), PagePermission.Full(Account),
            PagePermission.Full(Load), PagePermission.Full(Expedition),
            PagePermission.Full(Invoice), PagePermission.Full(Car),
            PagePermission.Full(Support), PagePermission.Full(Report),
            PagePermission.Full(UserManagement), PagePermission.Full(RoleManagement),
            PagePermission.Full(AuditLog),
            PagePermission.Full(Finance), PagePermission.Full(Accounting),
        };

        pages.AddRange(DefinitionPages.Select(PagePermission.Full));
        return pages;
    }

    private static IReadOnlyList<PagePermission> OperationsPages() => Compose(
        PagePermission.Full(Load), PagePermission.Full(Expedition),
        PagePermission.ReadWrite(Car),
        // Cari TAM yetki: operasyon yeni müşteri açıyor ve kayıt düzeltiyor;
        // salt-okunur bırakmak günlük işi bloke ediyordu.
        PagePermission.Full(Account),
        PagePermission.ReadOnly(Invoice), PagePermission.ReadOnly(Finance),
        PagePermission.Full(Support));

    /// <summary>
    /// Siber departman adı → rol slug'ı. Karşılaştırma Türkçe normalizasyonlu
    /// yapılır (bkz. QueryableExtensions.NormalizeTurkish), çünkü Siber adları
    /// BÜYÜK harfle ("İHRACAT OPERASYON"), yerel tanım tablosu ise başlık
    /// düzeninde ("İhracat Operasyon") tutuyor.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> DepartmentToRoleSlug =
        new Dictionary<string, string>
        {
            // DİKKAT: anahtarlar NormalizeTurkish ÇIKTISIDIR. O metot yalnızca
            // İ/I/ı harflerini sadeleştirip küçültüyor; ş/ö/ü/ç/ğ AYNEN KALIYOR.
            // İlk sürümde anahtarlar "satis & pazarlama" / "yonetim" diye
            // yazılmıştı ve bu iki departman hiç eşleşmedi — Satış & Pazarlama
            // ile Yönetim çalışanları yanlışlıkla Standart Kullanıcı rolüne
            // düşmüştü. Doğru karşılıklar aşağıda.
            ["yönetim"] = "yonetim",
            ["satiş & pazarlama"] = "satis-pazarlama",
            ["ihracat operasyon"] = "ihracat-operasyon",
            ["ithalat operasyon"] = "ithalat-operasyon",
            ["transit operasyon"] = "transit-operasyon",
            ["muhasebe & finans"] = "muhasebe-finans",
            ["idari işler"] = "idari-isler",
        };
}
