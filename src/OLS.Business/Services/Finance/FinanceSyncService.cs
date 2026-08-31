using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OLS.Business.Common;
using OLS.Business.Services.TransferData;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;
using OLS.DataAccess.Siber;

namespace OLS.Business.Services.Finance;

/// <summary>
/// Siber'in muhasebe/finans tablolarını yerele aktarır.
///
/// PENCERE MANTIĞI — neden ekleme damgasıyla artımlı çekmiyoruz:
/// Siber güncellemeleri damgalamıyor (<c>sfy_fisdetay.updtime</c> 214.954
/// satırın 19'unda dolu). Ekleme zamanına göre çekmek, sonradan düzeltilen
/// kayıtları sessizce atlardı. Bunun yerine her tur son
/// <see cref="DefaultWindowMonths"/> ayın iş tarihi aralığı YENİDEN çekilir;
/// yerelde hiç kayıt yoksa pencere uygulanmaz ve tüm geçmiş bir kez alınır.
///
/// GUID: yerele daima KÜÇÜK harfle yazılır. Projede tutarlı bir konvansiyon
/// yok (financial_items tamamı büyük, load_transfers tamamı küçük), bu yüzden
/// mevcut tablolara yapılan eşleşmelerde iki taraf da küçültülür.
/// </summary>
public interface IFinanceSyncService
{
    Task<SiberImportSummary> SyncAccountingPlanAsync(CancellationToken cancellationToken = default);

    /// <param name="full">
    /// true ise tarih penceresi UYGULANMAZ ve tüm geçmiş yeniden çekilir.
    /// Geri dolum ve onarım için: yarım kalmış bir turdan sonra pencere,
    /// eksik kalan eski kayıtları bir daha hiç getirmez.
    /// </param>
    Task<SiberImportSummary> SyncVouchersAsync(bool full = false, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="SyncVouchersAsync"/>
    Task<SiberImportSummary> SyncInvoicesAsync(bool full = false, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="SyncVouchersAsync"/>
    Task<SiberImportSummary> SyncPaymentsAsync(bool full = false, CancellationToken cancellationToken = default);
}

public sealed class FinanceSyncService : IFinanceSyncService
{
    /// <summary>Her turda yeniden çekilen geçmiş aralık.</summary>
    private const int DefaultWindowMonths = 6;

    /// <summary>Değişiklik izleyicisini şişirmemek için ara kayıt aralığı.</summary>
    private const int SaveBatchSize = 5000;

    private readonly OlsDbContext _db;
    private readonly ISiberFinanceRepository _siber;
    private readonly IClock _clock;
    private readonly ILogger<FinanceSyncService> _logger;

    public FinanceSyncService(
        OlsDbContext db,
        ISiberFinanceRepository siber,
        IClock clock,
        ILogger<FinanceSyncService> logger)
    {
        _db = db;
        _siber = siber;
        _clock = clock;
        _logger = logger;
    }

    // ------------------------------------------------------------------
    // Hesap planı
    // ------------------------------------------------------------------
    public async Task<SiberImportSummary> SyncAccountingPlanAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_siber.IsConfigured)
            return SiberImportSummary.Empty;

        var rows = await _siber.GetAccountingPlanAsync(cancellationToken);
        if (rows.Count == 0)
            return SiberImportSummary.Empty;

        var existing = await _db.AccountingPlans
            .ToDictionaryAsync(p => p.SiberId, cancellationToken);

        int created = 0, updated = 0;

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.HesapKod))
                continue;

            if (!existing.TryGetValue(row.HesapPlanId, out var plan))
            {
                plan = new AccountingPlan
                {
                    SiberId = row.HesapPlanId,
                    Code = row.HesapKod,
                    CreatedAt = _clock.Now,
                };
                _db.AccountingPlans.Add(plan);
                existing[row.HesapPlanId] = plan;
                created++;
            }
            else
            {
                updated++;
            }

            plan.Code = row.HesapKod;
            plan.Name = Trim(row.Ad, 191);
            plan.Name2 = Trim(row.Ad2, 191);
            plan.Level = row.Seviye;
            plan.IsPassive = row.Pasif == true;
            plan.SiberCompanyId = row.SirketId;
            plan.UpdatedAt = _clock.Now;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new SiberImportSummary(created, updated, []);
    }

    // ------------------------------------------------------------------
    // Muhasebe fişi + satırları — cari ekstrenin kaynağı
    // ------------------------------------------------------------------
    public async Task<SiberImportSummary> SyncVouchersAsync(
        bool full = false, CancellationToken cancellationToken = default)
    {
        if (!_siber.IsConfigured)
            return SiberImportSummary.Empty;

        var since = await WindowStartAsync(
            full, _db.FinanceVouchers.AnyAsync(cancellationToken));

        var headers = await _siber.GetVouchersAsync(since, cancellationToken);
        if (headers.Count == 0)
            return SiberImportSummary.Empty;

        var existing = await _db.FinanceVouchers
            .Where(v => since == null || v.VoucherDate >= since)
            .ToDictionaryAsync(v => v.SiberId, cancellationToken);

        int created = 0, updated = 0;

        foreach (var row in headers)
        {
            if (!existing.TryGetValue(row.FisId, out var voucher))
            {
                voucher = new FinanceVoucher { SiberId = row.FisId, CreatedAt = _clock.Now };
                _db.FinanceVouchers.Add(voucher);
                existing[row.FisId] = voucher;
                created++;
            }
            else
            {
                updated++;
            }

            voucher.VoucherType = row.FisTur;
            voucher.VoucherDate = row.FisTarih;
            voucher.VoucherNumber = row.FisNo;
            voucher.JournalNumber = row.YevmiyeNo;
            voucher.Description = row.Aciklama;
            voucher.CurrencyCode = Trim(row.DovizTur, 8);
            voucher.DocumentNumber = Trim(row.MuhasebeBelgeNo, 64);
            voucher.DocumentDate = row.MuhasebeBelgeTarih;
            voucher.IsChecked = row.KontrolEdildi == true;
            voucher.SiberCompanyId = row.SirketId;
            voucher.SiberCreatedAt = row.KayitGirisTarih;
            voucher.SiberCreatedBy = Trim(row.InsUser, 128);
            voucher.UpdatedAt = _clock.Now;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var lineSummary = await SyncVoucherLinesAsync(since, existing, cancellationToken);

        return new SiberImportSummary(created, updated, [])
        {
            Notes = lineSummary,
        };
    }

    private async Task<IReadOnlyList<string>> SyncVoucherLinesAsync(
        DateTime? since,
        IReadOnlyDictionary<string, FinanceVoucher> vouchers,
        CancellationToken cancellationToken)
    {
        var rows = await _siber.GetVoucherLinesAsync(since, cancellationToken);
        if (rows.Count == 0)
            return [];

        var existing = await _db.FinanceVoucherLines
            .Where(l => since == null || l.FinanceVoucher.VoucherDate >= since)
            .ToDictionaryAsync(l => l.SiberId, cancellationToken);

        var accountBySiberId = await AccountLookupAsync(cancellationToken);

        int created = 0, updated = 0, orphan = 0, matched = 0;
        var pending = 0;

        foreach (var row in rows)
        {
            // Başlığı çekilememiş satır yazılamaz: fiş tarihi penceresinin
            // dışında kalan bir başlığa ait olabilir.
            if (!vouchers.TryGetValue(row.FisId, out var voucher))
            {
                orphan++;
                continue;
            }

            if (!existing.TryGetValue(row.FisDetayId, out var line))
            {
                line = new FinanceVoucherLine
                {
                    SiberId = row.FisDetayId,
                    CreatedAt = _clock.Now,
                };
                _db.FinanceVoucherLines.Add(line);
                existing[row.FisDetayId] = line;
                created++;
            }
            else
            {
                updated++;
            }

            line.FinanceVoucher = voucher;
            line.AccountCode = row.HesapKod ?? string.Empty;
            line.Debit = ToDecimal(row.Borc);
            line.Credit = ToDecimal(row.Alacak);
            line.DebitFx = ToDecimal(row.BorcDoviz);
            line.CreditFx = ToDecimal(row.AlacakDoviz);
            line.CurrencyCode = Trim(row.DovizTur, 8);
            line.ExchangeRate = ToDecimal(row.DovizKur, 6);
            line.Description = row.Aciklama;
            line.SiberAccountId = EmptyGuidToNull(row.KartoteksId);
            line.SourceId = EmptyGuidToNull(row.EntegreId);
            line.DocumentNumber = Trim(row.BelgeNo, 64);
            line.DocumentDate = row.BelgeTarih;
            line.DueDate = row.VadeTarih;
            line.LineNumber = row.SiraNo;
            line.SiberCompanyId = row.SirketId;
            line.UpdatedAt = _clock.Now;

            // Cari bağı: Siber kartoteksid. Eşleşmeyen satır hata değil —
            // kasa/banka/gider hesabı satırlarının carisi yoktur.
            if (line.SiberAccountId is { } kartoteks &&
                accountBySiberId.TryGetValue(kartoteks, out var accountId))
            {
                line.AccountId = accountId;
                matched++;
            }
            else
            {
                line.AccountId = null;
            }

            if (++pending >= SaveBatchSize)
            {
                await _db.SaveChangesAsync(cancellationToken);
                pending = 0;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        var notes = new List<string>
        {
            $"Fiş satırı: {created} yeni, {updated} güncel, {matched} cariye bağlandı.",
        };

        if (orphan > 0)
        {
            notes.Add($"{orphan} fiş satırı, başlığı pencere dışında kaldığı için atlandı.");
            _logger.LogInformation(
                "Fiş satırı senkronunda {Orphan} satır başlıksız kaldı (pencere: {Since:yyyy-MM-dd}).",
                orphan, since);
        }

        return notes;
    }

    // ------------------------------------------------------------------
    // Fatura + satırları
    // ------------------------------------------------------------------
    public async Task<SiberImportSummary> SyncInvoicesAsync(
        bool full = false, CancellationToken cancellationToken = default)
    {
        if (!_siber.IsConfigured)
            return SiberImportSummary.Empty;

        var since = await WindowStartAsync(
            full, _db.FinanceInvoices.AnyAsync(cancellationToken));

        var headers = await _siber.GetInvoicesAsync(since, cancellationToken);
        if (headers.Count == 0)
            return SiberImportSummary.Empty;

        var existing = await _db.FinanceInvoices
            .Where(i => since == null || i.InvoiceDate >= since)
            .ToDictionaryAsync(i => i.SiberId, cancellationToken);

        var accountBySiberId = await AccountLookupAsync(cancellationToken);
        var transferRows = await _db.LoadTransfers.AsNoTracking()
            .Where(t => t.LoadTransferId != null)
            .Select(t => new { t.Id, t.LoadTransferId })
            .ToListAsync(cancellationToken);

        var transferBySiberId = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in transferRows)
            transferBySiberId[row.LoadTransferId!] = row.Id;

        int created = 0, updated = 0, linkedToLoad = 0;

        foreach (var row in headers)
        {
            if (!existing.TryGetValue(row.GelirGiderId, out var invoice))
            {
                invoice = new FinanceInvoice
                {
                    SiberId = row.GelirGiderId,
                    CreatedAt = _clock.Now,
                };
                _db.FinanceInvoices.Add(invoice);
                existing[row.GelirGiderId] = invoice;
                created++;
            }
            else
            {
                updated++;
            }

            invoice.Direction = Trim(row.Gc, 2);
            invoice.InvoiceSeries = Trim(row.FaturaSeriNo, 32);
            invoice.InvoiceNumber = Trim(row.FaturaNo, 64);
            invoice.InvoiceDate = row.FaturaTarihi;
            invoice.DueDate = row.VadeTarihi;
            invoice.SiberAccountId = EmptyGuidToNull(row.FirmaId);
            invoice.AccountName = Trim(row.FirmaAd, 255);
            invoice.CurrencyCode = Trim(row.DovizKod, 8);
            invoice.ExchangeRate = ToDecimal(row.DovizKur, 6);
            invoice.Amount = ToDecimal(row.Tutar);
            invoice.TaxAmount = ToDecimal(row.KdvTutar);
            invoice.TotalAmount = ToDecimal(row.ToplamTutar);
            invoice.AmountTl = ToDecimal(row.TutarTl);
            invoice.TaxAmountTl = ToDecimal(row.KdvTutarTl);
            invoice.TotalAmountTl = ToDecimal(row.ToplamTutarTl);
            invoice.Description = row.Aciklama;
            invoice.ModuleId = EmptyGuidToNull(row.ModulId);
            invoice.ModuleCode = Trim(row.ModulKod, 16);
            invoice.DocumentNumber = Trim(row.BelgeNo, 64);
            invoice.IsApproved = row.Onay == true;
            invoice.ApprovalDate = row.OnayTarih;
            invoice.SiberCompanyId = row.SirketId;
            invoice.SiberCreatedAt = row.KayitGirisTarih;
            invoice.SiberCreatedBy = Trim(row.KayitGiren, 128);
            invoice.UpdatedAt = _clock.Now;

            invoice.AccountId = invoice.SiberAccountId is { } firmaId &&
                accountBySiberId.TryGetValue(firmaId, out var accountId)
                    ? accountId
                    : null;

            // Yük bağı BAŞLIKTAN kurulur (modulid + modulkod). Satırdaki yukid
            // Siber'de hiç doldurulmamış, ona bakılmaz.
            invoice.LoadTransferId = null;
            if (invoice.ModuleId is { } moduleId && IsLoadModule(invoice.ModuleCode) &&
                transferBySiberId.TryGetValue(moduleId, out var transferId))
            {
                invoice.LoadTransferId = transferId;
                linkedToLoad++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        var lineNotes = await SyncInvoiceLinesAsync(since, existing, cancellationToken);

        return new SiberImportSummary(created, updated, [])
        {
            Notes = [$"{linkedToLoad} fatura yüke bağlandı.", .. lineNotes],
        };
    }

    private async Task<IReadOnlyList<string>> SyncInvoiceLinesAsync(
        DateTime? since,
        IReadOnlyDictionary<string, FinanceInvoice> invoices,
        CancellationToken cancellationToken)
    {
        var rows = await _siber.GetInvoiceLinesAsync(since, cancellationToken);
        if (rows.Count == 0)
            return [];

        var existing = await _db.FinanceInvoiceLines
            .Where(l => since == null || l.FinanceInvoice.InvoiceDate >= since)
            .ToDictionaryAsync(l => l.SiberId, cancellationToken);

        // Kalem adı tanım tablosundan gelir: Siber'in kalemyabanciad sütunu
        // 133.908 satırın yalnızca 39.849'unda dolu (yabancı ad alanı).
        // financial_items.siber_id BÜYÜK harfle saklanıyor, o yüzden anahtar
        // karşılaştırması harfe duyarsız.
        // MÜKERRER KİMLİK: financial_items içinde aynı siber_id'yi paylaşan 12
        // grup var (aynı kalem birden çok kez içe aktarılmış). Doğrudan sözlük
        // kurmak "same key has already been added" ile patlıyor; ilk ad alınır.
        var itemRows = await _db.FinancialItems.AsNoTracking()
            .Where(i => i.SiberId != null)
            .Select(i => new { i.SiberId, i.Name })
            .ToListAsync(cancellationToken);

        var itemNames = itemRows
            .GroupBy(i => i.SiberId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase);

        int created = 0, updated = 0, orphan = 0, named = 0;
        var pending = 0;

        foreach (var row in rows)
        {
            if (!invoices.TryGetValue(row.GelirGiderId, out var invoice))
            {
                orphan++;
                continue;
            }

            if (!existing.TryGetValue(row.GelirGiderDetayId, out var line))
            {
                line = new FinanceInvoiceLine
                {
                    SiberId = row.GelirGiderDetayId,
                    CreatedAt = _clock.Now,
                };
                _db.FinanceInvoiceLines.Add(line);
                existing[row.GelirGiderDetayId] = line;
                created++;
            }
            else
            {
                updated++;
            }

            line.FinanceInvoice = invoice;
            line.FinancialItemId = EmptyGuidToNull(row.KalemId);
            line.Quantity = ToDecimal(row.Miktar, 4);
            line.UnitPrice = ToDecimal(row.BirimFiyat, 4);
            line.CurrencyCode = Trim(row.DovizKod, 8);
            line.ExchangeRate = ToDecimal(row.DovizKur, 6);
            line.TaxRate = ToDecimal(row.KdvOran, 4);
            line.Amount = ToDecimal(row.Tutar);
            line.TaxAmount = ToDecimal(row.KdvTutar);
            line.AmountTl = ToDecimal(row.TutarTl);
            line.TaxAmountTl = ToDecimal(row.KdvTutarTl);
            line.Description = row.Aciklama;
            line.DocumentNumber = Trim(row.BelgeNo, 64);
            line.DocumentDate = row.BelgeTarih;
            line.UpdatedAt = _clock.Now;

            var name = line.FinancialItemId is { } itemId &&
                       itemNames.TryGetValue(itemId, out var itemName)
                ? itemName
                : null;

            line.FinancialItemName = Trim(name ?? row.KalemAd, 255);
            if (line.FinancialItemName is not null)
                named++;

            if (++pending >= SaveBatchSize)
            {
                await _db.SaveChangesAsync(cancellationToken);
                pending = 0;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        var notes = new List<string>
        {
            $"Fatura satırı: {created} yeni, {updated} güncel, {named} kalem adı çözüldü.",
        };

        if (orphan > 0)
            notes.Add($"{orphan} fatura satırı, başlığı pencere dışında kaldığı için atlandı.");

        return notes;
    }

    // ------------------------------------------------------------------
    // Tahsilat / ödeme
    // ------------------------------------------------------------------
    public async Task<SiberImportSummary> SyncPaymentsAsync(
        bool full = false, CancellationToken cancellationToken = default)
    {
        if (!_siber.IsConfigured)
            return SiberImportSummary.Empty;

        var since = await WindowStartAsync(
            full, _db.FinancePayments.AnyAsync(cancellationToken));

        var rows = await _siber.GetPaymentsAsync(since, cancellationToken);
        if (rows.Count == 0)
            return SiberImportSummary.Empty;

        var existing = await _db.FinancePayments
            .Where(p => since == null || p.ReceiptDate >= since)
            .ToDictionaryAsync(p => p.SiberId, cancellationToken);

        var accountBySiberId = await AccountLookupAsync(cancellationToken);

        int created = 0, updated = 0, withAccount = 0;
        var pending = 0;

        foreach (var row in rows)
        {
            if (!existing.TryGetValue(row.TahsilatOdemeId, out var payment))
            {
                payment = new FinancePayment
                {
                    SiberId = row.TahsilatOdemeId,
                    CreatedAt = _clock.Now,
                };
                _db.FinancePayments.Add(payment);
                existing[row.TahsilatOdemeId] = payment;
                created++;
            }
            else
            {
                updated++;
            }

            payment.ReceiptNumber = Trim(row.MakbuzNo, 64);
            payment.ReceiptDate = row.MakbuzTarih;
            payment.DueDate = row.VadeTarih;
            payment.TransactionType = row.IslemTur;
            payment.SiberDebitAccountId = EmptyGuidToNull(row.BorcId);
            payment.DebitName = Trim(row.BorcAd, 255);
            payment.DebitAccountCode = Trim(row.BorcHesapKod, 64);
            payment.SiberCreditAccountId = EmptyGuidToNull(row.AlacakId);
            payment.CreditName = Trim(row.AlacakAd, 255);
            payment.CreditAccountCode = Trim(row.AlacakHesapKod, 64);
            payment.CurrencyCode = Trim(row.DovizKod, 8);
            payment.ExchangeRate = ToDecimal(row.DovizKur, 6);
            payment.Amount = ToDecimal(row.Tutar);
            payment.AmountTl = ToDecimal(row.TutarTl);
            payment.Description = row.Aciklama;
            payment.ModuleId = EmptyGuidToNull(row.ModulId);
            payment.ModuleCode = Trim(row.ModulKod, 16);
            payment.SiberCompanyId = row.SirketId;
            payment.SiberCreatedAt = row.KayitGirisTarih;
            payment.SiberCreatedBy = Trim(row.KayitGiren, 128);
            payment.UpdatedAt = _clock.Now;

            // İki taraf da cari OLMAYABİLİR: karşı taraf kasa/banka hesabı
            // olabilir. 29.007 kaydın 12.371'inde borç, 6.423'ünde alacak
            // tarafı bir cariye bağlanıyor.
            payment.DebitAccountId = payment.SiberDebitAccountId is { } debitId &&
                accountBySiberId.TryGetValue(debitId, out var d) ? d : null;

            payment.CreditAccountId = payment.SiberCreditAccountId is { } creditId &&
                accountBySiberId.TryGetValue(creditId, out var c) ? c : null;

            if (payment.DebitAccountId is not null || payment.CreditAccountId is not null)
                withAccount++;

            if (++pending >= SaveBatchSize)
            {
                await _db.SaveChangesAsync(cancellationToken);
                pending = 0;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new SiberImportSummary(created, updated, [])
        {
            Notes = [$"{withAccount} tahsilat/ödeme en az bir tarafından cariye bağlandı."],
        };
    }

    // ------------------------------------------------------------------
    // Ortak yardımcılar
    // ------------------------------------------------------------------

    /// <summary>
    /// Yerelde kayıt varsa son <see cref="DefaultWindowMonths"/> ay, yoksa null
    /// (ilk turda tüm geçmiş bir kez alınır).
    /// </summary>
    private async Task<DateTime?> WindowStartAsync(bool full, Task<bool> hasRows)
    {
        if (full)
            return null;

        return await hasRows
            ? _clock.Now.AddMonths(-DefaultWindowMonths).Date
            : null;
    }

    /// <summary>
    /// Cari kimliği → yerel id. accounts.siber_id'nin 25 kaydı BÜYÜK, 7.407'si
    /// küçük harfli; bu yüzden sözlük harfe duyarsız.
    /// </summary>
    private async Task<Dictionary<string, long>> AccountLookupAsync(
        CancellationToken cancellationToken)
    {
        var rows = await _db.Accounts.AsNoTracking()
            .Where(a => a.SiberId != null)
            .Select(a => new { a.Id, a.SiberId })
            .ToListAsync(cancellationToken);

        var map = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
            map[row.SiberId!] = row.Id;

        return map;
    }

    /// <summary>Yük modülleri — iş türüne göre 0401-0404 (bkz. SiberArchiveWriter).</summary>
    private static bool IsLoadModule(string? modulKod) =>
        modulKod is "0401" or "0402" or "0403" or "0404";

    private static string? Trim(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    /// <summary>Siber boş GUID'i null yerine sıfır GUID olarak yazıyor.</summary>
    private static string? EmptyGuidToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) || value == "00000000-0000-0000-0000-000000000000"
            ? null
            : value;

    private static decimal? ToDecimal(double? value, int scale = 2)
    {
        if (value is not { } d || double.IsNaN(d) || double.IsInfinity(d))
            return null;

        // Siber tutarları float tutuyor; ölçeğe yuvarlanmadan decimal(18,x)
        // sütununa yazmak taşma hatası veriyor.
        return Math.Round((decimal)d, scale, MidpointRounding.AwayFromZero);
    }
}
