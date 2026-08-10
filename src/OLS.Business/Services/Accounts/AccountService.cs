using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;
using OLS.DataAccess.Siber;

namespace OLS.Business.Services.Accounts;

public interface IAccountService
{
    Task<object> ListAsync(AccountListQuery query, CancellationToken cancellationToken = default);
    Task<AccountDetailDto?> SingleAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> IsVisibleToUserAsync(long userId, long accountId, CancellationToken cancellationToken = default);
    Task<bool> IsSuperAdminAsync(long userId, CancellationToken cancellationToken = default);

    Task<AccountSaveResult> CreateAsync(AccountWriteModel model, CancellationToken cancellationToken = default);
    Task<AccountSaveResult> UpdateAsync(AccountWriteModel model, CancellationToken cancellationToken = default);
    Task DeleteAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default);
}

public sealed record AccountListQuery(
    long UserId,
    string? Search,
    long? AccountTypeId,
    int? PerPage,
    int Page,
    string Path);

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
    private readonly ISiberAccountRepository _siber;
    private readonly IClock _clock;

    public AccountService(OlsDbContext db, ISiberAccountRepository siber, IClock clock)
    {
        _db = db;
        _siber = siber;
        _clock = clock;
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
        var isSuperAdmin = await IsSuperAdminAsync(query.UserId, cancellationToken);

        var accounts = _db.Accounts.AsNoTracking();

        // Süper admin değilse yalnızca kendisine atanmış cariler görünür.
        if (!isSuperAdmin)
        {
            var allowedIds = _db.UserAccountMappings
                .Where(m => m.UserId == query.UserId)
                .Select(m => (long)m.AccountId);

            accounts = accounts.Where(a => allowedIds.Contains(a.Id));
        }

        if (query.AccountTypeId is { } typeId)
        {
            var idsWithType = _db.AccountTypeMappings
                .Where(m => m.AccountTypeId == (int)typeId)
                .Select(m => (long)(m.AccountId ?? 0));

            accounts = accounts.Where(a => idsWithType.Contains(a.Id));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search}%";

            // olsold: name / phone / email + ilişkili ülke ve telefon ülkesi adı.
            // (Süper admin dalında 'address' aranmıyordu, diğerinde aranıyordu —
            //  bu tutarsızlık kaynakta var; burada her iki dalda da aynı davranıyoruz.)
            var countryIds = _db.Countries
                .Where(c => EF.Functions.ILike(c.Name!, pattern))
                .Select(c => (Guid?)c.Id);

            accounts = accounts.Where(a =>
                EF.Functions.ILike(a.Name!, pattern) ||
                EF.Functions.ILike(a.Phone!, pattern) ||
                EF.Functions.ILike(a.Email!, pattern) ||
                EF.Functions.ILike(a.Address!, pattern) ||
                countryIds.Contains(a.CountryId) ||
                countryIds.Contains(a.PhoneCountryId));
        }

        var ordered = accounts.OrderBy(a => a.Name);

        var projected = ordered.Select(a => new AccountListItemDto
        {
            Id = a.Id,
            Name = a.Name,
            Phone = a.Phone,
            Email = a.Email,
            Avatar = a.Avatar,
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

    public async Task<AccountDetailDto?> SingleAsync(long id, CancellationToken cancellationToken = default)
    {
        var account = await _db.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (account is null)
            return null;

        return await BuildDetailAsync(account, cancellationToken);
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

        await transaction.CommitAsync(cancellationToken);

        // PostgreSQL commit edildikten SONRA Siber'e yazıyoruz. İki veritabanı
        // arasında dağıtık işlem yok; Siber yazımı başarısız olursa cari yerelde
        // kalır ve senkron dışı olur. olsold da aynı riski taşıyordu.
        await SyncToSiberAsync(account, model, alici, satici, isNew: true, cancellationToken);

        var detail = await BuildDetailAsync(account, cancellationToken);
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

        await _db.SaveChangesAsync(cancellationToken);

        var (alici, satici) = await WriteChildRowsAsync(account, model, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        await SyncToSiberAsync(account, model, alici, satici, isNew: false, cancellationToken);

        var detail = await BuildDetailAsync(account, cancellationToken);
        return new AccountSaveResult(detail, null);
    }

    /// <summary>
    /// olsold'da cari silme HARD delete: accounts tablosunda deleted_at yok.
    /// Siber tarafındaki silme satırları kaynak kodda yorum satırındaydı
    /// (muhtemelen Siber'deki geçmiş hareketler FK ile bağlı olduğu için),
    /// bu yüzden burada da Siber'e dokunmuyoruz.
    /// </summary>
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
            SirketId = SiberAccountRepository.SirketId,
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
                InsTime = _clock.Now,
                InsUser = user.SiberCode,
                MusteriTemsilcisi = 1,
                SatisTemsilcisi = 0,
            }, cancellationToken);
        }
    }

    private async Task<AccountDetailDto> BuildDetailAsync(
        Account account, CancellationToken cancellationToken)
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
