using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

/// <summary>
/// Tahsilat / ödeme — Siber <c>sfy_tahsilatodeme</c> aynası.
///
/// Kayıt ÇİFT TARAFLIDIR: bir borç tarafı, bir alacak tarafı. Taraflar cari
/// olabildiği gibi kasa/banka hesabı da olabilir; 29.007 kaydın 12.371'inde
/// borç tarafı, 6.423'ünde alacak tarafı bir cariye bağlanıyor. Bu yüzden
/// taraf alanları ayrı ayrı null olabilir ve Siber'in denormalize ettiği
/// adlar (<see cref="DebitName"/>/<see cref="CreditName"/>) her kayıtta dolu.
///
/// ÇEK/SENET ALANLARI TAŞINMADI: Siber'de <c>ceksenetno</c> ve <c>cekbanka</c>
/// 29.007 kaydın HİÇBİRİNDE dolu değil — kurum bu takibi kullanmıyor.
/// </summary>
public partial class FinancePayment
{
    public long Id { get; set; }

    public string SiberId { get; set; } = null!;

    public string? ReceiptNumber { get; set; }

    public DateTime? ReceiptDate { get; set; }

    public DateTime? DueDate { get; set; }

    /// <summary>Siber <c>islemtur</c> — işlem alt türü (51, 52, 5, 6 baskın).</summary>
    public int? TransactionType { get; set; }

    public long? DebitAccountId { get; set; }

    public string? SiberDebitAccountId { get; set; }

    public string? DebitName { get; set; }

    public string? DebitAccountCode { get; set; }

    public long? CreditAccountId { get; set; }

    public string? SiberCreditAccountId { get; set; }

    public string? CreditName { get; set; }

    public string? CreditAccountCode { get; set; }

    public string? CurrencyCode { get; set; }

    public decimal? ExchangeRate { get; set; }

    public decimal? Amount { get; set; }

    public decimal? AmountTl { get; set; }

    public string? Description { get; set; }

    public string? ModuleId { get; set; }

    public string? ModuleCode { get; set; }

    public string? SiberCompanyId { get; set; }

    public DateTime? SiberCreatedAt { get; set; }

    public string? SiberCreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account? DebitAccount { get; set; }

    public virtual Account? CreditAccount { get; set; }
}
