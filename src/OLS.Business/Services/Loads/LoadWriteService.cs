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

    /// <summary>Yük numarası oluşmuş kayıt güncellenemez (olsold kuralı).</summary>
    Task<LoadUpdateResult> UpdateAsync(LoadWriteModel model, CancellationToken cancellationToken = default);

    /// <summary>Yük numarası oluşmuş kayda ait satır silinemez (olsold kuralı).</summary>
    Task<LoadChildDeleteResult> DeleteContentsAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default);

    /// <summary>Yük numarası oluşmuş kayda ait satır silinemez (olsold kuralı).</summary>
    Task<LoadChildDeleteResult> DeleteFinancialItemsAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default);
}

/// <summary>
/// <c>RemovedFileNames</c>: bu güncellemede listeden çıkarılan dosyaların
/// diskteki adları. <c>OLS.Business</c>, <c>IFileStorage</c>'a (API katmanı)
/// bağımlı olamaz — fiziksel silme çağrıyı yapan controller'a bırakılır
/// (bkz. <see cref="LoadFileService.SyncAsync"/>'teki aynı desen).
///
/// <c>IsLocked</c>: <c>Id</c> bulundu ama <c>load_number</c> doluydu — kaynaktaki
/// "Yük oluşturulmuş kayıt güncellenemez" kuralı (<c>LoadService.LoadDeleteResult</c>
/// ile aynı desen).
/// </summary>
public sealed record LoadUpdateResult(long? Id, IReadOnlyList<string> RemovedFileNames, bool IsLocked = false)
{
    public static LoadUpdateResult NotFound() => new(null, []);
    public static LoadUpdateResult Locked() => new(null, [], IsLocked: true);
}

/// <summary>
/// olsold: <c>delete_load_contents</c>/<c>delete_load_financial_items</c> döngü
/// İÇİNDEN return ediyordu — birkaç kayıt zaten silinmişken sonuncusu engellenirse
/// kısmi silme oluyordu. <c>LoadService.DeleteAsync</c>'teki aynı düzeltme burada
/// da uygulanıyor: önce TÜM hedef satırların Yük'ü kontrol edilir, biri kilitliyse
/// hiçbiri silinmez.
/// </summary>
public sealed record LoadChildDeleteResult(bool Success);

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

    /// <summary>Teklif Olumsuz isaretlendiginde girilen gerekce (bkz. Load.RejectionReason).</summary>
    public string? RejectionReason { get; init; }
    public Guid? DepartureCountryId { get; init; }
    public Guid? TransitCountryId { get; init; }
    public Guid? TargetCountryId { get; init; }
    public int? DepartmentId { get; init; }
    public int FrontTransportationByUs { get; init; }
    public int FinalTransportationByUs { get; init; }
    public int WayOfWorking { get; init; }

    public IReadOnlyList<LoadContentInput> Contents { get; init; } = [];
    public IReadOnlyList<LoadFinancialItemInput> FinancialItems { get; init; } = [];
    /// <summary>Teslim şekli / döviz — dönüşümde yüke taşınır.</summary>
    public int? DeliveryMethodId { get; init; }
    public int? CurrencyId { get; init; }

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
    /// <summary>status_types tablosundaki "Olumsuz" satırı (bkz. DbSeeder / gerçek Siber).</summary>
    private const int NegativeStatusTypeId = 1;

    /// <summary>status_types tablosundaki "Olumlu" satırı.</summary>
    private const int PositiveStatusTypeId = 5;

    /// <summary>
    /// load_charge_people.user_type: 1 = Operasyon Yetkilisi, 2 = Satış
    /// Temsilcisi, 3 = Fiyatlandıran (bkz. LoadChargePerson — hangi rolün
    /// Siber'de gerçekten kullanıldığı orada ölçümle yazılı).
    /// </summary>
    private const int OperationOfficerType = 1;
    private const int SalesRepType = 2;
    private const int PricingUserType = 3;

    /// <summary>
    /// Gerekçe YALNIZCA Olumsuz durumunda saklanır: kullanıcı önce Olumsuz seçip
    /// gerekçe yazar, sonra fikir değiştirip Olumlu'ya çevirirse eski gerekçenin
    /// kayıtta kalması raporlamayı yanıltırdı ("olumlu ama red gerekçesi var").
    /// </summary>
    private static string? NormalizeRejectionReason(LoadWriteModel model) =>
        model.StatusTypeId == NegativeStatusTypeId
            ? string.IsNullOrWhiteSpace(model.RejectionReason) ? null : model.RejectionReason.Trim()
            : null;

    /// <summary>
    /// "Olumlu'ya çekilme tarihi". Teklif Olumlu'ya İLK geçtiğinde o günün tarihi
    /// damgalanır ve sonraki kayıtlarda korunur — her kaydetmede bugüne çekilseydi
    /// "kaç günde onaylandı" bilgisi kaybolurdu. Durum Olumlu'dan çıkarsa temizlenir
    /// (aynı gerekçe <see cref="NormalizeRejectionReason"/>'da da geçerli: duruma
    /// ait alanlar durum değişince kayıtta kalmamalı).
    /// </summary>
    private DateOnly? ResolveApprovalDate(LoadWriteModel model, DateOnly? existing) =>
        model.StatusTypeId == PositiveStatusTypeId
            ? existing ?? DateOnly.FromDateTime(_clock.Now)
            : null;

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
            DeliveryMethodId = model.DeliveryMethodId,
            CurrencyId = model.CurrencyId,
            OfferDate = model.OfferDate,
            ApprovalDate = ResolveApprovalDate(model, null),
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
            RejectionReason = NormalizeRejectionReason(model),
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
            return LoadUpdateResult.NotFound();

        if (load.LoadNumber is not null)
            return LoadUpdateResult.Locked();

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var now = _clock.Now;

        load.WorkTypeId = model.WorkTypeId;
        load.LoadingTypeId = model.LoadingTypeId;
        load.PaymentTypeId = model.PaymentTypeId;
        load.ApprovalDate = ResolveApprovalDate(model, load.ApprovalDate);
        load.StatusTypeId = model.StatusTypeId;
        load.DeliveryMethodId = model.DeliveryMethodId;
        load.CurrencyId = model.CurrencyId;
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
        load.RejectionReason = NormalizeRejectionReason(model);
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

        await WriteChargePersonsAsync(load, model, now, cancellationToken);

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
    /// Görevlileri yazar.
    ///
    /// OPERASYON YETKİLİSİ (user_type=1) — İKİ TANE VE ELLE DÜZENLENİR.
    ///
    /// Siber'in teklif kaydında yetkili için İKİ ayrı sütun var:
    /// <c>musteritemsilcisi</c> (kullanıcı ADI) ve <c>operasyonyetkilisikod2</c>
    /// (kullanıcı KODU). Canlıda 19.554 rezervasyonun 17.620'sinde birincisi,
    /// 17.127'sinde ikincisi dolu; ikisi 6.627 kayıtta FARKLI kişiyi
    /// gösteriyor — yani ikinci yetkili gerçekten ikinci bir kişi, birincinin
    /// kopyası değil. Uygulama bu alanı tek ve türetilmiş tutuyordu.
    ///
    /// Sıra:
    ///
    ///   1. Formdan gelen seçim(ler) yazılır — en fazla iki, gönderilen sırada.
    ///      Alan artık ELLE DÜZENLENEBİLİR: müşterinin tanımlı yetkilisi
    ///      formu yalnızca ÖN DOLDURUR, kullanıcı değiştirebilir.
    ///   2. İstekte hiç yetkili yoksa (arayüz dışı çağrı) müşteriye tanımlı
    ///      AKTİF yetkililere düşülür.
    ///   3. O da yoksa kaydeden kullanıcıya düşülür — Siber aktarımı boş
    ///      <c>musteritemsilcisi</c>/<c>insuser</c> kabul etmiyor.
    ///
    /// Seçilen kişinin var VE aktif olduğu doğrulanır: var olmayan bir kimlik
    /// Siber'e boş ad/kod gönderir, yük dönüşümü de "Müşteri temsilcisi boş
    /// olamaz" ile düşerdi.
    ///
    /// AYRILMIŞ PERSONEL "TANIMLI" SAYILMAZ: bağların 3.209 carisinden
    /// 1.651'inde yetkili pasif/silinmiş bir kullanıcı; ön doldurma da bu
    /// yüzden yalnızca aktif kullanıcıya bakar.
    ///
    /// 243 carinin birden çok yetkilisi var; sıralama olmadan hangisinin
    /// seçildiği çağrıdan çağrıya değişebilirdi — kimliğe göre sıralanır.
    ///
    /// SATIŞ TEMSİLCİSİ (user_type=2) — TEK KİŞİ. Müşteriye tanımlı temsilci,
    /// arayüzde salt-okunur, istekten geleni bilinçli olarak yok sayar. Hiç
    /// yoksa birinci operasyon yetkilisi yazılır — Siber aktarımı
    /// <c>satistemsilcisikod</c> boş gelirse doğrulamada takılıyor, alanı boş
    /// bırakmak teklifi aktarılamaz hâle getirirdi.
    ///
    /// Eskiden cariye tanımlı TÜM satış temsilcileri yazılıyordu (249 caride
    /// 2-4 kişi), ama Siber'e yalnızca ilki gidiyordu: ikinci sütun
    /// <c>satistemsilcisi2kod</c> 19.561 teklifin 540'ında dolu (%2,8) ve
    /// fiilen kullanılmıyor. Ekranda kaydedilecek diye gösterilen fazladan
    /// kişiler Siber'e hiç ulaşmıyordu; artık kimliğe göre sıralanıp TEK kişi
    /// yazılıyor — gösterilen ile yazılan aynı.
    ///
    /// FİYATLANDIRAN (user_type=3) — Siber'de
    /// <c>skn_rezervasyon.fiyatlandirankullaniciid</c>. Formdan gelmezse
    /// BİRİNCİ OPERASYON YETKİLİSİ yazılır: Siber'in kendi verisinde dolu 7.952
    /// kaydın 5.798'inde (%73) fiyatlandıran zaten 1. operasyon yetkilisiyle
    /// aynı kişi.
    ///
    /// Kural arayüzde değil BURADA uygulanıyor: tek doğruluk noktası sunucu
    /// tarafıdır.
    /// </summary>
    private async Task WriteChargePersonsAsync(
        Load load, LoadWriteModel model, DateTime now, CancellationToken cancellationToken)
    {
        void Add(int? userId, int userType) =>
            _db.LoadChargePeople.Add(new LoadChargePerson
            {
                LoadId = (int)load.Id,
                UserId = userId,
                UserType = userType,
                CreatedAt = now,
                UpdatedAt = now,
            });

        var officers = await ResolveOperationOfficersAsync(model, cancellationToken);

        // SIRA ANLAMLI: 1. yetkili Siber'de musteritemsilcisi, 2. yetkili
        // operasyonyetkilisikod2 olarak yazılıyor. Okuma tarafı satırları
        // kimliğe (ekleme sırasına) göre sıralıyor.
        foreach (var userId in officers)
            Add(userId, OperationOfficerType);

        // TEK SATIŞ TEMSİLCİSİ (yukarıdaki nota bakınız). Sıralama olmadan
        // hangisinin seçildiği çağrıdan çağrıya değişebilirdi.
        var salesRep = model.CustomerId is { } customerId
            ? await _db.AccountRepresentatives.AsNoTracking()
                .Where(r => r.AccountId == customerId && r.UserType == SalesRepType)
                .Select(r => r.UserId)
                .Distinct()
                .OrderBy(userId => userId)
                .Cast<int?>()
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        Add(salesRep ?? officers[0], SalesRepType);

        // FİYATLANDIRAN: formdan geleni doğrula, yoksa 1. operasyon yetkilisi.
        var pricingUser = await RequestedPricingUserAsync(model, cancellationToken)
                          ?? officers[0];

        Add(pricingUser, PricingUserType);
    }

    /// <summary>
    /// Formdan seçilen fiyatlandıran. Var/aktif olduğu doğrulanır: var olmayan
    /// bir kimlik Siber'e çözülemeyen bir GUID gönderir ve
    /// <c>fiyatlandirankullaniciid</c> FK'siz olsa da çöp veri bırakırdı.
    /// </summary>
    private async Task<int?> RequestedPricingUserAsync(
        LoadWriteModel model, CancellationToken cancellationToken)
    {
        var requested = model.ChargePersons
            .Where(p => p.UserType == PricingUserType && p.UserId is > 0)
            .Select(p => p.UserId!.Value)
            .FirstOrDefault();

        if (requested == 0)
            return null;

        return (await ActiveUserIdsAsync([requested], cancellationToken)).Contains(requested)
            ? requested
            : null;
    }

    /// <summary>Teklif formundaki operasyon yetkilisi alanı sayısı.</summary>
    private const int MaxOperationOfficers = 2;

    /// <summary>
    /// Yazılacak operasyon yetkilileri, sırasıyla. En az bir kişi döner
    /// (en kötü hâlde kaydeden kullanıcı).
    /// </summary>
    private async Task<IReadOnlyList<int>> ResolveOperationOfficersAsync(
        LoadWriteModel model, CancellationToken cancellationToken)
    {
        var requested = model.ChargePersons
            .Where(p => p.UserType == OperationOfficerType && p.UserId is > 0)
            .Select(p => p.UserId!.Value)
            .Distinct()
            .Take(MaxOperationOfficers)
            .ToList();

        if (requested.Count > 0)
        {
            var valid = await ActiveUserIdsAsync(requested, cancellationToken);

            // GÖNDERİLEN SIRA KORUNUR — "yetkili 1" ile "yetkili 2" yer
            // değiştirirse Siber'de ad ve kod sütunları da yer değiştirirdi.
            var ordered = requested.Where(valid.Contains).ToList();
            if (ordered.Count > 0)
                return ordered;
        }

        var fromCustomer = await CustomerOperationOfficersAsync(model.CustomerId, cancellationToken);

        return fromCustomer.Count > 0 ? fromCustomer : [(int)model.CurrentUserId];
    }

    /// <summary>
    /// Müşteriye tanımlı AKTİF operasyon yetkilileri (en fazla iki, kimliğe
    /// göre sıralı). İstekte hiç yetkili gelmediğinde ön dolduranın aynısına
    /// düşmek için kullanılır.
    /// </summary>
    private async Task<List<int>> CustomerOperationOfficersAsync(
        int? customerId, CancellationToken cancellationToken)
    {
        if (customerId is not { } id)
            return [];

        return await _db.AccountRepresentatives.AsNoTracking()
            .Where(r => r.AccountId == id && r.UserType == OperationOfficerType)
            .Join(_db.Users.AsNoTracking().Where(u => u.DeletedAt == null && u.Status),
                  r => (long)r.UserId, u => u.Id, (r, u) => r.UserId)
            .Distinct()
            .OrderBy(userId => userId)
            .Take(MaxOperationOfficers)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Verilen kimliklerden var VE aktif olanlar.</summary>
    private async Task<HashSet<int>> ActiveUserIdsAsync(
        IReadOnlyList<int> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Select(id => (long)id).ToList();

        var found = await _db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id) && u.DeletedAt == null && u.Status)
            .Select(u => (int)u.Id)
            .ToListAsync(cancellationToken);

        return [.. found];
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
    public async Task<LoadChildDeleteResult> DeleteContentsAsync(
        IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        var contents = await _db.LoadContents
            .Where(c => ids.Contains(c.Id)).ToListAsync(cancellationToken);

        var loadIds = contents.Select(c => c.LoadId).Distinct().ToList();
        var locked = await _db.Loads.AnyAsync(
            l => loadIds.Contains(l.Id) && l.LoadNumber != null, cancellationToken);
        if (locked)
            return new LoadChildDeleteResult(false);

        _db.LoadContents.RemoveRange(contents);
        await _db.SaveChangesAsync(cancellationToken);
        return new LoadChildDeleteResult(true);
    }

    public async Task<LoadChildDeleteResult> DeleteFinancialItemsAsync(
        IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        var items = await _db.LoadFinancialItems
            .Where(f => ids.Contains(f.Id)).ToListAsync(cancellationToken);

        var loadIds = items.Select(f => f.LoadId).Distinct().ToList();
        var locked = await _db.Loads.AnyAsync(
            l => loadIds.Contains(l.Id) && l.LoadNumber != null, cancellationToken);
        if (locked)
            return new LoadChildDeleteResult(false);

        _db.LoadFinancialItems.RemoveRange(items);
        await _db.SaveChangesAsync(cancellationToken);
        return new LoadChildDeleteResult(true);
    }
}
