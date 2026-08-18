import { useEffect, useState } from "react";
import { FileText, Package, Plus, Trash2, Truck, Upload, File as FileIcon, X } from "lucide-react";
import { api, ApiError, type DataMessage, type Paginated } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useDebouncedValue, useLookupOptions } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { ModulePage } from "@/components/ui/ModulePage";
import { DataTable, EmptyState, Pagination, RowActions, type Column } from "@/components/ui/DataTable";
import { Drawer, Modal } from "@/components/ui/Overlay";
import { Btn, FormField, SelectInput, Tabs, TextInput, TextareaInput } from "@/components/ui/primitives";
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
  loading_type_id: NamedRef | null;
  customer_id: AccountOption | null;
  load_content_count: number;
  siber_id: string | null;
  load_charge_person: LoadChargePersonDetail[];
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
  front_transportation_by_us: number | null;
  final_transportation_by_us: number | null;
  email_to: string[];
  email_cc: string[];
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

const PER_PAGE = 8;
const TABS = ["Genel Bilgiler", "Taraflar", "Güzergah", "Görevliler", "Mali Kalemler", "Dosya Arşivi", "E-Posta Ayarları"];
// olsold: Offer.vue TabList — "Talep"/"Olumlu"/"Olumsuz"/"Sipariş"/"Düzeltme Talebi"/"Zaman
// Aşımı", bu sırayla. Kaynak status_type_id'yi SABİT (4/5/1/2/3) kullanıyor; burada DATA-002
// düzeltmesi (bkz. StatusTypeCodes) nedeniyle ham id'ye güvenilmiyor, seed'in verdiği kararlı
// isimle eşleştiriliyor (Invoice'ın "Onay Bekliyor" sekmesiyle aynı desen). "Talep" sekmesi
// altta yatan durumun kendi adı "Teklif" olsa da (kaynakta da böyle — "Teklifler" sayfasında
// "Teklif" sekmesi demek yerine "Talep" deniyor) statusName="Teklif" ile eşleşiyor. "Zaman
// Aşımı" statusName=null'dır — status_type_id değil, backend'in ayrı `timeout=1` parametresiyle
// filtrelenir (LoadService.ListAsync: durum 2/3/4/5 + load_number boş + 1 haftadır güncellenmemiş).
const STATUS_TABS: { label: string; statusName: string | null }[] = [
  { label: "Talep", statusName: "Teklif" },
  { label: "Olumlu", statusName: "Olumlu" },
  { label: "Olumsuz", statusName: "Olumsuz" },
  { label: "Sipariş", statusName: "Sipariş" },
  { label: "Düzeltme Talebi", statusName: "Düzeltme Talebi" },
  { label: "Zaman Aşımı", statusName: null },
];
const WAY_OF_WORKING_OPTIONS = [
  { value: "0", label: "Spot" },
  { value: "1", label: "Yıllık" },
];
const YES_NO_OPTIONS = [
  { value: "1", label: "Evet" },
  { value: "0", label: "Hayır" },
];
// olsold: buysell_types (Alış=1, Satış=2) -- Yük modülüyle aynı kod, Teklif'te ayrıca
// tanımlıydı ve YANLIŞLIKLA ters (0/1) kullanılıyordu, birebir düzeltildi.
const BUYSELL_OPTIONS = [
  { value: "1", label: "Alış" },
  { value: "2", label: "Satış" },
];
const FINANCIAL_ITEM_BUY_QUERY = { type: "1" };
const FINANCIAL_ITEM_SELL_QUERY = { type: "2" };

/** olsold: "E-Posta Ayarları" sekmesindeki AutoComplete multiple (serbest metin, chip listesi). */
function EmailChipInput({ label, emails, onChange }: { label: string; emails: string[]; onChange: (emails: string[]) => void }) {
  const [value, setValue] = useState("");

  function add() {
    const trimmed = value.trim();
    if (trimmed && !emails.includes(trimmed)) onChange([...emails, trimmed]);
    setValue("");
  }

  return (
    <div>
      <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider mb-2">{label}</p>
      <div className="flex gap-2 mb-2">
        <TextInput value={value} onChange={setValue} placeholder="ornek@sirket.com" />
        <Btn variant="secondary" onClick={add}>Ekle</Btn>
      </div>
      {emails.length === 0 ? (
        <p className="text-xs text-gray-400">Henüz e-posta eklenmedi.</p>
      ) : (
        <div className="flex flex-wrap gap-1.5">
          {emails.map((email, i) => (
            <span key={i} className="inline-flex items-center gap-1 pl-2.5 pr-1.5 py-1 rounded-full bg-blue-50 text-blue-700 text-xs">
              {email}
              <button type="button" onClick={() => onChange(emails.filter((_, xi) => xi !== i))} className="text-blue-400 hover:text-red-500">
                <X size={12} />
              </button>
            </span>
          ))}
        </div>
      )}
    </div>
  );
}

/** olsold: OfferListChargePersonsDialog.vue — liste satırındaki "Görevli" düğmesi + popup. */
function ChargePersonsCell({ people }: { people: LoadChargePersonDetail[] }) {
  const [open, setOpen] = useState(false);
  return (
    <>
      <button
        type="button"
        onClick={(e) => { e.stopPropagation(); setOpen(true); }}
        className="text-[11px] font-medium px-2.5 py-1 rounded-full border border-gray-200 text-gray-600 hover:border-blue-300 hover:text-blue-600 transition-colors whitespace-nowrap"
      >
        {people.length > 0 ? `${people.length} Görevli` : "Belirtilmedi"}
      </button>
      <Modal open={open} onClose={() => setOpen(false)} title="Görevliler">
        <div className="w-[360px] max-w-full p-1">
          {people.length === 0 ? (
            <p className="text-xs text-gray-400 text-center py-6">Görevli bulunamadı.</p>
          ) : (
            <table className="w-full text-xs">
              <thead>
                <tr className="border-b border-gray-100">
                  <th className="text-left py-2 px-2 text-[10px] font-semibold text-gray-500 uppercase tracking-wider">Görevli</th>
                  <th className="text-left py-2 px-2 text-[10px] font-semibold text-gray-500 uppercase tracking-wider">Görevi</th>
                </tr>
              </thead>
              <tbody>
                {people.map((p, i) => (
                  <tr key={i} className="border-b border-gray-50">
                    <td className="py-2 px-2 text-gray-700">{p.user_id ? `${p.user_id.name ?? ""} ${p.user_id.surname ?? ""}`.trim() : "—"}</td>
                    <td className="py-2 px-2 text-gray-500">{p.user_type === 1 ? "Operasyon Yetkilisi" : "Satış Temsilcisi"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </Modal>
    </>
  );
}

export function QuotesPage() {
  const { can } = useAuth();
  const { addToast } = useToast();
  const canCreate = can("load_management", "create");
  const canUpdate = can("load_management", "update");
  const canDelete = can("load_management", "delete");

  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [page, setPage] = useState(1);
  const [listTab, setListTab] = useState(STATUS_TABS[0].label);
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
    front_transportation_by_us: "0", final_transportation_by_us: "0",
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
  // olsold "E-Posta Ayarları" sekmesi: offer_data.email.to / .cc (serbest metin, çoğul).
  const [emailTo, setEmailTo] = useState<string[]>([]);
  const [emailCc, setEmailCc] = useState<string[]>([]);

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
  // olsold: SelectAjax fetchParams={type: buysell} -- Alış/Satış'a göre farklı kalem listesi.
  const { options: financialItemsBuy } = useLookupOptions("/api/v1/financial_item", FINANCIAL_ITEM_BUY_QUERY);
  const { options: financialItemsSell } = useLookupOptions("/api/v1/financial_item", FINANCIAL_ITEM_SELL_QUERY);
  const { options: transportTypes } = useLookupOptions("/api/v1/transport_type");
  const { options: currencies } = useLookupOptions("/api/v1/currency");
  const { options: countries } = useLookupOptions("/api/v1/country");

  function opts(list: { id: string | number; name: string }[]) {
    return [{ value: "", label: "Seçiniz" }, ...list.map((t) => ({ value: String(t.id), label: t.name }))];
  }

  const activeStatusTab = STATUS_TABS.find((t) => t.label === listTab) ?? STATUS_TABS[0];
  const activeStatusTypeId = activeStatusTab.statusName
    ? statusTypes.find((s) => s.name === activeStatusTab.statusName)?.id
    : undefined;
  const isTimeoutTab = activeStatusTab.statusName === null;

  function load() {
    setLoading(true);
    api
      .get<DataMessage<Paginated<LoadItem>>>("/api/v1/load", {
        search: debouncedSearch || undefined,
        status_type_id: isTimeoutTab ? undefined : activeStatusTypeId,
        timeout: isTimeoutTab ? 1 : undefined,
        date_from: dateFrom || undefined,
        date_to: dateTo || undefined,
        per_page: PER_PAGE,
        page,
      })
      .then((res) => {
        setRows(res.data.data);
        setTotal(res.data.total);
      })
      .catch(() => addToast("Teklif listesi yüklenemedi", "error"))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    // Zaman Aşımı dışındaki sekmeler ilgili status_type kaydı yüklenene kadar
    // beklemeli — aksi hâlde ilk render'da filtresiz (tüm durumlar) bir istek gider.
    if (!isTimeoutTab && statusTypes.length === 0) return;
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch, dateFrom, dateTo, page, listTab, statusTypes.length]);

  function resetForm() {
    // olsold: OfferFormDrawer.vue offer_data başlangıç değerleri — yeni teklif her
    // zaman Ödeme Tipi="PEŞİN", Durum="TEKLİF", Departman="SATIŞ & PAZARLAMA" ve
    // Geçerlilik Tarihi=bugün+7 ile açılır (ID değil AD ile eşleniyor — Kritik yön
    // değişikliği #23'teki durum-sekmesi eşlemesiyle aynı yaklaşım). "Peşin"/"Teklif"
    // target'ın seed verisinde büyük harf değil (Kritik yön değişikliği #27); Departman
    // artık gerçek Siber adıyla ("Satış & Pazarlama") eşleşiyor (Kritik yön
    // değişikliği #35 — DbSeeder gerçek sbr_departman verisiyle güncellendi).
    setForm({
      work_type_id: "", loading_type_id: "",
      payment_type_id: String(paymentTypes.find((t) => t.name === "Peşin")?.id ?? ""),
      status_type_id: String(statusTypes.find((t) => t.name === "Teklif")?.id ?? ""),
      load_transfer_type_id: "", instruction_id: "", romork_type_id: "",
      department_id: String(departments.find((t) => t.name === "Satış & Pazarlama")?.id ?? ""),
      offer_date: new Date().toISOString().slice(0, 10),
      offer_validity_date: new Date(Date.now() + 7 * 86400000).toISOString().slice(0, 10),
      marketing_notification_date: new Date().toISOString().slice(0, 10),
      payer_company: "", description: "", way_of_working: "0",
    front_transportation_by_us: "0", final_transportation_by_us: "0",
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
    setEmailTo([]);
    setEmailCc([]);
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
        front_transportation_by_us: d.front_transportation_by_us != null ? String(d.front_transportation_by_us) : "0",
        final_transportation_by_us: d.final_transportation_by_us != null ? String(d.final_transportation_by_us) : "0",
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
      setEmailTo(d.email_to ?? []);
      setEmailCc(d.email_cc ?? []);
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
    fd.append("front_transportation_by_us", form.front_transportation_by_us);
    fd.append("final_transportation_by_us", form.final_transportation_by_us);

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

    emailTo.forEach((email) => fd.append("email_to[]", email));
    emailCc.forEach((email) => fd.append("email_cc[]", email));

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

  // olsold: OfferTable.vue — "Yük No"/"Rezervasyon No" ve "Yük Tipi"/"Yük Türü" AYRI
  // sütunlar (load_number/reservation_number, loading_type_id/work_type_id).
  const columns: Column<LoadItem>[] = [
    { key: "load_number", header: "Yük No", render: (r) => <span className="font-mono text-[11px]">{r.load_number ?? "—"}</span> },
    { key: "id", header: "Rezervasyon No", sortable: true, render: (r) => <span className="font-mono text-[11px] text-blue-600">{r.reservation_number ?? "—"}</span> },
    { key: "loading_type", header: "Yük Tipi", render: (r) => <span className="text-xs text-gray-500">{r.loading_type_id?.name ?? "—"}</span> },
    { key: "work_type", header: "Yük Türü", render: (r) => <span className="text-xs text-gray-500">{r.work_type_id?.name ?? "—"}</span> },
    {
      key: "customer",
      header: "Müşteri",
      sortable: true,
      // olsold: OfferTable.vue Müşteri sütunu — ad + (varsa) ülke adı alt satırda.
      // Avatar/bayrak görselleri kaynakta da `&& false`/`v-if="false"` ile KAPALI, taşınmadı.
      render: (r) => (
        <div>
          <div className="font-semibold">{r.customer_id?.name ?? "—"}</div>
          {r.customer_id?.country_id && <div className="text-[11px] text-gray-500 mt-0.5">{r.customer_id.country_id.name}</div>}
        </div>
      ),
    },
    { key: "content_count", header: "İçerik", render: (r) => <span className="font-mono text-xs">{r.load_content_count} kalem</span> },
    { key: "charge_person", header: "Görevli", render: (r) => <ChargePersonsCell people={r.load_charge_person} /> },
    { key: "offer_date", header: "Tarih", render: (r) => <span className="font-mono text-xs text-gray-500">{r.offer_date ? new Date(r.offer_date).toLocaleDateString("tr-TR") : "—"}</span> },
  ];

  return (
    <>
      <ModulePage
        title="Teklifler"
        search={search}
        onSearchChange={(v) => { setSearch(v); setPage(1); }}
        searchPlaceholder="Teklif no, müşteri..."
        filters={
          <div className="flex items-center gap-1.5">
            <div className="w-[136px]"><TextInput type="date" value={dateFrom} onChange={(v) => { setDateFrom(v); setPage(1); }} /></div>
            <span className="text-xs text-gray-400">–</span>
            <div className="w-[136px]"><TextInput type="date" value={dateTo} onChange={(v) => { setDateTo(v); setPage(1); }} /></div>
          </div>
        }
        action={canCreate ? <Btn onClick={openNew}><Plus size={14} />Yeni Teklif</Btn> : undefined}
      >
        <Tabs
          tabs={STATUS_TABS.map((t) => t.label)}
          active={listTab}
          onChange={(t) => { setListTab(t); setPage(1); }}
          className="px-6 bg-white"
        />
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
                    <SelectInput
                      value={form.work_type_id}
                      onChange={(v) => setForm((f) => ({
                        ...f,
                        work_type_id: v,
                        // olsold onChangeWorkType: İhracat/İthalat -> ön taşıma evet/son hayır; Transit -> ikisi de hayır.
                        front_transportation_by_us: v === "1" || v === "2" ? "1" : "0",
                        final_transportation_by_us: "0",
                      }))}
                      options={opts(workTypes)}
                    />
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
                  <FormField label="Yük/Taşıma Tipi" required error={errors.load_transfer_type_id?.[0]}>
                    <SelectInput value={form.load_transfer_type_id} onChange={(v) => setForm((f) => ({ ...f, load_transfer_type_id: v }))} options={opts(loadTransferTypes)} />
                  </FormField>
                  <FormField label="Ön Taşıma Tarafımızdan Yapılır">
                    <SelectInput value={form.front_transportation_by_us} onChange={(v) => setForm((f) => ({ ...f, front_transportation_by_us: v }))} options={YES_NO_OPTIONS} />
                  </FormField>
                  <FormField label="Son Taşıma Tarafımızdan Yapılır">
                    <SelectInput value={form.final_transportation_by_us} onChange={(v) => setForm((f) => ({ ...f, final_transportation_by_us: v }))} options={YES_NO_OPTIONS} />
                  </FormField>
                  <FormField label="Talimat" required error={errors.instruction_id?.[0]}>
                    <SelectInput value={form.instruction_id} onChange={(v) => setForm((f) => ({ ...f, instruction_id: v }))} options={opts(instructions)} />
                  </FormField>
                  <FormField label="Römork Tipi" required error={errors.romork_type_id?.[0]}>
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
                        <FormField label="Ürün Tipi" required error={errors[`load_content.${i}.product_type_id`]?.[0]}>
                          <SelectInput value={item.product_type_id} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, product_type_id: v } : x)))} options={opts(productTypes)} />
                        </FormField>
                        <FormField label="Kap Tipi" required error={errors[`load_content.${i}.case_type_id`]?.[0]}>
                          <SelectInput value={item.case_type_id} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, case_type_id: v } : x)))} options={opts(caseTypes)} />
                        </FormField>
                        <FormField label="Adet" required error={errors[`load_content.${i}.quantity`]?.[0]}>
                          <TextInput value={item.quantity} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, quantity: v } : x)))} type="number" error={!!errors[`load_content.${i}.quantity`]} />
                        </FormField>
                        <FormField label="Brüt Ağırlık (kg)" required error={errors[`load_content.${i}.gross_weight`]?.[0]}>
                          <TextInput value={item.gross_weight} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, gross_weight: v } : x)))} error={!!errors[`load_content.${i}.gross_weight`]} />
                        </FormField>
                        <FormField label="Net Ağırlık (kg)">
                          <TextInput value={item.net_weight} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, net_weight: v } : x)))} />
                        </FormField>
                        <FormField label="Hacim (m³)">
                          <TextInput value={item.volume} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, volume: v } : x)))} />
                        </FormField>
                        <FormField label="Lademetre" required error={errors[`load_content.${i}.lademeter`]?.[0]}>
                          <TextInput value={item.lademeter} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, lademeter: v } : x)))} error={!!errors[`load_content.${i}.lademeter`]} />
                        </FormField>
                        <FormField label="En (cm)" required error={errors[`load_content.${i}.width`]?.[0]}>
                          <TextInput value={item.width} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, width: v } : x)))} error={!!errors[`load_content.${i}.width`]} />
                        </FormField>
                        <FormField label="Boy (cm)" required error={errors[`load_content.${i}.length`]?.[0]}>
                          <TextInput value={item.length} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, length: v } : x)))} error={!!errors[`load_content.${i}.length`]} />
                        </FormField>
                        <FormField label="Yükseklik (cm)" required error={errors[`load_content.${i}.height`]?.[0]}>
                          <TextInput value={item.height} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, height: v } : x)))} error={!!errors[`load_content.${i}.height`]} />
                        </FormField>
                        <FormField label="İstiflenebilir" required error={errors[`load_content.${i}.stackable`]?.[0]}>
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
                <AccountPicker label="Gönderici" value={sender} onChange={setSender} required error={errors.sender_id?.[0]} />
                <AccountPicker label="Alıcı" value={receiver} onChange={setReceiver} required error={errors.receiver_id?.[0]} />
                <AccountPicker label="Acente" value={agent} onChange={setAgent} error={errors.agent_id?.[0]} accountType={5} />
                <AccountPicker label="Navlun Ödeyen Firma (Cari)" value={companyPayFreight} onChange={setCompanyPayFreight} error={errors.company_pay_freight_id?.[0]} accountType={1} />
              </div>
            )}

            {tab === "Güzergah" && (
              <div className="grid grid-cols-2 gap-4">
                <FormField label="Çıkış Ülkesi" required error={errors.departure_country_id?.[0]}>
                  <SelectInput value={route.departure_country_id} onChange={(v) => setRoute((r) => ({ ...r, departure_country_id: v }))} options={opts(countries)} />
                </FormField>
                <FormField label="Transit Ülke">
                  <SelectInput value={route.transit_country_id} onChange={(v) => setRoute((r) => ({ ...r, transit_country_id: v }))} options={opts(countries)} />
                </FormField>
                <FormField label="Varış Ülkesi" required error={errors.target_country_id?.[0]}>
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
                        <FormField label="Kalem" required error={errors[`load_financial_item.${i}.item`]?.[0]}>
                          <SelectInput
                            value={item.item}
                            onChange={(v) => setFinancialItems((list) => list.map((x, xi) => (xi === i ? { ...x, item: v } : x)))}
                            options={opts(item.buysell === "1" ? financialItemsBuy : financialItemsSell)}
                          />
                        </FormField>
                        <FormField label="Taşıma Tipi" required error={errors[`load_financial_item.${i}.transport_type_id`]?.[0]}>
                          <SelectInput value={item.transport_type_id} onChange={(v) => setFinancialItems((list) => list.map((x, xi) => (xi === i ? { ...x, transport_type_id: v } : x)))} options={opts(transportTypes)} />
                        </FormField>
                        <FormField label="Alış/Satış" required error={errors[`load_financial_item.${i}.buysell`]?.[0]}>
                          <SelectInput
                            value={item.buysell}
                            onChange={(v) => setFinancialItems((list) => list.map((x, xi) => (xi === i ? { ...x, buysell: v, item: "", account: null } : x)))}
                            options={BUYSELL_OPTIONS}
                          />
                        </FormField>
                      </div>
                      <div className="mb-3">
                        <AccountPicker
                          label={item.buysell === "1" ? "Tedarikçiler" : "Müşteriler"}
                          value={item.account}
                          onChange={(v) => setFinancialItems((list) => list.map((x, xi) => (xi === i ? { ...x, account: v } : x)))}
                          accountType={item.buysell === "1" ? 2 : 1}
                        />
                      </div>
                      <div className="grid grid-cols-4 gap-3">
                        <FormField label="Adet" required error={errors[`load_financial_item.${i}.quantity`]?.[0]}>
                          <TextInput value={item.quantity} onChange={(v) => setFinancialItems((list) => list.map((x, xi) => (xi === i ? { ...x, quantity: v } : x)))} type="number" error={!!errors[`load_financial_item.${i}.quantity`]} />
                        </FormField>
                        <FormField label="Birim Fiyat" required error={errors[`load_financial_item.${i}.net_price`]?.[0]}>
                          <TextInput value={item.net_price} onChange={(v) => setFinancialItems((list) => list.map((x, xi) => (xi === i ? { ...x, net_price: v } : x)))} error={!!errors[`load_financial_item.${i}.net_price`]} />
                        </FormField>
                        <FormField label="Toplam Fiyat" required error={errors[`load_financial_item.${i}.total_price`]?.[0]}>
                          <TextInput value={item.total_price} onChange={(v) => setFinancialItems((list) => list.map((x, xi) => (xi === i ? { ...x, total_price: v } : x)))} error={!!errors[`load_financial_item.${i}.total_price`]} />
                        </FormField>
                        <FormField label="Para Birimi" required error={errors[`load_financial_item.${i}.currency`]?.[0]}>
                          <SelectInput value={item.currency} onChange={(v) => setFinancialItems((list) => list.map((x, xi) => (xi === i ? { ...x, currency: v } : x)))} options={opts(currencies)} />
                        </FormField>
                      </div>
                      <div className="mt-3">
                        <FormField label="Açıklama" error={errors[`load_financial_item.${i}.description`]?.[0]} hint="Kalem tutarı 0 ise açıklama zorunludur.">
                          <TextInput value={item.description} onChange={(v) => setFinancialItems((list) => list.map((x, xi) => (xi === i ? { ...x, description: v } : x)))} error={!!errors[`load_financial_item.${i}.description`]} />
                        </FormField>
                      </div>
                    </div>
                  ))
                )}
              </div>
            )}

            {tab === "Dosya Arşivi" && (
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

            {tab === "E-Posta Ayarları" && (
              <div className="space-y-6">
                <EmailChipInput label="Gönderilecek" emails={emailTo} onChange={setEmailTo} />
                <EmailChipInput label="CC" emails={emailCc} onChange={setEmailCc} />
              </div>
            )}
          </div>
        )}
      </Drawer>
    </>
  );
}
