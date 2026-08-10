import { useEffect, useState } from "react";
import { Car, Plus } from "lucide-react";
import { api, ApiError, type DataMessage, type Paginated } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useDebouncedValue, useLookupOptions } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { ModulePage } from "@/components/ui/ModulePage";
import { DataTable, EmptyState, Pagination, RowActions, type Column } from "@/components/ui/DataTable";
import { Drawer } from "@/components/ui/Overlay";
import { Btn, FormField, TextInput, SelectInput } from "@/components/ui/primitives";

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
  customer: NamedRef | null;
}

const PER_PAGE = 10;

export function VehiclesPage() {
  const { can } = useAuth();
  const { addToast } = useToast();
  const canCreate = can("car_management", "create");
  const canUpdate = can("car_management", "update");
  const canDelete = can("car_management", "delete");

  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);
  const [page, setPage] = useState(1);
  const [rows, setRows] = useState<CarItem[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);

  const [drawerOpen, setDrawerOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  const [form, setForm] = useState({
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

  const { options: carTypes } = useLookupOptions("/api/v1/car_type");
  const { options: romorkTypes } = useLookupOptions("/api/v1/romork_type");
  const { options: carOwners } = useLookupOptions("/api/v1/car_owner");
  const { options: carStatuses } = useLookupOptions("/api/v1/car_status");

  function load() {
    setLoading(true);
    api
      .get<DataMessage<Paginated<CarItem>>>("/api/v1/car", { search: debouncedSearch || undefined, per_page: PER_PAGE, page })
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
  }, [debouncedSearch, page]);

  function resetForm() {
    setForm({ plate_number: "", car_type: "", romork_type: "", vehicle_owner: "", vehicle_status: "", km: "", width: "", length: "", height: "", capacity: "" });
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
      setErrors({});
    } catch {
      addToast("Araç bilgileri yüklenemedi", "error");
      setDrawerOpen(false);
    }
  }

  async function handleSubmit() {
    setSaving(true);
    setErrors({});
    const num = (v: string) => (v === "" ? null : Number(v));
    const body = {
      id: editingId ?? undefined,
      plate_number: form.plate_number,
      car_type: num(form.car_type),
      romork_type: num(form.romork_type),
      vehicle_owner: num(form.vehicle_owner),
      vehicle_status: num(form.vehicle_status),
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

  const columns: Column<CarItem>[] = [
    { key: "plate_number", header: "Plaka", sortable: true, render: (r) => <span className="font-mono text-sm font-bold text-gray-900">{r.plate_number}</span> },
    { key: "car_type", header: "Araç Tipi", render: (r) => r.car_type?.name ?? "—" },
    { key: "romork_type", header: "Romork", render: (r) => <span className="text-gray-600">{r.romork_type?.name ?? "—"}</span> },
    { key: "vehicle_owner", header: "Sahiplik", render: (r) => r.vehicle_owner?.name ?? "—" },
    { key: "vehicle_status", header: "Durum", render: (r) => r.vehicle_status?.name ?? "—" },
    { key: "capacity", header: "Kapasite", render: (r) => <span className="font-mono text-xs">{r.capacity ? `${r.capacity} kg` : "—"}</span> },
    { key: "km", header: "Kilometre", render: (r) => <span className="font-mono text-xs">{r.km ?? "—"}</span> },
  ];

  return (
    <>
      <ModulePage
        title="Araçlar"
        search={search}
        onSearchChange={(v) => { setSearch(v); setPage(1); }}
        searchPlaceholder="Plaka..."
        action={canCreate ? <Btn onClick={openNew}><Plus size={14} />Yeni Araç</Btn> : undefined}
      >
        <div className="bg-white">
          {!loading && rows.length === 0 ? (
            <EmptyState icon={Car} title="Araç bulunamadı" desc="Arama kriterlerine uygun araç bulunamadı." />
          ) : (
            <>
              <DataTable
                data={rows}
                columns={columns}
                loading={loading}
                onRowClick={(r) => openEdit(r.id)}
                actions={
                  canUpdate || canDelete
                    ? (r) => <RowActions onView={() => openEdit(r.id)} onEdit={canUpdate ? () => openEdit(r.id) : undefined} onDelete={canDelete ? () => handleDelete(r.id, r.plate_number) : undefined} />
                    : undefined
                }
              />
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
          <FormField label="Plaka" required error={errors.plate_number?.[0]}>
            <TextInput value={form.plate_number} onChange={(v) => setForm((f) => ({ ...f, plate_number: v }))} placeholder="34 TRK 0000" error={!!errors.plate_number} />
          </FormField>
          <FormField label="Araç Tipi">
            <SelectInput value={form.car_type} onChange={(v) => setForm((f) => ({ ...f, car_type: v }))} options={[{ value: "", label: "Seçiniz" }, ...carTypes.map((t) => ({ value: String(t.id), label: t.name }))]} />
          </FormField>
          <FormField label="Romork Tipi">
            <SelectInput value={form.romork_type} onChange={(v) => setForm((f) => ({ ...f, romork_type: v }))} options={[{ value: "", label: "Seçiniz" }, ...romorkTypes.map((t) => ({ value: String(t.id), label: t.name }))]} />
          </FormField>
          <FormField label="Sahiplik Durumu">
            <SelectInput value={form.vehicle_owner} onChange={(v) => setForm((f) => ({ ...f, vehicle_owner: v }))} options={[{ value: "", label: "Seçiniz" }, ...carOwners.map((t) => ({ value: String(t.id), label: t.name }))]} />
          </FormField>
          <FormField label="Araç Durumu">
            <SelectInput value={form.vehicle_status} onChange={(v) => setForm((f) => ({ ...f, vehicle_status: v }))} options={[{ value: "", label: "Seçiniz" }, ...carStatuses.map((t) => ({ value: String(t.id), label: t.name }))]} />
          </FormField>
          <FormField label="Kilometre">
            <TextInput value={form.km} onChange={(v) => setForm((f) => ({ ...f, km: v }))} type="number" />
          </FormField>
          <FormField label="Genişlik (m)">
            <TextInput value={form.width} onChange={(v) => setForm((f) => ({ ...f, width: v }))} type="number" />
          </FormField>
          <FormField label="Uzunluk (m)">
            <TextInput value={form.length} onChange={(v) => setForm((f) => ({ ...f, length: v }))} type="number" />
          </FormField>
          <FormField label="Yükseklik (m)">
            <TextInput value={form.height} onChange={(v) => setForm((f) => ({ ...f, height: v }))} type="number" />
          </FormField>
          <FormField label="Taşıma Kapasitesi (kg)">
            <TextInput value={form.capacity} onChange={(v) => setForm((f) => ({ ...f, capacity: v }))} type="number" />
          </FormField>
        </div>
      </Drawer>
    </>
  );
}
