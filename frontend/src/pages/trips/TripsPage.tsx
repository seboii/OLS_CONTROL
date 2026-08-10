import { useEffect, useState } from "react";
import { Truck, Plus } from "lucide-react";
import { api, ApiError, type DataMessage, type Paginated } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useDebouncedValue, useLookupOptions } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { ModulePage } from "@/components/ui/ModulePage";
import { DataTable, EmptyState, Pagination, RowActions, type Column } from "@/components/ui/DataTable";
import { Drawer } from "@/components/ui/Overlay";
import { Badge, Btn, FormField, SelectInput, TextInput } from "@/components/ui/primitives";

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
  work_type: NamedRef | null;
  expedition_type_id: NamedRef | null;
  status_id: NamedRef | null;
  department_id: NamedRef | null;
  romork_id: CarRef | null;
  start_city_id: NamedRef | null;
  end_city_id: NamedRef | null;
}

const PER_PAGE = 8;

export function TripsPage() {
  const { can } = useAuth();
  const { addToast } = useToast();
  const canCreate = can("expedition_management", "create");
  const canUpdate = can("expedition_management", "update");
  const canDelete = can("expedition_management", "delete");

  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);
  const [page, setPage] = useState(1);
  const [rows, setRows] = useState<ExpeditionItem[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);

  const [drawerOpen, setDrawerOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  const [form, setForm] = useState({
    romork_id: "",
    work_type: "",
    department_id: "",
    expedition_type: "",
    release_date: "",
    entry_date: "",
    loading_date: "",
    return_date: "",
  });

  const { options: workTypes } = useLookupOptions("/api/v1/work_type");
  const { options: departments } = useLookupOptions("/api/v1/department");
  const { options: expeditionTypes } = useLookupOptions("/api/v1/expedition_type");

  function load() {
    setLoading(true);
    api
      .get<DataMessage<Paginated<ExpeditionItem>>>("/api/v1/expedition", { search: debouncedSearch || undefined, per_page: PER_PAGE, page })
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
  }, [debouncedSearch, page]);

  function openNew() {
    setEditingId(null);
    setForm({ romork_id: "", work_type: "", department_id: "", expedition_type: "", release_date: "", entry_date: "", loading_date: "", return_date: "" });
    setErrors({});
    setDrawerOpen(true);
  }

  async function handleSubmit() {
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
      if (editingId) {
        body.id = editingId;
        await api.put("/api/v1/expedition", body);
        addToast("Sefer güncellendi");
      } else {
        await api.post("/api/v1/expedition", body);
        addToast("Sefer oluşturuldu");
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
      await api.delete("/api/v1/expedition", { deletion_id: [id] });
      addToast("Sefer silindi");
      load();
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Silinemedi", "error");
    }
  }

  const columns: Column<ExpeditionItem>[] = [
    { key: "expedition_number", header: "Sefer No", sortable: true, render: (r) => <span className="font-mono text-[11px] text-blue-600">{r.expedition_number ?? `SEF-${r.id}`}</span> },
    { key: "romork_id", header: "Araç", render: (r) => <span className="font-mono text-xs font-semibold">{r.romork_id?.plate_number ?? "—"}</span> },
    { key: "work_type", header: "İş Tipi", render: (r) => r.work_type?.name ?? "—" },
    { key: "route", header: "Güzergâh", render: (r) => <span>{r.start_city_id?.name ?? "—"} → {r.end_city_id?.name ?? "—"}</span> },
    { key: "department_id", header: "Departman", render: (r) => r.department_id?.name ?? "—" },
    { key: "status_id", header: "Durum", render: (r) => (r.status_id?.name ? <Badge label={r.status_id.name} /> : "—") },
  ];

  return (
    <>
      <ModulePage
        title="Seferler"
        search={search}
        onSearchChange={(v) => { setSearch(v); setPage(1); }}
        searchPlaceholder="Sefer no, plaka..."
        action={canCreate ? <Btn onClick={openNew}><Plus size={14} />Yeni Sefer</Btn> : undefined}
      >
        <div className="bg-white">
          {!loading && rows.length === 0 ? (
            <EmptyState icon={Truck} title="Sefer bulunamadı" desc="Arama kriterlerine uygun sefer bulunamadı." />
          ) : (
            <>
              <DataTable
                data={rows}
                columns={columns}
                loading={loading}
                actions={
                  canUpdate || canDelete
                    ? (r) => <RowActions onDelete={canDelete ? () => handleDelete(r.id, r.expedition_number) : undefined} />
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
        title="Yeni Sefer"
        subtitle="Yeni sefer kaydı oluştur"
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
          <FormField label="Araç (Plaka)" required error={errors.romork_id?.[0]}>
            <TextInput value={form.romork_id} onChange={(v) => setForm((f) => ({ ...f, romork_id: v }))} placeholder="Araç ID" error={!!errors.romork_id} />
          </FormField>
          <FormField label="İş Tipi" required>
            <SelectInput value={form.work_type} onChange={(v) => setForm((f) => ({ ...f, work_type: v }))} options={[{ value: "", label: "Seçiniz" }, ...workTypes.map((t) => ({ value: String(t.id), label: t.name }))]} />
          </FormField>
          <FormField label="Departman" required>
            <SelectInput value={form.department_id} onChange={(v) => setForm((f) => ({ ...f, department_id: v }))} options={[{ value: "", label: "Seçiniz" }, ...departments.map((t) => ({ value: String(t.id), label: t.name }))]} />
          </FormField>
          <FormField label="Sefer Tipi">
            <SelectInput value={form.expedition_type} onChange={(v) => setForm((f) => ({ ...f, expedition_type: v }))} options={[{ value: "", label: "Seçiniz" }, ...expeditionTypes.map((t) => ({ value: String(t.id), label: t.name }))]} />
          </FormField>
          <FormField label="Çıkış Tarihi">
            <TextInput value={form.release_date} onChange={(v) => setForm((f) => ({ ...f, release_date: v }))} type="date" />
          </FormField>
          <FormField label="Kayıt Tarihi">
            <TextInput value={form.entry_date} onChange={(v) => setForm((f) => ({ ...f, entry_date: v }))} type="date" />
          </FormField>
          <FormField label="Yükleme Tarihi">
            <TextInput value={form.loading_date} onChange={(v) => setForm((f) => ({ ...f, loading_date: v }))} type="date" />
          </FormField>
          <FormField label="Dönüş Tarihi">
            <TextInput value={form.return_date} onChange={(v) => setForm((f) => ({ ...f, return_date: v }))} type="date" />
          </FormField>
        </div>
      </Drawer>
    </>
  );
}
