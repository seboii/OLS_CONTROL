import { useEffect, useRef, useState, type UIEvent } from "react";
import { motion, AnimatePresence } from "motion/react";
import { clsx } from "clsx";
import { useNavigate } from "react-router-dom";
import { FileText, Package, Plus, Trash2, Truck, Upload, Download, File as FileIcon, X, Filter, ChevronDown, CalendarDays, Globe, CreditCard, Building2, StickyNote, User, MoreVertical, Copy } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { api, ApiError, downloadFile, type DataMessage, type Paginated } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useDebouncedValue, useLookupOptions } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { ModulePage } from "@/components/ui/ModulePage";
import { EmptyState, Pagination } from "@/components/ui/DataTable";
import { Drawer, Modal } from "@/components/ui/Overlay";
import { Btn, FormField, SelectInput, Tabs, TextInput, TextareaInput } from "@/components/ui/primitives";
import { AccountPicker, type AccountOption } from "@/components/shared/AccountPicker";
import { UserPicker, type UserOption } from "@/components/shared/UserPicker";
import { FinancialItemPicker, type FinancialItemOption } from "@/components/shared/FinancialItemPicker";
import { LookupPicker, type LookupOption } from "@/components/shared/LookupPicker";
import { clearDraft, formatDraftTime, readDraft, writeDraft } from "@/lib/autodraft";
import { BusyLabel, FullScreenBusy } from "@/components/ui/Busy";
import { SiberAuditPanel, type SiberAuditInfo } from "@/components/shared/SiberAudit";
import { RecordHistoryTab } from "@/components/shared/RecordHistory";

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
  item: FinancialItemOption | null;
  currency: { id: number; name: string | null } | null;
  account_id: AccountOption | null;
  transport_type_id: NamedRef | null;
}

interface SiberArchiveFile {
  id: string;
  name: string | null;
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

interface LoadDetail {
  siber_audit?: SiberAuditInfo | null;
  id: number;
  reservation_number: string | null;
  load_number: string | null;
  siber_id: string | null;
  offer_date: string | null;
  /** Olumlu'ya cekilme gunu — sunucu damgalar, form salt-okunur gosterir. */
  approval_date: string | null;
  offer_validity_date: string | null;
  marketing_notification_date: string | null;
  description: string | null;
  /** Teklif Olumsuz ise gerekcesi (bkz. Load.RejectionReason). */
  rejection_reason: string | null;
  /** Teklifin Siber arşivindeki evrakları. */
  siber_archive: SiberArchiveFile[];
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
  product_type_id: LookupOption | null; case_type_id: LookupOption | null; quantity: string;
  gross_weight: string; net_weight: string; volume: string; lademeter: string;
  width: string; height: string; length: string; stackable: string;
};

type FinancialItemRow = {
  item: FinancialItemOption | null; transport_type_id: string; account: AccountOption | null;
  description: string; order: string; buysell: string; currency: string;
  net_price: string; total_price: string; quantity: string;
};

const EMPTY_CONTENT_ROW: ContentRow = {
  product_type_id: null, case_type_id: null, quantity: "1", gross_weight: "",
  net_weight: "", volume: "", lademeter: "", width: "", height: "", length: "", stackable: "1",
};

const EMPTY_FINANCIAL_ROW: FinancialItemRow = {
  item: null, transport_type_id: "", account: null, description: "", order: "1",
  buysell: "1", currency: "", net_price: "", total_price: "", quantity: "1",
};

/**
 * Kaydedilmemiş "Yeni Teklif" otomatik taslağı.
 *
 * Sunucudaki "Taslaklar" (is_draft=1) yalnızca KAYDEDİLMİŞ ama içeriği/finansı
 * eksik kalmış teklifleri gösterir — kullanıcı formu doldururken kaydetmeden
 * çıkarsa her şey kayboluyordu. Bu taslak, form her değiştiğinde tarayıcıya
 * yazılır ve Taslaklar menüsünde en üstte "kaldığı yerden devam" girdisi olarak
 * çıkar. Sunucuya yazılmaz: yarım form zaten doğrulamadan geçmez ve her yarım
 * denemede Siber'e/veritabanına çöp kayıt açmak istemiyoruz.
 */
const LOCAL_DRAFT_KEY = "ols.quote.autodraft.v1";

type LocalDraft = {
  savedAt: string;
  form: Record<string, string>;
  customer: AccountOption | null;
  sender: AccountOption | null;
  receiver: AccountOption | null;
  agent: AccountOption | null;
  companyPayFreight: AccountOption | null;
  route: { departure_country_id: string; transit_country_id: string; target_country_id: string };
  operationOfficer: UserOption | null;
  salesReps: UserOption[];
  content: ContentRow[];
  financialItems: FinancialItemRow[];
  emailTo: string[];
  emailCc: string[];
  tab: string;
};

const readLocalDraft = () => readDraft<LocalDraft>(LOCAL_DRAFT_KEY);
const writeLocalDraft = (draft: LocalDraft) => writeDraft(LOCAL_DRAFT_KEY, draft);
const clearLocalDraft = () => clearDraft(LOCAL_DRAFT_KEY);

/** Kullanıcı gerçekten bir şey doldurdu mu — boş formu taslak diye kaydetmeyelim. */
function draftHasContent(d: Omit<LocalDraft, "savedAt">): boolean {
  if (d.customer || d.sender || d.receiver || d.agent || d.companyPayFreight) return true;
  if (d.operationOfficer) return true;
  if (d.route.departure_country_id || d.route.transit_country_id || d.route.target_country_id) return true;
  if (d.financialItems.length > 0) return true;
  if (d.emailTo.length > 0 || d.emailCc.length > 0) return true;
  if (d.form.description?.trim()) return true;
  if (d.form.work_type_id || d.form.loading_type_id || d.form.load_transfer_type_id) return true;
  if (d.form.instruction_id || d.form.romork_type_id || d.form.payer_company?.trim()) return true;
  return d.content.some(
    (c) => c.product_type_id || c.case_type_id || c.gross_weight || c.volume ||
      c.width || c.length || c.height || c.net_weight || c.lademeter,
  );
}

// En/boy (cm) -> lademetre. Referans Laravel uygulamasıyla aynı formül: (en * boy) / 24000.
function computeLademeter(widthCm: string, lengthCm: string): string {
  const w = parseFloat(widthCm);
  const l = parseFloat(lengthCm);
  return Number.isFinite(w) && Number.isFinite(l) && w > 0 && l > 0 ? ((w * l) / 24000).toFixed(2) : "";
}

const PER_PAGE = 24;
// olsnew: OfferFormDrawer.vue TabList — Genel Bilgiler/Yük İçeriği/Finans/Görevliler/
// Dosya Arşivi/E-Posta Ayarları (yapı olsnew ile birebir; "İlgili E-Posta" AI-mail
// sekmesi kapsam dışı — bkz. Mail Analizi hariç tutma kararı).
const TABS = ["Genel Bilgiler", "Yük İçeriği", "Finans", "Görevliler", "Dosya Arşivi", "E-Posta Ayarları", "İşlem Geçmişi"];

// status_types tablosundaki sabit satırlar (gerçek Siber verisiyle eşleşir; backend
// tarafında da aynı sabitler var — LoadController/LoadWriteService).
const NEGATIVE_STATUS_ID = 1;
const POSITIVE_STATUS_ID = 5;
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

/** olsnew: OfferFormDrawer.vue'daki ikon + başlık deseni (Tarih/Konum/Ödeme/Şirketler/...). */
function SectionHeader({ icon: Icon, title }: { icon: React.ComponentType<{ size?: number }>; title: string }) {
  return (
    <div className="flex items-center gap-3 mb-4">
      <Icon size={18} />
      <div className="text-sm font-semibold text-gray-800">{title}</div>
    </div>
  );
}

/**
 * Kart üstündeki üç nokta menüsü.
 *
 * Silme düğmesinin yerini aldı: kartta tek bir yıkıcı işlem dururken yanlışlıkla
 * tıklanma riski vardı ve başka işlem eklenecek yer yoktu. Menü dışarı
 * tıklandığında ve Escape ile kapanır; kart tıklamasını tetiklememesi için her
 * seviyede stopPropagation uygulanır (kartın kendisi tıklanınca teklif açılıyor).
 */
function CardMenu({ items }: {
  items: { label: string; icon: LucideIcon; onSelect: () => void; danger?: boolean }[];
}) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;

    function onDown(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    }
    function onKey(e: KeyboardEvent) {
      if (e.key === "Escape") setOpen(false);
    }

    document.addEventListener("mousedown", onDown);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("mousedown", onDown);
      document.removeEventListener("keydown", onKey);
    };
  }, [open]);

  if (items.length === 0) return null;

  return (
    <div className="relative shrink-0" ref={ref} onClick={(e) => e.stopPropagation()}>
      <button
        type="button"
        title="İşlemler"
        onClick={(e) => { e.stopPropagation(); setOpen((v) => !v); }}
        className="p-1.5 rounded text-gray-400 hover:text-gray-700 hover:bg-gray-100 transition-colors"
      >
        <MoreVertical size={15} />
      </button>

      {open && (
        <div className="absolute right-0 z-30 mt-1 w-52 rounded-lg border border-gray-200 bg-white py-1 shadow-lg">
          {items.map((item) => (
            <button
              key={item.label}
              type="button"
              onClick={(e) => { e.stopPropagation(); setOpen(false); item.onSelect(); }}
              className={clsx(
                "flex w-full items-center gap-2 px-3 py-2 text-left text-xs transition-colors",
                item.danger
                  ? "text-red-600 hover:bg-red-50"
                  : "text-gray-700 hover:bg-gray-50",
              )}
            >
              <item.icon size={13} className="shrink-0" />
              {item.label}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

function QuoteCard({
  row, index, onClick, canCreate, canDelete, onTransferToSiber, onConvertToLoad,
  onDelete, onDuplicate, onOpenLoad,
}: {
  row: LoadItem; index: number; onClick: () => void; canCreate: boolean; canDelete: boolean;
  onTransferToSiber: () => void; onConvertToLoad: () => void; onDelete: () => void;
  onDuplicate: () => void; onOpenLoad: () => void;
}) {
  const date = row.offer_date ? new Date(row.offer_date).toLocaleDateString("tr-TR") : null;

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
            <FileText size={16} />
          </div>
          <div className="min-w-0">
            {/* Rezervasyon numarasını SİBER üretir (MAX(rezervasyonno)+1, kilit altında —
                bkz. SiberReservationRepository.InsertRezervasyonWithLockedNumberAsync) ve
                yalnızca "Siber'e Aktar" adımında atanır. Burada eskiden yerel id'den
                türetilen sahte bir "T{id}" gösteriliyordu; gerçek numarayla (2615566)
                karıştırıldığı için kaldırıldı — numara yoksa açıkça öyle yazıyor. */}
            {row.reservation_number ? (
              <p className="font-mono text-xs font-semibold text-blue-600 truncate">{row.reservation_number}</p>
            ) : (
              <p className="text-xs font-medium text-gray-400 truncate">Numara atanmadı</p>
            )}
            {row.load_number && <p className="text-[10px] text-gray-400 mt-0.5 truncate">Yük No: {row.load_number}</p>}
          </div>
        </div>
        <CardMenu
          items={[
            // Kopyalama teklif AÇMA yetkisine bağlı — kopya yeni bir kayıt.
            ...(canCreate ? [{ label: "Teklifi Kopyala", icon: Copy, onSelect: onDuplicate }] : []),
            // Yalnızca yüke dönüşmüş tekliflerde anlamlı.
            ...(row.load_number ? [{ label: "Yüke Git", icon: Truck, onSelect: onOpenLoad }] : []),
            ...(canDelete ? [{ label: "Teklifi Sil", icon: Trash2, onSelect: onDelete, danger: true }] : []),
          ]}
        />
      </div>

      <div className="pt-3 border-t border-gray-100">
        <p className="text-[10px] font-semibold text-gray-400 uppercase tracking-wider mb-0.5">Müşteri</p>
        <p className="text-sm font-semibold text-gray-900 truncate">{row.customer_id?.name ?? "—"}</p>
        {row.customer_id?.country_id?.name && <p className="text-[11px] text-gray-500 mt-0.5 truncate">{row.customer_id.country_id.name}</p>}
      </div>

      <div className="grid grid-cols-2 gap-3 pt-2.5 border-t border-gray-100">
        <div className="min-w-0">
          <p className="text-[10px] font-semibold text-gray-400 uppercase tracking-wider mb-0.5">Yük Tipi</p>
          <p className="text-xs text-gray-700 truncate">{row.loading_type_id?.name ?? "—"}</p>
        </div>
        <div className="min-w-0">
          <p className="text-[10px] font-semibold text-gray-400 uppercase tracking-wider mb-0.5">Yük Türü</p>
          <p className="text-xs text-gray-700 truncate">{row.work_type_id?.name ?? "—"}</p>
        </div>
      </div>

      <div className="flex items-center justify-between gap-2 pt-2.5 border-t border-gray-100 min-w-0">
        <div className="flex items-center gap-3 text-[11px] text-gray-500 min-w-0">
          <span className="font-mono shrink-0">{row.load_content_count} kalem</span>
          {date && (
            <span className="flex items-center gap-1 shrink-0">
              <CalendarDays size={11} />
              {date}
            </span>
          )}
        </div>
        <ChargePersonsCell people={row.load_charge_person} />
      </div>

      <QuoteStageBar
        row={row}
        canCreate={canCreate}
        onTransferToSiber={onTransferToSiber}
        onConvertToLoad={onConvertToLoad}
      />
    </motion.div>
  );
}

/**
 * Teklif → Yük akışının GÖRÜNÜR adım çubuğu.
 *
 * Kullanıcı isteği: eskiden bu akış kartın köşesindeki 14px'lik iki simgeydi
 * ("Siber'e Aktar" / "Yüke Dönüştür") — hangisinin ne yaptığı ve sıranın ne
 * olduğu anlaşılmıyordu. Artık her kart, o an NEREDE olduğunu yazıyla gösterir
 * ve YALNIZCA sıradaki tek adımı tam etiketli bir düğme olarak sunar.
 *
 * Sıra: Teklif → (Olumlu/Olumsuz) → Siber'e Aktar → Yük Oluştur.
 */
function QuoteStageBar({ row, canCreate, onTransferToSiber, onConvertToLoad }: {
  row: LoadItem; canCreate: boolean;
  onTransferToSiber: () => void; onConvertToLoad: () => void;
}) {
  const stop = (fn: () => void) => (e: React.MouseEvent) => { e.stopPropagation(); fn(); };

  // Yük oluşmuş: akış tamamlandı.
  if (row.load_number) {
    return (
      <div className="flex items-center gap-2 pt-3 border-t border-gray-100 text-emerald-700">
        <Truck size={14} className="shrink-0" />
        <span className="text-xs font-semibold">Yük oluşturuldu</span>
        <span className="ml-auto font-mono text-xs">{row.load_number}</span>
      </div>
    );
  }

  if (row.status_type_id === NEGATIVE_STATUS_ID) {
    return (
      <div className="flex items-center gap-2 pt-3 border-t border-gray-100 text-red-600">
        <X size={14} className="shrink-0" />
        <span className="text-xs font-semibold">Olumsuz — akış durdu</span>
      </div>
    );
  }

  // Olumlu değilse bir sonraki adım kullanıcının karar vermesi: kartı açıp
  // Durum'u Olumlu/Olumsuz yapması gerekiyor.
  if (row.status_type_id !== POSITIVE_STATUS_ID) {
    return (
      <div className="flex items-center gap-2 pt-3 border-t border-gray-100 text-gray-400">
        <span className="text-xs">Sıradaki adım:</span>
        <span className="text-xs font-medium text-gray-600">Olumlu / Olumsuz belirle</span>
      </div>
    );
  }

  if (!canCreate) return null;

  // Olumlu ve henüz Siber'e gitmemiş → 1. adım.
  if (!row.siber_id) {
    return (
      <div className="pt-3 border-t border-gray-100">
        <button
          type="button"
          onClick={stop(onTransferToSiber)}
          className="w-full flex items-center justify-center gap-2 px-3 py-2 rounded-lg bg-blue-600 text-white text-xs font-semibold hover:bg-blue-700 transition-colors"
        >
          <Package size={14} />
          1. Adım — Siber'e Aktar
        </button>
        {!row.reservation_number && (
          <p className="text-[10px] text-gray-400 text-center mt-1.5">
            Teklif numarası bu adımda Siber tarafından atanır
          </p>
        )}
      </div>
    );
  }

  // Siber'de var, yük yok → 2. adım.
  return (
    <div className="pt-3 border-t border-gray-100">
      <button
        type="button"
        onClick={stop(onConvertToLoad)}
        className="w-full flex items-center justify-center gap-2 px-3 py-2 rounded-lg bg-emerald-600 text-white text-xs font-semibold hover:bg-emerald-700 transition-colors"
      >
        <Truck size={14} />
        2. Adım — Yük Oluştur
      </button>
    </div>
  );
}

/**
 * Salt-okunur görevli satırı. Görevliler türetildiği için seçici gösterilmez;
 * kullanıcının NE KAYDEDİLECEĞİNİ görmesi yeterli.
 */
function ReadOnlyPerson({
  label, person, hint, empty,
}: {
  label: string;
  person: UserOption | null;
  hint?: string;
  empty?: string;
}) {
  const name = person && person.id
    ? [person.name, person.surname].filter(Boolean).join(" ").trim() || `#${person.id}`
    : null;

  return (
    <div>
      <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider mb-1">{label}</p>
      <div className="flex items-center gap-2 rounded-lg border border-gray-200 bg-gray-50 px-3 py-2">
        <User size={14} className="text-gray-400 shrink-0" />
        {name ? (
          <>
            <span className="text-sm text-gray-800">{name}</span>
            {hint && <span className="ml-auto text-[11px] text-gray-400">{hint}</span>}
          </>
        ) : (
          <span className="text-xs text-gray-400">{empty ?? "Belirlenmedi"}</span>
        )}
      </div>
    </div>
  );
}

export function QuotesPage() {
  const { can, user: currentUser } = useAuth();
  const navigate = useNavigate();
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

  // Taslaklar: Yük İçeriği veya Finans sekmesi eksik bırakılmış, henüz Yük'e
  // dönüşmemiş teklifler ("taslak mantığı" — bkz. LoadController.Validate).
  // Ayrı bir sayfa yerine küçük bir açılır menüde listeleniyor.
  const [draftsOpen, setDraftsOpen] = useState(false);
  const [draftItems, setDraftItems] = useState<LoadItem[]>([]);
  const [draftsLoading, setDraftsLoading] = useState(false);
  const [draftsLoadingMore, setDraftsLoadingMore] = useState(false);
  const [draftsTotal, setDraftsTotal] = useState<number | null>(null);
  const [draftsPage, setDraftsPage] = useState(1);
  const [draftsHasMore, setDraftsHasMore] = useState(false);
  const draftsRef = useRef<HTMLDivElement>(null);
  const DRAFTS_PAGE_SIZE = 10;
  // Kaydedilmemiş "Yeni Teklif" otomatik taslağı — bkz. LOCAL_DRAFT_KEY açıklaması.
  const [localDraft, setLocalDraft] = useState<LocalDraft | null>(() => readLocalDraft());
  // Taslak geri yüklenirken otomatik kaydediciyi susturur (aksi hâlde geri yükleme
  // sırasındaki ara state'ler taslağın üstüne yazardı).
  const restoringDraftRef = useRef(false);

  const [fCustomer, setFCustomer] = useState<AccountOption | null>(null);
  const [fSender, setFSender] = useState<AccountOption | null>(null);
  const [fReceiver, setFReceiver] = useState<AccountOption | null>(null);
  const [fAgent, setFAgent] = useState<AccountOption | null>(null);
  const [fAssignedUser, setFAssignedUser] = useState<UserOption | null>(null);
  const [fWorkType, setFWorkType] = useState("");
  const [showAdvanced, setShowAdvanced] = useState(false);
  const hasActiveAdvancedFilters = !!(
    dateFrom || dateTo || fCustomer || fSender || fReceiver || fAgent || fAssignedUser || fWorkType
  );
  const hasActiveFilters = !!(search || hasActiveAdvancedFilters);

  function clearFilters() {
    setSearch("");
    setDateFrom("");
    setDateTo("");
    setFCustomer(null);
    setFSender(null);
    setFReceiver(null);
    setFAgent(null);
    setFAssignedUser(null);
    setFWorkType("");
    setPage(1);
  }

  // Uzun süren Siber işlemleri için bekleme göstergesi (bkz. components/ui/Busy.tsx).
  // Metin, hangi adımda olduğumuzu söyler; null ise işlem yok.
  const [busyLabel, setBusyLabel] = useState<string | null>(null);

  const [drawerOpen, setDrawerOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [tab, setTab] = useState(TABS[0]);
  const [saving, setSaving] = useState(false);
  const [detailLoading, setDetailLoading] = useState(false);
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  // "Sibere Aktar" / "Yüke Dönüştür" düğmelerinin durumu için — bkz. QuoteCard'daki
  // aynı mantık (row.siber_id && canCreate).
  const [detailMeta, setDetailMeta] = useState<{
    siberId: string | null; reservationNumber: string | null; loadNumber: string | null;
    approvalDate: string | null; siberAudit: SiberAuditInfo | null;
  }>({ siberId: null, reservationNumber: null, loadNumber: null, approvalDate: null, siberAudit: null });

  const [form, setForm] = useState({
    work_type_id: "", loading_type_id: "", payment_type_id: "", status_type_id: "",
    load_transfer_type_id: "", instruction_id: "", romork_type_id: "", department_id: "",
    offer_date: new Date().toISOString().slice(0, 10), offer_validity_date: "",
    marketing_notification_date: new Date().toISOString().slice(0, 10),
    payer_company: "", description: "", rejection_reason: "", way_of_working: "0",
    front_transportation_by_us: "0", final_transportation_by_us: "0",
  });
  const [customer, setCustomer] = useState<AccountOption | null>(null);
  const [sender, setSender] = useState<AccountOption | null>(null);
  const [receiver, setReceiver] = useState<AccountOption | null>(null);
  const [agent, setAgent] = useState<AccountOption | null>(null);
  const [companyPayFreight, setCompanyPayFreight] = useState<AccountOption | null>(null);
  const [route, setRoute] = useState({ departure_country_id: "", transit_country_id: "", target_country_id: "" });
  // GÖREVLİLER ARTIK ELLE SEÇİLMİYOR — ikisi de türetilir ve salt-okunur gösterilir:
  //   • Operasyon Yetkilisi = o an giriş yapmış kullanıcı. Kullanıcının Siber
  //     karşılığı yoksa (kurulum admini: siber_code NULL) müşteriye tanımlı
  //     operasyon yetkilisine düşülür — aksi hâlde Siber'e boş alan giderdi.
  //   • Satış Temsilcisi   = müşteriye tanımlı satış temsilcileri.
  // Aynı kural sunucuda da uygulanıyor (LoadWriteService.WriteChargePersonsAsync);
  // burası yalnızca kullanıcıya ne kaydedileceğini GÖSTERİR. Bu yüzden form
  // artık load_charge_person alanlarını göndermiyor.
  const [operationOfficer, setOperationOfficer] = useState<UserOption | null>(null);
  const [salesReps, setSalesReps] = useState<UserOption[]>([]);

  const [content, setContent] = useState<ContentRow[]>([{ ...EMPTY_CONTENT_ROW }]);
  const [financialItems, setFinancialItems] = useState<FinancialItemRow[]>([]);
  const [existingFiles, setExistingFiles] = useState<LoadFileDetail[]>([]);
  // Siber arşivi teklifin kendi kaydına bağlı (rezervasyonid); yerel yüklenen
  // dosyalardan ayrı tutulur — sahibi Siber, buradan silinemez.
  const [siberArchive, setSiberArchive] = useState<SiberArchiveFile[]>([]);
  const [removedFileIds, setRemovedFileIds] = useState<number[]>([]);
  const [newFiles, setNewFiles] = useState<File[]>([]);
  const [savingFiles, setSavingFiles] = useState(false);
  // olsold "E-Posta Ayarları" sekmesi: offer_data.email.to / .cc (serbest metin, çoğul).
  const [emailTo, setEmailTo] = useState<string[]>([]);
  const [emailCc, setEmailCc] = useState<string[]>([]);

  const { options: workTypes } = useLookupOptions("/api/v1/work_type");
  const { options: loadingTypes } = useLookupOptions("/api/v1/loading_type");
  const { options: paymentTypes } = useLookupOptions("/api/v1/payment_type");
  const { options: statusTypes } = useLookupOptions("/api/v1/status_type");
  // Olumsuz gerekcesi yalnizca bu durumda gosterilir/zorunludur.
  const isNegativeStatus =
    !!form.status_type_id &&
    form.status_type_id === String(statusTypes.find((s) => s.name === "Olumsuz")?.id ?? "");
  // Siber'e Aktar / Yük Oluştur adımlari yalnizca Olumlu teklifte anlamli.
  const isPositiveStatus =
    !!form.status_type_id &&
    form.status_type_id === String(statusTypes.find((s) => s.name === "Olumlu")?.id ?? "");
  const { options: departments } = useLookupOptions("/api/v1/department");
  const { options: instructions } = useLookupOptions("/api/v1/instruction");
  const { options: romorkTypes } = useLookupOptions("/api/v1/romork_type");
  const { options: loadTransferTypes } = useLookupOptions("/api/v1/load_transfer_type");
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
        customer_id: fCustomer?.id || undefined,
        sender_id: fSender?.id || undefined,
        receiver_id: fReceiver?.id || undefined,
        agent_id: fAgent?.id || undefined,
        assigned_user_id: fAssignedUser?.id || undefined,
        work_type_id: fWorkType || undefined,
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
  }, [
    debouncedSearch, dateFrom, dateTo, page, listTab, statusTypes.length,
    fCustomer, fSender, fReceiver, fAgent, fAssignedUser, fWorkType,
  ]);

  // Buton rozetindeki taslak sayısı: menü hiç açılmasa bile görünsün diye
  // sayfa açılışında bir kere (yalnızca toplam sayı için, per_page=1) çekilir.
  useEffect(() => {
    api
      .get<DataMessage<Paginated<LoadItem>>>("/api/v1/load", { is_draft: 1, per_page: 1, page: 1 })
      .then((res) => setDraftsTotal(res.data.total))
      .catch(() => {});
  }, []);

  useEffect(() => {
    if (!draftsOpen) return;
    setDraftsLoading(true);
    api
      .get<DataMessage<Paginated<LoadItem>>>("/api/v1/load", { is_draft: 1, per_page: DRAFTS_PAGE_SIZE, page: 1 })
      .then((res) => {
        setDraftItems(res.data.data);
        setDraftsTotal(res.data.total);
        setDraftsPage(1);
        setDraftsHasMore(res.data.current_page < res.data.last_page);
      })
      .catch(() => addToast("Taslaklar yüklenemedi", "error"))
      .finally(() => setDraftsLoading(false));
  }, [draftsOpen]);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (draftsRef.current && !draftsRef.current.contains(e.target as Node)) setDraftsOpen(false);
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  function loadMoreDrafts() {
    if (draftsLoading || draftsLoadingMore || !draftsHasMore) return;
    const nextPage = draftsPage + 1;
    setDraftsLoadingMore(true);
    api
      .get<DataMessage<Paginated<LoadItem>>>("/api/v1/load", { is_draft: 1, per_page: DRAFTS_PAGE_SIZE, page: nextPage })
      .then((res) => {
        setDraftItems((prev) => [...prev, ...res.data.data]);
        setDraftsPage(nextPage);
        setDraftsHasMore(res.data.current_page < res.data.last_page);
      })
      .catch(() => setDraftsHasMore(false))
      .finally(() => setDraftsLoadingMore(false));
  }

  function handleDraftsScroll(e: UIEvent<HTMLDivElement>) {
    const el = e.currentTarget;
    if (el.scrollHeight - el.scrollTop - el.clientHeight < 48) loadMoreDrafts();
  }

  function openDraft(id: number) {
    setDraftsOpen(false);
    openEdit(id);
  }

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
      payer_company: "", description: "", rejection_reason: "", way_of_working: "0",
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
    // Operasyon Yetkilisi gibi hemen seçilebilir olsun diye en az bir boş satır
    // baştan hazır — bkz. content'in EMPTY_CONTENT_ROW ile aynı deseni.
    setSalesReps([{ id: 0, name: null, surname: null }]);
    setEmailTo([]);
    setEmailCc([]);
    setErrors({});
    setDetailMeta({ siberId: null, reservationNumber: null, loadNumber: null, approvalDate: null, siberAudit: null });
  }

  /**
   * Müşteri seçilince Görevliler sekmesini Siber'deki cari-görevli bağından
   * doldurur (kullanıcı isteği: "müşteriyi seçince otomatik dolsun").
   *
   * Kural: yalnızca BOŞ alanlar doldurulur — kullanıcı elle bir seçim yaptıysa
   * üzerine yazılmaz. Bağ tanımlı değilse hiçbir şey değişmez (sessiz geçilir,
   * kullanıcıyı hata mesajıyla rahatsız etmez).
   */
  /**
   * Müşteri seçilince görevlileri YENİDEN TÜRETİR.
   *
   * Eski davranış "boşsa doldur"du (cur ?? officer): müşteri değiştirilince eski
   * müşterinin temsilcisi kayıtta kalıyordu. Alanlar artık elle değiştirilemediği
   * için doğru davranış her seferinde baştan hesaplamaktır.
   */
  /**
   * Siber arşiv evrağını açar. Dosya API üzerinden vekil geliyor; jetonlu istek
   * gerektiği için blob'a alınıp öyle açılıyor (düz bağlantı 401 dönerdi).
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

  async function applyCustomerRepresentatives(account: AccountOption | null) {
    const selfAsOfficer: UserOption | null =
      currentUser && currentUser.siber_code
        ? { id: currentUser.id, name: currentUser.name, surname: currentUser.surname }
        : null;

    if (!account) {
      setOperationOfficer(selfAsOfficer);
      setSalesReps([]);
      return;
    }

    try {
      const res = await api.get<DataMessage<{
        operation_officer: UserOption | null;
        sales_reps: UserOption[];
      }>>(`/api/v1/account/${account.id}/representatives`);

      const { operation_officer: officer, sales_reps: reps } = res.data;

      setOperationOfficer(selfAsOfficer ?? officer ?? null);
      setSalesReps(reps ?? []);
    } catch {
      // Bağ okunamazsa en azından giriş yapan kullanıcı gösterilir; sunucu
      // kaydederken kuralı yine kendisi uygular.
      setOperationOfficer(selfAsOfficer);
      setSalesReps([]);
    }
  }

  function openNew() {
    resetForm();
    setEditingId(null);
    setTab(TABS[0]);
    setDrawerOpen(true);
  }

  /**
   * Yeni teklif formu her değiştiğinde tarayıcıya yazılır — kullanıcı kaydetmeden
   * çıkarsa (sekmeyi kapatsa bile) Taslaklar menüsünden kaldığı yerden devam eder.
   * Yalnızca YENİ teklifte çalışır; mevcut bir teklif düzenlenirken taslak tutmayız
   * (o zaten sunucuda kayıtlı).
   */
  useEffect(() => {
    if (!drawerOpen || editingId !== null || restoringDraftRef.current) return;

    const snapshot = {
      form, customer, sender, receiver, agent, companyPayFreight, route,
      operationOfficer, salesReps, content, financialItems, emailTo, emailCc, tab,
    };
    if (!draftHasContent(snapshot)) return;

    const timer = setTimeout(() => {
      const draft: LocalDraft = { ...snapshot, savedAt: new Date().toISOString() };
      writeLocalDraft(draft);
      setLocalDraft(draft);
    }, 600);
    return () => clearTimeout(timer);
  }, [
    drawerOpen, editingId, form, customer, sender, receiver, agent, companyPayFreight,
    route, operationOfficer, salesReps, content, financialItems, emailTo, emailCc, tab,
  ]);

  /** Otomatik taslağı forma geri yükler ve çekmeceyi açar. */
  function resumeLocalDraft() {
    const d = readLocalDraft();
    if (!d) return;

    restoringDraftRef.current = true;
    setEditingId(null);
    setErrors({});
    setDetailMeta({ siberId: null, reservationNumber: null, loadNumber: null, approvalDate: null, siberAudit: null });
    setForm(d.form as typeof form);
    setCustomer(d.customer);
    setSender(d.sender);
    setReceiver(d.receiver);
    setAgent(d.agent);
    setCompanyPayFreight(d.companyPayFreight);
    setRoute(d.route);
    setOperationOfficer(d.operationOfficer);
    setSalesReps(d.salesReps?.length ? d.salesReps : [{ id: 0, name: null, surname: null }]);
    setContent(d.content?.length ? d.content : [{ ...EMPTY_CONTENT_ROW }]);
    setFinancialItems(d.financialItems ?? []);
    setEmailTo(d.emailTo ?? []);
    setEmailCc(d.emailCc ?? []);
    setExistingFiles([]);
    setRemovedFileIds([]);
    setNewFiles([]);
    setTab(TABS.includes(d.tab) ? d.tab : TABS[0]);
    setDrawerOpen(true);
    setDraftsOpen(false);
    // Geri yükleme state güncellemeleri işlendikten sonra otomatik kaydediciyi aç.
    setTimeout(() => { restoringDraftRef.current = false; }, 0);
  }

  function discardLocalDraft() {
    clearLocalDraft();
    setLocalDraft(null);
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
      setDetailMeta({ siberId: d.siber_id, reservationNumber: d.reservation_number, loadNumber: d.load_number, approvalDate: d.approval_date, siberAudit: d.siber_audit ?? null });
      setSiberArchive(d.siber_archive ?? []);
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
        rejection_reason: d.rejection_reason ?? "",
        way_of_working: d.way_of_working != null ? String(d.way_of_working) : "0",
        front_transportation_by_us: d.front_transportation_by_us != null ? String(d.front_transportation_by_us) : "0",
        final_transportation_by_us: d.final_transportation_by_us != null ? String(d.final_transportation_by_us) : "0",
      });
      setOperationOfficer(d.load_charge_person.find((p) => p.user_type === 1)?.user_id ?? null);
      const existingSalesReps = d.load_charge_person
        .filter((p) => p.user_type === 2 && p.user_id)
        .map((p) => p.user_id!);
      setSalesReps(existingSalesReps.length > 0 ? existingSalesReps : [{ id: 0, name: null, surname: null }]);
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
              product_type_id: c.product_type_id,
              case_type_id: c.case_type_id,
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
          item: f.item,
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

  /**
   * Bağımsız bir kaydet aksiyonu — ana "Kaydet" butonuna (ve onun zorunlu alan
   * doğrulamasına) bağlı değil, sadece dosyaları yazar (olsold:
   * OfferFormDrawer.vue Dosya Arşivi sekmesindeki ayrı "Dosyaları Kaydet").
   */
  async function saveFiles() {
    if (!editingId) return;
    setSavingFiles(true);
    try {
      const fd = new FormData();
      fd.append("load_id", String(editingId));
      existingFiles
        .filter((f) => !removedFileIds.includes(f.id))
        .forEach((f, i) => fd.append(`files[${i}][id]`, String(f.id)));
      newFiles.forEach((f, i) => fd.append(`files[${existingFiles.length + i}][file]`, f));
      await api.postForm("/api/v1/load/file/upload", fd);
      addToast("Dosyalar kaydedildi");
      setNewFiles([]);
      openEdit(editingId);
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Dosyalar kaydedilemedi", "error");
    } finally {
      setSavingFiles(false);
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
    // Buton `disabled={saving}` ile korunuyor ama bu, React'ın bir sonraki render'ına
    // kadar DOM'a yansımıyor — bu aralıkta gelen ikinci bir tetikleme (hızlı çift
    // tıklama, programatik çağrı) handleSubmit'i tekrar başlatıp aynı isteği iki kez
    // gönderebiliyor, hatta bazen düğmenin sonsuza dek "Kaydediliyor..." da takılı
    // kalmasına yol açıyordu. Erken çıkış bunu tamamen engelliyor.
    if (saving) return;
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
    fd.append("rejection_reason", form.rejection_reason);
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
      fd.append(`load_content[${i}][product_type_id]`, item.product_type_id ? String(item.product_type_id.id) : "");
      fd.append(`load_content[${i}][case_type_id]`, item.case_type_id ? String(item.case_type_id.id) : "");
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
      if (item.item) fd.append(`load_financial_item[${i}][item]`, String(item.item.id));
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

    // GÖREVLİ GÖNDERİLMİYOR: sunucu Operasyon Yetkilisi'ni giriş yapan
    // kullanıcıdan, Satış Temsilcisi'ni müşteriden türetiyor ve istekten geleni
    // bilinçli olarak yok sayıyor (bkz. LoadWriteService.WriteChargePersonsAsync).

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
        // Teklif artık sunucuda: kaydedilmemiş otomatik taslak gereksiz.
        discardLocalDraft();
      }
      setDrawerOpen(false);
      load();
    } catch (err) {
      if (err instanceof ApiError && err.errors) {
        setErrors(err.errors);
        // "load_content"/"load_financial_item" gibi dizi-seviyesi hatalar (tek bir
        // satır bile eklenmemişse) herhangi bir FormField'a bağlı değil — sekmeler
        // arasında kaybolup "Kaydet'e basınca hiçbir şey olmuyor" izlenimi
        // veriyordu. Artık her durumda görünür bir toast da gösteriliyor.
        const message = err.errors.load_content
          ? "Yük içerikleri boş olamaz — “Yük İçeriği” sekmesinden en az bir satır ekleyin."
          : err.errors.load_financial_item
            ? "Mali kalemler boş olamaz — “Finans” sekmesinden en az bir satır ekleyin."
            : (err.message || "Formda eksik/hatalı alanlar var, lütfen sekmeleri kontrol edin.");
        addToast(message, "error");
      } else {
        addToast(err instanceof Error ? err.message : "Kaydedilemedi", "error");
      }
    } finally {
      setSaving(false);
    }
  }

  /**
   * Teklifi kopyalar ve kopyayı hemen açar.
   *
   * Sunucu tarafı kopyayı YENİ taslak olarak üretiyor: Siber kimlikleri,
   * rezervasyon/yük numarası, durum ve onay bilgisi devredilmiyor
   * (bkz. LoadService.DuplicateAsync). Kullanıcı kopyayı görüp üzerinde
   * çalışabilsin diye kart doğrudan açılıyor.
   */
  async function handleDuplicate(id: number) {
    try {
      const res = await api.post<{ data: { id: number }; message: string }>(
        `/api/v1/load/${id}/duplicate`, {});
      addToast("Teklif kopyalandı");
      load();
      if (res?.data?.id) await openEdit(res.data.id);
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Teklif kopyalanamadı", "error");
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
    if (busyLabel) return;
    setBusyLabel("Teklif Siber'e aktarılıyor...");
    try {
      await api.post("/api/v1/transfer_to_siber", { id });
      addToast("Teklif Siber'e aktarıldı");
      load();
      // Çekmece aynı teklifi gösteriyorsa (Sibere Aktar çekmece içinden tetiklendiyse)
      // siber_id/rezervasyon no güncel değerle tazelensin.
      if (editingId === id) openEdit(id);
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Siber'e aktarılamadı", "error");
    } finally {
      setBusyLabel(null);
    }
  }

  /**
   * Onaylanmış (Siber'e aktarılmış) bir teklifi yüke dönüştürür.
   * LoadTransferController.ConvertOffer, teklifin SİBER kimliğini (siber_id) —
   * ID DEĞİL — bekliyor; bu yüzden buton yalnızca siber_id doluyken etkin.
   */
  async function handleConvertToLoad(siberId: string) {
    if (busyLabel) return;
    setBusyLabel("Yük oluşturuluyor...");
    try {
      const res = await api.post<{ data: { yuk_no: string }; message: string }>("/api/v1/load_transfer", { id: siberId });
      addToast(`Yük oluşturuldu: ${res.data.yuk_no}`);
      load();
      if (editingId !== null && detailMeta.siberId === siberId) openEdit(editingId);
    } catch (err) {
      if (err instanceof ApiError) addToast(err.message, "error");
      else addToast(err instanceof Error ? err.message : "Yüke dönüştürülemedi", "error");
    } finally {
      setBusyLabel(null);
    }
  }

  return (
    <>
      <FullScreenBusy show={busyLabel !== null} label={busyLabel ?? undefined} />
      <ModulePage
        title="Teklifler"
        action={
          <div className="flex items-center gap-2">
            <div className="relative" ref={draftsRef}>
              <Btn variant="secondary" onClick={() => setDraftsOpen((o) => !o)}>
                <FileText size={14} />
                Taslaklar
                {(draftsTotal ?? 0) + (localDraft ? 1 : 0) > 0 && (
                  <span className="ml-1 px-1.5 py-0.5 rounded-full bg-amber-100 text-amber-700 text-[10px] font-semibold">
                    {(draftsTotal ?? 0) + (localDraft ? 1 : 0)}
                  </span>
                )}
              </Btn>
              {draftsOpen && (
                <div className="absolute z-30 mt-1 right-0 w-96 bg-white border border-gray-200 rounded-md shadow-2xl">
                  <div className="px-4 py-2.5 border-b border-gray-100 flex items-center justify-between">
                    <p className="text-xs font-semibold text-gray-700">Taslaklar</p>
                    <p className="text-[11px] text-gray-400">
                      {draftsTotal !== null ? `${draftsTotal + (localDraft ? 1 : 0)} kayıt` : ""}
                    </p>
                  </div>
                  <div onScroll={handleDraftsScroll} className="max-h-96 overflow-y-auto">
                    {/* Kaydedilmemiş otomatik taslak — sunucudakilerin üstünde,
                        ayırt edilsin diye amber vurgulu. */}
                    {localDraft && (
                      <div className="flex items-stretch border-b border-gray-100 bg-amber-50/50">
                        <button
                          type="button"
                          onClick={resumeLocalDraft}
                          className="flex-1 text-left px-4 py-2.5 hover:bg-amber-50 transition-colors"
                        >
                          <div className="flex items-center justify-between gap-2">
                            <p className="text-sm font-medium text-gray-800 truncate">
                              {localDraft.customer?.name ?? "Müşteri seçilmedi"}
                            </p>
                            <span className="shrink-0 text-[10px] font-semibold text-amber-700">
                              Kaydedilmedi
                            </span>
                          </div>
                          <p className="text-[11px] text-gray-500 mt-0.5">
                            Kaldığı yerden devam et · {formatDraftTime(localDraft.savedAt)}
                          </p>
                        </button>
                        <button
                          type="button"
                          onClick={discardLocalDraft}
                          title="Taslağı sil"
                          className="px-3 text-gray-300 hover:text-red-500 transition-colors"
                        >
                          <Trash2 size={13} />
                        </button>
                      </div>
                    )}
                    {draftsLoading ? (
                      <p className="text-xs text-gray-400 text-center py-6">Yükleniyor...</p>
                    ) : draftItems.length === 0 ? (
                      !localDraft && <p className="text-xs text-gray-400 text-center py-6">Taslak bulunamadı.</p>
                    ) : (
                      <>
                        {draftItems.map((d) => (
                          <button
                            key={d.id}
                            type="button"
                            onClick={() => openDraft(d.id)}
                            className="w-full text-left px-4 py-2.5 border-b border-gray-50 last:border-b-0 hover:bg-gray-50 transition-colors"
                          >
                            <div className="flex items-center justify-between gap-2">
                              <p className="text-sm font-medium text-gray-800 truncate">
                                {d.customer_id?.name ?? "Müşteri seçilmedi"}
                              </p>
                              {d.load_content_count === 0 && (
                                <span className="shrink-0 text-[10px] text-red-500">İçerik yok</span>
                              )}
                            </div>
                            <p className="text-[11px] text-gray-400 mt-0.5">
                              {d.work_type_id?.name ?? "İş tipi yok"}
                              {d.offer_date && ` · ${d.offer_date}`}
                            </p>
                          </button>
                        ))}
                        {draftsLoadingMore && (
                          <p className="text-xs text-gray-400 text-center py-2">Daha fazla yükleniyor...</p>
                        )}
                      </>
                    )}
                  </div>
                </div>
              )}
            </div>
            {canCreate && <Btn onClick={openNew}><Plus size={14} />Yeni Teklif</Btn>}
          </div>
        }
      >
        <div className="bg-white border-b border-gray-200 px-6 py-4">
          <div className="flex items-center gap-2.5">
            <div className="flex-1 max-w-md">
              <TextInput value={search} onChange={(v) => { setSearch(v); setPage(1); }} placeholder="Genel arama: teklif no, müşteri..." />
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
                <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-3 pt-4 mt-4 border-t border-gray-100">
                  <AccountPicker label="Müşteri" value={fCustomer} onChange={(v) => { setFCustomer(v); setPage(1); }} />
                  <AccountPicker label="Gönderici" value={fSender} onChange={(v) => { setFSender(v); setPage(1); }} />
                  <AccountPicker label="Alıcı" value={fReceiver} onChange={(v) => { setFReceiver(v); setPage(1); }} />
                  <AccountPicker label="Acente" value={fAgent} onChange={(v) => { setFAgent(v); setPage(1); }} />
                  <UserPicker label="Görevli" value={fAssignedUser} onChange={(v) => { setFAssignedUser(v); setPage(1); }} />
                  <FormField label="İş Tipi">
                    <SelectInput value={fWorkType} onChange={(v) => { setFWorkType(v); setPage(1); }} options={opts(workTypes)} />
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
          tabs={STATUS_TABS.map((t) => t.label)}
          active={listTab}
          onChange={(t) => { setListTab(t); setPage(1); }}
          className="px-6 bg-white"
        />
        <div className="bg-gray-50/70 min-h-full">
          {!loading && rows.length === 0 ? (
            <EmptyState icon={FileText} title="Teklif bulunamadı" desc="Arama kriterlerine uygun teklif bulunamadı." />
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
                      <QuoteCard
                        key={r.id}
                        row={r}
                        index={i}
                        onClick={() => openEdit(r.id)}
                        canCreate={canCreate}
                        canDelete={canDelete}
                        onTransferToSiber={() => handleTransferToSiber(r.id)}
                        onConvertToLoad={() => r.siber_id && handleConvertToLoad(r.siber_id)}
                        onDelete={() => handleDelete(r.id, r.reservation_number)}
                        onDuplicate={() => handleDuplicate(r.id)}
                        onOpenLoad={() => navigate(`/yukler?yuk=${r.id}`)}
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
        title={editingId ? `Teklif — ${customer?.name ?? ""}` : "Yeni Teklif"}
        subtitle={editingId ? undefined : "Yeni teklif oluştur"}
        width="w-[min(1180px,95vw)]"
        footer={
          (editingId ? canUpdate : canCreate) ? (
            <div className="flex items-center justify-between gap-2 w-full">
              <div className="flex items-center gap-2 min-w-0">
                {/* Akışın SIRADAKİ tek adımı gösterilir — kart üzerindeki
                    QuoteStageBar ile aynı mantık, aynı sıra. */}
                {editingId && detailMeta.loadNumber && (
                  <span className="flex items-center gap-1.5 text-xs font-semibold text-emerald-700">
                    <Truck size={14} />Yük oluşturuldu
                  </span>
                )}
                {editingId && !detailMeta.loadNumber && isNegativeStatus && (
                  <span className="flex items-center gap-1.5 text-xs font-semibold text-red-600">
                    <X size={14} />Olumsuz — akış durdu
                  </span>
                )}
                {editingId && !detailMeta.loadNumber && !isNegativeStatus && !isPositiveStatus && (
                  <span className="text-xs text-gray-500">
                    Sıradaki adım: <span className="font-medium text-gray-700">Olumlu / Olumsuz belirle</span>
                  </span>
                )}
                {editingId && !detailMeta.loadNumber && isPositiveStatus && !detailMeta.siberId && canUpdate && (
                  <Btn onClick={() => handleTransferToSiber(editingId)} disabled={saving || detailLoading || busyLabel !== null}>
                    <BusyLabel busy={busyLabel !== null} busyText="Aktarılıyor...">
                      <Package size={14} />
                      1. Adım — Siber'e Aktar
                    </BusyLabel>
                  </Btn>
                )}
                {editingId && !detailMeta.loadNumber && isPositiveStatus && detailMeta.siberId && canCreate && (
                  <Btn onClick={() => handleConvertToLoad(detailMeta.siberId!)} disabled={saving || detailLoading || busyLabel !== null}>
                    <BusyLabel busy={busyLabel !== null} busyText="Oluşturuluyor...">
                      <Truck size={14} />
                      2. Adım — Yük Oluştur
                    </BusyLabel>
                  </Btn>
                )}
                {editingId && (detailMeta.reservationNumber || detailMeta.loadNumber) && (
                  <span className="text-[11px] text-gray-400 font-mono truncate">
                    {detailMeta.reservationNumber && `Rez. No: ${detailMeta.reservationNumber}`}
                    {detailMeta.loadNumber && ` · Yük No: ${detailMeta.loadNumber}`}
                  </span>
                )}
              </div>
              <div className="flex gap-2 shrink-0">
                <Btn onClick={handleSubmit} disabled={saving || detailLoading || busyLabel !== null}>
                  <BusyLabel busy={saving} busyText="Kaydediliyor...">Kaydet</BusyLabel>
                </Btn>
                <Btn variant="secondary" onClick={() => setDrawerOpen(false)}>İptal</Btn>
              </div>
            </div>
          ) : undefined
        }
      >
        {editingId !== null && detailMeta.siberAudit && (
          <div className="px-6 pt-4">
            <SiberAuditPanel audit={detailMeta.siberAudit} />
          </div>
        )}

        <Tabs tabs={TABS} active={tab} onChange={setTab} className="px-6" />

        {detailLoading ? (
          <p className="text-sm text-gray-400 text-center py-10">Yükleniyor...</p>
        ) : (
          <div className="p-8">
            {tab === "Genel Bilgiler" && (
              <div className="space-y-8">
                <div className="grid grid-cols-3 gap-x-6 gap-y-5">
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
                  <FormField label="Yüklenme Durumu" required error={errors.loading_type_id?.[0]}>
                    <SelectInput value={form.loading_type_id} onChange={(v) => setForm((f) => ({ ...f, loading_type_id: v }))} options={opts(loadingTypes)} />
                  </FormField>
                  <FormField label="Departman" required error={errors.department_id?.[0]}>
                    <SelectInput value={form.department_id} onChange={(v) => setForm((f) => ({ ...f, department_id: v }))} options={opts(departments)} />
                  </FormField>
                  <FormField label="Yük Türü" required={isPositiveStatus} error={errors.load_transfer_type_id?.[0]}>
                    <SelectInput value={form.load_transfer_type_id} onChange={(v) => setForm((f) => ({ ...f, load_transfer_type_id: v }))} options={opts(loadTransferTypes)} />
                  </FormField>
                  <FormField label="Talimatın Geliş Şekli" required={isPositiveStatus} error={errors.instruction_id?.[0]}>
                    <SelectInput value={form.instruction_id} onChange={(v) => setForm((f) => ({ ...f, instruction_id: v }))} options={opts(instructions)} />
                  </FormField>
                  <FormField label="İstenilen Römork Cinsi" required={isPositiveStatus} error={errors.romork_type_id?.[0]}>
                    <SelectInput value={form.romork_type_id} onChange={(v) => setForm((f) => ({ ...f, romork_type_id: v }))} options={opts(romorkTypes)} />
                  </FormField>
                  <FormField label="Durum" required error={errors.status_type_id?.[0]}>
                    <SelectInput value={form.status_type_id} onChange={(v) => setForm((f) => ({ ...f, status_type_id: v }))} options={opts(statusTypes)} />
                  </FormField>
                  {/* olsnew: v-if="offer_data.status_type_id?.id == 5" — yalnızca Durum="Olumlu"
                      iken görünür. Taslak mantığı eklenirken backend'in bunu zorunlu tutması
                      kaldırıldı (bkz. LoadController.Validate XML açıklaması) — alan yine de
                      yalnızca "Olumlu"da anlamlı olduğu için görünürlüğü korunuyor, ama artık
                      zorunlu işaretlenmiyor. */}
                  {form.status_type_id && form.status_type_id === String(statusTypes.find((s) => s.name === "Olumlu")?.id ?? "") && (
                    <FormField label="Çalışma Şekli" required error={errors.way_of_working?.[0]}>
                      <SelectInput value={form.way_of_working} onChange={(v) => setForm((f) => ({ ...f, way_of_working: v }))} options={WAY_OF_WORKING_OPTIONS} />
                    </FormField>
                  )}
                </div>

                {/* Olumlu teklif Yük'e dönüşeceği için dönüşümün ihtiyaç duyduğu alanlar
                    zorunlu hâle gelir (liste Siber'in rezervasyon ekranındaki kırmızı
                    alanlardan alındı). Kullanıcı "Olumlu" seçtiği anda hangi alanların
                    zorunlulaştığını görsün diye bu şerit gösterilir. */}
                {isPositiveStatus && (
                  <div className="rounded-lg border border-blue-200 bg-blue-50/50 px-4 py-3">
                    <p className="text-xs font-semibold text-blue-800">Olumlu teklif — zorunlu alanlar</p>
                    <p className="text-[11px] text-blue-700 mt-1 leading-relaxed">
                      Müşteri, Gönderici, Alıcı · Kalkış/Varış Ülkesi · Talimatın Geliş Şekli ·
                      İstenilen Römork Cinsi · Yük Türü · Çalışma Şekli. Bu alanlar
                      <span className="font-semibold"> Yük'e dönüştürme</span> için gereklidir; formda
                      <span className="text-red-500 font-semibold"> *</span> ile işaretlidir.
                    </p>
                  </div>
                )}

                {/* Olumsuz teklifin gerekçesi: yalnızca Durum="Olumsuz" iken görünür ve
                    zorunludur (backend de zorunlu tutar — LoadController.Validate).
                    Raporlamada tekliflerin NEDEN kaybedildiğini görebilmek için. */}
                {isNegativeStatus && (
                  <div className="rounded-lg border border-red-200 bg-red-50/40 p-4">
                    <FormField label="Olumsuzluk Nedeni" required error={errors.rejection_reason?.[0]}>
                      <TextareaInput
                        value={form.rejection_reason}
                        onChange={(v) => setForm((f) => ({ ...f, rejection_reason: v }))}
                        rows={3}
                      />
                    </FormField>
                    <p className="text-[11px] text-gray-500 mt-1.5">
                      Teklifin neden olumsuz sonuçlandığını yazın (fiyat, termin, kapasite vb.).
                      Bu bilgi raporlamada kullanılır.
                    </p>
                  </div>
                )}

                <div>
                  <SectionHeader icon={CalendarDays} title="Tarih" />
                  <div className="grid grid-cols-3 gap-4">
                    <FormField label="Teklif Tarihi" required error={errors.offer_date?.[0]}>
                      <TextInput value={form.offer_date} onChange={(v) => setForm((f) => ({ ...f, offer_date: v }))} type="date" error={!!errors.offer_date} />
                    </FormField>
                    <FormField label="Pazarlama Bildirim Tarihi" required error={errors.marketing_notification_date?.[0]}>
                      <TextInput value={form.marketing_notification_date} onChange={(v) => setForm((f) => ({ ...f, marketing_notification_date: v }))} type="date" error={!!errors.marketing_notification_date} />
                    </FormField>
                    <FormField label="Geçerlilik Tarihi" required error={errors.offer_validity_date?.[0]}>
                      <TextInput value={form.offer_validity_date} onChange={(v) => setForm((f) => ({ ...f, offer_validity_date: v }))} type="date" error={!!errors.offer_validity_date} />
                    </FormField>
                  </div>

                  {/*
                    Olumlu Tarihi ELLE GİRİLMEZ: durum Olumlu'ya çekildiğinde sunucu
                    o günün tarihini damgalar (LoadWriteService.ResolveApprovalDate) ve
                    Siber'de skn_rezervasyon.onaytarih sütununa yazar. Teklif Tarihi'ne
                    dokunulmaz — ikisinin farkı "teklif kaç günde onaylandı" bilgisidir.
                  */}
                  {isPositiveStatus && (
                    <div className="mt-3 flex items-center gap-2 rounded-lg border border-emerald-100 bg-emerald-50/70 px-3 py-2">
                      <CalendarDays size={14} className="text-emerald-600 shrink-0" />
                      <span className="text-xs text-emerald-900">
                        <b>Olumlu Tarihi:</b>{" "}
                        {detailMeta.approvalDate
                          ? new Date(detailMeta.approvalDate).toLocaleDateString("tr-TR")
                          : new Date().toLocaleDateString("tr-TR")}
                      </span>
                      <span className="ml-auto text-[11px] text-emerald-700/70">
                        {detailMeta.approvalDate ? "kayıtlı" : "kaydedince damgalanacak"}
                      </span>
                    </div>
                  )}
                </div>

                <div>
                  <SectionHeader icon={Globe} title="Konum" />
                  <div className="grid grid-cols-3 gap-4">
                    <FormField label="Kalkış Ülkesi" required={isPositiveStatus} error={errors.departure_country_id?.[0]}>
                      <SelectInput value={route.departure_country_id} onChange={(v) => setRoute((r) => ({ ...r, departure_country_id: v }))} options={opts(countries)} />
                    </FormField>
                    <FormField label="Varış Ülkesi" required={isPositiveStatus} error={errors.target_country_id?.[0]}>
                      <SelectInput value={route.target_country_id} onChange={(v) => setRoute((r) => ({ ...r, target_country_id: v }))} options={opts(countries)} />
                    </FormField>
                    <FormField label="Transfer Ülkesi">
                      <SelectInput value={route.transit_country_id} onChange={(v) => setRoute((r) => ({ ...r, transit_country_id: v }))} options={opts(countries)} />
                    </FormField>
                  </div>
                </div>

                <div>
                  <SectionHeader icon={CreditCard} title="Ödeme" />
                  <div className="grid grid-cols-3 gap-4">
                    <FormField label="Ödeme Şekli" required error={errors.payment_type_id?.[0]}>
                      <SelectInput value={form.payment_type_id} onChange={(v) => setForm((f) => ({ ...f, payment_type_id: v }))} options={opts(paymentTypes)} />
                    </FormField>
                    <div className="col-span-2">
                      <AccountPicker label="Navlun Ödeyecek Firma" value={companyPayFreight} onChange={setCompanyPayFreight} error={errors.company_pay_freight_id?.[0]} accountType={1} />
                    </div>
                    <div className="col-span-3">
                      <FormField label="Navlun Ödeyen Firma (serbest metin)">
                        <TextInput value={form.payer_company} onChange={(v) => setForm((f) => ({ ...f, payer_company: v }))} />
                      </FormField>
                    </div>
                  </div>
                </div>

                <div>
                  <SectionHeader icon={Building2} title="Şirketler" />
                  <div className="grid grid-cols-2 gap-4">
                    <AccountPicker label="Müşteri" value={customer} onChange={(v) => { setCustomer(v); applyCustomerRepresentatives(v); }} required error={errors.customer_id?.[0]} />
                    <AccountPicker label="Gönderici" value={sender} onChange={setSender} required={isPositiveStatus} error={errors.sender_id?.[0]} />
                    <AccountPicker label="Alıcı" value={receiver} onChange={setReceiver} required={isPositiveStatus} error={errors.receiver_id?.[0]} />
                    <AccountPicker label="Acente" value={agent} onChange={setAgent} error={errors.agent_id?.[0]} accountType={5} />
                  </div>
                </div>

                <div>
                  <SectionHeader icon={Truck} title="Taşıma Ayarları" />
                  <div className="grid grid-cols-2 gap-4">
                    <FormField label="Ön Taşıma Tarafımızdan Yapılır">
                      <SelectInput value={form.front_transportation_by_us} onChange={(v) => setForm((f) => ({ ...f, front_transportation_by_us: v }))} options={YES_NO_OPTIONS} />
                    </FormField>
                    <FormField label="Son Taşıma Tarafımızdan Yapılır">
                      <SelectInput value={form.final_transportation_by_us} onChange={(v) => setForm((f) => ({ ...f, final_transportation_by_us: v }))} options={YES_NO_OPTIONS} />
                    </FormField>
                  </div>
                </div>

                <div>
                  <SectionHeader icon={StickyNote} title="Not" />
                  <TextareaInput value={form.description} onChange={(v) => setForm((f) => ({ ...f, description: v }))} rows={5} />
                </div>
              </div>
            )}

            {tab === "Yük İçeriği" && (
              <div>
                <div className="flex justify-end mb-6">
                  <Btn variant="secondary" onClick={addContentRow}><Plus size={14} />Yeni İçerik Ekle</Btn>
                </div>
                {content.map((item, i) => (
                  <div key={i} className="border border-gray-200 rounded-lg p-4 mb-2 relative">
                    {content.length > 1 && (
                      <button type="button" onClick={() => removeContentRow(i)} className="absolute top-2 right-2 text-gray-300 hover:text-red-500">
                        <Trash2 size={13} />
                      </button>
                    )}
                    <div className="grid grid-cols-3 gap-3">
                      <LookupPicker
                        label="Ürün Tipi"
                        endpoint="/api/v1/product_type"
                        required
                        error={errors[`load_content.${i}.product_type_id`]?.[0]}
                        value={item.product_type_id}
                        onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, product_type_id: v } : x)))}
                      />
                      <LookupPicker
                        label="Kap Tipi"
                        endpoint="/api/v1/case_type"
                        required
                        error={errors[`load_content.${i}.case_type_id`]?.[0]}
                        value={item.case_type_id}
                        onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, case_type_id: v } : x)))}
                      />
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
                        <TextInput value={item.width} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, width: v, lademeter: computeLademeter(v, x.length) } : x)))} error={!!errors[`load_content.${i}.width`]} />
                      </FormField>
                      <FormField label="Boy (cm)" required error={errors[`load_content.${i}.length`]?.[0]}>
                        <TextInput value={item.length} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, length: v, lademeter: computeLademeter(x.width, v) } : x)))} error={!!errors[`load_content.${i}.length`]} />
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
            )}

            {tab === "Finans" && (
              <div>
                <div className="flex justify-end mb-6">
                  <Btn variant="secondary" onClick={addFinancialRow}><Plus size={14} />Yeni Kayıt Ekle</Btn>
                </div>
                {financialItems.length === 0 ? (
                  <p className="text-xs text-gray-400 text-center py-8">Henüz mali kalem eklenmedi.</p>
                ) : (
                  <div className="space-y-8">
                    {(["1", "2"] as const).map((buysellValue) => {
                      const group = financialItems
                        .map((item, i) => ({ item, i }))
                        .filter(({ item }) => item.buysell === buysellValue);
                      if (group.length === 0) return null;
                      return (
                        <div key={buysellValue}>
                          <SectionHeader
                            icon={buysellValue === "1" ? Download : Upload}
                            title={buysellValue === "1" ? "Alış Hareketleri" : "Satış Hareketleri"}
                          />
                          {group.map(({ item, i }) => (
                            <div key={i} className="border border-gray-200 rounded-lg p-4 mb-2 relative">
                              <button type="button" onClick={() => removeFinancialRow(i)} className="absolute top-2 right-2 text-gray-300 hover:text-red-500">
                                <Trash2 size={13} />
                              </button>
                              <div className="grid grid-cols-3 gap-3 mb-3">
                                <FinancialItemPicker
                                  label="Kalem"
                                  required
                                  error={errors[`load_financial_item.${i}.item`]?.[0]}
                                  value={item.item}
                                  onChange={(v) => setFinancialItems((list) => list.map((x, xi) => {
                                    if (xi !== i) return x;
                                    // Kalem hem Alış hem Satış'ta kullanılabiliyorsa (type=3) ya da
                                    // Siber'den senkronlanırken type hiç set edilmemişse (null — ör.
                                    // "Gümrükleme") mevcut seçim korunur; yalnızca tek yönlü olduğu
                                    // KESİN olarak biliniyorsa (1 veya 2) otomatik ayarlanır. Önceki
                                    // hâli (`type !== 3`) null'u da "tek yönlü" sayıp buysell'i
                                    // string "null" yapıyordu — satır hiçbir Alış/Satış grubuna
                                    // düşmediği için listeden tamamen kayboluyordu.
                                    const buysell = v && (v.type === 1 || v.type === 2) ? String(v.type) : x.buysell;
                                    // Alış/Satış fiilen değiştiyse Cari listesi de değişir (Tedarikçi<->Müşteri) - eski seçim geçersiz olabilir.
                                    const account = buysell === x.buysell ? x.account : null;
                                    return { ...x, item: v, buysell, account };
                                  }))}
                                />
                                <FormField label="Taşıma Tipi" required error={errors[`load_financial_item.${i}.transport_type_id`]?.[0]}>
                                  <SelectInput value={item.transport_type_id} onChange={(v) => setFinancialItems((list) => list.map((x, xi) => (xi === i ? { ...x, transport_type_id: v } : x)))} options={opts(transportTypes)} />
                                </FormField>
                                <FormField label="Alış/Satış" required error={errors[`load_financial_item.${i}.buysell`]?.[0]}>
                                  <SelectInput
                                    value={item.buysell}
                                    onChange={(v) => setFinancialItems((list) => list.map((x, xi) => (xi === i ? { ...x, buysell: v, item: null, account: null } : x)))}
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
                          ))}
                        </div>
                      );
                    })}
                  </div>
                )}
              </div>
            )}

            {tab === "Görevliler" && (
              <div className="space-y-5 max-w-xl">
                <div className="rounded-lg border border-blue-100 bg-blue-50/60 p-3 text-xs text-blue-900">
                  Görevliler otomatik belirlenir ve elle değiştirilemez: <b>Operasyon
                  Yetkilisi</b> kaydı açan kullanıcı, <b>Satış Temsilcisi</b> müşteriye
                  tanımlı temsilcidir.
                </div>

                <ReadOnlyPerson
                  label="Operasyon Yetkilisi"
                  person={operationOfficer}
                  hint="Kaydı açan kullanıcı"
                  empty="Giriş yapan kullanıcının Siber karşılığı yok ve müşteriye operasyon yetkilisi tanımlı değil."
                />

                <div>
                  <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider mb-2">
                    Satış Temsilcisi
                  </p>
                  {salesReps.length === 0 ? (
                    <p className="text-xs text-gray-400 py-3">
                      {customer
                        ? "Bu müşteriye tanımlı satış temsilcisi yok — kaydederken operasyon yetkilisi yazılır."
                        : "Önce müşteri seçin."}
                    </p>
                  ) : (
                    <div className="space-y-2">
                      {salesReps.map((rep, i) => (
                        <ReadOnlyPerson
                          key={rep.id || i}
                          label={`Satış Temsilcisi ${i + 1}`}
                          person={rep}
                          hint="Müşteriye tanımlı"
                        />
                      ))}
                    </div>
                  )}
                </div>
              </div>
            )}

            {tab === "İşlem Geçmişi" && (
              <RecordHistoryTab resource="load" recordId={editingId} />
            )}

            {tab === "Dosya Arşivi" && (
              <div className="space-y-4">
                {/* Siber'den gelen evraklar — sahibi Siber, salt görüntüleme. */}
                <div>
                  <div className="flex items-center justify-between mb-2">
                    <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider">Siber Arşivi</p>
                    <span className="text-[10px] text-gray-400">Siber'den okunur · salt görüntüleme</span>
                  </div>
                  {siberArchive.length === 0 ? (
                    <p className="text-xs text-gray-400">Bu teklif için Siber arşivinde evrak yok.</p>
                  ) : (
                    <div className="space-y-1.5">
                      {siberArchive.map((a) => (
                        <div key={a.id} className="flex items-center gap-2 p-2 rounded-lg border border-gray-100 bg-gray-50/60 text-sm">
                          <FileIcon size={14} className="text-gray-400 shrink-0" />
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

                {editingId && canUpdate && (
                  <div className="flex justify-end">
                    <Btn onClick={saveFiles} disabled={savingFiles}>{savingFiles ? "Kaydediliyor..." : "Dosyaları Kaydet"}</Btn>
                  </div>
                )}
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
