import { useEffect, useState } from "react";
import { motion, AnimatePresence } from "motion/react";
import { clsx } from "clsx";
import { Receipt, Plus, Trash2, Tag, CheckCircle, Pencil, PencilOff, Filter, ChevronDown, X, CalendarDays } from "lucide-react";
import { api, ApiError, type DataMessage, type Paginated } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useDebouncedValue, useLookupOptions } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { ModulePage } from "@/components/ui/ModulePage";
import { EmptyState, Pagination } from "@/components/ui/DataTable";
import { Drawer, Modal } from "@/components/ui/Overlay";
import { Badge, Btn, FormField, SearchInput, SelectInput, Tabs, TextareaInput, TextInput } from "@/components/ui/primitives";
import { AccountPicker, type AccountOption } from "@/components/shared/AccountPicker";

interface NamedRef {
  id: number;
  name: string | null;
}

interface InvoiceItem {
  id: number;
  box_type: 0 | 1;
  commercial_type: number;
  invoice_id: string | null;
  target_title: string | null;
  target_identity_no: string | null;
  message: string | null;
  invoice_create_date: string | null;
  invoice_execution_date: string | null;
  payable_amount: number | null;
  document_currency_code: string | null;
  invoice_account: AccountOption | null;
  invoice_status: NamedRef | null;
  invoice_type: NamedRef | null;
}

interface MappedInvoiceItem {
  id: number;
  insert_name: string | null;
  description: string | null;
  buysell: string | null;
  net_price: number | null;
  total_price: number | null;
  status: string | null;
  item: NamedRef | null;
  account_id: NamedRef | null;
  load_transfer: NamedRef | null;
}

interface LoadTransferInvoiceMap {
  id: number;
  load_transfer: NamedRef | null;
  load_transfer_invoice_item: MappedInvoiceItem | null;
}

interface InvoiceDetail extends InvoiceItem {
  account_id: number | null;
  invoice_type_id: number | null;
  order_document_id: string | null;
  load_transfer_invoice_maps: LoadTransferInvoiceMap[];
}

interface InvoiceFooterRow {
  id: number;
  invoice_id: number;
  value: string;
}

// olsold: InvoiceFormDescription.vue — satır bazlı düzenleme aç/kapat.
// editable/editable_value kaynaktaki AYNI alan adlarıyla istemci tarafında
// tutuluyor (API'ye gitmiyor).
interface FooterRowState extends InvoiceFooterRow {
  editable: boolean;
  editable_value: string;
}

const PER_PAGE = 24;
// olsold: pages/invoices.vue 3 Tab — "Gelen Faturalar"/"Giden Faturalar" (gelen/
// giden evrak yönü, Alış/Satış DEĞİL) ve "Onay Bekleyen Faturalar" (invoice-type=0
// SABİT + invoice_status="Onay Bekliyor" filtresi bir arada — kaynak bunu ayrı bir
// Tab olarak gösteriyor, burada aynı filtre seçicisinde özel bir değer olarak temsil
// edildi). Kaynak durum id'sini (7) SABİT KULLANMIYORUZ — Siber'e özgü, bu portta
// aynı sıra garantili değil; bunun yerine seed'in verdiği kararlı isimle eşleşiyor.
const BOX_TABS = [
  { value: "", label: "Tümü" },
  { value: "0", label: "Gelen Faturalar" },
  { value: "1", label: "Giden Faturalar" },
  { value: "pending_approval", label: "Onay Bekleyen Faturalar" },
];
// olsold: InvoiceFormDrawer.vue TabList — "Bilgiler"/"Kalemler"/"Ek Bilgiler"
// (Dipnotlar/Genel Bilgiler DEĞİL; "Fatura Önizleme" Uyumsoft e-fatura PDF
// önizlemesi, kapsam dışı).
const DETAIL_TABS = ["Bilgiler", "Kalemler", "Ek Bilgiler"];

// olsold: data/system_data.js invoice_commercial_type — "status:false" olan
// E-Arşiv (4) formda seçilemez (bkz. Yeni Fatura/Bilgiler'deki 2 seçenekli
// SelectInput, değişmedi) ama tabloda bir kayıt bu değere sahipse yine de
// doğru renk/etiketle gösterilmeli.
const COMMERCIAL_TYPE_META: Record<number, { name: string; dot: string }> = {
  0: { name: "Temel Fatura", dot: "bg-blue-500" },
  1: { name: "Ticari Fatura", dot: "bg-orange-500" },
  4: { name: "E-Arşiv", dot: "bg-green-500" },
};

function InvoiceCard({
  row, index, onClick, canDelete, onDelete,
}: {
  row: InvoiceItem; index: number; onClick: () => void; canDelete: boolean; onDelete: () => void;
}) {
  const meta = COMMERCIAL_TYPE_META[row.commercial_type];
  const date = row.invoice_create_date ? new Date(row.invoice_create_date).toLocaleDateString("tr-TR") : null;

  return (
    <motion.div
      initial={{ opacity: 0, y: 8 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.2, delay: Math.min(index, 10) * 0.03 }}
      whileHover={{ y: -2 }}
      onClick={onClick}
      className="bg-white rounded-xl border border-gray-200 shadow-sm hover:shadow-md hover:border-blue-200 transition-shadow cursor-pointer p-4 flex flex-col gap-3"
    >
      <div className="flex items-start justify-between gap-2">
        <div className="flex items-center gap-2.5 min-w-0">
          <div className="w-9 h-9 rounded-lg bg-blue-50 text-blue-600 flex items-center justify-center shrink-0">
            <Receipt size={16} />
          </div>
          <div className="min-w-0">
            <p className="font-mono text-xs font-semibold text-blue-600 truncate">{row.invoice_id ?? `FAT-${row.id}`}</p>
            {date && (
              <p className="text-[10px] text-gray-400 mt-0.5 flex items-center gap-1">
                <CalendarDays size={10} />
                {date}
              </p>
            )}
          </div>
        </div>
        <div className="flex items-center gap-1 shrink-0">
          {row.invoice_status?.name && <Badge label={row.invoice_status.name} />}
          {canDelete && (
            <button
              type="button"
              onClick={(e) => { e.stopPropagation(); onDelete(); }}
              className="p-1 rounded text-gray-300 hover:text-red-500 hover:bg-red-50 transition-colors"
            >
              <Trash2 size={13} />
            </button>
          )}
        </div>
      </div>

      <div className="pt-3 border-t border-gray-100">
        <p className="text-[10px] font-semibold text-gray-400 uppercase tracking-wider mb-0.5">Firma</p>
        <p className="text-sm font-semibold text-gray-900 truncate">{row.target_title || "—"}</p>
        {row.target_identity_no && <p className="text-[11px] text-gray-500 mt-0.5 font-mono truncate">{row.target_identity_no}</p>}
      </div>

      <div className="flex items-center justify-between gap-2 pt-2.5 border-t border-gray-100 min-w-0">
        <span className="font-mono text-sm font-bold text-gray-900 truncate">
          {(row.payable_amount ?? 0).toLocaleString("tr-TR", { minimumFractionDigits: 2 })} {row.document_currency_code ?? ""}
        </span>
        {meta && (
          <span className="inline-flex items-center gap-1.5 px-2 py-1 rounded-full border border-gray-200 text-[11px] shrink-0">
            <span className={`w-1.5 h-1.5 rounded-full shrink-0 ${meta.dot}`} />
            {meta.name}
          </span>
        )}
      </div>

      {row.invoice_execution_date && (
        <div className="flex items-center gap-1.5 text-[11px] text-gray-500 pt-2.5 border-t border-gray-100">
          <CalendarDays size={12} className="text-gray-400 shrink-0" />
          <span>Vade: {new Date(row.invoice_execution_date).toLocaleDateString("tr-TR")}</span>
        </div>
      )}
    </motion.div>
  );
}

export function InvoicesPage() {
  const { can } = useAuth();
  const { addToast } = useToast();
  const canCreate = can("invoice_management", "create");
  const canUpdate = can("invoice_management", "update");
  const canDelete = can("invoice_management", "delete");

  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);
  const [boxType, setBoxType] = useState("");
  const [page, setPage] = useState(1);
  const [rows, setRows] = useState<InvoiceItem[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);

  const [fAccount, setFAccount] = useState<AccountOption | null>(null);
  const [fInvoiceType, setFInvoiceType] = useState("");
  const [fCommercialType, setFCommercialType] = useState("");
  const [fInvoiceStatus, setFInvoiceStatus] = useState("");
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [showAdvanced, setShowAdvanced] = useState(false);
  const hasActiveAdvancedFilters = !!(
    boxType || fAccount || fInvoiceType || fCommercialType || fInvoiceStatus || dateFrom || dateTo
  );
  const hasActiveFilters = !!(search || hasActiveAdvancedFilters);

  function clearFilters() {
    setSearch("");
    setBoxType("");
    setFAccount(null);
    setFInvoiceType("");
    setFCommercialType("");
    setFInvoiceStatus("");
    setDateFrom("");
    setDateTo("");
    setPage(1);
  }

  const [drawerOpen, setDrawerOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  const [account, setAccount] = useState<AccountOption | null>(null);
  const [form, setForm] = useState({
    box_type: "1",
    commercial_type: "0",
    invoice_type_id: "",
    invoice_create_date: "",
    invoice_execution_date: "",
    message: "",
  });

  const { options: invoiceTypes } = useLookupOptions("/api/v1/invoice_type");
  const { options: invoiceStatuses } = useLookupOptions("/api/v1/invoice_status");
  const pendingApprovalStatusId = invoiceStatuses.find((s) => s.name === "Onay Bekliyor")?.id;
  const isPendingApprovalFilter = boxType === "pending_approval";

  function load() {
    setLoading(true);
    api
      .get<DataMessage<Paginated<InvoiceItem>>>("/api/v1/invoice", {
        search: debouncedSearch || undefined,
        box_type: isPendingApprovalFilter ? "0" : boxType || undefined,
        invoice_status_id: isPendingApprovalFilter ? pendingApprovalStatusId : fInvoiceStatus || undefined,
        account_id: fAccount?.id || undefined,
        invoice_type_id: fInvoiceType || undefined,
        commercial_type: fCommercialType || undefined,
        date_from: dateFrom || undefined,
        date_to: dateTo || undefined,
        per_page: PER_PAGE,
        page,
      })
      .then((res) => {
        setRows(res.data.data);
        setTotal(res.data.total);
      })
      .catch(() => addToast("Fatura listesi yüklenemedi", "error"))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    debouncedSearch, boxType, page, pendingApprovalStatusId,
    fAccount, fInvoiceType, fCommercialType, fInvoiceStatus, dateFrom, dateTo,
  ]);

  function openNew() {
    setForm({ box_type: "1", commercial_type: "0", invoice_type_id: "", invoice_create_date: "", invoice_execution_date: "", message: "" });
    setAccount(null);
    setErrors({});
    setDrawerOpen(true);
  }

  async function handleSubmit() {
    // olsold: InvoiceFormDrawer.vue handleForm — bu 3 alan boşsa istemci tarafında
    // engelleniyor ("Lütfen tüm alanları doldurunuz."), backend'e hiç gitmiyor.
    if (!form.commercial_type || !account || !form.invoice_type_id) {
      addToast("Lütfen tüm alanları doldurunuz.", "error");
      return;
    }
    // Buton disabled={saving} render'a kadar DOM'a yansımıyor — hızlı çift
    // tıklama/tekrar tetiklemeye karşı erken çıkış.
    if (saving) return;

    setSaving(true);
    setErrors({});
    const fd = new FormData();
    fd.append("box_type", form.box_type);
    fd.append("commercial_type", form.commercial_type);
    fd.append("account_id", account?.id ? String(account.id) : "");
    fd.append("invoice_type_id", form.invoice_type_id);
    fd.append("invoice_create_date", form.invoice_create_date);
    fd.append("invoice_execution_date", form.invoice_execution_date);
    fd.append("message", form.message);
    try {
      await api.postForm("/api/v1/invoice", fd);
      addToast("Fatura oluşturuldu");
      setDrawerOpen(false);
      load();
    } catch (err) {
      if (err instanceof ApiError && err.errors) setErrors(err.errors);
      else addToast(err instanceof Error ? err.message : "Kaydedilemedi", "error");
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(id: number) {
    if (!window.confirm(`Fatura FAT-${id} silinsin mi?`)) return;
    try {
      await api.delete("/api/v1/invoice/delete", { deletion_id: [id] });
      addToast("Fatura silindi");
      load();
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Silinemedi", "error");
    }
  }

  // --- Detay/düzenleme drawer'ı (Bilgiler + Kalemler + Ek Bilgiler) ---

  const [detailOpen, setDetailOpen] = useState(false);
  const [detailId, setDetailId] = useState<number | null>(null);
  const [detailTab, setDetailTab] = useState(DETAIL_TABS[0]);
  const [detail, setDetail] = useState<InvoiceDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailSaving, setDetailSaving] = useState(false);
  const [detailErrors, setDetailErrors] = useState<Record<string, string[]>>({});
  const [detailAccount, setDetailAccount] = useState<AccountOption | null>(null);
  const [detailForm, setDetailForm] = useState({
    box_type: "1", commercial_type: "0", invoice_type_id: "",
    invoice_create_date: "", invoice_execution_date: "", message: "",
  });
  const [maps, setMaps] = useState<LoadTransferInvoiceMap[]>([]);

  async function openDetail(id: number) {
    setDetailId(id);
    setDetailTab(DETAIL_TABS[0]);
    setDetailOpen(true);
    setDetailLoading(true);
    setDetailErrors({});
    try {
      const res = await api.get<DataMessage<InvoiceDetail>>(`/api/v1/invoice/${id}`);
      const d = res.data;
      setDetail(d);
      setDetailAccount(d.invoice_account);
      setDetailForm({
        box_type: String(d.box_type),
        commercial_type: String(d.commercial_type),
        invoice_type_id: d.invoice_type_id ? String(d.invoice_type_id) : "",
        invoice_create_date: d.invoice_create_date ? d.invoice_create_date.slice(0, 10) : "",
        invoice_execution_date: d.invoice_execution_date ? d.invoice_execution_date.slice(0, 10) : "",
        message: d.message ?? "",
      });
      setMaps(d.load_transfer_invoice_maps);
      loadFooters(id);
    } catch {
      addToast("Fatura bilgileri yüklenemedi", "error");
      setDetailOpen(false);
    } finally {
      setDetailLoading(false);
    }
  }

  async function handleDetailSave() {
    if (!detailId) return;
    setDetailSaving(true);
    setDetailErrors({});
    const fd = new FormData();
    fd.append("id", String(detailId));
    fd.append("box_type", detailForm.box_type);
    fd.append("commercial_type", detailForm.commercial_type);
    fd.append("account_id", detailAccount?.id ? String(detailAccount.id) : "");
    fd.append("invoice_type_id", detailForm.invoice_type_id);
    fd.append("invoice_create_date", detailForm.invoice_create_date);
    fd.append("invoice_execution_date", detailForm.invoice_execution_date);
    fd.append("message", detailForm.message);
    // Zarf her güncellemede kalem eşlemelerini BAŞTAN kurar (bkz. InvoiceService.
    // UpdateAsync) — bu yüzden mevcut hâl (Kalemler sekmesinde düzenlenen) her
    // kayıtta TAMAMEN gönderilmeli, aksi hâlde sessizce silinir.
    maps.forEach((m, i) => {
      fd.append(`load_transfer_invoice_maps[${i}][load_transfer_id]`, String(m.load_transfer?.id ?? ""));
      fd.append(`load_transfer_invoice_maps[${i}][invoice_item_id]`, String(m.load_transfer_invoice_item?.id ?? ""));
    });
    try {
      await api.postForm("/api/v1/invoice/update", fd);
      addToast("Fatura güncellendi");
      load();
      openDetail(detailId);
    } catch (err) {
      if (err instanceof ApiError && err.errors) setDetailErrors(err.errors);
      else addToast(err instanceof Error ? err.message : "Kaydedilemedi", "error");
    } finally {
      setDetailSaving(false);
    }
  }

  function removeMap(mapId: number) {
    setMaps((list) => list.filter((m) => m.id !== mapId));
  }

  // --- Kalem seçici (mevcut load_transfer_invoice_item kayıtları arasından) ---
  //
  // olsold: InvoiceFormInvoiceItems.vue financial_item_list_filter — status
  // HER ZAMAN "pending" (zaten başka bir faturaya bağlanmış kalemler burada
  // ASLA görünmemeli, aksi hâlde aynı tutar iki kez faturalanabilir) ve
  // buysell faturanın box_type'ına göre sabit (Gelen/Alış=1, Giden/Satış=2).
  // Backend bu filtreleri zaten destekliyordu (LoadTransferInvoiceItemController),
  // yalnızca burada hiç gönderilmiyordu.

  const [pickerOpen, setPickerOpen] = useState(false);
  const [pickerSearch, setPickerSearch] = useState("");
  const debouncedPickerSearch = useDebouncedValue(pickerSearch);
  const [pickerAccount, setPickerAccount] = useState<AccountOption | null>(null);
  const [pickerResults, setPickerResults] = useState<MappedInvoiceItem[]>([]);
  const [pickerLoading, setPickerLoading] = useState(false);
  const pickerBuysell = detailForm.box_type === "0" ? "1" : "2";

  useEffect(() => {
    if (!pickerOpen) return;
    setPickerLoading(true);
    api
      .get<DataMessage<Paginated<MappedInvoiceItem>>>("/api/v1/load_transfer_invoice_item", {
        search: debouncedPickerSearch || undefined,
        status: "pending",
        buysell: pickerBuysell,
        account_id: pickerAccount?.id,
        per_page: 8,
        page: 1,
      })
      .then((res) => setPickerResults(res.data.data))
      .catch(() => setPickerResults([]))
      .finally(() => setPickerLoading(false));
  }, [pickerOpen, debouncedPickerSearch, pickerBuysell, pickerAccount]);

  function addMap(item: MappedInvoiceItem) {
    if (!item.load_transfer) {
      addToast("Bu kalem bir yük kaydına bağlı değil, faturaya eklenemez", "error");
      return;
    }
    if (maps.some((m) => m.load_transfer_invoice_item?.id === item.id)) {
      addToast("Bu kalem zaten eklendi", "error");
      return;
    }
    setMaps((list) => [...list, { id: -Date.now(), load_transfer: item.load_transfer, load_transfer_invoice_item: item }]);
    setPickerOpen(false);
  }

  // --- Ek Bilgiler / Maddeler (footer) — bağımsız, anında kaydedilen CRUD ---
  //
  // olsold: InvoiceFormDescription.vue — Güncelle/Sil düğmeleri yalnızca
  // düzenleme modu açıkken (kalem editable=true) görünür; kalem-bazlı toggle
  // düğmesi HER ZAMAN görünür. Üç işlem de (ekle/güncelle/sil) bir onay
  // adımından geçer (confirm.require) — bu projede window.confirm ile.

  const [footers, setFooters] = useState<FooterRowState[]>([]);
  const [footersLoading, setFootersLoading] = useState(false);
  const [newFooterValue, setNewFooterValue] = useState("");
  const [footerSaving, setFooterSaving] = useState(false);

  function loadFooters(invoiceId: number) {
    setFootersLoading(true);
    api
      .get<DataMessage<InvoiceFooterRow[]>>("/api/v1/invoice/footer", { invoice_id: invoiceId })
      .then((res) => setFooters(res.data.map((f) => ({ ...f, editable: false, editable_value: f.value }))))
      .catch(() => addToast("Maddeler yüklenemedi", "error"))
      .finally(() => setFootersLoading(false));
  }

  async function addFooter() {
    if (!detailId || !newFooterValue.trim()) return;
    if (!window.confirm("Yeni madde eklensin mi?")) return;
    setFooterSaving(true);
    const fd = new FormData();
    fd.append("invoice_id", String(detailId));
    fd.append("value", newFooterValue.trim());
    try {
      await api.postForm("/api/v1/invoice/footer", fd);
      setNewFooterValue("");
      loadFooters(detailId);
      addToast("Fatura ek bilgisi eklendi");
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Madde eklenemedi", "error");
    } finally {
      setFooterSaving(false);
    }
  }

  function toggleFooterEdit(id: number) {
    setFooters((list) => list.map((f) => (f.id === id ? { ...f, editable: !f.editable, editable_value: f.value } : f)));
  }

  function setFooterEditableValue(id: number, value: string) {
    setFooters((list) => list.map((f) => (f.id === id ? { ...f, editable_value: value } : f)));
  }

  async function saveFooterEdit(id: number, value: string) {
    if (!detailId) return;
    if (!value.trim()) return;
    if (!window.confirm("Fatura ek bilgisini güncellemek istediğinize emin misiniz?")) return;
    setFooterSaving(true);
    const fd = new FormData();
    fd.append("id", String(id));
    fd.append("invoice_id", String(detailId));
    fd.append("value", value.trim());
    try {
      await api.postForm("/api/v1/invoice/footer/update", fd);
      setFooters((list) => list.map((f) => (f.id === id ? { ...f, value: value.trim(), editable_value: value.trim(), editable: false } : f)));
      addToast("Fatura ek bilgisi güncellendi");
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Güncellenemedi", "error");
    } finally {
      setFooterSaving(false);
    }
  }

  async function removeFooter(id: number) {
    if (!detailId) return;
    if (!window.confirm("Bu madde silinsin mi?")) return;
    try {
      await api.delete("/api/v1/invoice/footer", { deletion_id: [id] });
      loadFooters(detailId);
      addToast("Fatura ek bilgisi silindi");
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Silinemedi", "error");
    }
  }

  return (
    <>
      <ModulePage
        title="Faturalar"
        action={canCreate ? <Btn onClick={openNew}><Plus size={14} />Yeni Fatura</Btn> : undefined}
      >
        <div className="bg-white border-b border-gray-200 px-6 py-4">
          <div className="flex items-center gap-2.5">
            <div className="flex-1 max-w-md">
              <TextInput value={search} onChange={(v) => { setSearch(v); setPage(1); }} placeholder="Genel arama: müşteri, referans..." />
            </div>
            <div className="w-48 shrink-0">
              <SelectInput
                value={boxType}
                onChange={(v) => { setBoxType(v); setPage(1); }}
                options={BOX_TABS.map((b) => ({ value: b.value, label: b.label }))}
              />
            </div>
            <button
              type="button"
              onClick={() => setShowAdvanced((s) => !s)}
              className={clsx(
                "flex items-center gap-1.5 text-xs font-medium px-3 py-2 rounded-md border transition-colors shrink-0",
                showAdvanced || hasActiveAdvancedFilters
                  ? "text-blue-600 border-blue-200 bg-blue-50/50"
                  : "text-gray-600 border-gray-200 hover:border-blue-200 hover:text-blue-600",
              )}
            >
              <Filter size={13} />
              Detaylı Arama
              {hasActiveAdvancedFilters && <span className="w-1.5 h-1.5 rounded-full bg-blue-600" />}
              <ChevronDown size={13} className={clsx("transition-transform", showAdvanced && "rotate-180")} />
            </button>
            {hasActiveFilters && (
              <button type="button" onClick={clearFilters} className="text-xs text-gray-500 hover:text-red-600 flex items-center gap-1 shrink-0">
                <X size={12} />
                Temizle
              </button>
            )}
          </div>

          <AnimatePresence initial={false}>
            {showAdvanced && (
              <motion.div
                initial={{ height: 0, opacity: 0 }}
                animate={{ height: "auto", opacity: 1 }}
                exit={{ height: 0, opacity: 0 }}
                transition={{ duration: 0.2, ease: "easeInOut" }}
                className="overflow-hidden"
              >
                <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3 pt-4 mt-4 border-t border-gray-100">
                  <AccountPicker label="Müşteri" value={fAccount} onChange={(v) => { setFAccount(v); setPage(1); }} />
                  <FormField label="Fatura Tipi">
                    <SelectInput value={fInvoiceType} onChange={(v) => { setFInvoiceType(v); setPage(1); }} options={[{ value: "", label: "Seçiniz" }, ...invoiceTypes.map((t) => ({ value: String(t.id), label: t.name }))]} />
                  </FormField>
                  <FormField label="Fatura Türü">
                    <SelectInput value={fCommercialType} onChange={(v) => { setFCommercialType(v); setPage(1); }} options={[{ value: "", label: "Seçiniz" }, { value: "0", label: "Temel Fatura" }, { value: "1", label: "Ticari Fatura" }, { value: "4", label: "E-Arşiv" }]} />
                  </FormField>
                  <FormField label="Fatura Durumu">
                    <SelectInput value={fInvoiceStatus} onChange={(v) => { setFInvoiceStatus(v); setPage(1); }} options={[{ value: "", label: "Seçiniz" }, ...invoiceStatuses.map((t) => ({ value: String(t.id), label: t.name }))]} />
                  </FormField>
                  <FormField label="Başlangıç Tarihi">
                    <TextInput type="date" value={dateFrom} onChange={(v) => { setDateFrom(v); setPage(1); }} />
                  </FormField>
                  <FormField label="Bitiş Tarihi">
                    <TextInput type="date" value={dateTo} onChange={(v) => { setDateTo(v); setPage(1); }} />
                  </FormField>
                </div>
              </motion.div>
            )}
          </AnimatePresence>
        </div>
        <div className="bg-gray-50/70 min-h-full">
          {!loading && rows.length === 0 ? (
            <EmptyState icon={Receipt} title="Fatura bulunamadı" desc="Arama kriterlerine uygun fatura bulunamadı." />
          ) : (
            <>
              <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-3 p-4">
                {loading
                  ? Array.from({ length: 6 }).map((_, i) => (
                      <div key={i} className="bg-white rounded-xl border border-gray-200 p-4 h-[132px] animate-pulse">
                        <div className="h-3 w-20 bg-gray-200 rounded mb-3" />
                        <div className="h-3 w-32 bg-gray-200 rounded mb-2" />
                        <div className="h-3 w-24 bg-gray-100 rounded" />
                      </div>
                    ))
                  : rows.map((r, i) => (
                      <InvoiceCard
                        key={r.id}
                        row={r}
                        index={i}
                        onClick={() => openDetail(r.id)}
                        canDelete={canDelete}
                        onDelete={() => handleDelete(r.id)}
                      />
                    ))}
              </div>
              <Pagination page={page} total={total} perPage={PER_PAGE} onChange={setPage} />
            </>
          )}
        </div>
      </ModulePage>

      <Drawer
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        title="Yeni Fatura"
        width="w-[560px]"
        footer={
          canCreate && (
            <div className="flex gap-2">
              <Btn onClick={handleSubmit} disabled={saving}>{saving ? "Kaydediliyor..." : "Kaydet"}</Btn>
              <Btn variant="secondary" onClick={() => setDrawerOpen(false)}>İptal</Btn>
            </div>
          )
        }
      >
        <div className="p-6 grid grid-cols-2 gap-4">
          <FormField label="Yön" required>
            <SelectInput value={form.box_type} onChange={(v) => setForm((f) => ({ ...f, box_type: v }))} options={[{ value: "0", label: "Gelen Fatura" }, { value: "1", label: "Giden Fatura" }]} />
          </FormField>
          <FormField label="Fatura Tipi" required error={errors.invoice_type_id?.[0]}>
            <SelectInput value={form.invoice_type_id} onChange={(v) => setForm((f) => ({ ...f, invoice_type_id: v }))} options={[{ value: "", label: "Seçiniz" }, ...invoiceTypes.map((t) => ({ value: String(t.id), label: t.name }))]} />
          </FormField>
          <div className="col-span-2">
            <AccountPicker label="Müşteri" value={account} onChange={setAccount} required error={errors.account_id?.[0]} />
          </div>
          <FormField label="Fatura Türü">
            <SelectInput value={form.commercial_type} onChange={(v) => setForm((f) => ({ ...f, commercial_type: v }))} options={[{ value: "0", label: "Temel Fatura" }, { value: "1", label: "Ticari Fatura" }]} />
          </FormField>
          <FormField label="Fatura Tarihi" required error={errors.invoice_create_date?.[0]}>
            <TextInput value={form.invoice_create_date} onChange={(v) => setForm((f) => ({ ...f, invoice_create_date: v }))} type="date" error={!!errors.invoice_create_date} />
          </FormField>
          <FormField label="Vade Tarihi" required error={errors.invoice_execution_date?.[0]}>
            <TextInput value={form.invoice_execution_date} onChange={(v) => setForm((f) => ({ ...f, invoice_execution_date: v }))} type="date" error={!!errors.invoice_execution_date} />
          </FormField>
          <div className="col-span-2">
            <FormField label="Açıklama">
              <TextareaInput value={form.message} onChange={(v) => setForm((f) => ({ ...f, message: v }))} placeholder="Fatura notu..." />
            </FormField>
          </div>
        </div>
      </Drawer>

      <Drawer
        open={detailOpen}
        onClose={() => setDetailOpen(false)}
        title={detail ? `FAT-${detail.id}` : "Fatura"}
        subtitle={detail?.invoice_account?.name ?? undefined}
        width="w-[640px]"
        footer={
          detailTab !== "Ek Bilgiler" && canUpdate ? (
            <div className="flex gap-2">
              <Btn onClick={handleDetailSave} disabled={detailSaving || detailLoading}>{detailSaving ? "Kaydediliyor..." : "Kaydet"}</Btn>
              <Btn variant="secondary" onClick={() => setDetailOpen(false)}>İptal</Btn>
            </div>
          ) : undefined
        }
      >
        <Tabs tabs={DETAIL_TABS} active={detailTab} onChange={setDetailTab} className="px-6" />
        {detailLoading ? (
          <div className="p-10 text-center text-sm text-gray-400">Yükleniyor...</div>
        ) : (
          detail && (
            <div className="p-6">
              {detailTab === "Bilgiler" && (
                <div className="space-y-4">
                  <AccountPicker label="Müşteri" value={detailAccount} onChange={setDetailAccount} required error={detailErrors.account_id?.[0]} />
                  <div className="grid grid-cols-2 gap-4">
                    <FormField label="Yön" required>
                      <SelectInput value={detailForm.box_type} onChange={(v) => setDetailForm((f) => ({ ...f, box_type: v }))} options={[{ value: "0", label: "Gelen Fatura" }, { value: "1", label: "Giden Fatura" }]} />
                    </FormField>
                    <FormField label="Fatura Tipi" required error={detailErrors.invoice_type_id?.[0]}>
                      <SelectInput value={detailForm.invoice_type_id} onChange={(v) => setDetailForm((f) => ({ ...f, invoice_type_id: v }))} options={[{ value: "", label: "Seçiniz" }, ...invoiceTypes.map((t) => ({ value: String(t.id), label: t.name }))]} />
                    </FormField>
                    <FormField label="Fatura Türü">
                      <SelectInput value={detailForm.commercial_type} onChange={(v) => setDetailForm((f) => ({ ...f, commercial_type: v }))} options={[{ value: "0", label: "Temel Fatura" }, { value: "1", label: "Ticari Fatura" }]} />
                    </FormField>
                    <FormField label="Fatura Tarihi" required error={detailErrors.invoice_create_date?.[0]}>
                      <TextInput value={detailForm.invoice_create_date} onChange={(v) => setDetailForm((f) => ({ ...f, invoice_create_date: v }))} type="date" />
                    </FormField>
                    <FormField label="Vade Tarihi" required error={detailErrors.invoice_execution_date?.[0]}>
                      <TextInput value={detailForm.invoice_execution_date} onChange={(v) => setDetailForm((f) => ({ ...f, invoice_execution_date: v }))} type="date" />
                    </FormField>
                  </div>
                  <FormField label="Açıklama">
                    <TextareaInput value={detailForm.message} onChange={(v) => setDetailForm((f) => ({ ...f, message: v }))} placeholder="Fatura notu..." />
                  </FormField>
                </div>
              )}

              {detailTab === "Kalemler" && (
                <div>
                  <div className="flex items-center justify-between mb-3">
                    <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider">Kalemler</p>
                    {canUpdate && (
                      <button type="button" onClick={() => { setPickerSearch(""); setPickerAccount(null); setPickerOpen(true); }} className="text-[11px] text-blue-600 hover:underline flex items-center gap-1">
                        <Tag size={12} />Kalem Ekle
                      </button>
                    )}
                  </div>

                  {maps.length === 0 ? (
                    <p className="text-xs text-gray-400 text-center py-8">Henüz kalem eklenmedi.</p>
                  ) : (
                    <>
                      <div className="space-y-2">
                        {maps.map((m) => (
                          <div key={m.id} className="border border-gray-200 rounded-lg p-3 flex items-center justify-between">
                            <div>
                              <p className="text-xs font-semibold text-blue-600">{m.load_transfer_invoice_item?.item?.name ?? m.load_transfer_invoice_item?.description ?? `Kalem #${m.load_transfer_invoice_item?.id}`}</p>
                              <p className="text-[11px] text-gray-500">{m.load_transfer?.name ?? "—"} · {m.load_transfer_invoice_item?.buysell === "1" ? "Alış" : "Satış"}</p>
                            </div>
                            <div className="flex items-center gap-3">
                              <span className="font-mono text-[11px] text-gray-500">{m.load_transfer_invoice_item?.total_price ?? m.load_transfer_invoice_item?.net_price ?? 0}</span>
                              {canUpdate && (
                                <button type="button" onClick={() => removeMap(m.id)} className="text-gray-300 hover:text-red-500">
                                  <Trash2 size={13} />
                                </button>
                              )}
                            </div>
                          </div>
                        ))}
                      </div>
                      <p className="mt-3 text-[11px] text-gray-400">Değişiklikler "Kaydet" ile birlikte uygulanır.</p>
                    </>
                  )}
                </div>
              )}

              {detailTab === "Ek Bilgiler" && (
                <div>
                  <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider mb-3">Yeni Madde Ekle</p>
                  <div className="flex gap-2 mb-4">
                    <TextInput value={newFooterValue} onChange={setNewFooterValue} placeholder="Yeni madde metni..." />
                    <Btn variant="secondary" size="sm" onClick={addFooter} disabled={footerSaving || !newFooterValue.trim()}>Ekle</Btn>
                  </div>
                  <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider mb-3">Maddeler</p>
                  {footersLoading ? (
                    <p className="text-xs text-gray-400 text-center py-8">Yükleniyor...</p>
                  ) : footers.length === 0 ? (
                    <p className="text-xs text-gray-400 text-center py-8">Henüz madde eklenmedi.</p>
                  ) : (
                    <div className="space-y-2">
                      {footers.map((f) => (
                        <div key={f.id} className="border border-gray-200 rounded-lg px-3 py-2 flex items-center gap-2">
                          {f.editable ? (
                            <TextInput value={f.editable_value} onChange={(v) => setFooterEditableValue(f.id, v)} />
                          ) : (
                            <span className="flex-1 text-xs text-gray-700">{f.value}</span>
                          )}
                          {canUpdate && f.editable && (
                            <button
                              type="button"
                              title="Güncelle"
                              onClick={() => saveFooterEdit(f.id, f.editable_value)}
                              disabled={footerSaving || !f.editable_value.trim()}
                              className="text-green-500 hover:text-green-600 shrink-0 disabled:opacity-40"
                            >
                              <CheckCircle size={14} />
                            </button>
                          )}
                          {canUpdate && f.editable && (
                            <button type="button" title="Sil" onClick={() => removeFooter(f.id)} className="text-gray-300 hover:text-red-500 shrink-0">
                              <Trash2 size={13} />
                            </button>
                          )}
                          {canUpdate && (
                            <button
                              type="button"
                              title={f.editable ? "Düzenleme modunu kapat" : "Düzenleme modunu aç"}
                              onClick={() => toggleFooterEdit(f.id)}
                              className="text-gray-300 hover:text-blue-600 shrink-0"
                            >
                              {f.editable ? <PencilOff size={13} /> : <Pencil size={13} />}
                            </button>
                          )}
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              )}
            </div>
          )
        )}
      </Drawer>

      <Modal open={pickerOpen} onClose={() => setPickerOpen(false)} title="Faturaya Kalem Ekle">
        <div className="w-[440px] max-w-full">
          <SearchInput value={pickerSearch} onChange={setPickerSearch} placeholder="Kalem, yük no, cari..." />
          <div className="mt-2">
            <AccountPicker
              label={pickerBuysell === "1" ? "Tedarikçiler" : "Müşteriler"}
              value={pickerAccount}
              onChange={setPickerAccount}
              accountType={pickerBuysell === "1" ? 2 : 1}
            />
          </div>
          <div className="mt-3 max-h-80 overflow-y-auto space-y-1">
            {pickerLoading ? (
              <p className="text-xs text-gray-400 text-center py-6">Yükleniyor...</p>
            ) : pickerResults.length === 0 ? (
              <p className="text-xs text-gray-400 text-center py-6">Kalem bulunamadı.</p>
            ) : (
              pickerResults.map((r) => (
                <button
                  key={r.id}
                  type="button"
                  onClick={() => addMap(r)}
                  className="w-full text-left px-3 py-2 rounded-lg text-sm hover:bg-blue-50 flex items-center justify-between"
                >
                  <span>
                    <span className="text-blue-700 font-medium">{r.item?.name ?? r.description ?? `#${r.id}`}</span>
                    <span className="text-gray-500 ml-2 text-xs">{r.load_transfer?.name ?? "yük bağlı değil"}</span>
                    {maps.some((m) => m.load_transfer_invoice_item?.id === r.id) && (
                      <Badge label="Eklendi" />
                    )}
                  </span>
                  <span className="font-mono text-xs text-gray-500">{r.total_price ?? r.net_price ?? 0}</span>
                </button>
              ))
            )}
          </div>
        </div>
      </Modal>
    </>
  );
}
