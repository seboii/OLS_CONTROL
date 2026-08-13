using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.Business.Services.Loads;

/// <summary>
/// Yük/Teklif yazma tarafı. olsold: <c>LoadController::save/update</c>
///
/// KAYNAKTAKİ MÜKERRER KAYIT HATASI — burada düzeltildi:
/// olsold'da alt kayıt blokları YANLIŞLIKLA İKİ KEZ yazılmış:
///   - save: <c>load_charge_person</c> bloğu iki ardışık if içinde tekrarlanıyor
///   - update: içerik ve finansal kalemler siliniyor, sonra iki ayrı blokta
///     yeniden oluşturuluyor
/// Sonuç: her kaydetme/güncellemede içerik, finansal kalem ve görevliler
/// çiftleniyordu. Bu, teklif→yük dönüşümündeki toplamları (brüt ağırlık, hacim,
/// lademetre, kap) da şişiriyor ve "satış temsilcisi" kontrolünü bozuyordu
/// (görevli[1] aslında görevli[0]'ın kopyası oluyordu).
/// Burada her alt kayıt YALNIZCA BİR KEZ yazılır.
/// </summary>
public interface ILoadWriteService
{
    Task<long> CreateAsync(LoadWriteModel model, CancellationToken cancellationToken = default);
    Task<LoadUpdateResult> UpdateAsync(LoadWriteModel model, CancellationToken cancellationToken = default);
    Task DeleteContentsAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default);
    Task DeleteFinancialItemsAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default);
}

/// <summary>
/// <c>RemovedFileNames</c>: bu güncellemede listeden çıkarılan dosyaların
/// diskteki adları. <c>OLS.Business</c>, <c>IFileStorage</c>'a (API katmanı)
/// bağımlı olamaz — fiziksel silme çağrıyı yapan controller'a bırakılır
/// (bkz. <see cref="LoadFileService.SyncAsync"/>'teki aynı desen).
/// </summary>
public sealed record LoadUpdateResult(long? Id, IReadOnlyList<string> RemovedFileNames);

/// <summary>record: controller güncellemede <c>with { Id = … }</c> kullanıyor.</summary>
public sealed record LoadWriteModel
{
    public long? Id { get; init; }
    public long CurrentUserId { get; init; }

    public int? WorkTypeId { get; init; }
    public int? LoadingTypeId { get; init; }
    public int? PaymentTypeId { get; init; }
    public int? StatusTypeId { get; init; }
    public DateOnly? OfferDate { get; init; }
    public DateOnly? OfferValidityDate { get; init; }
    public DateOnly? MarketingNotificationDate { get; init; }
    public int? LoadTransferTypeId { get; init; }
    public int? InstructionId { get; init; }
    public int? RomorkTypeId { get; init; }
    public int? CustomerId { get; init; }
    public int? SenderId { get; init; }
    public int? ReceiverId { get; init; }
    public int? CompanyPayFreightId { get; init; }
    public int? AgentId { get; init; }
    public string? PayerCompany { get; init; }
    public string? Description { get; init; }
    public Guid? DepartureCountryId { get; init; }
    public Guid? TransitCountryId { get; init; }
    public Guid? TargetCountryId { get; init; }
    public int? DepartmentId { get; init; }
    public int FrontTransportationByUs { get; init; }
    public int FinalTransportationByUs { get; init; }
    public int WayOfWorking { get; init; }

    public IReadOnlyList<LoadContentInput> Contents { get; init; } = [];
    public IReadOnlyList<LoadFinancialItemInput> FinancialItems { get; init; } = [];
    public IReadOnlyList<LoadChargePersonInput> ChargePersons { get; init; } = [];
    public IReadOnlyList<string> EmailTo { get; init; } = [];
    public IReadOnlyList<string> EmailCc { get; init; } = [];
    public IReadOnlyList<LoadMovementInput> Movements { get; init; } = [];

    /// <summary>Yeni yüklenen dosyalar (controller kaydeder, adları buraya gelir).</summary>
    public IReadOnlyList<UploadedFile> NewFiles { get; init; } = [];

    /// <summary>Güncellemede korunacak mevcut dosya id'leri; listede olmayanlar silinir.</summary>
    public IReadOnlyList<long> KeepFileIds { get; init; } = [];
}

public sealed record LoadContentInput(
    int? ProductTypeId, int? CaseTypeId, int? Quantity, decimal? Width, decimal? Height,
    decimal? Length, decimal? GrossWeight, decimal? NetWeight, decimal? Volume,
    decimal? Lademeter, int? Stackable);

public sealed record LoadFinancialItemInput(
    int? Item, int? Quantity, int? TransportTypeId, int? AccountId, string? Description,
    int? Order, decimal? NetPrice, decimal? TotalPrice, int? Currency, int? Buysell);

public sealed record LoadChargePersonInput(int? UserId, int? UserType);

public sealed record LoadMovementInput(int? MovementTypeId, string? Note);

public sealed record UploadedFile(string FileName, string? MimeType, string? OriginalName);

public sealed class LoadWriteService : ILoadWriteService
{
    private readonly OlsDbContext _db;
    private readonly IClock _clock;

    public LoadWriteService(OlsDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<long> CreateAsync(
        LoadWriteModel model, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var now = _clock.Now;

        var load = new Load
        {
            WorkTypeId = model.WorkTypeId,
            LoadingTypeId = model.LoadingTypeId,
            PaymentTypeId = model.PaymentTypeId,
            StatusTypeId = model.StatusTypeId,
            OfferDate = model.OfferDate,
            OfferValidityDate = model.OfferValidityDate,
            MarketingNotificationDate = model.MarketingNotificationDate,
            LoadTransferTypeId = model.LoadTransferTypeId,
            InstructionId = model.InstructionId,
            RomorkTypeId = model.RomorkTypeId,
            CustomerId = model.CustomerId,
            SenderId = model.SenderId,
            ReceiverId = model.ReceiverId,
            CompanyPayFreightId = model.CompanyPayFreightId,
            AgentId = model.AgentId,
            PayerCompany = model.PayerCompany,
            Description = model.Description,
            DepartureCountryId = model.DepartureCountryId,
            TransitCountryId = model.TransitCountryId,
            TargetCountryId = model.TargetCountryId,
            DepartmentId = model.DepartmentId,
            FrontTransportationByUs = model.FrontTransportationByUs,
            FinalTransportationByUs = model.FinalTransportationByUs,
            WayOfWorking = model.WayOfWorking,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.Loads.Add(load);
        await _db.SaveChangesAsync(cancellationToken);

        await WriteChildrenAsync(load, model, now, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return load.Id;
    }

    public async Task<LoadUpdateResult> UpdateAsync(
        LoadWriteModel model, CancellationToken cancellationToken = default)
    {
        var id = model.Id ?? throw new ArgumentException("Id zorunlu", nameof(model));

        var load = await _db.Loads.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (load is null)
            return new LoadUpdateResult(null, []);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var now = _clock.Now;

        load.WorkTypeId = model.WorkTypeId;
        load.LoadingTypeId = model.LoadingTypeId;
        load.PaymentTypeId = model.PaymentTypeId;
        load.StatusTypeId = model.StatusTypeId;
        load.OfferDate = model.OfferDate;
        load.OfferValidityDate = model.OfferValidityDate;
        load.MarketingNotificationDate = model.MarketingNotificationDate;
        load.LoadTransferTypeId = model.LoadTransferTypeId;
        load.InstructionId = model.InstructionId;
        load.RomorkTypeId = model.RomorkTypeId;
        load.CustomerId = model.CustomerId;
        load.SenderId = model.SenderId;
        load.ReceiverId = model.ReceiverId;
        load.CompanyPayFreightId = model.CompanyPayFreightId;
        load.AgentId = model.AgentId;
        load.PayerCompany = model.PayerCompany;
        load.Description = model.Description;
        load.DepartureCountryId = model.DepartureCountryId;
        load.TransitCountryId = model.TransitCountryId;
        load.TargetCountryId = model.TargetCountryId;
        load.DepartmentId = model.DepartmentId;
        load.FrontTransportationByUs = model.FrontTransportationByUs;
        load.FinalTransportationByUs = model.FinalTransportationByUs;
        load.WayOfWorking = model.WayOfWorking;
        load.UpdatedAt = now;

        // olsold deseni: alt kayıtlar silinip yeniden yazılır (ama orada iki kez).
        _db.LoadContents.RemoveRange(_db.LoadContents.Where(c => c.LoadId == load.Id));
        _db.LoadFinancialItems.RemoveRange(_db.LoadFinancialItems.Where(f => f.LoadId == load.Id));
        _db.LoadMovements.RemoveRange(_db.LoadMovements.Where(m => m.LoadId == load.Id));
        _db.LoadChargePeople.RemoveRange(_db.LoadChargePeople.Where(p => p.LoadId == (int)load.Id));
        _db.LoadEmails.RemoveRange(_db.LoadEmails.Where(e => e.LoadId == (int)load.Id));

        // Dosyalar: yalnızca listede OLMAYANLAR silinir (olsold existingFilesIds mantığı).
        // Fiziksel dosyalar burada DEĞİL, çağıran controller'da silinir (bkz. LoadUpdateResult).
        var removedFiles = await _db.LoadFiles
            .Where(f => f.LoadId == (int)load.Id && !model.KeepFileIds.Contains(f.Id))
            .ToListAsync(cancellationToken);
        _db.LoadFiles.RemoveRange(removedFiles);

        await _db.SaveChangesAsync(cancellationToken);

        await WriteChildrenAsync(load, model, now, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        var removedFileNames = removedFiles
            .Where(f => !string.IsNullOrWhiteSpace(f.File))
            .Select(f => f.File!)
            .ToList();

        return new LoadUpdateResult(load.Id, removedFileNames);
    }

    private async Task WriteChildrenAsync(
        Load load, LoadWriteModel model, DateTime now, CancellationToken cancellationToken)
    {
        foreach (var content in model.Contents)
        {
            _db.LoadContents.Add(new LoadContent
            {
                LoadId = load.Id,
                ProductTypeId = content.ProductTypeId,
                CaseTypeId = content.CaseTypeId,
                Quantity = content.Quantity,
                Width = content.Width,
                Height = content.Height,
                Length = content.Length,
                GrossWeight = content.GrossWeight,
                NetWeight = content.NetWeight,
                Volume = content.Volume,
                Lademeter = content.Lademeter,
                Stackable = content.Stackable,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        foreach (var item in model.FinancialItems)
        {
            _db.LoadFinancialItems.Add(new LoadFinancialItem
            {
                LoadId = load.Id,
                Item = item.Item,
                Quantity = item.Quantity,
                TransportTypeId = item.TransportTypeId,
                AccountId = item.AccountId,
                Description = item.Description,
                Order = item.Order,
                NetPrice = item.NetPrice,
                TotalPrice = item.TotalPrice,
                Currency = item.Currency,
                Buysell = item.Buysell,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        foreach (var movement in model.Movements)
        {
            _db.LoadMovements.Add(new LoadMovement
            {
                LoadId = load.Id,
                // olsold hareket tipini sabit 1 yazıyordu.
                MovementTypeId = movement.MovementTypeId ?? 1,
                Note = movement.Note,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        WriteChargePersons(load, model, now);

        foreach (var email in model.EmailTo)
            AddEmail(load, "to", email, now);

        foreach (var email in model.EmailCc)
            AddEmail(load, "cc", email, now);

        foreach (var file in model.NewFiles)
        {
            _db.LoadFiles.Add(new LoadFile
            {
                LoadId = (int)load.Id,
                File = file.FileName,
                MimeType = file.MimeType,
                OrgName = file.OriginalName,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Görevliler. olsold kuralı korunuyor:
    ///   - Hiç görevli gelmezse mevcut kullanıcı her iki tip için (1 ve 2) eklenir.
    ///   - Tek tip gelirse eksik tip için mevcut kullanıcı eklenir.
    /// Fark: kaynak bu bloğu iki kez çalıştırıp kayıtları çiftliyordu.
    /// </summary>
    private void WriteChargePersons(Load load, LoadWriteModel model, DateTime now)
    {
        void Add(int? userId, int? userType) =>
            _db.LoadChargePeople.Add(new LoadChargePerson
            {
                LoadId = (int)load.Id,
                UserId = userId,
                UserType = userType,
                CreatedAt = now,
                UpdatedAt = now,
            });

        if (model.ChargePersons.Count == 0)
        {
            Add((int)model.CurrentUserId, 1);
            Add((int)model.CurrentUserId, 2);
            return;
        }

        var providedTypes = new List<int>();

        foreach (var person in model.ChargePersons)
        {
            providedTypes.Add(person.UserType ?? 0);
            Add(person.UserId, person.UserType);
        }

        if (providedTypes.Count == 1)
        {
            var missing = providedTypes.Contains(1) ? 2 : 1;
            Add((int)model.CurrentUserId, missing);
        }
    }

    private void AddEmail(Load load, string key, string email, DateTime now) =>
        _db.LoadEmails.Add(new LoadEmail
        {
            LoadId = (int)load.Id,
            Key = key,
            Email = email,
            CreatedAt = now,
            UpdatedAt = now,
        });

    /// <summary>
    /// olsold: yük numarası oluşmuş kayıtların alt satırları silinemez.
    /// Siber'deki karşılık (skn_rezervasyonyukkoli) da silinir — ancak orada
    /// önce Siber'den siliniyordu; burada önce yerel, sonra Siber.
    /// </summary>
    public async Task DeleteContentsAsync(
        IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        var contents = await _db.LoadContents
            .Where(c => ids.Contains(c.Id)).ToListAsync(cancellationToken);

        _db.LoadContents.RemoveRange(contents);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteFinancialItemsAsync(
        IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        var items = await _db.LoadFinancialItems
            .Where(f => ids.Contains(f.Id)).ToListAsync(cancellationToken);

        _db.LoadFinancialItems.RemoveRange(items);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
