using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.Accounts;
using OLS.DataAccess.Context;
using OLS.DataAccess.Siber;
using OLS.DataAccess.Entities;

namespace OLS.Business.Services.Loads;

public interface ILoadService
{
    Task<object> ListAsync(LoadListQuery query, CancellationToken cancellationToken = default);
    Task<LoadDetailDto?> SingleAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Yük numarası oluşmuş kayıt silinemez (olsold kuralı).</summary>
    Task<LoadDeleteResult> DeleteAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Teklifi KOPYALAR. Yeni teklif taslak olarak açılır; Siber'e aktarılmaz.
    /// </summary>
    Task<long?> DuplicateAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Yük numarası oluşmuş kayıt güncellenemez (olsold kuralı).</summary>
    Task<LoadTimeOutUpdateStatus> UpdateTimeOutAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Formdan gelen referans seçimlerinin (departman, ödeme tipi, cari...) YEREL
    /// tabloda hâlâ var olduğunu doğrular. Alan adı → hata mesajı döner; boşsa sorun yok.
    /// </summary>
    Task<Dictionary<string, string[]>> ValidateReferencesAsync(
        LoadReferenceIds ids, CancellationToken cancellationToken = default);
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
    string Path,
    int? CustomerId = null,
    int? SenderId = null,
    int? ReceiverId = null,
    int? AgentId = null,
    int? AssignedUserId = null,
    int? WorkTypeId = null,
    /// <summary>
    /// Taslak mantığı: Yük İçeriği veya Finans sekmesi eksik bırakılmış, henüz
    /// Yük'e dönüşmemiş teklifler ("Taslaklar" menüsü — bkz. QuotesPage.tsx).
    /// </summary>
    bool DraftOnly = false,
    /// <summary>
    /// true ise Siber'den SİLİNMİŞ kayıtlar da listelenir. Varsayılan false:
    /// silinen kayıt yerelde duruyor (geçmiş ve bağlı kayıtlar için) ama günlük
    /// listede görünmemeli.
    /// </summary>
    bool IncludeDeleted = false);

public sealed record LoadDeleteResult(bool Success, string? BlockedByLoadNumber);

/// <summary>Kaydetmede doğrulanacak referans seçimleri — bkz. ILoadService.ValidateReferencesAsync.</summary>
public sealed record LoadReferenceIds(
    int? DepartmentId, int? PaymentTypeId, int? StatusTypeId, int? WorkTypeId,
    int? LoadingTypeId, int? LoadTransferTypeId, int? InstructionId, int? RomorkTypeId,
    int? CustomerId, int? SenderId, int? ReceiverId, int? AgentId, int? CompanyPayFreightId);

public sealed class LoadService : ILoadService
{
    private readonly OlsDbContext _db;
    private readonly IAccountService _accounts;
    private readonly IClock _clock;
    private readonly ISiberReservationRepository _reservations;

    /// <summary>status_types: 4 = "Teklif" — kopya bu durumda başlar.</summary>
    private const int OfferStatusTypeId = 4;

    private readonly ISiberArchiveRepository _archive;
    private readonly ICompanyScope _companyScope;
    private readonly ICurrentUser _currentUser;

    public LoadService(
        OlsDbContext db, IAccountService accounts, IClock clock,
        ISiberReservationRepository reservations, ISiberArchiveRepository archive,
        ICompanyScope companyScope, ICurrentUser currentUser)
    {
        _db = db;
        _accounts = accounts;
        _clock = clock;
        _reservations = reservations;
        _archive = archive;
        _companyScope = companyScope;
        _currentUser = currentUser;
    }

    public async Task<object> ListAsync(LoadListQuery query, CancellationToken cancellationToken = default)
    {
        var loads = _db.Loads.AsNoTracking();

        // Siber'den silinen kayıtlar varsayılan olarak gizlenir; kayıt yerelde
        // duruyor (bkz. Load.SiberDeletedAt) ama günlük listede yer almamalı.
        if (!query.IncludeDeleted)
            loads = loads.Where(l => l.SiberDeletedAt == null);

        // ŞİRKET GÖRÜNÜRLÜĞÜ (AVRORA / OLS) — yük ve seferdeki ile aynı kural.
        // Teklifler de kapsama alındı: yükler tekliften doğduğu için burası açık
        // kalsaydı Avrora yükünün bilgisi teklif üzerinden sızardı.
        var visibility = await _companyScope.ResolveAsync(_currentUser.Id, cancellationToken);

        if (!visibility.SeesEverything)
        {
            loads = visibility.OnlyCompanyId is { } only
                ? loads.Where(l => l.SiberCompanyId == only)
                : loads.Where(l => l.SiberCompanyId == null ||
                                   l.SiberCompanyId != visibility.ExcludeCompanyId);
        }

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

        if (query.DraftOnly)
        {
            // Yalnızca oturum açan kullanıcının kendi görevlendirildiği teklifler —
            // aksi hâlde Siber'den senkronlanmış (LoadChargePerson'sız) binlerce eski
            // kayıt da "taslak" görünüp menüyü kullanılmaz hâle getiriyordu.
            loads = loads.Where(l =>
                l.LoadNumber == null &&
                (!_db.LoadContents.Any(c => c.LoadId == l.Id) ||
                 !_db.LoadFinancialItems.Any(f => f.LoadId == l.Id)) &&
                _db.LoadChargePeople.Any(p => p.LoadId == l.Id && p.UserId == (int)query.UserId));
        }

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

        if (query.CustomerId is { } customerId)
            loads = loads.Where(l => l.CustomerId == customerId);
        if (query.SenderId is { } senderId)
            loads = loads.Where(l => l.SenderId == senderId);
        if (query.ReceiverId is { } receiverId)
            loads = loads.Where(l => l.ReceiverId == receiverId);
        if (query.AgentId is { } agentId)
            loads = loads.Where(l => l.AgentId == agentId);
        if (query.WorkTypeId is { } workTypeId)
            loads = loads.Where(l => l.WorkTypeId == workTypeId);
        if (query.AssignedUserId is { } assignedUserId)
        {
            var chargedIds = _db.LoadChargePeople
                .Where(p => p.UserId == assignedUserId)
                .Select(p => (long)(p.LoadId ?? 0));

            loads = loads.Where(l => chargedIds.Contains(l.Id));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Türkçe noktasız I/ı normalizasyonu için bkz. QueryableExtensions.NormalizeTurkish.
            var pattern = $"%{QueryableExtensions.NormalizeTurkish(query.Search)}%";

            // olsold aramayı yük numarası / rezervasyon no / tarihler ve ilişkili
            // iş tipi, yükleme tipi, müşteri/gönderici/alıcı/acente adları ile
            // onların ülke adları üzerinde yapıyordu.
            var matchingAccountIds = _db.Accounts
                .Where(a => EF.Functions.Like(a.Name!.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern) ||
                            _db.Countries.Any(c => c.Id == a.CountryId &&
                                                   EF.Functions.Like(c.Name!.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern)))
                .Select(a => a.Id);

            var matchingWorkTypeIds = _db.WorkTypes
                .Where(w => EF.Functions.Like(w.Name!.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern))
                .Select(w => w.Id);

            var matchingLoadingTypeIds = _db.LoadingTypes
                .Where(t => EF.Functions.Like(t.Name!.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern))
                .Select(t => t.Id);

            loads = loads.Where(l =>
                EF.Functions.Like(l.LoadNumber!.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern) ||
                EF.Functions.Like(l.ReservationNumber!.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern) ||
                (l.WorkTypeId != null && matchingWorkTypeIds.Contains(l.WorkTypeId.Value)) ||
                (l.LoadingTypeId != null && matchingLoadingTypeIds.Contains(l.LoadingTypeId.Value)) ||
                (l.CustomerId != null && matchingAccountIds.Contains(l.CustomerId.Value)) ||
                (l.SenderId != null && matchingAccountIds.Contains(l.SenderId.Value)) ||
                (l.ReceiverId != null && matchingAccountIds.Contains(l.ReceiverId.Value)) ||
                (l.AgentId != null && matchingAccountIds.Contains(l.AgentId.Value)));
        }

        var projected = loads
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.Id)
            .Select(l => new LoadListItemDto
            {
                Id = l.Id,
                ReservationNumber = l.ReservationNumber,
                LoadNumber = l.LoadNumber,
                OfferDate = l.OfferDate,
                ApprovalDate = l.ApprovalDate,
                OfferValidityDate = l.OfferValidityDate,
                MarketingNotificationDate = l.MarketingNotificationDate,
                Description = l.Description,
                RejectionReason = l.RejectionReason,
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
            .Include(x => x.SiberCreatedByUser)
            .Include(x => x.SiberUpdatedByUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (l is null)
            return null;

        // Detay da filtrelenir — liste gizlese bile id ile doğrudan istenebilirdi.
        var visibility = await _companyScope.ResolveAsync(_currentUser.Id, cancellationToken);
        if (!visibility.Allows(l.SiberCompanyId))
            return null;

        return new LoadDetailDto
        {
            SiberAudit = SiberAuditDto.From(
                l.SiberCreatedBy, l.SiberCreatedByUser?.Name, l.SiberCreatedAt,
                l.SiberUpdatedBy, l.SiberUpdatedByUser?.Name, l.SiberUpdatedAt,
                l.SiberDeletedAt),
            Id = l.Id,
            ReservationNumber = l.ReservationNumber,
            LoadNumber = l.LoadNumber,
            OfferDate = l.OfferDate,
            ApprovalDate = l.ApprovalDate,
            OfferValidityDate = l.OfferValidityDate,
            MarketingNotificationDate = l.MarketingNotificationDate,
            Description = l.Description,
            RejectionReason = l.RejectionReason,
            PayerCompany = l.PayerCompany,
            FrontTransportationByUs = l.FrontTransportationByUs,
            FinalTransportationByUs = l.FinalTransportationByUs,
            WayOfWorking = l.WayOfWorking,
            TransferToSiber = l.TransferToSiber,
            SiberId = l.SiberId,
            MailId = l.MailId,
            CreatedAt = l.CreatedAt,
            UpdatedAt = l.UpdatedAt,

            // Teklifin Siber kimliği rezervasyonid; arşiv bağı bunun üzerinden.
            SiberArchive = (await _archive.ListByModuleAsync(l.SiberId ?? string.Empty, cancellationToken))
                .Select(a => new LoadArchiveDto
                {
                    Id = a.ArsivId,
                    Name = a.Ad,
                    CreatedAt = a.KayitGirisTarih,
                    CreatedBy = a.KayitGiren,
                    PersonalData = a.KisiselVeri,
                    RestrictedGroups = string.IsNullOrWhiteSpace(a.YetkiliGruplar) ? null : a.YetkiliGruplar,
                })
                .ToList(),

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
                    Buysell = f.Buysell, Status = f.Status, Order = f.Order,
                    Item = _db.FinancialItems.Where(fi => fi.Id == f.Item)
                        .Select(fi => new FinancialItemRefDto { Id = fi.Id, Name = fi.Name, Type = fi.Type ?? 0 })
                        .FirstOrDefault(),
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
    /// <summary>
    /// Silinmiş/geçersiz bir referans seçimini KAYIT ANINDA yakalar.
    ///
    /// Neden gerekli: açılır listeler sayfa açılışında bir kez yükleniyor. Bir
    /// referans satırı (ör. Siber'de karşılığı olmayan bir departman) sonradan
    /// kaldırılırsa, açık duran sekmedeki liste onu göstermeye devam ediyor;
    /// kullanıcı seçip kaydedince veritabanına ARTIK OLMAYAN bir id yazılıyordu.
    /// Sonuç, aktarım aşamasında ortaya çıkan ve yanıltıcı olan "Departman boş
    /// olamaz" hatasıydı — oysa alan boş değil, GEÇERSİZDİ.
    /// </summary>
    public async Task<Dictionary<string, string[]>> ValidateReferencesAsync(
        LoadReferenceIds ids, CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string[]>();
        const string gone = "Seçilen kayıt artık tanımlı değil — lütfen listeden yeniden seçin.";

        async Task CheckAsync<TEntity>(
            DbSet<TEntity> set, int? id, string field, Func<TEntity, long> key) where TEntity : class
        {
            if (id is not { } value) return;
            var exists = (await set.AsNoTracking().ToListAsync(cancellationToken)).Any(e => key(e) == value);
            if (!exists) errors[field] = [gone];
        }

        await CheckAsync(_db.Departments, ids.DepartmentId, "department_id", e => e.Id);
        await CheckAsync(_db.PaymentTypes, ids.PaymentTypeId, "payment_type_id", e => e.Id);
        await CheckAsync(_db.StatusTypes, ids.StatusTypeId, "status_type_id", e => e.Id);
        await CheckAsync(_db.WorkTypes, ids.WorkTypeId, "work_type_id", e => e.Id);
        await CheckAsync(_db.LoadingTypes, ids.LoadingTypeId, "loading_type_id", e => e.Id);
        await CheckAsync(_db.LoadTransferTypes, ids.LoadTransferTypeId, "load_transfer_type_id", e => e.Id);
        await CheckAsync(_db.Instructions, ids.InstructionId, "instruction_id", e => e.Id);
        await CheckAsync(_db.RomorkTypes, ids.RomorkTypeId, "romork_type_id", e => e.Id);
        await CheckAsync(_db.Accounts, ids.CustomerId, "customer_id", e => e.Id);
        await CheckAsync(_db.Accounts, ids.SenderId, "sender_id", e => e.Id);
        await CheckAsync(_db.Accounts, ids.ReceiverId, "receiver_id", e => e.Id);
        await CheckAsync(_db.Accounts, ids.AgentId, "agent_id", e => e.Id);
        await CheckAsync(_db.Accounts, ids.CompanyPayFreightId, "company_pay_freight_id", e => e.Id);

        return errors;
    }

    /// <summary>
    /// Teklif kopyalama.
    ///
    /// NE KOPYALANIR: müşteri/gönderici/alıcı, iş ve yükleme tipi, ülkeler,
    /// departman, açıklama, yük içeriği ve mali kalemler — yani teklifi yeniden
    /// yazmak yerine üzerinde oynanacak her şey.
    ///
    /// NE KOPYALANMAZ (ve NEDEN): kopya YENİ ve Siber'e hiç gitmemiş bir taslaktır.
    ///   • siber_id / rezervasyon numarası / transfer_to_siber: bunlar Siber'in
    ///     ürettiği kimliklerdir. Kopyalansaydı iki yerel teklif AYNI Siber
    ///     kaydını gösterir, ikisinden biri kaydedildiğinde diğerinin verisi
    ///     ezilirdi.
    ///   • load_number: yük numarası tek bir yüke aittir; kopyalamak "bu teklifin
    ///     yükü zaten oluşturulmuş" durumunu yanlışlıkla taşırdı.
    ///   • durum, onay tarihi, red gerekçesi: kopya baştan "Teklif" durumunda
    ///     başlar; Olumlu/Olumsuz kararı ve tarihi devredilmez.
    ///   • görevliler: kayıt sırasında zaten türetiliyor
    ///     (bkz. LoadWriteService.WriteChargePersonsAsync), kopyalamak anlamsız.
    /// </summary>
    public async Task<long?> DuplicateAsync(long id, CancellationToken cancellationToken = default)
    {
        var source = await _db.Loads.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (source is null)
            return null;

        var now = _clock.Now;
        var today = DateOnly.FromDateTime(now);

        var copy = new Load
        {
            WorkTypeId = source.WorkTypeId,
            LoadingTypeId = source.LoadingTypeId,
            PaymentTypeId = source.PaymentTypeId,
            LoadTransferTypeId = source.LoadTransferTypeId,
            InstructionId = source.InstructionId,
            RomorkTypeId = source.RomorkTypeId,
            CustomerId = source.CustomerId,
            SenderId = source.SenderId,
            ReceiverId = source.ReceiverId,
            AgentId = source.AgentId,
            CompanyPayFreightId = source.CompanyPayFreightId,
            PayerCompany = source.PayerCompany,
            Description = source.Description,
            DepartureCountryId = source.DepartureCountryId,
            TransitCountryId = source.TransitCountryId,
            TargetCountryId = source.TargetCountryId,
            DepartmentId = source.DepartmentId,
            FrontTransportationByUs = source.FrontTransportationByUs,
            FinalTransportationByUs = source.FinalTransportationByUs,
            WayOfWorking = source.WayOfWorking,

            // Yeni taslak: tarihler bugünden başlar, geçerlilik +7 gün
            // (yeni teklif formunun varsayılanıyla aynı).
            StatusTypeId = OfferStatusTypeId,
            OfferDate = today,
            MarketingNotificationDate = today,
            OfferValidityDate = today.AddDays(7),

            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.Loads.Add(copy);
        await _db.SaveChangesAsync(cancellationToken);

        foreach (var content in await _db.LoadContents.AsNoTracking()
                     .Where(c => c.LoadId == source.Id).ToListAsync(cancellationToken))
        {
            _db.LoadContents.Add(new LoadContent
            {
                LoadId = copy.Id,
                ProductTypeId = content.ProductTypeId,
                CaseTypeId = content.CaseTypeId,
                Quantity = content.Quantity,
                GrossWeight = content.GrossWeight,
                NetWeight = content.NetWeight,
                Volume = content.Volume,
                Lademeter = content.Lademeter,
                Width = content.Width,
                Height = content.Height,
                Length = content.Length,
                Stackable = content.Stackable,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        foreach (var item in await _db.LoadFinancialItems.AsNoTracking()
                     .Where(f => f.LoadId == source.Id).ToListAsync(cancellationToken))
        {
            _db.LoadFinancialItems.Add(new LoadFinancialItem
            {
                LoadId = copy.Id,
                Item = item.Item,
                Buysell = item.Buysell,
                Currency = item.Currency,
                NetPrice = item.NetPrice,
                TotalPrice = item.TotalPrice,
                Quantity = item.Quantity,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return copy.Id;
    }

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

        // SİBER'DEN DE SİL — ve ÖNCE Siber, sonra yerel.
        //
        // Eskiden yalnızca yerel siliniyordu: Siber'e aktarılmış (siber_id dolu) bir
        // teklif silindiğinde periyodik senkron onu bir sonraki turda Siber'den geri
        // getiriyordu, yani silme kalıcı olmuyordu. Sıra da önemli — canlıda
        // doğrulandı: yerel önce silinip Siber adımı hata verirse kayıt yerelde
        // gider, Siber'de kalır ve senkron yeni bir id'yle geri yazar
        // (bkz. LoadTransferWriteService.DeleteAsync'teki aynı not).
        if (_reservations.IsConfigured)
        {
            foreach (var siberId in loads.Select(l => l.SiberId).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct())
                await _reservations.DeleteRezervasyonAsync(siberId!, cancellationToken);
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
