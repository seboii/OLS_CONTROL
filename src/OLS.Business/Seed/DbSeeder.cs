using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OLS.Business.Services.Authentication;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;
using OLS.Business.Services.Authorization;

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
        IDefaultUserPassword defaultPassword,
        Services.Roles.IRoleService roles,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await SeedPermissionPagesAsync(db, cancellationToken);
        await SeedStatusTypesAsync(db, cancellationToken);
        await SeedCoreLookupsAsync(db, cancellationToken);
        await SeedAdminUserAsync(db, hasher, configuration, environment, logger, cancellationToken);
        await SeedUserPasswordsAsync(db, defaultPassword, configuration, logger, cancellationToken);
        await roles.SyncCatalogAsync(cancellationToken);

        // TEK SEFERLİK bakım anahtarı (şifre sıfırlamayla aynı desen):
        // Seed:ApplyRolesFromSiber=true iken açılışta tüm kullanıcılara Siber
        // departmanına göre rol uygulanır ve Siber'de engelli olanlar pasife
        // alınır. Her açılışta çalışacağı için kullandıktan sonra kapatılmalı —
        // elle verilmiş özel yetkileri ezer. Bu yüzden her çalıştığında uyarı
        // loglanır. Gündelik kullanımda rol atama arayüzden yapılır.
        if (configuration.GetValue("Seed:ApplyRolesFromSiber", false))
        {
            var summary = await roles.ApplyFromSiberAsync(cancellationToken);

            logger.LogWarning(
                "Seed:ApplyRolesFromSiber ETKİN — {Assigned} kullanıcıya rol uygulandı, " +
                "{Deactivated} hesap pasife alındı, {Skipped} hesap atlandı (Siber bağı yok). " +
                "Dağılım: {PerRole}. Bu anahtarı kapatın.",
                summary.Assigned, summary.Deactivated, summary.Skipped,
                string.Join(", ", summary.PerRole.Select(x => $"{x.Key}={x.Value}")));
        }

        if (environment.IsDevelopment())
            await SeedDemoConvenienceDataAsync(db, cancellationToken);
    }


    // ------------------------------------------------------------------
    // Şifresiz kalan kullanıcılara ortak başlangıç şifresi verir.
    //
    // BULUNAN GERÇEK BOŞLUK: Siber'den içe aktarılan kullanıcılar password =
    // NULL ile geliyordu (Siber bizim bcrypt formatımızda şifre taşımıyor),
    // dolayısıyla hiçbiri giriş yapamıyordu. Canlıda 131 kullanıcının 126'sı
    // bu durumdaydı. Ayrıntı ve yapılandırma için bkz. IDefaultUserPassword.
    //
    // Varsayılan davranış TAMAMLAYICIDIR: yalnızca şifresi boş olanlara yazar,
    // mevcut şifreleri ASLA ezmez — seed'in geri kalanıyla aynı "yoksa ekle"
    // sözleşmesi.
    //
    // Seed:ResetAllPasswords=true verilirse MEVCUT şifreler DAHİL tüm
    // kullanıcılar varsayılana döndürülür. Tek seferlik bir bakım anahtarıdır;
    // her açılışta çalışacağı için açık bırakılmamalıdır — bu yüzden her
    // çalıştığında uyarı loglanır.
    // ------------------------------------------------------------------
    private static async Task SeedUserPasswordsAsync(
        OlsDbContext db,
        IDefaultUserPassword defaultPassword,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken ct)
    {
        if (!defaultPassword.IsEnabled)
        {
            logger.LogInformation(
                "Seed:DefaultUserPassword tanımlı değil — kullanıcı şifreleri atanmadı.");
            return;
        }

        var resetAll = configuration.GetValue("Seed:ResetAllPasswords", false);

        var users = await db.Users
            .Where(u => resetAll || u.Password == null || u.Password == "")
            .ToListAsync(ct);

        if (users.Count == 0)
            return;

        var hash = defaultPassword.Hash();
        foreach (var user in users)
            user.Password = hash;

        await db.SaveChangesAsync(ct);

        if (resetAll)
            logger.LogWarning(
                "Seed:ResetAllPasswords ETKİN — {Count} kullanıcının şifresi varsayılana " +
                "DÖNDÜRÜLDÜ (mevcut şifreler dahil). Bu anahtarı kapatın.", users.Count);
        else
            logger.LogInformation(
                "{Count} şifresiz kullanıcıya varsayılan şifre atandı.", users.Count);
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
    // Yetki sayfaları — tek kaynak: PermissionPages.All.
    // Eksik bir slug arayüzde varsayılan-reddet davranışına düşer, yani
    // listelenmeyen bir modül menüde HİÇ görünmez.
    // ------------------------------------------------------------------
    private static async Task SeedPermissionPagesAsync(OlsDbContext db, CancellationToken ct)
    {
        // Liste PermissionPages.All'da — silme koruması da (PermissionPageService)
        // aynı listeyi kullanıyor, ikisi ayrışmasın diye tek kaynak.
        var pages = PermissionPages.All;

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
        // SiberId = gerçek Siber sdn_rezervasyondurum.durumid (canlı sunucudan doğrulandı).
        // TransferSiberService bu alanı skn_rezervasyon.durumid'ye yazıyor
        // (olsold: TransferSiberController.php, $data->statusTypeId->siber_id ?? null);
        // boş kalırsa "Durum boş olamaz" hatasıyla her teklif aktarımı baştan reddediliyordu.
        (string Code, string Name, string SiberId)[] statuses =
        [
            (StatusTypeCodes.Rejected, "Olumsuz", "5E0B49DD-E425-4537-90F2-710EEB44A19F"),
            (StatusTypeCodes.Order, "Sipariş", "DDF0614E-CA55-4C26-B125-A3AEBFAFB20B"),
            (StatusTypeCodes.Correction, "Düzeltme Talebi", "FCF55F7C-876A-482B-B4A7-BADCA250BB91"),
            (StatusTypeCodes.Offer, "Teklif", "EC922C9E-C2CF-4716-A198-F716FDA50358"),
            (StatusTypeCodes.Approved, "Olumlu", "F377242D-0121-4090-BDD2-FF420F21235A"),
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
                SiberId = s.SiberId,
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
            new WorkType { Name = "İhracat", Code = "0", GroupCode = "ISTURU", AdditionalCode = "EX", SiberId = "1704A279-D076-4C38-B448-D8047FB6193D", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new WorkType { Name = "İthalat", Code = "1", GroupCode = "ISTURU", AdditionalCode = "IM", SiberId = "EA147918-3714-4DEF-A379-A44DF2233F7E", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new WorkType { Name = "Transit", Code = "2", GroupCode = "ISTURU", AdditionalCode = "TR", SiberId = "0A99104E-1523-44B4-A986-C8529DDEDA21", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new WorkType { Name = "Yurtiçi", Code = "3", GroupCode = "ISTURU", AdditionalCode = "YI", SiberId = "577D934A-BB8F-48DA-9322-1633CC1F5241", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
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

        // Gerçek `sbr_odemesekli` 12 ayrıntılı ödeme şekli içeriyor (Mal Mukabili /
        // Akreditif / Vesaik Mukabili vb.) ve tamamı senkronla geliyor. Burada
        // yalnızca GUID'i doğrulanmış "Peşin" tohumlanır — Teklif formunun kendi
        // PEŞİN varsayılanıyla da birebir aynı.
        //
        // Eskiden bir de kodsuz/GUID'siz "Vadeli" tohumlanıyordu; hangi Siber
        // karşılığına denk geldiği bir iş kararıydı ve hiç verilmemişti. Sonuç:
        // Siber'de OLMAYAN bir seçenek listede duruyordu. Kaldırıldı.
        await SeedIfEmptyAsync(db.PaymentTypes, ct, () =>
        [
            new PaymentType { Name = "Peşin", Code = "2", SiberId = "97081C47-4F6A-4F37-9557-BC1CAC802106", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        // Gerçek skn_sabittanim(YUKLEMETIP) TAM OLARAK üç satır: GRUPAJ(0) /
        // KOMPLE(1) / CO-LOAD(2). Üçü de GUID'iyle birlikte tohumlanır.
        //
        // Eskiden GRUPAJ yerine "Parsiyel" tohumlanıyordu. İkisi terim olarak aynı
        // şey (LTL / konsolide kısmi yük) ama Siber'in kullandığı ad GRUPAJ ve
        // "Parsiyel" satırının GUID'i yoktu. Daha kötüsü: senkron eşlemesi KODA
        // göre sözlük kuruyor (ByCode) ve iki satır da kod "0" taşıyordu — yani
        // Siber'in GRUPAJ yükleri yerelde bu iki satırdan hangisine düşeceği
        // sıraya bağlıydı. Yinelenen satır kaldırıldı.
        await SeedIfEmptyAsync(db.LoadingTypes, ct, () =>
        [
            new LoadingType { Name = "Grupaj", Code = "0", GroupCode = "YUKLEMETIP", SiberId = "6F8B8B0E-357E-446B-99AC-E365E70AABED", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new LoadingType { Name = "Komple", Code = "1", GroupCode = "YUKLEMETIP", SiberId = "DDA7585E-B003-4594-A261-131C046F6031", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new LoadingType { Name = "Co-Load", Code = "2", GroupCode = "YUKLEMETIP", SiberId = "3456324E-2FDF-4D50-AB3A-29A6F218DFA7", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
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
            new RomorkType { Name = "Frigo", Code = "0", GroupCode = "ROMORKCINS", SiberId = "25135BDD-8249-4FD1-896B-94142A428D18", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new RomorkType { Name = "Jumbo", Code = "1", GroupCode = "ROMORKCINS", SiberId = "68ABE69A-41D5-4935-99D5-F07B981B0382", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new RomorkType { Name = "Romork [Kamyon]", Code = "2", GroupCode = "ROMORKCINS", SiberId = "952CC66F-34D1-4B76-85ED-E3E275113978", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new RomorkType { Name = "Optima", Code = "3", GroupCode = "ROMORKCINS", SiberId = "FFE0E488-8E94-4BD9-A3CA-A37C67A12715", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new RomorkType { Name = "Tanker", Code = "4", GroupCode = "ROMORKCINS", SiberId = "9B59AFD9-5BC6-4DAD-BFE3-DCE0E178DDC9", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new RomorkType { Name = "Tekstil Dorse", Code = "5", GroupCode = "ROMORKCINS", SiberId = "58ECFED9-14CA-4688-A54A-FEF5FCB45BB6", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new RomorkType { Name = "Oto Taşıyıcı", Code = "6", GroupCode = "ROMORKCINS", SiberId = "DC0D13BC-847F-425E-88B9-AFBC5A971495", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new RomorkType { Name = "Silobas", Code = "7", GroupCode = "ROMORKCINS", SiberId = "45739811-8473-4F51-9834-0D55FFE14036", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new RomorkType { Name = "Low Bed", Code = "8", GroupCode = "ROMORKCINS", SiberId = "B2DEF3C4-CBCD-421C-B080-D1749F59614F", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new RomorkType { Name = "Mega Maksima", Code = "9", GroupCode = "ROMORKCINS", SiberId = "6D07D705-5A7C-4DD2-8D6E-B5848A7D86D9", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new RomorkType { Name = "Maksima", Code = "10", GroupCode = "ROMORKCINS", SiberId = "6F394089-9FE1-11D7-BFF7-0000B4BEFACA", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new RomorkType { Name = "Mega", Code = "11", GroupCode = "ROMORKCINS", SiberId = "8F942D33-41E4-4A4A-8C29-CE6F225CE308", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        // additional_code = Siber skn_sabittanim(ARACSAHIP).ekkod: ExpeditionWriteService.CreateAsync
        // sefer numarasını Siber'de ararken skn_sefer.aracsahipad (nvarchar(10), kısa kod - "OZ"/"KR"/"SK")
        // ile eşleştiriyor (olsold: ExpeditionController.php:90, ->where('aracsahipad', ...additional_code)).
        // Bu alan boş bırakılırsa arama hiçbir zaman eşleşmez ve her pozisyon için gereksiz yeni
        // skn_sefer satırı açılır - canlı Siber'den doğrulandı.
        await SeedIfEmptyAsync(db.CarOwners, ct, () =>
        [
            new CarOwner { Name = "Öz Mal", Code = 0, AdditionalCode = "OZ", GroupCode = "ARACSAHIP", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new CarOwner { Name = "Kiralık", Code = 1, AdditionalCode = "KR", GroupCode = "ARACSAHIP", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new CarOwner { Name = "Sözleşmeli Kiralık", Code = 2, AdditionalCode = "SK", GroupCode = "ARACSAHIP", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
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

        // Aynı şekilde boştu — SiberLoadRepository.InsertYukAsync'in gönderdiği
        // "YukTurKod" doğrudan skn_yuk.yukturkod (tinyint) sütununa gidiyor; bu alanın
        // gerçek karşılığı skn_sabittanim(grupkod=YUKTUR) — isim eşleşmesi çok net
        // (yük TÜRÜ kodu ↔ YUKTUR). Eskiden yalnızca 3 temel mod tohumlanıyordu;
        // gerçek listede 9 satır var (çok modlu HAVA > DENİZ vb. kombinasyonlar
        // dâhil) ve tamamı GUID'iyle eklendi.
        await SeedIfEmptyAsync(db.LoadTransferTypes, ct, () =>
        [
            new LoadTransferType { Name = "Kara", Code = "1", GroupCode = "YUKTUR", SiberId = "3F0D3C58-B2EE-4E02-A5C0-0A6C90CA07B4", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new LoadTransferType { Name = "Hava", Code = "2", GroupCode = "YUKTUR", SiberId = "7324C4B9-C193-487F-9AEC-25737BC60E78", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new LoadTransferType { Name = "Deniz", Code = "3", GroupCode = "YUKTUR", SiberId = "5CABCEA6-699D-447E-A587-69B911FD99DA", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new LoadTransferType { Name = "Hava > Deniz", Code = "4", GroupCode = "YUKTUR", SiberId = "1DAA0F11-BFCE-4085-80A5-882B5DBFCBC6", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new LoadTransferType { Name = "Hava > Kara", Code = "5", GroupCode = "YUKTUR", SiberId = "B91789FE-05F8-42E7-9753-1BC3108D268A", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new LoadTransferType { Name = "Kara > Deniz", Code = "6", GroupCode = "YUKTUR", SiberId = "F0290594-DC9F-4B4B-8442-C85D7EADE135", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new LoadTransferType { Name = "Kara > Hava", Code = "7", GroupCode = "YUKTUR", SiberId = "D674A09B-4157-4E13-8617-3A457B83EF7C", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new LoadTransferType { Name = "Deniz > Hava", Code = "8", GroupCode = "YUKTUR", SiberId = "C60FECD5-C11A-4F7B-BD12-BE97A9E9344E", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new LoadTransferType { Name = "Deniz > Kara", Code = "9", GroupCode = "YUKTUR", SiberId = "193E81CD-254E-4144-9AFE-9C1FFA8A5748", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        // DÜRÜST NOT: bu tabloda hiç seeder yoktu (yalnızca elle eklenmiş test kaydı
        // vardı) — "Kap Tipi" Teklif/Yük'ün İçerik formunda ZORUNLU alan olduğundan bu
        // da fiilen içerik satırı eklemeyi engelliyordu. Gerçek `skn_kapcins` yüzlerce
        // kayıt içeriyor (kodu sütunu boş, yalnızca GUID+ad var); tamamını modellemek
        // yerine en yaygın 8 paketleme türü seçildi.
        await SeedIfEmptyAsync(db.CaseTypes, ct, () =>
        [
            new CaseType { Name = "Adet", SiberId = "066C7361-FD78-11D5-982A-00306E00B104", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new CaseType { Name = "Koli", SiberId = "DAF41673-2114-494A-8F01-D9075A365B15", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new CaseType { Name = "Kutu", SiberId = "A60036AA-CCB9-4FD9-A71A-F3D988BD7D09", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new CaseType { Name = "Palet", SiberId = "EAC8DA4F-895F-435B-BCB4-D5E6FF767D54", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new CaseType { Name = "Kasa", SiberId = "E2B1D3CD-B181-4DCF-B4FA-5686ACF167B5", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new CaseType { Name = "Fıçı", SiberId = "265A3BB7-644C-46AC-BE1B-6FDE00DD8B43", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new CaseType { Name = "Rulo", SiberId = "4FCA74A2-26A7-11D6-982E-00306E00B104", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new CaseType { Name = "Konteyner", SiberId = "4FCA748B-26A7-11D6-982E-00306E00B104", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
        ]);

        // Aynı şekilde boştu. Siber'in skn_sabittanim (grupkod=TALIMATGELISSEKLI) —
        // "Talimat" alanının geliş şekli.
        await SeedIfEmptyAsync(db.Instructions, ct, () =>
        [
            new Instruction { Name = "Telefon", Code = "0", GroupCode = "TALIMATGELISSEKLI", SiberId = "CE31EDA5-E59A-4D93-9EA5-615460F7EF5B", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new Instruction { Name = "E-Mail", Code = "1", GroupCode = "TALIMATGELISSEKLI", SiberId = "122BD16C-8633-4B73-BA0C-943567475255", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new Instruction { Name = "Faks", Code = "2", GroupCode = "TALIMATGELISSEKLI", SiberId = "5BD163B1-79A4-47D3-9C0A-6143AC4629E9", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new Instruction { Name = "Pazarlama", Code = "3", GroupCode = "TALIMATGELISSEKLI", SiberId = "F4A8AFB5-272D-466F-9D2D-892599E0FF45", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
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

        // Siber'de bu 10 tür için ayrı bir tanım tablosu yok — skn_yukevrak.sirano
        // sabit değerleri (gerçek Siber'de doğrulandı, bkz. EvrakTuru). Code, evrak
        // takibi Siber'e yazılırken doğrudan sirano sütununa gidiyor.
        await SeedIfEmptyAsync(db.EvrakTurus, ct, () =>
        [
            new EvrakTuru { Name = "Navlun Faturası", Code = "1", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new EvrakTuru { Name = "Invoice", Code = "2", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new EvrakTuru { Name = "Konşimento", Code = "3", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new EvrakTuru { Name = "CMR", Code = "4", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new EvrakTuru { Name = "Mal Faturası", Code = "5", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new EvrakTuru { Name = "ATR-1", Code = "6", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new EvrakTuru { Name = "Packing List", Code = "7", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new EvrakTuru { Name = "Sağlık Sertifikası", Code = "8", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new EvrakTuru { Name = "Çeki Listesi", Code = "9", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new EvrakTuru { Name = "Menşei Şehadetnamesi", Code = "10", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
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

        // AD, SİBER'DEKİ ADLA EŞLEŞMELİ. Tohum ülkelerin SiberId'si boş gelir;
        // SiberImportService.ImportCountriesAsync bunu sonradan AD EŞLEŞMESİYLE
        // dolduruyor (Key = NormalizeTurkish, yani yalnızca İ/I/ı katlanır).
        // "Rusya" bu yüzden Siber'in "RUSYA FEDERASYONU" satırıyla HİÇ eşleşmiyor
        // ve SiberId'si sonsuza kadar boş kalan bir seçenek olarak listede
        // duruyordu — yük Siber'e ülke ADIYLA yazıldığı için böyle bir seçim
        // Siber'de ülkesi boş bir kayıt bırakır (bkz. RemoveMockCountry).
        if (!await db.Countries.AnyAsync(ct))
        {
            db.Countries.AddRange(
                new Country { Id = Guid.NewGuid(), Name = "Türkiye", CountryCode = "TR", PhoneCode = "90", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new Country { Id = Guid.NewGuid(), Name = "Almanya", CountryCode = "DE", PhoneCode = "49", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new Country { Id = Guid.NewGuid(), Name = "Rusya Federasyonu", CountryCode = "RU", PhoneCode = "7", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now });
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
