using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.Business.Services.Loads;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;
using OLS.DataAccess.Siber;

namespace OLS.Business.Services.LoadTransfers;

/// <summary>
/// GERÇEK KOLİ BİLGİLERİ — Siber'de <c>skn_yukkolidepo</c>.
///
/// Siber'in yük ekranında koli bölümü iki sete ayrılıyor: beyan edilen
/// "Yük Koli Bilgileri" (<c>skn_yukkoli</c>) ve depoda ölçülen "Gerçek Koli
/// Bilgileri". Aradaki <b>"Gerçek Koli Bilgilerine Aktar"</b> düğmesi ilkini
/// ikincisine kopyalıyor; kullanıcı sonra gerçekleşen ölçülere göre düzeltiyor.
///
/// Canlı veri bu akışı doğruluyor: iki seti de olan 4.064 yükün 3.920'sinde
/// (%96,5) toplamlar birebir aynı — yani aktarılmış ve dokunulmamış; kalan
/// ~144'ünde farklı — aktarıldıktan sonra düzeltilmiş.
/// </summary>
public interface IActualPackageService
{
    Task<IReadOnlyList<ActualPackageDto>> ListAsync(
        long loadTransferId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Beyan edilen kolileri gerçek koli setine KOPYALAR (Siber'deki
    /// "Gerçek Koli Bilgilerine Aktar" düğmesi).
    /// </summary>
    Task<ActualPackageResult> CopyFromDeclaredAsync(
        long loadTransferId, bool replaceExisting, CancellationToken cancellationToken = default);

    Task<ActualPackageResult> SaveAsync(
        long loadTransferId, IReadOnlyList<ActualPackageInput> rows,
        CancellationToken cancellationToken = default);

    Task<ActualPackageResult> DeleteAsync(
        IReadOnlyList<long> ids, CancellationToken cancellationToken = default);
}

public sealed record ActualPackageResult(int Affected, string? ErrorMessage)
{
    public bool IsSuccess => ErrorMessage is null;
    public static ActualPackageResult Fail(string message) => new(0, message);
    public static ActualPackageResult Ok(int affected) => new(affected, null);
}

public sealed class ActualPackageInput
{
    public long? Id { get; set; }
    public int? Quantity { get; set; }
    public string? CaseTypeId { get; set; }
    public int? ProductTypeId { get; set; }
    public decimal? Width { get; set; }
    public decimal? Length { get; set; }
    public decimal? Height { get; set; }
    public decimal? Volume { get; set; }
    public decimal? GrossWeight { get; set; }
    public decimal? NetWeight { get; set; }
    public decimal? Lademeter { get; set; }
    public int? Stackable { get; set; }
}

public sealed class ActualPackageDto
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("yukkolidepoid")] public string? Yukkolidepoid { get; init; }
    [JsonPropertyName("quantity")] public int? Quantity { get; init; }
    // Beyan edilen koli DTO'suyla AYNI şekil: arayüz iki seti de aynı satır
    // bileşeniyle çiziyor, tipler ayrışırsa eşleme elle yazılmak zorunda kalır.
    [JsonPropertyName("case_type_id")] public NamedRefDto? CaseTypeId { get; init; }
    [JsonPropertyName("product_type_id")] public NamedRefDto? ProductTypeId { get; init; }
    [JsonPropertyName("width")] public decimal? Width { get; init; }
    [JsonPropertyName("length")] public decimal? Length { get; init; }
    [JsonPropertyName("height")] public decimal? Height { get; init; }
    [JsonPropertyName("volume")] public decimal? Volume { get; init; }
    [JsonPropertyName("gross_weight")] public decimal? GrossWeight { get; init; }
    [JsonPropertyName("net_weight")] public decimal? NetWeight { get; init; }
    [JsonPropertyName("lademeter")] public decimal? Lademeter { get; init; }
    [JsonPropertyName("stackable")] public int? Stackable { get; init; }
}

public sealed class ActualPackageService : IActualPackageService
{
    private readonly OlsDbContext _db;
    private readonly ISiberLoadRepository _siber;
    private readonly IClock _clock;

    public ActualPackageService(OlsDbContext db, ISiberLoadRepository siber, IClock clock)
    {
        _db = db;
        _siber = siber;
        _clock = clock;
    }

    public async Task<IReadOnlyList<ActualPackageDto>> ListAsync(
        long loadTransferId, CancellationToken cancellationToken = default)
    {
        var key = await KeyAsync(loadTransferId, cancellationToken);

        if (key is null)
            return [];

        return await _db.LoadTransferActualPackages.AsNoTracking()
            .Where(p => p.LoadTransferId == key)
            .OrderBy(p => p.Id)
            .Select(p => new ActualPackageDto
            {
                Id = p.Id,
                Yukkolidepoid = p.Yukkolidepoid,
                Quantity = p.Quantity,
                CaseTypeId = _db.CaseTypes
                    .Where(c => c.Id.ToString() == p.CaseTypeId)
                    .Select(c => new NamedRefDto { Id = c.Id, Name = c.Name })
                    .FirstOrDefault(),
                ProductTypeId = _db.ProductTypes
                    .Where(t => t.Id == p.ProductTypeId)
                    .Select(t => new NamedRefDto { Id = t.Id, Name = t.Name })
                    .FirstOrDefault(),
                Width = p.Width,
                Length = p.Length,
                Height = p.Height,
                Volume = p.Volume,
                GrossWeight = p.GrossWeight,
                NetWeight = p.NetWeight,
                Lademeter = p.Lademeter,
                Stackable = p.Stackable,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ActualPackageResult> CopyFromDeclaredAsync(
        long loadTransferId, bool replaceExisting, CancellationToken cancellationToken = default)
    {
        var transfer = await _db.LoadTransfers
            .FirstOrDefaultAsync(t => t.Id == loadTransferId, cancellationToken);

        if (transfer is null)
            return ActualPackageResult.Fail("Yük bulunamadı");

        // ANAHTAR SİBER YUKID — BULUNAN GERÇEK HATA.
        //
        // load_transfer_packages.load_transfer_id yerel sayısal kimliği DEĞİL,
        // Siber'in yukid GUID'ini tutuyor (senkron öyle yazıyor, detay ucu da
        // öyle okuyor). Burada yerel kimlikle aranıyordu; sorgu hiçbir zaman
        // satır bulamıyor ve düğme "Aktarılacak koli bilgisi yok" diyordu.
        var key = transfer.LoadTransferId;

        if (string.IsNullOrWhiteSpace(key))
            return ActualPackageResult.Fail("Yükün Siber kaydı yok");

        var declared = await _db.LoadTransferPackages.AsNoTracking()
            .Where(p => p.LoadTransferId == key)
            .OrderBy(p => p.Id)
            .ToListAsync(cancellationToken);

        if (declared.Count == 0)
            return ActualPackageResult.Fail("Aktarılacak koli bilgisi yok");

        var existing = await _db.LoadTransferActualPackages
            .Where(p => p.LoadTransferId == key)
            .ToListAsync(cancellationToken);

        // ÜZERİNE YAZMA VARSAYILAN DEĞİL: gerçek koli satırları depoda elle
        // düzeltilmiş olabilir (canlıda ~144 yükte öyle). Kullanıcı bilerek
        // istemedikçe mevcut satırlar korunur.
        if (existing.Count > 0 && !replaceExisting)
            return ActualPackageResult.Fail(
                "Gerçek koli bilgileri zaten dolu. Üzerine yazmak için onaylayın.");

        var now = _clock.Now;

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        foreach (var row in existing)
        {
            if (_siber.IsConfigured && row.Yukkolidepoid is { } depoId)
                await _siber.DeleteYukKoliDepoAsync(depoId, cancellationToken);
        }

        _db.LoadTransferActualPackages.RemoveRange(existing);

        foreach (var source in declared)
        {
            var depoId = _siber.IsConfigured
                ? (await _siber.GenerateYukKoliDepoIdAsync(cancellationToken)).ToString()
                : Guid.NewGuid().ToString();

            _db.LoadTransferActualPackages.Add(new LoadTransferActualPackage
            {
                Yukkolidepoid = depoId,
                LoadTransferId = key,
                Quantity = source.Quantity,
                CaseTypeId = source.CaseTypeId,
                ProductTypeId = source.ProductTypeId,
                Width = source.Width,
                Length = source.Length,
                Height = source.Height,
                Volume = source.Volume,
                GrossWeight = source.GrossWeight,
                NetWeight = source.NetWeight,
                Lademeter = source.Lademeter,
                Stackable = source.Stackable,
                CreatedAt = now,
                UpdatedAt = now,
            });

            if (_siber.IsConfigured && transfer.LoadTransferId is { } yukId)
                await _siber.InsertYukKoliDepoAsync(
                    await ToSiberAsync(depoId, yukId, source.CaseTypeId, source.ProductTypeId,
                        source.Quantity, source.Width, source.Length, source.Height, source.Volume,
                        source.GrossWeight, source.NetWeight, source.Lademeter, source.Stackable,
                        cancellationToken),
                    cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return ActualPackageResult.Ok(declared.Count);
    }

    public async Task<ActualPackageResult> SaveAsync(
        long loadTransferId, IReadOnlyList<ActualPackageInput> rows,
        CancellationToken cancellationToken = default)
    {
        var transfer = await _db.LoadTransfers
            .FirstOrDefaultAsync(t => t.Id == loadTransferId, cancellationToken);

        if (transfer is null)
            return ActualPackageResult.Fail("Yük bulunamadı");

        var key = transfer.LoadTransferId;

        if (string.IsNullOrWhiteSpace(key))
            return ActualPackageResult.Fail("Yükün Siber kaydı yok");

        var now = _clock.Now;

        var existing = await _db.LoadTransferActualPackages
            .Where(p => p.LoadTransferId == key)
            .ToListAsync(cancellationToken);

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        foreach (var input in rows)
        {
            var row = input.Id is { } id ? existing.FirstOrDefault(p => p.Id == id) : null;
            var isNew = row is null;

            if (isNew)
            {
                row = new LoadTransferActualPackage
                {
                    LoadTransferId = key,
                    Yukkolidepoid = _siber.IsConfigured
                        ? (await _siber.GenerateYukKoliDepoIdAsync(cancellationToken)).ToString()
                        : Guid.NewGuid().ToString(),
                    CreatedAt = now,
                };

                _db.LoadTransferActualPackages.Add(row);
            }

            row!.Quantity = input.Quantity;
            row.CaseTypeId = input.CaseTypeId;
            row.ProductTypeId = input.ProductTypeId;
            row.Width = input.Width;
            row.Length = input.Length;
            row.Height = input.Height;
            row.Volume = input.Volume;
            row.GrossWeight = input.GrossWeight;
            row.NetWeight = input.NetWeight;
            row.Lademeter = input.Lademeter;
            row.Stackable = input.Stackable;
            row.UpdatedAt = now;

            if (_siber.IsConfigured && transfer.LoadTransferId is { } yukId
                && row.Yukkolidepoid is { } depoId)
            {
                var koli = await ToSiberAsync(depoId, yukId, row.CaseTypeId, row.ProductTypeId,
                    row.Quantity, row.Width, row.Length, row.Height, row.Volume,
                    row.GrossWeight, row.NetWeight, row.Lademeter, row.Stackable, cancellationToken);

                if (isNew)
                    await _siber.InsertYukKoliDepoAsync(koli, cancellationToken);
                else
                    await _siber.UpdateYukKoliDepoAsync(koli, cancellationToken);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return ActualPackageResult.Ok(rows.Count);
    }

    public async Task<ActualPackageResult> DeleteAsync(
        IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        var rows = await _db.LoadTransferActualPackages
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
            return ActualPackageResult.Ok(0);

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        foreach (var row in rows)
        {
            if (_siber.IsConfigured && row.Yukkolidepoid is { } depoId)
                await _siber.DeleteYukKoliDepoAsync(depoId, cancellationToken);
        }

        _db.LoadTransferActualPackages.RemoveRange(rows);
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return ActualPackageResult.Ok(rows.Count);
    }

    /// <summary>
    /// Koli satırlarının anahtarı: Siber'in <c>yukid</c> GUID'i. Beyan edilen
    /// koli tablosu da bu anahtarı kullanıyor; ikisi ayrışırsa "Gerçek Koli
    /// Bilgilerine Aktar" hiçbir satır bulamaz.
    /// </summary>
    private async Task<string?> KeyAsync(long loadTransferId, CancellationToken cancellationToken) =>
        await _db.LoadTransfers.AsNoTracking()
            .Where(t => t.Id == loadTransferId)
            .Select(t => t.LoadTransferId)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Yerel kimlikleri Siber karşılıklarına çevirir: kap cinsi ve ürün grubu
    /// Siber'de GUID tutuluyor, yerelde sayısal kimlik.
    /// </summary>
    private async Task<SiberYukKoliDepo> ToSiberAsync(
        string depoId, string yukId, string? caseTypeId, int? productTypeId,
        int? quantity, decimal? width, decimal? length, decimal? height, decimal? volume,
        decimal? grossWeight, decimal? netWeight, decimal? lademeter, int? stackable,
        CancellationToken cancellationToken)
    {
        var kapId = long.TryParse(caseTypeId, out var caseId)
            ? await _db.CaseTypes.AsNoTracking()
                .Where(c => c.Id == caseId).Select(c => c.SiberId)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var malCinsId = productTypeId is { } productId
            ? await _db.ProductTypes.AsNoTracking()
                .Where(p => p.Id == productId).Select(p => p.SiberId)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        return new SiberYukKoliDepo
        {
            YukKoliDepoId = depoId,
            YukId = yukId,
            KapAdet = quantity,
            KapId = kapId,
            En = width,
            Boy = length,
            Yukseklik = height,
            Hacim = volume,
            BurutAgirlik = grossWeight,
            NetAgirlik = netWeight,
            Lademetre = lademeter,
            Istiflenemez = stackable,
            MalCinsId = malCinsId,
        };
    }
}
