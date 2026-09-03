import { useEffect, useState } from "react";
import { motion, AnimatePresence } from "motion/react";
import { clsx } from "clsx";
import { Car, Plus, Filter, ChevronDown, X, Gauge, Trash2 } from "lucide-react";
import { api, ApiError, type DataMessage, type Paginated } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useDebouncedValue, useLookupOptions } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { ModulePage } from "@/components/ui/ModulePage";
import { EmptyState, Pagination } from "@/components/ui/DataTable";
import { Drawer } from "@/components/ui/Overlay";
import { Badge, Btn, FormField, TextInput, SelectInput } from "@/components/ui/primitives";
import { AccountPicker, type AccountOption } from "@/components/shared/AccountPicker";
import { DepartmentManagerModal } from "@/components/shared/DepartmentManagerModal";
import { CompanyPicker } from "@/components/shared/CompanyPicker";

interface NamedRef {
  id: number;
  name: string | null;
}

interface CarItem {
  id: number;
  plate_number: string | null;
  km: number | null;
  width: number | null;
  length: number | null;
  height: number | null;
  capacity: number | null;
  car_type: NamedRef | null;
  romork_type: NamedRef | null;
  vehicle_owner: NamedRef | null;
  vehicle_status: NamedRef | null;
  customer: AccountOption | null;
}

const PER_PAGE = 24;

function CarCard({
  row, index, onClick, canDelete, onDelete,
}: {
  row: CarItem; index: number; onClick: () => void; canDelete: boolean; onDelete: () => void;
}) {
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
            <Car size={16} />
          </div>
          <div className="min-w-0">
            <p className="font-mono text-sm font-bold text-gray-900 truncate">{row.plate_number}</p>
            {row.customer?.name && <p className="text-[10px] text-gray-400 mt-0.5 truncate">{row.customer.name}</p>}
          </div>
        </div>
        <div className="flex items-center gap-1 shrink-0">
          {row.vehicle_status?.name && <Badge label={row.vehicle_status.name} />}
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

      <div className="grid grid-cols-2 gap-3 pt-3 border-t border-gray-100">
        <div className="min-w-0">
          <p className="text-[10px] font-semibold text-gray-400 uppercase tracking-wider mb-0.5">Tipi</p>
          <p className="text-xs text-gray-700 truncate">{row.car_type?.name ?? "—"}</p>
        </div>
        <div className="min-w-0">
          <p className="text-[10px] font-semibold text-gray-400 uppercase tracking-wider mb-0.5">Sahiplik Durumu</p>
          <p className="text-xs text-gray-700 truncate">{row.vehicle_owner?.name ?? "—"}</p>
        </div>
      </div>

      <div className="flex items-center gap-1.5 text-[11px] text-gray-500 pt-2.5 border-t border-gray-100">
        <Gauge size={12} className="text-gray-400 shrink-0" />
        <span>{row.km != null ? `${row.km.toLocaleString("tr-TR")} km` : "Kilometre girilmedi"}</span>
      </div>
    </motion.div>
  );
}

export function VehiclesPage() {
  const { can } = useAuth();
  const { addToast } = useToast();
  // OLUŞTURMA HERKESE AÇIK: müşteri / araç / teklif / yük / sefer kaydı
  // AÇMAK yetkiye bağlı değil. Okuma, güncelleme ve silme yetkileri
  // olduğu gibi duruyor; uç tarafında da create izni aranmıyor.
  const canCreate = true;
  const canUpdate = can("car_management", "update");
  const canDelete = can("car_management", "delete");

  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);
  const [page, setPage] = useState(1);
  const [rows, setRows] = useState<CarItem[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);

  const [fCarType, setFCarType] = useState("");
  const [fRomorkType, setFRomorkType] = useState("");
  const [fVehicleOwner, setFVehicleOwner] = useState("");
  const [fVehicleStatus, setFVehicleStatus] = useState("");
  const [fCustomer, setFCustomer] = useState<AccountOption | null>(null);
  const [showAdvanced, setShowAdvanced] = useState(false);
  const hasActiveAdvancedFilters = !!(fCarType || fRomorkType || fVehicleOwner || fVehicleStatus || fCustomer);
  const hasActiveFilters = !!(search || hasActiveAdvancedFilters);

  function clearFilters() {
    setSearch("");
    setFCarType("");
    setFRomorkType("");
    setFVehicleOwner("");
    setFVehicleStatus("");
    setFCustomer(null);
    setPage(1);
  }

  const [drawerOpen, setDrawerOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  const [form, setForm] = useState({
    siber_company_id: "",
    plate_number: "",
    car_type: "",
    romork_type: "",
    vehicle_owner: "",
    vehicle_status: "",
    km: "",
    width: "",
    length: "",
    height: "",
    capacity: "",
  });
  const [customer, setCustomer] = useState<AccountOption | null>(null);
  // olsold: car/form.vue — "İstenilen Römork Cinsi" alanının "Yeni Ekle" düğmesi
  // kopyala-yapıştır sonucu Departmanlar penceresini açıyor (Romork Tipi ile
  // ilgisiz). Kullanıcı isteğiyle bu hata birebir korunuyor.
  const [departmentModalOpen, setDepartmentModalOpen] = useState(false);

  const { options: carTypes } = useLookupOptions("/api/v1/car_type");
  const { options: romorkTypes } = useLookupOptions("/api/v1/romork_type");
  const { options: carOwners } = useLookupOptions("/api/v1/car_owner");
  const { options: carStatuses } = useLookupOptions("/api/v1/car_status");

  function load() {
    setLoading(true);
    api
      .get<DataMessage<Paginated<CarItem>>>("/api/v1/car", {
        search: debouncedSearch || undefined,
        car_type_id: fCarType || undefined,
        romork_type_id: fRomorkType || undefined,
        vehicle_owner_id: fVehicleOwner || undefined,
        vehicle_status_id: fVehicleStatus || undefined,
        customer_id: fCustomer?.siber_id || undefined,
        per_page: PER_PAGE,
        page,
      })
      .then((res) => {
        setRows(res.data.data);
        setTotal(res.data.total);
      })
      .catch(() => addToast("Araç listesi yüklenemedi", "error"))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch, page, fCarType, fRomorkType, fVehicleOwner, fVehicleStatus, fCustomer]);

  function resetForm() {
    setForm({ siber_company_id: "", plate_number: "", car_type: "", romork_type: "", vehicle_owner: "", vehicle_status: "", km: "", width: "", length: "", height: "", capacity: "" });
    setCustomer(null);
    setErrors({});
  }

  function openNew() {
    setEditingId(null);
    resetForm();
    setDrawerOpen(true);
  }

  async function openEdit(id: number) {
    setEditingId(id);
    setDrawerOpen(true);
    try {
      const res = await api.get<DataMessage<CarItem>>(`/api/v1/car/${id}`);
      const c = res.data;
      setForm({
        // Mevcut kayıtta şirket değiştirilmiyor; seçici boş kalır.
        siber_company_id: "",
        plate_number: c.plate_number ?? "",
        car_type: c.car_type?.id ? String(c.car_type.id) : "",
        romork_type: c.romork_type?.id ? String(c.romork_type.id) : "",
        vehicle_owner: c.vehicle_owner?.id ? String(c.vehicle_owner.id) : "",
        vehicle_status: c.vehicle_status?.id ? String(c.vehicle_status.id) : "",
        km: c.km != null ? String(c.km) : "",
        width: c.width != null ? String(c.width) : "",
        length: c.length != null ? String(c.length) : "",
        height: c.height != null ? String(c.height) : "",
        capacity: c.capacity != null ? String(c.capacity) : "",
      });
      setCustomer(c.customer);
      setErrors({});
    } catch {
      addToast("Araç bilgileri yüklenemedi", "error");
      setDrawerOpen(false);
    }
  }

  async function handleSubmit() {
    // Buton disabled={saving} render'a kadar DOM'a yansımıyor — hızlı çift
    // tıklama/tekrar tetiklemeye karşı erken çıkış.
    if (saving) return;
    setSaving(true);
    setErrors({});
    const num = (v: string) => (v === "" ? null : Number(v));
    const body = {
      id: editingId ?? undefined,
      siber_company_id: form.siber_company_id || null,
      plate_number: form.plate_number,
      car_type: num(form.car_type),
      romork_type: num(form.romork_type),
      vehicle_owner: num(form.vehicle_owner),
      vehicle_status: num(form.vehicle_status),
      // olsold: cars.customer_id yerel id değil, cari'nin Siber id'sini tutar
      // (BagliFirmaId olarak Siber'e yazılıyor) — bkz. CarService.SingleAsync.
      customer_id: customer?.siber_id ?? null,
      km: num(form.km),
      width: num(form.width),
      length: num(form.length),
      height: num(form.height),
      capacity: num(form.capacity),
    };
    try {
      if (editingId) {
        await api.put("/api/v1/car", body);
        addToast("Araç güncellendi");
      } else {
        await api.post("/api/v1/car", body);
        addToast("Araç eklendi");
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

  async function handleDelete(id: number, plate: string | null) {
    if (!window.confirm(`"${plate ?? id}" silinsin mi?`)) return;
    try {
      await api.delete("/api/v1/car", { deletion_id: [id] });
      addToast("Araç silindi");
      load();
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Silinemedi", "error");
    }
  }

  return (
    <>
      <ModulePage
        title="Araçlar"
        action={canCreate ? <Btn onClick={openNew}><Plus size={14} />Yeni Araç</Btn> : undefined}
      >
        <div className="bg-white border-b border-gray-200 px-6 py-4">
          <div className="flex items-center gap-2.5">
            <div className="flex-1 max-w-md">
              <TextInput value={search} onChange={(v) => { setSearch(v); setPage(1); }} placeholder="Genel arama: plaka..." />
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
                  <AccountPicker label="Kiralanan Firma" value={fCustomer} onChange={(v) => { setFCustomer(v); setPage(1); }} />
                  <FormField label="Araç Tipi">
                    <SelectInput value={fCarType} onChange={(v) => { setFCarType(v); setPage(1); }} options={[{ value: "", label: "Seçiniz" }, ...carTypes.map((t) => ({ value: String(t.id), label: t.name }))]} />
                  </FormField>
                  <FormField label="Romork Tipi">
                    <SelectInput value={fRomorkType} onChange={(v) => { setFRomorkType(v); setPage(1); }} options={[{ value: "", label: "Seçiniz" }, ...romorkTypes.map((t) => ({ value: String(t.id), label: t.name }))]} />
                  </FormField>
                  <FormField label="Sahiplik Durumu">
                    <SelectInput value={fVehicleOwner} onChange={(v) => { setFVehicleOwner(v); setPage(1); }} options={[{ value: "", label: "Seçiniz" }, ...carOwners.map((t) => ({ value: String(t.id), label: t.name }))]} />
                  </FormField>
                  <FormField label="Araç Durumu">
                    <SelectInput value={fVehicleStatus} onChange={(v) => { setFVehicleStatus(v); setPage(1); }} options={[{ value: "", label: "Seçiniz" }, ...carStatuses.map((t) => ({ value: String(t.id), label: t.name }))]} />
                  </FormField>
                </div>
              </motion.div>
            )}
          </AnimatePresence>
        </div>
        <div className="bg-gray-50/70 min-h-full">
          {!loading && rows.length === 0 ? (
            <EmptyState icon={Car} title="Araç bulunamadı" desc="Arama kriterlerine uygun araç bulunamadı." />
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
                      <CarCard
                        key={r.id}
                        row={r}
                        index={i}
                        onClick={() => openEdit(r.id)}
                        canDelete={canDelete}
                        onDelete={() => handleDelete(r.id, r.plate_number)}
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
        title={editingId ? form.plate_number || "Araç" : "Yeni Araç"}
        subtitle={editingId ? undefined : "Yeni araç kaydı oluştur"}
        footer={
          (editingId ? canUpdate : canCreate) && (
            <div className="flex gap-2">
              <Btn onClick={handleSubmit} disabled={saving}>{saving ? "Kaydediliyor..." : "Kaydet"}</Btn>
              <Btn variant="secondary" onClick={() => setDrawerOpen(false)}>İptal</Btn>
            </div>
          )
        }
      >
        <div className="p-6 grid grid-cols-2 gap-4">
          <div className="col-span-2">
            <CompanyPicker
              value={form.siber_company_id}
              onChange={(v) => setForm((f) => ({ ...f, siber_company_id: v }))}
            />
            <FormField label="Plaka" required error={errors.plate_number?.[0]}>
              <TextInput value={form.plate_number} onChange={(v) => setForm((f) => ({ ...f, plate_number: v }))} placeholder="34 TRK 0000" error={!!errors.plate_number} />
            </FormField>
            {editingId && <p className="mt-1.5 text-xs text-gray-500">Kullanımda olan aracın plakası değiştirilemez.</p>}
          </div>
          <div className="col-span-2">
            <AccountPicker label="Kiralanan Firma" value={customer} onChange={setCustomer} required error={errors.customer_id?.[0]} />
          </div>
          <FormField label="Araç Tipi" required error={errors.car_type?.[0]}>
            <SelectInput value={form.car_type} onChange={(v) => setForm((f) => ({ ...f, car_type: v }))} options={[{ value: "", label: "Seçiniz" }, ...carTypes.map((t) => ({ value: String(t.id), label: t.name }))]} />
          </FormField>
          <FormField label="Romork Tipi" required error={errors.romork_type?.[0]}>
            <SelectInput value={form.romork_type} onChange={(v) => setForm((f) => ({ ...f, romork_type: v }))} options={[{ value: "", label: "Seçiniz" }, ...romorkTypes.map((t) => ({ value: String(t.id), label: t.name }))]} />
            <button type="button" onClick={() => setDepartmentModalOpen(true)} className="mt-1 text-[11px] text-blue-600 hover:underline text-left">Yeni Ekle</button>
          </FormField>
          <FormField label="Sahiplik Durumu" required error={errors.vehicle_owner?.[0]}>
            <SelectInput value={form.vehicle_owner} onChange={(v) => setForm((f) => ({ ...f, vehicle_owner: v }))} options={[{ value: "", label: "Seçiniz" }, ...carOwners.map((t) => ({ value: String(t.id), label: t.name }))]} />
          </FormField>
          <FormField label="Araç Durumu" required error={errors.vehicle_status?.[0]}>
            <SelectInput value={form.vehicle_status} onChange={(v) => setForm((f) => ({ ...f, vehicle_status: v }))} options={[{ value: "", label: "Seçiniz" }, ...carStatuses.map((t) => ({ value: String(t.id), label: t.name }))]} />
          </FormField>
          <FormField label="Kilometre" required error={errors.km?.[0]}>
            <TextInput value={form.km} onChange={(v) => setForm((f) => ({ ...f, km: v }))} type="number" error={!!errors.km} />
          </FormField>
          <FormField label="Genişlik (m)" required error={errors.width?.[0]}>
            <TextInput value={form.width} onChange={(v) => setForm((f) => ({ ...f, width: v }))} type="number" error={!!errors.width} />
          </FormField>
          <FormField label="Uzunluk (m)" required error={errors.length?.[0]}>
            <TextInput value={form.length} onChange={(v) => setForm((f) => ({ ...f, length: v }))} type="number" error={!!errors.length} />
          </FormField>
          <FormField label="Yükseklik (m)" required error={errors.height?.[0]}>
            <TextInput value={form.height} onChange={(v) => setForm((f) => ({ ...f, height: v }))} type="number" error={!!errors.height} />
          </FormField>
          <FormField label="Taşıma Kapasitesi (kg)" required error={errors.capacity?.[0]}>
            <TextInput value={form.capacity} onChange={(v) => setForm((f) => ({ ...f, capacity: v }))} type="number" error={!!errors.capacity} />
          </FormField>
        </div>
      </Drawer>

      <DepartmentManagerModal open={departmentModalOpen} onClose={() => setDepartmentModalOpen(false)} />
    </>
  );
}
