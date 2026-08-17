import { useEffect, useState } from "react";
import { Truck, Plus, Trash2, Link2 } from "lucide-react";
import { api, ApiError, type DataMessage, type Paginated } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useDebouncedValue, useLookupOptions } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { ModulePage } from "@/components/ui/ModulePage";
import { DataTable, EmptyState, Pagination, RowActions, type Column } from "@/components/ui/DataTable";
import { Drawer, Modal } from "@/components/ui/Overlay";
import { Badge, Btn, FormField, SearchInput, SelectInput, Tabs, TextareaInput, TextInput } from "@/components/ui/primitives";

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

interface ExpeditionDetail extends ExpeditionItem {
  release_date: string | null;
  loading_date: string | null;
  return_date: string | null;
  car_exit_date: string | null;
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

interface MappedLoadTransfer {
  id: number;
  load_number_work_type: string | null;
  customer_id: { id: number; name: string | null } | null;
}

interface ExpeditionMapping {
  id: number;
  date: string | null;
  load_transfer_id: MappedLoadTransfer | null;
  romork_id: CarRef | null;
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

const PER_PAGE = 8;
const DETAIL_TABS = ["Genel Bilgiler", "Bağlı Yükler", "Hareketler"];
const ZERO_TOTALS: MappingTotals = { total_quantity: 0, total_gross_weight: 0, total_net_weight: 0, total_lademeter: 0, total_volume: 0 };
const EMPTY_MOVEMENT_FORM = { destination_id: "", expedition_status_id: "", description: "", address: "" };

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
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  const [form, setForm] = useState({
    romork_id: "", work_type: "", department_id: "", expedition_type: "",
    release_date: "", entry_date: "", loading_date: "", return_date: "",
  });

  const { options: workTypes } = useLookupOptions("/api/v1/work_type");
  const { options: departments } = useLookupOptions("/api/v1/department");
  const { options: expeditionTypes } = useLookupOptions("/api/v1/expedition_type");
  const { options: expeditionStatuses } = useLookupOptions("/api/v1/expedition_status");
  const { options: destinations } = useLookupOptions("/api/v1/destination");

  function opts(list: { id: string | number; name: string }[]) {
    return [{ value: "", label: "Seçiniz" }, ...list.map((t) => ({ value: String(t.id), label: t.name }))];
  }

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
      await api.post("/api/v1/expedition", body);
      addToast("Sefer oluşturuldu");
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

  // --- Detay/düzenleme drawer'ı (Genel Bilgiler + Bağlı Yükler) ---

  const [detailOpen, setDetailOpen] = useState(false);
  const [detailId, setDetailId] = useState<number | null>(null);
  const [detailTab, setDetailTab] = useState(DETAIL_TABS[0]);
  const [detail, setDetail] = useState<ExpeditionDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailSaving, setDetailSaving] = useState(false);
  const [detailErrors, setDetailErrors] = useState<Record<string, string[]>>({});
  const [detailForm, setDetailForm] = useState({
    romork_id: "", work_type: "", department_id: "", expedition_type: "", status_id: "",
    release_date: "", entry_date: "", loading_date: "", return_date: "",
  });

  const [mappings, setMappings] = useState<ExpeditionMapping[]>([]);
  const [mappingTotals, setMappingTotals] = useState<MappingTotals>(ZERO_TOTALS);
  const [mappingsLoading, setMappingsLoading] = useState(false);

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
        work_type: d.work_type ? String(d.work_type.id) : "",
        department_id: d.department_id ? String(d.department_id.id) : "",
        expedition_type: d.expedition_type_id ? String(d.expedition_type_id.id) : "",
        status_id: d.status_id ? String(d.status_id.id) : "",
        release_date: d.release_date ?? "",
        entry_date: "",
        loading_date: d.loading_date ?? "",
        return_date: d.return_date ?? "",
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
                onRowClick={(r) => openDetail(r.id)}
                actions={
                  canDelete
                    ? (r) => <RowActions onDelete={() => handleDelete(r.id, r.expedition_number)} />
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
            <SelectInput value={form.work_type} onChange={(v) => setForm((f) => ({ ...f, work_type: v }))} options={opts(workTypes)} />
          </FormField>
          <FormField label="Departman" required>
            <SelectInput value={form.department_id} onChange={(v) => setForm((f) => ({ ...f, department_id: v }))} options={opts(departments)} />
          </FormField>
          <FormField label="Sefer Tipi" required error={errors.expedition_type?.[0]}>
            <SelectInput value={form.expedition_type} onChange={(v) => setForm((f) => ({ ...f, expedition_type: v }))} options={opts(expeditionTypes)} />
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

      <Drawer
        open={detailOpen}
        onClose={() => setDetailOpen(false)}
        title={detail?.expedition_number ?? "Sefer"}
        subtitle={detail?.romork_id?.plate_number ?? undefined}
        width="w-[640px]"
        footer={
          detailTab === "Genel Bilgiler" && canUpdate ? (
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
              {detailTab === "Genel Bilgiler" && (
                <div className="grid grid-cols-2 gap-4">
                  <FormField label="Araç (Plaka)" required error={detailErrors.romork_id?.[0]}>
                    <TextInput value={detailForm.romork_id} onChange={(v) => setDetailForm((f) => ({ ...f, romork_id: v }))} placeholder="Araç ID" error={!!detailErrors.romork_id} />
                  </FormField>
                  <FormField label="Durum" required error={detailErrors.expedition_status_id?.[0]}>
                    <SelectInput value={detailForm.status_id} onChange={(v) => setDetailForm((f) => ({ ...f, status_id: v }))} options={opts(expeditionStatuses)} />
                  </FormField>
                  <FormField label="İş Tipi" required>
                    <SelectInput value={detailForm.work_type} onChange={(v) => setDetailForm((f) => ({ ...f, work_type: v }))} options={opts(workTypes)} />
                  </FormField>
                  <FormField label="Departman" required>
                    <SelectInput value={detailForm.department_id} onChange={(v) => setDetailForm((f) => ({ ...f, department_id: v }))} options={opts(departments)} />
                  </FormField>
                  <FormField label="Sefer Tipi" required>
                    <SelectInput value={detailForm.expedition_type} onChange={(v) => setDetailForm((f) => ({ ...f, expedition_type: v }))} options={opts(expeditionTypes)} />
                  </FormField>
                  <FormField label="Çıkış Tarihi">
                    <TextInput value={detailForm.release_date} onChange={(v) => setDetailForm((f) => ({ ...f, release_date: v }))} type="date" />
                  </FormField>
                  <FormField label="Yükleme Tarihi">
                    <TextInput value={detailForm.loading_date} onChange={(v) => setDetailForm((f) => ({ ...f, loading_date: v }))} type="date" />
                  </FormField>
                  <FormField label="Dönüş Tarihi">
                    <TextInput value={detailForm.return_date} onChange={(v) => setDetailForm((f) => ({ ...f, return_date: v }))} type="date" />
                  </FormField>
                </div>
              )}

              {detailTab === "Bağlı Yükler" && (
                <div>
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
                      <div className="space-y-2">
                        {mappings.map((m) => (
                          <div key={m.id} className="border border-gray-200 rounded-lg p-3 flex items-center justify-between">
                            <div>
                              <p className="text-xs font-semibold text-blue-600">{m.load_transfer_id?.load_number_work_type ?? `#${m.load_transfer_id?.id ?? "?"}`}</p>
                              <p className="text-[11px] text-gray-500">{m.load_transfer_id?.customer_id?.name ?? "—"}</p>
                            </div>
                            <div className="flex items-center gap-3">
                              <span className="font-mono text-[11px] text-gray-500">{m.total_values.total_gross_weight} kg · {m.total_values.total_volume} m³</span>
                              {canUpdate && (
                                <button type="button" onClick={() => removeMapping(m.id)} className="text-gray-300 hover:text-red-500">
                                  <Trash2 size={13} />
                                </button>
                              )}
                            </div>
                          </div>
                        ))}
                      </div>
                      <div className="mt-4 pt-3 border-t border-gray-100 grid grid-cols-3 gap-2 text-[11px] text-gray-500">
                        <span>Toplam Adet: <b className="text-gray-700">{mappingTotals.total_quantity}</b></span>
                        <span>Brüt: <b className="text-gray-700">{mappingTotals.total_gross_weight} kg</b></span>
                        <span>Hacim: <b className="text-gray-700">{mappingTotals.total_volume} m³</b></span>
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
    </>
  );
}
