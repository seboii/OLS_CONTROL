using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;
using OLS.DataAccess.Siber;

namespace OLS.Business.Services.TransferData;

/// <summary>
/// Siber referans verisini yerel Postgres'e aktarır — olsold:
/// <c>Front\TransferData\TransferDataController</c>.
///
/// BULUNAN GERÇEK BOŞLUK: bu servis dosyasına <c>init-reference.sql</c>'in
/// kendi yorumunda ATIFTA BULUNULUYORDU ama dosya hiç yoktu — Teklif→Yük
/// dönüşümünün gerektirdiği tüm <c>siber_id</c> eşlemeleri (payment_types,
/// work_types, departments, ...) bu yüzden hep boştu (bkz. TESLIM-RAPORU.md §8
/// "Siber kimlik eşleşmesi kısıtı" — o not artık ÇÖZÜLDÜĞÜ için güncellendi).
///
/// KAPSAM: olsold'un TransferDataController'ı 20'den fazla uç içeriyor.
/// Burada yalnızca REFERANS/TANIM verisi ve HENÜZ boş olan lookup tabloları
/// portlandı — bu 8 modülün ÇALIŞMASI için gereken budur. Bilinçli olarak
/// PORTLANMAYAN (kaynakta var, burada yok):
///   - pullLoad, update_siber_id, pull_expdition, pull_skn_yukaktarma,
///     pull_sbr_dovizkur: GEÇMİŞ işlem verisi taşıma (bu port canlı bir
///     sistemden göç etmiyor, sıfırdan test ediliyor — taşınacak geçmiş yok).
///   - pullsbr_kzgelirgider, pull_sfy_efatura/efaturadetay/edurum/efirma:
///     Uyumsoft e-fatura ve Reports/Hedef-ciro — proje kapsamının DIŞINDA
///     (bkz. TESLIM-RAPORU.md §1 "Dışarıda").
///
/// GÜVENLİK TASARIMI (kaynaktan BİLİNÇLİ SAPMA): olsold'un çoğu <c>get*</c>
/// ucu koşulsuz <c>::create()</c> yapıyordu — tekrar çalıştırılırsa yinelenen
/// satır üretirdi. Burada TÜM aktarımlar AD/KOD eşleşmesiyle YUKARI YAZAR
/// (upsert): eşleşen yerel satır varsa yalnızca <c>siber_id</c> güncellenir
/// (Ad/Kod gibi diğer alanlara DOKUNULMAZ — yerel seed doğru kabul edilir),
/// eşleşme yoksa yeni satır açılır. Bu, zaten doğru seed edilmiş verinin
/// (work_types, payment_types, departments, ...) yanlışlıkla bozulmasını
/// önler ve servisin güvenle TEKRAR ÇALIŞTIRILABİLİR olmasını sağlar.
///
/// EŞLEŞTİRME BELLEK-İÇİ YAPILIR (SQL'e DEĞİL): ilk sürüm her satır için
/// <c>WHERE x.Name.ToUpper() == @key</c> sorgusu çalıştırıyordu — canlı testte
/// "Satış" (ş harfi) için bu sessizce EŞLEŞMEDİ ve yinelenen bir satır açtı
/// (kök neden: EF Core'un ürettiği Postgres <c>upper()</c> çağrısıyla .NET'in
/// <c>ToUpperInvariant()</c>'ı arasındaki bir normalize/parametre farkı —
/// ikisi ayrı ayrı doğru sonuç verdiği hâlde birlikte eşleşmedi). Düzeltme:
/// her tablonun mevcut satırları ÖNCE TEK SORGUYLA belleğe alınır, eşleştirme
/// tamamen .NET string karşılaştırmasıyla yapılır — SQL tarafı hiç karışmaz.
/// </summary>
public interface ISiberImportService
{
    /// <summary>olsold: <c>POST /transfer_data</c> (save) — temel referans/tanım tabloları.</summary>
    Task<SiberImportSummary> ImportReferenceDataAsync(CancellationToken cancellationToken = default);

    /// <summary>olsold: <c>getSiberAccount</c> — cari eşleme (muhasebe koduna göre tip ataması).</summary>
    Task<SiberImportSummary> ImportAccountsAsync(CancellationToken cancellationToken = default);

    Task<SiberImportSummary> ImportLoadStatusTypesAsync(CancellationToken cancellationToken = default);
    Task<SiberImportSummary> ImportExpeditionTypesAsync(CancellationToken cancellationToken = default);
    Task<SiberImportSummary> ImportExpeditionStatusesAsync(CancellationToken cancellationToken = default);
    Task<SiberImportSummary> ImportCarTypesAsync(CancellationToken cancellationToken = default);
    Task<SiberImportSummary> ImportCarStatusTypesAsync(CancellationToken cancellationToken = default);
    Task<SiberImportSummary> ImportCarOwnersAsync(CancellationToken cancellationToken = default);
    Task<SiberImportSummary> ImportDeliveryMethodsAsync(CancellationToken cancellationToken = default);
    Task<SiberImportSummary> ImportCarsAsync(CancellationToken cancellationToken = default);
}

public sealed record SiberImportSummary(int Created, int Updated, IReadOnlyList<string> Errors)
{
    public static readonly SiberImportSummary Empty = new(0, 0, []);
}

public sealed class SiberImportService : ISiberImportService
{
    private readonly OlsDbContext _db;
    private readonly ISiberConnectionFactory _siber;

    public SiberImportService(OlsDbContext db, ISiberConnectionFactory siber)
    {
        _db = db;
        _siber = siber;
    }

    /// <summary>Tamamen .NET tarafında normalize eder — SQL'e hiç gönderilmez (bkz. sınıf yorumu).</summary>
    private static string Key(string? value) =>
        (value ?? string.Empty).Trim().Normalize(System.Text.NormalizationForm.FormC).ToUpperInvariant();

    private sealed record SabitTanimRow(string Sabittanimid, string? Ad, int? Kod, int? Ozelkod, string? Ekkod);

    private async Task<IDbConnection> OpenAsync(CancellationToken cancellationToken)
    {
        if (!_siber.IsConfigured)
            throw new InvalidOperationException("Siber bağlantısı yapılandırılmamış.");

        return await _siber.CreateOpenAsync(cancellationToken);
    }

    private async Task<List<SabitTanimRow>> QuerySabitTanimAsync(
        IDbConnection connection, string grupKod, CancellationToken cancellationToken)
    {
        var rows = await connection.QueryAsync<SabitTanimRow>(
            new CommandDefinition(
                """
                SELECT sabittanimid AS Sabittanimid, ad AS Ad, kod AS Kod,
                       ozelkod AS Ozelkod, ekkod AS Ekkod
                FROM skn_sabittanim WHERE grupkod = @grupKod
                """,
                new { grupKod },
                cancellationToken: cancellationToken));

        return rows.ToList();
    }

    /// <summary>Mevcut satırları TEK sorguyla belleğe alır — eşleştirme SQL'e hiç gitmez.</summary>
    private async Task<Dictionary<string, TEntity>> LoadExistingByNameAsync<TEntity>(
        IQueryable<TEntity> query, Func<TEntity, string?> nameSelector, CancellationToken cancellationToken)
        where TEntity : class
    {
        var all = await query.ToListAsync(cancellationToken);
        var map = new Dictionary<string, TEntity>();
        foreach (var entity in all)
        {
            var key = Key(nameSelector(entity));
            if (key.Length > 0 && !map.ContainsKey(key))
                map[key] = entity;
        }

        return map;
    }

    // ── save(): temel referans/tanım tabloları ──────────────────────────────

    public async Task<SiberImportSummary> ImportReferenceDataAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await OpenAsync(cancellationToken);
        var created = 0;
        var updated = 0;
        var errors = new List<string>();

        async Task RunAsync(string label, Func<Task<(int c, int u)>> action)
        {
            try
            {
                var (c, u) = await action();
                created += c;
                updated += u;
            }
            catch (Exception ex)
            {
                errors.Add($"{label}: {ex.Message}");
            }
        }

        await RunAsync("TaxOffice", () => ImportTaxOfficesAsync(connection, cancellationToken));
        await RunAsync("StatusType", () => ImportStatusTypesAsync(connection, cancellationToken));
        await RunAsync("Instruction", () => ImportSabitTanimTableAsync(
            connection, "TALIMATGELISSEKLI", cancellationToken,
            _db.Instructions, x => x.Name,
            (x, row) => { x.SiberId = row.Sabittanimid; if (string.IsNullOrEmpty(x.Code)) x.Code = row.Kod?.ToString(); },
            row => new Instruction { Name = row.Ad, GroupCode = "TALIMATGELISSEKLI", Code = row.Kod?.ToString(), SiberId = row.Sabittanimid },
            _db.Instructions));
        await RunAsync("RomorkType", () => ImportSabitTanimTableAsync(
            connection, "ROMORKCINS", cancellationToken,
            _db.RomorkTypes, x => x.Name,
            (x, row) => { x.SiberId = row.Sabittanimid; if (string.IsNullOrEmpty(x.Code)) x.Code = row.Kod?.ToString(); },
            row => new RomorkType { Name = row.Ad, GroupCode = "ROMORKCINS", Code = row.Kod?.ToString(), SiberId = row.Sabittanimid },
            _db.RomorkTypes));
        await RunAsync("WorkType", () => ImportSabitTanimTableAsync(
            connection, "ISTURU", cancellationToken,
            _db.WorkTypes, x => x.Name,
            (x, row) =>
            {
                x.SiberId = row.Sabittanimid;
                if (string.IsNullOrEmpty(x.Code)) x.Code = row.Ekkod ?? row.Kod?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(x.AdditionalCode)) x.AdditionalCode = row.Ekkod ?? string.Empty;
            },
            row => new WorkType
            {
                Name = row.Ad ?? string.Empty, GroupCode = "ISTURU",
                Code = row.Ekkod ?? row.Kod?.ToString() ?? string.Empty,
                AdditionalCode = row.Ekkod ?? string.Empty, SiberId = row.Sabittanimid,
            },
            _db.WorkTypes));
        await RunAsync("LoadingType", () => ImportSabitTanimTableAsync(
            connection, "YUKLEMETIP", cancellationToken,
            _db.LoadingTypes, x => x.Name,
            (x, row) => { x.SiberId = row.Sabittanimid; if (string.IsNullOrEmpty(x.Code)) x.Code = row.Kod?.ToString(); },
            row => new LoadingType { Name = row.Ad, GroupCode = "YUKLEMETIP", Code = row.Kod?.ToString(), SiberId = row.Sabittanimid },
            _db.LoadingTypes));
        await RunAsync("PaymentType", () => ImportPaymentTypesAsync(connection, cancellationToken));
        await RunAsync("LoadTransferType", () => ImportSabitTanimTableAsync(
            connection, "YUKTUR", cancellationToken,
            _db.LoadTransferTypes, x => x.Name,
            (x, row) => { x.SiberId = row.Sabittanimid; if (string.IsNullOrEmpty(x.Code)) x.Code = row.Kod?.ToString(); },
            row => new LoadTransferType { Name = row.Ad, GroupCode = "YUKTUR", Code = row.Kod?.ToString(), SiberId = row.Sabittanimid },
            _db.LoadTransferTypes));
        await RunAsync("TransportType", () => ImportSabitTanimTableAsync(
            connection, "REZERVASYONTASIMASEKLI", cancellationToken,
            _db.TransportTypes, x => x.Name,
            (x, row) => { x.SiberId = row.Sabittanimid; if (string.IsNullOrEmpty(x.Code)) x.Code = row.Kod?.ToString(); },
            row => new TransportType { Name = row.Ad, GroupCode = "REZERVASYONTASIMASEKLI", Code = row.Kod?.ToString(), SiberId = row.Sabittanimid },
            _db.TransportTypes));
        await RunAsync("Department", () => ImportDepartmentsAsync(connection, cancellationToken));
        await RunAsync("ProductType", () => ImportProductTypesAsync(connection, cancellationToken));
        await RunAsync("CaseType", () => ImportCaseTypesAsync(connection, cancellationToken));
        await RunAsync("ItemType", () => ImportItemTypesAsync(connection, cancellationToken));
        await RunAsync("FinancialItem", () => ImportFinancialItemsAsync(connection, cancellationToken));
        await RunAsync("Country", () => ImportCountriesAsync(connection, cancellationToken));
        await RunAsync("City", () => ImportCitiesAsync(connection, cancellationToken));
        await RunAsync("District", () => ImportDistrictsAsync(connection, cancellationToken));
        await RunAsync("Currency", () => ImportCurrenciesAsync(connection, cancellationToken));
        await RunAsync("User", () => ImportUsersAsync(connection, cancellationToken));

        return new SiberImportSummary(created, updated, errors);
    }

    /// <summary>
    /// <c>skn_sabittanim</c>'in tek bir grupkod'unu okuyup verilen yerel tabloya
    /// ad-eşleşmeli upsert yapan ortak yordam (ISTURU/PaymentType hariç — onlar
    /// farklı kaynak tablodan geldiği ve/veya farklı alan eşlemesi gerektirdiği
    /// için ayrı yazıldı).
    /// <paramref name="setSiberId"/> tüm satırı (yalnızca id'yi değil) alır: bazı
    /// tablolar (RomorkType, LoadingType) DbSeeder'da yalnızca <c>Name</c> ile
    /// seed ediliyor, <c>Code</c> hiç set edilmiyor — eşleşen mevcut satırda
    /// Code hâlâ boşsa burada da doldurulur (zaten dolu bir yerel değer asla
    /// ezilmez).
    /// </summary>
    private async Task<(int, int)> ImportSabitTanimTableAsync<TEntity>(
        IDbConnection connection, string grupKod, CancellationToken cancellationToken,
        IQueryable<TEntity> existingQuery, Func<TEntity, string?> nameSelector,
        Action<TEntity, SabitTanimRow> setSiberId, Func<SabitTanimRow, TEntity> createNew, DbSet<TEntity> dbSet)
        where TEntity : class
    {
        var rows = await QuerySabitTanimAsync(connection, grupKod, cancellationToken);
        var existingByName = await LoadExistingByNameAsync(existingQuery, nameSelector, cancellationToken);
        var created = 0;
        var updated = 0;

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Ad))
                continue;

            if (existingByName.TryGetValue(Key(row.Ad), out var existing))
            {
                setSiberId(existing, row);
                updated++;
            }
            else
            {
                dbSet.Add(createNew(row));
                created++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (created, updated);
    }

    private async Task<(int, int)> ImportTaxOfficesAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        var rows = await connection.QueryAsync(
            new CommandDefinition(
                "SELECT vergidaireid, ad, ozelkod, sehir FROM sbr_vergidaire",
                cancellationToken: cancellationToken));

        var existingByName = await LoadExistingByNameAsync(_db.TaxOffices, x => x.Name, cancellationToken);

        int created = 0, updated = 0;
        foreach (var row in rows)
        {
            string? name = row.ad;
            if (string.IsNullOrWhiteSpace(name)) continue;

            if (existingByName.TryGetValue(Key(name), out var existing))
            {
                existing.SiberId = row.vergidaireid;
                updated++;
            }
            else
            {
                _db.TaxOffices.Add(new TaxOffice
                {
                    Name = name, SpecialCode = row.ozelkod, City = row.sehir, SiberId = row.vergidaireid,
                });
                created++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (created, updated);
    }

    /// <summary>
    /// StatusType ÖZEL DURUM: yerel <c>status_types</c> zaten DbSeeder.cs ile
    /// gerçek iş anlamıyla (DATA-002: 1=Olumsuz..5=Olumlu) seed edilmiş —
    /// buradan ASLA yeni satır AÇILMAZ, yalnızca AD eşleşen mevcut satırlara
    /// siber_id yazılır. Eşleşmeyen bir Siber durumu varsa sessizce atlanır
    /// (yerel iş kuralı kaynaktan daha dar/net — kasıtlı).
    /// </summary>
    private async Task<(int, int)> ImportStatusTypesAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        var rows = await connection.QueryAsync(
            new CommandDefinition(
                "SELECT durumid, ad FROM sdn_rezervasyondurum",
                cancellationToken: cancellationToken));

        var existingByName = await LoadExistingByNameAsync(_db.StatusTypes, x => x.Name, cancellationToken);

        var updated = 0;
        foreach (var row in rows)
        {
            string? name = row.ad;
            if (string.IsNullOrWhiteSpace(name)) continue;

            if (existingByName.TryGetValue(Key(name), out var existing))
            {
                existing.SiberId = row.durumid;
                updated++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (0, updated);
    }

    private async Task<(int, int)> ImportPaymentTypesAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        var rows = await connection.QueryAsync(
            new CommandDefinition(
                "SELECT odemesekliid, ad, kodu FROM sbr_odemesekli",
                cancellationToken: cancellationToken));

        var existingByName = await LoadExistingByNameAsync(_db.PaymentTypes, x => x.Name, cancellationToken);

        int created = 0, updated = 0;
        foreach (var row in rows)
        {
            string? name = row.ad;
            if (string.IsNullOrWhiteSpace(name)) continue;

            if (existingByName.TryGetValue(Key(name), out var existing))
            {
                existing.SiberId = row.odemesekliid;
                updated++;
            }
            else
            {
                _db.PaymentTypes.Add(new PaymentType { Name = name, Code = row.kodu, SiberId = row.odemesekliid });
                created++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (created, updated);
    }

    private async Task<(int, int)> ImportDepartmentsAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        var rows = await connection.QueryAsync(
            new CommandDefinition("SELECT departmanid, ad FROM sbr_departman", cancellationToken: cancellationToken));

        var existingByName = await LoadExistingByNameAsync(_db.Departments, x => x.Name, cancellationToken);

        int created = 0, updated = 0;
        foreach (var row in rows)
        {
            string? name = row.ad;
            if (string.IsNullOrWhiteSpace(name)) continue;

            if (existingByName.TryGetValue(Key(name), out var existing))
            {
                existing.SiberId = row.departmanid;
                updated++;
            }
            else
            {
                _db.Departments.Add(new Department { Name = name, SiberId = row.departmanid });
                created++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (created, updated);
    }

    private async Task<(int, int)> ImportProductTypesAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        var rows = await connection.QueryAsync(
            new CommandDefinition("SELECT malcinsid, ad FROM sbr_malcinsi", cancellationToken: cancellationToken));

        var existingByName = await LoadExistingByNameAsync(_db.ProductTypes, x => x.Name, cancellationToken);

        int created = 0, updated = 0;
        foreach (var row in rows)
        {
            string? name = row.ad;
            if (string.IsNullOrWhiteSpace(name)) continue;

            if (existingByName.TryGetValue(Key(name), out var existing))
            {
                existing.SiberId = row.malcinsid;
                updated++;
            }
            else
            {
                _db.ProductTypes.Add(new ProductType { Name = name, SiberId = row.malcinsid });
                created++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (created, updated);
    }

    private async Task<(int, int)> ImportCaseTypesAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        var rows = await connection.QueryAsync(
            new CommandDefinition("SELECT kapcinsid, ad, edikod FROM skn_kapcins", cancellationToken: cancellationToken));

        var existingByName = await LoadExistingByNameAsync(_db.CaseTypes, x => x.Name, cancellationToken);

        int created = 0, updated = 0;
        foreach (var row in rows)
        {
            string? name = row.ad;
            if (string.IsNullOrWhiteSpace(name)) continue;

            if (existingByName.TryGetValue(Key(name), out var existing))
            {
                existing.SiberId = row.kapcinsid;
                updated++;
            }
            else
            {
                _db.CaseTypes.Add(new CaseType { Name = name, Edikod = row.edikod, SiberId = row.kapcinsid });
                created++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (created, updated);
    }

    /// <summary>Kaynakta ItemType ve FinancialItem İKİSİ DE aynı <c>skn_kalem</c>'den okunuyor (birebir).</summary>
    private async Task<(int, int)> ImportItemTypesAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        var rows = await connection.QueryAsync(
            new CommandDefinition("SELECT kalemid, ad FROM skn_kalem", cancellationToken: cancellationToken));

        var existingByName = await LoadExistingByNameAsync(_db.ItemTypes, x => x.Name, cancellationToken);

        int created = 0, updated = 0;
        foreach (var row in rows)
        {
            string? name = row.ad;
            if (string.IsNullOrWhiteSpace(name)) continue;

            if (existingByName.TryGetValue(Key(name), out var existing))
            {
                existing.SiberId = row.kalemid;
                updated++;
            }
            else
            {
                _db.ItemTypes.Add(new ItemType { Name = name, SiberId = row.kalemid });
                created++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (created, updated);
    }

    private async Task<(int, int)> ImportFinancialItemsAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        var rows = await connection.QueryAsync(
            new CommandDefinition("SELECT kalemid, ad FROM skn_kalem", cancellationToken: cancellationToken));

        var existingByName = await LoadExistingByNameAsync(_db.FinancialItems, x => x.Name, cancellationToken);

        int created = 0, updated = 0;
        foreach (var row in rows)
        {
            string? name = row.ad;
            if (string.IsNullOrWhiteSpace(name)) continue;

            if (existingByName.TryGetValue(Key(name), out var existing))
            {
                existing.SiberId = row.kalemid;
                updated++;
            }
            else
            {
                _db.FinancialItems.Add(new FinancialItem { Name = name, SiberId = row.kalemid });
                created++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (created, updated);
    }

    /// <summary>
    /// Country/City/District ÖZEL DURUM: kaynakta Siber id'si doğrudan yerel
    /// PK olarak yazılıyordu (<c>'id' => $item->ulkeid</c>). Burada YAPILMAZ —
    /// yerel <c>Guid Id</c> zaten DbSeeder/önceki kayıtlarla sabitlenmiş
    /// olabilir; onu Siber id'siyle değiştirmek FK'leri kırar. Bunun yerine
    /// ad eşleşmesiyle yalnızca <c>SiberId</c> yazılır, yerel Id SABİT kalır.
    /// </summary>
    private async Task<(int, int)> ImportCountriesAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        var rows = await connection.QueryAsync(
            new CommandDefinition("SELECT ulkeid, ad, telefonkod, kisaad FROM sbr_ulke", cancellationToken: cancellationToken));

        var existingByName = await LoadExistingByNameAsync(_db.Countries, x => x.Name, cancellationToken);

        int created = 0, updated = 0;
        foreach (var row in rows)
        {
            string? name = row.ad;
            if (string.IsNullOrWhiteSpace(name)) continue;

            if (existingByName.TryGetValue(Key(name), out var existing))
            {
                existing.SiberId = row.ulkeid;
                updated++;
            }
            else
            {
                var fresh = new Country
                {
                    Id = Guid.NewGuid(), Name = name,
                    CountryCode = row.kisaad is string s ? s.Trim() : null,
                    PhoneCode = row.telefonkod, SiberId = row.ulkeid,
                };
                _db.Countries.Add(fresh);
                existingByName[Key(name)] = fresh;
                created++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (created, updated);
    }

    private async Task<(int, int)> ImportCitiesAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        var rows = await connection.QueryAsync(
            new CommandDefinition("SELECT sehirid, ad, ulkeid FROM sbr_sehir", cancellationToken: cancellationToken));

        var countryBySiberId = await _db.Countries.AsNoTracking()
            .Where(c => c.SiberId != null)
            .ToDictionaryAsync(c => c.SiberId!, c => c.Id, cancellationToken);
        var existingByName = await LoadExistingByNameAsync(_db.Cities, x => x.Name, cancellationToken);

        int created = 0, updated = 0;
        foreach (var row in rows)
        {
            string? name = row.ad;
            string? ulkeid = row.ulkeid;
            if (string.IsNullOrWhiteSpace(name) || ulkeid is null || !countryBySiberId.TryGetValue(ulkeid, out var countryId))
                continue;

            if (existingByName.TryGetValue(Key(name), out var existing))
            {
                existing.SiberId = row.sehirid;
                updated++;
            }
            else
            {
                var fresh = new City { Id = Guid.NewGuid(), Name = name, CountryId = countryId.ToString(), SiberId = row.sehirid };
                _db.Cities.Add(fresh);
                existingByName[Key(name)] = fresh;
                created++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (created, updated);
    }

    private async Task<(int, int)> ImportDistrictsAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        var rows = await connection.QueryAsync(
            new CommandDefinition("SELECT ilceid, ad, sehirid FROM sbr_ilce", cancellationToken: cancellationToken));

        var cityBySiberId = await _db.Cities.AsNoTracking()
            .Where(c => c.SiberId != null)
            .ToDictionaryAsync(c => c.SiberId!, c => c.Id, cancellationToken);
        var existingByName = await LoadExistingByNameAsync(_db.Districts, x => x.Name, cancellationToken);

        int created = 0, updated = 0;
        foreach (var row in rows)
        {
            string? name = row.ad;
            string? sehirid = row.sehirid;
            if (string.IsNullOrWhiteSpace(name) || sehirid is null || !cityBySiberId.TryGetValue(sehirid, out var cityId))
                continue;

            if (existingByName.TryGetValue(Key(name), out var existing))
            {
                existing.SiberId = row.ilceid;
                updated++;
            }
            else
            {
                var fresh = new District { Id = Guid.NewGuid(), Name = name, CityId = cityId.ToString(), SiberId = row.ilceid };
                _db.Districts.Add(fresh);
                existingByName[Key(name)] = fresh;
                created++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (created, updated);
    }

    /// <summary>Kod ile eşleşir (ad yerel tarafta büyük harf/farklı yazılabiliyor — bkz. sınıf yorumu).</summary>
    private async Task<(int, int)> ImportCurrenciesAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        var rows = await connection.QueryAsync(
            new CommandDefinition("SELECT rowguid, ad, kod FROM sbr_doviztur", cancellationToken: cancellationToken));

        var existingByCode = await LoadExistingByNameAsync(_db.Currencies, x => x.Code, cancellationToken);

        int created = 0, updated = 0;
        foreach (var row in rows)
        {
            string? code = row.kod;
            string? name = row.ad;
            if (string.IsNullOrWhiteSpace(code)) continue;

            if (existingByCode.TryGetValue(Key(code), out var existing))
            {
                existing.SiberId = row.rowguid;
                updated++;
            }
            else
            {
                var fresh = new Currency { Name = name, Code = code, SiberId = row.rowguid };
                _db.Currencies.Add(fresh);
                existingByCode[Key(code)] = fresh;
                created++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (created, updated);
    }

    /// <summary>E-posta ile eşleşir; yerel şifresi olan kullanıcılara ASLA dokunmaz, yeni gelenler şifresiz açılır (kaynakta da öyle — ilk girişte sıfırlanması beklenir).</summary>
    private async Task<(int, int)> ImportUsersAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        var rows = await connection.QueryAsync(
            new CommandDefinition(
                "SELECT kullaniciid, ad, kod, email FROM sky_kullanici WHERE engelle = 0",
                cancellationToken: cancellationToken));

        var permissionPageIds = await _db.UserPermissionPages.AsNoTracking()
            .Select(p => p.Id).ToListAsync(cancellationToken);
        var existingByEmail = await LoadExistingByNameAsync(_db.Users, x => x.Email, cancellationToken);

        int created = 0, updated = 0;
        foreach (var row in rows)
        {
            string? fullName = row.ad;
            string? email = row.email;
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email)) continue;

            if (existingByEmail.TryGetValue(Key(email), out var existing))
            {
                existing.SiberId = row.kullaniciid;
                existing.SiberName = fullName;
                existing.SiberCode = row.kod;
                updated++;
                continue;
            }

            var parts = fullName.Split(' ', 2);
            var user = new User
            {
                Name = parts[0],
                Surname = parts.Length > 1 ? parts[1] : string.Empty,
                Email = email,
                SiberId = row.kullaniciid,
                SiberName = fullName,
                SiberCode = row.kod,
                Status = true,
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);
            existingByEmail[Key(email)] = user;

            foreach (var pageId in permissionPageIds)
            {
                _db.UserPermissions.Add(new UserPermission
                {
                    UserPermissionPageId = pageId, UserId = user.Id,
                    Read = 0, Create = 0, Update = 0, Delete = 0,
                });
            }

            created++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (created, updated);
    }

    // ── Ayrı get* uçları ─────────────────────────────────────────────────────

    public async Task<SiberImportSummary> ImportLoadStatusTypesAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await OpenAsync(cancellationToken);
        var rows = await connection.QueryAsync(
            new CommandDefinition("SELECT yukdurumid, ad, sirano FROM skn_yukdurum", cancellationToken: cancellationToken));

        var existingByLoadStatusId = await _db.LoadStatusTypes
            .Where(x => x.LoadStatusId != null)
            .ToDictionaryAsync(x => x.LoadStatusId!.Value, cancellationToken);

        int created = 0, updated = 0;
        foreach (var row in rows)
        {
            string? name = row.ad;
            if (string.IsNullOrWhiteSpace(name)) continue;
            int? loadStatusId = row.yukdurumid;

            if (loadStatusId is { } lsid && existingByLoadStatusId.TryGetValue(lsid, out var existing))
            {
                existing.Name = name;
                existing.OrderNo = row.sirano;
                updated++;
            }
            else
            {
                _db.LoadStatusTypes.Add(new LoadStatusType { Name = name, LoadStatusId = loadStatusId, OrderNo = row.sirano });
                created++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new SiberImportSummary(created, updated, []);
    }

    public async Task<SiberImportSummary> ImportExpeditionTypesAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await OpenAsync(cancellationToken);
        var (created, updated) = await ImportSabitTanimTableAsync(
            connection, "SEFERTUR", cancellationToken,
            _db.ExpeditionTypes, x => x.Name,
            (x, row) => { x.SiberId = row.Sabittanimid; if (string.IsNullOrEmpty(x.Code)) x.Code = row.Kod?.ToString(); },
            row => new ExpeditionType { Name = row.Ad, GroupCode = "SEFERTUR", Code = row.Kod?.ToString(), SiberId = row.Sabittanimid },
            _db.ExpeditionTypes);
        return new SiberImportSummary(created, updated, []);
    }

    public async Task<SiberImportSummary> ImportExpeditionStatusesAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await OpenAsync(cancellationToken);
        var rows = await connection.QueryAsync(
            new CommandDefinition(
                "SELECT pozisyondurumid, ad, yukdurumid, rowguid, sirano FROM skn_pozisyondurum",
                cancellationToken: cancellationToken));

        var existingByStatusId = await _db.ExpeditionStatuses
            .Where(x => x.ExpeditionStatusId != null)
            .ToDictionaryAsync(x => x.ExpeditionStatusId!.Value, cancellationToken);

        int created = 0, updated = 0;
        foreach (var row in rows)
        {
            int? statusId = row.pozisyondurumid;
            string? name = row.ad;
            if (statusId is not { } sid || string.IsNullOrWhiteSpace(name)) continue;

            if (existingByStatusId.TryGetValue(sid, out var existing))
            {
                existing.Name = name;
                existing.LoadStatusId = row.yukdurumid;
                existing.Rowguid = row.rowguid;
                existing.OrderNumber = row.sirano;
                updated++;
            }
            else
            {
                _db.ExpeditionStatuses.Add(new ExpeditionStatus
                {
                    ExpeditionStatusId = statusId, Name = name,
                    LoadStatusId = row.yukdurumid, Rowguid = row.rowguid, OrderNumber = row.sirano,
                });
                created++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new SiberImportSummary(created, updated, []);
    }

    public async Task<SiberImportSummary> ImportCarTypesAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await OpenAsync(cancellationToken);
        var rows = await QuerySabitTanimAsync(connection, "ARACTIP", cancellationToken);
        var existingByName = await LoadExistingByNameAsync(_db.CarTypes, x => x.Name, cancellationToken);

        int created = 0, updated = 0;
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Ad)) continue;

            if (existingByName.TryGetValue(Key(row.Ad), out var existing))
            {
                existing.SiberId = row.Sabittanimid;
                updated++;
            }
            else
            {
                _db.CarTypes.Add(new CarType
                {
                    Name = row.Ad, GroupCode = "ARACTIP", Code = row.Kod,
                    SpecialCode = row.Ozelkod, SiberId = row.Sabittanimid,
                });
                created++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new SiberImportSummary(created, updated, []);
    }

    public async Task<SiberImportSummary> ImportCarStatusTypesAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await OpenAsync(cancellationToken);
        var rows = await QuerySabitTanimAsync(connection, "ARACDURUM", cancellationToken);
        var existingByName = await LoadExistingByNameAsync(_db.CarStatusTypes, x => x.Name, cancellationToken);

        int created = 0, updated = 0;
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Ad)) continue;

            if (existingByName.TryGetValue(Key(row.Ad), out var existing))
            {
                existing.SiberId = row.Sabittanimid;
                updated++;
            }
            else
            {
                _db.CarStatusTypes.Add(new CarStatusType
                {
                    Name = row.Ad, GroupCode = "ARACDURUM", Code = row.Kod,
                    SpecialCode = row.Ozelkod, SiberId = row.Sabittanimid,
                });
                created++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new SiberImportSummary(created, updated, []);
    }

    public async Task<SiberImportSummary> ImportCarOwnersAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await OpenAsync(cancellationToken);
        var rows = await QuerySabitTanimAsync(connection, "ARACSAHIP", cancellationToken);
        var existingByName = await LoadExistingByNameAsync(_db.CarOwners, x => x.Name, cancellationToken);

        int created = 0, updated = 0;
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Ad)) continue;

            if (existingByName.TryGetValue(Key(row.Ad), out var existing))
            {
                existing.SiberId = row.Sabittanimid;
                updated++;
            }
            else
            {
                _db.CarOwners.Add(new CarOwner
                {
                    Name = row.Ad, GroupCode = "ARACSAHIP", Code = row.Kod,
                    AdditionalCode = row.Ekkod, SpecialCode = row.Ozelkod, SiberId = row.Sabittanimid,
                });
                created++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new SiberImportSummary(created, updated, []);
    }

    public async Task<SiberImportSummary> ImportDeliveryMethodsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await OpenAsync(cancellationToken);
        var rows = await connection.QueryAsync(
            new CommandDefinition("SELECT teslimsekliid, edikod, ad FROM sbr_teslimsekli", cancellationToken: cancellationToken));

        var existingSiberIds = await _db.LoadTransferDeliveryMethods
            .Where(x => x.SiberId != null)
            .Select(x => x.SiberId!)
            .ToHashSetAsync(cancellationToken);

        int created = 0;
        foreach (var row in rows)
        {
            string? siberId = row.teslimsekliid;
            if (siberId is null || existingSiberIds.Contains(siberId))
                continue; // olsold: zaten varsa atlar (birebir).

            _db.LoadTransferDeliveryMethods.Add(new LoadTransferDeliveryMethod
            {
                SiberId = siberId, Edikod = row.edikod, Name = row.ad,
            });
            created++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new SiberImportSummary(created, 0, []);
    }

    private static int? ParseInt(object? value) =>
        value switch
        {
            null or DBNull => null,
            int i => i,
            _ => int.TryParse(Convert.ToString(value), out var parsed) ? parsed : null,
        };

    private static double? ParseDouble(object? value) =>
        value switch
        {
            null or DBNull => null,
            double d => d,
            decimal dec => (double)dec,
            _ => double.TryParse(Convert.ToString(value), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : null,
        };

    /// <summary>
    /// <c>skn_arac</c>'ta aractip/aracsahip/aracdurum/en/boy/yukseklik/yici/uluslararasi
    /// NVARCHAR olarak tutuluyor (gerçek Siber şemasının birebir kopyası değil, bkz.
    /// init.sql başlığı) — Dapper'ın dynamic satırından doğrudan <c>int?</c>/<c>double?</c>'a
    /// atama YAPILAMAZ (çalışma zamanı hatası verir, canlıda böyle bulundu). Bu yüzden
    /// hepsi açıkça ayrıştırılır.
    /// </summary>
    public async Task<SiberImportSummary> ImportCarsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await OpenAsync(cancellationToken);
        var rows = await connection.QueryAsync(
            new CommandDefinition(
                """
                SELECT aracid, plakano, aractip, romorkcins, aracsahip, aracdurum,
                       baglifirmaid, km, yici, uluslararasi, en, boy, yukseklik, kapasite
                FROM skn_arac
                """,
                cancellationToken: cancellationToken));

        var carTypeByCode = await _db.CarTypes.AsNoTracking()
            .Where(x => x.Code != null).ToDictionaryAsync(x => x.Code!.Value, x => x.Id, cancellationToken);
        var romorkTypeByCode = await _db.RomorkTypes.AsNoTracking()
            .Where(x => x.Code != null).ToDictionaryAsync(x => x.Code!, x => x.Id, cancellationToken);
        var carOwnerByCode = await _db.CarOwners.AsNoTracking()
            .Where(x => x.Code != null).ToDictionaryAsync(x => x.Code!.Value, x => x.Id, cancellationToken);
        var carStatusByCode = await _db.CarStatusTypes.AsNoTracking()
            .Where(x => x.Code != null).ToDictionaryAsync(x => x.Code!.Value, x => x.Id, cancellationToken);
        var accountBySiberId = await _db.Accounts.AsNoTracking()
            .Where(x => x.SiberId != null).ToDictionaryAsync(x => x.SiberId!, x => x.Id, cancellationToken);
        var existingSiberIds = await _db.Cars.Where(x => x.SiberId != null)
            .Select(x => x.SiberId!).ToHashSetAsync(cancellationToken);

        int created = 0;
        foreach (var row in rows)
        {
            string? siberId = row.aracid;
            if (siberId is null || existingSiberIds.Contains(siberId))
                continue; // olsold: zaten varsa atlar (birebir).

            int? aractip = ParseInt(row.aractip);
            string? romorkcins = row.romorkcins;
            int? aracsahip = ParseInt(row.aracsahip);
            int? aracdurum = ParseInt(row.aracdurum);
            string? baglifirmaid = row.baglifirmaid;

            _db.Cars.Add(new Car
            {
                SiberId = siberId,
                PlateNumber = row.plakano,
                CarType = aractip is { } ct && carTypeByCode.TryGetValue(ct, out var carTypeId) ? (int)carTypeId : null,
                RomorkType = romorkcins is not null && romorkTypeByCode.TryGetValue(romorkcins, out var romorkId) ? (int)romorkId : null,
                VehicleOwner = aracsahip is { } vo && carOwnerByCode.TryGetValue(vo, out var ownerId) ? (int)ownerId : null,
                VehicleStatus = aracdurum is { } vs && carStatusByCode.TryGetValue(vs, out var statusId) ? (int)statusId : null,
                CustomerId = baglifirmaid is not null && accountBySiberId.TryGetValue(baglifirmaid, out var accountId)
                    ? accountId.ToString() : null,
                Km = ParseDouble(row.km), InCountry = ParseInt(row.yici), International = ParseInt(row.uluslararasi),
                Width = ParseDouble(row.en), Length = ParseDouble(row.boy), Height = ParseDouble(row.yukseklik),
                Capacity = ParseDouble(row.kapasite),
            });
            created++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new SiberImportSummary(created, 0, []);
    }

    // ── getSiberAccount(): cari eşleme ───────────────────────────────────────

    public async Task<SiberImportSummary> ImportAccountsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await OpenAsync(cancellationToken);
        var rows = await connection.QueryAsync(
            new CommandDefinition(
                """
                SELECT f.firmaid, f.ad, f.adres1, f.telefon1, f.email, f.vergidaire, f.vergino,
                       m.muhasebekod
                FROM sbr_firma f
                LEFT JOIN sfy_muhasebeentegrekodu m ON m.entegread = f.ad
                """,
                cancellationToken: cancellationToken));

        var accountBySiberId = await _db.Accounts.Where(a => a.SiberId != null)
            .ToDictionaryAsync(a => a.SiberId!, cancellationToken);

        int created = 0, updated = 0;
        foreach (var row in rows)
        {
            string? siberId = row.firmaid;
            if (string.IsNullOrWhiteSpace(siberId)) continue;

            string? code = row.muhasebekod;

            // olsold: 320* -> tedarikçi(2), 120* -> müşteri(1), yoksa hem müşteri hem gönderici/alıcı(3,4).
            int[] typeIds = code is null
                ? [3, 4]
                : code.StartsWith("320", StringComparison.Ordinal) ? [2]
                : code.StartsWith("120", StringComparison.Ordinal) ? [1]
                : [];

            if (typeIds.Length == 0)
                continue;

            Account account;
            if (accountBySiberId.TryGetValue(siberId, out var existing))
            {
                existing.Name = row.ad ?? existing.Name;
                existing.Address = row.adres1 ?? existing.Address;
                existing.Phone = row.telefon1 ?? existing.Phone;
                existing.Email = row.email ?? existing.Email;
                existing.TaxOffice = row.vergidaire ?? existing.TaxOffice;
                existing.TaxNumber = row.vergino ?? existing.TaxNumber;
                if (code is not null) existing.AccountingCode = code;
                account = existing;
                updated++;
            }
            else
            {
                account = new Account
                {
                    SiberId = siberId, Name = row.ad, Address = row.adres1, Phone = row.telefon1,
                    Email = row.email, TaxOffice = row.vergidaire, TaxNumber = row.vergino,
                    AccountingCode = code, Discount = 0,
                };
                _db.Accounts.Add(account);
                await _db.SaveChangesAsync(cancellationToken);
                accountBySiberId[siberId] = account;
                created++;
            }

            var currentMappings = await _db.AccountTypeMappings
                .Where(m => m.AccountId == (int)account.Id).ToListAsync(cancellationToken);
            var currentTypeIds = currentMappings.Select(m => m.AccountTypeId).ToHashSet();

            foreach (var typeId in typeIds)
            {
                if (!currentTypeIds.Contains(typeId))
                    _db.AccountTypeMappings.Add(new AccountTypeMapping { AccountId = (int)account.Id, AccountTypeId = typeId });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new SiberImportSummary(created, updated, []);
    }
}
