using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class Account
{
    public long Id { get; set; }

    public string? Name { get; set; }

    public string? TaxNumber { get; set; }

    public string? TaxOffice { get; set; }

    public string? AccountingCode { get; set; }

    public int? InvoiceType { get; set; }

    public Guid? CountryId { get; set; }

    public Guid? CityId { get; set; }

    public Guid? DistrictId { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public Guid? PhoneCountryId { get; set; }

    public string? Avatar { get; set; }

    public string? Email { get; set; }

    public string? ContactPerson { get; set; }

    public string? IndividualPersonal { get; set; }

    public int Discount { get; set; }

    public string? SiberId { get; set; }

    public string? ContactLanguage { get; set; }

    /// <summary>Siber sbr_firma.aktif — pasif (false) cariler varsayılan listede gösterilmez.</summary>
    public bool IsActive { get; set; } = true;

    // ---------------------------------------------------------------------
    // PROGRAM DIŞI SİLME İZİ
    //
    // Cari Siber ekranından silinebiliyor ve bu kontrol olmadan yerelde
    // sonsuza kadar canlı görünüyordu. Canlıda ölçüldü: üç firma (Logista
    // Global, CK Boğaziçi, DLS Logistic) SERKANK ve ASLIY tarafından
    // silinmişti, listede duruyordu ve teklifsiz yük açarken FK hatası
    // veriyordu. Yük/teklif/seferde bu iz vardı, caride yoktu.
    //
    // Kayıt SİLİNMEZ, damgalanır: bağlı finans kayıtları, ilgili kişiler ve
    // denetim izi korunmalı. Cari Siber'de yeniden görünürse damga temizlenir.
    // ---------------------------------------------------------------------

    /// <summary>Bizim silinmiş olduğunu FARK ETTİĞİMİZ an.</summary>
    public DateTime? SiberDeletedAt { get; set; }

    /// <summary>Silen kullanıcının Siber kodu (sbr_log.kullanici).</summary>
    public string? SiberDeletedBy { get; set; }

    /// <summary>Siber kodundan çözülebilen yerel kullanıcı.</summary>
    public long? SiberDeletedByUserId { get; set; }

    /// <summary>
    /// Siber'deki GERÇEK silme anı (sbr_log.tarih). <see cref="SiberDeletedAt"/>
    /// ile KARIŞTIRILMAMALI — o bizim fark ettiğimiz an.
    /// </summary>
    public DateTime? SiberDeletedOn { get; set; }

    public virtual User? SiberDeletedByUser { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
