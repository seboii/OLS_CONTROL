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

        if (environment.IsDevelopment())
            await SeedDemoConvenienceDataAsync(db, cancellationToken);
    }

    // ------------------------------------------------------------------
    // Geliştirme kolaylığı — YALNIZCA Development ortamında çalışır.
    //
    // Siber'den içe aktarılan referans verisi (financial_items, accounts)
    // başarıyla geldikten sonra bile bazı alanlar hâlâ boş kalır, çünkü
    // Siber'in kendi şeması bu alanları hiç taşımıyor:
    //   - financial_items.type (Alış=1/Satış=2): mock Siber'in skn_kalem
    //     tablosunda böyle bir sütun YOK; olsold'un kendi FinancialItemSeeder'ı
    //     da doldurmuyor — hem olsold hem "olimpikgama" (aynı ürünün başka
    //     bir müşteri dağıtımı, resources/js/data + database/seeders BİREBİR
    //     aynı) karşılaştırılarak doğrulandı. Sınıflandırılmamış hâliyle
    //     Teklif/Yük'ün "Kalem" seçicisi hiçbir zaman dolu görünmez — backend
    //     type=null'ı bilinçli olarak filtreden HARİÇ tutuyor (kaynakla
    //     birebir, bkz. LookupService.AllAsync).
    //   - account_type_id=5 (Acente) tipinde hiçbir cari gelmeyebilir, çünkü
    //     mock Siber örnek verisinde bir acente yok.
    // Bu, canlı Docker'da "Yeni Teklif" akışı denenirken keşfedildi: Kalem ve
    // Acente seçicileri "Sonuç bulunamadı" gösteriyordu. Mevcut/özelleştirilmiş
    // veriyi ASLA ezmez — yalnızca hiç örnek yoksa bir tane ekler.
    // ------------------------------------------------------------------
    private static async Task SeedDemoConvenienceDataAsync(OlsDbContext db, CancellationToken ct)
    {
        await ClassifyFinancialItemsAsync(db, ct);
        await EnsureAcenteAccountAsync(db, ct);
    }

    /// <summary>
    /// En az bir Alış(1) ve bir Satış(2) örneği olsun diye önce sınıflandırılmamış
    /// kalemlerden kullanır; hiç kalem yoksa (Siber import'u henüz hiç çalışmamış
    /// taze bir kurulum) birkaç temel kalemi ZATEN sınıflandırılmış olarak ekler.
    /// </summary>
    private static async Task ClassifyFinancialItemsAsync(OlsDbContext db, CancellationToken ct)
    {
        var hasBuy = await db.FinancialItems.AnyAsync(f => f.Type == 1, ct);
        var hasSell = await db.FinancialItems.AnyAsync(f => f.Type == 2, ct);
        if (hasBuy && hasSell)
            return;

        var unclassified = await db.FinancialItems
            .Where(f => f.Type == null)
            .OrderBy(f => f.Id)
            .ToListAsync(ct);

        foreach (var item in unclassified)
        {
            if (!hasBuy) { item.Type = 1; hasBuy = true; }
            else if (!hasSell) { item.Type = 2; hasSell = true; }
            else break;
        }

        if (!hasBuy)
            db.FinancialItems.Add(new FinancialItem { Name = "Gümrükleme (Demo)", Type = 1, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now });
        if (!hasSell)
            db.FinancialItems.Add(new FinancialItem { Name = "Navlun (Demo)", Type = 2, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now });

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Acente (account_type_id=5) tipinde hiç cari yoksa bir demo cari ekler.</summary>
    private static async Task EnsureAcenteAccountAsync(OlsDbContext db, CancellationToken ct)
    {
        var acenteTypeId = await db.AccountTypes
            .Where(t => t.Name == "Acente")
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync(ct);
        if (acenteTypeId is null)
            return;

        var hasAcente = await db.AccountTypeMappings.AnyAsync(m => m.AccountTypeId == acenteTypeId, ct);
        if (hasAcente)
            return;

        var account = new Account
        {
            Name = "Deniz Acente (Demo)",
            Email = "info@denizacente.test",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync(ct);

        db.AccountTypeMappings.Add(new AccountTypeMapping
        {
            AccountId = (int)account.Id,
            AccountTypeId = acenteTypeId,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        });
        await db.SaveChangesAsync(ct);
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

        // olsold'un kendi kodları ("IHR"/"ITH"/"TRN") gerçek Siber'in ne sayısal kod'una
        // (0-3) ne de ekkod'una (EX/IM/TR) uyuyordu, ayrıca "Yurtiçi" (kod 3) hiç yoktu.
        // Gerçek `skn_sabittanim` (grupkod=ISTURU) ile düzeltildi.
        await SeedIfEmptyAsync(db.WorkTypes, ct, () =>
        [
            new WorkType { Name = "İhracat", Code = "0", GroupCode = "ISTURU", AdditionalCode = "EX", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new WorkType { Name = "İthalat", Code = "1", GroupCode = "ISTURU", AdditionalCode = "IM", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new WorkType { Name = "Transit", Code = "2", GroupCode = "ISTURU", AdditionalCode = "TR", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new WorkType { Name = "Yurtiçi", Code = "3", GroupCode = "ISTURU", AdditionalCode = "YI", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        // Yerel 3 genel isim ("Operasyon"/"Satış"/"Muhasebe") gerçek sunucudaki 7
        // departmanla hiç eşleşmiyordu. `sbr_departman`'dan birebir kopyalandı —
        // "SATIŞ & PAZARLAMA"nın gerçek GUID'i (C249E951-...) Teklif formunun kendi
        // varsayılan departman GUID'iyle (Kritik yön değişikliği #27) birebir eşleşti.
        await SeedIfEmptyAsync(db.Departments, ct, () =>
        [
            new Department { Name = "İdari İşler", SiberId = "3416B6FC-2323-4471-B0AD-12B673317109", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new Department { Name = "İhracat Operasyon", SiberId = "D919053A-2CF0-4CB7-AD77-C487D312A71C", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new Department { Name = "İthalat Operasyon", SiberId = "4575BDF4-B72F-44D0-BFA9-7C63BBD913F5", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new Department { Name = "Muhasebe & Finans", SiberId = "CD95920F-12E3-48ED-821C-620A7442240E", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new Department { Name = "Satış & Pazarlama", SiberId = "C249E951-FB3F-4FF9-A1C4-EF0223A00B75", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new Department { Name = "Transit Operasyon", SiberId = "33289770-585F-4AFC-A007-C699CA8F7FBB", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new Department { Name = "Yönetim", SiberId = "DB3B6E91-B9D4-430B-BE96-AD5030EBC967", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        // DÜRÜST NOT: gerçek `sbr_odemesekli` 12 ayrıntılı ödeme şekli içeriyor (Mal
        // Mukabili/Akreditif/Vesaik Mükabili vb.) — bizim basit "Peşin/Vadeli" ikilisi
        // bunlarla 1:1 eşleşmiyor. Yalnızca "Peşin" tam eşleşme (kod+GUID, Teklif'in
        // kendi PEŞİN varsayılanıyla da birebir aynı); "Vadeli" için TEK bir doğru
        // karşılık yok (VADELİ AKREDİTİF mi, MAL MUKABİLİ mi — iş kararı gerektirir),
        // bu yüzden kodsuz bırakıldı.
        await SeedIfEmptyAsync(db.PaymentTypes, ct, () =>
        [
            new PaymentType { Name = "Peşin", Code = "2", SiberId = "97081C47-4F6A-4F37-9557-BC1CAC802106", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new PaymentType { Name = "Vadeli", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        await SeedIfEmptyAsync(db.LoadingTypes, ct, () =>
        [
            new LoadingType { Name = "Komple", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new LoadingType { Name = "Parsiyel", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        // olsold/Siber tarafında bu 4 "tanım" tablosu için hiç Seeder yoktu (yalnızca
        // canlı ortamda admin ekranından elle giriliyor) — bu port da şimdiye kadar
        // isim/kod uydurmuştu (ör. "Tır" diye bir Siber değeri hiç yok). 192.168.1.101
        // üzerindeki gerçek sunucunun `skn_sabittanim` (grupkod=ARACTIP/ROMORKCINS/
        // ARACSAHIP/ARACDURUM) ve `skn_arac`'ın denormalize ad sütunlarından SALT-OKUNUR
        // sorgu ile çekilen GERÇEK kod+isim çiftleriyle değiştirildi.
        await SeedIfEmptyAsync(db.CarTypes, ct, () =>
        [
            new CarType { Name = "Çekici", Code = 0, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new CarType { Name = "Kamyon", Code = 1, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new CarType { Name = "Römork", Code = 2, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new CarType { Name = "Otomobil", Code = 3, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new CarType { Name = "Konteyner", Code = 4, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        await SeedIfEmptyAsync(db.RomorkTypes, ct, () =>
        [
            new RomorkType { Name = "Frigo", Code = "0", GroupCode = "ROMORKCINS", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new RomorkType { Name = "Jumbo", Code = "1", GroupCode = "ROMORKCINS", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new RomorkType { Name = "Romork [Kamyon]", Code = "2", GroupCode = "ROMORKCINS", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new RomorkType { Name = "Optima", Code = "3", GroupCode = "ROMORKCINS", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new RomorkType { Name = "Tanker", Code = "4", GroupCode = "ROMORKCINS", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new RomorkType { Name = "Tekstil Dorse", Code = "5", GroupCode = "ROMORKCINS", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new RomorkType { Name = "Oto Taşıyıcı", Code = "6", GroupCode = "ROMORKCINS", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new RomorkType { Name = "Silobas", Code = "7", GroupCode = "ROMORKCINS", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new RomorkType { Name = "Low Bed", Code = "8", GroupCode = "ROMORKCINS", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new RomorkType { Name = "Mega Maksima", Code = "9", GroupCode = "ROMORKCINS", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new RomorkType { Name = "Maksima", Code = "10", GroupCode = "ROMORKCINS", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new RomorkType { Name = "Mega", Code = "11", GroupCode = "ROMORKCINS", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        await SeedIfEmptyAsync(db.CarOwners, ct, () =>
        [
            new CarOwner { Name = "Öz Mal", Code = 0, GroupCode = "ARACSAHIP", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new CarOwner { Name = "Kiralık", Code = 1, GroupCode = "ARACSAHIP", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new CarOwner { Name = "Sözleşmeli Kiralık", Code = 2, GroupCode = "ARACSAHIP", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        // DÜRÜST NOT: kaynaktaki "Boşta/Seferde" isimleri Siber'in gerçek `aracdurum`
        // alanının anlamıyla (araç bakım/hurda/satış durumu) örtüşmüyordu — ama bu C#
        // alanı ZATEN SiberCarRepository üzerinden birebir `aracdurum` sütununa
        // senkronlanıyor (CarService.SyncToSiberAsync). Yani isimler Siber'in GERÇEK
        // anlamına göre düzeltildi; "araç şu an boşta mı seferde mi" ayrı bir
        // (hesaplanan, statik tanım gerektirmeyen) kavram olarak kalmalı.
        await SeedIfEmptyAsync(db.CarStatusTypes, ct, () =>
        [
            new CarStatusType { Name = "Çalışan", Code = 0, GroupCode = "ARACDURUM", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new CarStatusType { Name = "Bakımda", Code = 1, GroupCode = "ARACDURUM", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new CarStatusType { Name = "Hurda", Code = 2, GroupCode = "ARACDURUM", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new CarStatusType { Name = "Satıldı", Code = 3, GroupCode = "ARACDURUM", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new CarStatusType { Name = "Kombinasyonda", Code = 4, GroupCode = "ARACDURUM", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        // olsold'da hiç Seeder yoktu, tablo tamamen boştu (canlı ortamda admin ekranından
        // dolduruluyor). Siber'in skn_sabittanim (grupkod=SEFERTUR) tablosundan gerçek
        // değerlerle dolduruldu — kaynaktaki "Sefer Tipi" dropdown'ı bu yüzden BOŞTU
        // ve Sefer oluşturmayı fiilen imkânsız kılıyordu (zorunlu 4 alandan biri).
        await SeedIfEmptyAsync(db.ExpeditionTypes, ct, () =>
        [
            new ExpeditionType { Name = "Kara", Code = "10", GroupCode = "SEFERTUR", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new ExpeditionType { Name = "Hava", Code = "11", GroupCode = "SEFERTUR", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new ExpeditionType { Name = "Deniz", Code = "12", GroupCode = "SEFERTUR", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        // Aynı şekilde boştu. Siber'in skn_sabittanim (grupkod=TALIMATGELISSEKLI) —
        // "Talimat" alanının geliş şekli.
        await SeedIfEmptyAsync(db.Instructions, ct, () =>
        [
            new Instruction { Name = "Telefon", Code = "0", GroupCode = "TALIMATGELISSEKLI", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new Instruction { Name = "E-Mail", Code = "1", GroupCode = "TALIMATGELISSEKLI", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new Instruction { Name = "Faks", Code = "2", GroupCode = "TALIMATGELISSEKLI", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new Instruction { Name = "Pazarlama", Code = "3", GroupCode = "TALIMATGELISSEKLI", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        // Aynı şekilde boştu. Siber'in skn_sabittanim (grupkod=REZERVASYONTASIMASEKLI) —
        // GUID'ler doğrudan gerçek sunucudan (`sabittanimid`), olsold'un kendi
        // system_data.js statik listesindeki GUID'lerle birebir eşleşti (çapraz doğrulandı).
        await SeedIfEmptyAsync(db.TransportTypes, ct, () =>
        [
            new TransportType { Name = "RO-RO", Code = "1", GroupCode = "REZERVASYONTASIMASEKLI", SiberId = "9E45ED23-EF9F-45E4-9530-0FA9F2D6C51C", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new TransportType { Name = "Tren", Code = "2", GroupCode = "REZERVASYONTASIMASEKLI", SiberId = "E0ADF7B0-6711-48ED-B2F5-FFBDEBD405A2", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new TransportType { Name = "Kara", Code = "3", GroupCode = "REZERVASYONTASIMASEKLI", SiberId = "B84B6983-7328-469C-8CBE-58E4AB2B3DB4", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
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
