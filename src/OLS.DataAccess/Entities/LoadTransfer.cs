using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class LoadTransfer
{
    public long Id { get; set; }

    /// <summary>
    /// Yükün ait olduğu Siber şirketi (skn_yuk/skn_pozisyon.sirketid).
    /// Siber'de iki şirket var: AVRORA ve OLS. Görünürlük ayrımı bu sütuna
    /// dayanıyor — bkz. CompanyScope.
    /// </summary>
    public string? SiberCompanyId { get; set; }

    public string? LoadTransferId { get; set; }

    public string? LoadNumber { get; set; }

    public int? WorkType { get; set; }

    public string? ConnectedLoadNumber { get; set; }

    public int? LoadStatusId { get; set; }

    public int? LoadTypeId { get; set; }

    public int? CustomerId { get; set; }

    public int? SenderId { get; set; }

    public int? ReceiverId { get; set; }

    public int? PaymentTypeId { get; set; }

    public int? InTruck { get; set; }

    public int? InTail { get; set; }

    public int? CmrWaiting { get; set; }

    public int? FcrWaiting { get; set; }

    public int? InstructionId { get; set; }

    public int? RomorkTypeId { get; set; }

    public decimal? TotalGrossWeight { get; set; }

    public decimal? TotalVolume { get; set; }

    public decimal? TotalLademeter { get; set; }

    public decimal? WeightFee { get; set; }

    public int? CustomerRepresentativeName { get; set; }

    public int? DepartmentId { get; set; }

    public int? OperationDepartmentId { get; set; }

    public string? LoadNumberWorkType { get; set; }

    public string? ConnectedLoadNumberWorkType { get; set; }

    public decimal? TotalCap { get; set; }

    public int? SecondCustomerRepresentativeName { get; set; }

    public decimal? TotalLademeterM3 { get; set; }

    public decimal? CarHeight { get; set; }

    public int? LoadTransferTypeId { get; set; }

    public int? DeliveryMethodId { get; set; }

    public string? DepartureCountryId { get; set; }

    public string? TargetCountryId { get; set; }

    /// <summary>
    /// Transit ülke. YALNIZCA YEREL: Siber'in <c>skn_yuk</c> tablosunda transit
    /// ülke için sütun YOKTUR (400 sütunun tamamı tarandı — yalnızca yükleme,
    /// boşaltma ve menşe ülkesi var). Alan formda toplanıyordu ama hiçbir yere
    /// yazılmadığı için kaydetme anında sessizce kayboluyordu; en azından yerel
    /// kayıtta ve ekranda korunsun diye burada tutuluyor.
    /// </summary>
    public string? TransitCountryId { get; set; }

    public string? LoadingContinent { get; set; }

    public string? UnloadingContinent { get; set; }

    public int? UsercodeWithNotification { get; set; }

    public int? SalesRepCode { get; set; }

    public int? WayOfWorking { get; set; }

    public int? FrontTransportationByUs { get; set; }

    public int? FinalTransportationByUs { get; set; }

    public DateOnly? InstructionArrivalDate { get; set; }

    public DateOnly? RequestArrivalDate { get; set; }

    public DateOnly? ReadinessDate { get; set; }

    public DateOnly? DateOfReceiptCustomer { get; set; }

    public string? SiberId { get; set; }

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

    public virtual ICollection<LoadTransferInvoiceMap> LoadTransferInvoiceMaps { get; set; } = new List<LoadTransferInvoiceMap>();

    public virtual ICollection<LoadTransferMovement> LoadTransferMovements { get; set; } = new List<LoadTransferMovement>();
}
