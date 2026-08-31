import { useEffect, useState } from "react";
import { motion, AnimatePresence } from "motion/react";
import { clsx } from "clsx";
import { Headphones, Filter, ChevronDown, X, CalendarDays, Phone, Mail } from "lucide-react";
import { api, type Paginated } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useDebouncedValue } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { ModulePage } from "@/components/ui/ModulePage";
import { EmptyState, Pagination } from "@/components/ui/DataTable";
import { Drawer } from "@/components/ui/Overlay";
import { Badge, Btn, FormField, SelectInput, TextInput } from "@/components/ui/primitives";

interface ContactForm {
  id: number;
  first_name: string | null;
  last_name: string | null;
  email: string | null;
  phone: string | null;
  message: string | null;
  is_read: boolean;
  is_answered: boolean;
  created_at: string | null;
}

interface Envelope<T> {
  success: boolean;
  data: T;
  message?: string;
}

const PER_PAGE = 24;

function SupportCard({ row, index, onClick }: { row: ContactForm; index: number; onClick: () => void }) {
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
            <Headphones size={16} />
          </div>
          <div className="min-w-0">
            <p className="text-sm font-semibold text-gray-900 truncate">{row.first_name} {row.last_name}</p>
            {date && (
              <p className="text-[10px] text-gray-400 mt-0.5 flex items-center gap-1">
                <CalendarDays size={10} />
                {date}
              </p>
            )}
          </div>
        </div>
        <div className="flex flex-col items-end gap-1 shrink-0">
          <Badge label={row.is_read ? "Okundu" : "Okunmadı"} />
          <Badge label={row.is_answered ? "Yanıtlandı" : "Yanıtlanmadı"} />
        </div>
      </div>

      <div className="pt-2.5 border-t border-gray-100 space-y-1.5">
        <div className="flex items-center gap-1.5 text-[11px] text-gray-500 min-w-0">
          <Phone size={12} className="text-gray-400 shrink-0" />
          <span className="truncate">{row.phone || "—"}</span>
        </div>
        <div className="flex items-center gap-1.5 text-[11px] text-gray-500 min-w-0">
          <Mail size={12} className="text-gray-400 shrink-0" />
          <span className="truncate">{row.email || "—"}</span>
        </div>
      </div>

      {row.message && (
        <p className="text-xs text-gray-600 pt-2.5 border-t border-gray-100 line-clamp-2">{row.message}</p>
      )}
    </motion.div>
  );
}

export function SupportPage() {
  const { can } = useAuth();
  const { addToast } = useToast();
  const canRead = can("support_request_management", "read");
  const canUpdate = can("support_request_management", "update");

  const [page, setPage] = useState(1);
  const [rows, setRows] = useState<ContactForm[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);
  const [fIsRead, setFIsRead] = useState("");
  const [fIsAnswered, setFIsAnswered] = useState("");
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [showAdvanced, setShowAdvanced] = useState(false);
  const hasActiveAdvancedFilters = !!(fIsRead || fIsAnswered || dateFrom || dateTo);
  const hasActiveFilters = !!(search || hasActiveAdvancedFilters);

  function clearFilters() {
    setSearch(""); setFIsRead(""); setFIsAnswered(""); setDateFrom(""); setDateTo(""); setPage(1);
  }

  const [drawerOpen, setDrawerOpen] = useState(false);
  const [selected, setSelected] = useState<ContactForm | null>(null);
  const [updating, setUpdating] = useState(false);

  function load() {
    setLoading(true);
    api
      .get<Envelope<Paginated<ContactForm>>>("/api/website/contact/form", {
        search: debouncedSearch || undefined,
        is_read: fIsRead || undefined,
        is_answered: fIsAnswered || undefined,
        date_from: dateFrom || undefined,
        date_to: dateTo || undefined,
        page,
      })
      .then((res) => {
        setRows(res.data.data);
        setTotal(res.data.total);
      })
      .catch(() => addToast("Destek talepleri yüklenemedi", "error"))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch, page, fIsRead, fIsAnswered, dateFrom, dateTo]);

  async function openDetail(row: ContactForm) {
    try {
      const res = await api.get<Envelope<ContactForm>>(`/api/website/contact/form/${row.id}`);
      setSelected(res.data);
      setDrawerOpen(true);
      // GET görüntülemesi kaynak davranışına uygun olarak is_read'i true yapar — listeyi tazele.
      load();
    } catch {
      addToast("Talep detayı yüklenemedi", "error");
    }
  }

  async function handleAnsweredChange(isAnswered: boolean) {
    if (!selected) return;
    setUpdating(true);
    try {
      const res = await api.patch<Envelope<ContactForm>>(`/api/website/contact/form/${selected.id}/answered`, {
        is_answered: isAnswered,
      });
      setSelected(res.data);
      addToast(isAnswered ? "Talep yanıtlandı olarak işaretlendi" : "Talep yanıtlanmadı olarak işaretlendi");
      load();
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Güncellenemedi", "error");
    } finally {
      setUpdating(false);
    }
  }

  if (!canRead) {
    return <EmptyState icon={Headphones} title="Yetkiniz yok" desc="Bu ekranı görüntülemek için gerekli yetkiye sahip değilsiniz." />;
  }

  return (
    <>
      <ModulePage title="Destek Talepleri">
        <div className="bg-white border-b border-gray-200 px-6 py-4">
          <div className="flex items-center gap-2.5">
            <div className="flex-1 max-w-md">
              <TextInput value={search} onChange={(v) => { setSearch(v); setPage(1); }} placeholder="Genel arama: ad, e-posta, telefon, mesaj..." />
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
                <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 pt-4 mt-4 border-t border-gray-100">
                  <FormField label="Okunma Durumu">
                    <SelectInput value={fIsRead} onChange={(v) => { setFIsRead(v); setPage(1); }} options={[{ value: "", label: "Seçiniz" }, { value: "true", label: "Okundu" }, { value: "false", label: "Okunmadı" }]} />
                  </FormField>
                  <FormField label="Yanıtlanma Durumu">
                    <SelectInput value={fIsAnswered} onChange={(v) => { setFIsAnswered(v); setPage(1); }} options={[{ value: "", label: "Seçiniz" }, { value: "true", label: "Yanıtlandı" }, { value: "false", label: "Yanıtlanmadı" }]} />
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
            <EmptyState icon={Headphones} title="Talep bulunamadı" desc="Henüz destek talebi gönderilmemiş." />
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
                      <SupportCard key={r.id} row={r} index={i} onClick={() => openDetail(r)} />
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
        title="Destek Talebi"
        subtitle={selected ? `${selected.first_name} ${selected.last_name}` : undefined}
      >
        {selected && (
          <div className="p-6 space-y-5">
            <div className="grid grid-cols-2 gap-3">
              <div className="bg-gray-50 rounded-lg p-3">
                <p className="text-[11px] text-gray-400 uppercase font-semibold tracking-wide">Kullanıcı</p>
                <p className="text-sm font-medium text-gray-800 mt-0.5">{selected.first_name} {selected.last_name}</p>
              </div>
              <div className="bg-gray-50 rounded-lg p-3">
                <p className="text-[11px] text-gray-400 uppercase font-semibold tracking-wide">E-posta</p>
                <p className="text-sm font-medium text-gray-800 mt-0.5">{selected.email}</p>
              </div>
              <div className="bg-gray-50 rounded-lg p-3">
                <p className="text-[11px] text-gray-400 uppercase font-semibold tracking-wide">Telefon Numarası</p>
                <p className="text-sm font-medium text-gray-800 mt-0.5">{selected.phone ?? "—"}</p>
              </div>
              <div className="bg-gray-50 rounded-lg p-3">
                <p className="text-[11px] text-gray-400 uppercase font-semibold tracking-wide">Tarih</p>
                <p className="text-sm font-medium text-gray-800 mt-0.5">{selected.created_at ? new Date(selected.created_at).toLocaleString("tr-TR") : "—"}</p>
              </div>
            </div>
            <FormFieldLike label="Mesaj">
              <div className="p-4 bg-gray-50 border border-gray-200 rounded-lg">
                <p className="text-sm text-gray-700 leading-relaxed whitespace-pre-line">{selected.message}</p>
              </div>
            </FormFieldLike>
            <FormFieldLike label="Yanıtlanma Durumu">
              <SelectInput
                value={selected.is_answered ? "1" : "0"}
                onChange={(v) => handleAnsweredChange(v === "1")}
                disabled={!canUpdate || updating}
                options={[
                  { value: "0", label: "Yanıtlanmadı" },
                  { value: "1", label: "Yanıtlandı" },
                ]}
              />
            </FormFieldLike>
            <div className="flex justify-end">
              <Btn variant="secondary" onClick={() => setDrawerOpen(false)}>
                Kapat
              </Btn>
            </div>
          </div>
        )}
      </Drawer>
    </>
  );
}

function FormFieldLike({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-1.5">
      <label className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider">{label}</label>
      {children}
    </div>
  );
}
