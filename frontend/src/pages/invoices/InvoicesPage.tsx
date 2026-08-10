import { useEffect, useState } from "react";
import { Receipt, Plus } from "lucide-react";
import { api, ApiError, type DataMessage, type Paginated } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useDebouncedValue, useLookupOptions } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { ModulePage } from "@/components/ui/ModulePage";
import { DataTable, EmptyState, Pagination, RowActions, type Column } from "@/components/ui/DataTable";
import { Drawer } from "@/components/ui/Overlay";
import { Badge, Btn, FormField, SelectInput, TextareaInput, TextInput } from "@/components/ui/primitives";

interface NamedRef {
  id: number;
  name: string | null;
}

interface InvoiceItem {
  id: number;
  box_type: 0 | 1;
  commercial_type: number;
  message: string | null;
  invoice_create_date: string | null;
  invoice_execution_date: string | null;
  payable_amount: number | null;
  document_currency_code: string | null;
  account: NamedRef | null;
  invoice_status: NamedRef | null;
  invoice_type: NamedRef | null;
}

const PER_PAGE = 8;
const BOX_TABS = [
  { value: "", label: "Tümü" },
  { value: "0", label: "Gider Faturalar" },
  { value: "1", label: "Gelir Faturalar" },
];

export function InvoicesPage() {
  const { can } = useAuth();
  const { addToast } = useToast();
  const canCreate = can("invoice_management", "create");
  const canDelete = can("invoice_management", "delete");

  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);
  const [boxType, setBoxType] = useState("");
  const [page, setPage] = useState(1);
  const [rows, setRows] = useState<InvoiceItem[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);

  const [drawerOpen, setDrawerOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  const [form, setForm] = useState({
    box_type: "1",
    commercial_type: "0",
    account_id: "",
    invoice_type_id: "",
    invoice_create_date: "",
    invoice_execution_date: "",
    message: "",
  });

  const { options: invoiceTypes } = useLookupOptions("/api/v1/invoice_type");

  function load() {
    setLoading(true);
    api
      .get<DataMessage<Paginated<InvoiceItem>>>("/api/v1/invoice", {
        search: debouncedSearch || undefined,
        box_type: boxType || undefined,
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
  }, [debouncedSearch, boxType, page]);

  function openNew() {
    setForm({ box_type: "1", commercial_type: "0", account_id: "", invoice_type_id: "", invoice_create_date: "", invoice_execution_date: "", message: "" });
    setErrors({});
    setDrawerOpen(true);
  }

  async function handleSubmit() {
    setSaving(true);
    setErrors({});
    const fd = new FormData();
    fd.append("box_type", form.box_type);
    fd.append("commercial_type", form.commercial_type);
    fd.append("account_id", form.account_id);
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

  const columns: Column<InvoiceItem>[] = [
    { key: "id", header: "Fatura No", sortable: true, render: (r) => <span className="font-mono text-[11px] text-blue-600">FAT-{r.id}</span> },
    { key: "account", header: "Müşteri", sortable: true, render: (r) => <span className="font-semibold">{r.account?.name ?? "—"}</span> },
    { key: "invoice_type", header: "Tip", render: (r) => <span className="text-xs text-gray-500">{r.invoice_type?.name ?? "—"}</span> },
    { key: "invoice_create_date", header: "Fatura Tarihi", render: (r) => <span className="font-mono text-xs">{r.invoice_create_date ? new Date(r.invoice_create_date).toLocaleDateString("tr-TR") : "—"}</span> },
    { key: "payable_amount", header: "Toplam", render: (r) => <span className="font-mono text-xs font-bold">{r.payable_amount != null ? r.payable_amount.toLocaleString("tr-TR", { minimumFractionDigits: 2 }) : "0,00"} {r.document_currency_code ?? ""}</span> },
    { key: "invoice_status", header: "Durum", render: (r) => (r.invoice_status?.name ? <Badge label={r.invoice_status.name} /> : "—") },
  ];

  return (
    <>
      <ModulePage
        title="Faturalar"
        search={search}
        onSearchChange={(v) => { setSearch(v); setPage(1); }}
        searchPlaceholder="Müşteri, referans..."
        filters={
          <SelectInput
            value={boxType}
            onChange={(v) => { setBoxType(v); setPage(1); }}
            options={BOX_TABS.map((b) => ({ value: b.value, label: b.label }))}
          />
        }
        action={canCreate ? <Btn onClick={openNew}><Plus size={14} />Yeni Fatura</Btn> : undefined}
      >
        <div className="bg-white">
          {!loading && rows.length === 0 ? (
            <EmptyState icon={Receipt} title="Fatura bulunamadı" desc="Arama kriterlerine uygun fatura bulunamadı." />
          ) : (
            <>
              <DataTable
                data={rows}
                columns={columns}
                loading={loading}
                actions={canDelete ? (r) => <RowActions onDelete={() => handleDelete(r.id)} /> : undefined}
              />
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
            <SelectInput value={form.box_type} onChange={(v) => setForm((f) => ({ ...f, box_type: v }))} options={[{ value: "0", label: "Gider (Alış)" }, { value: "1", label: "Gelir (Satış)" }]} />
          </FormField>
          <FormField label="Fatura Tipi" required error={errors.invoice_type_id?.[0]}>
            <SelectInput value={form.invoice_type_id} onChange={(v) => setForm((f) => ({ ...f, invoice_type_id: v }))} options={[{ value: "", label: "Seçiniz" }, ...invoiceTypes.map((t) => ({ value: String(t.id), label: t.name }))]} />
          </FormField>
          <FormField label="Müşteri (Cari ID)" required error={errors.account_id?.[0]}>
            <TextInput value={form.account_id} onChange={(v) => setForm((f) => ({ ...f, account_id: v }))} placeholder="Cari ID" error={!!errors.account_id} />
          </FormField>
          <FormField label="Fatura Türü">
            <SelectInput value={form.commercial_type} onChange={(v) => setForm((f) => ({ ...f, commercial_type: v }))} options={[{ value: "0", label: "Temel Fatura" }, { value: "1", label: "Ticari Fatura" }]} />
          </FormField>
          <FormField label="Fatura Tarihi" required error={errors.invoice_create_date?.[0]}>
            <TextInput value={form.invoice_create_date} onChange={(v) => setForm((f) => ({ ...f, invoice_create_date: v }))} type="date" error={!!errors.invoice_create_date} />
          </FormField>
          <FormField label="Vade Tarihi">
            <TextInput value={form.invoice_execution_date} onChange={(v) => setForm((f) => ({ ...f, invoice_execution_date: v }))} type="date" />
          </FormField>
          <div className="col-span-2">
            <FormField label="Açıklama">
              <TextareaInput value={form.message} onChange={(v) => setForm((f) => ({ ...f, message: v }))} placeholder="Fatura notu..." />
            </FormField>
          </div>
        </div>
      </Drawer>
    </>
  );
}
