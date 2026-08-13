import { useEffect, useState } from "react";
import { FileText, Package, Plus, Trash2, Truck, Upload, File as FileIcon, X } from "lucide-react";
import { api, ApiError, type DataMessage, type Paginated } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useDebouncedValue, useLookupOptions } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { ModulePage } from "@/components/ui/ModulePage";
import { DataTable, EmptyState, Pagination, RowActions, type Column } from "@/components/ui/DataTable";
import { Drawer } from "@/components/ui/Overlay";
import { Badge, Btn, FormField, SelectInput, Tabs, TextInput, TextareaInput } from "@/components/ui/primitives";
import { AccountPicker, type AccountOption } from "@/components/shared/AccountPicker";
import { UserPicker, type UserOption } from "@/components/shared/UserPicker";

interface NamedRef {
  id: number;
  name: string | null;
}

interface LoadItem {
  id: number;
  reservation_number: string | null;
  load_number: string | null;
  offer_date: string | null;
  status_type_id: number | null;
  work_type_id: NamedRef | null;
  customer_id: AccountOption | null;
  load_content_count: number;
  siber_id: string | null;
}

interface LoadContentDetail {
  id: number;
  quantity: number | null;
  gross_weight: number | null;
  net_weight: number | null;
  volume: number | null;
  lademeter: number | null;
  width: number | null;
  length: number | null;
  height: number | null;
  stackable: number | null;
  product_type_id: NamedRef | null;
  case_type_id: NamedRef | null;
}

interface LoadFinancialItemDetail {
  id: number;
  net_price: number | null;
  total_price: number | null;
  quantity: number | null;
  description: string | null;
  buysell: number | null;
  order: number | null;
  item: number | null;
  currency: { id: number; name: string | null } | null;
  account_id: AccountOption | null;
  transport_type_id: NamedRef | null;
}

interface LoadFileDetail {
  id: number;
  file: string | null;
  org_name: string | null;
}

interface LoadDetail {
  id: number;
  reservation_number: string | null;
  load_number: string | null;
  offer_date: string | null;
  offer_validity_date: string | null;
  marketing_notification_date: string | null;
  description: string | null;
  payer_company: string | null;
  work_type_id: NamedRef | null;
  loading_type_id: NamedRef | null;
  payment_type_id: NamedRef | null;
  status_type_id: NamedRef | null;
  load_transfer_type_id: NamedRef | null;
  instruction_id: NamedRef | null;
  romork_type_id: NamedRef | null;
  department_id: NamedRef | null;
  customer_id: AccountOption | null;
  sender_id: AccountOption | null;
  receiver_id: AccountOption | null;
  agent_id: AccountOption | null;
  company_pay_freight_id: AccountOption | null;
  departure_country_id: { id: string; name: string | null } | null;
  transit_country_id: { id: string; name: string | null } | null;
  target_country_id: { id: string; name: string | null } | null;
  load_content: LoadContentDetail[];
  load_financial_item: LoadFinancialItemDetail[];
  load_file: LoadFileDetail[];
  load_charge_person: LoadChargePersonDetail[];
  way_of_working: number | null;
}

interface LoadChargePersonDetail {
  user_id: UserOption | null;
  user_type: number | null;
}

type ContentRow = {
  product_type_id: string; case_type_id: string; quantity: string;
  gross_weight: string; net_weight: string; volume: string; lademeter: string;
  width: string; height: string; length: string; stackable: string;
};

type FinancialItemRow = {
  item: string; transport_type_id: string; account: AccountOption | null;
  description: string; order: string; buysell: string; currency: string;
  net_price: string; total_price: string; quantity: string;
};

const EMPTY_CONTENT_ROW: ContentRow = {
  product_type_id: "", case_type_id: "", quantity: "1", gross_weight: "",
  net_weight: "", volume: "", lademeter: "", width: "", height: "", length: "", stackable: "1",
};

const EMPTY_FINANCIAL_ROW: FinancialItemRow = {
  item: "", transport_type_id: "", account: null, description: "", order: "1",
  buysell: "1", currency: "", net_price: "", total_price: "", quantity: "1",
};

function useStatusTypeMap() {
  const { options } = useLookupOptions("/api/v1/status_type");
  const map: Record<number, string> = {};
  options.forEach((o) => (map[Number(o.id)] = o.name));
  return map;
}

const PER_PAGE = 8;
const TABS = ["Genel Bilgiler", "Taraflar", "Güzergah", "Görevliler", "Mali Kalemler", "Dosyalar"];
const WAY_OF_WORKING_OPTIONS = [
  { value: "0", label: "Spot" },
  { value: "1", label: "Yıllık" },
];

export function QuotesPage() {
  const { can } = useAuth();
  const { addToast } = useToast();
  const canCreate = can("load_management", "create");
  const canUpdate = can("load_management", "update");
  const canDelete = can("load_management", "delete");
  const statusMap = useStatusTypeMap();

  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);
  const [page, setPage] = useState(1);
  const [rows, setRows] = useState<LoadItem[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);

  const [drawerOpen, setDrawerOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [tab, setTab] = useState(TABS[0]);
  const [saving, setSaving] = useState(false);
  const [detailLoading, setDetailLoading] = useState(false);
  const [errors, setErrors] = useState<Record<string, string[]>>({});

  const [form, setForm] = useState({
    work_type_id: "", loading_type_id: "", payment_type_id: "", status_type_id: "",
    load_transfer_type_id: "", instruction_id: "", romork_type_id: "", department_id: "",
    offer_date: new Date().toISOString().slice(0, 10), offer_validity_date: "",
    marketing_notification_date: new Date().toISOString().slice(0, 10),
    payer_company: "", description: "", way_of_working: "0",
  });
  const [customer, setCustomer] = useState<AccountOption | null>(null);
  const [sender, setSender] = useState<AccountOption | null>(null);
  const [receiver, setReceiver] = useState<AccountOption | null>(null);
  const [agent, setAgent] = useState<AccountOption | null>(null);
  const [companyPayFreight, setCompanyPayFreight] = useState<AccountOption | null>(null);
  const [route, setRoute] = useState({ departure_country_id: "", transit_country_id: "", target_country_id: "" });
  // olsold "Görevliler" sekmesi: Operasyon Yetkilisi tekil, Satış Temsilcisi çoğul seçim.
  const [operationOfficer, setOperationOfficer] = useState<UserOption | null>(null);
  const [salesReps, setSalesReps] = useState<UserOption[]>([]);

  const [content, setContent] = useState<ContentRow[]>([{ ...EMPTY_CONTENT_ROW }]);
  const [financialItems, setFinancialItems] = useState<FinancialItemRow[]>([]);
  const [existingFiles, setExistingFiles] = useState<LoadFileDetail[]>([]);
  const [removedFileIds, setRemovedFileIds] = useState<number[]>([]);
  const [newFiles, setNewFiles] = useState<File[]>([]);

  const { options: workTypes } = useLookupOptions("/api/v1/work_type");
  const { options: loadingTypes } = useLookupOptions("/api/v1/loading_type");
  const { options: paymentTypes } = useLookupOptions("/api/v1/payment_type");
  const { options: statusTypes } = useLookupOptions("/api/v1/status_type");
  const { options: departments } = useLookupOptions("/api/v1/department");
  const { options: instructions } = useLookupOptions("/api/v1/instruction");
  const { options: romorkTypes } = useLookupOptions("/api/v1/romork_type");
  const { options: loadTransferTypes } = useLookupOptions("/api/v1/load_transfer_type");
  const { options: productTypes } = useLookupOptions("/api/v1/product_type");
  const { options: caseTypes } = useLookupOptions("/api/v1/case_type");
  const { options: itemTypes } = useLookupOptions("/api/v1/item_type");
  const { options: transportTypes } = useLookupOptions("/api/v1/transport_type");
  const { options: currencies } = useLookupOptions("/api/v1/currency");
  const { options: countries } = useLookupOptions("/api/v1/country");

  function opts(list: { id: string | number; name: string }[]) {
    return [{ value: "", label: "Seçiniz" }, ...list.map((t) => ({ value: String(t.id), label: t.name }))];
  }

  function load() {
    setLoading(true);
    api
      .get<DataMessage<Paginated<LoadItem>>>("/api/v1/load", { search: debouncedSearch || undefined, per_page: PER_PAGE, page })
      .then((res) => {
        setRows(res.data.data);
        setTotal(res.data.total);
      })
      .catch(() => addToast("Teklif listesi yüklenemedi", "error"))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch, page]);

  function resetForm() {
    setForm({
      work_type_id: "", loading_type_id: "", payment_type_id: "", status_type_id: "",
      load_transfer_type_id: "", instruction_id: "", romork_type_id: "", department_id: "",
      offer_date: new Date().toISOString().slice(0, 10), offer_validity_date: "",
      marketing_notification_date: new Date().toISOString().slice(0, 10),
      payer_company: "", description: "", way_of_working: "0",
    });
    setCustomer(null);
    setSender(null);
    setReceiver(null);
    setAgent(null);
    setCompanyPayFreight(null);
    setRoute({ departure_country_id: "", transit_country_id: "", target_country_id: "" });
    setContent([{ ...EMPTY_CONTENT_ROW }]);
    setFinancialItems([]);
    setExistingFiles([]);
    setRemovedFileIds([]);
    setNewFiles([]);
    setOperationOfficer(null);
    setSalesReps([]);
    setErrors({});
  }

  function openNew() {
    resetForm();
    setEditingId(null);
    setTab(TABS[0]);
    setDrawerOpen(true);
  }

  async function openEdit(id: number) {
    resetForm();
    setEditingId(id);
    setTab(TABS[0]);
    setDrawerOpen(true);
    setDetailLoading(true);
    try {
      const res = await api.get<DataMessage<LoadDetail>>(`/api/v1/load/${id}`);
      const d = res.data;
      setForm({
        work_type_id: d.work_type_id ? String(d.work_type_id.id) : "",
        loading_type_id: d.loading_type_id ? String(d.loading_type_id.id) : "",
        payment_type_id: d.payment_type_id ? String(d.payment_type_id.id) : "",
        status_type_id: d.status_type_id ? String(d.status_type_id.id) : "",
        load_transfer_type_id: d.load_transfer_type_id ? String(d.load_transfer_type_id.id) : "",
        instruction_id: d.instruction_id ? String(d.instruction_id.id) : "",
        romork_type_id: d.romork_type_id ? String(d.romork_type_id.id) : "",
        department_id: d.department_id ? String(d.department_id.id) : "",
        offer_date: d.offer_date ?? "",
        offer_validity_date: d.offer_validity_date ?? "",
        marketing_notification_date: d.marketing_notification_date ?? "",
        payer_company: d.payer_company ?? "",
        description: d.description ?? "",
        way_of_working: d.way_of_working != null ? String(d.way_of_working) : "0",
      });
      setOperationOfficer(d.load_charge_person.find((p) => p.user_type === 1)?.user_id ?? null);
      setSalesReps(
        d.load_charge_person.filter((p) => p.user_type === 2 && p.user_id).map((p) => p.user_id!),
      );
      setCustomer(d.customer_id);
      setSender(d.sender_id);
      setReceiver(d.receiver_id);
      setAgent(d.agent_id);
      setCompanyPayFreight(d.company_pay_freight_id);
      setRoute({
        departure_country_id: d.departure_country_id?.id ?? "",
        transit_country_id: d.transit_country_id?.id ?? "",
        target_country_id: d.target_country_id?.id ?? "",
      });
      setContent(
        d.load_content.length > 0
          ? d.load_content.map((c) => ({
              product_type_id: c.product_type_id ? String(c.product_type_id.id) : "",
              case_type_id: c.case_type_id ? String(c.case_type_id.id) : "",
              quantity: c.quantity != null ? String(c.quantity) : "",
              gross_weight: c.gross_weight != null ? String(c.gross_weight) : "",
              net_weight: c.net_weight != null ? String(c.net_weight) : "",
              volume: c.volume != null ? String(c.volume) : "",
              lademeter: c.lademeter != null ? String(c.lademeter) : "",
              width: c.width != null ? String(c.width) : "",
              height: c.height != null ? String(c.height) : "",
              length: c.length != null ? String(c.length) : "",
              stackable: c.stackable != null ? String(c.stackable) : "1",
            }))
          : [{ ...EMPTY_CONTENT_ROW }],
      );
      setFinancialItems(
        d.load_financial_item.map((f) => ({
          item: f.item != null ? String(f.item) : "",
          transport_type_id: f.transport_type_id ? String(f.transport_type_id.id) : "",
          account: f.account_id,
          description: f.description ?? "",
          order: f.order != null ? String(f.order) : "1",
          buysell: f.buysell != null ? String(f.buysell) : "1",
          currency: f.currency ? String(f.currency.id) : "",
          net_price: f.net_price != null ? String(f.net_price) : "",
          total_price: f.total_price != null ? String(f.total_price) : "",
          quantity: f.quantity != null ? String(f.quantity) : "1",
        })),
      );
      setExistingFiles(d.load_file);
    } catch {
      addToast("Teklif detayı yüklenemedi", "error");
      setDrawerOpen(false);
    } finally {
      setDetailLoading(false);
    }
  }

  function addContentRow() {
    setContent((list) => [...list, { ...EMPTY_CONTENT_ROW }]);
  }
  function removeContentRow(i: number) {
    setContent((list) => (list.length > 1 ? list.filter((_, xi) => xi !== i) : list));
  }
  function addFinancialRow() {
    setFinancialItems((list) => [...list, { ...EMPTY_FINANCIAL_ROW }]);
  }
  function removeFinancialRow(i: number) {
    setFinancialItems((list) => list.filter((_, xi) => xi !== i));
  }

  async function handleSubmit() {
    setSaving(true);
    setErrors({});
    const fd = new FormData();
    fd.append("work_type_id", form.work_type_id);
    fd.append("loading_type_id", form.loading_type_id);
    fd.append("payment_type_id", form.payment_type_id);
    fd.append("status_type_id", form.status_type_id);
    fd.append("load_transfer_type_id", form.load_transfer_type_id);
    fd.append("instruction_id", form.instruction_id);
    fd.append("romork_type_id", form.romork_type_id);
    fd.append("department_id", form.department_id);
    fd.append("offer_date", form.offer_date);
    fd.append("offer_validity_date", form.offer_validity_date);
    fd.append("marketing_notification_date", form.marketing_notification_date);
    fd.append("payer_company", form.payer_company);
    fd.append("description", form.description);
    fd.append("way_of_working", form.way_of_working);

    if (customer) fd.append("customer_id", String(customer.id));
    if (sender) fd.append("sender_id", String(sender.id));
    if (receiver) fd.append("receiver_id", String(receiver.id));
    if (agent) fd.append("agent_id", String(agent.id));
    if (companyPayFreight) fd.append("company_pay_freight_id", String(companyPayFreight.id));

    if (route.departure_country_id) fd.append("departure_country_id", route.departure_country_id);
    if (route.transit_country_id) fd.append("transit_country_id", route.transit_country_id);
    if (route.target_country_id) fd.append("target_country_id", route.target_country_id);

    content.forEach((item, i) => {
      fd.append(`load_content[${i}][product_type_id]`, item.product_type_id);
      fd.append(`load_content[${i}][case_type_id]`, item.case_type_id);
      fd.append(`load_content[${i}][quantity]`, item.quantity);
      fd.append(`load_content[${i}][gross_weight]`, item.gross_weight);
      fd.append(`load_content[${i}][net_weight]`, item.net_weight);
      fd.append(`load_content[${i}][volume]`, item.volume);
      fd.append(`load_content[${i}][lademeter]`, item.lademeter);
      fd.append(`load_content[${i}][width]`, item.width);
      fd.append(`load_content[${i}][height]`, item.height);
      fd.append(`load_content[${i}][length]`, item.length);
      fd.append(`load_content[${i}][stackable]`, item.stackable);
    });

    financialItems.forEach((item, i) => {
      fd.append(`load_financial_item[${i}][item]`, item.item);
      fd.append(`load_financial_item[${i}][transport_type_id]`, item.transport_type_id);
      if (item.account) fd.append(`load_financial_item[${i}][account_id]`, String(item.account.id));
      fd.append(`load_financial_item[${i}][description]`, item.description);
      fd.append(`load_financial_item[${i}][order]`, item.order);
      fd.append(`load_financial_item[${i}][buysell]`, item.buysell);
      fd.append(`load_financial_item[${i}][currency]`, item.currency);
      fd.append(`load_financial_item[${i}][net_price]`, item.net_price);
      fd.append(`load_financial_item[${i}][total_price]`, item.total_price);
      fd.append(`load_financial_item[${i}][quantity]`, item.quantity);
    });

    // olsold: görevli[0] = Operasyon Yetkilisi, görevli[1..] = Satış Temsilcisi (çoğul).
    if (operationOfficer) {
      fd.append("load_charge_person[0][user_id]", String(operationOfficer.id));
      fd.append("load_charge_person[0][user_type]", "1");
    }
    salesReps.filter((rep) => rep.id).forEach((rep, i) => {
      fd.append(`load_charge_person[${i + 1}][user_id]`, String(rep.id));
      fd.append(`load_charge_person[${i + 1}][user_type]`, "2");
    });

    existingFiles
      .filter((f) => !removedFileIds.includes(f.id))
      .forEach((f) => fd.append("existing_file_ids[]", String(f.id)));
    newFiles.forEach((f) => fd.append("files", f));

    try {
      if (editingId) {
        await api.postForm(`/api/v1/load/${editingId}`, fd);
        addToast("Teklif güncellendi");
      } else {
        await api.postForm("/api/v1/load", fd);
        addToast("Teklif oluşturuldu");
      }
      setDrawerOpen(false);
      load();
    } catch (err) {
      if (err instanceof ApiError && err.errors) setErrors(err.errors);
      else addToast(err instanceof Error ? err.message : "Kaydedilemedi", "error");
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(id: number, no: string | null) {
    if (!window.confirm(`"${no ?? id}" silinsin mi?`)) return;
    try {
      await api.delete("/api/v1/load", { deletion_id: [id] });
      addToast("Teklif silindi");
      load();
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Silinemedi", "error");
    }
  }

  /**
   * BR-002/003/004: teklif önce Siber'e aktarılmış ve durumu Olumlu olmalı.
   * Gerçek iş kuralı hatası (backend'den gelen mesaj) olduğu gibi gösterilir —
   * sahte başarı üretilmez.
   */
  async function handleTransferToSiber(id: number) {
    try {
      await api.post("/api/v1/transfer_to_siber", { id });
      addToast("Teklif Siber'e aktarıldı");
      load();
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Siber'e aktarılamadı", "error");
    }
  }

  /**
   * Onaylanmış (Siber'e aktarılmış) bir teklifi yüke dönüştürür.
   * LoadTransferController.ConvertOffer, teklifin SİBER kimliğini (siber_id) —
   * ID DEĞİL — bekliyor; bu yüzden buton yalnızca siber_id doluyken etkin.
   */
  async function handleConvertToLoad(siberId: string) {
    try {
      const res = await api.post<{ data: { yuk_no: string }; message: string }>("/api/v1/load_transfer", { id: siberId });
      addToast(`Yük oluşturuldu: ${res.data.yuk_no}`);
    } catch (err) {
      if (err instanceof ApiError) addToast(err.message, "error");
      else addToast(err instanceof Error ? err.message : "Yüke dönüştürülemedi", "error");
    }
  }

  const columns: Column<LoadItem>[] = [
    { key: "id", header: "Teklif No", sortable: true, render: (r) => <span className="font-mono text-[11px] text-blue-600">{r.reservation_number ?? `T${r.id}`}</span> },
    { key: "customer", header: "Müşteri", sortable: true, render: (r) => <span className="font-semibold">{r.customer_id?.name ?? "—"}</span> },
    { key: "work_type", header: "İş Tipi", render: (r) => <span className="text-xs text-gray-500">{r.work_type_id?.name ?? "—"}</span> },
    { key: "content_count", header: "İçerik", render: (r) => <span className="font-mono text-xs">{r.load_content_count} kalem</span> },
    { key: "offer_date", header: "Tarih", render: (r) => <span className="font-mono text-xs text-gray-500">{r.offer_date ? new Date(r.offer_date).toLocaleDateString("tr-TR") : "—"}</span> },
    { key: "status", header: "Durum", render: (r) => (r.status_type_id != null && statusMap[r.status_type_id] ? <Badge label={statusMap[r.status_type_id]} /> : "—") },
  ];

  return (
    <>
      <ModulePage
        title="Teklifler"
        search={search}
        onSearchChange={(v) => { setSearch(v); setPage(1); }}
        searchPlaceholder="Teklif no, müşteri..."
        action={canCreate ? <Btn onClick={openNew}><Plus size={14} />Yeni Teklif</Btn> : undefined}
      >
        <div className="bg-white">
          {!loading && rows.length === 0 ? (
            <EmptyState icon={FileText} title="Teklif bulunamadı" desc="Arama kriterlerine uygun teklif bulunamadı." />
          ) : (
            <>
              <DataTable
                data={rows}
                columns={columns}
                loading={loading}
                onRowClick={(r) => openEdit(r.id)}
                actions={(r) => (
                  <div className="flex items-center justify-end gap-1">
                    <button
                      title="Siber'e Aktar"
                      onClick={(e) => { e.stopPropagation(); handleTransferToSiber(r.id); }}
                      className="p-1.5 rounded text-gray-400 hover:bg-blue-50 hover:text-blue-600 transition-colors"
                    >
                      <Package size={14} />
                    </button>
                    {r.siber_id && canCreate && (
                      <button
                        title="Yüke Dönüştür"
                        onClick={(e) => { e.stopPropagation(); handleConvertToLoad(r.siber_id!); }}
                        className="p-1.5 rounded text-gray-400 hover:bg-emerald-50 hover:text-emerald-600 transition-colors"
                      >
                        <Truck size={14} />
                      </button>
                    )}
                    <RowActions
                      onView={() => openEdit(r.id)}
                      onEdit={canUpdate ? () => openEdit(r.id) : undefined}
                      onDelete={canDelete ? () => handleDelete(r.id, r.reservation_number) : undefined}
                    />
                  </div>
                )}
              />
              <Pagination page={page} total={total} perPage={PER_PAGE} onChange={setPage} />
            </>
          )}
        </div>
      </ModulePage>

      <Drawer
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        title={editingId ? `Teklif — ${customer?.name ?? ""}` : "Yeni Teklif"}
        subtitle={editingId ? undefined : "Yeni teklif oluştur"}
        width="w-[720px]"
        footer={
          (editingId ? canUpdate : canCreate) ? (
            <div className="flex gap-2">
              <Btn onClick={handleSubmit} disabled={saving || detailLoading}>{saving ? "Kaydediliyor..." : "Kaydet"}</Btn>
              <Btn variant="secondary" onClick={() => setDrawerOpen(false)}>İptal</Btn>
            </div>
          ) : undefined
        }
      >
        <Tabs tabs={TABS} active={tab} onChange={setTab} className="px-6" />

        {detailLoading ? (
          <p className="text-sm text-gray-400 text-center py-10">Yükleniyor...</p>
        ) : (
          <div className="p-6">
            {tab === "Genel Bilgiler" && (
              <div className="space-y-6">
                <AccountPicker label="Müşteri" value={customer} onChange={setCustomer} required error={errors.customer_id?.[0]} />
                <div className="grid grid-cols-2 gap-4">
                  <FormField label="İş Tipi" required error={errors.work_type_id?.[0]}>
                    <SelectInput value={form.work_type_id} onChange={(v) => setForm((f) => ({ ...f, work_type_id: v }))} options={opts(workTypes)} />
                  </FormField>
                  <FormField label="Yükleme Tipi" required error={errors.loading_type_id?.[0]}>
                    <SelectInput value={form.loading_type_id} onChange={(v) => setForm((f) => ({ ...f, loading_type_id: v }))} options={opts(loadingTypes)} />
                  </FormField>
                  <FormField label="Ödeme Tipi" required error={errors.payment_type_id?.[0]}>
                    <SelectInput value={form.payment_type_id} onChange={(v) => setForm((f) => ({ ...f, payment_type_id: v }))} options={opts(paymentTypes)} />
                  </FormField>
                  <FormField label="Durum" required error={errors.status_type_id?.[0]}>
                    <SelectInput value={form.status_type_id} onChange={(v) => setForm((f) => ({ ...f, status_type_id: v }))} options={opts(statusTypes)} />
                  </FormField>
                  <FormField label="Departman" required error={errors.department_id?.[0]}>
                    <SelectInput value={form.department_id} onChange={(v) => setForm((f) => ({ ...f, department_id: v }))} options={opts(departments)} />
                  </FormField>
                  <FormField label="Çalışma Şekli" required error={errors.way_of_working?.[0]}>
                    <SelectInput value={form.way_of_working} onChange={(v) => setForm((f) => ({ ...f, way_of_working: v }))} options={WAY_OF_WORKING_OPTIONS} />
                  </FormField>
                  <FormField label="Yük/Taşıma Tipi">
                    <SelectInput value={form.load_transfer_type_id} onChange={(v) => setForm((f) => ({ ...f, load_transfer_type_id: v }))} options={opts(loadTransferTypes)} />
                  </FormField>
                  <FormField label="Talimat">
                    <SelectInput value={form.instruction_id} onChange={(v) => setForm((f) => ({ ...f, instruction_id: v }))} options={opts(instructions)} />
                  </FormField>
                  <FormField label="Römork Tipi">
                    <SelectInput value={form.romork_type_id} onChange={(v) => setForm((f) => ({ ...f, romork_type_id: v }))} options={opts(romorkTypes)} />
                  </FormField>
                  <FormField label="Teklif Tarihi" required error={errors.offer_date?.[0]}>
                    <TextInput value={form.offer_date} onChange={(v) => setForm((f) => ({ ...f, offer_date: v }))} type="date" error={!!errors.offer_date} />
                  </FormField>
                  <FormField label="Geçerlilik Tarihi" required error={errors.offer_validity_date?.[0]}>
                    <TextInput value={form.offer_validity_date} onChange={(v) => setForm((f) => ({ ...f, offer_validity_date: v }))} type="date" error={!!errors.offer_validity_date} />
                  </FormField>
                  <FormField label="Pazarlama Bildirim Tarihi" required error={errors.marketing_notification_date?.[0]}>
                    <TextInput value={form.marketing_notification_date} onChange={(v) => setForm((f) => ({ ...f, marketing_notification_date: v }))} type="date" error={!!errors.marketing_notification_date} />
                  </FormField>
                  <FormField label="Navlun Ödeyen Firma (serbest metin)">
                    <TextInput value={form.payer_company} onChange={(v) => setForm((f) => ({ ...f, payer_company: v }))} />
                  </FormField>
                </div>

                <div>
                  <div className="flex items-center justify-between mb-2">
                    <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider">Yük İçeriği</p>
                    <button type="button" onClick={addContentRow} className="text-[11px] text-blue-600 hover:underline flex items-center gap-1">
                      <Plus size={12} />Satır Ekle
                    </button>
                  </div>
                  {content.map((item, i) => (
                    <div key={i} className="border border-gray-200 rounded-lg p-4 mb-2 relative">
                      {content.length > 1 && (
                        <button type="button" onClick={() => removeContentRow(i)} className="absolute top-2 right-2 text-gray-300 hover:text-red-500">
                          <Trash2 size={13} />
                        </button>
                      )}
                      <div className="grid grid-cols-3 gap-3">
                        <FormField label="Ürün Tipi">
                          <SelectInput value={item.product_type_id} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, product_type_id: v } : x)))} options={opts(productTypes)} />
                        </FormField>
                        <FormField label="Kap Tipi">
                          <SelectInput value={item.case_type_id} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, case_type_id: v } : x)))} options={opts(caseTypes)} />
                        </FormField>
                        <FormField label="Adet">
                          <TextInput value={item.quantity} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, quantity: v } : x)))} type="number" />
                        </FormField>
                        <FormField label="Brüt Ağırlık (kg)">
                          <TextInput value={item.gross_weight} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, gross_weight: v } : x)))} />
                        </FormField>
                        <FormField label="Net Ağırlık (kg)">
                          <TextInput value={item.net_weight} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, net_weight: v } : x)))} />
                        </FormField>
                        <FormField label="Hacim (m³)">
                          <TextInput value={item.volume} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, volume: v } : x)))} />
                        </FormField>
                        <FormField label="Lademetre">
                          <TextInput value={item.lademeter} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, lademeter: v } : x)))} />
                        </FormField>
                        <FormField label="En (cm)">
                          <TextInput value={item.width} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, width: v } : x)))} />
                        </FormField>
                        <FormField label="Boy (cm)">
                          <TextInput value={item.length} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, length: v } : x)))} />
                        </FormField>
                        <FormField label="Yükseklik (cm)">
                          <TextInput value={item.height} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, height: v } : x)))} />
                        </FormField>
                        <FormField label="İstiflenebilir">
                          <SelectInput value={item.stackable} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, stackable: v } : x)))} options={[{ value: "1", label: "Evet" }, { value: "0", label: "Hayır" }]} />
                        </FormField>
                      </div>
                    </div>
                  ))}
                </div>

                <FormField label="Açıklama">
                  <TextareaInput value={form.description} onChange={(v) => setForm((f) => ({ ...f, description: v }))} rows={3} />
                </FormField>
              </div>
            )}

            {tab === "Taraflar" && (
              <div className="space-y-4">
                <AccountPicker label="Gönderici" value={sender} onChange={setSender} error={errors.sender_id?.[0]} />
                <AccountPicker label="Alıcı" value={receiver} onChange={setReceiver} error={errors.receiver_id?.[0]} />
                <AccountPicker label="Acente" value={agent} onChange={setAgent} error={errors.agent_id?.[0]} />
                <AccountPicker label="Navlun Ödeyen Firma (Cari)" value={companyPayFreight} onChange={setCompanyPayFreight} error={errors.company_pay_freight_id?.[0]} />
              </div>
            )}

            {tab === "Güzergah" && (
              <div className="grid grid-cols-2 gap-4">
                <FormField label="Çıkış Ülkesi">
                  <SelectInput value={route.departure_country_id} onChange={(v) => setRoute((r) => ({ ...r, departure_country_id: v }))} options={opts(countries)} />
                </FormField>
                <FormField label="Transit Ülke">
                  <SelectInput value={route.transit_country_id} onChange={(v) => setRoute((r) => ({ ...r, transit_country_id: v }))} options={opts(countries)} />
                </FormField>
                <FormField label="Varış Ülkesi">
                  <SelectInput value={route.target_country_id} onChange={(v) => setRoute((r) => ({ ...r, target_country_id: v }))} options={opts(countries)} />
                </FormField>
              </div>
            )}

            {tab === "Görevliler" && (
              <div className="space-y-6">
                <UserPicker
                  label="Operasyon Yetkilisi"
                  value={operationOfficer}
                  onChange={setOperationOfficer}
                  error={errors["load_charge_person.0.user_id"]?.[0]}
                />

                <div>
                  <div className="flex items-center justify-between mb-2">
                    <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider">Satış Temsilcisi</p>
                    <button type="button" onClick={() => setSalesReps((list) => [...list, { id: 0, name: null, surname: null }])} className="text-[11px] text-blue-600 hover:underline flex items-center gap-1">
                      <Plus size={12} />Ekle
                    </button>
                  </div>
                  {salesReps.length === 0 ? (
                    <p className="text-xs text-gray-400 text-center py-8">Henüz satış temsilcisi eklenmedi.</p>
                  ) : (
                    salesReps.map((rep, i) => (
                      <div key={i} className="flex items-start gap-2 mb-2">
                        <div className="flex-1">
                          <UserPicker
                            label={`Satış Temsilcisi ${i + 1}`}
                            value={rep.id ? rep : null}
                            onChange={(v) => setSalesReps((list) => list.map((x, xi) => (xi === i ? (v ?? { id: 0, name: null, surname: null }) : x)))}
                          />
                        </div>
                        <button type="button" onClick={() => setSalesReps((list) => list.filter((_, xi) => xi !== i))} className="mt-6 text-gray-300 hover:text-red-500">
                          <Trash2 size={14} />
                        </button>
                      </div>
                    ))
                  )}
                </div>
              </div>
            )}

            {tab === "Mali Kalemler" && (
              <div>
                <div className="flex items-center justify-between mb-2">
                  <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider">Mali Kalemler</p>
                  <button type="button" onClick={addFinancialRow} className="text-[11px] text-blue-600 hover:underline flex items-center gap-1">
                    <Plus size={12} />Kalem Ekle
                  </button>
                </div>
                {financialItems.length === 0 ? (
                  <p className="text-xs text-gray-400 text-center py-8">Henüz mali kalem eklenmedi.</p>
                ) : (
                  financialItems.map((item, i) => (
                    <div key={i} className="border border-gray-200 rounded-lg p-4 mb-2 relative">
                      <button type="button" onClick={() => removeFinancialRow(i)} className="absolute top-2 right-2 text-gray-300 hover:text-red-500">
                        <Trash2 size={13} />
                      </button>
                      <div className="grid grid-cols-3 gap-3 mb-3">
                        <FormField label="Kalem Tipi">
                          <SelectInput value={item.item} onChange={(v) => setFinancialItems((list) => list.map((x, xi) => (xi === i ? { ...x, item: v } : x)))} options={opts(itemTypes)} />
                        </FormField>
                        <FormField label="Taşıma Tipi">
                          <SelectInput value={item.transport_type_id} onChange={(v) => setFinancialItems((list) => list.map((x, xi) => (xi === i ? { ...x, transport_type_id: v } : x)))} options={opts(transportTypes)} />
                        </FormField>
                        <FormField label="Alış/Satış">
                          <SelectInput value={item.buysell} onChange={(v) => setFinancialItems((list) => list.map((x, xi) => (xi === i ? { ...x, buysell: v } : x)))} options={[{ value: "1", label: "Satış" }, { value: "0", label: "Alış" }]} />
                        </FormField>
                      </div>
                      <div className="mb-3">
                        <AccountPicker
                          label="Cari"
                          value={item.account}
                          onChange={(v) => setFinancialItems((list) => list.map((x, xi) => (xi === i ? { ...x, account: v } : x)))}
                        />
                      </div>
                      <div className="grid grid-cols-4 gap-3">
                        <FormField label="Adet">
                          <TextInput value={item.quantity} onChange={(v) => setFinancialItems((list) => list.map((x, xi) => (xi === i ? { ...x, quantity: v } : x)))} type="number" />
                        </FormField>
                        <FormField label="Birim Fiyat">
                          <TextInput value={item.net_price} onChange={(v) => setFinancialItems((list) => list.map((x, xi) => (xi === i ? { ...x, net_price: v } : x)))} />
                        </FormField>
                        <FormField label="Toplam Fiyat">
                          <TextInput value={item.total_price} onChange={(v) => setFinancialItems((list) => list.map((x, xi) => (xi === i ? { ...x, total_price: v } : x)))} />
                        </FormField>
                        <FormField label="Para Birimi">
                          <SelectInput value={item.currency} onChange={(v) => setFinancialItems((list) => list.map((x, xi) => (xi === i ? { ...x, currency: v } : x)))} options={opts(currencies)} />
                        </FormField>
                      </div>
                      <div className="mt-3">
                        <FormField label="Açıklama">
                          <TextInput value={item.description} onChange={(v) => setFinancialItems((list) => list.map((x, xi) => (xi === i ? { ...x, description: v } : x)))} />
                        </FormField>
                      </div>
                    </div>
                  ))
                )}
              </div>
            )}

            {tab === "Dosyalar" && (
              <div className="space-y-4">
                <div>
                  <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider mb-2">Mevcut Dosyalar</p>
                  {existingFiles.filter((f) => !removedFileIds.includes(f.id)).length === 0 ? (
                    <p className="text-xs text-gray-400">Dosya yok.</p>
                  ) : (
                    <div className="space-y-1.5">
                      {existingFiles.filter((f) => !removedFileIds.includes(f.id)).map((f) => (
                        <div key={f.id} className="flex items-center gap-2 p-2 rounded-lg border border-gray-100 text-sm">
                          <FileIcon size={14} className="text-gray-400 shrink-0" />
                          <a href={f.file ? `/storage/${f.file}` : "#"} target="_blank" rel="noreferrer" className="flex-1 truncate text-blue-600 hover:underline">
                            {f.org_name ?? f.file}
                          </a>
                          <button type="button" onClick={() => setRemovedFileIds((ids) => [...ids, f.id])} className="text-gray-300 hover:text-red-500">
                            <X size={14} />
                          </button>
                        </div>
                      ))}
                    </div>
                  )}
                </div>

                <div>
                  <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider mb-2">Yeni Dosya Ekle</p>
                  <label className="flex items-center gap-2 justify-center border-2 border-dashed border-gray-200 rounded-lg p-4 text-xs text-gray-500 cursor-pointer hover:border-blue-300 hover:text-blue-600 transition-colors">
                    <Upload size={14} />Dosya seç veya sürükle
                    <input
                      type="file"
                      multiple
                      className="hidden"
                      onChange={(e) => setNewFiles((prev) => [...prev, ...Array.from(e.target.files ?? [])])}
                    />
                  </label>
                  {newFiles.length > 0 && (
                    <div className="space-y-1.5 mt-2">
                      {newFiles.map((f, i) => (
                        <div key={i} className="flex items-center gap-2 p-2 rounded-lg border border-gray-100 text-sm">
                          <FileIcon size={14} className="text-gray-400 shrink-0" />
                          <span className="flex-1 truncate">{f.name}</span>
                          <button type="button" onClick={() => setNewFiles((list) => list.filter((_, xi) => xi !== i))} className="text-gray-300 hover:text-red-500">
                            <X size={14} />
                          </button>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>
            )}
          </div>
        )}
      </Drawer>
    </>
  );
}
