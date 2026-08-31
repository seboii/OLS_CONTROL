using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;
using OLS.DataAccess.Siber;

namespace OLS.Business.Services.Finance;

/// <summary>
/// Uygulamadan fatura açar.
///
/// SİBER ÖNCE, YEREL SONRA. Kayıt önce Siber'e yazılır, ancak başarılı olursa
/// yerel satır açılır. Ters sıra, Siber'de karşılığı olmayan bir yerel fatura
/// bırakırdı — projede silme sırasında öğrenilen aynı kural.
///
/// TUTARLAR SUNUCUDA HESAPLANIR. İstemci yalnızca kalem, miktar, birim fiyat
/// ve KDV oranı gönderir; toplamları istemciden almak, satırlarla başlığın
/// ayrışmasına ve muhasebe kaydının sessizce yanlış olmasına yol açar.
/// </summary>
public interface IFinanceInvoiceWriteService
{
    Task<FinanceInvoiceCreateResult> CreateAsync(
        FinanceInvoiceCreateRequest request, CancellationToken cancellationToken = default);
}

public sealed class FinanceInvoiceCreateRequest
{
    /// <summary>"C" gelir, "G" gider.</summary>
    public string Direction { get; init; } = "C";

    public long AccountId { get; init; }

    /// <summary>Gelir faturasında zorunlu (DKT, UA, OGM…).</summary>
    public string? Series { get; init; }

    /// <summary>Gider faturasında tedarikçinin fatura numarası.</summary>
    public string? InvoiceNumber { get; init; }

    public DateTime InvoiceDate { get; init; }
    public DateTime? DueDate { get; init; }

    public string CurrencyCode { get; init; } = "TL ";
    public decimal ExchangeRate { get; init; } = 1m;

    public string? Description { get; init; }
    public string? DocumentNumber { get; init; }

    public long? LoadTransferId { get; init; }

    public IReadOnlyList<FinanceInvoiceLineRequest> Lines { get; init; } = [];
}

public sealed class FinanceInvoiceLineRequest
{
    /// <summary>Yerel mali kalem kaydı.</summary>
    public long FinancialItemId { get; init; }

    public decimal Quantity { get; init; } = 1m;
    public decimal UnitPrice { get; init; }
    public decimal TaxRate { get; init; }
    public string? Description { get; init; }
}

public sealed record FinanceInvoiceCreateResult(
    bool Success, long? Id, string? InvoiceNumber, string? Error);

public sealed class FinanceInvoiceWriteService : IFinanceInvoiceWriteService
{
    private readonly OlsDbContext _db;
    private readonly ISiberInvoiceWriter _writer;
    private readonly ICompanyScope _companyScope;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly ILogger<FinanceInvoiceWriteService> _logger;

    public FinanceInvoiceWriteService(
        OlsDbContext db,
        ISiberInvoiceWriter writer,
        ICompanyScope companyScope,
        ICurrentUser currentUser,
        IClock clock,
        ILogger<FinanceInvoiceWriteService> logger)
    {
        _db = db;
        _writer = writer;
        _companyScope = companyScope;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    public async Task<FinanceInvoiceCreateResult> CreateAsync(
        FinanceInvoiceCreateRequest request, CancellationToken cancellationToken = default)
    {
        if (!_writer.IsConfigured)
            return new(false, null, null, "Siber bağlantısı yapılandırılmamış.");

        if (request.Lines.Count == 0)
            return new(false, null, null, "Fatura en az bir kalem içermeli.");

        var isIncome = string.Equals(request.Direction, "C", StringComparison.OrdinalIgnoreCase);

        if (isIncome && string.IsNullOrWhiteSpace(request.Series))
            return new(false, null, null, "Gelir faturası için seri kodu zorunlu.");

        if (!isIncome && string.IsNullOrWhiteSpace(request.InvoiceNumber))
            return new(false, null, null, "Gider faturası için fatura numarası zorunlu.");

        var account = await _db.Accounts.AsNoTracking()
            .Where(a => a.Id == request.AccountId)
            .Select(a => new { a.Id, a.Name, a.SiberId })
            .FirstOrDefaultAsync(cancellationToken);

        if (account is null)
            return new(false, null, null, "Cari bulunamadı.");

        if (string.IsNullOrWhiteSpace(account.SiberId))
            return new(false, null, null, "Cari Siber'de eşleşmiyor; fatura açılamaz.");

        // Kalem kimlikleri Siber'e GİDECEK: yerel id değil, Siber kalemid'i.
        var itemIds = request.Lines.Select(l => l.FinancialItemId).Distinct().ToList();
        var items = await _db.FinancialItems.AsNoTracking()
            .Where(i => itemIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Name, i.SiberId })
            .ToListAsync(cancellationToken);

        var itemById = items.ToDictionary(i => i.Id);

        var missing = itemIds.Where(id =>
            !itemById.TryGetValue(id, out var item) || string.IsNullOrWhiteSpace(item.SiberId)).ToList();

        if (missing.Count > 0)
            return new(false, null, null, "Bazı mali kalemler Siber'de eşleşmiyor; fatura açılamaz.");

        // Şirket: kullanıcının kapsamı belirler. Avrora ekibi Avrora şirketine,
        // diğerleri OLS'e yazar; her ikisini gören yönetici OLS'e yazar.
        var visibility = await _companyScope.ResolveAsync(_currentUser.Id, cancellationToken);
        var sirketId = visibility.OnlyCompanyId ?? SiberInvoiceWriter.DefaultSirketId;

        string? moduleId = null;
        string? moduleCode = null;

        if (request.LoadTransferId is { } transferId)
        {
            var transfer = await _db.LoadTransfers.AsNoTracking()
                .Where(t => t.Id == transferId)
                .Select(t => new { t.LoadTransferId, t.WorkType })
                .FirstOrDefaultAsync(cancellationToken);

            if (transfer?.LoadTransferId is null)
                return new(false, null, null, "Yük Siber'de eşleşmiyor.");

            moduleId = transfer.LoadTransferId;
            moduleCode = await ModuleCodeAsync(transfer.WorkType, cancellationToken);
        }

        var userCode = _currentUser.Id is { } userId
            ? await _db.Users.AsNoTracking()
                .Where(u => u.Id == userId).Select(u => u.SiberCode)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var insert = new SiberInvoiceInsert
        {
            Direction = isIncome ? "C" : "G",
            SirketId = sirketId,
            FirmaId = account.SiberId!,
            FirmaAd = account.Name,
            SeriNo = request.Series,
            FaturaNo = request.InvoiceNumber,
            FaturaTarihi = request.InvoiceDate,
            VadeTarihi = request.DueDate,
            DovizKod = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "TL " : request.CurrencyCode,
            DovizKur = (double)(request.ExchangeRate <= 0 ? 1m : request.ExchangeRate),
            Aciklama = request.Description,
            BelgeNo = request.DocumentNumber,
            ModulId = moduleId,
            ModulKod = moduleCode,
            KayitGiren = userCode,
            Lines = request.Lines.Select(l => new SiberInvoiceLineInsert
            {
                KalemId = itemById[l.FinancialItemId].SiberId!,
                Miktar = (double)l.Quantity,
                BirimFiyat = (double)l.UnitPrice,
                KdvOran = (double)l.TaxRate,
                Aciklama = l.Description,
            }).ToList(),
        };

        SiberInvoiceWriteResult written;

        try
        {
            written = await _writer.InsertAsync(insert, cancellationToken);
        }
        catch (Exception ex)
        {
            // Siber'e yazılamadıysa yerelde de kayıt açılmaz; hata yutulmaz.
            _logger.LogError(ex, "Fatura Siber'e yazılamadı (cari {AccountId}).", request.AccountId);
            return new(false, null, null, "Fatura Siber'e yazılamadı.");
        }

        var rate = request.ExchangeRate <= 0 ? 1m : request.ExchangeRate;

        var lines = request.Lines.Select(l =>
        {
            var amount = Math.Round(l.Quantity * l.UnitPrice, 2);
            var tax = Math.Round(amount * l.TaxRate / 100m, 2);

            return new FinanceInvoiceLine
            {
                // Siber satır kimliklerini geri okumuyoruz; bir sonraki senkron
                // turu satırları kendi kimlikleriyle üzerine yazar. Geçici
                // kimlik, o tura kadar yerel benzersizliği sağlar.
                SiberId = Guid.NewGuid().ToString(),
                FinancialItemId = itemById[l.FinancialItemId].SiberId,
                FinancialItemName = itemById[l.FinancialItemId].Name,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                CurrencyCode = request.CurrencyCode,
                ExchangeRate = rate,
                TaxRate = l.TaxRate,
                Amount = amount,
                TaxAmount = tax,
                AmountTl = Math.Round(amount * rate, 2),
                TaxAmountTl = Math.Round(tax * rate, 2),
                Description = l.Description,
                CreatedAt = _clock.Now,
                UpdatedAt = _clock.Now,
            };
        }).ToList();

        var total = lines.Sum(l => l.Amount ?? 0m);
        var totalTax = lines.Sum(l => l.TaxAmount ?? 0m);

        var invoice = new FinanceInvoice
        {
            SiberId = written.GelirGiderId,
            Direction = isIncome ? "C" : "G",
            InvoiceSeries = request.Series,
            InvoiceNumber = written.FaturaNo,
            InvoiceDate = request.InvoiceDate,
            DueDate = request.DueDate,
            AccountId = account.Id,
            SiberAccountId = account.SiberId,
            AccountName = account.Name,
            CurrencyCode = request.CurrencyCode,
            ExchangeRate = rate,
            Amount = total,
            TaxAmount = totalTax,
            TotalAmount = total + totalTax,
            AmountTl = Math.Round(total * rate, 2),
            TaxAmountTl = Math.Round(totalTax * rate, 2),
            TotalAmountTl = Math.Round((total + totalTax) * rate, 2),
            Description = request.Description,
            DocumentNumber = request.DocumentNumber,
            ModuleId = moduleId,
            ModuleCode = moduleCode,
            LoadTransferId = request.LoadTransferId,
            SiberCompanyId = sirketId,
            SiberCreatedAt = _clock.Now,
            SiberCreatedBy = userCode,
            CreatedAt = _clock.Now,
            UpdatedAt = _clock.Now,
            Lines = lines,
        };

        _db.FinanceInvoices.Add(invoice);
        await _db.SaveChangesAsync(cancellationToken);

        return new(true, invoice.Id, written.FaturaNo, null);
    }

    /// <summary>Modül kodu yükün iş türüne bağlı (bkz. SiberArchiveWriter).</summary>
    private async Task<string> ModuleCodeAsync(int? workTypeId, CancellationToken cancellationToken)
    {
        if (workTypeId is not { } id)
            return SiberArchiveWriter.ModulKodForWorkType(null);

        var code = await _db.WorkTypes.AsNoTracking()
            .Where(w => w.Id == id).Select(w => w.Code)
            .FirstOrDefaultAsync(cancellationToken);

        return SiberArchiveWriter.ModulKodForWorkType(
            int.TryParse(code, out var parsed) ? parsed : null);
    }
}
