using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

/// <summary>
/// Fatura — Siber <c>sfy_gelirgider</c> aynası.
///
/// Siber'de gelir ve gider faturaları TEK tabloda tutulur; ayrım
/// <see cref="Direction"/> ("C" gelir / "G" gider) ile yapılır
/// (30.589 gelir, 7.836 gider).
///
/// YÜK BAĞI BAŞLIKTADIR: <see cref="ModuleId"/> + <see cref="ModuleCode"/>
/// çiftiyle kurulur (arşivdeki desenin aynısı; yük 0401-0404, sefer 0405).
/// Satırdaki <c>yukid</c> sütunu Siber'de HİÇ doldurulmamıştır — 133.908
/// satırın tamamı boş — bu yüzden ona güvenilmez.
/// </summary>
public partial class FinanceInvoice
{
    public long Id { get; set; }

    public string SiberId { get; set; } = null!;

    /// <summary>"C" gelir, "G" gider.</summary>
    public string? Direction { get; set; }

    public string? InvoiceSeries { get; set; }

    public string? InvoiceNumber { get; set; }

    public DateTime? InvoiceDate { get; set; }

    /// <summary>Vade — Siber'de 38.425 kaydın tamamında dolu, yaşlandırma buna dayanır.</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>Yerel cari kaydı; Siber tarafında karşılığı olmayan fatura için null.</summary>
    public long? AccountId { get; set; }

    public string? SiberAccountId { get; set; }

    /// <summary>Siber'de saklanan cari adı — cari eşleşmese de fatura okunabilsin diye.</summary>
    public string? AccountName { get; set; }

    public string? CurrencyCode { get; set; }

    public decimal? ExchangeRate { get; set; }

    public decimal? Amount { get; set; }

    public decimal? TaxAmount { get; set; }

    public decimal? TotalAmount { get; set; }

    public decimal? AmountTl { get; set; }

    public decimal? TaxAmountTl { get; set; }

    public decimal? TotalAmountTl { get; set; }

    public string? Description { get; set; }

    /// <summary>Bağlı operasyon kaydının Siber kimliği (yükid / seferid).</summary>
    public string? ModuleId { get; set; }

    /// <summary>Modül kodu — yük iş türüne göre 0401-0404, sefer 0405.</summary>
    public string? ModuleCode { get; set; }

    /// <summary>Çözümlenmiş yerel yük aktarımı; yalnızca modül bağı kurulabildiğinde dolar.</summary>
    public long? LoadTransferId { get; set; }

    public string? DocumentNumber { get; set; }

    public bool IsApproved { get; set; }

    public DateTime? ApprovalDate { get; set; }

    public string? SiberCompanyId { get; set; }

    public DateTime? SiberCreatedAt { get; set; }

    public string? SiberCreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account? Account { get; set; }

    public virtual LoadTransfer? LoadTransfer { get; set; }

    public virtual ICollection<FinanceInvoiceLine> Lines { get; set; } = new List<FinanceInvoiceLine>();
}
