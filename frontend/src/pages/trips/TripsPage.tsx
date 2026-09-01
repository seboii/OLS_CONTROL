import { useEffect, useRef, useState } from "react";
import { motion, AnimatePresence } from "motion/react";
import { clsx } from "clsx";
import { useNavigate, useSearchParams } from "react-router-dom";
import { Truck, Plus, Trash2, Link2, ChevronDown, ChevronUp, Filter, X, CalendarDays, FileText, ExternalLink } from "lucide-react";
import { api, ApiError, downloadFile, type DataMessage, type Paginated } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useDebouncedValue, useLookupOptions } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { ModulePage } from "@/components/ui/ModulePage";
import { EmptyState, Pagination } from "@/components/ui/DataTable";
import { Drawer, Modal } from "@/components/ui/Overlay";
import { Badge, Btn, FormField, SearchInput, SelectInput, Tabs, TextareaInput, TextInput } from "@/components/ui/primitives";
import { DepartmentManagerModal } from "@/components/shared/DepartmentManagerModal";
import { CarPicker } from "@/components/shared/CarPicker";
import { clearDraft, formatDraftTime, readDraft, writeDraft } from "@/lib/autodraft";
import { BusyLabel } from "@/components/ui/Busy";
import { SiberAuditPanel, type SiberAuditInfo } from "@/components/shared/SiberAudit";

/**
 * Kaydedilmemiş "Yeni Sefer" otomatik taslağı — Teklif'teki (QuotesPage) ile aynı
 * mantık: form doldurulurken kaydetmeden çıkılırsa kaybolmasın, Taslaklar
 * menüsünden kaldığı yerden devam edilsin. Sefer'in sunucu tarafında bir "taslak"
 * kavramı yok (Teklif'teki is_draft gibi), bu yüzden menüde yalnızca bu
 * kaydedilmemiş taslak listelenir.
 */
const TRIP_DRAFT_KEY = "ols.trip.autodraft.v1";

type TripForm = {
  // romork_id kaydedilen DEĞER (araç kimliği), romork_plate ise SEÇİCİDE ve
  // taslak listesinde gösterilen plaka. İkisi de metin tutuluyor çünkü
  // tripFormHasContent tüm alanlarda .trim() çağırıyor ve taslak JSON'a
  // serileştiriliyor — forma nesne koymak ikisini de bozardı.
  romork_id: string; romork_plate: string;
  work_type: string; department_id: string; expedition_type: string;
  release_date: string; entry_date: string; loading_date: string; return_date: string;
};

type TripDraft = { savedAt: string; form: TripForm };

const EMPTY_TRIP_FORM: TripForm = {
  romork_id: "", romork_plate: "", work_type: "", department_id: "", expedition_type: "",
  release_date: "", entry_date: "", loading_date: "", return_date: "",
};

/** Boş formu taslak diye kaydetmeyelim — en az bir alan dolu olmalı. */
const tripFormHasContent = (f: TripForm) => Object.values(f).some((v) => v.trim() !== "");

interface NamedRef {
  id: string;
  name: string | null;
}
interface CarRef {
  id: number;
  plate_number: string | null;
}

interface ExpeditionItem {
  id: number;
  expedition_number: string | null;
  created_at: string | null;
  work_type: NamedRef | null;
  expedition_type_id: NamedRef | null;
  status_id: NamedRef | null;
  department_id: NamedRef | null;
  romork_id: CarRef | null;
  start_city_id: NamedRef | null;
  end_city_id: NamedRef | null;
}

interface ExpeditionDetail extends ExpeditionItem {
  siber_audit?: SiberAuditInfo | null;
  /** Seferin KENDİ Siber arşiv evrakları. */
  siber_archive: SiberArchiveFile[];
  release_date: string | null;
  loading_date: string | null;
  return_date: string | null;
  car_exit_date: string | null;
  load_city_id: NamedRef | null;
  expedition_id: string | null;
  sefer_id: string | null;
  year_week: string | null;
  registration_login_date: string | null;
}

interface MovementUserDetail {
  id: number;
  name: string | null;
  surname: string | null;
  email: string | null;
}

const movementUserLabel = (u: MovementUserDetail | null) =>
  u ? `${u.name ?? ""} ${u.surname ?? ""} (${u.email ?? ""})`.trim() : "—";

interface MovementDetail {
  id: number;
  description: string | null;
  address: string | null;
  created_at: string | null;
  deleted_at: string | null;
  destination: NamedRef | null;
  user: MovementUserDetail | null;
  expedition_status: NamedRef | null;
}

interface MappingTotals {
  total_quantity: number;
  total_gross_weight: number;
  total_net_weight: number;
  total_lademeter: number;
  total_volume: number;
}

interface LoadPackageDetail {
  id: number;
  quantity: number | null;
  gross_weight: number | null;
  net_weight: number | null;
  lademeter: number | null;
  volume: number | null;
  product_type_id: NamedRef | null;
  case_type_id: NamedRef | null;
}

interface SiberArchiveFile {
  id: string;
  name: string | null;
  created_at: string | null;
  created_by: string | null;
  personal_data: boolean;
  restricted_groups: string | null;
}

interface MappedLoadTransfer {
  id: number;
  load_number_work_type: string | null;
  customer_id: { id: number; name: string | null } | null;
  load_transfer_package: LoadPackageDetail[];
  /** Bu yükün Siber arşivindeki evrakları. */
  siber_archive: SiberArchiveFile[];
}

interface ExpeditionMapping {
  id: number;
  yukaktarmaid: string | null;
  upload_unload: number | null;
  date: string | null;
  load_transfer_id: MappedLoadTransfer | null;
  romork_id: CarRef | null;
  yer_id: NamedRef | null;
  total_values: MappingTotals;
}

interface ExpeditionMappingResponse {
  data: ExpeditionMapping[];
  total_expedition_values: MappingTotals;
}

interface AvailableLoad {
  id: number;
  load_number_work_type: string | null;
  load_status_id: { id: number; name: string | null } | null;
  customer_id: { id: number; name: string | null } | null;
}

const PER_PAGE = 24;
const DETAIL_TABS = ["Genel Bilgiler", "Bağlı Yükler", "Hareketler"];
// İş Tipi ham id'leri seed'e göre değişebilir (bkz. QuotesPage STATUS_TABS notu),
// bu yüzden sekmeler workTypes listesinden AD ile eşleştirilir, sabit id kullanılmaz.
const WORK_TYPE_TABS = ["Tümü", "İhracat", "İthalat", "Transit", "Yurtiçi"];
const ZERO_TOTALS: MappingTotals = { total_quantity: 0, total_gross_weight: 0, total_net_weight: 0, total_lademeter: 0, total_volume: 0 };
const EMPTY_MOVEMENT_FORM = { destination_id: "", expedition_status_id: "", description: "", address: "" };

function ExpeditionCard({
  row, index, onClick, canDelete, onDelete,
}: {
  row: ExpeditionItem; index: number; onClick: () => void; canDelete: boolean; onDelete: () => void;
}) {
  const date = row.created_at ? new Date(row.created_at).toLocaleDateString("tr-TR") : null;

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
            <Truck size={16} />
          </div>
          <div className="min-w-0">
            <p className="font-mono text-xs font-semibold text-blue-600 truncate">{row.expedition_number ?? `SEF-${row.id}`}</p>
            {date && (
              <p className="text-[10px] text-gray-400 mt-0.5 flex items-center gap-1">
                <CalendarDays size={10} />
                {date}
              </p>
            )}
          </div>
        </div>
        <div className="flex items-center gap-1 shrink-0">
          {row.status_id?.name && <Badge label={row.status_id.name} />}
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
        <p className="text-[10px] font-semibold text-gray-400 uppercase tracking-wider mb-0.5">Araç</p>
        <p className="text-sm font-semibold text-gray-900 truncate">{row.romork_id?.plate_number ?? "—"}</p>
      </div>

      {(row.start_city_id?.name || row.end_city_id?.name) && (
        <div className="pt-2.5 border-t border-gray-100">
          <p className="text-[10px] font-semibold text-gray-400 uppercase tracking-wider mb-0.5">Güzergâh</p>
          <p className="text-xs text-gray-700 truncate">
            {row.start_city_id?.name && row.end_city_id?.name
              ? `${row.start_city_id.name} → ${row.end_city_id.name}`
              : (row.start_city_id?.name ?? row.end_city_id?.name)}
          </p>
        </div>
      )}

      <div className="grid grid-cols-3 gap-3 pt-2.5 border-t border-gray-100">
        <div className="min-w-0">
          <p className="text-[10px] font-semibold text-gray-400 uppercase tracking-wider mb-0.5">İş Tipi</p>
          <p className="text-xs text-gray-700 truncate">{row.work_type?.name ?? "—"}</p>
        </div>
        <div className="min-w-0">
          <p className="text-[10px] font-semibold text-gray-400 uppercase tracking-wider mb-0.5">Sefer Tipi</p>
          <p className="text-xs text-gray-700 truncate">{row.expedition_type_id?.name ?? "—"}</p>
        </div>
        <div className="min-w-0">
          <p className="text-[10px] font-semibold text-gray-400 uppercase tracking-wider mb-0.5">Departman</p>
          <p className="text-xs text-gray-700 truncate">{row.department_id?.name ?? "—"}</p>
        </div>
      </div>
    </motion.div>
  );
}

export function TripsPage() {
  const { can } = useAuth();
  const { addToast } = useToast();
  const navigate = useNavigate();

  // DERİN BAĞLANTI: /seferler?sefer=<id> ile doğrudan o seferin kartı açılır.
  // Yük ekranındaki "Sefere Git" düğmesi buraya yönlendiriyor.
  const [searchParams, setSearchParams] = useSearchParams();

  // "Yüke Git": sefere bağlı yükün kartını Yükler ekranında açar. Yük modülünü
  // okuma yetkisi yoksa düğme gösterilmez — tıklayınca "Yetkiniz yok" ekranına
  // düşmek kullanıcıyı boşuna dolaştırırdı.
  const canOpenLoad = can("load_management", "read");

  const canCreate = can("expedition_management", "create");
  const canUpdate = can("expedition_management", "update");
  const canDelete = can("expedition_management", "delete");

  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);
  const [workTypeTab, setWorkTypeTab] = useState(WORK_TYPE_TABS[0]);
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [page, setPage] = useState(1);
  const [rows, setRows] = useState<ExpeditionItem[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);

  const [fExpeditionType, setFExpeditionType] = useState("");
  const [fStatus, setFStatus] = useState("");
  const [fDepartment, setFDepartment] = useState("");
  const [showAdvanced, setShowAdvanced] = useState(false);
  const hasActiveAdvancedFilters = !!(dateFrom || dateTo || fExpeditionType || fStatus || fDepartment);
  const hasActiveFilters = !!(search || hasActiveAdvancedFilters);

  function clearFilters() {
    setSearch("");
    setDateFrom("");
    setDateTo("");
    setFExpeditionType("");
    setFStatus("");
    setFDepartment("");
    setPage(1);
  }

  const [drawerOpen, setDrawerOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  const [form, setForm] = useState<TripForm>({ ...EMPTY_TRIP_FORM });

  // Kaydedilmemiş "Yeni Sefer" taslağı — bkz. TRIP_DRAFT_KEY açıklaması.
  const [tripDraft, setTripDraft] = useState<TripDraft | null>(() => readDraft<TripDraft>(TRIP_DRAFT_KEY));
  const [draftsOpen, setDraftsOpen] = useState(false);
  const draftsRef = useRef<HTMLDivElement>(null);
  // Geri yükleme sırasındaki ara state'ler taslağın üstüne yazmasın diye.
  const restoringDraftRef = useRef(false);

  const { options: workTypes } = useLookupOptions("/api/v1/work_type");
  const { options: departments, refresh: refreshDepartments } = useLookupOptions("/api/v1/department");
  const { options: expeditionTypes } = useLookupOptions("/api/v1/expedition_type");
  const { options: expeditionStatuses } = useLookupOptions("/api/v1/expedition_status");
  const { options: destinations } = useLookupOptions("/api/v1/destination");
  const { options: cities } = useLookupOptions("/api/v1/city");
  // olsold: ExpeditionFormDrawer.vue — Römork/Sefer Durumu/Sefer Tipi/Çalışma
  // Tipi/Departman alanlarının HEPSİNİN "Yeni Ekle" düğmesi kopyala-yapıştır
  // sonucu AYNI Departmanlar penceresini açıyor (yalnızca Departman alanı için
  // bu doğru). Kullanıcı isteğiyle birebir korunuyor.
  const [departmentModalOpen, setDepartmentModalOpen] = useState(false);

  function opts(list: { id: string | number; name: string }[]) {
    return [{ value: "", label: "Seçiniz" }, ...list.map((t) => ({ value: String(t.id), label: t.name }))];
  }

  const activeWorkTypeId = workTypeTab === "Tümü" ? undefined : workTypes.find((w) => w.name === workTypeTab)?.id;

  function load() {
    setLoading(true);
    api
      .get<DataMessage<Paginated<ExpeditionItem>>>("/api/v1/expedition", {
        search: debouncedSearch || undefined,
        date_from: dateFrom || undefined,
        date_to: dateTo || undefined,
        work_type_id: activeWorkTypeId || undefined,
        expedition_type_id: fExpeditionType || undefined,
        status_id: fStatus || undefined,
        department_id: fDepartment || undefined,
        per_page: PER_PAGE,
        page,
      })
      .then((res) => {
        setRows(res.data.data);
        setTotal(res.data.total);
      })
      .catch(() => addToast("Sefer listesi yüklenemedi", "error"))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch, workTypeTab, workTypes.length, dateFrom, dateTo, page, fExpeditionType, fStatus, fDepartment]);

  function openNew() {
    setForm({ ...EMPTY_TRIP_FORM });
    setErrors({});
    setDrawerOpen(true);
  }

  /**
   * Form her değiştiğinde tarayıcıya yazılır — kullanıcı kaydetmeden çıksa (hatta
   * sekmeyi kapatsa) bile Taslaklar menüsünden kaldığı yerden devam eder.
   */
  useEffect(() => {
    if (!drawerOpen || restoringDraftRef.current) return;
    if (!tripFormHasContent(form)) return;

    const timer = setTimeout(() => {
      const draft: TripDraft = { savedAt: new Date().toISOString(), form };
      writeDraft(TRIP_DRAFT_KEY, draft);
      setTripDraft(draft);
    }, 600);
    return () => clearTimeout(timer);
  }, [drawerOpen, form]);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (draftsRef.current && !draftsRef.current.contains(e.target as Node)) setDraftsOpen(false);
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  function resumeTripDraft() {
    const d = readDraft<TripDraft>(TRIP_DRAFT_KEY);
    if (!d) return;

    restoringDraftRef.current = true;
    setErrors({});
    setForm({ ...EMPTY_TRIP_FORM, ...d.form });
    setDrawerOpen(true);
    setDraftsOpen(false);
    setTimeout(() => { restoringDraftRef.current = false; }, 0);
  }

  function discardTripDraft() {
    clearDraft(TRIP_DRAFT_KEY);
    setTripDraft(null);
  }

  async function handleSubmit() {
    // olsold: ExpeditionFormDrawer.vue handleForm — bu 4 alan boşsa istemci
    // tarafında engelleniyor ("Lütfen tüm alanları doldurunuz."), backend'e hiç
    // gitmiyor (backend de aynı 4 alanı zorunlu tutuyor, burada ek bir güvenlik
    // ağı değil, kaynaktaki anında-engelleme UX'i taşınıyor).
    if (!form.expedition_type || !form.department_id || !form.work_type || !form.romork_id) {
      addToast("Lütfen tüm alanları doldurunuz.", "error");
      return;
    }
    // Buton disabled={saving} render'a kadar DOM'a yansımıyor — hızlı çift
    // tıklama/tekrar tetiklemeye karşı erken çıkış.
    if (saving) return;

    setSaving(true);
    setErrors({});
    const body: Record<string, unknown> = {
      romork_id: form.romork_id ? Number(form.romork_id) : null,
      work_type: form.work_type ? Number(form.work_type) : null,
      department_id: form.department_id ? Number(form.department_id) : null,
      expedition_type: form.expedition_type ? Number(form.expedition_type) : null,
      expedition_type_id: form.expedition_type ? Number(form.expedition_type) : null,
      release_date: form.release_date || null,
      entry_date: form.entry_date || null,
      loading_date: form.loading_date || null,
      return_date: form.return_date || null,
    };
    try {
      // YÜK EKLEME AKIŞI: yeni sefer formunda yük bağlama alanı OLAMAZ, çünkü
      // eşleme ucu (/api/v1/expedition_load_mapping) var olan bir sefer kimliği
      // istiyor — sefer kaydedilmeden bağlanacak bir şey yok. Eskiden kayıttan
      // sonra sadece liste yenileniyordu ve kullanıcı yükleri eklemek için
      // seferi listeden bulup tekrar açmak zorundaydı; "yük ekleme yeri yok"
      // şikâyeti buradan geliyordu.
      //
      // Uç zaten oluşturulan seferi geri döndürüyor; onu kullanıp kartı hemen
      // "Bağlı Yükler" sekmesinde açıyoruz. Akış kesintisiz: kaydet -> yük bağla.
      const created = await api.post<DataMessage<ExpeditionDetail>>("/api/v1/expedition", body);
      addToast("Sefer oluşturuldu");
      // Sefer artık sunucuda: kaydedilmemiş otomatik taslak gereksiz.
      discardTripDraft();
      setDrawerOpen(false);
      load();

      if (created?.data?.id) {
        await openDetail(created.data.id);
        setDetailTab("Bağlı Yükler");
      }
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
      await api.delete("/api/v1/expedition", { deletion_id: [id] });
      addToast("Sefer silindi");
      load();
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Silinemedi", "error");
    }
  }

  // --- Detay/düzenleme drawer'ı (Genel Bilgiler + Bağlı Yükler) ---

  const [detailOpen, setDetailOpen] = useState(false);
  const [detailId, setDetailId] = useState<number | null>(null);
  const [detailTab, setDetailTab] = useState(DETAIL_TABS[0]);
  const [detail, setDetail] = useState<ExpeditionDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailSaving, setDetailSaving] = useState(false);
  const [detailErrors, setDetailErrors] = useState<Record<string, string[]>>({});
  const [detailForm, setDetailForm] = useState({
    romork_id: "", romork_plate: "", work_type: "", department_id: "", expedition_type: "", status_id: "",
    release_date: "", entry_date: "", loading_date: "", return_date: "", car_exit_date: "",
    start_city_id: "", load_city_id: "", end_city_id: "",
  });

  const [mappings, setMappings] = useState<ExpeditionMapping[]>([]);
  const [mappingTotals, setMappingTotals] = useState<MappingTotals>(ZERO_TOTALS);
  const [mappingsLoading, setMappingsLoading] = useState(false);
  // olsold: ExpeditionLoad.vue — Accordion varsayılan kapalı, satır başına bağımsız aç/kapa.
  const [expandedMappings, setExpandedMappings] = useState<Set<number>>(new Set());

  const [movements, setMovements] = useState<MovementDetail[]>([]);
  const [deletedMovements, setDeletedMovements] = useState<MovementDetail[]>([]);
  const [deletedMovementsModalOpen, setDeletedMovementsModalOpen] = useState(false);
  const [movementModalOpen, setMovementModalOpen] = useState(false);
  const [movementForm, setMovementForm] = useState(EMPTY_MOVEMENT_FORM);
  const [savingMovement, setSavingMovement] = useState(false);

  const [pickerOpen, setPickerOpen] = useState(false);
  const [pickerSearch, setPickerSearch] = useState("");
  const debouncedPickerSearch = useDebouncedValue(pickerSearch);
  const [pickerResults, setPickerResults] = useState<AvailableLoad[]>([]);
  const [pickerLoading, setPickerLoading] = useState(false);

  // ?sefer=<id> geldiyse o seferin kartını aç.
  //
  // SIRA ÖNEMLİ: ilk sürümde parametre kartı AÇMADAN ÖNCE siliniyordu. Bu, aynı
  // tik içinde bir gezinme (setSearchParams) tetikliyor ve kartı açan durum
  // güncellemesiyle yarışıyordu — sonuç: Seferler ekranına gidiliyor ama kart
  // açılmıyordu. Artık önce açılıyor, parametre kart KAPANINCA temizleniyor.
  //
  // handledRef: aynı id için ikinci kez açmayı engeller. Parametre URL'de
  // kaldığı sürece bu efekt her searchParams değişiminde yeniden çalışır.
  const handledExpeditionRef = useRef<string | null>(null);

  useEffect(() => {
    const requested = searchParams.get("sefer");

    if (!requested) {
      handledExpeditionRef.current = null;
      return;
    }

    if (handledExpeditionRef.current === requested) return;
    handledExpeditionRef.current = requested;

    const id = Number(requested);
    if (Number.isFinite(id) && id > 0) openDetail(id);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchParams]);

  /** Kart kapanınca derin bağlantı parametresini temizle (yenilemede tekrar açılmasın). */
  function clearExpeditionDeepLink() {
    if (!searchParams.get("sefer")) return;

    setSearchParams((current) => {
      const next = new URLSearchParams(current);
      next.delete("sefer");
      return next;
    }, { replace: true });
  }

  async function openDetail(id: number) {
    setDetailId(id);
    setDetailTab(DETAIL_TABS[0]);
    setDetailOpen(true);
    setDetailLoading(true);
    setDetailErrors({});
    try {
      const res = await api.get<DataMessage<ExpeditionDetail>>(`/api/v1/expedition/${id}`);
      const d = res.data;
      setDetail(d);
      setDetailForm({
        romork_id: d.romork_id ? String(d.romork_id.id) : "",
        romork_plate: d.romork_id?.plate_number ?? "",
        work_type: d.work_type ? String(d.work_type.id) : "",
        department_id: d.department_id ? String(d.department_id.id) : "",
        expedition_type: d.expedition_type_id ? String(d.expedition_type_id.id) : "",
        status_id: d.status_id ? String(d.status_id.id) : "",
        release_date: d.release_date ?? "",
        entry_date: "",
        loading_date: d.loading_date ?? "",
        return_date: d.return_date ?? "",
        car_exit_date: d.car_exit_date ?? "",
        start_city_id: d.start_city_id ? String(d.start_city_id.id) : "",
        load_city_id: d.load_city_id ? String(d.load_city_id.id) : "",
        end_city_id: d.end_city_id ? String(d.end_city_id.id) : "",
      });
      loadMappings(id);
      fetchMovements(id);
    } catch {
      addToast("Sefer bilgileri yüklenemedi", "error");
      setDetailOpen(false);
    } finally {
      setDetailLoading(false);
    }
  }

  function loadMappings(id: number) {
    setMappingsLoading(true);
    api
      .get<ExpeditionMappingResponse>(`/api/v1/expedition_load_mapping/${id}`)
      .then((res) => {
        setMappings(res.data);
        setMappingTotals(res.total_expedition_values ?? ZERO_TOTALS);
      })
      .catch(() => addToast("Bağlı yükler yüklenemedi", "error"))
      .finally(() => setMappingsLoading(false));
  }

  function fetchMovements(expeditionId: number) {
    api
      .get<{ data: MovementDetail[]; deleted_movements: MovementDetail[] }>(`/api/v1/expedition/${expeditionId}/movements`)
      .then((res) => { setMovements(res.data); setDeletedMovements(res.deleted_movements ?? []); })
      .catch(() => { setMovements([]); setDeletedMovements([]); });
  }

  async function handleDetailSave() {
    if (!detailId) return;
    setDetailSaving(true);
    setDetailErrors({});
    try {
      await api.put("/api/v1/expedition", {
        id: detailId,
        romork_id: detailForm.romork_id ? Number(detailForm.romork_id) : null,
        work_type: detailForm.work_type ? Number(detailForm.work_type) : null,
        department_id: detailForm.department_id ? Number(detailForm.department_id) : null,
        expedition_type_id: detailForm.expedition_type ? Number(detailForm.expedition_type) : null,
        expedition_status_id: detailForm.status_id ? Number(detailForm.status_id) : null,
        release_date: detailForm.release_date || null,
        loading_date: detailForm.loading_date || null,
        return_date: detailForm.return_date || null,
        car_exit_date: detailForm.car_exit_date || null,
        start_city_id: detailForm.start_city_id || null,
        load_city_id: detailForm.load_city_id || null,
        end_city_id: detailForm.end_city_id || null,
      });
      addToast("Sefer güncellendi");
      load();
      openDetail(detailId);
    } catch (err) {
      if (err instanceof ApiError && err.errors) setDetailErrors(err.errors);
      else addToast(err instanceof Error ? err.message : "Kaydedilemedi", "error");
    } finally {
      setDetailSaving(false);
    }
  }

  useEffect(() => {
    if (!pickerOpen || !detailId) return;
    setPickerLoading(true);
    api
      .get<DataMessage<Paginated<AvailableLoad>>>("/api/v1/expedition_load_mapping", { search: debouncedPickerSearch || undefined, per_page: 8, page: 1 })
      .then((res) => setPickerResults(res.data.data))
      .catch(() => setPickerResults([]))
      .finally(() => setPickerLoading(false));
  }, [pickerOpen, debouncedPickerSearch, detailId]);

  /**
   * Siber arşiv evrağını açar. Dosya API üzerinden vekil geliyor (FTP adresi ve
   * parolası tarayıcıya verilmiyor) ve jetonlu istek gerektiği için blob'a
   * alınıp öyle açılıyor — düz bağlantı 401 dönerdi.
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

      setTimeout(() => URL.revokeObjectURL(url), 60_000);
    } catch {
      addToast("Evrak açılamadı", "error");
    }
  }

  /** Arşiv listesi — hem seferin kendi evrakları hem bağlı yüklerinki için. */
  function ArchiveList({ files, empty }: { files: SiberArchiveFile[]; empty: string }) {
    if (files.length === 0)
      return <p className="text-[11px] text-gray-400 py-2">{empty}</p>;

    return (
      <div className="space-y-1.5">
        {files.map((f) => (
          <div key={f.id} className="flex items-center gap-2 rounded-lg border border-gray-100 bg-gray-50/60 p-2">
            <FileText size={13} className="text-gray-400 shrink-0" />
            <button
              type="button"
              onClick={() => openArchiveFile(f)}
              className="flex-1 truncate text-left text-xs text-blue-600 hover:underline"
              title="Siber arşivinden aç"
            >
              {f.name ?? "—"}
            </button>
            {f.personal_data && (
              <span className="shrink-0 rounded border border-amber-200 bg-amber-50 px-1.5 py-0.5 text-[10px] font-medium text-amber-700">
                Kişisel veri
              </span>
            )}
            {f.restricted_groups && (
              <span className="shrink-0 rounded border border-purple-200 bg-purple-50 px-1.5 py-0.5 text-[10px] font-medium text-purple-700" title={f.restricted_groups}>
                Kısıtlı
              </span>
            )}
            <span className="shrink-0 text-[10px] text-gray-400">
              {f.created_at ? new Date(f.created_at).toLocaleDateString("tr-TR") : "—"}
              {f.created_by ? ` · ${f.created_by}` : ""}
            </span>
          </div>
        ))}
      </div>
    );
  }

  async function addMapping(loadTransferId: number) {
    if (!detailId) return;
    try {
      await api.post("/api/v1/expedition_load_mapping", { expedition_id: detailId, load_transfer_id: loadTransferId });
      addToast("Yük sefere bağlandı");
      setPickerOpen(false);
      loadMappings(detailId);
    } catch (err) {
      if (err instanceof ApiError) addToast(err.message, "error");
      else addToast(err instanceof Error ? err.message : "Yük bağlanamadı", "error");
    }
  }

  async function removeMapping(mappingId: number) {
    if (!detailId) return;
    if (!window.confirm("Bu yük seferden çıkarılsın mı?")) return;
    try {
      await api.delete("/api/v1/expedition_load_mapping", { deletion_id: [mappingId] });
      addToast("Yük seferden çıkarıldı");
      loadMappings(detailId);
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Çıkarılamadı", "error");
    }
  }

  function toggleMappingAccordion(mappingId: number) {
    setExpandedMappings((set) => {
      const next = new Set(set);
      if (next.has(mappingId)) next.delete(mappingId);
      else next.add(mappingId);
      return next;
    });
  }

  function openMovementModal() {
    setMovementForm(EMPTY_MOVEMENT_FORM);
    setMovementModalOpen(true);
  }

  async function saveMovement() {
    if (!detailId) return;
    if (!movementForm.destination_id || !movementForm.expedition_status_id) {
      addToast("Lütfen konum ve durum seçiniz", "error");
      return;
    }
    setSavingMovement(true);
    try {
      const fd = new FormData();
      fd.append("expedition_id", String(detailId));
      fd.append("destination_id", movementForm.destination_id);
      fd.append("expedition_status_id", movementForm.expedition_status_id);
      fd.append("description", movementForm.description);
      fd.append("address", movementForm.address);
      await api.postForm(`/api/v1/expedition/${detailId}/movements`, fd);
      addToast("Sefer hareketi eklendi");
      setMovementModalOpen(false);
      fetchMovements(detailId);
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Hareket kaydedilemedi", "error");
    } finally {
      setSavingMovement(false);
    }
  }

  async function deleteMovement(movementId: number) {
    if (!detailId) return;
    if (!window.confirm("Bu hareket silinsin mi?")) return;
    try {
      await api.delete(`/api/v1/expedition/${detailId}/movements/${movementId}`);
      fetchMovements(detailId);
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Hareket silinemedi", "error");
    }
  }

  return (
    <>
      <ModulePage
        title="Seferler"
        action={canCreate ? (
          <div className="flex items-center gap-2">
            {/* Kaydedilmemiş "Yeni Sefer" taslağı — Teklif'teki Taslaklar menüsüyle
                aynı desen (bkz. QuotesPage.tsx). Sefer'de sunucu taraflı taslak
                kavramı olmadığı için menü yalnızca bu girdiyi listeler. */}
            <div className="relative" ref={draftsRef}>
              <Btn variant="secondary" onClick={() => setDraftsOpen((o) => !o)}>
                <FileText size={14} />
                Taslaklar
                {tripDraft && (
                  <span className="ml-1 px-1.5 py-0.5 rounded-full bg-amber-100 text-amber-700 text-[10px] font-semibold">
                    1
                  </span>
                )}
              </Btn>
              {draftsOpen && (
                <div className="absolute z-30 mt-1 right-0 w-80 bg-white border border-gray-200 rounded-md shadow-2xl">
                  <div className="px-4 py-2.5 border-b border-gray-100">
                    <p className="text-xs font-semibold text-gray-700">Taslaklar</p>
                  </div>
                  {tripDraft ? (
                    <div className="flex items-stretch bg-amber-50/50">
                      <button
                        type="button"
                        onClick={resumeTripDraft}
                        className="flex-1 text-left px-4 py-2.5 hover:bg-amber-50 transition-colors"
                      >
                        <div className="flex items-center justify-between gap-2">
                          <p className="text-sm font-medium text-gray-800 truncate">
                            {workTypes.find((w) => String(w.id) === tripDraft.form.work_type)?.name
                              ?? (tripDraft.form.romork_plate || (tripDraft.form.romork_id ? `Araç #${tripDraft.form.romork_id}` : "Yeni sefer"))}
                          </p>
                          <span className="shrink-0 text-[10px] font-semibold text-amber-700">
                            Kaydedilmedi
                          </span>
                        </div>
                        <p className="text-[11px] text-gray-500 mt-0.5">
                          Kaldığı yerden devam et · {formatDraftTime(tripDraft.savedAt)}
                        </p>
                      </button>
                      <button
                        type="button"
                        onClick={discardTripDraft}
                        title="Taslağı sil"
                        className="px-3 text-gray-300 hover:text-red-500 transition-colors"
                      >
                        <Trash2 size={13} />
                      </button>
                    </div>
                  ) : (
                    <p className="text-xs text-gray-400 text-center py-6">Taslak bulunamadı.</p>
                  )}
                </div>
              )}
            </div>
            <Btn onClick={openNew}><Plus size={14} />Yeni Sefer</Btn>
          </div>
        ) : undefined}
      >
        <div className="bg-white border-b border-gray-200 px-6 py-4">
          <div className="flex items-center gap-2.5">
            <div className="flex-1 max-w-md">
              <TextInput value={search} onChange={(v) => { setSearch(v); setPage(1); }} placeholder="Genel arama: sefer no, plaka..." />
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
                <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-3 pt-4 mt-4 border-t border-gray-100">
                  <FormField label="Sefer Tipi">
                    <SelectInput value={fExpeditionType} onChange={(v) => { setFExpeditionType(v); setPage(1); }} options={opts(expeditionTypes)} />
                  </FormField>
                  <FormField label="Durum">
                    <SelectInput value={fStatus} onChange={(v) => { setFStatus(v); setPage(1); }} options={opts(expeditionStatuses)} />
                  </FormField>
                  <FormField label="Departman">
                    <SelectInput value={fDepartment} onChange={(v) => { setFDepartment(v); setPage(1); }} options={opts(departments)} />
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
            <EmptyState icon={Truck} title="Sefer bulunamadı" desc="Arama kriterlerine uygun sefer bulunamadı." />
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
                      <ExpeditionCard
                        key={r.id}
                        row={r}
                        index={i}
                        onClick={() => openDetail(r.id)}
                        canDelete={canDelete}
                        onDelete={() => handleDelete(r.id, r.expedition_number)}
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
        title="Yeni Sefer"
        subtitle="Yeni sefer kaydı oluştur"
        width="w-[720px]"
        footer={
          canCreate && (
            <div className="flex gap-2">
              <Btn onClick={handleSubmit} disabled={saving}>
                <BusyLabel busy={saving} busyText="Kaydediliyor...">Kaydet</BusyLabel>
              </Btn>
              <Btn variant="secondary" onClick={() => setDrawerOpen(false)}>İptal</Btn>
            </div>
          )
        }
      >
        <div className="p-8 grid grid-cols-2 gap-x-6 gap-y-5">
          <CarPicker
            label="Araç (Plaka)"
            required
            error={errors.romork_id?.[0]}
            value={form.romork_id ? { id: Number(form.romork_id), plate_number: form.romork_plate || null } : null}
            onChange={(v) => setForm((f) => ({
              ...f,
              romork_id: v ? String(v.id) : "",
              romork_plate: v?.plate_number ?? "",
            }))}
          />
          <FormField label="İş Tipi" required error={errors.work_type?.[0]}>
            <SelectInput value={form.work_type} onChange={(v) => setForm((f) => ({ ...f, work_type: v }))} options={opts(workTypes)} />
          </FormField>
          <FormField label="Departman" required error={errors.department_id?.[0]}>
            <SelectInput value={form.department_id} onChange={(v) => setForm((f) => ({ ...f, department_id: v }))} options={opts(departments)} />
            <button type="button" onClick={() => setDepartmentModalOpen(true)} className="mt-1 text-[11px] text-blue-600 hover:underline text-left">Yeni Ekle</button>
          </FormField>
          <FormField label="Sefer Tipi" required error={errors.expedition_type?.[0]}>
            <SelectInput value={form.expedition_type} onChange={(v) => setForm((f) => ({ ...f, expedition_type: v }))} options={opts(expeditionTypes)} />
          </FormField>
          <FormField label="Çıkış Tarihi" error={errors.release_date?.[0]}>
            <TextInput value={form.release_date} onChange={(v) => setForm((f) => ({ ...f, release_date: v }))} type="date" error={!!errors.release_date} />
          </FormField>
          <FormField label="Kayıt Tarihi">
            <TextInput value={form.entry_date} onChange={(v) => setForm((f) => ({ ...f, entry_date: v }))} type="date" />
          </FormField>
          <FormField label="Yükleme Tarihi" error={errors.loading_date?.[0]}>
            <TextInput value={form.loading_date} onChange={(v) => setForm((f) => ({ ...f, loading_date: v }))} type="date" error={!!errors.loading_date} />
          </FormField>
          <FormField label="Dönüş Tarihi" error={errors.return_date?.[0]}>
            <TextInput value={form.return_date} onChange={(v) => setForm((f) => ({ ...f, return_date: v }))} type="date" error={!!errors.return_date} />
          </FormField>
        </div>
      </Drawer>

      <Drawer
        open={detailOpen}
        onClose={() => { setDetailOpen(false); clearExpeditionDeepLink(); }}
        title={detail?.expedition_number ?? "Sefer"}
        subtitle={detail?.romork_id?.plate_number ?? undefined}
        width="w-[760px]"
        footer={
          detailTab === "Genel Bilgiler" && canUpdate ? (
            <div className="flex gap-2">
              <Btn onClick={handleDetailSave} disabled={detailSaving || detailLoading}>
                <BusyLabel busy={detailSaving} busyText="Kaydediliyor...">Kaydet</BusyLabel>
              </Btn>
              <Btn variant="secondary" onClick={() => { setDetailOpen(false); clearExpeditionDeepLink(); }}>İptal</Btn>
            </div>
          ) : undefined
        }
      >
        {detail?.siber_audit && (
          <div className="px-6 pt-4">
            <SiberAuditPanel audit={detail.siber_audit} />
          </div>
        )}

        <Tabs tabs={DETAIL_TABS} active={detailTab} onChange={setDetailTab} className="px-6" />
        {detailLoading ? (
          <div className="p-10 text-center text-sm text-gray-400">Yükleniyor...</div>
        ) : (
          detail && (
            <div className="p-8">
              {detailTab === "Genel Bilgiler" && (
                <div>
                  <div className="bg-gray-50 border border-gray-200 rounded-lg p-3 mb-5">
                    <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider mb-2">Sefer Bilgisi</p>
                    <div className="grid grid-cols-2 gap-2">
                      <div className="bg-white p-2 rounded-md border border-gray-200">
                        <div className="text-[10px] text-gray-500">Hafta</div>
                        <div className="text-xs font-semibold text-gray-800">{detail.year_week || "—"}</div>
                      </div>
                      <div className="bg-white p-2 rounded-md border border-gray-200">
                        <div className="text-[10px] text-gray-500">Kayıt Giriş Tarihi</div>
                        <div className="text-xs font-semibold text-gray-800">
                          {detail.registration_login_date ? new Date(detail.registration_login_date).toLocaleDateString("tr-TR") : "—"}
                        </div>
                      </div>
                    </div>
                  </div>
                  <div className="grid grid-cols-3 gap-x-6 gap-y-5">
                  <CarPicker
                    label="Araç (Plaka)"
                    required
                    error={detailErrors.romork_id?.[0]}
                    value={detailForm.romork_id
                      ? { id: Number(detailForm.romork_id), plate_number: detailForm.romork_plate || null }
                      : null}
                    onChange={(v) => setDetailForm((f) => ({
                      ...f,
                      romork_id: v ? String(v.id) : "",
                      romork_plate: v?.plate_number ?? "",
                    }))}
                  />
                  <FormField label="Durum" required error={detailErrors.expedition_status_id?.[0]}>
                    <SelectInput value={detailForm.status_id} onChange={(v) => setDetailForm((f) => ({ ...f, status_id: v }))} options={opts(expeditionStatuses)} />
                  </FormField>
                  <FormField label="İş Tipi" required error={detailErrors.work_type?.[0]}>
                    <SelectInput value={detailForm.work_type} onChange={(v) => setDetailForm((f) => ({ ...f, work_type: v }))} options={opts(workTypes)} />
                  </FormField>
                  <FormField label="Departman" required error={detailErrors.department_id?.[0]}>
                    <SelectInput value={detailForm.department_id} onChange={(v) => setDetailForm((f) => ({ ...f, department_id: v }))} options={opts(departments)} />
                    <button type="button" onClick={() => setDepartmentModalOpen(true)} className="mt-1 text-[11px] text-blue-600 hover:underline text-left">Yeni Ekle</button>
                  </FormField>
                  <FormField label="Sefer Tipi" required error={detailErrors.expedition_type?.[0]}>
                    <SelectInput value={detailForm.expedition_type} onChange={(v) => setDetailForm((f) => ({ ...f, expedition_type: v }))} options={opts(expeditionTypes)} />
                  </FormField>
                  <FormField label="Araç Çıkış Tarihi" error={detailErrors.car_exit_date?.[0]} hint="Sefer durumu 8 iken zorunlu.">
                    <TextInput value={detailForm.car_exit_date} onChange={(v) => setDetailForm((f) => ({ ...f, car_exit_date: v }))} type="date" error={!!detailErrors.car_exit_date} />
                  </FormField>
                  <FormField label="Çıkış Tarihi" error={detailErrors.release_date?.[0]} hint="Sefer durumu 8 iken zorunlu.">
                    <TextInput value={detailForm.release_date} onChange={(v) => setDetailForm((f) => ({ ...f, release_date: v }))} type="date" error={!!detailErrors.release_date} />
                  </FormField>
                  <FormField label="Yükleme Tarihi" error={detailErrors.loading_date?.[0]} hint="Sefer durumu 8 iken zorunlu.">
                    <TextInput value={detailForm.loading_date} onChange={(v) => setDetailForm((f) => ({ ...f, loading_date: v }))} type="date" error={!!detailErrors.loading_date} />
                  </FormField>
                  <FormField label="Dönüş Tarihi" error={detailErrors.return_date?.[0]} hint="Sefer durumu 8 iken zorunlu.">
                    <TextInput value={detailForm.return_date} onChange={(v) => setDetailForm((f) => ({ ...f, return_date: v }))} type="date" error={!!detailErrors.return_date} />
                  </FormField>
                  <FormField label="Başlangıç Şehri" error={detailErrors.start_city_id?.[0]} hint="Sefer durumu 8 iken zorunlu.">
                    <SelectInput value={detailForm.start_city_id} onChange={(v) => setDetailForm((f) => ({ ...f, start_city_id: v }))} options={opts(cities)} />
                  </FormField>
                  <FormField label="Yükleme Şehri" error={detailErrors.load_city_id?.[0]} hint="Sefer durumu 8 iken zorunlu.">
                    <SelectInput value={detailForm.load_city_id} onChange={(v) => setDetailForm((f) => ({ ...f, load_city_id: v }))} options={opts(cities)} />
                  </FormField>
                  <FormField label="Bitiş Şehri" error={detailErrors.end_city_id?.[0]} hint="Sefer durumu 8 iken zorunlu.">
                    <SelectInput value={detailForm.end_city_id} onChange={(v) => setDetailForm((f) => ({ ...f, end_city_id: v }))} options={opts(cities)} />
                  </FormField>
                  </div>
                </div>
              )}

              {detailTab === "Bağlı Yükler" && (
                <div>
                  {/* SEFERİN KENDİ evrakları — bağlı yüklerinkinden ayrı.
                      Siber'de ikisi farklı kayda bağlanıyor: sefer evrakı
                      pozisyonid'ye, yük evrakı yukid'ye. */}
                  <div className="mb-5 rounded-lg border border-gray-200 bg-gray-50/70 p-3">
                    <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider mb-2">
                      Sefer Evrakları (Siber Arşivi)
                    </p>
                    <ArchiveList
                      files={detail?.siber_archive ?? []}
                      empty="Bu sefer için Siber arşivinde evrak yok."
                    />
                  </div>

                  <div className="flex items-center justify-between mb-3">
                    <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider">Bağlı Yükler</p>
                    {canUpdate && (
                      <button type="button" onClick={() => { setPickerSearch(""); setPickerOpen(true); }} className="text-[11px] text-blue-600 hover:underline flex items-center gap-1">
                        <Link2 size={12} />Yük Bağla
                      </button>
                    )}
                  </div>

                  {mappingsLoading ? (
                    <p className="text-xs text-gray-400 text-center py-8">Yükleniyor...</p>
                  ) : mappings.length === 0 ? (
                    <p className="text-xs text-gray-400 text-center py-8">Bu sefere henüz yük bağlanmadı.</p>
                  ) : (
                    <>
                      {/* olsold: ExpeditionLoad.vue "Toplam Değerler" kartı — tüm bağlı yüklerin toplamı. */}
                      <div className="bg-gray-50 border border-gray-200 rounded-lg p-3 mb-3">
                        <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider mb-2">Toplam Değerler</p>
                        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-2">
                          <div className="bg-white p-2 rounded-md border border-gray-200">
                            <div className="text-[10px] text-gray-500">Toplam Adet</div>
                            <div className="text-sm font-semibold text-gray-800">{mappingTotals.total_quantity}</div>
                          </div>
                          <div className="bg-white p-2 rounded-md border border-gray-200">
                            <div className="text-[10px] text-gray-500">Brüt Ağırlık</div>
                            <div className="text-sm font-semibold text-gray-800">{mappingTotals.total_gross_weight} kg</div>
                          </div>
                          <div className="bg-white p-2 rounded-md border border-gray-200">
                            <div className="text-[10px] text-gray-500">Net Ağırlık</div>
                            <div className="text-sm font-semibold text-gray-800">{mappingTotals.total_net_weight} kg</div>
                          </div>
                          <div className="bg-white p-2 rounded-md border border-gray-200">
                            <div className="text-[10px] text-gray-500">Lademetre</div>
                            <div className="text-sm font-semibold text-gray-800">{mappingTotals.total_lademeter}</div>
                          </div>
                          <div className="bg-white p-2 rounded-md border border-gray-200">
                            <div className="text-[10px] text-gray-500">Hacim</div>
                            <div className="text-sm font-semibold text-gray-800">{mappingTotals.total_volume} m³</div>
                          </div>
                        </div>
                      </div>

                      <div className="space-y-3">
                        {mappings.map((m) => {
                          const expanded = expandedMappings.has(m.id);
                          const packages = m.load_transfer_id?.load_transfer_package ?? [];
                          return (
                            <div key={m.id} className="border border-gray-200 rounded-lg p-3">
                              <div className="flex items-start justify-between gap-3">
                                <div className="space-y-0.5">
                                  <p className="text-xs text-gray-700">
                                    Yük Numarası: <span className="font-semibold text-blue-600">{m.load_transfer_id?.load_number_work_type ?? `#${m.load_transfer_id?.id ?? "?"}`}</span>
                                  </p>
                                  <p className="text-xs text-gray-700">
                                    Müşteri: <span className="font-medium">{m.load_transfer_id?.customer_id?.name ?? "—"}</span>
                                  </p>
                                  <p className="text-xs text-gray-700">
                                    Römork: <span className="font-medium">{m.romork_id?.plate_number ?? "—"}</span>
                                  </p>
                                  <p className="text-xs text-gray-700">
                                    Şehir: <span className="font-medium">{m.yer_id?.name ?? "—"}</span>
                                    {" · "}
                                    {m.upload_unload === 1 ? "Yükleme" : m.upload_unload === 2 ? "Boşaltma" : "—"}
                                    {m.date && ` · ${new Date(m.date).toLocaleDateString("tr-TR")}`}
                                  </p>
                                </div>
                                <div className="flex items-center gap-1 shrink-0">
                                  {canOpenLoad && m.load_transfer_id?.id && (
                                    <button
                                      type="button"
                                      onClick={() => navigate(`/yukler?yuk=${m.load_transfer_id!.id}`)}
                                      title="Bu yükü Yükler ekranında aç"
                                      className="flex items-center gap-1 rounded-md border border-blue-200 bg-blue-50 px-2 py-1 text-[11px] font-medium text-blue-700 hover:bg-blue-100"
                                    >
                                      <ExternalLink size={12} />
                                      Yüke Git
                                    </button>
                                  )}
                                  {canUpdate && (
                                    <button type="button" onClick={() => removeMapping(m.id)} className="text-gray-300 hover:text-red-500">
                                      <Trash2 size={13} />
                                    </button>
                                  )}
                                </div>
                              </div>

                              <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-2 mt-3">
                                <div className="bg-gray-50 p-2 rounded-md">
                                  <div className="text-[10px] text-gray-500">Toplam Adet</div>
                                  <div className="text-xs font-semibold text-gray-800">{m.total_values.total_quantity}</div>
                                </div>
                                <div className="bg-gray-50 p-2 rounded-md">
                                  <div className="text-[10px] text-gray-500">Hacim</div>
                                  <div className="text-xs font-semibold text-gray-800">{m.total_values.total_volume} m³</div>
                                </div>
                                <div className="bg-gray-50 p-2 rounded-md">
                                  <div className="text-[10px] text-gray-500">Brüt Ağırlık</div>
                                  <div className="text-xs font-semibold text-gray-800">{m.total_values.total_gross_weight} kg</div>
                                </div>
                                <div className="bg-gray-50 p-2 rounded-md">
                                  <div className="text-[10px] text-gray-500">Net Ağırlık</div>
                                  <div className="text-xs font-semibold text-gray-800">{m.total_values.total_net_weight} kg</div>
                                </div>
                                <div className="bg-gray-50 p-2 rounded-md">
                                  <div className="text-[10px] text-gray-500">Lademetre</div>
                                  <div className="text-xs font-semibold text-gray-800">{m.total_values.total_lademeter}</div>
                                </div>
                              </div>

                              <button
                                type="button"
                                onClick={() => toggleMappingAccordion(m.id)}
                                className="w-full flex items-center justify-between mt-3 pt-2 border-t border-gray-100 text-[11px] font-medium text-gray-500 hover:text-gray-700"
                              >
                                Detaylar
                                {expanded ? <ChevronUp size={13} /> : <ChevronDown size={13} />}
                              </button>
                              {expanded && (
                                <div className="mt-2 space-y-2">
                                  {/* Bu yükün Siber arşivindeki evrakları — yük
                                      kartına gitmeden buradan açılabilsin. */}
                                  <div>
                                    <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider mb-1">
                                      Evraklar (Siber Arşivi)
                                    </p>
                                    <ArchiveList
                                      files={m.load_transfer_id?.siber_archive ?? []}
                                      empty="Bu yük için Siber arşivinde evrak yok."
                                    />
                                  </div>

                                  {packages.length === 0 ? (
                                    <p className="text-[11px] text-gray-400 text-center py-3">Bu yüke ait paket bulunamadı.</p>
                                  ) : (
                                    packages.map((p, i) => (
                                      <div key={p.id} className="border border-gray-100 rounded-lg p-3 bg-white">
                                        <p className="text-xs font-semibold text-gray-700 mb-2">{i + 1}. Ürün</p>
                                        <div className="flex flex-wrap gap-1.5">
                                          <span className="text-[11px] text-gray-500 py-1 px-2 bg-gray-50 rounded-md">Mal Cinsi: <b className="text-gray-700">{p.product_type_id?.name ?? "—"}</b></span>
                                          <span className="text-[11px] text-gray-500 py-1 px-2 bg-gray-50 rounded-md">Ambalaj: <b className="text-gray-700">{p.case_type_id?.name ?? "—"}</b></span>
                                          <span className="text-[11px] text-gray-500 py-1 px-2 bg-gray-50 rounded-md">Adet: <b className="text-gray-700">{p.quantity ?? 0}</b></span>
                                          <span className="text-[11px] text-gray-500 py-1 px-2 bg-gray-50 rounded-md">Hacim: <b className="text-gray-700">{p.volume ?? 0} m³</b></span>
                                          <span className="text-[11px] text-gray-500 py-1 px-2 bg-gray-50 rounded-md">Brüt Ağırlık: <b className="text-gray-700">{p.gross_weight ?? 0} kg</b></span>
                                          <span className="text-[11px] text-gray-500 py-1 px-2 bg-gray-50 rounded-md">Net Ağırlık: <b className="text-gray-700">{p.net_weight ?? 0} kg</b></span>
                                          <span className="text-[11px] text-gray-500 py-1 px-2 bg-gray-50 rounded-md">Lademetre: <b className="text-gray-700">{p.lademeter ?? 0}</b></span>
                                        </div>
                                      </div>
                                    ))
                                  )}
                                </div>
                              )}
                            </div>
                          );
                        })}
                      </div>
                    </>
                  )}
                </div>
              )}

              {detailTab === "Hareketler" && (
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
                {m.description && <p className="text-xs text-gray-500 mt-2 pt-2 border-t border-gray-100">{m.description}</p>}
              </div>
            ))
          )}
        </div>
      </Modal>

      <Modal open={pickerOpen} onClose={() => setPickerOpen(false)} title="Sefere Yük Bağla">
        <div className="w-[420px] max-w-full">
          <SearchInput value={pickerSearch} onChange={setPickerSearch} placeholder="Yük numarası..." />
          <div className="mt-3 max-h-80 overflow-y-auto space-y-1">
            {pickerLoading ? (
              <p className="text-xs text-gray-400 text-center py-6">Yükleniyor...</p>
            ) : pickerResults.length === 0 ? (
              <p className="text-xs text-gray-400 text-center py-6">Eklenebilecek yük bulunamadı.</p>
            ) : (
              pickerResults.map((r) => (
                <button
                  key={r.id}
                  type="button"
                  onClick={() => addMapping(r.id)}
                  className="w-full text-left px-3 py-2 rounded-lg text-sm hover:bg-blue-50 flex items-center justify-between"
                >
                  <span>
                    <span className="text-blue-700 font-medium">{r.load_number_work_type ?? `#${r.id}`}</span>
                    <span className="text-gray-500 ml-2">{r.customer_id?.name ?? "—"}</span>
                  </span>
                  {r.load_status_id?.name && <Badge label={r.load_status_id.name} />}
                </button>
              ))
            )}
          </div>
        </div>
      </Modal>

      <DepartmentManagerModal open={departmentModalOpen} onClose={() => setDepartmentModalOpen(false)} onSaved={refreshDepartments} />
    </>
  );
}
