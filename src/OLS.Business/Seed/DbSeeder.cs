using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OLS.Business.Services.Authentication;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.Business.Seed;

/// <summary>
/// Temiz bir veritabanı kurulumunu çalışır hale getiren idempotent seed.
/// Her bölüm "yoksa ekle" mantığıyla çalışır — tekrar çalıştırmak güvenlidir
/// ve mevcut/özelleştirilmiş veriyi silmez/ezmez.
///
/// Kapsam bilinçli olarak dardır: 8 modülün asgari çalışması için gereken
/// yetki sayfaları, doğru <see cref="StatusType"/> eşlemesi (DATA-002 düzeltmesi,
/// bkz. docs/SECILI-MODUL-PARITE-MATRISI.md) ve az sayıda temel tanım verisi.
/// Bu, olsold/Siber'den gelecek tam referans verisinin (245 ülke, tam para
/// birimi listesi vb.) yerini TUTMAZ — gerçek dağıtımda gerekli.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(
        OlsDbContext db,
        IPasswordHasher hasher,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await SeedPermissionPagesAsync(db, cancellationToken);
        await SeedStatusTypesAsync(db, cancellationToken);
        await SeedCoreLookupsAsync(db, cancellationToken);
        await SeedAdminUserAsync(db, hasher, configuration, environment, logger, cancellationToken);
    }

    // ------------------------------------------------------------------
    // Yetki sayfaları — 8 modül + ortak altyapının kullandığı TÜM slug'lar.
    // Eksik bir slug varsayılan-reddet davranışına düşer (PermissionService),
    // yani burada listelenmeyen bir sayfa "izin ver" değil "reddet" olur.
    // ------------------------------------------------------------------
    private static async Task SeedPermissionPagesAsync(OlsDbContext db, CancellationToken ct)
    {
        (string Slug, string Name)[] pages =
        [
            // AccountService.IsSuperAdminAsync bu slug'ı arar: Read=1 olan
            // kullanıcı tüm carileri görür, aksi halde yalnızca kendisine
            // user_account_mappings ile atanmış carileri görür (object-level
            // kural, olsold'dan birebir). Seed edilmezse KİMSE süper admin
            // olamaz ve hiçbir cari hiçbir yeni kullanıcıya görünmez.
            ("super_admin", "Süper Admin"),
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
        ];

        var existingSlugs = await db.UserPermissionPages
            .Select(p => p.PermissionPageSlug)
            .ToListAsync(ct);
        var existingSet = existingSlugs.ToHashSet(StringComparer.Ordinal);

        var toAdd = pages
            .Where(p => !existingSet.Contains(p.Slug))
            .Select(p => new UserPermissionPage
            {
                PermissionPageSlug = p.Slug,
                PermissionPageName = p.Name,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
            })
            .ToList();

        if (toAdd.Count > 0)
        {
            db.UserPermissionPages.AddRange(toAdd);
            await db.SaveChangesAsync(ct);
        }
    }

    // ------------------------------------------------------------------
    // status_types — DATA-002 düzeltmesi.
    //
    // olsold'un StatusTypeSeeder çıktısı (Teklif/İşlemde/Onaylandı/Tamamlandı/
    // İptal) gerçek çalışma zamanı anlamıyla ÇELİŞİYORDU. Gerçek anlam, kodun
    // her yerinde tekrarlanan literal karşılaştırmalardan çıkarıldı:
    //   1=Olumsuz, 2=Sipariş, 3=Düzeltme Talebi, 4=Teklif, 5=Olumlu
    // `Number` sütunu (önceden hiç kullanılmıyordu) artık kararlı bir metin
    // kod olarak dolduruluyor — yeni kod bu koda göre yazılmalı, ham id'ye
    // güvenilmemeli (bkz. StatusTypeCodes).
    // ------------------------------------------------------------------
    private static async Task SeedStatusTypesAsync(OlsDbContext db, CancellationToken ct)
    {
        (string Code, string Name)[] statuses =
        [
            (StatusTypeCodes.Rejected, "Olumsuz"),
            (StatusTypeCodes.Order, "Sipariş"),
            (StatusTypeCodes.Correction, "Düzeltme Talebi"),
            (StatusTypeCodes.Offer, "Teklif"),
            (StatusTypeCodes.Approved, "Olumlu"),
        ];

        var existingCodes = await db.StatusTypes
            .Select(s => s.Number)
            .ToListAsync(ct);
        var existingSet = existingCodes.Where(c => c != null).ToHashSet(StringComparer.Ordinal);

        var toAdd = statuses
            .Where(s => !existingSet.Contains(s.Code))
            .Select(s => new StatusType
            {
                Number = s.Code,
                Name = s.Name,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
            })
            .ToList();

        if (toAdd.Count > 0)
        {
            db.StatusTypes.AddRange(toAdd);
            await db.SaveChangesAsync(ct);
        }
    }

    // ------------------------------------------------------------------
    // Asgari çalışan tanım verisi. Tam referans veri seti (245 ülke vb.)
    // değildir — sadece 8 modülün formları boş dropdown göstermesin diyedir.
    // ------------------------------------------------------------------
    private static async Task SeedCoreLookupsAsync(OlsDbContext db, CancellationToken ct)
    {
        await SeedIfEmptyAsync(db.Currencies, ct, () =>
        [
            new Currency { Name = "TÜRK LİRASI", Symbol = "₺", Code = "TL", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new Currency { Name = "ABD DOLARI", Symbol = "$", Code = "USD", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new Currency { Name = "EURO", Symbol = "€", Code = "EUR", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new Currency { Name = "İNGİLİZ STERLİNİ", Symbol = "£", Code = "GBP", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        await SeedIfEmptyAsync(db.AccountTypes, ct, () =>
        [
            new AccountType { Name = "Müşteri", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new AccountType { Name = "Tedarikçi", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new AccountType { Name = "Alıcı", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new AccountType { Name = "Gönderici", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new AccountType { Name = "Acente", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        await SeedIfEmptyAsync(db.WorkTypes, ct, () =>
        [
            new WorkType { Name = "İhracat", Code = "IHR", GroupCode = "ISTURU", AdditionalCode = "IHR", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new WorkType { Name = "İthalat", Code = "ITH", GroupCode = "ISTURU", AdditionalCode = "ITH", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new WorkType { Name = "Transit", Code = "TRN", GroupCode = "ISTURU", AdditionalCode = "TRN", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        await SeedIfEmptyAsync(db.Departments, ct, () =>
        [
            new Department { Name = "Operasyon", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new Department { Name = "Satış", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new Department { Name = "Muhasebe", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        await SeedIfEmptyAsync(db.PaymentTypes, ct, () =>
        [
            new PaymentType { Name = "Peşin", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new PaymentType { Name = "Vadeli", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        await SeedIfEmptyAsync(db.LoadingTypes, ct, () =>
        [
            new LoadingType { Name = "Komple", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new LoadingType { Name = "Parsiyel", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        await SeedIfEmptyAsync(db.CarTypes, ct, () =>
        [
            new CarType { Name = "Tır", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new CarType { Name = "Kamyon", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        await SeedIfEmptyAsync(db.RomorkTypes, ct, () =>
        [
            new RomorkType { Name = "Tenteli", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new RomorkType { Name = "Frigo", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new RomorkType { Name = "Lowbed", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        await SeedIfEmptyAsync(db.CarOwners, ct, () =>
        [
            new CarOwner { Name = "Öz Mal", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new CarOwner { Name = "Anlaşmalı", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        await SeedIfEmptyAsync(db.CarStatusTypes, ct, () =>
        [
            new CarStatusType { Name = "Boşta", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new CarStatusType { Name = "Seferde", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        await SeedIfEmptyAsync(db.InvoiceTypes, ct, () =>
        [
            new InvoiceType { Name = "Satış", Code = "0", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new InvoiceType { Name = "İade", Code = "1", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        await SeedIfEmptyAsync(db.InvoiceStatuses, ct, () =>
        [
            new InvoiceStatus { Name = "Taslak", Code = "0", EnumValue = "Draft", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new InvoiceStatus { Name = "Onaylandı", Code = "1000", EnumValue = "Approved", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new InvoiceStatus { Name = "Onay Bekliyor", Code = "1100", EnumValue = "WaitingForApprovement", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new InvoiceStatus { Name = "Reddedildi", Code = "1200", EnumValue = "Declined", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        await db.SaveChangesAsync(ct);

        if (!await db.Countries.AnyAsync(ct))
        {
            db.Countries.AddRange(
                new Country { Id = Guid.NewGuid(), Name = "Türkiye", CountryCode = "TR", PhoneCode = "90", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new Country { Id = Guid.NewGuid(), Name = "Almanya", CountryCode = "DE", PhoneCode = "49", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new Country { Id = Guid.NewGuid(), Name = "Rusya", CountryCode = "RU", PhoneCode = "7", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now });
            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task SeedIfEmptyAsync<T>(
        DbSet<T> set, CancellationToken ct, Func<T[]> factory) where T : class
    {
        if (!await set.AnyAsync(ct))
        {
            set.AddRange(factory());
        }
    }

    // ------------------------------------------------------------------
    // Geliştirme admin kullanıcısı.
    //
    // Bilgiler ortam değişkeninden okunur (Seed:AdminEmail / Seed:AdminPassword).
    // Production ortamında (IsDevelopment() == false) HİÇBİR sabit varsayılan
    // parola kullanılmaz — env değişkeni verilmemişse admin seed atlanır.
    // ------------------------------------------------------------------
    private static async Task SeedAdminUserAsync(
        OlsDbContext db,
        IPasswordHasher hasher,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger logger,
        CancellationToken ct)
    {
        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];

        if (environment.IsDevelopment())
        {
            email ??= "admin@ols-scoped.local";
            password ??= "ChangeMe!Dev1";
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation(
                "Seed:AdminEmail/Seed:AdminPassword tanımlı değil — admin kullanıcı seed edilmedi.");
            return;
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
        {
            user = new User
            {
                Name = "Sistem",
                Surname = "Yöneticisi",
                Email = email,
                Password = hasher.Hash(password),
                Status = true,
                NotificationMail = false,
                NotificationSms = false,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
            };
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);

            if (!environment.IsDevelopment())
                logger.LogWarning(
                    "Admin kullanıcı Seed:AdminEmail/Seed:AdminPassword ile oluşturuldu ({Email}). " +
                    "İlk girişten sonra parolayı değiştirin.", email);
        }

        // Her sayfada tam yetki — FrontUserController::save'in yeni-kullanıcı
        // davranışının tersi (orada sıfır yetki, bilinçli); admin için tam
        // yetki tek seferlik bootstrap amaçlı.
        var pageIds = await db.UserPermissionPages.Select(p => p.Id).ToListAsync(ct);
        var existingPermissionPageIds = await db.UserPermissions
            .Where(p => p.UserId == user.Id)
            .Select(p => p.UserPermissionPageId)
            .ToListAsync(ct);
        var existingSet = existingPermissionPageIds.ToHashSet();

        var missing = pageIds.Where(id => !existingSet.Contains(id)).ToList();
        if (missing.Count > 0)
        {
            db.UserPermissions.AddRange(missing.Select(pageId => new UserPermission
            {
                UserId = user.Id,
                UserPermissionPageId = pageId,
                Read = 1,
                Create = 1,
                Update = 1,
                Delete = 1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
            }));
            await db.SaveChangesAsync(ct);
        }
    }
}

/// <summary>
/// <see cref="StatusType"/> için kararlı kodlar. Ham sayısal id'ye asla
/// güvenmeyin — bkz. docs/SECILI-MODUL-PARITE-MATRISI.md, DATA-002.
/// </summary>
public static class StatusTypeCodes
{
    public const string Rejected = "REJECTED";
    public const string Order = "ORDER";
    public const string Correction = "CORRECTION";
    public const string Offer = "OFFER";
    public const string Approved = "APPROVED";
}
