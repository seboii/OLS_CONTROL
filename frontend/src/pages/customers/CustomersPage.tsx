import { useEffect, useMemo, useState } from "react";
import { Plus, Trash2, Users } from "lucide-react";
import { api, ApiError, type DataMessage, type Paginated } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useDebouncedValue, useLookupOptions } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { ModulePage } from "@/components/ui/ModulePage";
import { DataTable, EmptyState, Pagination, RowActions, type Column } from "@/components/ui/DataTable";
import { Drawer } from "@/components/ui/Overlay";
import { Btn, FormField, TextInput, SelectInput, Tabs } from "@/components/ui/primitives";

interface NamedRef {
  id: string;
  name: string | null;
}

interface AccountTypeMappingRow {
  id: number;
  account_type_id: { id: number; name: string } | null;
}

interface AccountListItem {
  id: number;
  name: string | null;
  phone: string | null;
  email: string | null;
  country_id: NamedRef | null;
  tax_office: { id: number; name: string | null } | null;
  account_type_mapping_id: AccountTypeMappingRow[];
}

interface AccountDetail extends AccountListItem {
  tax_number: string | null;
  address: string | null;
  contact_person: string | null;
  individual_personal: string | null;
  discount: number;
  city_id: NamedRef | null;
  district_id: NamedRef | null;
  phone_country_id: NamedRef | null;
  account_contact_person: { id: number; name: string | null; email: string | null }[];
}

const PER_PAGE = 10;

export function CustomersPage() {
  const { can } = useAuth();
  const { addToast } = useToast();
  const canCreate = can("account_management", "create");
  const canUpdate = can("account_management", "update");
  const canDelete = can("account_management", "delete");

  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);
  const [page, setPage] = useState(1);
  const [rows, setRows] = useState<AccountListItem[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);

  const [drawerOpen, setDrawerOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [detail, setDetail] = useState<AccountDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [tab, setTab] = useState("Genel Bilgiler");
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string[]>>({});

  const [form, setForm] = useState({
    name: "",
    tax_number: "",
    tax_office: "",
    country_id: "",
    city_id: "",
    district_id: "",
    address: "",
    phone: "",
    phone_country_id: "",
    email: "",
    contact_person: "",
    individual_personal: "S",
    discount: "0",
    account_type_mapping: [] as string[],
  });
  const [contactPersons, setContactPersons] = useState<{ name: string; email: string }[]>([]);

  const { options: accountTypes } = useLookupOptions("/api/v1/account_type");
  const { options: countries } = useLookupOptions("/api/v1/country");
  const cityQuery = useMemo(() => (form.country_id ? { country_id: form.country_id } : undefined), [form.country_id]);
  const { options: cities } = useLookupOptions(form.country_id ? "/api/v1/city" : null, cityQuery);
  const districtQuery = useMemo(() => (form.city_id ? { city_id: form.city_id } : undefined), [form.city_id]);
  const { options: districts } = useLookupOptions(form.city_id ? "/api/v1/district" : null, districtQuery);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    api
      .get<DataMessage<Paginated<AccountListItem>>>("/api/v1/account", {
        search: debouncedSearch || undefined,
        per_page: PER_PAGE,
        page,
      })
      .then((res) => {
        if (cancelled) return;
        setRows(res.data.data);
        setTotal(res.data.total);
      })
      .catch(() => {
        if (!cancelled) addToast("Müşteri listesi yüklenemedi", "error");
      })
      .finally(() => !cancelled && setLoading(false));
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch, page]);

  function resetForm() {
    setForm({
      name: "",
      tax_number: "",
      tax_office: "",
      country_id: "",
      city_id: "",
      district_id: "",
      address: "",
      phone: "",
      phone_country_id: "",
      email: "",
      contact_person: "",
      individual_personal: "S",
      discount: "0",
      account_type_mapping: [],
    });
    setContactPersons([]);
    setErrors({});
  }

  function openNew() {
    setEditingId(null);
    setDetail(null);
    resetForm();
    setTab("Genel Bilgiler");
    setDrawerOpen(true);
  }

  async function openEdit(id: number) {
    setEditingId(id);
    setTab("Genel Bilgiler");
    setDrawerOpen(true);
    setDetailLoading(true);
    try {
      const res = await api.get<DataMessage<AccountDetail>>(`/api/v1/account/${id}`);
      const d = res.data;
      setDetail(d);
      setForm({
        name: d.name ?? "",
        tax_number: d.tax_number ?? "",
        tax_office: d.tax_office?.name ?? "",
        country_id: d.country_id?.id ?? "",
        city_id: d.city_id?.id ?? "",
        district_id: d.district_id?.id ?? "",
        address: d.address ?? "",
        phone: d.phone ?? "",
        phone_country_id: d.phone_country_id?.id ?? "",
        email: d.email ?? "",
        contact_person: d.contact_person ?? "",
        individual_personal: d.individual_personal ?? "S",
        discount: String(d.discount ?? 0),
        account_type_mapping: d.account_type_mapping_id.map((m) => String(m.account_type_id?.id ?? "")),
      });
      setContactPersons(d.account_contact_person.map((p) => ({ name: p.name ?? "", email: p.email ?? "" })));
      setErrors({});
    } catch {
      addToast("Müşteri bilgileri yüklenemedi", "error");
      setDrawerOpen(false);
    } finally {
      setDetailLoading(false);
    }
  }

  function reload() {
    setPage((p) => p);
    // search etkisiyle aynı efekt tetiklensin diye page state'ini "değiştirip" tetikleme
    // yerine basitçe mevcut sayfayı yeniden çekiyoruz:
    api
      .get<DataMessage<Paginated<AccountListItem>>>("/api/v1/account", {
        search: debouncedSearch || undefined,
        per_page: PER_PAGE,
        page,
      })
      .then((res) => {
        setRows(res.data.data);
        setTotal(res.data.total);
      })
      .catch(() => {});
  }

  async function handleSubmit() {
    setSaving(true);
    setErrors({});
    try {
      const fd = new FormData();
      if (editingId) fd.append("id", String(editingId));
      fd.append("name", form.name);
      fd.append("tax_number", form.tax_number);
      fd.append("tax_office", form.tax_office);
      if (form.country_id) fd.append("country_id", form.country_id);
      if (form.city_id) fd.append("city_id", form.city_id);
      if (form.district_id) fd.append("district_id", form.district_id);
      fd.append("address", form.address);
      fd.append("phone", form.phone);
      if (form.phone_country_id) fd.append("phone_country_id", form.phone_country_id);
      fd.append("email", form.email);
      fd.append("contact_person", form.contact_person);
      fd.append("individual_personal", form.individual_personal);
      fd.append("discount", form.discount || "0");
      form.account_type_mapping.forEach((id) => fd.append("account_type_mapping", id));
      contactPersons.forEach((p, i) => {
        fd.append(`contact_persons[${i}][name]`, p.name);
        fd.append(`contact_persons[${i}][email]`, p.email);
      });

      if (editingId) {
        await api.postForm(`/api/v1/account/update`, fd);
        addToast("Müşteri güncellendi");
      } else {
        await api.postForm(`/api/v1/account`, fd);
        addToast("Müşteri oluşturuldu");
      }
      setDrawerOpen(false);
      reload();
    } catch (err) {
      if (err instanceof ApiError && err.errors) setErrors(err.errors);
      else addToast(err instanceof Error ? err.message : "Kaydedilemedi", "error");
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(id: number, name: string | null) {
    if (!window.confirm(`"${name ?? id}" silinsin mi?`)) return;
    try {
      await api.delete("/api/v1/account", { deletion_id: [id] });
      addToast("Müşteri silindi");
      reload();
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Silinemedi", "error");
    }
  }

  const columns: Column<AccountListItem>[] = [
    { key: "id", header: "Kod", sortable: true, width: "w-20", render: (r) => <span className="font-mono text-[11px] text-blue-600">C{r.id}</span> },
    { key: "name", header: "Müşteri Adı", sortable: true, render: (r) => <span className="font-semibold">{r.name}</span> },
    {
      key: "type",
      header: "Tip",
      render: (r) => (
        <span className="text-xs text-gray-500">
          {r.account_type_mapping_id.map((m) => m.account_type_id?.name).filter(Boolean).join(", ") || "—"}
        </span>
      ),
    },
    { key: "tax_office", header: "Vergi Dairesi", render: (r) => <span className="text-xs text-gray-500">{r.tax_office?.name ?? "—"}</span> },
    { key: "country", header: "Ülke", render: (r) => r.country_id?.name ?? "—" },
    { key: "phone", header: "Telefon", render: (r) => <span className="font-mono text-xs text-gray-500">{r.phone ?? "—"}</span> },
    { key: "email", header: "E-Posta", render: (r) => <span className="text-xs text-gray-500">{r.email ?? "—"}</span> },
  ];

  return (
    <>
      <ModulePage
        title="Müşteriler"
        search={search}
        onSearchChange={(v) => {
          setSearch(v);
          setPage(1);
        }}
        searchPlaceholder="Ad, vergi no, e-posta..."
        action={canCreate ? <Btn onClick={openNew}><Plus size={14} />Yeni Müşteri</Btn> : undefined}
      >
        <div className="bg-white">
          {!loading && rows.length === 0 ? (
            <EmptyState icon={Users} title="Kayıt bulunamadı" desc="Arama kriterlerinize uygun müşteri bulunamadı." />
          ) : (
            <>
              <DataTable
                data={rows}
                columns={columns}
                loading={loading}
                onRowClick={(r) => openEdit(r.id)}
                actions={
                  canUpdate || canDelete
                    ? (r) => (
                        <RowActions
                          onView={() => openEdit(r.id)}
                          onEdit={canUpdate ? () => openEdit(r.id) : undefined}
                          onDelete={canDelete ? () => handleDelete(r.id, r.name) : undefined}
                        />
                      )
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
        title={editingId ? (detail?.name ?? "Müşteri") : "Yeni Müşteri"}
        subtitle={editingId ? `C${editingId}` : "Yeni müşteri kaydı oluştur"}
        footer={
          (editingId ? canUpdate : canCreate) && (
            <div className="flex gap-2">
              <Btn onClick={handleSubmit} disabled={saving}>
                {saving ? "Kaydediliyor..." : "Kaydet"}
              </Btn>
              <Btn variant="secondary" onClick={() => setDrawerOpen(false)}>
                İptal
              </Btn>
            </div>
          )
        }
      >
        {detailLoading ? (
          <div className="p-10 text-center text-sm text-gray-400">Yükleniyor...</div>
        ) : (
          <>
            <Tabs tabs={["Genel Bilgiler", "Yetkililer"]} active={tab} onChange={setTab} className="px-6" />
            {tab === "Genel Bilgiler" && (
              <div className="p-6 grid grid-cols-2 gap-4">
                <FormField label="Müşteri Adı" required error={errors.name?.[0]}>
                  <TextInput value={form.name} onChange={(v) => setForm((f) => ({ ...f, name: v }))} placeholder="Şirket adı" error={!!errors.name} />
                </FormField>
                <FormField label="Müşteri/Tedarikçi Tipi">
                  <SelectInput
                    value={form.account_type_mapping[0] ?? ""}
                    onChange={(v) => setForm((f) => ({ ...f, account_type_mapping: v ? [v] : [] }))}
                    options={[{ value: "", label: "Seçiniz" }, ...accountTypes.map((t) => ({ value: String(t.id), label: t.name }))]}
                  />
                </FormField>
                <FormField label="Vergi No" error={errors.tax_number?.[0]}>
                  <TextInput value={form.tax_number} onChange={(v) => setForm((f) => ({ ...f, tax_number: v }))} placeholder="1234567890" error={!!errors.tax_number} />
                </FormField>
                <FormField label="Vergi Dairesi">
                  <TextInput value={form.tax_office} onChange={(v) => setForm((f) => ({ ...f, tax_office: v }))} placeholder="Büyük Mükellefler" />
                </FormField>
                <FormField label="Ülke">
                  <SelectInput
                    value={form.country_id}
                    onChange={(v) => setForm((f) => ({ ...f, country_id: v, city_id: "", district_id: "" }))}
                    options={[{ value: "", label: "Seçiniz" }, ...countries.map((c) => ({ value: String(c.id), label: c.name }))]}
                  />
                </FormField>
                <FormField label="Şehir">
                  <SelectInput
                    value={form.city_id}
                    onChange={(v) => setForm((f) => ({ ...f, city_id: v, district_id: "" }))}
                    disabled={!form.country_id}
                    options={[{ value: "", label: "Seçiniz" }, ...cities.map((c) => ({ value: String(c.id), label: c.name }))]}
                  />
                </FormField>
                <FormField label="İlçe">
                  <SelectInput
                    value={form.district_id}
                    onChange={(v) => setForm((f) => ({ ...f, district_id: v }))}
                    disabled={!form.city_id}
                    options={[{ value: "", label: "Seçiniz" }, ...districts.map((d) => ({ value: String(d.id), label: d.name }))]}
                  />
                </FormField>
                <FormField label="Firma Tipi">
                  <SelectInput
                    value={form.individual_personal}
                    onChange={(v) => setForm((f) => ({ ...f, individual_personal: v }))}
                    options={[
                      { value: "T", label: "Tüzel" },
                      { value: "S", label: "Şahıs" },
                    ]}
                  />
                </FormField>
                <FormField label="Telefon" error={errors.phone?.[0]}>
                  <TextInput value={form.phone} onChange={(v) => setForm((f) => ({ ...f, phone: v }))} placeholder="+90 212 555 0000" error={!!errors.phone} />
                </FormField>
                <FormField label="E-posta" error={errors.email?.[0]}>
                  <TextInput value={form.email} onChange={(v) => setForm((f) => ({ ...f, email: v }))} type="email" placeholder="info@sirket.com" error={!!errors.email} />
                </FormField>
                <FormField label="Yetkili Kişi">
                  <TextInput value={form.contact_person} onChange={(v) => setForm((f) => ({ ...f, contact_person: v }))} />
                </FormField>
                <FormField label="İskonto (%)">
                  <TextInput value={form.discount} onChange={(v) => setForm((f) => ({ ...f, discount: v }))} type="number" />
                </FormField>
                <div className="col-span-2">
                  <FormField label="Adres">
                    <TextInput value={form.address} onChange={(v) => setForm((f) => ({ ...f, address: v }))} placeholder="Açık adres" />
                  </FormField>
                </div>
              </div>
            )}
            {tab === "Yetkililer" && (
              <div className="p-6 space-y-3">
                {contactPersons.map((p, i) => (
                  <div key={i} className="border border-gray-200 rounded-lg p-4 flex items-start gap-3">
                    <div className="flex-1 grid grid-cols-2 gap-3">
                      <TextInput
                        value={p.name}
                        onChange={(v) => setContactPersons((list) => list.map((x, xi) => (xi === i ? { ...x, name: v } : x)))}
                        placeholder="Ad Soyad"
                      />
                      <TextInput
                        value={p.email}
                        onChange={(v) => setContactPersons((list) => list.map((x, xi) => (xi === i ? { ...x, email: v } : x)))}
                        placeholder="E-posta"
                        type="email"
                      />
                    </div>
                    <button
                      onClick={() => setContactPersons((list) => list.filter((_, xi) => xi !== i))}
                      className="p-2 text-gray-300 hover:text-red-500"
                    >
                      <Trash2 size={14} />
                    </button>
                  </div>
                ))}
                <Btn variant="secondary" size="sm" onClick={() => setContactPersons((list) => [...list, { name: "", email: "" }])}>
                  <Plus size={12} />
                  Yetkili Ekle
                </Btn>
              </div>
            )}
          </>
        )}
      </Drawer>
    </>
  );
}
