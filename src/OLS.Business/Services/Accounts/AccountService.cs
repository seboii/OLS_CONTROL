using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;
using OLS.DataAccess.Siber;

namespace OLS.Business.Services.Accounts;

public interface IAccountService
{
    Task<object> ListAsync(AccountListQuery query, CancellationToken cancellationToken = default);
    Task<AccountDetailDto?> SingleAsync(long id, bool includeInvoices, CancellationToken cancellationToken = default);
    Task<bool> IsVisibleToUserAsync(long userId, long accountId, CancellationToken cancellationToken = default);
    Task<bool> IsSuperAdminAsync(long userId, CancellationToken cancellationToken = default);

    Task<AccountSaveResult> CreateAsync(AccountWriteModel model, CancellationToken cancellationToken = default);
    Task<AccountSaveResult> UpdateAsync(AccountWriteModel model, CancellationToken cancellationToken = default);
    Task DeleteAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cariye bağlı görevliler — teklif formunda müşteri seçilince Görevliler
    /// sekmesini önceden doldurmak için (bkz. AccountRepresentative).
    /// </summary>
    Task<AccountRepresentativesDto> RepresentativesAsync(
        long accountId, CancellationToken cancellationToken = default);
}

/// <summary>Cariye bağlı varsayılan görevliler.</summary>
public sealed class AccountRepresentativesDto
{
    [JsonPropertyName("operation_officer")] public MappedUserDto? OperationOfficer { get; init; }
    [JsonPropertyName("sales_reps")] public IReadOnlyList<MappedUserDto> SalesReps { get; init; } = [];
}

public sealed record AccountListQuery(
    long UserId,
    string? Search,
    long? AccountTypeId,
    int? PerPage,
    int Page,
    string Path,
    Guid? CountryId = null,
    long? TaxOfficeId = null,
    int? AssignedUserId = null,
    string? IndividualPersonal = null);

/// <summary>Kaydetme sonucu. Ad/e-posta çakışması olsold'da 500 + alan hatası dönüyordu.</summary>
public sealed record AccountSaveResult(AccountDetailDto? Account, string? DuplicateField)
{
    public bool IsSuccess => DuplicateField is null;
}

/// <summary>Controller'dan gelen yazma verisi (form-data).</summary>
public sealed class AccountWriteModel
{
    public long? Id { get; init; }
    public string? Name { get; init; }
    public string? TaxNumber { get; init; }
    public string? TaxOffice { get; init; }
    public int? InvoiceType { get; init; }
    public Guid? CountryId { get; init; }
    public Guid? CityId { get; init; }
    public Guid? DistrictId { get; init; }
    public string? Address { get; init; }
    public string? Phone { get; init; }
    public Guid? PhoneCountryId { get; init; }
    public string? Email { get; init; }
    public string? ContactPerson { get; init; }
    public int Discount { get; init; }
    public string? IndividualPersonal { get; init; }
    public string? ContactLanguage { get; init; }

    /// <summary>Kaydedilmiş avatar dosya adı (controller yükleme işini yapar).</summary>
    public string? AvatarFileName { get; init; }
    public bool AvatarRemoved { get; init; }

    public IReadOnlyList<int> AccountTypeMapping { get; init; } = [];
    public IReadOnlyList<ContactPersonInput> ContactPersons { get; init; } = [];
    public IReadOnlyList<int> AccountChargePersons { get; init; } = [];
}

public sealed record ContactPersonInput(string? Name, string? Email);

public sealed class AccountService : IAccountService
{
    private readonly OlsDbContext _db;
    /// <summary>account_representatives.user_type: 1 = Operasyon Yetkilisi, 2 = Satış Temsilcisi.</summary>
    private const int SalesRepresentativeType = 2;

    private readonly ISiberAccountRepository _siber;
    private readonly IClock _clock;

    private readonly ICompanyScope _companyScope;
    private readonly ICurrentUser _currentUser;

    public AccountService(
        OlsDbContext db, ISiberAccountRepository siber, IClock clock,
        ICompanyScope companyScope, ICurrentUser currentUser)
    {
        _db = db;
        _siber = siber;
        _clock = clock;
        _companyScope = companyScope;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Cariyi AÇAN kullanıcının şirketi. Siber'de cariler iki şirkete
    /// bölünmüş (6.577 OLS / 865 AVRORA); sabit yazmak Avrora carisini
    /// OLS'e düşürüyordu.
    /// </summary>
    private async Task<string> CompanyIdAsync(CancellationToken cancellationToken)
    {
        var visibility = await _companyScope.ResolveAsync(_currentUser.Id, cancellationToken);
        return visibility.OnlyCompanyId ?? SiberAccountRepository.SirketId;
    }

    /// <summary>
    /// olsold'da bu kontrol <c>UserPermission::where(...)->first()->read</c> şeklindeydi
    /// ve süper admin yetki satırı olmayan kullanıcıda null'a erişip 500 veriyordu
    /// (ör. hiç yetkisi olmayan bir kullanıcı cari listesini açtığında). Burada
    /// satır yoksa "yetkisi yok" kabul ediliyor.
    /// </summary>
    public async Task<bool> IsSuperAdminAsync(long userId, CancellationToken cancellationToken = default)
    {
        var pageId = await _db.UserPermissionPages
            .Where(p => p.PermissionPageSlug == "super_admin")
            .Select(p => (long?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (pageId is null)
            return false;

        return await _db.UserPermissions
            .AnyAsync(p => p.UserId == userId && p.UserPermissionPageId == pageId && p.Read == 1,
                cancellationToken);
    }

    public async Task<object> ListAsync(AccountListQuery query, CancellationToken cancellationToken = default)
    {
        var accounts = _db.Accounts.AsNoTracking().Where(a => a.IsActive);

        // ATANMIŞ CARİ FİLTRESİ KALDIRILDI (bilinçli).
        //
        // Eskiden süper admin olmayan kullanıcı yalnızca user_account_mappings
        // ile KENDİSİNE atanmış carileri görüyordu (olsold'dan gelen
        // nesne-seviyesi kural). Ancak bu eşleme tablosu canlıda TAMAMEN BOŞ
        // (0 satır), yani 7.443 carinin tamamı iki süper admin dışında
        // HİÇ KİMSEYE görünmüyordu — ekip müşteri listesini boş buluyordu.
        //
        // Kural, kimsenin doldurmadığı bir tabloya dayandığı için pratikte
        // "herkese kapalı" anlamına geliyordu. Cari listesi artık yetki
        // sayfasıyla (account_management) korunuyor: okuma hakkı olan tüm
        // carileri görür.

        if (query.AccountTypeId is { } typeId)
        {
            var idsWithType = _db.AccountTypeMappings
                .Where(m => m.AccountTypeId == (int)typeId)
                .Select(m => (long)(m.AccountId ?? 0));

            accounts = accounts.Where(a => idsWithType.Contains(a.Id));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Türkçe noktasız I/ı normalizasyonu için bkz. QueryableExtensions.NormalizeTurkish
            // (en_US.utf8 yerelinde ILIKE "taşıma" gibi aramaları sessizce hiç eşleştirmiyordu).
            var pattern = $"%{QueryableExtensions.NormalizeTurkish(query.Search)}%";

            // olsold: name / phone / email + ilişkili ülke ve telefon ülkesi adı.
            // (Süper admin dalında 'address' aranmıyordu, diğerinde aranıyordu —
            //  bu tutarsızlık kaynakta var; burada her iki dalda da aynı davranıyoruz.)
            var countryIds = _db.Countries
                .Where(c => EF.Functions.Like(c.Name!.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern))
                .Select(c => (Guid?)c.Id);

            accounts = accounts.Where(a =>
                EF.Functions.Like(a.Name!.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern) ||
                EF.Functions.Like(a.Phone!.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern) ||
                EF.Functions.Like(a.Email!.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern) ||
                EF.Functions.Like(a.Address!.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern) ||
                countryIds.Contains(a.CountryId) ||
                countryIds.Contains(a.PhoneCountryId));
        }

        if (query.CountryId is { } countryId)
            accounts = accounts.Where(a => a.CountryId == countryId);

        if (query.TaxOfficeId is { } taxOfficeId)
        {
            var taxOfficeIdText = taxOfficeId.ToString();
            accounts = accounts.Where(a => a.TaxOffice == taxOfficeIdText);
        }

        if (query.AssignedUserId is { } assignedUserId)
        {
            var accountIdsForUser = _db.UserAccountMappings
                .Where(m => m.UserId == assignedUserId)
                .Select(m => (long)m.AccountId);

            accounts = accounts.Where(a => accountIdsForUser.Contains(a.Id));
        }

        if (!string.IsNullOrWhiteSpace(query.IndividualPersonal))
            accounts = accounts.Where(a => a.IndividualPersonal == query.IndividualPersonal);

        var ordered = accounts.OrderBy(a => a.Name);

        var projected = ordered.Select(a => new AccountListItemDto
        {
            Id = a.Id,
            Name = a.Name,
            Phone = a.Phone,
            Email = a.Email,
            Avatar = a.Avatar,
            SiberId = a.SiberId,
            CountryId = _db.Countries.Where(c => c.Id == a.CountryId).Select(MapCountry).FirstOrDefault(),
            PhoneCountryId = _db.Countries.Where(c => c.Id == a.PhoneCountryId).Select(MapCountry).FirstOrDefault(),
            TaxOffice = _db.TaxOffices
                .Where(t => a.TaxOffice != null && t.Id.ToString() == a.TaxOffice)
                .Select(t => new TaxOfficeDto { Id = t.Id, Name = t.Name, City = t.City, SpecialCode = t.SpecialCode })
                .FirstOrDefault(),
            AccountTypeMappingId = _db.AccountTypeMappings
                .Where(m => m.AccountId == (int)a.Id)
                .Select(m => new AccountTypeMappingDto
                {
                    Id = m.Id,
                    AccountId = m.AccountId,
                    SiberId = m.SiberId,
                    AccountTypeId = _db.AccountTypes
                        .Where(t => t.Id == m.AccountTypeId)
                        .Select(t => new AccountTypeDto { Id = t.Id, Name = t.Name, SiberId = t.SiberId })
                        .FirstOrDefault(),
                })
                .ToList(),
        });

        return await projected.ToPagedOrListAsync(
            query.PerPage, query.Page, query.Path, cancellationToken);
    }

    public async Task<bool> IsVisibleToUserAsync(
        long userId, long accountId, CancellationToken cancellationToken = default)
    {
        if (await IsSuperAdminAsync(userId, cancellationToken))
            return true;

        return await _db.UserAccountMappings
            .AnyAsync(m => m.UserId == userId && m.AccountId == (int)accountId, cancellationToken);
    }

    public async Task<AccountDetailDto?> SingleAsync(long id, bool includeInvoices, CancellationToken cancellationToken = default)
    {
        var account = await _db.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (account is null)
            return null;

        return await BuildDetailAsync(account, includeInvoices, cancellationToken);
    }

    public async Task<AccountSaveResult> CreateAsync(
        AccountWriteModel model, CancellationToken cancellationToken = default)
    {
        if (await _db.Accounts.AnyAsync(a => a.Name == model.Name, cancellationToken))
            return new AccountSaveResult(null, "name");

        if (!string.IsNullOrWhiteSpace(model.Email) &&
            await _db.Accounts.AnyAsync(a => a.Email == model.Email, cancellationToken))
        {
            return new AccountSaveResult(null, "email");
        }

        // Siber tarafındaki firmaid; çakışmayan bir GUID üretilir.
        var siberId = _siber.IsConfigured
            ? (await _siber.GenerateFirmaIdAsync(cancellationToken)).ToString()
            : Guid.NewGuid().ToString();

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var account = new Account
        {
            Name = model.Name,
            TaxNumber = model.TaxNumber,
            TaxOffice = model.TaxOffice,
            InvoiceType = model.InvoiceType,
            CountryId = model.CountryId,
            CityId = model.CityId,
            DistrictId = model.DistrictId,
            Address = model.Address,
            Avatar = model.AvatarFileName,
            Phone = model.Phone,
            PhoneCountryId = model.PhoneCountryId,
            Email = model.Email,
            ContactPerson = model.ContactPerson,
            Discount = model.Discount,
            IndividualPersonal = model.IndividualPersonal,
            SiberId = siberId,
            ContactLanguage = model.ContactLanguage,
            CreatedAt = _clock.Now,
            UpdatedAt = _clock.Now,
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);

        var (alici, satici) = await WriteChildRowsAsync(account, model, cancellationToken);

        // SIRA ÖNEMLİ — ÖNCE SİBER, SONRA YEREL COMMIT.
        //
        // Eskiden tam tersiydi ve yorumda "Siber yazımı başarısız olursa cari
        // yerelde kalır" diye kabul edilmişti. Bunun bedeli somut: cari yerelde
        // Siber'de KARŞILIĞI OLMAYAN bir siber_id ile duruyor ve o cari bir
        // teklifte müşteri seçilince Siber'e yazma FK hatasıyla patlıyor
        // ("beklenmeyen hata"). Aynı ders yük tarafında da alınmıştı
        // (bkz. LoadTransferWriteService.DeleteAsync). Siber önce yazılırsa
        // hata durumunda yerel taraf tamamen geri alınır ve tutarsız kayıt
        // hiç oluşmaz.
        await SyncToSiberAsync(account, model, alici, satici, isNew: true, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        // olsold: save() yanıtı 'Invoice' ilişkisini hiç yüklemiyordu.
        var detail = await BuildDetailAsync(account, includeInvoices: false, cancellationToken);
        return new AccountSaveResult(detail, null);
    }

    public async Task<AccountSaveResult> UpdateAsync(
        AccountWriteModel model, CancellationToken cancellationToken = default)
    {
        var id = model.Id ?? throw new ArgumentException("Id zorunlu", nameof(model));

        if (await _db.Accounts.AnyAsync(a => a.Name == model.Name && a.Id != id, cancellationToken))
            return new AccountSaveResult(null, "name");

        if (!string.IsNullOrWhiteSpace(model.Email) &&
            await _db.Accounts.AnyAsync(a => a.Email == model.Email && a.Id != id, cancellationToken))
        {
            return new AccountSaveResult(null, "email");
        }

        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Cari bulunamadı: {id}");

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        account.Name = model.Name;
        account.TaxNumber = model.TaxNumber;
        account.TaxOffice = model.TaxOffice;
        account.InvoiceType = model.InvoiceType;
        account.CountryId = model.CountryId;
        account.CityId = model.CityId;
        account.DistrictId = model.DistrictId;
        account.Address = model.Address;
        account.Phone = model.Phone;
        account.PhoneCountryId = model.PhoneCountryId;
        account.Email = model.Email;
        account.ContactPerson = model.ContactPerson;
        account.Discount = model.Discount;
        account.IndividualPersonal = model.IndividualPersonal;
        account.ContactLanguage = model.ContactLanguage;
        account.UpdatedAt = _clock.Now;

        // Avatar: kaldırıldıysa null, yeni dosya geldiyse o, aksi halde mevcut kalır.
        if (model.AvatarRemoved)
            account.Avatar = null;
        else if (model.AvatarFileName is not null)
            account.Avatar = model.AvatarFileName;

        // olsold deseni: alt kayıtlar silinip yeniden yazılır.
        var oldContacts = _db.AccountContactPeople.Where(c => c.AccountId == (int)id);
        _db.AccountContactPeople.RemoveRange(oldContacts);

        var oldTypes = _db.AccountTypeMappings.Where(m => m.AccountId == (int)id);
        _db.AccountTypeMappings.RemoveRange(oldTypes);

        var oldUsers = _db.UserAccountMappings.Where(m => m.AccountId == (int)id);
        _db.UserAccountMappings.RemoveRange(oldUsers);

        // Temsilci satırları da aynı sil-yeniden-yaz desenine dahil; aksi hâlde
        // görevliden çıkarılan kişi teklif formunda görünmeye devam ederdi.
        // Siber'den senkronlanmış satırlara (siber_id dolu) DOKUNULMAZ: onların
        // sahibi Siber, burada silinirse bir sonraki senkron zaten geri getirir
        // ve bu arada müşteri temsilcisiz görünürdü.
        var oldReps = _db.AccountRepresentatives
            .Where(r => r.AccountId == (int)id && r.SiberId == null);
        _db.AccountRepresentatives.RemoveRange(oldReps);

        await _db.SaveChangesAsync(cancellationToken);

        var (alici, satici) = await WriteChildRowsAsync(account, model, cancellationToken);

        // Create ile aynı gerekçe: önce Siber, sonra yerel commit.
        await SyncToSiberAsync(account, model, alici, satici, isNew: false, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        // olsold: update() yanıtı da 'Invoice' ilişkisini yüklemiyordu.
        var detail = await BuildDetailAsync(account, includeInvoices: false, cancellationToken);
        return new AccountSaveResult(detail, null);
    }

    /// <summary>
    /// olsold'da cari silme HARD delete: accounts tablosunda deleted_at yok.
    /// Siber tarafındaki silme satırları kaynak kodda yorum satırındaydı
    /// (muhtemelen Siber'deki geçmiş hareketler FK ile bağlı olduğu için),
    /// bu yüzden burada da Siber'e dokunmuyoruz.
    /// </summary>
    /// <summary>
    /// Siber'den senkronlanan cari-görevli bağını okur (bkz.
    /// SiberSyncService.SyncAccountRepresentativesAsync). Operasyon Yetkilisi tekil,
    /// Satış Temsilcisi çoğul — teklif formundaki alanlarla aynı şekil.
    /// </summary>
    public async Task<AccountRepresentativesDto> RepresentativesAsync(
        long accountId, CancellationToken cancellationToken = default)
    {
        var reps = await _db.AccountRepresentatives.AsNoTracking()
            .Where(r => r.AccountId == (int)accountId)
            .Join(_db.Users.AsNoTracking(), r => (long)r.UserId, u => u.Id, (r, u) => new { r.UserType, User = u })
            .OrderBy(x => x.User.Name)
            .ToListAsync(cancellationToken);

        static MappedUserDto Map(dynamic x) => new()
        {
            Id = x.User.Id,
            Name = x.User.Name ?? string.Empty,
            Surname = x.User.Surname ?? string.Empty,
            Email = x.User.Email ?? string.Empty,
            Avatar = x.User.Avatar,
            SiberCode = x.User.SiberCode,
        };

        return new AccountRepresentativesDto
        {
            OperationOfficer = reps.Where(x => x.UserType == 1).Select(x => Map(x)).FirstOrDefault(),
            SalesReps = reps.Where(x => x.UserType == 2).Select(x => Map(x)).ToList(),
        };
    }

    public async Task DeleteAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        foreach (var id in ids)
        {
            var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (account is null)
                continue;

            _db.AccountContactPeople.RemoveRange(
                _db.AccountContactPeople.Where(c => c.AccountId == (int)id));
            _db.AccountTypeMappings.RemoveRange(
                _db.AccountTypeMappings.Where(m => m.AccountId == (int)id));
            _db.UserAccountMappings.RemoveRange(
                _db.UserAccountMappings.Where(m => m.AccountId == (int)id));
            // Temsilci bağları da silinmeli — aksi hâlde cari gittikten sonra
            // account_representatives'ta hiçbir cariye ait olmayan satırlar kalır.
            _db.AccountRepresentatives.RemoveRange(
                _db.AccountRepresentatives.Where(r => r.AccountId == (int)id));

            _db.Accounts.Remove(account);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Cari tipi / ilgili kişi / sorumlu kullanıcı satırlarını yazar.</summary>
    private async Task<(int Alici, int Satici)> WriteChildRowsAsync(
        Account account, AccountWriteModel model, CancellationToken cancellationToken)
    {
        var alici = 0;
        var satici = 0;

        foreach (var typeId in model.AccountTypeMapping)
        {
            // olsold eşlemesi: 1,3 -> alıcı; 2,4 -> satıcı
            if (typeId is 1 or 3) alici = 1;
            else if (typeId is 2 or 4) satici = 1;

            _db.AccountTypeMappings.Add(new AccountTypeMapping
            {
                AccountId = (int)account.Id,
                AccountTypeId = typeId,
                CreatedAt = _clock.Now,
                UpdatedAt = _clock.Now,
            });
        }

        foreach (var person in model.ContactPersons)
        {
            _db.AccountContactPeople.Add(new AccountContactPerson
            {
                AccountId = (int)account.Id,
                Name = person.Name,
                Email = person.Email,
                CreatedAt = _clock.Now,
                UpdatedAt = _clock.Now,
            });
        }

        foreach (var userId in model.AccountChargePersons)
        {
            _db.UserAccountMappings.Add(new UserAccountMapping
            {
                AccountId = (int)account.Id,
                UserId = userId,
                CreatedAt = _clock.Now,
                UpdatedAt = _clock.Now,
            });

            // BULUNAN GERÇEK BOŞLUK: buraya kadar yalnızca user_account_mappings
            // (görünürlük/yetki) yazılıyordu. Teklif formundaki "Satış Temsilcisi
            // müşteriye bağlı olsun" kuralı ise account_representatives tablosunu
            // okuyor — bu yüzden uygulamadan açılan bir carinin satış temsilcisi
            // teklifte HİÇ görünmüyor, her seferinde operasyon yetkilisine
            // düşülüyordu. Arayüzdeki sekme bu kişileri zaten "Satış Temsilcisi"
            // diye topluyor, dolayısıyla user_type = 2.
            //
            // Siber'e de yazılıyor (bkz. SyncToSiberAsync) ama senkron turunu
            // beklememek için yerel satır burada doğrudan açılır: kullanıcı cariyi
            // kaydedip hemen teklif açtığında temsilci dolu gelsin.
            _db.AccountRepresentatives.Add(new AccountRepresentative
            {
                AccountId = (int)account.Id,
                UserId = userId,
                UserType = SalesRepresentativeType,
                CreatedAt = _clock.Now,
                UpdatedAt = _clock.Now,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (alici, satici);
    }

    private async Task SyncToSiberAsync(
        Account account, AccountWriteModel model, int alici, int satici,
        bool isNew, CancellationToken cancellationToken)
    {
        if (!_siber.IsConfigured || account.SiberId is null)
            return;

        var taxOffice = account.TaxOffice is null
            ? null
            : await _db.TaxOffices
                .Where(t => t.Id.ToString() == account.TaxOffice)
                .Select(t => new { t.Name, t.SiberId })
                .FirstOrDefaultAsync(cancellationToken);

        var firma = new SiberFirma
        {
            FirmaId = account.SiberId,
            SirketId = await CompanyIdAsync(cancellationToken),
            Ad = account.Name,
            // olsold adres alanını 50 karaktere kırpıyordu (Siber sütun sınırı)
            Adres1 = account.Address?[..Math.Min(account.Address.Length, 50)],
            Telefon1 = account.Phone,
            Email = account.Email,
            VergiDaire = taxOffice?.Name,
            VergiDaireId = taxOffice?.SiberId,
            VergiNo = account.TaxNumber,
            MuhasebeKod = account.AccountingCode,
            UlkeId = account.CountryId?.ToString(),
            SehirId = account.CityId?.ToString(),
            IlceId = account.DistrictId?.ToString(),
            FirmaDurumId = SiberAccountRepository.FirmaDurumId,
            Alici = alici,
            Satici = satici,
            SahisTuzel = account.IndividualPersonal,
        };

        if (isNew)
            await _siber.InsertFirmaAsync(firma, cancellationToken);
        else
            await _siber.UpdateFirmaAsync(firma, cancellationToken);

        // Sorumlu kullanıcılar: sil-yeniden-ekle (olsold deseni)
        await _siber.DeleteFirmaTemsilcileriAsync(account.SiberId, cancellationToken);

        if (model.AccountChargePersons.Count == 0)
            return;

        var users = await _db.Users
            .Where(u => model.AccountChargePersons.Contains((int)u.Id))
            .Select(u => new { u.Id, u.SiberName, u.SiberCode })
            .ToListAsync(cancellationToken);

        foreach (var user in users)
        {
            await _siber.InsertFirmaTemsilcisiAsync(new SiberFirmaTemsilcisi
            {
                FirmaTemsilciId = Guid.NewGuid().ToString(),
                FirmaId = account.SiberId,
                Ad = user.SiberName,
                // kod sütunu ŞART: cari görevlisi senkronu kullanıcıyı önce KODLA
                // eşliyor (ada göre eşleşme yalnızca yedek). Boş bırakılırsa satır
                // yalnızca ad benzerliğine kalıyordu.
                Kod = user.SiberCode,
                InsTime = _clock.Now,
                InsUser = user.SiberCode,
                MusteriTemsilcisi = 1,
                // BULUNAN GERÇEK HATA: burası eskiden satistemsilcisi = 0 yazıyordu.
                // Arayüzdeki sekme bu kişileri "Satış Temsilcisi" diye topluyor, ama
                // Siber'e satış bayrağı KAPALI gidiyordu — ve cari görevlisi senkronu
                // rolü tam da bu sütundan okuduğu için satır "rolsüz" sayılıp
                // atlanıyordu. Sonuç: uygulamadan açılan/düzenlenen carinin
                // account_representatives kaydı HİÇ oluşmuyor, dolayısıyla teklif
                // formundaki "Satış Temsilcisi müşteriden gelsin" kuralı bu carilerde
                // hiçbir zaman çalışmıyordu.
                //
                // Siber'in kendi verisi de bunu doğruluyor: 4375 firmatemsilci
                // satırının 4375'inde satistemsilcisi = 1.
                SatisTemsilcisi = 1,
                // Arayüz yalnızca satış temsilcisi topluyor; olmayan bir operasyon
                // yetkilisi uydurmuyoruz (Siber'de bu bayrak 3495/4375 satırda dolu,
                // yani her zaman değil).
                OperasyonYetkilisi = 0,
            }, cancellationToken);
        }
    }

    /// <summary>
    /// olsold: <c>single()</c> yalnızca süper admin dalında <c>'Invoice'</c> ilişkisini
    /// eager-load ediyordu; normal kullanıcı dalında (ve save/update yanıtlarında) bu
    /// ilişki hiç yüklenmiyor, dolayısıyla JSON'da yer almıyordu.
    /// </summary>
    private async Task<AccountDetailDto> BuildDetailAsync(
        Account account, bool includeInvoices, CancellationToken cancellationToken)
    {
        var country = await LoadCountryAsync(account.CountryId, cancellationToken);
        var phoneCountry = await LoadCountryAsync(account.PhoneCountryId, cancellationToken);

        CountryDto? contactLanguage = null;
        if (Guid.TryParse(account.ContactLanguage, out var languageId))
            contactLanguage = await LoadCountryAsync(languageId, cancellationToken);

        var city = await _db.Cities.AsNoTracking()
            .Where(c => c.Id == account.CityId)
            .Select(c => new CityDto { Id = c.Id, Name = c.Name, CountryId = c.CountryId })
            .FirstOrDefaultAsync(cancellationToken);

        var district = await _db.Districts.AsNoTracking()
            .Where(d => d.Id == account.DistrictId)
            .Select(d => new DistrictDto { Id = d.Id, Name = d.Name, CityId = d.CityId })
            .FirstOrDefaultAsync(cancellationToken);

        var taxOffice = await _db.TaxOffices.AsNoTracking()
            .Where(t => account.TaxOffice != null && t.Id.ToString() == account.TaxOffice)
            .Select(t => new TaxOfficeDto { Id = t.Id, Name = t.Name, City = t.City, SpecialCode = t.SpecialCode })
            .FirstOrDefaultAsync(cancellationToken);

        var typeMappings = await _db.AccountTypeMappings.AsNoTracking()
            .Where(m => m.AccountId == (int)account.Id)
            .Select(m => new AccountTypeMappingDto
            {
                Id = m.Id,
                AccountId = m.AccountId,
                SiberId = m.SiberId,
                AccountTypeId = _db.AccountTypes
                    .Where(t => t.Id == m.AccountTypeId)
                    .Select(t => new AccountTypeDto { Id = t.Id, Name = t.Name, SiberId = t.SiberId })
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        var contacts = await _db.AccountContactPeople.AsNoTracking()
            .Where(c => c.AccountId == (int)account.Id)
            .Select(c => new AccountContactPersonDto
            {
                Id = c.Id, AccountId = c.AccountId, Name = c.Name, Email = c.Email,
            })
            .ToListAsync(cancellationToken);

        var userMappings = await _db.UserAccountMappings.AsNoTracking()
            .Where(m => m.AccountId == (int)account.Id)
            .Select(m => new UserAccountMappingDto
            {
                Id = m.Id,
                AccountId = m.AccountId,
                UserId = _db.Users
                    .Where(u => u.Id == m.UserId)
                    .Select(u => new MappedUserDto
                    {
                        Id = u.Id, Name = u.Name, Surname = u.Surname,
                        Email = u.Email, Avatar = u.Avatar, SiberCode = u.SiberCode,
                    })
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        var invoices = includeInvoices
            ? await _db.Invoices.AsNoTracking()
                .Where(i => i.AccountId == account.Id)
                .OrderByDescending(i => i.Id)
                .Select(i => new AccountInvoiceDto
                {
                    Id = i.Id,
                    InvoiceId = i.InvoiceId,
                    BoxType = i.BoxType,
                    CommercialType = i.CommercialType,
                    TargetTitle = i.TargetTitle,
                    TargetIdentityNo = i.TargetIdentityNo,
                    PayableAmount = i.PayableAmount,
                    TaxExclusiveAmount = i.TaxExclusiveAmount,
                    TaxAmount = i.TaxAmount,
                    TaxRate = i.TaxRate,
                    DocumentCurrencyCode = i.DocumentCurrencyCode,
                    InvoiceType = i.InvoiceType == null ? null
                        : new AccountTypeDto { Id = i.InvoiceType.Id, Name = i.InvoiceType.Name ?? string.Empty },
                    InvoiceStatus = i.InvoiceStatus == null ? null
                        : new AccountTypeDto { Id = i.InvoiceStatus.Id, Name = i.InvoiceStatus.Name ?? string.Empty },
                })
                .ToListAsync(cancellationToken)
            : [];

        return new AccountDetailDto
        {
            Id = account.Id,
            Name = account.Name,
            TaxNumber = account.TaxNumber,
            AccountingCode = account.AccountingCode,
            InvoiceType = account.InvoiceType,
            Address = account.Address,
            Phone = account.Phone,
            Avatar = account.Avatar,
            Email = account.Email,
            ContactPerson = account.ContactPerson,
            IndividualPersonal = account.IndividualPersonal,
            Discount = account.Discount,
            SiberId = account.SiberId,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt,
            CountryId = country,
            CityId = city,
            DistrictId = district,
            PhoneCountryId = phoneCountry,
            ContactLanguage = contactLanguage,
            TaxOffice = taxOffice,
            AccountTypeMappingId = typeMappings,
            AccountContactPerson = contacts,
            UserAccountMapping = userMappings,
            Invoice = invoices,
        };
    }

    private async Task<CountryDto?> LoadCountryAsync(Guid? id, CancellationToken cancellationToken)
    {
        if (id is null)
            return null;

        return await _db.Countries.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(MapCountry)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static readonly System.Linq.Expressions.Expression<Func<Country, CountryDto>> MapCountry =
        c => new CountryDto
        {
            Id = c.Id,
            Name = c.Name,
            CountryCode = c.CountryCode,
            Flag = c.Flag,
            PhoneCode = c.PhoneCode,
            Slug = c.Slug,
        };
}
