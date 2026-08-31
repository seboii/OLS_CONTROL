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

    public virtual ICollection<LoadTransferInvoiceMap> LoadTransferInvoiceMaps { get; set; } = new List<LoadTransferInvoiceMap>();

    public virtual ICollection<LoadTransferMovement> LoadTransferMovements { get; set; } = new List<LoadTransferMovement>();
}
