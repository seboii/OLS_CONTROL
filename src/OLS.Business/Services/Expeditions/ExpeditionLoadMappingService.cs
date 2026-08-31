using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;
using OLS.DataAccess.Siber;

namespace OLS.Business.Services.Expeditions;

/// <summary>
/// Sefere yük bağlama (<c>expedition_load_mapping</c>).
/// olsold: <c>Front\ExpeditionLoadMapping\ExpeditionLoadMappingController</c>
///
/// Uçların adları yanıltıcı, kaynaktaki anlamları şöyle:
/// <list type="bullet">
/// <item><c>all</c>  → sefere <b>eklenebilecek</b> (henüz eşlenmemiş) yük aktarmaları</item>
/// <item><c>single(id)</c> → o <b>seferin</b> eşlenmiş yükleri + toplamlar</item>
/// </list>
///
/// Yazma işlemleri PostgreSQL ve Siber'i birlikte günceller: eşleme satırı
/// <c>skn_yukaktarma</c>'ya yazılır, yükün <c>skn_yuk.pozisyonid</c> alanı
/// sefere bağlanır. Silmede ikisi de geri alınır.
/// </summary>
public interface IExpeditionLoadMappingService
{
    Task<object> AvailableLoadsAsync(
        string? search, int? perPage, int page, string path,
        CancellationToken cancellationToken = default);

    Task<ExpeditionMappingListDto> ByExpeditionAsync(
        long expeditionId, CancellationToken cancellationToken = default);

    Task<MappingSaveResult> SaveAsync(
        long? expeditionId, long? loadTransferId, CancellationToken cancellationToken = default);

    Task UpdateAsync(ExpeditionMappingUpdateInput input, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default);
}

public sealed class ExpeditionMappingUpdateInput
{
    public string? MappingSiberId { get; set; }
    public string? LoadTransferId { get; set; }
    public string? ExpeditionId { get; set; }
    public string? RomorkId { get; set; }
    public string? YerId { get; set; }
    public DateTime? Date { get; set; }
}

/// <summary>
/// Doğrulama hataları kaynakta <c>400 {"errors":{"message":["…"]}}</c> —
/// arayüzün <c>useGetFirstError</c> yardımcısı bu biçimi bekliyor.
/// </summary>
public sealed record MappingSaveResult(string? SiberId, string? ErrorMessage)
{
    public bool IsSuccess => ErrorMessage is null;
    public static MappingSaveResult Fail(string message) => new(null, message);
}

public sealed class ExpeditionMappingListDto
{
    [JsonPropertyName("data")] public IReadOnlyList<ExpeditionMappingDto> Data { get; init; } = [];

    /// <summary>Yanıtın kökünde ayrı bir anahtar olarak döner (zarfın içinde değil).</summary>
    [JsonPropertyName("total_expedition_values")] public MappingTotalsDto Totals { get; init; } = new();
}

public sealed class MappingTotalsDto
{
    [JsonPropertyName("total_quantity")] public decimal TotalQuantity { get; set; }
    [JsonPropertyName("total_gross_weight")] public decimal TotalGrossWeight { get; set; }
    [JsonPropertyName("total_net_weight")] public decimal TotalNetWeight { get; set; }
    [JsonPropertyName("total_lademeter")] public decimal TotalLademeter { get; set; }
    [JsonPropertyName("total_volume")] public decimal TotalVolume { get; set; }
}

public sealed class ExpeditionMappingDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("yukaktarmaid")] public string? Yukaktarmaid { get; init; }
    [JsonPropertyName("upload_unload")] public int? UploadUnload { get; init; }
    [JsonPropertyName("date")] public DateOnly? Date { get; init; }

    /// <summary>İlişki sütunu EZER — arayüz <c>load_transfer_id.load_number_work_type</c> okuyor.</summary>
    [JsonPropertyName("load_transfer_id")] public MappedLoadTransferDto? LoadTransferId { get; init; }
    [JsonPropertyName("expedition_id")] public MappedExpeditionDto? ExpeditionId { get; init; }
    [JsonPropertyName("romork_id")] public MappedCarDto? RomorkId { get; init; }
    [JsonPropertyName("yer_id")] public MappedCityDto? YerId { get; init; }
    [JsonPropertyName("total_values")] public MappingTotalsDto TotalValues { get; init; } = new();
}

public sealed class MappedLoadTransferDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("load_number_work_type")] public string? LoadNumberWorkType { get; init; }
    [JsonPropertyName("load_transfer_id")] public string? LoadTransferId { get; init; }
    [JsonPropertyName("customer_id")] public MappedNameDto? CustomerId { get; init; }
    [JsonPropertyName("load_transfer_package")] public IReadOnlyList<MappedPackageDto> LoadTransferPackage { get; init; } = [];

    /// <summary>
    /// Bu yükün Siber arşivindeki evrakları. Sefer ekranında da gösterilir:
    /// sefere bağlı yüklerin belgelerine ulaşmak için yük kartına gitmek
    /// gerekmesin (kullanıcı isteği).
    /// </summary>
    [JsonPropertyName("siber_archive")] public IReadOnlyList<MappedArchiveDto> SiberArchive { get; init; } = [];
}

public sealed class MappedArchiveDto
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; init; }
    [JsonPropertyName("created_by")] public string? CreatedBy { get; init; }
    [JsonPropertyName("personal_data")] public bool PersonalData { get; init; }
    [JsonPropertyName("restricted_groups")] public string? RestrictedGroups { get; init; }
}

public sealed class MappedPackageDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("quantity")] public int? Quantity { get; init; }
    [JsonPropertyName("gross_weight")] public decimal? GrossWeight { get; init; }
    [JsonPropertyName("net_weight")] public decimal? NetWeight { get; init; }
    [JsonPropertyName("lademeter")] public decimal? Lademeter { get; init; }
    [JsonPropertyName("volume")] public decimal? Volume { get; init; }
    [JsonPropertyName("case_type_id")] public MappedNameDto? CaseTypeId { get; init; }
    [JsonPropertyName("product_type_id")] public MappedNameDto? ProductTypeId { get; init; }
}

public sealed class MappedExpeditionDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("expedition_number")] public string? ExpeditionNumber { get; init; }
}

public sealed class MappedCarDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("plate_number")] public string? PlateNumber { get; init; }
}

public sealed class MappedCityDto
{
    [JsonPropertyName("id")] public Guid Id { get; init; }
    [JsonPropertyName("country_id")] public string? CountryId { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
}

public sealed class MappedNameDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
}

/// <summary>Eklenebilir yük listesi — kaynaktaki <c>select</c> alanları birebir.</summary>
public sealed class AvailableLoadDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("load_number_work_type")] public string? LoadNumberWorkType { get; init; }
    [JsonPropertyName("load_status_id")] public LoadStatusRefDto? LoadStatusId { get; init; }
    [JsonPropertyName("customer_id")] public MappedNameDto? CustomerId { get; init; }
    [JsonPropertyName("sender_id")] public MappedNameDto? SenderId { get; init; }
}

public sealed class LoadStatusRefDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("load_status_id")] public int? LoadStatusId { get; init; }
    [JsonPropertyName("order_no")] public int? OrderNo { get; init; }
}

public sealed class ExpeditionLoadMappingService : IExpeditionLoadMappingService
{
    private readonly OlsDbContext _db;
    private readonly ISiberLoadMappingRepository _siber;
    private readonly IClock _clock;

    private readonly ISiberArchiveRepository _archive;

    public ExpeditionLoadMappingService(
        OlsDbContext db, ISiberLoadMappingRepository siber,
        ISiberArchiveRepository archive, IClock clock)
    {
        _db = db;
        _siber = siber;
        _archive = archive;
        _clock = clock;
    }

    public async Task<object> AvailableLoadsAsync(
        string? search, int? perPage, int page, string path,
        CancellationToken cancellationToken = default)
    {
        // Eşleme tablosunda load_transfer_id METİN tutuluyor; sayıya çevirip
        // hariç tutuyoruz (kaynak pluck + whereNotIn ile aynı sonuç).
        var mappedIds = await _db.ExpeditionLoadMappings.AsNoTracking()
            .Select(m => m.LoadTransferId)
            .ToListAsync(cancellationToken);

        var excluded = mappedIds
            .Where(id => long.TryParse(id, out _))
            .Select(long.Parse)
            .Distinct()
            .ToList();

        var transfers = _db.LoadTransfers.AsNoTracking()
            .Where(t => !excluded.Contains(t.Id));

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Türkçe noktasız I/ı normalizasyonu için bkz. QueryableExtensions.NormalizeTurkish.
            var pattern = $"%{QueryableExtensions.NormalizeTurkish(search)}%";
            transfers = transfers.Where(t =>
                t.LoadNumberWorkType != null &&
                EF.Functions.Like(t.LoadNumberWorkType.Replace("İ", "i").Replace("I", "i").Replace("ı", "i").ToLower(), pattern));
        }

        var projected = transfers
            .OrderByDescending(t => t.Id)
            .Select(t => new AvailableLoadDto
            {
                Id = t.Id,
                LoadNumberWorkType = t.LoadNumberWorkType,
                LoadStatusId = _db.LoadStatusTypes.Where(s => s.Id == t.LoadStatusId)
                    .Select(s => new LoadStatusRefDto
                    {
                        Id = s.Id, Name = s.Name, LoadStatusId = s.LoadStatusId, OrderNo = s.OrderNo,
                    })
                    .FirstOrDefault(),
                CustomerId = _db.Accounts.Where(a => a.Id == t.CustomerId)
                    .Select(a => new MappedNameDto { Id = a.Id, Name = a.Name })
                    .FirstOrDefault(),
                SenderId = _db.Accounts.Where(a => a.Id == t.SenderId)
                    .Select(a => new MappedNameDto { Id = a.Id, Name = a.Name })
                    .FirstOrDefault(),
            });

        return await projected.ToPagedOrListAsync(perPage, page, path, cancellationToken);
    }

    public async Task<ExpeditionMappingListDto> ByExpeditionAsync(
        long expeditionId, CancellationToken cancellationToken = default)
    {
        var key = expeditionId.ToString();

        var mappings = await _db.ExpeditionLoadMappings.AsNoTracking()
            .Where(m => m.ExpeditionId == key)
            .ToListAsync(cancellationToken);

        if (mappings.Count == 0)
            return new ExpeditionMappingListDto();

        // İlişkili kayıtlar metin anahtarlar üzerinden bağlı; toplu okuyup
        // bellekte eşliyoruz (EF metin→sayı çevrimini SQL'e taşıyamıyor).
        var transferIds = ParseIds(mappings.Select(m => m.LoadTransferId));
        var romorkIds = ParseIds(mappings.Select(m => m.RomorkId));
        var yerIds = ParseGuids(mappings.Select(m => m.YerId));

        var transfers = await _db.LoadTransfers.AsNoTracking()
            .Where(t => transferIds.Contains(t.Id))
            .Select(t => new { t.Id, t.LoadNumberWorkType, t.LoadTransferId, t.CustomerId })
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        var customerIds = transfers.Values
            .Where(t => t.CustomerId != null)
            .Select(t => (long)t.CustomerId!.Value)
            .Distinct()
            .ToList();

        var customers = await _db.Accounts.AsNoTracking()
            .Where(a => customerIds.Contains(a.Id))
            .Select(a => new MappedNameDto { Id = a.Id, Name = a.Name })
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        var transferKeys = transfers.Values
            .Where(t => t.LoadTransferId != null)
            .Select(t => t.LoadTransferId!)
            .ToList();

        var packages = await _db.LoadTransferPackages.AsNoTracking()
            .Where(p => p.LoadTransferId != null && transferKeys.Contains(p.LoadTransferId))
            .ToListAsync(cancellationToken);

        var caseTypes = await _db.CaseTypes.AsNoTracking()
            .Select(c => new MappedNameDto { Id = c.Id, Name = c.Name })
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var productTypes = await _db.ProductTypes.AsNoTracking()
            .Select(p => new MappedNameDto { Id = p.Id, Name = p.Name })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var cars = await _db.Cars.AsNoTracking()
            .Where(c => romorkIds.Contains(c.Id))
            .Select(c => new MappedCarDto { Id = c.Id, PlateNumber = c.PlateNumber })
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var cities = await _db.Cities.AsNoTracking()
            .Where(c => yerIds.Contains(c.Id))
            .Select(c => new MappedCityDto { Id = c.Id, CountryId = c.CountryId, Name = c.Name })
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var expedition = await _db.Expeditions.AsNoTracking()
            .Where(e => e.Id == expeditionId)
            .Select(e => new MappedExpeditionDto { Id = e.Id, ExpeditionNumber = e.ExpeditionNumber })
            .FirstOrDefaultAsync(cancellationToken);

        // Bağlı yüklerin arşivi TEK sorguda çekilir (yük başına gidiş-dönüş yerine).
        var archivesByYukId = await _archive.ListByModulesAsync(transferKeys, cancellationToken);

        var totals = new MappingTotalsDto();
        var rows = new List<ExpeditionMappingDto>(mappings.Count);

        foreach (var mapping in mappings)
        {
            MappedLoadTransferDto? transferDto = null;
            var rowTotals = new MappingTotalsDto();

            if (long.TryParse(mapping.LoadTransferId, out var transferId) &&
                transfers.TryGetValue(transferId, out var transfer))
            {
                var own = transfer.LoadTransferId is null
                    ? []
                    : packages.Where(p => p.LoadTransferId == transfer.LoadTransferId).ToList();

                foreach (var package in own)
                {
                    rowTotals.TotalQuantity += package.Quantity ?? 0;
                    rowTotals.TotalGrossWeight += package.GrossWeight ?? 0;
                    rowTotals.TotalNetWeight += package.NetWeight ?? 0;
                    rowTotals.TotalLademeter += package.Lademeter ?? 0;
                    rowTotals.TotalVolume += package.Volume ?? 0;
                }

                transferDto = new MappedLoadTransferDto
                {
                    Id = transfer.Id,
                    LoadNumberWorkType = transfer.LoadNumberWorkType,
                    LoadTransferId = transfer.LoadTransferId,
                    CustomerId = transfer.CustomerId != null &&
                                 customers.TryGetValue(transfer.CustomerId.Value, out var customer)
                        ? customer
                        : null,
                    SiberArchive = (transfer.LoadTransferId is not null &&
                                    archivesByYukId.TryGetValue(transfer.LoadTransferId, out var files)
                        ? files
                        : [])
                        .Select(a => new MappedArchiveDto
                        {
                            Id = a.ArsivId,
                            Name = a.Ad,
                            CreatedAt = a.KayitGirisTarih,
                            CreatedBy = a.KayitGiren,
                            PersonalData = a.KisiselVeri,
                            RestrictedGroups = string.IsNullOrWhiteSpace(a.YetkiliGruplar) ? null : a.YetkiliGruplar,
                        })
                        .ToList(),

                    LoadTransferPackage = own.Select(p => new MappedPackageDto
                    {
                        Id = p.Id,
                        Quantity = p.Quantity,
                        GrossWeight = p.GrossWeight,
                        NetWeight = p.NetWeight,
                        Lademeter = p.Lademeter,
                        Volume = p.Volume,
                        CaseTypeId = long.TryParse(p.CaseTypeId, out var caseId) &&
                                     caseTypes.TryGetValue(caseId, out var caseType)
                            ? caseType
                            : null,
                        ProductTypeId = p.ProductTypeId != null &&
                                        productTypes.TryGetValue(p.ProductTypeId.Value, out var productType)
                            ? productType
                            : null,
                    }).ToList(),
                };
            }

            totals.TotalQuantity += rowTotals.TotalQuantity;
            totals.TotalGrossWeight += rowTotals.TotalGrossWeight;
            totals.TotalNetWeight += rowTotals.TotalNetWeight;
            totals.TotalLademeter += rowTotals.TotalLademeter;
            totals.TotalVolume += rowTotals.TotalVolume;

            rows.Add(new ExpeditionMappingDto
            {
                Id = mapping.Id,
                Yukaktarmaid = mapping.Yukaktarmaid,
                UploadUnload = mapping.UploadUnload,
                Date = mapping.Date,
                LoadTransferId = transferDto,
                ExpeditionId = expedition,
                RomorkId = long.TryParse(mapping.RomorkId, out var romorkId) &&
                           cars.TryGetValue(romorkId, out var car)
                    ? car
                    : null,
                YerId = Guid.TryParse(mapping.YerId, out var yerId) &&
                        cities.TryGetValue(yerId, out var city)
                    ? city
                    : null,
                TotalValues = rowTotals,
            });
        }

        return new ExpeditionMappingListDto { Data = rows, Totals = totals };
    }

    public async Task<MappingSaveResult> SaveAsync(
        long? expeditionId, long? loadTransferId, CancellationToken cancellationToken = default)
    {
        var expedition = await _db.Expeditions
            .FirstOrDefaultAsync(e => e.Id == expeditionId, cancellationToken);

        if (expedition is null)
            return MappingSaveResult.Fail("Sefer Bulunamadı");

        var romork = await _db.Cars
            .FirstOrDefaultAsync(c => c.Id == expedition.RomorkId, cancellationToken);

        if (romork is null)
            return MappingSaveResult.Fail("Araç Bulunamadı");

        var transfer = await _db.LoadTransfers
            .FirstOrDefaultAsync(t => t.Id == loadTransferId, cancellationToken);

        if (transfer is null)
            return MappingSaveResult.Fail("Transfer Tipi Bulunamadı");

        if (romork.RomorkType != transfer.RomorkTypeId)
            return MappingSaveResult.Fail("Yük ile Araç romork tipi uyuşmuyor");

        var key = transfer.Id.ToString();

        var alreadyMapped = await _db.ExpeditionLoadMappings
            .AnyAsync(m => m.LoadTransferId == key, cancellationToken);

        if (alreadyMapped)
            return MappingSaveResult.Fail("Bu Sefer Zaten Eklendi");

        var now = _clock.Now;

        var siberId = _siber.IsConfigured
            ? (await _siber.GenerateYukAktarmaIdAsync(cancellationToken)).ToString()
            : Guid.NewGuid().ToString();

        var mapping = new ExpeditionLoadMapping
        {
            Yukaktarmaid = siberId,
            UploadUnload = 1,
            LoadTransferId = key,
            ExpeditionId = expedition.Id.ToString(),
            RomorkId = romork.Id.ToString(),
            YerId = expedition.StartCityId?.ToString(),
            Date = DateOnly.FromDateTime(now),
            CreatedAt = now,
            UpdatedAt = now,
        };

        // Kaynak iki bağlantıyı birlikte açıp birlikte commit ediyor. Burada da
        // Siber yazımı patlarsa PostgreSQL satırı KALMAMALI.
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        _db.ExpeditionLoadMappings.Add(mapping);
        await _db.SaveChangesAsync(cancellationToken);

        if (_siber.IsConfigured)
        {
            await _siber.InsertYukAktarmaAsync(new SiberYukAktarma
            {
                YukAktarmaId = siberId,
                YuklemeBosaltma = 1,
                YukId = transfer.LoadTransferId,
                PozisyonId = expedition.ExpeditionId,
                RomorkId = romork.SiberId,
                YerId = expedition.StartCityId?.ToString(),
                Tarih = now,
            }, cancellationToken);

            if (transfer.LoadTransferId is { } yukId)
                await _siber.SetYukPozisyonAsync(yukId, expedition.ExpeditionId, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return new MappingSaveResult(siberId, null);
    }

    /// <summary>
    /// KAYNAK DAVRANIŞI: bu uç YALNIZCA Siber tarafını günceller, PostgreSQL
    /// satırına dokunmaz. Arayüzden de çağrılmıyor. Davranış aynen korundu.
    /// </summary>
    public async Task UpdateAsync(
        ExpeditionMappingUpdateInput input, CancellationToken cancellationToken = default)
    {
        if (!_siber.IsConfigured || string.IsNullOrWhiteSpace(input.MappingSiberId))
            return;

        await _siber.UpdateYukAktarmaAsync(new SiberYukAktarma
        {
            YukAktarmaId = input.MappingSiberId,
            YuklemeBosaltma = 1,
            YukId = input.LoadTransferId,
            PozisyonId = input.ExpeditionId,
            RomorkId = input.RomorkId,
            YerId = input.YerId,
            Tarih = input.Date,
        }, cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return false;

        var mappings = await _db.ExpeditionLoadMappings
            .Where(m => ids.Contains(m.Id))
            .ToListAsync(cancellationToken);

        if (mappings.Count == 0)
            return false;

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        if (_siber.IsConfigured)
        {
            var transferIds = ParseIds(mappings.Select(m => m.LoadTransferId));

            var yukIds = await _db.LoadTransfers.AsNoTracking()
                .Where(t => transferIds.Contains(t.Id))
                .Select(t => new { t.Id, t.LoadTransferId })
                .ToDictionaryAsync(t => t.Id, t => t.LoadTransferId, cancellationToken);

            foreach (var mapping in mappings)
            {
                if (mapping.Yukaktarmaid is { } siberId)
                    await _siber.DeleteYukAktarmaAsync(siberId, cancellationToken);

                if (long.TryParse(mapping.LoadTransferId, out var transferId) &&
                    yukIds.TryGetValue(transferId, out var yukId) && yukId is not null)
                {
                    // Yükün sefere bağı koparılır.
                    await _siber.SetYukPozisyonAsync(yukId, null, cancellationToken);
                }
            }
        }

        _db.ExpeditionLoadMappings.RemoveRange(mappings);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    private static List<long> ParseIds(IEnumerable<string?> values) =>
        values.Where(v => long.TryParse(v, out _)).Select(long.Parse!).Distinct().ToList();

    private static List<Guid> ParseGuids(IEnumerable<string?> values) =>
        values.Where(v => Guid.TryParse(v, out _)).Select(Guid.Parse!).Distinct().ToList();
}
