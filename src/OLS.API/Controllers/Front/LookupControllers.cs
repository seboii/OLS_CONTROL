using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OLS.Business.Services.Lookups;
using OLS.DataAccess.Entities;

namespace OLS.API.Controllers.Front;

// ---------------------------------------------------------------------------
// Referans/tanım modülleri.
//
// Tüm davranış LookupControllerBase içinde; burada yalnızca her modülün
// rotası, entity'si, yetki slug'ı ve sıralama yönü tanımlanıyor.
//
// Slug'lar ve sıralama yönleri olsold kaynak kodundan birebir alındı.
// Kaynakta bazı slug'lar kopyala-yapıştır sonucu paylaşılıyor — örneğin
// RomorkType / Instruction / LoadTransferType / TaxOffice hepsi
// "transport_type_management", CarType / CarStatus / CarOwner /
// ExpeditionStatus / ExpeditionType / EinvoicePrefix ise
// "case_type_management" slug'ını kullanıyor. Yetki davranışı değişmesin
// diye bu eşleşmeler aynen korundu.
// ---------------------------------------------------------------------------

[Route("api/v1/work_type")]
public sealed class WorkTypeController(ILookupService<WorkType> s) : LookupControllerBase<WorkType>(s)
{
    protected override string PermissionSlug => "work_type_management";
    protected override bool OrderAscending => true;
}

[Route("api/v1/status_type")]
public sealed class StatusTypeController(ILookupService<StatusType> s) : LookupControllerBase<StatusType>(s)
{
    protected override string PermissionSlug => "status_type_management";
}

[Route("api/v1/payment_type")]
public sealed class PaymentTypeController(ILookupService<PaymentType> s) : LookupControllerBase<PaymentType>(s)
{
    protected override string PermissionSlug => "payment_management";
}

[Route("api/v1/loading_type")]
public sealed class LoadingTypeController(ILookupService<LoadingType> s) : LookupControllerBase<LoadingType>(s)
{
    protected override string PermissionSlug => "loading_type_management";
}

[Route("api/v1/department")]
public sealed class DepartmentController(ILookupService<Department> s) : LookupControllerBase<Department>(s)
{
    // olsold'da bu modülde hiç yetki kontrolü yoktu. Tanımsız slug,
    // RoleHelper davranışına uygun olarak serbest bırakır.
    protected override string PermissionSlug => "department_management";
}

[Route("api/v1/product_type")]
public sealed class ProductTypeController(ILookupService<ProductType> s) : LookupControllerBase<ProductType>(s)
{
    protected override string PermissionSlug => "product_type_management";
}

[Route("api/v1/case_type")]
public sealed class CaseTypeController(ILookupService<CaseType> s) : LookupControllerBase<CaseType>(s)
{
    protected override string PermissionSlug => "case_type_management";
}

[Route("api/v1/item_type")]
public sealed class ItemTypeController(ILookupService<ItemType> s) : LookupControllerBase<ItemType>(s)
{
    protected override string PermissionSlug => "financial_item_type_management";
}

[Route("api/v1/financial_item")]
public sealed class FinancialItemController(ILookupService<FinancialItem> s) : LookupControllerBase<FinancialItem>(s)
{
    protected override string PermissionSlug => "financial_item_management";
}

[Route("api/v1/movement_type")]
public sealed class MovementTypeController(ILookupService<MovementType> s) : LookupControllerBase<MovementType>(s)
{
    protected override string PermissionSlug => "movement_type_management";
}

[Route("api/v1/transport_type")]
public sealed class TransportTypeController(ILookupService<TransportType> s) : LookupControllerBase<TransportType>(s)
{
    protected override string PermissionSlug => "transport_type_management";
}

[Route("api/v1/romork_type")]
public sealed class RomorkTypeController(ILookupService<RomorkType> s) : LookupControllerBase<RomorkType>(s)
{
    protected override string PermissionSlug => "transport_type_management";
    protected override bool OrderAscending => true;
}

[Route("api/v1/instruction")]
public sealed class InstructionController(ILookupService<Instruction> s) : LookupControllerBase<Instruction>(s)
{
    protected override string PermissionSlug => "transport_type_management";
    protected override bool OrderAscending => true;
}

[Route("api/v1/load_transfer_type")]
public sealed class LoadTransferTypeController(ILookupService<LoadTransferType> s)
    : LookupControllerBase<LoadTransferType>(s)
{
    protected override string PermissionSlug => "transport_type_management";
    protected override bool OrderAscending => true;
}

[Route("api/v1/load_status_type")]
public sealed class LoadStatusTypeController(ILookupService<LoadStatusType> s)
    : LookupControllerBase<LoadStatusType>(s)
{
    protected override string PermissionSlug => "payment_management";
}

[Route("api/v1/account_type")]
public sealed class AccountTypeController(ILookupService<AccountType> s) : LookupControllerBase<AccountType>(s)
{
    protected override string PermissionSlug => "account_type_management";
}

[Route("api/v1/car_type")]
public sealed class CarTypeController(ILookupService<CarType> s) : LookupControllerBase<CarType>(s)
{
    protected override string PermissionSlug => "case_type_management";
}

[Route("api/v1/car_status")]
public sealed class CarStatusController(ILookupService<CarStatusType> s) : LookupControllerBase<CarStatusType>(s)
{
    protected override string PermissionSlug => "case_type_management";
}

[Route("api/v1/car_owner")]
public sealed class CarOwnerController(ILookupService<CarOwner> s) : LookupControllerBase<CarOwner>(s)
{
    protected override string PermissionSlug => "case_type_management";
}

[Route("api/v1/tax_office")]
public sealed class TaxOfficeController(ILookupService<TaxOffice> s) : LookupControllerBase<TaxOffice>(s)
{
    protected override string PermissionSlug => "transport_type_management";
}

[Route("api/v1/expedition_status")]
public sealed class ExpeditionStatusController(ILookupService<ExpeditionStatus> s)
    : LookupControllerBase<ExpeditionStatus>(s)
{
    protected override string PermissionSlug => "case_type_management";
}

[Route("api/v1/expedition_type")]
public sealed class ExpeditionTypeController(ILookupService<ExpeditionType> s)
    : LookupControllerBase<ExpeditionType>(s)
{
    protected override string PermissionSlug => "case_type_management";
}

[Route("api/v1/load_transfer_deliver_method")]
public sealed class LoadTransferDeliveryMethodController(ILookupService<LoadTransferDeliveryMethod> s)
    : LookupControllerBase<LoadTransferDeliveryMethod>(s)
{
    protected override string PermissionSlug => "loading_type_management";
}

[Route("api/v1/invoice_status")]
public sealed class InvoiceStatusController(ILookupService<InvoiceStatus> s) : LookupControllerBase<InvoiceStatus>(s)
{
    protected override string PermissionSlug => "invoice_type_management";
    protected override bool OrderAscending => true;
}

[Route("api/v1/invoice_type")]
public sealed class InvoiceTypeController(ILookupService<InvoiceType> s) : LookupControllerBase<InvoiceType>(s)
{
    protected override string PermissionSlug => "invoice_type_management";
    protected override bool OrderAscending => true;
}

// EinvoicePrefix (Uyumsoft e-fatura ön eki) kapsam dışı bırakıldı — hiçbir
// kapsam-içi modül referans vermiyor (bkz. docs/API-PARITE-MATRISI.md).

[Route("api/v1/currency")]
public sealed class CurrencyController(ILookupService<Currency> s) : LookupControllerBase<Currency>(s)
{
    // olsnew'deki bespoke CurrencyController (TCMB döviz kuru entegrasyonu,
    // ExchangeRateFetcher) kapsam dışı bırakıldı — Teklif/Yük/Fatura formları
    // Currency'yi yalnızca salt-okunur dropdown kaynağı olarak kullanıyor.
    protected override string PermissionSlug => "currency_management";
    protected override bool OrderAscending => true;
}

/// <summary>
/// Varış noktası — generic tanım modüllerinden TEK farkı rota biçimi:
/// güncelleme ve silme id'yi <b>yoldan</b> alır (<c>PUT /destination/{id}</c>,
/// <c>DELETE /destination/{id}</c>), gövdeden değil. Arayüz de böyle çağırıyor
/// (<c>data_store.js</c>), kaynak rotası da böyle.
/// </summary>
[Route("api/v1/destination")]
public sealed class DestinationController(ILookupService<Destination> s) : LookupControllerBase<Destination>(s)
{
    protected override string PermissionSlug => "case_type_management";

    [HttpPut("{id:long}")]
    public Task<IActionResult> UpdateByRoute(
        long id, [FromBody] Dictionary<string, JsonElement> body, CancellationToken cancellationToken)
    {
        // Gövdede id olmasa da temel sınıfın beklediği biçime tamamlanır.
        body["id"] = JsonSerializer.SerializeToElement(id);

        return UpdateViaPut(body, cancellationToken);
    }

    [HttpDelete("{id:long}")]
    public Task<IActionResult> DeleteByRoute(long id, CancellationToken cancellationToken) =>
        Delete(new DeletionRequest { DeletionId = [id] }, cancellationToken);
}
