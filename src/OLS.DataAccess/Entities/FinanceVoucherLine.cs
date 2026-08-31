using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

/// <summary>
/// Fiş satırı — Siber <c>sfy_fisdetay</c> aynası. CARİ EKSTRENİN KAYNAĞIDIR.
///
/// Cari bağı <see cref="AccountId"/> (Siber <c>kartoteksid</c>) üzerinden
/// kurulur — 214.954 satırın 55.903'ü bir cariye bağlanıyor. Siber'de
/// <c>sbr_firma.muhasebekod</c> 7.429 firmanın HİÇBİRİNDE dolu değil, bu
/// yüzden cari ↔ hesap kodu eşlemesi hesap kodundan KURULAMAZ; tek güvenilir
/// yol kartoteksid'dir.
///
/// Kaynak belge <see cref="SourceId"/> (Siber <c>entegreid</c>) ile bulunur:
/// 156.499 satır bir faturaya, 57.857 satır bir tahsilat/ödemeye iz sürüyor.
/// </summary>
public partial class FinanceVoucherLine
{
    public long Id { get; set; }

    public string SiberId { get; set; } = null!;

    public long FinanceVoucherId { get; set; }

    /// <summary>Hesap kodu — hesap planına METİN ile eşleşir.</summary>
    public string AccountCode { get; set; } = null!;

    public decimal? Debit { get; set; }

    public decimal? Credit { get; set; }

    /// <summary>Döviz cinsinden borç; <see cref="Debit"/> yerel para karşılığıdır.</summary>
    public decimal? DebitFx { get; set; }

    public decimal? CreditFx { get; set; }

    public string? CurrencyCode { get; set; }

    public decimal? ExchangeRate { get; set; }

    public string? Description { get; set; }

    /// <summary>Yerel cari kaydı (Siber <c>kartoteksid</c> çözümlenmiş hâli).</summary>
    public long? AccountId { get; set; }

    public string? SiberAccountId { get; set; }

    /// <summary>Kaynak belgenin Siber kimliği — fatura ya da tahsilat/ödeme.</summary>
    public string? SourceId { get; set; }

    public string? DocumentNumber { get; set; }

    public DateTime? DocumentDate { get; set; }

    public DateTime? DueDate { get; set; }

    public long? LineNumber { get; set; }

    public string? SiberCompanyId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual FinanceVoucher FinanceVoucher { get; set; } = null!;

    public virtual Account? Account { get; set; }
}
