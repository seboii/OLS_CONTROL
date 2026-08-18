using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.Business.Services.Accounts;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.Business.Services.Loads;

public interface ILoadService
{
    Task<object> ListAsync(LoadListQuery query, CancellationToken cancellationToken = default);
    Task<LoadDetailDto?> SingleAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Yük numarası oluşmuş kayıt silinemez (olsold kuralı).</summary>
    Task<LoadDeleteResult> DeleteAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default);

    /// <summary>Yük numarası oluşmuş kayıt güncellenemez (olsold kuralı).</summary>
    Task<LoadTimeOutUpdateStatus> UpdateTimeOutAsync(long id, CancellationToken cancellationToken = default);
}

public enum LoadTimeOutUpdateStatus { NotFound, Locked, Success }

public sealed record LoadListQuery(
    long UserId,
    string? Search,
    int? StatusTypeId,
    bool TimeoutOnly,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    int? PerPage,
    int Page,
    string Path);

public sealed record LoadDeleteResult(bool Success, string? BlockedByLoadNumber);

public sealed class LoadService : ILoadService
{
    private readonly OlsDbContext _db;
    private readonly IAccountService _accounts;
    private readonly IClock _clock;

    public LoadService(OlsDbContext db, IAccountService accounts, IClock clock)
    {
        _db = db;
        _accounts = accounts;
        _clock = clock;
    }

    public async Task<object> ListAsync(LoadListQuery query, CancellationToken cancellationToken = default)
    {
        var loads = _db.Loads.AsNoTracking();

        // Süper admin değilse: ya yüke görevli atanmış olmalı, ya da yükün
        // müşterisi kendisine atanmış carilerden biri olmalı.
        if (!await _accounts.IsSuperAdminAsync(query.UserId, cancellationToken))
        {
            var chargedLoadIds = _db.LoadChargePeople
                .Where(p => p.UserId == (int)query.UserId)
                .Select(p => (long)(p.LoadId ?? 0));

            var mappedAccountIds = _db.UserAccountMappings
                .Where(m => m.UserId == (int)query.UserId)
                .Select(m => m.AccountId);

            loads = loads.Where(l =>
                chargedLoadIds.Contains(l.Id) ||
                (l.CustomerId != null && mappedAccountIds.Contains(l.CustomerId.Value)));
        }

        if (query.StatusTypeId is { } statusId)
            loads = loads.Where(l => l.StatusTypeId == statusId);

        // Zaman aşımı filtresi: teklif aşamasındaki (2,3,4,5) ve yük numarası
        // oluşmamış kayıtlardan bir haftadır güncellenmemiş olanlar.
        if (query.TimeoutOnly)
        {
            var oneWeekAgo = _clock.Now.AddDays(-7);
            int[] offerStatuses = [2, 3, 4, 5];

            loads = loads.Where(l =>
                l.StatusTypeId != null && offerStatuses.Contains(l.StatusTypeId.Value) &&
                l.LoadNumber == null &&
                l.UpdatedAt <= oneWeekAgo);
        }

        if (query.DateFrom is { } dateFrom)
        {
            var from = dateFrom.ToDateTime(TimeOnly.MinValue);
            loads = loads.Where(l => l.CreatedAt >= from);
        }

        if (query.DateTo is { } dateTo)
        {
            var to = dateTo.AddDays(1).ToDateTime(TimeOnly.MinValue);
            loads = loads.Where(l => l.CreatedAt < to);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search}%";

            // olsold aramayı yük numarası / rezervasyon no / tarihler ve ilişkili
            // iş tipi, yükleme tipi, müşteri/gönderici/alıcı/acente adları ile
            // onların ülke adları üzerinde yapıyordu.
            var matchingAccountIds = _db.Accounts
                .Where(a => EF.Functions.ILike(a.Name!, pattern) ||
                            _db.Countries.Any(c => c.Id == a.CountryId &&
                                                   EF.Functions.ILike(c.Name!, pattern)))
                .Select(a => a.Id);

            var matchingWorkTypeIds = _db.WorkTypes
                .Where(w => EF.Functions.ILike(w.Name, pattern))
                .Select(w => w.Id);

            var matchingLoadingTypeIds = _db.LoadingTypes
                .Where(t => EF.Functions.ILike(t.Name!, pattern))
                .Select(t => t.Id);

            loads = loads.Where(l =>
                EF.Functions.ILike(l.LoadNumber!, pattern) ||
                EF.Functions.ILike(l.ReservationNumber!, pattern) ||
                (l.WorkTypeId != null && matchingWorkTypeIds.Contains(l.WorkTypeId.Value)) ||
                (l.LoadingTypeId != null && matchingLoadingTypeIds.Contains(l.LoadingTypeId.Value)) ||
                (l.CustomerId != null && matchingAccountIds.Contains(l.CustomerId.Value)) ||
                (l.SenderId != null && matchingAccountIds.Contains(l.SenderId.Value)) ||
                (l.ReceiverId != null && matchingAccountIds.Contains(l.ReceiverId.Value)) ||
                (l.AgentId != null && matchingAccountIds.Contains(l.AgentId.Value)));
        }

        var projected = loads
            .OrderByDescending(l => l.Id)
            .Select(l => new LoadListItemDto
            {
                Id = l.Id,
                ReservationNumber = l.ReservationNumber,
                LoadNumber = l.LoadNumber,
                OfferDate = l.OfferDate,
                OfferValidityDate = l.OfferValidityDate,
                MarketingNotificationDate = l.MarketingNotificationDate,
                Description = l.Description,
                PayerCompany = l.PayerCompany,
                FrontTransportationByUs = l.FrontTransportationByUs,
                FinalTransportationByUs = l.FinalTransportationByUs,
                WayOfWorking = l.WayOfWorking,
                TransferToSiber = l.TransferToSiber,
                SiberId = l.SiberId,
                StatusTypeIdRaw = l.StatusTypeId,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt,

                LoadContentCount = _db.LoadContents.Count(c => c.LoadId == l.Id),

                WorkTypeId = _db.WorkTypes.Where(w => w.Id == l.WorkTypeId)
                    .Select(w => new NamedRefDto { Id = w.Id, Name = w.Name, Code = w.Code, SiberId = w.SiberId })
                    .FirstOrDefault(),
                LoadingTypeId = _db.LoadingTypes.Where(t => t.Id == l.LoadingTypeId)
                    .Select(t => new NamedRefDto { Id = t.Id, Name = t.Name, SiberId = t.SiberId })
                    .FirstOrDefault(),
                LoadTransferTypeId = _db.LoadTransferTypes.Where(t => t.Id == l.LoadTransferTypeId)
                    .Select(t => new NamedRefDto { Id = t.Id, Name = t.Name, SiberId = t.SiberId })
                    .FirstOrDefault(),
                InstructionId = _db.Instructions.Where(t => t.Id == l.InstructionId)
                    .Select(t => new NamedRefDto { Id = t.Id, Name = t.Name, SiberId = t.SiberId })
                    .FirstOrDefault(),
                RomorkTypeId = _db.RomorkTypes.Where(t => t.Id == l.RomorkTypeId)
                    .Select(t => new NamedRefDto { Id = t.Id, Name = t.Name, SiberId = t.SiberId })
                    .FirstOrDefault(),
                DepartmentId = _db.Departments.Where(t => t.Id == l.DepartmentId)
                    .Select(t => new NamedRefDto { Id = t.Id, Name = t.Name, SiberId = t.SiberId })
                    .FirstOrDefault(),

                CustomerId = _db.Accounts.Where(a => a.Id == l.CustomerId).Select(MapAccountRef).FirstOrDefault(),
                SenderId = _db.Accounts.Where(a => a.Id == l.SenderId).Select(MapAccountRef).FirstOrDefault(),
                ReceiverId = _db.Accounts.Where(a => a.Id == l.ReceiverId).Select(MapAccountRef).FirstOrDefault(),
                AgentId = _db.Accounts.Where(a => a.Id == l.AgentId).Select(MapAccountRef).FirstOrDefault(),
                CompanyPayFreightId = _db.Accounts.Where(a => a.Id == l.CompanyPayFreightId).Select(MapAccountRef).FirstOrDefault(),

                LoadChargePerson = _db.LoadChargePeople
                    .Where(p => p.LoadId == (int)l.Id)
                    .Select(p => new LoadChargePersonDto
                    {
                        Id = p.Id,
                        LoadId = p.LoadId,
                        UserType = p.UserType,
                        UserId = _db.Users.Where(u => u.Id == p.UserId)
                            .Select(u => new MappedUserDto
                            {
                                Id = u.Id, Name = u.Name, Surname = u.Surname,
                                Email = u.Email, Avatar = u.Avatar, SiberCode = u.SiberCode,
                            })
                            .FirstOrDefault(),
                    })
                    .ToList(),
            });

        return await projected.ToPagedOrListAsync(
            query.PerPage, query.Page, query.Path, cancellationToken);
    }

    public async Task<LoadDetailDto?> SingleAsync(long id, CancellationToken cancellationToken = default)
    {
        var l = await _db.Loads.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (l is null)
            return null;

        return new LoadDetailDto
        {
            Id = l.Id,
            ReservationNumber = l.ReservationNumber,
            LoadNumber = l.LoadNumber,
            OfferDate = l.OfferDate,
            OfferValidityDate = l.OfferValidityDate,
            MarketingNotificationDate = l.MarketingNotificationDate,
            Description = l.Description,
            PayerCompany = l.PayerCompany,
            FrontTransportationByUs = l.FrontTransportationByUs,
            FinalTransportationByUs = l.FinalTransportationByUs,
            WayOfWorking = l.WayOfWorking,
            TransferToSiber = l.TransferToSiber,
            SiberId = l.SiberId,
            MailId = l.MailId,
            CreatedAt = l.CreatedAt,
            UpdatedAt = l.UpdatedAt,

            WorkTypeId = await NamedAsync(_db.WorkTypes.Where(w => w.Id == l.WorkTypeId)
                .Select(w => new NamedRefDto { Id = w.Id, Name = w.Name, Code = w.Code, SiberId = w.SiberId }), cancellationToken),
            LoadingTypeId = await NamedAsync(_db.LoadingTypes.Where(t => t.Id == l.LoadingTypeId)
                .Select(t => new NamedRefDto { Id = t.Id, Name = t.Name, SiberId = t.SiberId }), cancellationToken),
            PaymentTypeId = await NamedAsync(_db.PaymentTypes.Where(t => t.Id == l.PaymentTypeId)
                .Select(t => new NamedRefDto { Id = t.Id, Name = t.Name, SiberId = t.SiberId }), cancellationToken),
            StatusTypeId = await NamedAsync(_db.StatusTypes.Where(t => t.Id == l.StatusTypeId)
                .Select(t => new NamedRefDto { Id = t.Id, Name = t.Name, SiberId = t.SiberId }), cancellationToken),
            LoadTransferTypeId = await NamedAsync(_db.LoadTransferTypes.Where(t => t.Id == l.LoadTransferTypeId)
                .Select(t => new NamedRefDto { Id = t.Id, Name = t.Name, SiberId = t.SiberId }), cancellationToken),
            InstructionId = await NamedAsync(_db.Instructions.Where(t => t.Id == l.InstructionId)
                .Select(t => new NamedRefDto { Id = t.Id, Name = t.Name, SiberId = t.SiberId }), cancellationToken),
            RomorkTypeId = await NamedAsync(_db.RomorkTypes.Where(t => t.Id == l.RomorkTypeId)
                .Select(t => new NamedRefDto { Id = t.Id, Name = t.Name, SiberId = t.SiberId }), cancellationToken),
            DepartmentId = await NamedAsync(_db.Departments.Where(t => t.Id == l.DepartmentId)
                .Select(t => new NamedRefDto { Id = t.Id, Name = t.Name, SiberId = t.SiberId }), cancellationToken),

            CustomerId = await AccountRefAsync(l.CustomerId, cancellationToken),
            SenderId = await AccountRefAsync(l.SenderId, cancellationToken),
            ReceiverId = await AccountRefAsync(l.ReceiverId, cancellationToken),
            AgentId = await AccountRefAsync(l.AgentId, cancellationToken),
            CompanyPayFreightId = await AccountRefAsync(l.CompanyPayFreightId, cancellationToken),
            PayerCompanyId = long.TryParse(l.PayerCompany, out var payerId)
                ? await AccountRefAsync((int)payerId, cancellationToken)
                : null,

            DepartureCountryId = await CountryAsync(l.DepartureCountryId, cancellationToken),
            TransitCountryId = await CountryAsync(l.TransitCountryId, cancellationToken),
            TargetCountryId = await CountryAsync(l.TargetCountryId, cancellationToken),

            LoadChargePerson = await _db.LoadChargePeople.AsNoTracking()
                .Where(p => p.LoadId == (int)l.Id)
                .Select(p => new LoadChargePersonDto
                {
                    Id = p.Id,
                    LoadId = p.LoadId,
                    UserType = p.UserType,
                    UserId = _db.Users.Where(u => u.Id == p.UserId)
                        .Select(u => new MappedUserDto
                        {
                            Id = u.Id, Name = u.Name, Surname = u.Surname,
                            Email = u.Email, Avatar = u.Avatar, SiberCode = u.SiberCode,
                        })
                        .FirstOrDefault(),
                })
                .ToListAsync(cancellationToken),

            LoadContent = await _db.LoadContents.AsNoTracking()
                .Where(c => c.LoadId == l.Id)
                .Select(c => new LoadContentDto
                {
                    Id = c.Id, LoadId = c.LoadId, Quantity = c.Quantity,
                    GrossWeight = c.GrossWeight, NetWeight = c.NetWeight, Volume = c.Volume,
                    Lademeter = c.Lademeter, Width = c.Width, Length = c.Length, Height = c.Height,
                    Stackable = c.Stackable, SiberId = c.SiberId,
                    ProductTypeId = _db.ProductTypes.Where(t => t.Id == c.ProductTypeId)
                        .Select(t => new NamedRefDto { Id = t.Id, Name = t.Name, SiberId = t.SiberId })
                        .FirstOrDefault(),
                    CaseTypeId = _db.CaseTypes.Where(t => t.Id == c.CaseTypeId)
                        .Select(t => new NamedRefDto { Id = t.Id, Name = t.Name, SiberId = t.SiberId })
                        .FirstOrDefault(),
                })
                .ToListAsync(cancellationToken),

            LoadFinancialItem = await _db.LoadFinancialItems.AsNoTracking()
                .Where(f => f.LoadId == l.Id)
                .Select(f => new LoadFinancialItemDto
                {
                    Id = f.Id, LoadId = f.LoadId, NetPrice = f.NetPrice, TaxPrice = f.TaxPrice,
                    TotalPrice = f.TotalPrice, Quantity = f.Quantity, Description = f.Description,
                    Buysell = f.Buysell, Status = f.Status, Order = f.Order, Item = f.Item,
                    Currency = _db.Currencies.Where(c => c.Id == f.Currency)
                        .Select(c => new CurrencyDto { Id = c.Id, Name = c.Name, Code = c.Code })
                        .FirstOrDefault(),
                    AccountId = _db.Accounts.Where(a => a.Id == f.AccountId)
                        .Select(MapAccountRef)
                        .FirstOrDefault(),
                    TransportTypeId = _db.TransportTypes.Where(t => t.Id == f.TransportTypeId)
                        .Select(t => new NamedRefDto { Id = t.Id, Name = t.Name, SiberId = t.SiberId })
                        .FirstOrDefault(),
                    ItemTypeId = _db.ItemTypes.Where(t => t.Id == f.ItemTypeId)
                        .Select(t => new NamedRefDto { Id = t.Id, Name = t.Name, SiberId = t.SiberId })
                        .FirstOrDefault(),
                })
                .ToListAsync(cancellationToken),

            LoadMovement = await _db.LoadMovements.AsNoTracking()
                .Where(m => m.LoadId == l.Id)
                .Select(m => new LoadMovementDto
                {
                    Id = m.Id, LoadId = m.LoadId, Note = m.Note, CreatedAt = m.CreatedAt,
                    MovementTypeId = _db.MovementTypes.Where(t => t.Id == m.MovementTypeId)
                        .Select(t => new NamedRefDto { Id = t.Id, Name = t.Name, SiberId = t.SiberId })
                        .FirstOrDefault(),
                })
                .ToListAsync(cancellationToken),

            LoadFile = await _db.LoadFiles.AsNoTracking()
                .Where(f => f.LoadId == (int)l.Id)
                .Select(f => new LoadFileDto
                {
                    Id = f.Id, LoadId = f.LoadId, File = f.File,
                    MimeType = f.MimeType, OrgName = f.OrgName, CreatedAt = f.CreatedAt,
                })
                .ToListAsync(cancellationToken),

            EmailTo = await _db.LoadEmails.AsNoTracking()
                .Where(e => e.LoadId == (int)l.Id && e.Key == "to")
                .Select(e => e.Email!)
                .ToListAsync(cancellationToken),

            EmailCc = await _db.LoadEmails.AsNoTracking()
                .Where(e => e.LoadId == (int)l.Id && e.Key == "cc")
                .Select(e => e.Email!)
                .ToListAsync(cancellationToken),
        };
    }

    /// <summary>
    /// olsold: yük numarası oluşmuş kayıt silinemez. Ayrıca kaynak kodda silme
    /// döngüsünün İÇİNDEN return ediliyordu — 5 kayıt seçilip 4.'sü engellenirse
    /// ilk 3'ü zaten silinmiş oluyordu. Burada önce tüm liste kontrol edilir,
    /// engel varsa hiçbiri silinmez.
    /// </summary>
    public async Task<LoadDeleteResult> DeleteAsync(
        IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        var blocked = await _db.Loads
            .Where(l => ids.Contains(l.Id) && l.LoadNumber != null)
            .Select(l => l.LoadNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (blocked is not null)
            return new LoadDeleteResult(false, blocked);

        var loads = await _db.Loads
            .Where(l => ids.Contains(l.Id))
            .ToListAsync(cancellationToken);

        foreach (var load in loads)
        {
            _db.LoadContents.RemoveRange(_db.LoadContents.Where(c => c.LoadId == load.Id));
            _db.LoadFinancialItems.RemoveRange(_db.LoadFinancialItems.Where(f => f.LoadId == load.Id));
            _db.LoadMovements.RemoveRange(_db.LoadMovements.Where(m => m.LoadId == load.Id));
            _db.LoadChargePeople.RemoveRange(_db.LoadChargePeople.Where(p => p.LoadId == (int)load.Id));
            _db.Loads.Remove(load);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new LoadDeleteResult(true, null);
    }

    /// <summary>Zaman aşımı raporundan "takip edildi" işaretlemek için updated_at tazelenir.</summary>
    public async Task<LoadTimeOutUpdateStatus> UpdateTimeOutAsync(long id, CancellationToken cancellationToken = default)
    {
        var load = await _db.Loads.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (load is null)
            return LoadTimeOutUpdateStatus.NotFound;

        if (load.LoadNumber is not null)
            return LoadTimeOutUpdateStatus.Locked;

        load.UpdatedAt = _clock.Now;
        await _db.SaveChangesAsync(cancellationToken);
        return LoadTimeOutUpdateStatus.Success;
    }

    private async Task<NamedRefDto?> NamedAsync(
        IQueryable<NamedRefDto> query, CancellationToken cancellationToken) =>
        await query.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

    private async Task<AccountRefDto?> AccountRefAsync(int? accountId, CancellationToken cancellationToken)
    {
        if (accountId is null)
            return null;

        return await _db.Accounts.AsNoTracking()
            .Where(a => a.Id == accountId)
            .Select(MapAccountRef)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<CountryDto?> CountryAsync(Guid? id, CancellationToken cancellationToken)
    {
        if (id is null)
            return null;

        return await _db.Countries.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CountryDto
            {
                Id = c.Id, Name = c.Name, CountryCode = c.CountryCode,
                Flag = c.Flag, PhoneCode = c.PhoneCode, Slug = c.Slug,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Liste projeksiyonu içinde satır-içi kullanılan cari eşlemesi. Ayrı bir
    /// senkron metot olarak çağrılırsa EF Core bunu SQL'e gömemez, dış
    /// sorgunun okuyucusu açıkken aynı DbContext'te ikinci bir komut açmaya
    /// çalışır ve "operation already in progress" hatası verir.
    ///
    /// olsold: OfferTable.vue Müşteri sütunu data.customer_id?.country_id?.name
    /// okuyor. Account entity'sinde Country'ye EF navigasyonu YOK (olsold'da da
    /// gerçek bir FK yok — bkz. Account.cs) — navigasyon eklemek migration
    /// snapshot'ını bozup PendingModelChangesWarning ile konteyneri çökertti,
    /// bu yüzden burada da diğer alanlar gibi (WorkTypeId/LoadingTypeId/...)
    /// düz bir alt-sorgu kullanılıyor. Bu, ifadenin ARTIK `_db`'ye ihtiyaç
    /// duyması nedeniyle STATIC olamıyor (instance property'e çevrildi) —
    /// yine de gerçek bir metot ÇAĞRISI değil, EF'in SQL'e gömebildiği bir
    /// ifade ağacı (expression tree) olarak kalıyor.
    /// </summary>
    private Expression<Func<Account, AccountRefDto>> MapAccountRef => a => new AccountRefDto
    {
        Id = a.Id,
        Name = a.Name,
        Avatar = a.Avatar,
        Email = a.Email,
        Phone = a.Phone,
        Address = a.Address,
        TaxNumber = a.TaxNumber,
        SiberId = a.SiberId,
        CountryId = _db.Countries.Where(c => c.Id == a.CountryId)
            .Select(c => new CountryDto
            {
                Id = c.Id,
                Name = c.Name,
                CountryCode = c.CountryCode,
                Flag = c.Flag,
                PhoneCode = c.PhoneCode,
                Slug = c.Slug,
            })
            .FirstOrDefault(),
    };
}
