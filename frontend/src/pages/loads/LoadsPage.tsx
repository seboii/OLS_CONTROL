import { useCallback, useEffect, useRef, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { motion, AnimatePresence } from "motion/react";
import { clsx } from "clsx";
import { FileText, Package, Plus, Trash2, Upload, File as FileIcon, X, User, CalendarDays, Filter, ChevronDown, ChevronUp, Truck } from "lucide-react";
import { api, ApiError, downloadFile, type DataMessage, type Paginated } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useDebouncedValue, useLookupOptions } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { ModulePage } from "@/components/ui/ModulePage";
import { EmptyState, Pagination } from "@/components/ui/DataTable";
import { Drawer, Modal } from "@/components/ui/Overlay";
import { Badge, Btn, FormField, SelectInput, Tabs, TextareaInput, TextInput } from "@/components/ui/primitives";
import { AccountPicker, type AccountOption } from "@/components/shared/AccountPicker";
import { UserPicker, type UserOption } from "@/components/shared/UserPicker";
import { FinancialItemManagerModal } from "@/components/shared/FinancialItemManagerModal";
import { FinancialItemPicker, type FinancialItemOption } from "@/components/shared/FinancialItemPicker";
import { LookupPicker, type LookupOption } from "@/components/shared/LookupPicker";
import { BusyLabel } from "@/components/ui/Busy";
import { SiberAuditPanel, SiberDeletedBadge, type SiberAuditInfo } from "@/components/shared/SiberAudit";
import { RecordHistoryTab } from "@/components/shared/RecordHistory";
import {
  listDrafts, saveDraft, removeDraft, newDraftId, formatDraftTime, type Draft,
} from "@/lib/autodraft";

/** Teklifsiz yük taslakları — bkz. lib/autodraft.ts (çoklu). */
const DIRECT_DRAFT_KEY = "ols.directLoad.drafts.v2";

interface NamedRef {
  id: number;
  name: string | null;
}

interface LoadTransferItem {
  siber_deleted_at?: string | null;
  id: number;
  load_number: string | null;
  load_number_work_type: string | null;
  created_at: string | null;
  customer_id: NamedRef | null;
  sender_id: NamedRef | null;
  receiver_id: NamedRef | null;
  load_status_id: NamedRef | null;
  usercode_with_notification: NamedRef | null;
  work_type: NamedRef | null;
}

interface PackageDetail {
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

interface InvoiceItemDetail {
  id: number;
  buysell: string | null;
  net_price: number | null;
  total_price: number | null;
  quantity: number | null;
  description: string | null;
  status: string | null;
  item_id: FinancialItemOption | null;
  account_id: AccountOption | null;
  currency_code: NamedRef | null;
}

interface LoadTransferDetail extends LoadTransferItem {
  siber_audit?: SiberAuditInfo | null;
  receiver_id: NamedRef | null;
  romork_type_id: NamedRef | null;
  department_id: NamedRef | null;
  payment_type_id: NamedRef | null;
  total_gross_weight: number | null;
  total_volume: number | null;
  total_lademeter: number | null;
  total_lademeter_m3: number | null;
  total_cap: number | null;
  weight_fee: number | null;
  in_truck: number | null;
  in_tail: number | null;
  cmr_waiting: number | null;
  fcr_waiting: number | null;
  instruction_arrival_date: string | null;
  request_arrival_date: string | null;
  readiness_date: string | null;
  date_of_receipt_customer: string | null;
  load_transfer_package: PackageDetail[];
  load_transfer_invoice_item: InvoiceItemDetail[];
  customer_representative: UserOption | null;
  second_customer_representative: UserOption | null;
  load_id: number | null;
  load_file: LoadFileDetail[];
  /** Siber'in FTP arşivindeki evraklar — sahibi Siber, salt görüntüleme. */
  siber_archive: SiberArchiveFile[];
  /**
   * Bu yükün bağlı olduğu sefer(ler). Liste, çünkü canlıda 143 yük birden
   * fazla sefere bağlı (yük aktarma) — tek alan olsaydı ikincisi kaybolurdu.
   */
  expeditions: LinkedExpedition[];
  invoices: InvoiceSummaryDetail[];
  load_type_id: NamedRef | null;
  instruction_id: NamedRef | null;
  delivery_method_id: NamedRef | null;
  load_transfer_type_id: NamedRef | null;
  way_of_working: number | null;
  front_transportation_by_us: number | null;
  final_transportation_by_us: number | null;
  departure_country_id: { id: string; name: string | null } | null;
  target_country_id: { id: string; name: string | null } | null;
}

interface LinkedExpedition {
  id: number;
  expedition_number: string | null;
  upload_unload: number | null;
  date: string | null;
  plate_number: string | null;
}

interface SiberArchiveFile {
  id: string;
  name: string | null;
  description: string | null;
  created_at: string | null;
  created_by: string | null;
  personal_data: boolean;
  restricted_groups: string | null;
}

interface LoadFileDetail {
  id: number;
  file: string | null;
  org_name: string | null;
}

interface InvoiceSummaryDetail {
  id: number;
  invoice_id: string | null;
  box_type: number;
  commercial_type: number;
  target_title: string | null;
  target_identity_no: string | null;
  invoice_execution_date: string | null;
  invoice_status: NamedRef | null;
  invoice_type: NamedRef | null;
  payable_amount: number | null;
  tax_exclusive_amount: number | null;
  tax_amount: number | null;
  tax_rate: number | null;
  document_currency_code: string | null;
}

// olsold: system_data.js invoice_commercial_type / invoice_box_types.
const INVOICE_COMMERCIAL_TYPE_LABELS: Record<number, string> = { 0: "Temel Fatura", 1: "Ticari Fatura", 4: "E-Arşiv" };
const invoiceMoney = (value: number | null) =>
  (value ?? 0).toLocaleString("tr-TR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

interface MovementUserDetail {
  id: number;
  name: string | null;
  surname: string | null;
  email: string | null;
}

interface MovementDetail {
  id: number;
  description: string | null;
  address: string | null;
  created_at: string | null;
  deleted_at: string | null;
  destination: NamedRef | null;
  user: MovementUserDetail | null;
  expedition_status: NamedRef | null;
  expedition_movement: { expedition?: { expedition_number: string | null } } | null;
}

const movementUserLabel = (u: MovementUserDetail | null) =>
  u ? `${u.name ?? ""} ${u.surname ?? ""} (${u.email ?? ""})`.trim() : "—";

interface DocumentDetail {
  id: number;
  load_transfer_id: number;
  evrak_turu_id: number | null;
  evrak_turu_name: string | null;
  document_number: string | null;
  date: string | null;
  original_count: number | null;
  copy_count: number | null;
  delivered_to: string | null;
  delivered_at: string | null;
  note: string | null;
  created_at: string | null;
}

const EMPTY_DOCUMENT_FORM = {
  evrak_turu_id: "", document_number: "", date: "", original_count: "", copy_count: "",
  delivered_to: "", delivered_at: "", note: "",
};

type PackageRow = {
  id: number | null; product_type_id: LookupOption | null; case_type_id: LookupOption | null; quantity: string;
  gross_weight: string; net_weight: string; volume: string; lademeter: string;
  width: string; height: string; length: string; stackable: string;
};

const EMPTY_PACKAGE_ROW: PackageRow = {
  id: null, product_type_id: null, case_type_id: null, quantity: "1", gross_weight: "", net_weight: "",
  volume: "", lademeter: "", width: "", height: "", length: "", stackable: "1",
};

type InvoiceItemRow = {
  id: number | null; item_id: FinancialItemOption | null; account: AccountOption | null; currency_code: string;
  buysell: string; quantity: string; net_price: string; total_price: string; description: string;
  status: string;
};

const EMPTY_INVOICE_ITEM_ROW: InvoiceItemRow = {
  id: null, item_id: null, account: null, currency_code: "", buysell: "1",
  quantity: "1", net_price: "", total_price: "", description: "", status: "pending",
};

// olsold: system_data.js financial_item_status_type — buysell=1 (Alış) ise "Faturası
// Kesildi" (satış kavramı), aksi hâlde "Faturası Geldi" (alış kavramı) filtrelenir.
const FINANCIAL_ITEM_STATUS_OPTIONS = [
  { value: "pending", label: "Bekleniyor" },
  { value: "invoice_received", label: "Faturası Geldi" },
  { value: "invoice_issued", label: "Faturası Kesildi" },
];

const EMPTY_MOVEMENT_FORM = { destination_id: "", expedition_status_id: "", description: "", address: "" };
const WAY_OF_WORKING_OPTIONS = [
  { value: "", label: "Seçiniz" },
  { value: "0", label: "Spot" },
  { value: "1", label: "Yıllık" },
];
const YES_NO_OPTIONS = [
  { value: "", label: "Seçiniz" },
  { value: "1", label: "Evet" },
  { value: "0", label: "Hayır" },
];

// En/boy (cm) -> lademetre. Referans Laravel uygulamasıyla aynı formül: (en * boy) / 24000.
function computeLademeter(widthCm: string, lengthCm: string): string {
  const w = parseFloat(widthCm);
  const l = parseFloat(lengthCm);
  return Number.isFinite(w) && Number.isFinite(l) && w > 0 && l > 0 ? ((w * l) / 24000).toFixed(2) : "";
}

const PER_PAGE = 24;
const TABS = ["Genel Bilgiler", "Paketler", "Finans", "Görevliler", "Hareketler", "Evrak Takibi", "Faturalar", "Dosya Arşivi", "İşlem Geçmişi"];
// İş Tipi ham id'leri seed'e göre değişebilir (bkz. QuotesPage STATUS_TABS notu),
// bu yüzden sekmeler workTypes listesinden AD ile eşleştirilir, sabit id kullanılmaz.
const WORK_TYPE_TABS = ["Tümü", "İhracat", "İthalat", "Transit", "Yurtiçi"];

function LoadCard({ row, index, onClick }: { row: LoadTransferItem; index: number; onClick: () => void }) {
  const loadNumber = row.load_number_work_type ?? row.load_number ?? `Y${row.id}`;
  const date = row.created_at ? new Date(row.created_at).toLocaleDateString("tr-TR") : null;
  const assigned = row.usercode_with_notification?.name?.trim();

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
            <Package size={16} />
          </div>
          <div className="min-w-0">
            <p className="font-mono text-xs font-semibold text-blue-600 truncate">{loadNumber}</p>
            {row.siber_deleted_at && <div className="mt-1"><SiberDeletedBadge deletedAt={row.siber_deleted_at} /></div>}
            {date && (
              <p className="text-[10px] text-gray-400 mt-0.5 flex items-center gap-1">
                <CalendarDays size={10} />
                {date}
              </p>
            )}
          </div>
        </div>
        <div className="flex items-center gap-1 shrink-0">
          {row.work_type?.name && (
            <span className="text-[10px] font-medium px-2 py-0.5 rounded-full bg-gray-100 text-gray-600">
              {row.work_type.name}
            </span>
          )}
          {row.load_status_id?.name && <Badge label={row.load_status_id.name} />}
        </div>
      </div>

      <div className="pt-3 border-t border-gray-100">
        <p className="text-[10px] font-semibold text-gray-400 uppercase tracking-wider mb-0.5">Müşteri</p>
        <p className="text-sm font-semibold text-gray-900 truncate">{row.customer_id?.name ?? "—"}</p>
      </div>

      {(row.sender_id?.name || row.receiver_id?.name) && (
        <div className="grid grid-cols-2 gap-3 pt-2.5 border-t border-gray-100">
          <div className="min-w-0">
            <p className="text-[10px] font-semibold text-gray-400 uppercase tracking-wider mb-0.5">Gönderici</p>
            <p className="text-xs text-gray-700 truncate">{row.sender_id?.name ?? "—"}</p>
          </div>
          <div className="min-w-0">
            <p className="text-[10px] font-semibold text-gray-400 uppercase tracking-wider mb-0.5">Alıcı</p>
            <p className="text-xs text-gray-700 truncate">{row.receiver_id?.name ?? "—"}</p>
          </div>
        </div>
      )}

      <div className="flex items-center gap-1.5 text-[11px] text-gray-500 pt-2.5 border-t border-gray-100 min-w-0">
        <User size={12} className="text-gray-400 shrink-0" />
        <span className="truncate">{assigned || "Görevli atanmadı"}</span>
      </div>
    </motion.div>
  );
}

/**
 * Katlanabilir kayıt satırı. Yük kartındaki Paketler ve Finans sekmeleri onlarca
 * satır içerebiliyor ve her satır tam formuyla açık duruyordu — liste okunamaz
 * hale geliyordu. Artık kapalıyken yalnızca satırı TANIMLAYAN iki bilgi görünür
 * (finansta kalem + fiyat, pakette ürün + adet), tıklanınca form açılır.
 *
 * Silme düğmesi kapalıyken de erişilebilir kalır; başlığa tıklamayla karışmasın
 * diye kendi tıklamasını durdurur.
 */
function CollapsibleRow({
  title, summary, open, onToggle, onRemove, removeTitle, children,
}: {
  title: string;
  summary: string;
  open: boolean;
  onToggle: () => void;
  onRemove?: () => void;
  removeTitle?: string;
  children: React.ReactNode;
}) {
  return (
    <div className="border border-gray-200 rounded-lg mb-2 overflow-hidden">
      <div
        role="button"
        tabIndex={0}
        onClick={onToggle}
        onKeyDown={(e) => {
          if (e.key === "Enter" || e.key === " ") { e.preventDefault(); onToggle(); }
        }}
        className={clsx(
          "flex items-center gap-2 px-3 py-2 cursor-pointer select-none",
          open ? "bg-gray-50 border-b border-gray-200" : "hover:bg-gray-50",
        )}
      >
        {open ? <ChevronUp size={14} className="text-gray-400 shrink-0" /> : <ChevronDown size={14} className="text-gray-400 shrink-0" />}
        <span className="text-xs font-medium text-gray-800 truncate">{title}</span>
        <span className="ml-auto text-xs font-semibold text-gray-600 shrink-0 tabular-nums">{summary}</span>
        {onRemove && (
          <button
            type="button"
            title={removeTitle}
            onClick={(e) => { e.stopPropagation(); onRemove(); }}
            className="text-gray-300 hover:text-red-500 shrink-0"
          >
            <Trash2 size={13} />
          </button>
        )}
      </div>
      {open && <div className="p-4">{children}</div>}
    </div>
  );
}

export function LoadsPage() {
  const { can } = useAuth();
  const { addToast } = useToast();
  // DERİN BAĞLANTI: /yukler?yuk=<id> ile doğrudan o yükün kartı açılır.
  // Sefer ekranındaki "Yüke Git" düğmesi buraya yönlendiriyor. Yük listede
  // hangi sayfada olursa olsun çalışır — openDetail kaydı id ile tek tek çeker.
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();

  const canCreate = can("load_management", "create");
  const canUpdate = can("load_management", "update");
  const canDelete = can("load_management", "delete");

  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);
  const [workTypeTab, setWorkTypeTab] = useState(WORK_TYPE_TABS[0]);
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [fCustomer, setFCustomer] = useState<AccountOption | null>(null);
  const [fSender, setFSender] = useState<AccountOption | null>(null);
  const [fReceiver, setFReceiver] = useState<AccountOption | null>(null);
  const [fAssignedUser, setFAssignedUser] = useState<UserOption | null>(null);
  const [fStatusId, setFStatusId] = useState("");
  const [fCaseTypeId, setFCaseTypeId] = useState("");
  const [fFinancialItem, setFFinancialItem] = useState("");
  const [fWeight, setFWeight] = useState("");
  const debouncedFinancialItem = useDebouncedValue(fFinancialItem);
  const debouncedWeight = useDebouncedValue(fWeight);
  const hasActiveAdvancedFilters = !!(dateFrom || dateTo || fCustomer || fSender || fReceiver ||
    fAssignedUser || fStatusId || fCaseTypeId || fFinancialItem || fWeight);
  const hasActiveFilters = !!(search || hasActiveAdvancedFilters);
  const [showAdvanced, setShowAdvanced] = useState(false);

  function clearFilters() {
    setSearch(""); setDateFrom(""); setDateTo("");
    setFCustomer(null); setFSender(null); setFReceiver(null); setFAssignedUser(null);
    setFStatusId(""); setFCaseTypeId(""); setFFinancialItem(""); setFWeight("");
    setPage(1);
  }
  const [page, setPage] = useState(1);
  // Siber'de silinmiş kayıtlar normalde gizli; bu süzgeç açıldığında YALNIZCA
  // onlar listelenir ("ne silinmiş?" sorusuna tek tıkla cevap).
  const [onlyDeleted, setOnlyDeleted] = useState(false);
  const [rows, setRows] = useState<LoadTransferItem[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);

  const [drawerOpen, setDrawerOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [tab, setTab] = useState(TABS[0]);
  const [detail, setDetail] = useState<LoadTransferDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState(false);

  const [form, setForm] = useState({
    load_status_id: "", payment_type_id: "", department_id: "", romork_type_id: "",
    instruction_arrival_date: "", request_arrival_date: "", readiness_date: "", date_of_receipt_customer: "",
    load_type_id: "", instruction_id: "", delivery_method_id: "", load_transfer_type_id: "",
    way_of_working: "", front_transportation_by_us: "", final_transportation_by_us: "",
  });
  const [customer, setCustomer] = useState<AccountOption | null>(null);
  const [sender, setSender] = useState<AccountOption | null>(null);
  const [receiver, setReceiver] = useState<AccountOption | null>(null);
  const [departureCountry, setDepartureCountry] = useState("");
  const [targetCountry, setTargetCountry] = useState("");
  const [customerRep, setCustomerRep] = useState<UserOption | null>(null);
  const [secondCustomerRep, setSecondCustomerRep] = useState<UserOption | null>(null);
  const [packages, setPackages] = useState<PackageRow[]>([]);
  const [removedPackageIds, setRemovedPackageIds] = useState<number[]>([]);

  // Açık satırlar. Anahtar olarak DİZİN kullanılıyor (satırların kalıcı bir
  // kimliği yok, yeni eklenenlerde id null). Bir satır silinince dizinler
  // kaydığı için küme TEMİZLENİR — aksi hâlde yanlış satır açık kalırdı.
  const [openPackages, setOpenPackages] = useState<Set<number>>(new Set());
  const [openInvoiceItems, setOpenInvoiceItems] = useState<Set<number>>(new Set());

  function toggleIn(set: Set<number>, i: number) {
    const next = new Set(set);
    if (!next.delete(i)) next.add(i);
    return next;
  }
  const [invoiceItems, setInvoiceItems] = useState<InvoiceItemRow[]>([]);
  const [movements, setMovements] = useState<MovementDetail[]>([]);
  const [deletedMovements, setDeletedMovements] = useState<MovementDetail[]>([]);
  const [deletedMovementsModalOpen, setDeletedMovementsModalOpen] = useState(false);
  const [movementModalOpen, setMovementModalOpen] = useState(false);
  const [movementForm, setMovementForm] = useState(EMPTY_MOVEMENT_FORM);
  const [savingMovement, setSavingMovement] = useState(false);
  const [documents, setDocuments] = useState<DocumentDetail[]>([]);
  const [documentModalOpen, setDocumentModalOpen] = useState(false);
  const [documentForm, setDocumentForm] = useState(EMPTY_DOCUMENT_FORM);
  const [editingDocumentId, setEditingDocumentId] = useState<number | null>(null);
  const [savingDocument, setSavingDocument] = useState(false);
  const [existingFiles, setExistingFiles] = useState<LoadFileDetail[]>([]);
  const [removedFileIds, setRemovedFileIds] = useState<number[]>([]);
  const [newFiles, setNewFiles] = useState<File[]>([]);
  const [savingFiles, setSavingFiles] = useState(false);

  const { options: workTypes } = useLookupOptions("/api/v1/work_type");
  const { options: loadStatusTypes } = useLookupOptions("/api/v1/load_status_type");
  const { options: paymentTypes } = useLookupOptions("/api/v1/payment_type");
  const { options: departments } = useLookupOptions("/api/v1/department");
  const { options: romorkTypes } = useLookupOptions("/api/v1/romork_type");
  const { options: caseTypes } = useLookupOptions("/api/v1/case_type");
  // olsold: LoadFormDrawer.vue — Mali Kalem alanının "Yeni Ekle" düğmesi kaynakta
  // doğru bağlı (RealLoad/LoadFormFinancialItem.vue ile aynı), Teklif'in kendi
  // eşdeğerinden farklı olarak burada devre dışı DEĞİL.
  const [financialItemModalOpen, setFinancialItemModalOpen] = useState(false);
  const { options: currencies } = useLookupOptions("/api/v1/currency");
  const { options: destinations } = useLookupOptions("/api/v1/destination");
  const { options: expeditionStatuses } = useLookupOptions("/api/v1/expedition_status");
  const { options: evrakTurleri } = useLookupOptions("/api/v1/evrak_turu");
  const { options: loadingTypes } = useLookupOptions("/api/v1/loading_type");
  const { options: instructions } = useLookupOptions("/api/v1/instruction");
  const { options: deliveryMethods } = useLookupOptions("/api/v1/load_transfer_deliver_method");
  const { options: loadTransferTypes } = useLookupOptions("/api/v1/load_transfer_type");
  const { options: countries } = useLookupOptions("/api/v1/country");

  // TEKLİFSİZ YÜK AÇMA — teklif modülünü KULLANMAYAN şirketin yolu (Avrora).
  // OLS'te her yük bir teklifin dönüşümü olduğu için bu düğme görünmez.
  // Kararı sunucu veriyor (bkz. DirectLoadService.CanCreateAsync); arayüz
  // kendi başına karar vermiyor, sadece uca soruyor.
  const [canDirect, setCanDirect] = useState(false);
  const [directOpen, setDirectOpen] = useState(false);
  const [directSaving, setDirectSaving] = useState(false);
  const [directForm, setDirectForm] = useState({
    work_type_id: "", loading_type_id: "", load_transfer_type_id: "",
    instruction_id: "", romork_type_id: "", payment_type_id: "", department_id: "",
    delivery_method_id: "", payer_company: "",
    departure_country_id: "", transit_country_id: "", target_country_id: "",
    front_transportation_by_us: "0", final_transportation_by_us: "0", way_of_working: "0",
    instruction_arrival_date: "", request_arrival_date: "", readiness_date: "",
    description: "",
  });
  const [directCustomer, setDirectCustomer] = useState<AccountOption | null>(null);
  const [directSender, setDirectSender] = useState<AccountOption | null>(null);
  const [directReceiver, setDirectReceiver] = useState<AccountOption | null>(null);
  const [directAgent, setDirectAgent] = useState<AccountOption | null>(null);
  const [directFreightPayer, setDirectFreightPayer] = useState<AccountOption | null>(null);
  const [directPackages, setDirectPackages] = useState<PackageRow[]>([{ ...EMPTY_PACKAGE_ROW }]);
  const [directItems, setDirectItems] = useState<InvoiceItemRow[]>([]);

  // DOSYA ARŞİVİ — yük oluşmadan dosya yüklenemiyor: arşiv kaydı yükün Siber
  // kimliğine bağlanıyor ve o kimlik ancak kayıt sırasında oluşuyor. Bu yüzden
  // dosyalar burada tutulup kayıttan SONRA gönderiliyor.
  const [directFiles, setDirectFiles] = useState<File[]>([]);
  const [dragOver, setDragOver] = useState(false);
  const directFileInput = useRef<HTMLInputElement | null>(null);

  function addDirectFiles(list: FileList | null) {
    if (!list || list.length === 0) return;
    // Aynı dosya iki kez eklenmesin (ad + boyut yeterli ayırt edici).
    setDirectFiles((prev) => {
      const key = (f: File) => `${f.name}:${f.size}`;
      const seen = new Set(prev.map(key));
      return [...prev, ...Array.from(list).filter((f) => !seen.has(key(f)))];
    });
  }

  // TASLAK OTOMATİKTİR — elle "kaydet" yok.
  //
  // Eskiden adlı taslaklar ve bir "Taslak Kaydet" düğmesi vardı; kullanıcı
  // kaydetmeyi unutursa emeği yine kayboluyordu. Artık form her değiştiğinde
  // kendiliğinden yazılıyor, kullanıcı yanlışlıkla çıksa da kaldığı yerden
  // devam edebiliyor ve isterse taslağı siliyor. Teklif ekranındaki davranışın
  // aynısı (bkz. QuotesPage / lib/autodraft.ts).
  // Taslak LİSTE ekranından da görünür — teklif ve seferdeki "Taslaklar"
  // menüsünün aynısı. Kullanıcı çekmeceyi açmadan taslağı olduğunu görsün,
  // oradan devam etsin ya da silsin.
  const [drafts, setDrafts] = useState<Draft<unknown>[]>(() => listDrafts(DIRECT_DRAFT_KEY));
  const [draftsOpen, setDraftsOpen] = useState(false);
  const draftsRef = useRef<HTMLDivElement>(null);

  // Açık düzenleme oturumunun taslak kimliği. "Yeni" YENİ kimlik açar (önceki
  // taslak durur), taslaktan devam edilince O kimlik benimsenir (çoğalmaz).
  const activeDraftId = useRef<string>(newDraftId());

  /** Formun tüm durumu tek nesnede — taslak da kurtarma da bunu saklar. */
  const directSnapshot = useCallback(() => ({
    form: directForm,
    customer: directCustomer,
    sender: directSender,
    receiver: directReceiver,
    agent: directAgent,
    freightPayer: directFreightPayer,
    packages: directPackages,
    items: directItems,
  }), [directForm, directCustomer, directSender, directReceiver,
       directAgent, directFreightPayer, directPackages, directItems]);

  /** Anlık görüntüyü forma geri yükler. */
  const applySnapshot = useCallback((raw: unknown) => {
    const snap = raw as ReturnType<typeof directSnapshot> | null;
    if (!snap) return;

    if (snap.form) setDirectForm(snap.form);
    setDirectCustomer(snap.customer ?? null);
    setDirectSender(snap.sender ?? null);
    setDirectReceiver(snap.receiver ?? null);
    setDirectAgent(snap.agent ?? null);
    setDirectFreightPayer(snap.freightPayer ?? null);
    setDirectPackages(snap.packages?.length ? snap.packages : [{ ...EMPTY_PACKAGE_ROW }]);
    setDirectItems(snap.items ?? []);
  }, []);

  // Form açıkken her değişiklikte kaza kurtarma kopyası yazılır. Kaydedilmemiş
  // veri sekme kapanmasında/elektrik kesintisinde kaybolmasın.
  useEffect(() => {
    if (!directOpen) return;
    const snap = directSnapshot();
    // Boş formu taslak yapma: hiç dolu alan yoksa yazma.
    if (!JSON.stringify(snap).match(/:"[^"]+"/)) return;

    const timer = setTimeout(() => {
      saveDraft(DIRECT_DRAFT_KEY, activeDraftId.current, snap);
      setDrafts(listDrafts(DIRECT_DRAFT_KEY));
    }, 600);
    return () => clearTimeout(timer);
  }, [directOpen, directSnapshot]);

  // Taslaklar menüsü dışına tıklanınca kapansın.
  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (draftsRef.current && !draftsRef.current.contains(e.target as Node)) setDraftsOpen(false);
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  /**
   * Taslak satırında gösterilecek ad. Snapshot opak taşındığı için müşteri
   * adı burada güvenli biçimde çıkarılır; yoksa genel bir etiket kullanılır.
   */
  function draftLabel(payload: unknown): string {
    const snap = payload as { customer?: { name?: string | null } | null } | null;
    return snap?.customer?.name?.trim() || "Teklifsiz yük";
  }

  /** Taslaktan devam: o taslağın kimliğini benimseyip formu doldurur. */
  function resumeDirectDraft(draft: Draft<unknown>) {
    activeDraftId.current = draft.id;
    applySnapshot(draft.payload);
    setDirectOpen(true);
    setDraftsOpen(false);
  }

  function discardDirectDraft(id: string) {
    removeDraft(DIRECT_DRAFT_KEY, id);
    setDrafts(listDrafts(DIRECT_DRAFT_KEY));
  }

  /**
   * Formu boşaltır — taslağı SİLMEZ. "Teklifsiz Yük Aç" temiz bir form açar;
   * yarım kalan iş Taslaklar menüsünden geri alınır (teklif ve seferdeki
   * "Yeni …" düğmelerinin davranışının aynısı).
   */
  function resetDirectForm() {
    setDirectForm({
      work_type_id: "", loading_type_id: "", load_transfer_type_id: "",
      instruction_id: "", romork_type_id: "", payment_type_id: "", department_id: "",
      delivery_method_id: "", payer_company: "",
      departure_country_id: "", transit_country_id: "", target_country_id: "",
      front_transportation_by_us: "0", final_transportation_by_us: "0", way_of_working: "0",
      instruction_arrival_date: "", request_arrival_date: "", readiness_date: "",
      description: "",
    });
    setDirectCustomer(null); setDirectSender(null); setDirectReceiver(null);
    setDirectAgent(null); setDirectFreightPayer(null);
    setDirectPackages([{ ...EMPTY_PACKAGE_ROW }]);
    setDirectItems([]);
    setDirectFiles([]);
  }


  useEffect(() => {
    api
      .get<{ data: { allowed: boolean } }>("/api/v1/load_transfer/direct/allowed")
      .then((res) => setCanDirect(res.data.allowed))
      .catch(() => setCanDirect(false));
  }, []);

  async function submitDirectLoad() {
    if (directSaving) return;
    setDirectSaving(true);
    try {
      const res = await api.post<{ data: { yuk_no: string; id: number | null }; message: string }>(
        "/api/v1/load_transfer/direct", {
          work_type_id: int(directForm.work_type_id),
          loading_type_id: int(directForm.loading_type_id),
          load_transfer_type_id: int(directForm.load_transfer_type_id),
          instruction_id: int(directForm.instruction_id),
          romork_type_id: int(directForm.romork_type_id),
          payment_type_id: int(directForm.payment_type_id),
          department_id: int(directForm.department_id),
          customer_id: directCustomer?.id ?? null,
          sender_id: directSender?.id ?? null,
          receiver_id: directReceiver?.id ?? null,
          delivery_method_id: int(directForm.delivery_method_id),
          agent_id: directAgent?.id ?? null,
          company_pay_freight_id: directFreightPayer?.id ?? null,
          payer_company: directForm.payer_company || null,
          departure_country_id: directForm.departure_country_id || null,
          transit_country_id: directForm.transit_country_id || null,
          target_country_id: directForm.target_country_id || null,
          front_transportation_by_us: int(directForm.front_transportation_by_us) ?? 0,
          final_transportation_by_us: int(directForm.final_transportation_by_us) ?? 0,
          way_of_working: int(directForm.way_of_working) ?? 0,
          instruction_arrival_date: directForm.instruction_arrival_date || null,
          request_arrival_date: directForm.request_arrival_date || null,
          readiness_date: directForm.readiness_date || null,
          description: directForm.description || null,
          packages: directPackages.map((p) => ({
            product_type_id: p.product_type_id?.id ?? null,
            case_type_id: p.case_type_id?.id ?? null,
            quantity: int(p.quantity),
            gross_weight: num(p.gross_weight),
            net_weight: num(p.net_weight),
            volume: num(p.volume),
            lademeter: num(p.lademeter),
            width: num(p.width),
            height: num(p.height),
            length: num(p.length),
            stackable: int(p.stackable),
          })),
          financial_items: directItems
            .filter((f) => f.item_id)
            .map((f) => ({
              item_id: f.item_id?.id ?? null,
              account_id: f.account?.id ?? null,
              currency_id: int(f.currency_code),
              net_price: num(f.net_price),
              quantity: num(f.quantity),
              description: f.description || null,
            })),
        });

      addToast(`Yük oluşturuldu: ${res.data.yuk_no}`);

      // Dosyalar ancak yük oluştuktan sonra arşive gidebiliyor. Yükleme
      // başarısız olsa bile YÜK OLUŞMUŞ durumda — bu yüzden hata akışı
      // durdurmuyor, yalnızca bildiriliyor.
      if (directFiles.length > 0 && res.data.id) {
        try {
          const fd = new FormData();
          for (const file of directFiles) fd.append("files", file);

          const up = await api.postForm<{ data: { uploaded: number; total: number }; message: string }>(
            `/api/v1/load_transfer/${res.data.id}/archive`, fd);

          addToast(up.message, up.data.uploaded === up.data.total ? "success" : "error");
        } catch {
          addToast("Yük oluştu ancak dosyalar arşive gönderilemedi", "error");
        }
      }

      // Kayıt Siber'e gittiğine göre YALNIZCA bu taslak gereksiz; diğerleri durur.
      removeDraft(DIRECT_DRAFT_KEY, activeDraftId.current);
      setDrafts(listDrafts(DIRECT_DRAFT_KEY));
      activeDraftId.current = newDraftId();

      setDirectOpen(false);
      resetDirectForm();
      load();
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Yük oluşturulamadı", "error");
    } finally {
      setDirectSaving(false);
    }
  }

  /**
   * Tek sayfalık formda bölüm başlığı. Form eskiden sekmeliydi; sekmeler
   * kaldırılınca bölümlerin gözle ayrılması gerekti — kullanıcı artık tek
   * ekranda yukarıdan aşağı dolduruyor.
   */
  function SectionTitle({ children }: { children: React.ReactNode }) {
    return (
      <h3 className="mb-5 border-b border-gray-200 pb-2.5 text-[11px] font-semibold uppercase tracking-wider text-gray-500">
        {children}
      </h3>
    );
  }

  function opts(list: { id: string | number; name: string }[]) {
    return [{ value: "", label: "Seçiniz" }, ...list.map((t) => ({ value: String(t.id), label: t.name }))];
  }

  /**
   * Kodu olan tanımlar için "EXW — Fabrika çıkışında teslim" biçimi.
   *
   * Teslim şekli Siber'de Incoterm KODUYLA tutuluyor (skn_yuk.teslimsekil =
   * EXW/FOB/CIF...) ve kullanıcı da işini bu kodlarla konuşuyor. Liste yalnızca
   * açıklamayı gösterdiği için doğru seçeneği bulmak zordu.
   */
  function codeOpts(list: { id: string | number; name: string; edikod?: string | null; code?: string | null }[]) {
    return [
      { value: "", label: "Seçiniz" },
      ...list.map((t) => {
        const code = (t.edikod ?? t.code ?? "").trim();
        return { value: String(t.id), label: code ? `${code} — ${t.name}` : t.name };
      }),
    ];
  }

  // İş Tipi sekmeleri de STATUS_TABS deseniyle aynı sebepten (bkz. QuotesPage)
  // AD ile eşleşiyor, ham id'ye güvenilmiyor.
  const activeWorkTypeId = workTypeTab === "Tümü" ? undefined : workTypes.find((w) => w.name === workTypeTab)?.id;

  const loadRequestRef = useRef(0);

  function load() {
    // Yarış durumu koruması: örn. sayfa yeni açılırken filtresiz ilk istek,
    // hemen ardından yazılan aramanın filtreli isteğinden GEÇ dönerse eski
    // yanıt state'i ezip arama sonucunu yok sayıyordu. Sadece en son
    // başlatılan isteğin yanıtı uygulanır.
    const requestId = ++loadRequestRef.current;
    setLoading(true);
    api
      .get<DataMessage<Paginated<LoadTransferItem>>>("/api/v1/load_transfer", {
        search: debouncedSearch || undefined,
        work_type_id: activeWorkTypeId || undefined,
        date_from: dateFrom || undefined,
        date_to: dateTo || undefined,
        customer_id: fCustomer?.id || undefined,
        sender_id: fSender?.id || undefined,
        receiver_id: fReceiver?.id || undefined,
        assigned_user_id: fAssignedUser?.id || undefined,
        status_id: fStatusId || undefined,
        case_type_id: fCaseTypeId || undefined,
        financial_item: debouncedFinancialItem || undefined,
        weight: debouncedWeight || undefined,
        only_deleted: onlyDeleted || undefined,
        per_page: PER_PAGE,
        page,
      })
      .then((res) => {
        if (requestId !== loadRequestRef.current) return;
        setRows(res.data.data);
        setTotal(res.data.total);
      })
      .catch(() => {
        if (requestId === loadRequestRef.current) addToast("Yük listesi yüklenemedi", "error");
      })
      .finally(() => {
        if (requestId === loadRequestRef.current) setLoading(false);
      });
  }

  // ?yuk=<id> geldiyse o yükün kartını aç.
  //
  // Sefer ekranındaki ile AYNI desen ve aynı gerekçe: parametreyi kartı açmadan
  // önce silmek, aynı tik içinde gezinme tetikleyip kartı açan durum
  // güncellemesiyle yarışıyordu. Önce aç, parametreyi kart kapanınca temizle.
  const handledLoadRef = useRef<string | null>(null);

  useEffect(() => {
    const requested = searchParams.get("yuk");

    if (!requested) {
      handledLoadRef.current = null;
      return;
    }

    if (handledLoadRef.current === requested) return;
    handledLoadRef.current = requested;

    const id = Number(requested);
    if (Number.isFinite(id) && id > 0) openDetail(id);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchParams]);

  /** Kart kapanınca derin bağlantı parametresini temizle. */
  function clearLoadDeepLink() {
    if (!searchParams.get("yuk")) return;

    setSearchParams((current) => {
      const next = new URLSearchParams(current);
      next.delete("yuk");
      return next;
    }, { replace: true });
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    debouncedSearch, workTypeTab, workTypes.length, dateFrom, dateTo, page,
    fCustomer, fSender, fReceiver, fAssignedUser,
    fStatusId, fCaseTypeId, debouncedFinancialItem, debouncedWeight, onlyDeleted,
  ]);

  function fetchMovements(loadTransferId: number) {
    api
      .get<{ data: MovementDetail[]; deleted_movements: MovementDetail[] }>("/api/v1/load_transfer_movement", { load_transfer_id: loadTransferId })
      .then((res) => { setMovements(res.data); setDeletedMovements(res.deleted_movements ?? []); })
      .catch(() => { setMovements([]); setDeletedMovements([]); });
  }

  function fetchDocuments(loadTransferId: number) {
    api
      .get<{ data: DocumentDetail[] }>("/api/v1/load_transfer_document", { load_transfer_id: loadTransferId })
      .then((res) => setDocuments(res.data))
      .catch(() => setDocuments([]));
  }

  async function openDetail(id: number) {
    setEditingId(id);
    setTab(TABS[0]);
    setDrawerOpen(true);
    setDetailLoading(true);
    setRemovedPackageIds([]);
    setOpenPackages(new Set());
    setOpenInvoiceItems(new Set());
    setRemovedFileIds([]);
    setNewFiles([]);
    fetchMovements(id);
    fetchDocuments(id);
    try {
      const res = await api.get<DataMessage<LoadTransferDetail>>(`/api/v1/load_transfer/${id}`);
      const d = res.data;
      setDetail(d);
      setForm({
        load_status_id: d.load_status_id ? String(d.load_status_id.id) : "",
        payment_type_id: d.payment_type_id ? String(d.payment_type_id.id) : "",
        department_id: d.department_id ? String(d.department_id.id) : "",
        romork_type_id: d.romork_type_id ? String(d.romork_type_id.id) : "",
        instruction_arrival_date: d.instruction_arrival_date ?? "",
        request_arrival_date: d.request_arrival_date ?? "",
        readiness_date: d.readiness_date ?? "",
        date_of_receipt_customer: d.date_of_receipt_customer ?? "",
        load_type_id: d.load_type_id ? String(d.load_type_id.id) : "",
        instruction_id: d.instruction_id ? String(d.instruction_id.id) : "",
        delivery_method_id: d.delivery_method_id ? String(d.delivery_method_id.id) : "",
        load_transfer_type_id: d.load_transfer_type_id ? String(d.load_transfer_type_id.id) : "",
        way_of_working: d.way_of_working != null ? String(d.way_of_working) : "",
        front_transportation_by_us: d.front_transportation_by_us != null ? String(d.front_transportation_by_us) : "",
        final_transportation_by_us: d.final_transportation_by_us != null ? String(d.final_transportation_by_us) : "",
      });
      setCustomer(d.customer_id);
      setSender(d.sender_id);
      setReceiver(d.receiver_id);
      setCustomerRep(d.customer_representative);
      setSecondCustomerRep(d.second_customer_representative);
      setDepartureCountry(d.departure_country_id?.id ?? "");
      setTargetCountry(d.target_country_id?.id ?? "");
      setExistingFiles(d.load_file);
      setPackages(
        d.load_transfer_package.map((p) => ({
          id: p.id,
          product_type_id: p.product_type_id,
          case_type_id: p.case_type_id,
          quantity: p.quantity != null ? String(p.quantity) : "",
          gross_weight: p.gross_weight != null ? String(p.gross_weight) : "",
          net_weight: p.net_weight != null ? String(p.net_weight) : "",
          volume: p.volume != null ? String(p.volume) : "",
          lademeter: p.lademeter != null ? String(p.lademeter) : "",
          width: p.width != null ? String(p.width) : "",
          height: p.height != null ? String(p.height) : "",
          length: p.length != null ? String(p.length) : "",
          stackable: p.stackable != null ? String(p.stackable) : "1",
        })),
      );
      setInvoiceItems(
        d.load_transfer_invoice_item.map((f) => ({
          id: f.id,
          item_id: f.item_id,
          account: f.account_id,
          currency_code: f.currency_code ? String(f.currency_code.id) : "",
          buysell: f.buysell ?? "1",
          quantity: f.quantity != null ? String(f.quantity) : "1",
          net_price: f.net_price != null ? String(f.net_price) : "",
          total_price: f.total_price != null ? String(f.total_price) : "",
          description: f.description ?? "",
          status: f.status || "pending",
        })),
      );
    } catch {
      addToast("Yük bilgileri yüklenemedi", "error");
      setDrawerOpen(false);
    } finally {
      setDetailLoading(false);
    }
  }

  /**
   * Siber arşiv dosyasını açar. Dosya API üzerinden VEKİL olarak geliyor
   * (FTP adresi/parolası tarayıcıya hiç verilmiyor) ve jetonlu istek gerektiği
   * için blob'a alınıp öyle açılıyor. PDF/görsel yeni sekmede görüntülenir,
   * diğer türler indirilir.
   */
  async function openArchiveFile(file: SiberArchiveFile) {
    try {
      const { blob } = await downloadFile(`/api/v1/load_transfer/archive/${encodeURIComponent(file.id)}`);
      const url = URL.createObjectURL(blob);
      const viewable = blob.type === "application/pdf" || blob.type.startsWith("image/");

      if (viewable) {
        window.open(url, "_blank", "noopener");
      } else {
        const link = document.createElement("a");
        link.href = url;
        link.download = file.name ?? "evrak";
        link.click();
      }

      // Sekme/indirme URL'i tükettikten sonra bellekten düşür.
      setTimeout(() => URL.revokeObjectURL(url), 60_000);
    } catch {
      addToast("Evrak açılamadı", "error");
    }
  }

  function addPackageRow() {
    // Yeni satır AÇIK eklenir — kapalı gelseydi kullanıcı "Paket Ekle"ye basıp
    // dolduracak hiçbir alan göremezdi.
    setPackages((list) => {
      setOpenPackages((open) => new Set(open).add(list.length));
      return [...list, { ...EMPTY_PACKAGE_ROW }];
    });
  }

  async function removePackageRow(i: number) {
    const row = packages[i];
    if (row.id) {
      if (!window.confirm("Bu paket silinsin mi?")) return;
      try {
        await api.delete("/api/v1/load_transfer/load_transfer_package", { deletion_id: [row.id] });
        setRemovedPackageIds((ids) => [...ids, row.id!]);
      } catch (err) {
        addToast(err instanceof Error ? err.message : "Paket silinemedi", "error");
        return;
      }
    }
    setOpenPackages(new Set());
    setPackages((list) => list.filter((_, xi) => xi !== i));
  }

  function addInvoiceItemRow(buysell: string) {
    setInvoiceItems((list) => {
      setOpenInvoiceItems((open) => new Set(open).add(list.length));
      return [...list, { ...EMPTY_INVOICE_ITEM_ROW, buysell }];
    });
  }

  async function removeInvoiceItemRow(i: number) {
    const row = invoiceItems[i];
    if (row.id) {
      if (!window.confirm("Bu mali kalem silinsin mi?")) return;
      try {
        await api.delete("/api/v1/load_transfer/load_transfer_invoice_item", { deletion_id: [row.id] });
      } catch (err) {
        addToast(err instanceof Error ? err.message : "Mali kalem silinemedi", "error");
        return;
      }
    }
    setOpenInvoiceItems(new Set());
    setInvoiceItems((list) => list.filter((_, xi) => xi !== i));
  }

  function openMovementModal() {
    setMovementForm(EMPTY_MOVEMENT_FORM);
    setMovementModalOpen(true);
  }

  async function saveMovement() {
    if (!editingId || !detail) return;
    if (!movementForm.destination_id || !movementForm.expedition_status_id) {
      addToast("Lütfen konum ve durum seçiniz", "error");
      return;
    }
    setSavingMovement(true);
    try {
      const fd = new FormData();
      fd.append("load_number", detail.load_number_work_type ?? detail.load_number ?? "");
      fd.append("load_transfer_id", String(editingId));
      fd.append("destination_id", movementForm.destination_id);
      fd.append("expedition_status_id", movementForm.expedition_status_id);
      fd.append("description", movementForm.description);
      fd.append("address", movementForm.address);
      await api.postForm("/api/v1/load_transfer_movement", fd);
      addToast("Yük hareketi eklendi");
      setMovementModalOpen(false);
      fetchMovements(editingId);
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Hareket kaydedilemedi", "error");
    } finally {
      setSavingMovement(false);
    }
  }

  async function deleteMovement(id: number) {
    if (!editingId) return;
    if (!window.confirm("Bu hareket silinsin mi?")) return;
    try {
      await api.delete("/api/v1/load_transfer_movement", { id });
      fetchMovements(editingId);
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Hareket silinemedi", "error");
    }
  }

  function openDocumentModal() {
    setEditingDocumentId(null);
    setDocumentForm(EMPTY_DOCUMENT_FORM);
    setDocumentModalOpen(true);
  }

  function openEditDocumentModal(d: DocumentDetail) {
    setEditingDocumentId(d.id);
    setDocumentForm({
      evrak_turu_id: d.evrak_turu_id != null ? String(d.evrak_turu_id) : "",
      document_number: d.document_number ?? "",
      date: d.date ?? "",
      original_count: d.original_count != null ? String(d.original_count) : "",
      copy_count: d.copy_count != null ? String(d.copy_count) : "",
      delivered_to: d.delivered_to ?? "",
      delivered_at: d.delivered_at ?? "",
      note: d.note ?? "",
    });
    setDocumentModalOpen(true);
  }

  async function saveDocument() {
    if (!editingId) return;
    if (!documentForm.evrak_turu_id) {
      addToast("Lütfen evrak türü seçiniz", "error");
      return;
    }
    setSavingDocument(true);
    try {
      const body = {
        load_transfer_id: editingId,
        evrak_turu_id: Number(documentForm.evrak_turu_id),
        document_number: documentForm.document_number || null,
        date: documentForm.date || null,
        original_count: documentForm.original_count ? Number(documentForm.original_count) : null,
        copy_count: documentForm.copy_count ? Number(documentForm.copy_count) : null,
        delivered_to: documentForm.delivered_to || null,
        delivered_at: documentForm.delivered_at || null,
        note: documentForm.note || null,
      };
      if (editingDocumentId) {
        await api.post("/api/v1/load_transfer_document/update", { id: editingDocumentId, ...body });
        addToast("Evrak güncellendi");
      } else {
        await api.post("/api/v1/load_transfer_document", body);
        addToast("Evrak eklendi");
      }
      setDocumentModalOpen(false);
      fetchDocuments(editingId);
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Evrak kaydedilemedi", "error");
    } finally {
      setSavingDocument(false);
    }
  }

  async function deleteDocument(id: number) {
    if (!editingId) return;
    if (!window.confirm("Bu evrak kaydı silinsin mi?")) return;
    try {
      await api.delete("/api/v1/load_transfer_document", { id });
      fetchDocuments(editingId);
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Evrak silinemedi", "error");
    }
  }

  /**
   * Dosyalar Yük'ün DEĞİL, dönüştüğü orijinal Teklif'in kaydına yazılır
   * (olsold: LoadFormDrawer.vue updateLoadFiles → load_id gönderiyor).
   * Bağımsız bir kaydet aksiyonu — ana "Kaydet" butonuna bağlı değil.
   */
  async function saveFiles() {
    // Dosya ya teklife ya doğrudan yüke bağlanır. Eskiden yalnızca teklif
    // kimliği kabul ediliyordu ve teklifsiz yükte bu fonksiyon SESSİZCE
    // çıkıyordu — kullanıcı "Dosyaları Kaydet"e basıyor, hiçbir şey olmuyordu.
    // Canlıda 7.998 yükün 4.285'i teklifsiz, yani ekranın yarısından fazlası.
    if (!detail?.load_id && !editingId) {
      addToast("Yük kaydı açık değil", "error");
      return;
    }

    setSavingFiles(true);
    try {
      const fd = new FormData();
      if (detail?.load_id) fd.append("load_id", String(detail.load_id));
      else fd.append("load_transfer_id", String(editingId));
      existingFiles
        .filter((f) => !removedFileIds.includes(f.id))
        .forEach((f, i) => fd.append(`files[${i}][id]`, String(f.id)));
      newFiles.forEach((f, i) => fd.append(`files[${existingFiles.length + i}][file]`, f));
      await api.postForm("/api/v1/load/file/upload", fd);
      addToast("Dosyalar kaydedildi");
      setNewFiles([]);
      openDetail(editingId!);
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Dosyalar kaydedilemedi", "error");
    } finally {
      setSavingFiles(false);
    }
  }

  /**
   * Yükü siler. Backend bu işlemde yükü DOĞURAN TEKLİFİ de siler (yerelde ve
   * Siber'de) — aksi hâlde teklif "Olumlu" ve yük numarası dolu hâlde kalıp
   * raporlamada "olumlu ama yükü yok" gibi yanlış görünüyordu. Kullanıcıya bu
   * yan etki onay metninde açıkça söylenir.
   */
  async function handleDeleteLoadTransfer() {
    if (!editingId || deleting) return;
    const label = detail?.load_number_work_type ?? detail?.load_number ?? `#${editingId}`;
    if (!window.confirm(
      `"${label}" yükü silinecek.\n\n` +
      "Bu yükü oluşturan TEKLİF de birlikte silinecek (Siber dahil) — " +
      "aksi hâlde teklif 'Olumlu' kalır ve raporlama yanlış olur.\n\nDevam edilsin mi?",
    )) return;

    setDeleting(true);
    try {
      await api.delete("/api/v1/load_transfer", { deletion_id: [editingId] });
      addToast("Yük ve bağlı teklif silindi");
      setDrawerOpen(false);
      load();
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Silinemedi", "error");
    } finally {
      setDeleting(false);
    }
  }

  const num = (v: string) => (v.trim() === "" ? null : Number(v.replace(",", ".")));
  const int = (v: string) => (v.trim() === "" ? null : parseInt(v, 10));

  async function handleSave() {
    if (!editingId || saving) return;
    setSaving(true);
    try {
      await api.post(`/api/v1/load_transfer/${editingId}`, {
        load_status_id: int(form.load_status_id),
        payment_type_id: int(form.payment_type_id),
        department_id: int(form.department_id),
        romork_type_id: int(form.romork_type_id),
        customer_id: customer?.id ?? null,
        sender_id: sender?.id ?? null,
        receiver_id: receiver?.id ?? null,
        customer_representative_user_id: customerRep?.id ?? null,
        second_customer_representative_user_id: secondCustomerRep?.id ?? null,
        load_type_id: int(form.load_type_id),
        instruction_id: int(form.instruction_id),
        delivery_method_id: int(form.delivery_method_id),
        load_transfer_type_id: int(form.load_transfer_type_id),
        way_of_working: int(form.way_of_working),
        front_transportation_by_us: int(form.front_transportation_by_us),
        final_transportation_by_us: int(form.final_transportation_by_us),
        departure_country_id: departureCountry || null,
        target_country_id: targetCountry || null,
        instruction_arrival_date: form.instruction_arrival_date || null,
        request_arrival_date: form.request_arrival_date || null,
        readiness_date: form.readiness_date || null,
        date_of_receipt_customer: form.date_of_receipt_customer || null,
        packages: packages
          .filter((p) => !removedPackageIds.includes(p.id ?? -1))
          .map((p) => ({
            id: p.id,
            product_type_id: p.product_type_id?.id ?? null,
            case_type_id: p.case_type_id?.id ?? null,
            quantity: int(p.quantity),
            gross_weight: num(p.gross_weight),
            net_weight: num(p.net_weight),
            volume: num(p.volume),
            lademeter: num(p.lademeter),
            width: num(p.width),
            height: num(p.height),
            length: num(p.length),
            stackable: int(p.stackable),
          })),
        invoice_items: invoiceItems.map((f) => ({
          id: f.id,
          item_id: f.item_id?.id ?? null,
          buysell: f.buysell,
          account_id: f.account?.id ?? null,
          quantity: num(f.quantity),
          net_price: num(f.net_price),
          total_price: num(f.total_price),
          currency_code: int(f.currency_code),
          description: f.description,
          status: f.status,
        })),
      });
      addToast("Yük güncellendi");
      setDrawerOpen(false);
      load();
    } catch (err) {
      if (err instanceof ApiError) addToast(err.message, "error");
      else addToast(err instanceof Error ? err.message : "Kaydedilemedi", "error");
    } finally {
      setSaving(false);
    }
  }

  if (!can("load_management", "read")) {
    return <EmptyState icon={Package} title="Yetkiniz yok" desc="Bu ekranı görüntülemek için gerekli yetkiye sahip değilsiniz." />;
  }

  return (
    <>
      <ModulePage title="Yükler">
        <div className="bg-white border-b border-gray-200 px-6 py-4">
          <div className="flex items-center gap-2.5">
            <div className="flex-1 max-w-md">
              <TextInput value={search} onChange={(v) => { setSearch(v); setPage(1); }} placeholder="Genel arama: yük no, müşteri, durum..." />
            </div>
            {/* Kaydedilmemiş "Teklifsiz Yük" taslağı — Teklif ve Sefer
                ekranlarındaki Taslaklar menüsüyle aynı desen. Yükte sunucu
                taraflı taslak kavramı olmadığı için menü yalnızca bu girdiyi
                listeler. */}
            {canDirect && canCreate && (
              <div className="relative shrink-0" ref={draftsRef}>
                <Btn variant="secondary" onClick={() => { setDrafts(listDrafts(DIRECT_DRAFT_KEY)); setDraftsOpen((o) => !o); }}>
                  <FileText size={14} />
                  Taslaklar
                  {drafts.length > 0 && (
                    <span className="ml-1 px-1.5 py-0.5 rounded-full bg-amber-100 text-amber-700 text-[10px] font-semibold">
                      {drafts.length}
                    </span>
                  )}
                </Btn>
                {draftsOpen && (
                  <div className="absolute z-30 mt-1 right-0 w-80 bg-white border border-gray-200 rounded-md shadow-2xl">
                    <div className="px-4 py-2.5 border-b border-gray-100">
                      <p className="text-xs font-semibold text-gray-700">Taslaklar</p>
                    </div>
                    {drafts.length === 0 ? (
                      <p className="text-xs text-gray-400 text-center py-6">Taslak bulunamadı.</p>
                    ) : (
                      <div className="max-h-80 overflow-y-auto">
                        {drafts.map((d) => (
                          <div key={d.id} className="flex items-stretch border-b border-gray-100 last:border-b-0 bg-amber-50/50">
                            <button
                              type="button"
                              onClick={() => resumeDirectDraft(d)}
                              className="flex-1 text-left px-4 py-2.5 hover:bg-amber-50 transition-colors"
                            >
                              <div className="flex items-center justify-between gap-2">
                                <p className="text-sm font-medium text-gray-800 truncate">
                                  {draftLabel(d.payload)}
                                </p>
                                <span className="shrink-0 text-[10px] font-semibold text-amber-700">
                                  Kaydedilmedi
                                </span>
                              </div>
                              <p className="text-[11px] text-gray-500 mt-0.5">
                                Kaldığı yerden devam et · {formatDraftTime(d.savedAt)}
                              </p>
                            </button>
                            <button
                              type="button"
                              onClick={() => discardDirectDraft(d.id)}
                              title="Taslağı sil"
                              className="px-3 text-gray-300 hover:text-red-500 transition-colors"
                            >
                              <Trash2 size={13} />
                            </button>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                )}
              </div>
            )}
            {/* Teklifsiz yük açma — teklif kullanmayan şirket (Avrora) ve yöneticiler. */}
            {canDirect && canCreate && (
              <Btn onClick={() => { activeDraftId.current = newDraftId(); resetDirectForm(); setDirectOpen(true); }}>
                <Plus size={14} />Teklifsiz Yük Aç
              </Btn>
            )}
            <button
              type="button"
              onClick={() => { setOnlyDeleted((v) => !v); setPage(1); }}
              className={clsx(
                "flex items-center gap-1.5 text-xs font-medium px-3 py-2 rounded-md border transition-colors shrink-0",
                onlyDeleted
                  ? "text-red-600 border-red-200 bg-red-50"
                  : "text-gray-600 border-gray-200 hover:border-red-200 hover:text-red-600",
              )}
              title="Siber'de silinmiş kayıtları listeler"
            >
              <Trash2 size={13} />
              Siberde silinenler
            </button>
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
              <button
                type="button"
                onClick={clearFilters}
                className="text-xs text-gray-500 hover:text-red-600 flex items-center gap-1 shrink-0"
              >
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
                <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-3 pt-4 mt-4 border-t border-gray-100">
                  <AccountPicker label="Müşteri" value={fCustomer} onChange={(v) => { setFCustomer(v); setPage(1); }} />
                  <AccountPicker label="Gönderici" value={fSender} onChange={(v) => { setFSender(v); setPage(1); }} />
                  <AccountPicker label="Alıcı" value={fReceiver} onChange={(v) => { setFReceiver(v); setPage(1); }} />
                  <UserPicker label="Görevli" value={fAssignedUser} onChange={(v) => { setFAssignedUser(v); setPage(1); }} />
                  <FormField label="Durum">
                    <SelectInput value={fStatusId} onChange={(v) => { setFStatusId(v); setPage(1); }} options={opts(loadStatusTypes)} />
                  </FormField>
                  <FormField label="Kap Tipi">
                    <SelectInput value={fCaseTypeId} onChange={(v) => { setFCaseTypeId(v); setPage(1); }} options={opts(caseTypes)} />
                  </FormField>
                  <FormField label="Mali Kalem">
                    <TextInput value={fFinancialItem} onChange={(v) => { setFFinancialItem(v); setPage(1); }} />
                  </FormField>
                  <FormField label="Kilo (kg)">
                    <TextInput type="number" value={fWeight} onChange={(v) => { setFWeight(v); setPage(1); }} />
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
        <Tabs
          tabs={WORK_TYPE_TABS}
          active={workTypeTab}
          onChange={(t) => { setWorkTypeTab(t); setPage(1); }}
          className="px-6 bg-white"
        />
        <div className="bg-gray-50/70 min-h-full">
          {!loading && rows.length === 0 ? (
            <EmptyState
              icon={Package}
              title="Yük bulunamadı"
              desc="Henüz yüke dönüştürülmüş teklif yok. Yük kaydı, Siber'e aktarılmış bir teklifin Teklifler ekranından dönüştürülmesiyle oluşur."
            />
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
                  : rows.map((r, i) => <LoadCard key={r.id} row={r} index={i} onClick={() => openDetail(r.id)} />)}
              </div>
              <Pagination page={page} total={total} perPage={PER_PAGE} onChange={setPage} />
            </>
          )}
        </div>
      </ModulePage>

      {/*
        TEKLİFSİZ YÜK — Siber'e doğrudan skn_yuk satırı açar, rezervasyon
        oluşturmaz. Zorunlu alanlar sunucuda da doğrulanıyor; buradaki liste
        yalnızca kullanıcıyı erken uyarmak için.
      */}
      {/* Form tek sayfaya indi (eskiden 5 sekmeydi); 760px'de alanlar sıkışıyordu. */}
      <Drawer
        open={directOpen}
        onClose={() => setDirectOpen(false)}
        title="Teklifsiz Yük Aç"
        subtitle="Teklif aşaması olmadan doğrudan yük oluşturur"
        width="w-[min(1180px,95vw)]"
        footer={
          <div className="flex gap-2">
            <Btn onClick={submitDirectLoad} disabled={directSaving}>
              <BusyLabel busy={directSaving} busyText="Oluşturuluyor...">Yükü Oluştur</BusyLabel>
            </Btn>
            <Btn variant="secondary" onClick={() => setDirectOpen(false)} disabled={directSaving}>İptal</Btn>
          </div>
        }
      >

        {/* Kaza kurtarma: kaydedilmemiş bir form kalmışsa geri yüklenebilir.
            Sekme kapanması / sayfa yenilenmesi / elektrik kesintisi sonrası. */}
        {/* Kayıt sırasında formu kilitleyip ne olduğunu gösteren ekran:
            işlem Siber'e yazıyor ve saniyeler sürebiliyor. */}
        {directSaving && (
          <div className="mx-6 mt-4 flex items-center gap-3 rounded border border-blue-200 bg-blue-50 p-3">
            <span className="h-4 w-4 shrink-0 animate-spin rounded-full border-2 border-blue-600 border-t-transparent" />
            <div className="text-xs text-blue-900">
              <div className="font-semibold">Yük oluşturuluyor…</div>
              <div className="text-blue-700">
                Kayıt Siber'e yazılıyor, numara üretiliyor. Bu ekranı kapatmayın.
              </div>
            </div>
          </div>
        )}

        <div className="p-8 space-y-10">
          {/* ---------------------------------------------------------------
              GENEL BİLGİLER — tanımlar, taraflar ve güzergah tek blokta.
              Alan alt-başlığı YOK; alanlar tek sürekli akışta.

              ÇALIŞMA ŞEKLİ ile YÜKLEME TİPİ aynı şey DEĞİL; ikisi de Siber'e
              AYRI sütuna yazılıyor:
                skn_yuk.yuklemetip   -> GRUPAJ(0) / KOMPLE(1) / CO-LOAD(2)
                skn_yuk.calismasekli -> SPOT(0) / YILLIK(1)

              Bu ekran eskiden çalışma şeklini "Komple/Parsiyel" diye
              etiketliyordu, yani yükleme tipinin seçeneklerini gösteriyordu ve
              iki alan aynı şeymiş gibi duruyordu. Gerçek liste
              skn_sabittanim(CALISMASEKLI) = SPOT/YILLIK: işin spot mu yıllık
              sözleşme mi olduğu.
              --------------------------------------------------------------- */}
          <section>
            <SectionTitle>Genel Bilgiler</SectionTitle>
            <div className="grid grid-cols-3 gap-x-6 gap-y-6">
              <FormField label="İş Türü" required>
                <SelectInput value={directForm.work_type_id} onChange={(v) => setDirectForm((f) => ({ ...f, work_type_id: v }))} options={opts(workTypes)} />
              </FormField>
              <FormField label="Yükleme Tipi" required>
                <SelectInput value={directForm.loading_type_id} onChange={(v) => setDirectForm((f) => ({ ...f, loading_type_id: v }))} options={opts(loadingTypes)} />
              </FormField>
              <FormField label="Yük Türü">
                <SelectInput value={directForm.load_transfer_type_id} onChange={(v) => setDirectForm((f) => ({ ...f, load_transfer_type_id: v }))} options={opts(loadTransferTypes)} />
              </FormField>
              <FormField label="Departman" required>
                <SelectInput value={directForm.department_id} onChange={(v) => setDirectForm((f) => ({ ...f, department_id: v }))} options={opts(departments)} />
              </FormField>
              <FormField label="Çalışma Şekli">
                <SelectInput value={directForm.way_of_working} onChange={(v) => setDirectForm((f) => ({ ...f, way_of_working: v }))} options={[{ value: "0", label: "Spot" }, { value: "1", label: "Yıllık" }]} />
              </FormField>
              <FormField label="Ödeme Tipi">
                <SelectInput value={directForm.payment_type_id} onChange={(v) => setDirectForm((f) => ({ ...f, payment_type_id: v }))} options={opts(paymentTypes)} />
              </FormField>
              <FormField label="Teslim Şekli">
                <SelectInput value={directForm.delivery_method_id} onChange={(v) => setDirectForm((f) => ({ ...f, delivery_method_id: v }))} options={codeOpts(deliveryMethods)} />
              </FormField>
              <FormField label="Talimat Geliş Şekli">
                <SelectInput value={directForm.instruction_id} onChange={(v) => setDirectForm((f) => ({ ...f, instruction_id: v }))} options={opts(instructions)} />
              </FormField>
              <FormField label="İstenen Römork Cinsi">
                <SelectInput value={directForm.romork_type_id} onChange={(v) => setDirectForm((f) => ({ ...f, romork_type_id: v }))} options={opts(romorkTypes)} />
              </FormField>
              <FormField label="Talimat Geliş Tarihi">
                <TextInput type="date" value={directForm.instruction_arrival_date} onChange={(v) => setDirectForm((f) => ({ ...f, instruction_arrival_date: v }))} />
              </FormField>
              <FormField label="İstenen Varış Tarihi">
                <TextInput type="date" value={directForm.request_arrival_date} onChange={(v) => setDirectForm((f) => ({ ...f, request_arrival_date: v }))} />
              </FormField>
              <FormField label="Hazır Olma Tarihi">
                <TextInput type="date" value={directForm.readiness_date} onChange={(v) => setDirectForm((f) => ({ ...f, readiness_date: v }))} />
              </FormField>
            </div>

            {/* Cari seçiciler iki sütun — teklif ekranıyla aynı ölçü. */}
            <div className="mt-6 grid grid-cols-2 gap-x-6 gap-y-6">
              <AccountPicker label="Müşteri" value={directCustomer} onChange={setDirectCustomer} required />
              <AccountPicker label="Gönderici" value={directSender} onChange={setDirectSender} required />
              <AccountPicker label="Alıcı" value={directReceiver} onChange={setDirectReceiver} required />
              <AccountPicker label="Acente" value={directAgent} onChange={setDirectAgent} />
              <AccountPicker label="Navlunu Ödeyecek Firma" value={directFreightPayer} onChange={setDirectFreightPayer} />
              <FormField label="Ödeyen Firma (serbest metin)">
                <TextInput value={directForm.payer_company} onChange={(v) => setDirectForm((f) => ({ ...f, payer_company: v }))} />
              </FormField>
            </div>

            {/* Güzergah alanları — ayrı başlık YOK: taraflardan hemen sonra
                aynı akışta devam eder (önce kimden kime, sonra nereden nereye). */}
            <div className="mt-6 grid grid-cols-3 gap-x-6 gap-y-6">
            <FormField label="Yükleme Ülkesi" required>
              <SelectInput value={directForm.departure_country_id} onChange={(v) => setDirectForm((f) => ({ ...f, departure_country_id: v }))} options={opts(countries)} />
            </FormField>
            <FormField label="Transit Ülke">
              <SelectInput value={directForm.transit_country_id} onChange={(v) => setDirectForm((f) => ({ ...f, transit_country_id: v }))} options={opts(countries)} />
            </FormField>
            <FormField label="Varış Ülkesi" required>
              <SelectInput value={directForm.target_country_id} onChange={(v) => setDirectForm((f) => ({ ...f, target_country_id: v }))} options={opts(countries)} />
            </FormField>
            <FormField label="Ön Taşıma Bizde">
              <SelectInput value={directForm.front_transportation_by_us} onChange={(v) => setDirectForm((f) => ({ ...f, front_transportation_by_us: v }))} options={[{ value: "0", label: "Hayır" }, { value: "1", label: "Evet" }]} />
            </FormField>
            <FormField label="Son Taşıma Bizde">
              <SelectInput value={directForm.final_transportation_by_us} onChange={(v) => setDirectForm((f) => ({ ...f, final_transportation_by_us: v }))} options={[{ value: "0", label: "Hayır" }, { value: "1", label: "Evet" }]} />
            </FormField>
            </div>
          </section>

          {/* PAKETLER */}
          <section>
            <SectionTitle>Paketler</SectionTitle>
          <div>
            <div className="flex items-center justify-between mb-2">
              <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider">Paketler (en az bir tane)</p>
              <button type="button" onClick={() => setDirectPackages((l) => [...l, { ...EMPTY_PACKAGE_ROW }])} className="text-[11px] text-blue-600 hover:underline flex items-center gap-1">
                <Plus size={12} />Paket Ekle
              </button>
            </div>
            {directPackages.map((p, i) => (
              <div key={i} className="border border-gray-200 rounded-lg p-4 mb-2 relative">
                {directPackages.length > 1 && (
                  <button type="button" onClick={() => setDirectPackages((l) => l.filter((_, xi) => xi !== i))} className="absolute top-2 right-2 text-gray-300 hover:text-red-500">
                    <Trash2 size={13} />
                  </button>
                )}
                <div className="grid grid-cols-3 gap-3">
                  <LookupPicker label="Ürün Tipi" endpoint="/api/v1/product_type" value={p.product_type_id}
                    onChange={(v) => setDirectPackages((l) => l.map((x, xi) => (xi === i ? { ...x, product_type_id: v } : x)))} />
                  <LookupPicker label="Kap Tipi" endpoint="/api/v1/case_type" value={p.case_type_id}
                    onChange={(v) => setDirectPackages((l) => l.map((x, xi) => (xi === i ? { ...x, case_type_id: v } : x)))} />
                  <FormField label="Adet">
                    <TextInput value={p.quantity} onChange={(v) => setDirectPackages((l) => l.map((x, xi) => (xi === i ? { ...x, quantity: v } : x)))} type="number" />
                  </FormField>
                  <FormField label="Brüt Ağırlık (kg)">
                    <TextInput value={p.gross_weight} onChange={(v) => setDirectPackages((l) => l.map((x, xi) => (xi === i ? { ...x, gross_weight: v } : x)))} />
                  </FormField>
                  <FormField label="Net Ağırlık (kg)">
                    <TextInput value={p.net_weight} onChange={(v) => setDirectPackages((l) => l.map((x, xi) => (xi === i ? { ...x, net_weight: v } : x)))} />
                  </FormField>
                  <FormField label="Hacim (m3)">
                    <TextInput value={p.volume} onChange={(v) => setDirectPackages((l) => l.map((x, xi) => (xi === i ? { ...x, volume: v } : x)))} />
                  </FormField>
                  <FormField label="En (cm)">
                    <TextInput value={p.width} onChange={(v) => setDirectPackages((l) => l.map((x, xi) => (xi === i ? { ...x, width: v, lademeter: computeLademeter(v, x.length) } : x)))} />
                  </FormField>
                  <FormField label="Boy (cm)">
                    <TextInput value={p.length} onChange={(v) => setDirectPackages((l) => l.map((x, xi) => (xi === i ? { ...x, length: v, lademeter: computeLademeter(x.width, v) } : x)))} />
                  </FormField>
                  <FormField label="Yükseklik (cm)">
                    <TextInput value={p.height} onChange={(v) => setDirectPackages((l) => l.map((x, xi) => (xi === i ? { ...x, height: v } : x)))} />
                  </FormField>
                  <FormField label="Lademetre">
                    <TextInput value={p.lademeter} onChange={(v) => setDirectPackages((l) => l.map((x, xi) => (xi === i ? { ...x, lademeter: v } : x)))} />
                  </FormField>
                  <FormField label="İstiflenebilir">
                    <SelectInput value={p.stackable} onChange={(v) => setDirectPackages((l) => l.map((x, xi) => (xi === i ? { ...x, stackable: v } : x)))} options={[{ value: "1", label: "Evet" }, { value: "0", label: "Hayır" }]} />
                  </FormField>
                </div>
              </div>
            ))}
          </div>
          </section>

          {/* FİNANS */}
          <section>
            <SectionTitle>Finans</SectionTitle>
          <div>
            {/* Teklif akışındaki gibi her kalem Siber'de alış + satış olmak
                üzere İKİ satır üretir; burada tek satır giriliyor. */}
            <div className="flex items-center justify-between mb-2">
              <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider">Mali Kalemler (isteğe bağlı)</p>
              <button type="button" onClick={() => setDirectItems((l) => [...l, { ...EMPTY_INVOICE_ITEM_ROW }])} className="text-[11px] text-blue-600 hover:underline flex items-center gap-1">
                <Plus size={12} />Kalem Ekle
              </button>
            </div>
            {directItems.length === 0 ? (
              <p className="text-xs text-gray-400 py-6 text-center">Kalem eklenmedi. Yükü açtıktan sonra Finans sekmesinden de ekleyebilirsiniz.</p>
            ) : (
              directItems.map((item, i) => (
                <div key={i} className="border border-gray-200 rounded-lg p-4 mb-2 relative">
                  <button type="button" onClick={() => setDirectItems((l) => l.filter((_, xi) => xi !== i))} className="absolute top-2 right-2 text-gray-300 hover:text-red-500">
                    <Trash2 size={13} />
                  </button>
                  <div className="grid grid-cols-3 gap-3 mb-3">
                    <FinancialItemPicker label="Kalem" value={item.item_id}
                      onChange={(v) => setDirectItems((l) => l.map((x, xi) => (xi === i ? {
                        ...x,
                        item_id: v,
                        account: x.account ?? (v?.default_account_id
                          ? { id: v.default_account_id, name: v.default_account_name ?? null }
                          : null),
                      } : x)))} />
                    <FormField label="Para Birimi">
                      <SelectInput value={item.currency_code} onChange={(v) => setDirectItems((l) => l.map((x, xi) => (xi === i ? { ...x, currency_code: v } : x)))} options={opts(currencies)} />
                    </FormField>
                    <FormField label="Miktar">
                      <TextInput value={item.quantity} onChange={(v) => setDirectItems((l) => l.map((x, xi) => (xi === i ? { ...x, quantity: v } : x)))} type="number" />
                    </FormField>
                  </div>
                  <div className="mb-3">
                    <AccountPicker label="Cari" value={item.account}
                      onChange={(v) => setDirectItems((l) => l.map((x, xi) => (xi === i ? { ...x, account: v } : x)))} />
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    <FormField label="Net Fiyat">
                      <TextInput value={item.net_price} onChange={(v) => setDirectItems((l) => l.map((x, xi) => (xi === i ? { ...x, net_price: v } : x)))} />
                    </FormField>
                    <FormField label="Açıklama">
                      <TextInput value={item.description} onChange={(v) => setDirectItems((l) => l.map((x, xi) => (xi === i ? { ...x, description: v } : x)))} />
                    </FormField>
                  </div>
                </div>
              ))
            )}
          </div>
          </section>

          {/* AÇIKLAMA — paketlerin ve finansın ALTINDA. */}
          <section>
            <SectionTitle>Açıklama</SectionTitle>
            <TextareaInput value={directForm.description} onChange={(v) => setDirectForm((f) => ({ ...f, description: v }))} />
          </section>

          {/* ---------------------------------------------------------------
              DOSYA ARŞİVİ — en altta.

              Dosyalar BURADA gönderilmiyor: Siber'in evrak arşivi kaydı yükün
              kimliğine bağlanıyor ve o kimlik ancak yük yazılırken oluşuyor.
              Seçilenler tarayıcıda tutulup "Yükü Oluştur"dan SONRA
              POST /api/v1/load_transfer/{id}/archive ile gönderiliyor.
              --------------------------------------------------------------- */}
          <section>
            <SectionTitle>Dosya Arşivi</SectionTitle>

            <div
              onDragOver={(e) => { e.preventDefault(); setDragOver(true); }}
              onDragLeave={() => setDragOver(false)}
              onDrop={(e) => { e.preventDefault(); setDragOver(false); addDirectFiles(e.dataTransfer.files); }}
              className={clsx(
                "rounded-lg border-2 border-dashed p-6 text-center transition-colors",
                dragOver ? "border-blue-400 bg-blue-50" : "border-gray-200 bg-gray-50",
              )}
            >
              <p className="text-sm text-gray-600">Dosyaları buraya sürükleyip bırakın</p>
              <p className="mt-1 text-xs text-gray-400">
                Yük oluşturulduktan sonra Siber evrak arşivine gönderilir
              </p>

              <input
                ref={directFileInput}
                type="file"
                multiple
                className="hidden"
                onChange={(e) => { addDirectFiles(e.target.files); e.target.value = ""; }}
              />
              <Btn variant="secondary" size="sm" className="mt-3" onClick={() => directFileInput.current?.click()}>
                <Plus size={13} />Dosya Ekle
              </Btn>
            </div>

            {directFiles.length > 0 && (
              <ul className="mt-3 space-y-1.5">
                {directFiles.map((f, i) => (
                  <li key={f.name + i} className="flex items-center gap-2 rounded border border-gray-200 px-3 py-2">
                    <FileText size={14} className="shrink-0 text-gray-400" />
                    <span className="min-w-0 flex-1 truncate text-sm text-gray-700">{f.name}</span>
                    <span className="shrink-0 text-[11px] text-gray-400">{(f.size / 1024).toFixed(0)} KB</span>
                    <button
                      type="button"
                      title="Kaldır"
                      className="shrink-0 text-gray-300 hover:text-red-500"
                      onClick={() => setDirectFiles((l) => l.filter((_, xi) => xi !== i))}
                    >
                      <Trash2 size={13} />
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </section>
        </div>

      </Drawer>

      <Drawer
        open={drawerOpen}
        onClose={() => { setDrawerOpen(false); clearLoadDeepLink(); }}
        title={detail?.load_number_work_type ?? detail?.load_number ?? "Yük"}
        subtitle={detail?.customer_id?.name ?? undefined}
        width="w-[min(1180px,95vw)]"
        footer={
          canUpdate || canDelete ? (
            <div className="flex items-center justify-between gap-2 w-full">
              <div className="flex gap-2">
                {canUpdate && (
                  <>
                    <Btn onClick={handleSave} disabled={saving || detailLoading || deleting}>
                      <BusyLabel busy={saving} busyText="Kaydediliyor...">Kaydet</BusyLabel>
                    </Btn>
                    <Btn variant="secondary" onClick={() => setDrawerOpen(false)}>İptal</Btn>
                  </>
                )}
              </div>
              {canDelete && (
                <button
                  type="button"
                  onClick={handleDeleteLoadTransfer}
                  disabled={deleting || saving || detailLoading}
                  className="flex items-center gap-1.5 px-3 py-2 rounded-lg text-xs font-semibold text-red-600 hover:bg-red-50 disabled:opacity-50 transition-colors"
                >
                  <BusyLabel busy={deleting} busyText="Siliniyor...">
                    <Trash2 size={14} />
                    Yükü Sil
                  </BusyLabel>
                </button>
              )}
            </div>
          ) : undefined
        }
      >
        {detail?.siber_audit && (
          <div className="px-6 pt-4">
            <SiberAuditPanel audit={detail.siber_audit} />
          </div>
        )}

        <Tabs tabs={TABS} active={tab} onChange={setTab} className="px-6" />
        {detailLoading ? (
          <div className="p-10 text-center text-sm text-gray-400">Yükleniyor...</div>
        ) : (
          detail && (
            <div className="p-8">
              {tab === "Genel Bilgiler" && (
                <div className="space-y-6">
                  {/* BAĞLI SEFER — yükün hangi sefere ait olduğu buradan görünür
                      ve tek tıkla sefer kartına geçilir. Liste olmasının sebebi
                      için bkz. LoadDetail.expeditions açıklaması. */}
                  <div className="rounded-lg border border-gray-200 bg-gray-50/70 p-3">
                    <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider mb-2">
                      Bağlı Sefer
                    </p>
                    {(detail?.expeditions ?? []).length === 0 ? (
                      <p className="text-xs text-gray-400">Bu yük henüz bir sefere bağlanmamış.</p>
                    ) : (
                      <div className="flex flex-wrap gap-2">
                        {(detail?.expeditions ?? []).map((e) => (
                          <button
                            key={`${e.id}-${e.upload_unload ?? 0}-${e.date ?? ""}`}
                            type="button"
                            onClick={() => navigate(`/seferler?sefer=${e.id}`)}
                            title="Bu seferi Seferler ekranında aç"
                            className="flex items-center gap-2 rounded-md border border-blue-200 bg-blue-50 px-3 py-2 text-xs font-medium text-blue-700 hover:bg-blue-100"
                          >
                            <Truck size={13} />
                            <span>{e.expedition_number ?? `#${e.id}`}</span>
                            {e.plate_number && (
                              <span className="text-[11px] font-normal text-blue-500">{e.plate_number}</span>
                            )}
                            {e.upload_unload === 1 && <span className="text-[10px] text-blue-400">Yükleme</span>}
                            {e.upload_unload === 2 && <span className="text-[10px] text-blue-400">Boşaltma</span>}
                          </button>
                        ))}
                      </div>
                    )}
                  </div>

                  <AccountPicker label="Müşteri" value={customer} onChange={setCustomer} />
                  <AccountPicker label="Gönderici" value={sender} onChange={setSender} />
                  <AccountPicker label="Alıcı" value={receiver} onChange={setReceiver} />
                  <div className="grid grid-cols-3 gap-x-6 gap-y-6">
                    <FormField label="Durum">
                      <SelectInput value={form.load_status_id} onChange={(v) => setForm((f) => ({ ...f, load_status_id: v }))} options={opts(loadStatusTypes)} />
                    </FormField>
                    <FormField label="Ödeme Tipi">
                      <SelectInput value={form.payment_type_id} onChange={(v) => setForm((f) => ({ ...f, payment_type_id: v }))} options={opts(paymentTypes)} />
                    </FormField>
                    <FormField label="Departman">
                      <SelectInput value={form.department_id} onChange={(v) => setForm((f) => ({ ...f, department_id: v }))} options={opts(departments)} />
                    </FormField>
                    <FormField label="Römork Tipi">
                      <SelectInput value={form.romork_type_id} onChange={(v) => setForm((f) => ({ ...f, romork_type_id: v }))} options={opts(romorkTypes)} />
                    </FormField>
                    <FormField label="Yük Tipi">
                      <SelectInput value={form.load_type_id} onChange={(v) => setForm((f) => ({ ...f, load_type_id: v }))} options={opts(loadingTypes)} />
                    </FormField>
                    <FormField label="Yük Türü">
                      <SelectInput value={form.load_transfer_type_id} onChange={(v) => setForm((f) => ({ ...f, load_transfer_type_id: v }))} options={opts(loadTransferTypes)} />
                    </FormField>
                    <FormField label="Talimat">
                      <SelectInput value={form.instruction_id} onChange={(v) => setForm((f) => ({ ...f, instruction_id: v }))} options={opts(instructions)} />
                    </FormField>
                    <FormField label="Teslimat Şekli">
                      <SelectInput value={form.delivery_method_id} onChange={(v) => setForm((f) => ({ ...f, delivery_method_id: v }))} options={opts(deliveryMethods)} />
                    </FormField>
                    <FormField label="Çalışma Şekli">
                      <SelectInput value={form.way_of_working} onChange={(v) => setForm((f) => ({ ...f, way_of_working: v }))} options={WAY_OF_WORKING_OPTIONS} />
                    </FormField>
                    <FormField label="Ön Taşıma Tarafımızdan Yapılır">
                      <SelectInput value={form.front_transportation_by_us} onChange={(v) => setForm((f) => ({ ...f, front_transportation_by_us: v }))} options={YES_NO_OPTIONS} />
                    </FormField>
                    <FormField label="Son Taşıma Tarafımızdan Yapılır">
                      <SelectInput value={form.final_transportation_by_us} onChange={(v) => setForm((f) => ({ ...f, final_transportation_by_us: v }))} options={YES_NO_OPTIONS} />
                    </FormField>
                    <FormField label="Kalkış Ülkesi">
                      <SelectInput value={departureCountry} onChange={setDepartureCountry} options={opts(countries)} />
                    </FormField>
                    <FormField label="Varış Ülkesi">
                      <SelectInput value={targetCountry} onChange={setTargetCountry} options={opts(countries)} />
                    </FormField>
                    <FormField label="Talimat Varış Tarihi">
                      <TextInput value={form.instruction_arrival_date} onChange={(v) => setForm((f) => ({ ...f, instruction_arrival_date: v }))} type="date" />
                    </FormField>
                    <FormField label="Talep Varış Tarihi">
                      <TextInput value={form.request_arrival_date} onChange={(v) => setForm((f) => ({ ...f, request_arrival_date: v }))} type="date" />
                    </FormField>
                    <FormField label="Hazır Olma Tarihi">
                      <TextInput value={form.readiness_date} onChange={(v) => setForm((f) => ({ ...f, readiness_date: v }))} type="date" />
                    </FormField>
                    <FormField label="Müşteriye Teslim Tarihi">
                      <TextInput value={form.date_of_receipt_customer} onChange={(v) => setForm((f) => ({ ...f, date_of_receipt_customer: v }))} type="date" />
                    </FormField>
                  </div>
                </div>
              )}

              {tab === "Paketler" && (
                <div>
                  {/* olsold: LoadFormDrawer.vue "Yük İçeriği" sekmesindeki salt-okunur toplam
                      grid'i — paket satırlarından sunucu tarafında hesaplanır (bkz.
                      LoadTransferUpdateService.RecomputeTotalsFromPackagesAsync), formda
                      düzenlenemez. Genel Bilgiler'de DEĞİL burada gösterilir çünkü bu
                      sayılar doğrudan aşağıdaki paket satırlarının toplamıdır. */}
                  <div className="grid grid-cols-3 gap-3 mb-4 pb-4 border-b border-gray-100">
                    <FormField label="Brüt Ağırlık (kg)">
                      <TextInput value={detail?.total_gross_weight != null ? String(detail.total_gross_weight) : ""} onChange={() => {}} disabled placeholder="—" />
                    </FormField>
                    <FormField label="Hacim (m³)">
                      <TextInput value={detail?.total_volume != null ? String(detail.total_volume) : ""} onChange={() => {}} disabled placeholder="—" />
                    </FormField>
                    <FormField label="Lademetre">
                      <TextInput value={detail?.total_lademeter != null ? String(detail.total_lademeter) : ""} onChange={() => {}} disabled placeholder="—" />
                    </FormField>
                    <FormField label="Toplam Kap">
                      <TextInput value={detail?.total_cap != null ? String(detail.total_cap) : ""} onChange={() => {}} disabled placeholder="—" />
                    </FormField>
                    <FormField label="Toplam Lademetre (m³)">
                      <TextInput value={detail?.total_lademeter_m3 != null ? String(detail.total_lademeter_m3) : ""} onChange={() => {}} disabled placeholder="—" />
                    </FormField>
                    <FormField label="Ağırlık Ücreti">
                      <TextInput value={detail?.weight_fee != null ? String(detail.weight_fee) : ""} onChange={() => {}} disabled placeholder="—" />
                    </FormField>
                  </div>
                  <div className="flex items-center justify-between mb-2">
                    <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider">Paketler</p>
                    <button type="button" onClick={addPackageRow} className="text-[11px] text-blue-600 hover:underline flex items-center gap-1">
                      <Plus size={12} />Paket Ekle
                    </button>
                  </div>
                  {packages.length === 0 ? (
                    <p className="text-xs text-gray-400 text-center py-8">Henüz paket eklenmedi.</p>
                  ) : (
                    packages.map((p, i) => (
                      <CollapsibleRow
                        key={i}
                        title={p.product_type_id?.name ?? p.case_type_id?.name ?? `${i + 1}. Paket`}
                        summary={`${p.quantity || 0} adet${p.gross_weight ? ` · ${p.gross_weight} kg` : ""}`}
                        open={openPackages.has(i)}
                        onToggle={() => setOpenPackages((o) => toggleIn(o, i))}
                        onRemove={canDelete ? () => removePackageRow(i) : undefined}
                        removeTitle="Paketi sil"
                      >
                        <div className="grid grid-cols-3 gap-3">
                          <LookupPicker
                            label="Ürün Tipi"
                            endpoint="/api/v1/product_type"
                            value={p.product_type_id}
                            onChange={(v) => setPackages((list) => list.map((x, xi) => (xi === i ? { ...x, product_type_id: v } : x)))}
                          />
                          <LookupPicker
                            label="Kap Tipi"
                            endpoint="/api/v1/case_type"
                            value={p.case_type_id}
                            onChange={(v) => setPackages((list) => list.map((x, xi) => (xi === i ? { ...x, case_type_id: v } : x)))}
                          />
                          <FormField label="Adet">
                            <TextInput value={p.quantity} onChange={(v) => setPackages((list) => list.map((x, xi) => (xi === i ? { ...x, quantity: v } : x)))} type="number" />
                          </FormField>
                          <FormField label="Brüt Ağırlık (kg)">
                            <TextInput value={p.gross_weight} onChange={(v) => setPackages((list) => list.map((x, xi) => (xi === i ? { ...x, gross_weight: v } : x)))} />
                          </FormField>
                          <FormField label="Net Ağırlık (kg)">
                            <TextInput value={p.net_weight} onChange={(v) => setPackages((list) => list.map((x, xi) => (xi === i ? { ...x, net_weight: v } : x)))} />
                          </FormField>
                          <FormField label="Hacim (m³)">
                            <TextInput value={p.volume} onChange={(v) => setPackages((list) => list.map((x, xi) => (xi === i ? { ...x, volume: v } : x)))} />
                          </FormField>
                          <FormField label="Lademetre">
                            <TextInput value={p.lademeter} onChange={(v) => setPackages((list) => list.map((x, xi) => (xi === i ? { ...x, lademeter: v } : x)))} />
                          </FormField>
                          <FormField label="En (cm)">
                            <TextInput value={p.width} onChange={(v) => setPackages((list) => list.map((x, xi) => (xi === i ? { ...x, width: v, lademeter: computeLademeter(v, x.length) } : x)))} />
                          </FormField>
                          <FormField label="Boy (cm)">
                            <TextInput value={p.length} onChange={(v) => setPackages((list) => list.map((x, xi) => (xi === i ? { ...x, length: v, lademeter: computeLademeter(x.width, v) } : x)))} />
                          </FormField>
                          <FormField label="Yükseklik (cm)">
                            <TextInput value={p.height} onChange={(v) => setPackages((list) => list.map((x, xi) => (xi === i ? { ...x, height: v } : x)))} />
                          </FormField>
                          <FormField label="İstiflenebilir">
                            <SelectInput value={p.stackable} onChange={(v) => setPackages((list) => list.map((x, xi) => (xi === i ? { ...x, stackable: v } : x)))} options={[{ value: "1", label: "Evet" }, { value: "0", label: "Hayır" }]} />
                          </FormField>
                        </div>
                      </CollapsibleRow>
                    ))
                  )}
                </div>
              )}

              {tab === "Finans" && (
                <div className="space-y-8">
                  {([
                    { key: "1", title: "Alış Hareketleri" },
                    { key: "2", title: "Satış Hareketleri" },
                  ] as const).map(({ key, title }) => {
                    const rows = invoiceItems
                      .map((item, i) => ({ item, i }))
                      .filter(({ item }) => item.buysell === key);
                    return (
                      <div key={key}>
                        <div className="flex items-center justify-between mb-2">
                          <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider">{title}</p>
                          <button type="button" onClick={() => addInvoiceItemRow(key)} className="text-[11px] text-blue-600 hover:underline flex items-center gap-1">
                            <Plus size={12} />Kalem Ekle
                          </button>
                        </div>
                        {rows.length === 0 ? (
                          <p className="text-xs text-gray-400 text-center py-6">Hareket bulunamadı.</p>
                        ) : (
                          rows.map(({ item, i }) => (
                            <CollapsibleRow
                              key={i}
                              title={item.item_id?.name ?? "Kalem seçilmedi"}
                              summary={item.total_price
                                ? `${item.total_price} ${currencies.find((c) => String(c.id) === item.currency_code)?.code ?? ""}`.trim()
                                : "—"}
                              open={openInvoiceItems.has(i)}
                              onToggle={() => setOpenInvoiceItems((o) => toggleIn(o, i))}
                              onRemove={() => removeInvoiceItemRow(i)}
                              removeTitle="Mali kalemi sil"
                            >
                              <div className="grid grid-cols-3 gap-3 mb-3">
                                <div>
                                  <FinancialItemPicker
                                    label="Kalem"
                                    value={item.item_id}
                                    onChange={(v) => setInvoiceItems((list) => list.map((x, xi) => {
                                      if (xi !== i) return x;
                                      // Kalem hem Alış hem Satış'ta kullanılabiliyorsa (type=3) ya da
                                      // Siber senkronunda type hiç set edilmemişse (null) mevcut seçim
                                      // korunur; yalnızca tek yönlü olduğu KESİN biliniyorsa (1 veya 2)
                                      // otomatik ayarlanır. Önceki hâli (`type !== 3`) null'u da "tek
                                      // yönlü" sayıp buysell'i string "null" yapıyordu — satır hiçbir
                                      // Alış/Satış grubuna düşmediği için listeden kayboluyordu.
                                      const buysell = v && (v.type === 1 || v.type === 2) ? String(v.type) : x.buysell;
                                      // Alış/Satış fiilen değiştiyse Cari listesi de değişir (Tedarikçi<->Müşteri) - eski seçim geçersiz olabilir.
                                      const cleared = buysell === x.buysell ? x.account : null;

                                      // KALEME BAĞLI FİRMA: bazı kalemler Siber'de tek firmaya
                                      // kilitli (GÜMRÜK VERGİSİ %96, BELGESİZ GİDERLER %99) —
                                      // bu kalemler seçilince cari kendiliğinden gelir. Yalnızca
                                      // alan BOŞSA doldurulur, kullanıcının seçimi ezilmez;
                                      // dağınık kalemlerde default_account_id null olduğu için
                                      // hiçbir şey olmaz.
                                      const account = cleared ?? (
                                        v?.default_account_id
                                          ? { id: v.default_account_id, name: v.default_account_name ?? null }
                                          : null);

                                      return { ...x, item_id: v, buysell, account };
                                    }))}
                                  />
                                  <button type="button" onClick={() => setFinancialItemModalOpen(true)} className="mt-1 text-[11px] text-blue-600 hover:underline text-left">Yeni Ekle</button>
                                </div>
                                <FormField label="Para Birimi">
                                  <SelectInput value={item.currency_code} onChange={(v) => setInvoiceItems((list) => list.map((x, xi) => (xi === i ? { ...x, currency_code: v } : x)))} options={opts(currencies)} />
                                </FormField>
                                <FormField label="Alış/Satış">
                                  <SelectInput
                                    value={item.buysell}
                                    onChange={(v) => setInvoiceItems((list) => list.map((x, xi) => (xi === i ? { ...x, buysell: v, item_id: null, account: null } : x)))}
                                    options={[{ value: "1", label: "Alış" }, { value: "2", label: "Satış" }]}
                                  />
                                </FormField>
                              </div>
                              <div className="mb-3">
                                <AccountPicker
                                  label={item.buysell === "1" ? "Tedarikçiler" : "Müşteriler"}
                                  value={item.account}
                                  onChange={(v) => setInvoiceItems((list) => list.map((x, xi) => (xi === i ? { ...x, account: v } : x)))}
                                  accountType={item.buysell === "1" ? 2 : 1}
                                />
                              </div>
                              <div className="grid grid-cols-3 gap-3">
                                <FormField label="Miktar">
                                  <TextInput value={item.quantity} onChange={(v) => setInvoiceItems((list) => list.map((x, xi) => (xi === i ? { ...x, quantity: v } : x)))} type="number" />
                                </FormField>
                                <FormField label="Net Fiyat">
                                  <TextInput value={item.net_price} onChange={(v) => setInvoiceItems((list) => list.map((x, xi) => (xi === i ? { ...x, net_price: v } : x)))} />
                                </FormField>
                                <FormField label="Toplam Fiyat">
                                  <TextInput value={item.total_price} onChange={(v) => setInvoiceItems((list) => list.map((x, xi) => (xi === i ? { ...x, total_price: v } : x)))} />
                                </FormField>
                              </div>
                              <div className="mt-3">
                                <FormField label="Durum">
                                  <SelectInput
                                    value={item.status}
                                    onChange={(v) => setInvoiceItems((list) => list.map((x, xi) => (xi === i ? { ...x, status: v } : x)))}
                                    options={FINANCIAL_ITEM_STATUS_OPTIONS.filter((o) =>
                                      item.buysell === "1" ? o.value !== "invoice_issued" : o.value !== "invoice_received",
                                    )}
                                  />
                                </FormField>
                              </div>
                              <div className="mt-3">
                                <FormField label="Açıklama">
                                  <TextInput value={item.description} onChange={(v) => setInvoiceItems((list) => list.map((x, xi) => (xi === i ? { ...x, description: v } : x)))} />
                                </FormField>
                              </div>
                            </CollapsibleRow>
                          ))
                        )}
                      </div>
                    );
                  })}
                </div>
              )}

              {tab === "Görevliler" && (
                <div className="space-y-6">
                  <UserPicker label="Operasyon Yetkilisi" value={customerRep} onChange={setCustomerRep} />
                  <UserPicker label="Satış Temsilcisi" value={secondCustomerRep} onChange={setSecondCustomerRep} />
                </div>
              )}

              {tab === "Hareketler" && (
                <div>
                  <div className="flex items-center justify-between mb-2">
                    <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider">Hareketler</p>
                    <div className="flex items-center gap-3">
                      {deletedMovements.length > 0 && (
                        <button type="button" onClick={() => setDeletedMovementsModalOpen(true)} className="text-[11px] text-gray-500 hover:underline">
                          Silinen Hareketler
                        </button>
                      )}
                      <button type="button" onClick={openMovementModal} className="text-[11px] text-blue-600 hover:underline flex items-center gap-1">
                        <Plus size={12} />Yeni Hareket Ekle
                      </button>
                    </div>
                  </div>
                  {movements.length === 0 ? (
                    <p className="text-xs text-gray-400 text-center py-8">Henüz hareket kaydı bulunmamaktadır.</p>
                  ) : (
                    movements.map((m) => (
                      <div key={m.id} className="border border-gray-200 rounded-lg p-4 mb-2">
                        <div className="flex items-start justify-between gap-3">
                          <div>
                            {m.expedition_status?.name && (
                              <div className="text-xs font-semibold text-blue-600 border-l-2 border-blue-500 pl-2 mb-2">{m.expedition_status.name}</div>
                            )}
                            <p className="text-sm font-medium">{m.destination?.name ?? "—"}</p>
                            {m.address && <p className="text-xs text-gray-500 mt-1">{m.address}</p>}
                            {m.description && <p className="text-xs text-gray-500 mt-1">{m.description}</p>}
                            <p className="text-[11px] text-gray-400 mt-2">
                              {m.created_at ? new Date(m.created_at).toLocaleString("tr-TR") : "—"} · {movementUserLabel(m.user)}
                            </p>
                            {m.expedition_movement?.expedition?.expedition_number && (
                              <p className="text-[11px] text-blue-500 mt-1">
                                {m.expedition_movement.expedition.expedition_number} numaralı sefer hareketinden otomatik oluşmuştur.
                              </p>
                            )}
                          </div>
                          <button type="button" onClick={() => deleteMovement(m.id)} className="text-gray-300 hover:text-red-500 shrink-0">
                            <Trash2 size={14} />
                          </button>
                        </div>
                      </div>
                    ))
                  )}
                </div>
              )}

              {tab === "Evrak Takibi" && (
                <div>
                  <div className="flex items-center justify-between mb-2">
                    <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider">Evrak Takibi</p>
                    <button type="button" onClick={openDocumentModal} className="text-[11px] text-blue-600 hover:underline flex items-center gap-1">
                      <Plus size={12} />Yeni Evrak Ekle
                    </button>
                  </div>
                  {documents.length === 0 ? (
                    <p className="text-xs text-gray-400 text-center py-8">Henüz evrak kaydı bulunmamaktadır.</p>
                  ) : (
                    documents.map((d) => (
                      <div key={d.id} className="border border-gray-200 rounded-lg p-4 mb-2">
                        <div className="flex items-start justify-between gap-3">
                          <button type="button" onClick={() => openEditDocumentModal(d)} className="text-left flex-1">
                            <p className="text-sm font-medium">{d.evrak_turu_name ?? "—"}</p>
                            {d.document_number && <p className="text-xs text-gray-500 mt-1">Evrak No: {d.document_number}</p>}
                            <p className="text-xs text-gray-500 mt-1">{d.original_count ?? 0} orijinal · {d.copy_count ?? 0} kopya</p>
                            {d.delivered_to && (
                              <p className="text-xs text-gray-500 mt-1">
                                Teslim: {d.delivered_to}
                                {d.delivered_at ? ` · ${new Date(d.delivered_at).toLocaleDateString("tr-TR")}` : ""}
                              </p>
                            )}
                            {d.note && <p className="text-xs text-gray-500 mt-1">{d.note}</p>}
                            <p className="text-[11px] text-gray-400 mt-2">
                              {d.created_at ? new Date(d.created_at).toLocaleString("tr-TR") : "—"}
                            </p>
                          </button>
                          <button type="button" onClick={() => deleteDocument(d.id)} className="text-gray-300 hover:text-red-500 shrink-0">
                            <Trash2 size={14} />
                          </button>
                        </div>
                      </div>
                    ))
                  )}
                </div>
              )}

              {tab === "Faturalar" && (
                <div>
                  <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider mb-2">Faturalar</p>
                  {(detail?.invoices.length ?? 0) === 0 ? (
                    <p className="text-xs text-gray-400 text-center py-8">Bu yüke bağlı fatura bulunamadı.</p>
                  ) : (
                    detail!.invoices.map((inv) => (
                      <div key={inv.id} className="border border-gray-200 rounded-lg p-4 mb-2">
                        <div className="grid grid-cols-2 gap-3">
                          <div className="bg-gray-50 rounded-lg p-3">
                            <p className="text-[11px] text-gray-500">Fatura No</p>
                            <p className="text-sm font-medium">{inv.invoice_id ?? "—"}</p>
                          </div>
                          <div className="bg-gray-50 rounded-lg p-3">
                            <p className="text-[11px] text-gray-500">Fatura Ticareti Tipi</p>
                            <p className="text-sm font-medium">{INVOICE_COMMERCIAL_TYPE_LABELS[inv.commercial_type] ?? "—"}</p>
                          </div>
                          <div className="bg-gray-50 rounded-lg p-3">
                            <p className="text-[11px] text-gray-500">Gelen/Giden</p>
                            <p className="text-sm font-medium">{inv.box_type === 1 ? "Giden Fatura" : "Gelen Fatura"}</p>
                          </div>
                          <div className="bg-gray-50 rounded-lg p-3">
                            <p className="text-[11px] text-gray-500">Fatura Durumu</p>
                            <p className="text-sm font-medium">{inv.invoice_status?.name ?? "—"}</p>
                          </div>
                          <div className="bg-gray-50 rounded-lg p-3">
                            <p className="text-[11px] text-gray-500">Fatura Tipi</p>
                            <p className="text-sm font-medium">{inv.invoice_type?.name ?? "—"}</p>
                          </div>
                          <div className="bg-gray-50 rounded-lg p-3 col-span-2">
                            <p className="text-[11px] text-gray-500">Alıcı</p>
                            <p className="text-sm font-medium">{inv.target_title ?? "—"}</p>
                            <p className="text-xs text-gray-500">{inv.target_identity_no ?? "—"}</p>
                          </div>
                          <div className="bg-gray-50 rounded-lg p-3">
                            <p className="text-[11px] text-gray-500">Tutar</p>
                            <p className="text-sm font-medium">{invoiceMoney(inv.payable_amount)} {inv.document_currency_code ?? ""}</p>
                          </div>
                          <div className="bg-gray-50 rounded-lg p-3">
                            <p className="text-[11px] text-gray-500">KDV Hariç</p>
                            <p className="text-sm font-medium">{invoiceMoney(inv.tax_exclusive_amount)} {inv.document_currency_code ?? ""}</p>
                          </div>
                          <div className="bg-gray-50 rounded-lg p-3 col-span-2">
                            <p className="text-[11px] text-gray-500">KDV</p>
                            <p className="text-sm font-medium">{invoiceMoney(inv.tax_amount)} ({inv.tax_rate ?? 0}%) {inv.document_currency_code ?? ""}</p>
                          </div>
                          <div className="bg-gray-50 rounded-lg p-3 col-span-2">
                            <p className="text-[11px] text-gray-500">Fatura Tarihi</p>
                            <p className="text-sm font-medium">{inv.invoice_execution_date ? new Date(inv.invoice_execution_date).toLocaleDateString("tr-TR") : "—"}</p>
                          </div>
                        </div>
                      </div>
                    ))
                  )}
                </div>
              )}

              {tab === "İşlem Geçmişi" && (
                <RecordHistoryTab resource="load_transfer" recordId={detail?.id ?? null} />
              )}

              {tab === "Dosya Arşivi" && (
                <div className="space-y-4">
                  {/*
                    SİBER ARŞİVİ — Siber programından eklenen evraklar. Yerel
                    dosyalardan AYRI bölümde: sahibi Siber, buradan silinemez ve
                    düzenlenemez. Dosyalar veritabanında değil Siber'in FTP arşiv
                    sunucusunda; şimdilik LİSTE gösteriliyor, indirme ayrı adım.
                  */}
                  <div>
                    <div className="flex items-center justify-between mb-2">
                      <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider">
                        Siber Arşivi
                      </p>
                      <span className="text-[10px] text-gray-400">Siber'den okunur · salt görüntüleme</span>
                    </div>
                    {(detail?.siber_archive ?? []).length === 0 ? (
                      <p className="text-xs text-gray-400">Bu yük için Siber arşivinde evrak yok.</p>
                    ) : (
                      <div className="space-y-1.5">
                        {(detail?.siber_archive ?? []).map((a) => (
                          <div key={a.id} className="flex items-center gap-2 p-2 rounded-lg border border-gray-100 bg-gray-50/60 text-sm">
                            <FileIcon size={14} className="text-gray-400 shrink-0" />
                            {/*
                              Dosya API üzerinden vekil olarak servis ediliyor
                              (/api/v1/load_transfer/archive/{id}); FTP adresi ve
                              parolası tarayıcıya hiç verilmiyor. PDF/görseller
                              yeni sekmede açılır, diğer türler inmeye başlar.
                            */}
                            <button
                              type="button"
                              onClick={() => openArchiveFile(a)}
                              className="flex-1 truncate text-left text-blue-600 hover:underline"
                              title="Siber arşivinden aç"
                            >
                              {a.name ?? "—"}
                            </button>
                            {a.personal_data && (
                              <span className="shrink-0 rounded border border-amber-200 bg-amber-50 px-1.5 py-0.5 text-[10px] font-medium text-amber-700">
                                Kişisel veri
                              </span>
                            )}
                            {a.restricted_groups && (
                              <span className="shrink-0 rounded border border-purple-200 bg-purple-50 px-1.5 py-0.5 text-[10px] font-medium text-purple-700" title={a.restricted_groups}>
                                Kısıtlı
                              </span>
                            )}
                            <span className="shrink-0 text-[11px] text-gray-400">
                              {a.created_at ? new Date(a.created_at).toLocaleDateString("tr-TR") : "—"}
                              {a.created_by ? ` · ${a.created_by}` : ""}
                            </span>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>

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

                  {canUpdate && (
                    <div className="flex justify-end">
                      <Btn onClick={saveFiles} disabled={savingFiles}>{savingFiles ? "Kaydediliyor..." : "Dosyaları Kaydet"}</Btn>
                    </div>
                  )}
                </div>
              )}
            </div>
          )
        )}
      </Drawer>

      <Modal open={movementModalOpen} onClose={() => setMovementModalOpen(false)} title="Yeni Hareket Ekle">
        <div className="w-[420px] max-w-full space-y-4">
          <FormField label="Durum" required>
            <SelectInput value={movementForm.expedition_status_id} onChange={(v) => setMovementForm((f) => ({ ...f, expedition_status_id: v }))} options={opts(expeditionStatuses)} />
          </FormField>
          <FormField label="Konum" required>
            <SelectInput value={movementForm.destination_id} onChange={(v) => setMovementForm((f) => ({ ...f, destination_id: v }))} options={opts(destinations)} />
          </FormField>
          <FormField label="Adres">
            <TextareaInput value={movementForm.address} onChange={(v) => setMovementForm((f) => ({ ...f, address: v }))} rows={2} />
          </FormField>
          <FormField label="Açıklama">
            <TextareaInput value={movementForm.description} onChange={(v) => setMovementForm((f) => ({ ...f, description: v }))} rows={2} />
          </FormField>
          <div className="flex gap-2 justify-end">
            <Btn variant="secondary" onClick={() => setMovementModalOpen(false)}>İptal</Btn>
            <Btn onClick={saveMovement} disabled={savingMovement}>{savingMovement ? "Kaydediliyor..." : "Kaydet"}</Btn>
          </div>
        </div>
      </Modal>

      <Modal
        open={documentModalOpen}
        onClose={() => setDocumentModalOpen(false)}
        title={editingDocumentId ? "Evrağı Düzenle" : "Yeni Evrak Ekle"}
      >
        <div className="w-[420px] max-w-full space-y-4">
          <FormField label="Evrak Türü" required>
            <SelectInput value={documentForm.evrak_turu_id} onChange={(v) => setDocumentForm((f) => ({ ...f, evrak_turu_id: v }))} options={opts(evrakTurleri)} />
          </FormField>
          <FormField label="Evrak No">
            <TextInput value={documentForm.document_number} onChange={(v) => setDocumentForm((f) => ({ ...f, document_number: v }))} />
          </FormField>
          <FormField label="Tarih">
            <TextInput type="date" value={documentForm.date} onChange={(v) => setDocumentForm((f) => ({ ...f, date: v }))} />
          </FormField>
          <div className="grid grid-cols-2 gap-3">
            <FormField label="Orijinal Adet">
              <TextInput type="number" value={documentForm.original_count} onChange={(v) => setDocumentForm((f) => ({ ...f, original_count: v }))} />
            </FormField>
            <FormField label="Kopya Adet">
              <TextInput type="number" value={documentForm.copy_count} onChange={(v) => setDocumentForm((f) => ({ ...f, copy_count: v }))} />
            </FormField>
          </div>
          <FormField label="Teslim Alan">
            <TextInput value={documentForm.delivered_to} onChange={(v) => setDocumentForm((f) => ({ ...f, delivered_to: v }))} />
          </FormField>
          <FormField label="Teslim Tarihi">
            <TextInput type="date" value={documentForm.delivered_at} onChange={(v) => setDocumentForm((f) => ({ ...f, delivered_at: v }))} />
          </FormField>
          <FormField label="Açıklama">
            <TextareaInput value={documentForm.note} onChange={(v) => setDocumentForm((f) => ({ ...f, note: v }))} rows={2} />
          </FormField>
          <div className="flex gap-2 justify-end">
            <Btn variant="secondary" onClick={() => setDocumentModalOpen(false)}>İptal</Btn>
            <Btn onClick={saveDocument} disabled={savingDocument}>{savingDocument ? "Kaydediliyor..." : "Kaydet"}</Btn>
          </div>
        </div>
      </Modal>

      <Modal open={deletedMovementsModalOpen} onClose={() => setDeletedMovementsModalOpen(false)} title="Silinen Hareketler">
        <div className="w-[420px] max-w-full space-y-2 max-h-[60vh] overflow-y-auto">
          {deletedMovements.length === 0 ? (
            <p className="text-xs text-gray-400 text-center py-8">Silinen hareket bulunmamaktadır.</p>
          ) : (
            deletedMovements.map((m) => (
              <div key={m.id} className="border border-gray-200 rounded-lg p-4 mb-2">
                {m.expedition_status?.name && (
                  <div className="text-xs font-semibold text-blue-600 border-l-2 border-blue-500 pl-2 mb-2">{m.expedition_status.name}</div>
                )}
                <p className="text-sm font-medium mb-2">{m.destination?.name ?? "Belirtilmemiş"}</p>
                <p className="text-[11px] text-gray-500 mb-1">Oluşturan: {movementUserLabel(m.user)}</p>
                <p className="text-[11px] text-gray-500 mb-1">
                  Oluşturulma Tarihi: {m.created_at ? new Date(m.created_at).toLocaleString("tr-TR") : "—"}
                </p>
                <p className="text-[11px] text-red-400 mb-1">
                  Silinme Tarihi: {m.deleted_at ? new Date(m.deleted_at).toLocaleString("tr-TR") : "—"}
                </p>
                {m.expedition_movement?.expedition?.expedition_number && (
                  <p className="text-[11px] text-blue-500 mt-1">
                    {m.expedition_movement.expedition.expedition_number} numaralı sefer hareketinin değişikliği sonrasında otomatik oluşmuştur.
                  </p>
                )}
                {m.description && <p className="text-xs text-gray-500 mt-2 pt-2 border-t border-gray-100">{m.description}</p>}
              </div>
            ))
          )}
        </div>
      </Modal>

      <FinancialItemManagerModal
        open={financialItemModalOpen}
        onClose={() => setFinancialItemModalOpen(false)}
      />
    </>
  );
}
