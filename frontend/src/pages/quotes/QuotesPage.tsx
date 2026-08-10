import { useEffect, useState } from "react";
import { FileText, Package, Plus } from "lucide-react";
import { api, ApiError, type DataMessage, type Paginated } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useDebouncedValue, useLookupOptions } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { ModulePage } from "@/components/ui/ModulePage";
import { DataTable, EmptyState, Pagination, RowActions, type Column } from "@/components/ui/DataTable";
import { Drawer } from "@/components/ui/Overlay";
import { Badge, Btn, FormField, SelectInput, TextInput } from "@/components/ui/primitives";

interface NamedRef {
  id: number;
  name: string | null;
}
interface AccountRef {
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
  customer_id: AccountRef | null;
  load_content_count: number;
}

// DATA-002 düzeltmesi: ham status_type_id'ye güvenmek yerine gerçek StatusType
// tablosundan id -> ad eşlemesi çekilir (kararlı kod: status_types.number).
function useStatusTypeMap() {
  const { options } = useLookupOptions("/api/v1/status_type");
  const map: Record<number, string> = {};
  options.forEach((o) => (map[Number(o.id)] = o.name));
  return map;
}

const PER_PAGE = 8;

export function QuotesPage() {
  const { can } = useAuth();
  const { addToast } = useToast();
  const canCreate = can("load_management", "create");
  const canDelete = can("load_management", "delete");
  const statusMap = useStatusTypeMap();

  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);
  const [page, setPage] = useState(1);
  const [rows, setRows] = useState<LoadItem[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);

  const [drawerOpen, setDrawerOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  const [form, setForm] = useState({
    work_type_id: "",
    loading_type_id: "",
    payment_type_id: "",
    status_type_id: "",
    department_id: "",
    customer_id: "",
    offer_date: new Date().toISOString().slice(0, 10),
    offer_validity_date: "",
    marketing_notification_date: new Date().toISOString().slice(0, 10),
    description: "",
  });
  const [content, setContent] = useState([{ product_type_id: "", case_type_id: "", quantity: "1", gross_weight: "", volume: "", lademeter: "", width: "", height: "", length: "", stackable: "1" }]);

  const { options: workTypes } = useLookupOptions("/api/v1/work_type");
  const { options: loadingTypes } = useLookupOptions("/api/v1/loading_type");
  const { options: paymentTypes } = useLookupOptions("/api/v1/payment_type");
  const { options: statusTypes } = useLookupOptions("/api/v1/status_type");
  const { options: departments } = useLookupOptions("/api/v1/department");
  const { options: productTypes } = useLookupOptions("/api/v1/product_type");
  const { options: caseTypes } = useLookupOptions("/api/v1/case_type");

  function load() {
    setLoading(true);
    api
      .get<DataMessage<Paginated<LoadItem>>>("/api/v1/load", { search: debouncedSearch || undefined, per_page: PER_PAGE, page })
      .then((res) => {
        setRows(res.data.data);
        setTotal(res.data.total);
      })
      .catch(() => addToast("Teklif listesi yüklenemedi", "error"))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch, page]);

  function openNew() {
    setForm({
      work_type_id: "",
      loading_type_id: "",
      payment_type_id: "",
      status_type_id: "",
      department_id: "",
      customer_id: "",
      offer_date: new Date().toISOString().slice(0, 10),
      offer_validity_date: "",
      marketing_notification_date: new Date().toISOString().slice(0, 10),
      description: "",
    });
    setContent([{ product_type_id: "", case_type_id: "", quantity: "1", gross_weight: "", volume: "", lademeter: "", width: "", height: "", length: "", stackable: "1" }]);
    setErrors({});
    setDrawerOpen(true);
  }

  async function handleSubmit() {
    setSaving(true);
    setErrors({});
    const fd = new FormData();
    fd.append("work_type_id", form.work_type_id);
    fd.append("loading_type_id", form.loading_type_id);
    fd.append("payment_type_id", form.payment_type_id);
    fd.append("status_type_id", form.status_type_id);
    fd.append("department_id", form.department_id);
    fd.append("customer_id", form.customer_id);
    fd.append("offer_date", form.offer_date);
    fd.append("offer_validity_date", form.offer_validity_date);
    fd.append("marketing_notification_date", form.marketing_notification_date);
    fd.append("description", form.description);
    content.forEach((item, i) => {
      fd.append(`load_content[${i}][product_type_id]`, item.product_type_id);
      fd.append(`load_content[${i}][case_type_id]`, item.case_type_id);
      fd.append(`load_content[${i}][quantity]`, item.quantity);
      fd.append(`load_content[${i}][gross_weight]`, item.gross_weight);
      fd.append(`load_content[${i}][volume]`, item.volume);
      fd.append(`load_content[${i}][lademeter]`, item.lademeter);
      fd.append(`load_content[${i}][width]`, item.width);
      fd.append(`load_content[${i}][height]`, item.height);
      fd.append(`load_content[${i}][length]`, item.length);
      fd.append(`load_content[${i}][stackable]`, item.stackable);
    });

    try {
      await api.postForm("/api/v1/load", fd);
      addToast("Teklif oluşturuldu");
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

  const columns: Column<LoadItem>[] = [
    { key: "id", header: "Teklif No", sortable: true, render: (r) => <span className="font-mono text-[11px] text-blue-600">{r.reservation_number ?? `T${r.id}`}</span> },
    { key: "customer", header: "Müşteri", sortable: true, render: (r) => <span className="font-semibold">{r.customer_id?.name ?? "—"}</span> },
    { key: "work_type", header: "İş Tipi", render: (r) => <span className="text-xs text-gray-500">{r.work_type_id?.name ?? "—"}</span> },
    { key: "content_count", header: "İçerik", render: (r) => <span className="font-mono text-xs">{r.load_content_count} kalem</span> },
    { key: "offer_date", header: "Tarih", render: (r) => <span className="font-mono text-xs text-gray-500">{r.offer_date ? new Date(r.offer_date).toLocaleDateString("tr-TR") : "—"}</span> },
    { key: "status", header: "Durum", render: (r) => (r.status_type_id != null && statusMap[r.status_type_id] ? <Badge label={statusMap[r.status_type_id]} /> : "—") },
  ];

  return (
    <>
      <ModulePage
        title="Teklifler"
        search={search}
        onSearchChange={(v) => { setSearch(v); setPage(1); }}
        searchPlaceholder="Teklif no, müşteri..."
        action={canCreate ? <Btn onClick={openNew}><Plus size={14} />Yeni Teklif</Btn> : undefined}
      >
        <div className="bg-white">
          {!loading && rows.length === 0 ? (
            <EmptyState icon={FileText} title="Teklif bulunamadı" desc="Arama kriterlerine uygun teklif bulunamadı." />
          ) : (
            <>
              <DataTable
                data={rows}
                columns={columns}
                loading={loading}
                actions={(r) => (
                  <div className="flex items-center justify-end gap-1">
                    <button
                      title="Siber'e Aktar"
                      onClick={() => handleTransferToSiber(r.id)}
                      className="p-1.5 rounded text-gray-400 hover:bg-blue-50 hover:text-blue-600 transition-colors"
                    >
                      <Package size={14} />
                    </button>
                    {canDelete && <RowActions onDelete={() => handleDelete(r.id, r.reservation_number)} />}
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
        title="Yeni Teklif"
        subtitle="Yeni teklif oluştur"
        width="w-[640px]"
        footer={
          canCreate && (
            <div className="flex gap-2">
              <Btn onClick={handleSubmit} disabled={saving}>{saving ? "Kaydediliyor..." : "Kaydet"}</Btn>
              <Btn variant="secondary" onClick={() => setDrawerOpen(false)}>İptal</Btn>
            </div>
          )
        }
      >
        <div className="p-6 space-y-6">
          <div className="grid grid-cols-2 gap-4">
            <FormField label="Müşteri (Cari ID)" required error={errors.customer_id?.[0]}>
              <TextInput value={form.customer_id} onChange={(v) => setForm((f) => ({ ...f, customer_id: v }))} placeholder="Cari ID" error={!!errors.customer_id} />
            </FormField>
            <FormField label="İş Tipi" required error={errors.work_type_id?.[0]}>
              <SelectInput value={form.work_type_id} onChange={(v) => setForm((f) => ({ ...f, work_type_id: v }))} options={[{ value: "", label: "Seçiniz" }, ...workTypes.map((t) => ({ value: String(t.id), label: t.name }))]} />
            </FormField>
            <FormField label="Yükleme Tipi" required error={errors.loading_type_id?.[0]}>
              <SelectInput value={form.loading_type_id} onChange={(v) => setForm((f) => ({ ...f, loading_type_id: v }))} options={[{ value: "", label: "Seçiniz" }, ...loadingTypes.map((t) => ({ value: String(t.id), label: t.name }))]} />
            </FormField>
            <FormField label="Ödeme Tipi" required error={errors.payment_type_id?.[0]}>
              <SelectInput value={form.payment_type_id} onChange={(v) => setForm((f) => ({ ...f, payment_type_id: v }))} options={[{ value: "", label: "Seçiniz" }, ...paymentTypes.map((t) => ({ value: String(t.id), label: t.name }))]} />
            </FormField>
            <FormField label="Durum" required error={errors.status_type_id?.[0]}>
              <SelectInput value={form.status_type_id} onChange={(v) => setForm((f) => ({ ...f, status_type_id: v }))} options={[{ value: "", label: "Seçiniz" }, ...statusTypes.map((t) => ({ value: String(t.id), label: t.name }))]} />
            </FormField>
            <FormField label="Departman" required error={errors.department_id?.[0]}>
              <SelectInput value={form.department_id} onChange={(v) => setForm((f) => ({ ...f, department_id: v }))} options={[{ value: "", label: "Seçiniz" }, ...departments.map((t) => ({ value: String(t.id), label: t.name }))]} />
            </FormField>
            <FormField label="Teklif Tarihi" required error={errors.offer_date?.[0]}>
              <TextInput value={form.offer_date} onChange={(v) => setForm((f) => ({ ...f, offer_date: v }))} type="date" error={!!errors.offer_date} />
            </FormField>
            <FormField label="Geçerlilik Tarihi" required error={errors.offer_validity_date?.[0]}>
              <TextInput value={form.offer_validity_date} onChange={(v) => setForm((f) => ({ ...f, offer_validity_date: v }))} type="date" error={!!errors.offer_validity_date} />
            </FormField>
          </div>

          <div>
            <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider mb-2">Yük İçeriği</p>
            {content.map((item, i) => (
              <div key={i} className="border border-gray-200 rounded-lg p-4 mb-2 grid grid-cols-3 gap-3">
                <FormField label="Ürün Tipi">
                  <SelectInput value={item.product_type_id} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, product_type_id: v } : x)))} options={[{ value: "", label: "Seçiniz" }, ...productTypes.map((t) => ({ value: String(t.id), label: t.name }))]} />
                </FormField>
                <FormField label="Kap Tipi">
                  <SelectInput value={item.case_type_id} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, case_type_id: v } : x)))} options={[{ value: "", label: "Seçiniz" }, ...caseTypes.map((t) => ({ value: String(t.id), label: t.name }))]} />
                </FormField>
                <FormField label="Adet">
                  <TextInput value={item.quantity} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, quantity: v } : x)))} type="number" />
                </FormField>
                <FormField label="Brüt Ağırlık (kg)">
                  <TextInput value={item.gross_weight} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, gross_weight: v } : x)))} />
                </FormField>
                <FormField label="Hacim (m³)">
                  <TextInput value={item.volume} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, volume: v } : x)))} />
                </FormField>
                <FormField label="Lademetre">
                  <TextInput value={item.lademeter} onChange={(v) => setContent((list) => list.map((x, xi) => (xi === i ? { ...x, lademeter: v } : x)))} />
                </FormField>
              </div>
            ))}
          </div>

          <div className="col-span-2">
            <FormField label="Açıklama">
              <TextInput value={form.description} onChange={(v) => setForm((f) => ({ ...f, description: v }))} />
            </FormField>
          </div>
        </div>
      </Drawer>
    </>
  );
}
