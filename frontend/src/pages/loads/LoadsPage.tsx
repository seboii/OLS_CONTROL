import { useEffect, useState } from "react";
import { Package, Plus, Trash2, Upload, File as FileIcon, X } from "lucide-react";
import { api, ApiError, type DataMessage, type Paginated } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useDebouncedValue, useLookupOptions } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { ModulePage } from "@/components/ui/ModulePage";
import { DataTable, EmptyState, Pagination, type Column } from "@/components/ui/DataTable";
import { Drawer, Modal } from "@/components/ui/Overlay";
import { Badge, Btn, FormField, SelectInput, Tabs, TextareaInput, TextInput } from "@/components/ui/primitives";
import { AccountPicker, type AccountOption } from "@/components/shared/AccountPicker";
import { UserPicker, type UserOption } from "@/components/shared/UserPicker";

interface NamedRef {
  id: number;
  name: string | null;
}

interface LoadTransferItem {
  id: number;
  load_number: string | null;
  load_number_work_type: string | null;
  customer_id: NamedRef | null;
  sender_id: NamedRef | null;
  load_status_id: NamedRef | null;
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
}

interface InvoiceItemDetail {
  id: number;
  buysell: string | null;
  net_price: number | null;
  total_price: number | null;
  quantity: number | null;
  description: string | null;
  item_id: NamedRef | null;
  account_id: AccountOption | null;
  currency_code: NamedRef | null;
}

interface LoadTransferDetail extends LoadTransferItem {
  receiver_id: NamedRef | null;
  romork_type_id: NamedRef | null;
  department_id: NamedRef | null;
  payment_type_id: NamedRef | null;
  total_gross_weight: number | null;
  total_volume: number | null;
  total_lademeter: number | null;
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
}

interface LoadFileDetail {
  id: number;
  file: string | null;
  org_name: string | null;
}

interface MovementDetail {
  id: number;
  description: string | null;
  address: string | null;
  created_at: string | null;
  destination: NamedRef | null;
  user: NamedRef | null;
  expedition_status: NamedRef | null;
  expedition_movement: { expedition?: { expedition_number: string | null } } | null;
}

type PackageRow = {
  id: number | null; product_type_id: string; quantity: string;
  gross_weight: string; net_weight: string; volume: string; lademeter: string;
  width: string; height: string; length: string; stackable: string;
};

const EMPTY_PACKAGE_ROW: PackageRow = {
  id: null, product_type_id: "", quantity: "1", gross_weight: "", net_weight: "",
  volume: "", lademeter: "", width: "", height: "", length: "", stackable: "1",
};

type InvoiceItemRow = {
  id: number | null; item_id: string; account: AccountOption | null; currency_code: string;
  buysell: string; quantity: string; net_price: string; total_price: string; description: string;
};

const EMPTY_INVOICE_ITEM_ROW: InvoiceItemRow = {
  id: null, item_id: "", account: null, currency_code: "", buysell: "1",
  quantity: "1", net_price: "", total_price: "", description: "",
};

const EMPTY_MOVEMENT_FORM = { destination_id: "", expedition_status_id: "", description: "", address: "" };

const PER_PAGE = 8;
const TABS = ["Genel Bilgiler", "Paketler", "Finans", "Görevliler", "Hareketler", "Dosya Arşivi"];

export function LoadsPage() {
  const { can } = useAuth();
  const { addToast } = useToast();
  const canUpdate = can("load_management", "update");
  const canDelete = can("load_management", "delete");

  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);
  const [page, setPage] = useState(1);
  const [rows, setRows] = useState<LoadTransferItem[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);

  const [drawerOpen, setDrawerOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [tab, setTab] = useState(TABS[0]);
  const [detail, setDetail] = useState<LoadTransferDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [saving, setSaving] = useState(false);

  const [form, setForm] = useState({
    load_status_id: "", payment_type_id: "", department_id: "", romork_type_id: "",
    total_gross_weight: "", total_volume: "", total_lademeter: "",
    instruction_arrival_date: "", request_arrival_date: "", readiness_date: "", date_of_receipt_customer: "",
  });
  const [customer, setCustomer] = useState<AccountOption | null>(null);
  const [sender, setSender] = useState<AccountOption | null>(null);
  const [receiver, setReceiver] = useState<AccountOption | null>(null);
  const [customerRep, setCustomerRep] = useState<UserOption | null>(null);
  const [secondCustomerRep, setSecondCustomerRep] = useState<UserOption | null>(null);
  const [packages, setPackages] = useState<PackageRow[]>([]);
  const [removedPackageIds, setRemovedPackageIds] = useState<number[]>([]);
  const [invoiceItems, setInvoiceItems] = useState<InvoiceItemRow[]>([]);
  const [movements, setMovements] = useState<MovementDetail[]>([]);
  const [movementModalOpen, setMovementModalOpen] = useState(false);
  const [movementForm, setMovementForm] = useState(EMPTY_MOVEMENT_FORM);
  const [savingMovement, setSavingMovement] = useState(false);
  const [existingFiles, setExistingFiles] = useState<LoadFileDetail[]>([]);
  const [removedFileIds, setRemovedFileIds] = useState<number[]>([]);
  const [newFiles, setNewFiles] = useState<File[]>([]);
  const [savingFiles, setSavingFiles] = useState(false);

  const { options: loadStatusTypes } = useLookupOptions("/api/v1/load_status_type");
  const { options: paymentTypes } = useLookupOptions("/api/v1/payment_type");
  const { options: departments } = useLookupOptions("/api/v1/department");
  const { options: romorkTypes } = useLookupOptions("/api/v1/romork_type");
  const { options: productTypes } = useLookupOptions("/api/v1/product_type");
  const { options: itemTypes } = useLookupOptions("/api/v1/item_type");
  const { options: currencies } = useLookupOptions("/api/v1/currency");
  const { options: destinations } = useLookupOptions("/api/v1/destination");
  const { options: expeditionStatuses } = useLookupOptions("/api/v1/expedition_status");

  function opts(list: { id: string | number; name: string }[]) {
    return [{ value: "", label: "Seçiniz" }, ...list.map((t) => ({ value: String(t.id), label: t.name }))];
  }

  function load() {
    setLoading(true);
    api
      .get<DataMessage<Paginated<LoadTransferItem>>>("/api/v1/load_transfer", { search: debouncedSearch || undefined, per_page: PER_PAGE, page })
      .then((res) => {
        setRows(res.data.data);
        setTotal(res.data.total);
      })
      .catch(() => addToast("Yük listesi yüklenemedi", "error"))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch, page]);

  function fetchMovements(loadTransferId: number) {
    api
      .get<{ data: MovementDetail[] }>("/api/v1/load_transfer_movement", { load_transfer_id: loadTransferId })
      .then((res) => setMovements(res.data))
      .catch(() => setMovements([]));
  }

  async function openDetail(id: number) {
    setEditingId(id);
    setTab(TABS[0]);
    setDrawerOpen(true);
    setDetailLoading(true);
    setRemovedPackageIds([]);
    setRemovedFileIds([]);
    setNewFiles([]);
    fetchMovements(id);
    try {
      const res = await api.get<DataMessage<LoadTransferDetail>>(`/api/v1/load_transfer/${id}`);
      const d = res.data;
      setDetail(d);
      setForm({
        load_status_id: d.load_status_id ? String(d.load_status_id.id) : "",
        payment_type_id: d.payment_type_id ? String(d.payment_type_id.id) : "",
        department_id: d.department_id ? String(d.department_id.id) : "",
        romork_type_id: d.romork_type_id ? String(d.romork_type_id.id) : "",
        total_gross_weight: d.total_gross_weight != null ? String(d.total_gross_weight) : "",
        total_volume: d.total_volume != null ? String(d.total_volume) : "",
        total_lademeter: d.total_lademeter != null ? String(d.total_lademeter) : "",
        instruction_arrival_date: d.instruction_arrival_date ?? "",
        request_arrival_date: d.request_arrival_date ?? "",
        readiness_date: d.readiness_date ?? "",
        date_of_receipt_customer: d.date_of_receipt_customer ?? "",
      });
      setCustomer(d.customer_id);
      setSender(d.sender_id);
      setReceiver(d.receiver_id);
      setCustomerRep(d.customer_representative);
      setSecondCustomerRep(d.second_customer_representative);
      setExistingFiles(d.load_file);
      setPackages(
        d.load_transfer_package.map((p) => ({
          id: p.id,
          product_type_id: p.product_type_id ? String(p.product_type_id.id) : "",
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
          item_id: f.item_id ? String(f.item_id.id) : "",
          account: f.account_id,
          currency_code: f.currency_code ? String(f.currency_code.id) : "",
          buysell: f.buysell ?? "1",
          quantity: f.quantity != null ? String(f.quantity) : "1",
          net_price: f.net_price != null ? String(f.net_price) : "",
          total_price: f.total_price != null ? String(f.total_price) : "",
          description: f.description ?? "",
        })),
      );
    } catch {
      addToast("Yük bilgileri yüklenemedi", "error");
      setDrawerOpen(false);
    } finally {
      setDetailLoading(false);
    }
  }

  function addPackageRow() {
    setPackages((list) => [...list, { ...EMPTY_PACKAGE_ROW }]);
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
    setPackages((list) => list.filter((_, xi) => xi !== i));
  }

  function addInvoiceItemRow(buysell: string) {
    setInvoiceItems((list) => [...list, { ...EMPTY_INVOICE_ITEM_ROW, buysell }]);
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

  /**
   * Dosyalar Yük'ün DEĞİL, dönüştüğü orijinal Teklif'in kaydına yazılır
   * (olsold: LoadFormDrawer.vue updateLoadFiles → load_id gönderiyor).
   * Bağımsız bir kaydet aksiyonu — ana "Kaydet" butonuna bağlı değil.
   */
  async function saveFiles() {
    if (!detail?.load_id) return;
    setSavingFiles(true);
    try {
      const fd = new FormData();
      fd.append("load_id", String(detail.load_id));
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

  const num = (v: string) => (v.trim() === "" ? null : Number(v.replace(",", ".")));
  const int = (v: string) => (v.trim() === "" ? null : parseInt(v, 10));

  async function handleSave() {
    if (!editingId) return;
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
        total_gross_weight: num(form.total_gross_weight),
        total_volume: num(form.total_volume),
        total_lademeter: num(form.total_lademeter),
        instruction_arrival_date: form.instruction_arrival_date || null,
        request_arrival_date: form.request_arrival_date || null,
        readiness_date: form.readiness_date || null,
        date_of_receipt_customer: form.date_of_receipt_customer || null,
        packages: packages
          .filter((p) => !removedPackageIds.includes(p.id ?? -1))
          .map((p) => ({
            id: p.id,
            product_type_id: int(p.product_type_id),
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
          item_id: int(f.item_id),
          buysell: f.buysell,
          account_id: f.account?.id ?? null,
          quantity: num(f.quantity),
          net_price: num(f.net_price),
          total_price: num(f.total_price),
          currency_code: int(f.currency_code),
          description: f.description,
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

  const columns: Column<LoadTransferItem>[] = [
    { key: "load_number", header: "Yük No", sortable: true, render: (r) => <span className="font-mono text-[11px] text-blue-600">{r.load_number_work_type ?? r.load_number ?? `Y${r.id}`}</span> },
    { key: "customer", header: "Müşteri", sortable: true, render: (r) => <span className="font-semibold">{r.customer_id?.name ?? "—"}</span> },
    { key: "sender", header: "Gönderici", render: (r) => <span>{r.sender_id?.name ?? "—"}</span> },
    { key: "status", header: "Durum", render: (r) => (r.load_status_id?.name ? <Badge label={r.load_status_id.name} /> : "—") },
  ];

  if (!can("load_management", "read")) {
    return <EmptyState icon={Package} title="Yetkiniz yok" desc="Bu ekranı görüntülemek için gerekli yetkiye sahip değilsiniz." />;
  }

  return (
    <>
      <ModulePage title="Yükler" search={search} onSearchChange={(v) => { setSearch(v); setPage(1); }} searchPlaceholder="Yük no, müşteri...">
        <div className="bg-white">
          {!loading && rows.length === 0 ? (
            <EmptyState
              icon={Package}
              title="Yük bulunamadı"
              desc="Henüz yüke dönüştürülmüş teklif yok. Yük kaydı, Siber'e aktarılmış bir teklifin Teklifler ekranından dönüştürülmesiyle oluşur."
            />
          ) : (
            <>
              <DataTable data={rows} columns={columns} loading={loading} onRowClick={(r) => openDetail(r.id)} />
              <Pagination page={page} total={total} perPage={PER_PAGE} onChange={setPage} />
            </>
          )}
        </div>
      </ModulePage>

      <Drawer
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        title={detail?.load_number_work_type ?? detail?.load_number ?? "Yük"}
        subtitle={detail?.customer_id?.name ?? undefined}
        width="w-[640px]"
        footer={
          canUpdate ? (
            <div className="flex gap-2">
              <Btn onClick={handleSave} disabled={saving || detailLoading}>{saving ? "Kaydediliyor..." : "Kaydet"}</Btn>
              <Btn variant="secondary" onClick={() => setDrawerOpen(false)}>İptal</Btn>
            </div>
          ) : undefined
        }
      >
        <Tabs tabs={TABS} active={tab} onChange={setTab} className="px-6" />
        {detailLoading ? (
          <div className="p-10 text-center text-sm text-gray-400">Yükleniyor...</div>
        ) : (
          detail && (
            <div className="p-6">
              {tab === "Genel Bilgiler" && (
                <div className="space-y-4">
                  <AccountPicker label="Müşteri" value={customer} onChange={setCustomer} />
                  <AccountPicker label="Gönderici" value={sender} onChange={setSender} />
                  <AccountPicker label="Alıcı" value={receiver} onChange={setReceiver} />
                  <div className="grid grid-cols-2 gap-4">
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
                    <FormField label="Brüt Ağırlık (kg)">
                      <TextInput value={form.total_gross_weight} onChange={(v) => setForm((f) => ({ ...f, total_gross_weight: v }))} />
                    </FormField>
                    <FormField label="Hacim (m³)">
                      <TextInput value={form.total_volume} onChange={(v) => setForm((f) => ({ ...f, total_volume: v }))} />
                    </FormField>
                    <FormField label="Lademetre">
                      <TextInput value={form.total_lademeter} onChange={(v) => setForm((f) => ({ ...f, total_lademeter: v }))} />
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
                      <div key={i} className="border border-gray-200 rounded-lg p-4 mb-2 relative">
                        {canDelete && (
                          <button type="button" onClick={() => removePackageRow(i)} className="absolute top-2 right-2 text-gray-300 hover:text-red-500">
                            <Trash2 size={13} />
                          </button>
                        )}
                        <div className="grid grid-cols-3 gap-3">
                          <FormField label="Ürün Tipi">
                            <SelectInput value={p.product_type_id} onChange={(v) => setPackages((list) => list.map((x, xi) => (xi === i ? { ...x, product_type_id: v } : x)))} options={opts(productTypes)} />
                          </FormField>
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
                        </div>
                      </div>
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
                            <div key={i} className="border border-gray-200 rounded-lg p-4 mb-2 relative">
                              <button type="button" onClick={() => removeInvoiceItemRow(i)} className="absolute top-2 right-2 text-gray-300 hover:text-red-500">
                                <Trash2 size={13} />
                              </button>
                              <div className="grid grid-cols-3 gap-3 mb-3">
                                <FormField label="Kalem Tipi">
                                  <SelectInput value={item.item_id} onChange={(v) => setInvoiceItems((list) => list.map((x, xi) => (xi === i ? { ...x, item_id: v } : x)))} options={opts(itemTypes)} />
                                </FormField>
                                <FormField label="Para Birimi">
                                  <SelectInput value={item.currency_code} onChange={(v) => setInvoiceItems((list) => list.map((x, xi) => (xi === i ? { ...x, currency_code: v } : x)))} options={opts(currencies)} />
                                </FormField>
                                <FormField label="Alış/Satış">
                                  <SelectInput value={item.buysell} onChange={(v) => setInvoiceItems((list) => list.map((x, xi) => (xi === i ? { ...x, buysell: v } : x)))} options={[{ value: "1", label: "Alış" }, { value: "2", label: "Satış" }]} />
                                </FormField>
                              </div>
                              <div className="mb-3">
                                <AccountPicker
                                  label="Cari"
                                  value={item.account}
                                  onChange={(v) => setInvoiceItems((list) => list.map((x, xi) => (xi === i ? { ...x, account: v } : x)))}
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
                                <FormField label="Açıklama">
                                  <TextInput value={item.description} onChange={(v) => setInvoiceItems((list) => list.map((x, xi) => (xi === i ? { ...x, description: v } : x)))} />
                                </FormField>
                              </div>
                            </div>
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
                    <button type="button" onClick={openMovementModal} className="text-[11px] text-blue-600 hover:underline flex items-center gap-1">
                      <Plus size={12} />Yeni Hareket Ekle
                    </button>
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
                              {m.created_at ? new Date(m.created_at).toLocaleString("tr-TR") : "—"} · {m.user?.name ?? "—"}
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

              {tab === "Dosya Arşivi" && (
                <div className="space-y-4">
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
    </>
  );
}
