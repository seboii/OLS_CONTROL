using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class Load
{
    public long Id { get; set; }

    public string? ReservationNumber { get; set; }

    public string? LoadNumber { get; set; }

    public int? WorkTypeId { get; set; }

    public int? LoadingTypeId { get; set; }

    public int? PaymentTypeId { get; set; }

    public int? StatusTypeId { get; set; }

    public DateOnly? OfferDate { get; set; }

    public DateOnly? OfferValidityDate { get; set; }

    public DateOnly? MarketingNotificationDate { get; set; }

    /// <summary>
    /// Teklifin "Olumlu"ya çekildiği gün. Siber'de <c>skn_rezervasyon.onaytarih</c>
    /// sütununa karşılık gelir.
    ///
    /// <see cref="OfferDate"/> ile KARIŞTIRILMAMALI: o teklifin VERİLDİĞİ tarihtir
    /// ve Olumlu'ya geçince değişmez — ikisinin farkı "teklif kaç günde
    /// onaylandı" raporunu mümkün kılar. Durum Olumlu'dan çıkarsa temizlenir.
    /// </summary>
    public DateOnly? ApprovalDate { get; set; }

    public int? CustomerId { get; set; }

    public int? SenderId { get; set; }

    public int? ReceiverId { get; set; }

    public int? InstructionId { get; set; }

    public int? RomorkTypeId { get; set; }

    public int? AgentId { get; set; }

    public int? LoadTransferTypeId { get; set; }

    public int? CompanyPayFreightId { get; set; }

    public string? PayerCompany { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// Teklif "Olumsuz" işaretlendiğinde kullanıcının girdiği gerekçe. Raporlamada
    /// tekliflerin neden kaybedildiğini görebilmek için ayrı tutulur —
    /// <see cref="Description"/> genel not alanıdır, bu ikisi karıştırılmamalı.
    /// Yalnızca Olumsuz (status_type_id = 1) durumunda anlamlıdır.
    /// </summary>
    public string? RejectionReason { get; set; }

    public Guid? DepartureCountryId { get; set; }

    public Guid? TransitCountryId { get; set; }

    public Guid? TargetCountryId { get; set; }

    public int? DepartmentId { get; set; }

    public int FrontTransportationByUs { get; set; }

    public int FinalTransportationByUs { get; set; }

    /// <summary>
    /// Teklifin ait olduğu Siber şirketi (skn_rezervasyon.sirketid).
    /// Yük ve seferdeki ile aynı görünürlük ayrımı — bkz. CompanyScope.
    /// </summary>
    public string? SiberCompanyId { get; set; }

    public string? SiberId { get; set; }

    public string? MailId { get; set; }

    public int TransferToSiber { get; set; }

    public int WayOfWorking { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // ------------------------------------------------------------------
    // Siber izleri — kaydı kim açtı, kim son dokundu, Siber'den silindi mi.
    //
    // Siber bu bilgiyi KULLANICI KODU olarak tutuyor (insuser/kayitgiren,
    // upduser). Kod hem ham hâliyle hem çözümlenmiş kullanıcı kimliğiyle
    // saklanıyor: ayrılmış personelin kodu yerel users tablosunda karşılık
    // bulmuyor (91 koddan 3'ü) ve o durumda ekranda hiç olmazsa kod görünsün.
    // ------------------------------------------------------------------

    /// <summary>Kaydı Siber'de açan kullanıcının kodu.</summary>
    public string? SiberCreatedBy { get; set; }

    public long? SiberCreatedByUserId { get; set; }

    /// <summary>Siber'in kendi kayıt tarihi (yerel CreatedAt'ten farklı).</summary>
    public DateTime? SiberCreatedAt { get; set; }

    /// <summary>Kayda Siber'de EN SON dokunan kullanıcının kodu.</summary>
    public string? SiberUpdatedBy { get; set; }

    public long? SiberUpdatedByUserId { get; set; }

    public DateTime? SiberUpdatedAt { get; set; }

    /// <summary>
    /// Kayıt Siber'de artık yoksa, silindiğinin FARK EDİLDİĞİ an. Kayıt
    /// yerelden SİLİNMEZ: geçmiş, bağlı finans kayıtları ve denetim izi
    /// korunmalı. Kayıt Siber'de yeniden görünürse bu alan temizlenir.
    /// </summary>
    public DateTime? SiberDeletedAt { get; set; }

    /// <summary>Kaydı Siber'de SİLEN kullanıcının kodu (sbr_log'dan).</summary>
    public string? SiberDeletedBy { get; set; }

    public long? SiberDeletedByUserId { get; set; }

    /// <summary>
    /// Siber'deki GERÇEK silme anı. <see cref="SiberDeletedAt"/> ise bizim
    /// fark ettiğimiz an — ikisi farklı şeydir ve karıştırılmamalı.
    /// Silme günlüğü bulunamazsa null kalır.
    /// </summary>
    public DateTime? SiberDeletedOn { get; set; }

    public virtual User? SiberCreatedByUser { get; set; }

    public virtual User? SiberUpdatedByUser { get; set; }

    public virtual User? SiberDeletedByUser { get; set; }

    public virtual ICollection<LoadContent> LoadContents { get; set; } = new List<LoadContent>();

    public virtual ICollection<LoadFinancialItem> LoadFinancialItems { get; set; } = new List<LoadFinancialItem>();

    public virtual ICollection<LoadMovement> LoadMovements { get; set; } = new List<LoadMovement>();

    public virtual ICollection<LoadTransferMovement> LoadTransferMovements { get; set; } = new List<LoadTransferMovement>();
}
