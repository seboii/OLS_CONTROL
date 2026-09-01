import { useEffect, useMemo, useState } from "react";
import { motion, AnimatePresence } from "motion/react";
import { clsx } from "clsx";
import { Plus, Trash2, Users, Upload, Filter, ChevronDown, X, Phone, Mail } from "lucide-react";
import { api, ApiError, type DataMessage, type Paginated } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useDebouncedValue, useLookupOptions } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { ModulePage } from "@/components/ui/ModulePage";
import { EmptyState, Pagination } from "@/components/ui/DataTable";
import { Drawer } from "@/components/ui/Overlay";
import { Btn, FormField, TextInput, SelectInput, Tabs } from "@/components/ui/primitives";
import { UserPicker, type UserOption } from "@/components/shared/UserPicker";
import { RecordHistoryTab } from "@/components/shared/RecordHistory";

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
  avatar: string | null;
  country_id: NamedRef | null;
  phone_country_id: { id: string; name: string | null; phone_code: string | null } | null;
  tax_office: { id: number; name: string | null } | null;
  account_type_mapping_id: AccountTypeMappingRow[];
}

interface AccountInvoiceRow {
  id: number;
  invoice_id: string | null;
  box_type: 0 | 1;
  commercial_type: number;
  target_title: string | null;
  target_identity_no: string | null;
  payable_amount: number | null;
  tax_exclusive_amount: number | null;
  tax_amount: number | null;
  tax_rate: number | null;
  document_currency_code: string | null;
  invoice_type: { id: number; name: string } | null;
  invoice_status: { id: number; name: string } | null;
}

interface AccountDetail extends AccountListItem {
  tax_number: string | null;
  accounting_code: string | null;
  address: string | null;
  contact_person: string | null;
  individual_personal: string | null;
  discount: number;
  city_id: NamedRef | null;
  district_id: NamedRef | null;
  contact_language: NamedRef | null;
  account_contact_person: { id: number; name: string | null; email: string | null }[];
  user_account_mapping: { id: number; user_id: UserOption | null }[];
  invoice: AccountInvoiceRow[];
}

const PER_PAGE = 24;

function AccountCard({
  row, index, onClick, canDelete, onDelete,
}: {
  row: AccountListItem; index: number; onClick: () => void; canDelete: boolean; onDelete: () => void;
}) {
  const types = row.account_type_mapping_id.filter((m) => m.account_type_id?.name);

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
          <div className="bg-gray-100 w-9 h-9 rounded-lg overflow-hidden shrink-0 border border-gray-200 flex items-center justify-center">
            {row.avatar ? (
              <img src={`/storage/${row.avatar}`} alt="" className="w-full h-full object-cover" />
            ) : (
              <span className="text-sm text-gray-400">{row.name?.charAt(0).toUpperCase()}</span>
            )}
          </div>
          <div className="min-w-0">
            <p className="text-sm font-semibold text-gray-900 truncate">{row.name}</p>
            {row.country_id?.name && <p className="text-[10px] text-gray-400 mt-0.5 truncate">{row.country_id.name}</p>}
          </div>
        </div>
        {canDelete && (
          <button
            type="button"
            onClick={(e) => { e.stopPropagation(); onDelete(); }}
            className="p-1 rounded text-gray-300 hover:text-red-500 hover:bg-red-50 transition-colors shrink-0"
          >
            <Trash2 size={13} />
          </button>
        )}
      </div>

      <div className="flex flex-wrap gap-1 pt-3 border-t border-gray-100">
        {types.length === 0 ? (
          <span className="text-[11px] text-gray-400">Hesap türü atanmadı</span>
        ) : (
          types.map((m) => (
            <span key={m.id} className="text-[10px] font-medium px-2 py-0.5 rounded-full bg-blue-50 text-blue-700">
              {m.account_type_id!.name}
            </span>
          ))
        )}
      </div>

      <div className="pt-2.5 border-t border-gray-100 space-y-1.5">
        <div className="flex items-center gap-1.5 text-[11px] text-gray-500 min-w-0">
          <Phone size={12} className="text-gray-400 shrink-0" />
          <span className="truncate">
            {row.phone ? `${row.phone_country_id?.phone_code ? `+${row.phone_country_id.phone_code} ` : ""}${row.phone}` : "—"}
          </span>
        </div>
        <div className="flex items-center gap-1.5 text-[11px] text-gray-500 min-w-0">
          <Mail size={12} className="text-gray-400 shrink-0" />
          <span className="truncate">{row.email || "—"}</span>
        </div>
      </div>
    </motion.div>
  );
}

// olsold: AccountFormDrawer.vue TabList — "Genel Bilgiler"/"İletişim Bilgileri"/
// "Görevli"/"Faturalar" (son ikisi bu güncellemede eklendi; önceden hiç yoktu).
const TABS = ["Genel Bilgiler", "İletişim Bilgileri", "Görevli", "Faturalar", "İşlem Geçmişi"];
// olsold: pages/accounts/index.vue — üst seviye 6 sekme, aynı listeyi account_type_id
// ile filtreliyor. ID'ler AccountTypeSeeder ile birebir (Müşteri=1 ... Acente=5).
const TYPE_TABS: { label: string; typeId: number | null }[] = [
  { label: "Tümü", typeId: null },
  { label: "Müşteriler", typeId: 1 },
  { label: "Tedarikçiler", typeId: 2 },
  { label: "Alıcılar", typeId: 3 },
  { label: "Göndericiler", typeId: 4 },
  { label: "Acenteler", typeId: 5 },
];
const INVOICE_COMMERCIAL_TYPE_LABELS: Record<number, string> = { 0: "Temel Fatura", 1: "Ticari Fatura", 4: "E-Arşiv" };
const invoiceMoney = (value: number | null) =>
  (value ?? 0).toLocaleString("tr-TR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

export function CustomersPage() {
  const { can } = useAuth();
  const { addToast } = useToast();
  const canCreate = can("account_management", "create");
  const canUpdate = can("account_management", "update");
  const canDelete = can("account_management", "delete");

  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);
  const [page, setPage] = useState(1);
  const [listTab, setListTab] = useState(TYPE_TABS[0].label);
  const activeTypeId = TYPE_TABS.find((t) => t.label === listTab)?.typeId ?? null;
  const [rows, setRows] = useState<AccountListItem[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);

  const [fCountryId, setFCountryId] = useState("");
  const [fTaxOfficeId, setFTaxOfficeId] = useState("");
  const [fAssignedUser, setFAssignedUser] = useState<UserOption | null>(null);
  const [fIndividualPersonal, setFIndividualPersonal] = useState("");
  const [showAdvanced, setShowAdvanced] = useState(false);
  const hasActiveAdvancedFilters = !!(fCountryId || fTaxOfficeId || fAssignedUser || fIndividualPersonal);
  const hasActiveFilters = !!(search || hasActiveAdvancedFilters);

  function clearFilters() {
    setSearch("");
    setFCountryId("");
    setFTaxOfficeId("");
    setFAssignedUser(null);
    setFIndividualPersonal("");
    setPage(1);
  }

  const [drawerOpen, setDrawerOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [detail, setDetail] = useState<AccountDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [tab, setTab] = useState(TABS[0]);
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string[]>>({});

  const [form, setForm] = useState({
    name: "",
    tax_number: "",
    tax_office_id: "",
    country_id: "",
    city_id: "",
    district_id: "",
    address: "",
    phone: "",
    phone_country_id: "",
    email: "",
    contact_language_id: "",
    individual_personal: "T",
    discount: "0",
    account_type_mapping: [] as string[],
  });
  const [contactPersons, setContactPersons] = useState<{ name: string; email: string }[]>([]);
  const [chargePersons, setChargePersons] = useState<UserOption[]>([]);
  const [avatarFile, setAvatarFile] = useState<File | null>(null);
  const [avatarPreview, setAvatarPreview] = useState<string | null>(null);
  const [removeAvatar, setRemoveAvatar] = useState(false);

  const { options: accountTypes } = useLookupOptions("/api/v1/account_type");
  const { options: countries } = useLookupOptions("/api/v1/country");
  const { options: taxOffices } = useLookupOptions("/api/v1/tax_office");
  const cityQuery = useMemo(() => (form.country_id ? { country_id: form.country_id } : undefined), [form.country_id]);
  const { options: cities } = useLookupOptions(form.country_id ? "/api/v1/city" : null, cityQuery);
  const districtQuery = useMemo(() => (form.city_id ? { city_id: form.city_id } : undefined), [form.city_id]);
  const { options: districts } = useLookupOptions(form.city_id ? "/api/v1/district" : null, districtQuery);

  useEffect(() => {
    if (!avatarFile) {
      setAvatarPreview(null);
      return;
    }
    const url = URL.createObjectURL(avatarFile);
    setAvatarPreview(url);
    return () => URL.revokeObjectURL(url);
  }, [avatarFile]);

  function load() {
    setLoading(true);
    api
      .get<DataMessage<Paginated<AccountListItem>>>("/api/v1/account", {
        search: debouncedSearch || undefined,
        account_type_id: activeTypeId ?? undefined,
        country_id: fCountryId || undefined,
        tax_office_id: fTaxOfficeId || undefined,
        assigned_user_id: fAssignedUser?.id || undefined,
        individual_personal: fIndividualPersonal || undefined,
        per_page: PER_PAGE,
        page,
      })
      .then((res) => {
        setRows(res.data.data);
        setTotal(res.data.total);
      })
      .catch(() => addToast("Müşteri listesi yüklenemedi", "error"))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch, page, listTab, fCountryId, fTaxOfficeId, fAssignedUser, fIndividualPersonal]);

  function resetForm() {
    setForm({
      name: "",
      tax_number: "",
      tax_office_id: "",
      country_id: "",
      city_id: "",
      district_id: "",
      address: "",
      phone: "",
      phone_country_id: "",
      email: "",
      contact_language_id: "",
      individual_personal: "T",
      discount: "0",
      account_type_mapping: [],
    });
    setContactPersons([]);
    setChargePersons([]);
    setAvatarFile(null);
    setRemoveAvatar(false);
    setErrors({});
  }

  function openNew() {
    setEditingId(null);
    setDetail(null);
    resetForm();
    setTab(TABS[0]);
    setDrawerOpen(true);
  }

  async function openEdit(id: number) {
    setEditingId(id);
    setTab(TABS[0]);
    setDrawerOpen(true);
    setDetailLoading(true);
    try {
      const res = await api.get<DataMessage<AccountDetail>>(`/api/v1/account/${id}`);
      const d = res.data;
      setDetail(d);
      setForm({
        name: d.name ?? "",
        tax_number: d.tax_number ?? "",
        tax_office_id: d.tax_office?.id ? String(d.tax_office.id) : "",
        country_id: d.country_id?.id ?? "",
        city_id: d.city_id?.id ?? "",
        district_id: d.district_id?.id ?? "",
        address: d.address ?? "",
        phone: d.phone ?? "",
        phone_country_id: d.phone_country_id?.id ?? "",
        email: d.email ?? "",
        contact_language_id: d.contact_language?.id ?? "",
        individual_personal: d.individual_personal ?? "S",
        discount: String(d.discount ?? 0),
        account_type_mapping: d.account_type_mapping_id.map((m) => String(m.account_type_id?.id ?? "")).filter(Boolean),
      });
      setContactPersons(d.account_contact_person.map((p) => ({ name: p.name ?? "", email: p.email ?? "" })));
      setChargePersons(d.user_account_mapping.map((m) => m.user_id).filter((u): u is UserOption => !!u));
      setAvatarFile(null);
      setRemoveAvatar(false);
      setErrors({});
    } catch {
      addToast("Müşteri bilgileri yüklenemedi", "error");
      setDrawerOpen(false);
    } finally {
      setDetailLoading(false);
    }
  }

  function reload() {
    api
      .get<DataMessage<Paginated<AccountListItem>>>("/api/v1/account", {
        search: debouncedSearch || undefined,
        account_type_id: activeTypeId ?? undefined,
        country_id: fCountryId || undefined,
        tax_office_id: fTaxOfficeId || undefined,
        assigned_user_id: fAssignedUser?.id || undefined,
        individual_personal: fIndividualPersonal || undefined,
        per_page: PER_PAGE,
        page,
      })
      .then((res) => {
        setRows(res.data.data);
        setTotal(res.data.total);
      })
      .catch(() => {});
  }

  function toggleAccountType(id: string) {
    setForm((f) => ({
      ...f,
      account_type_mapping: f.account_type_mapping.includes(id)
        ? f.account_type_mapping.filter((t) => t !== id)
        : [...f.account_type_mapping, id],
    }));
    setErrors((e) => {
      if (!e.account_type_mapping) return e;
      const { account_type_mapping: _drop, ...rest } = e;
      return rest;
    });
  }

  async function handleSubmit() {
    // olsold: AccountFormDrawer.vue Vuelidate — name/account_type/country istemci
    // tarafında zorunlu (backend yalnızca name/country_id/discount'u doğruluyor).
    if (form.account_type_mapping.length === 0) {
      setErrors({ account_type_mapping: ["Hesap türü zorunludur."] });
      setTab(TABS[0]);
      return;
    }
    // Buton disabled={saving} render'a kadar DOM'a yansımıyor — hızlı çift
    // tıklama/tekrar tetiklemeye karşı erken çıkış.
    if (saving) return;

    setSaving(true);
    setErrors({});
    try {
      const fd = new FormData();
      if (editingId) fd.append("id", String(editingId));
      fd.append("name", form.name);
      fd.append("tax_number", form.tax_number);
      fd.append("tax_office", form.tax_office_id);
      if (form.country_id) fd.append("country_id", form.country_id);
      if (form.city_id) fd.append("city_id", form.city_id);
      if (form.district_id) fd.append("district_id", form.district_id);
      fd.append("address", form.address);
      fd.append("phone", form.phone);
      if (form.phone_country_id) fd.append("phone_country_id", form.phone_country_id);
      fd.append("email", form.email);
      fd.append("discount", form.discount || "0");
      fd.append("individual_personal", form.individual_personal);
      if (form.contact_language_id) fd.append("contact_language", form.contact_language_id);
      form.account_type_mapping.forEach((id) => fd.append("account_type_mapping", id));
      chargePersons.forEach((u) => fd.append("account_charge_person", String(u.id)));
      contactPersons.forEach((p, i) => {
        fd.append(`contact_persons[${i}][name]`, p.name);
        fd.append(`contact_persons[${i}][email]`, p.email);
      });
      if (avatarFile) fd.append("avatar", avatarFile);
      else if (removeAvatar) fd.append("avatar_remove", "1");

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

  return (
    <>
      <ModulePage
        title="Müşteriler"
        action={canCreate ? <Btn onClick={openNew}><Plus size={14} />Yeni Müşteri</Btn> : undefined}
      >
        <div className="bg-white border-b border-gray-200 px-6 py-4">
          <div className="flex items-center gap-2.5">
            <div className="flex-1 max-w-md">
              <TextInput value={search} onChange={(v) => { setSearch(v); setPage(1); }} placeholder="Genel arama: ad, telefon, e-posta, ülke..." />
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
                  <FormField label="Ülke">
                    <SelectInput value={fCountryId} onChange={(v) => { setFCountryId(v); setPage(1); }} options={[{ value: "", label: "Seçiniz" }, ...countries.map((c) => ({ value: String(c.id), label: c.name }))]} />
                  </FormField>
                  <FormField label="Vergi Dairesi">
                    <SelectInput value={fTaxOfficeId} onChange={(v) => { setFTaxOfficeId(v); setPage(1); }} options={[{ value: "", label: "Seçiniz" }, ...taxOffices.map((t) => ({ value: String(t.id), label: t.name }))]} />
                  </FormField>
                  <FormField label="Kurumsal/Şahıs">
                    <SelectInput value={fIndividualPersonal} onChange={(v) => { setFIndividualPersonal(v); setPage(1); }} options={[{ value: "", label: "Seçiniz" }, { value: "T", label: "Tüzel" }, { value: "S", label: "Şahıs" }]} />
                  </FormField>
                  <UserPicker label="Görevli" value={fAssignedUser} onChange={(v) => { setFAssignedUser(v); setPage(1); }} />
                </div>
              </motion.div>
            )}
          </AnimatePresence>
        </div>
        <Tabs
          tabs={TYPE_TABS.map((t) => t.label)}
          active={listTab}
          onChange={(v) => {
            setListTab(v);
            setPage(1);
          }}
          className="mb-4 px-6 pt-4"
        />
        <div className="bg-gray-50/70 min-h-full">
          {!loading && rows.length === 0 ? (
            <EmptyState icon={Users} title="Kayıt bulunamadı" desc="Arama kriterlerinize uygun müşteri bulunamadı." />
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
                      <AccountCard
                        key={r.id}
                        row={r}
                        index={i}
                        onClick={() => openEdit(r.id)}
                        canDelete={canDelete}
                        onDelete={() => handleDelete(r.id, r.name)}
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
        title={editingId ? (detail?.name ?? "Müşteri") : "Yeni Müşteri"}
        subtitle={editingId ? `C${editingId}` : "Yeni müşteri kaydı oluştur"}
        footer={
          (editingId ? canUpdate : canCreate) && (
            <div className="flex gap-2">
              <Btn onClick={handleSubmit} disabled={saving}>
                {saving ? "Kaydediliyor..." : editingId ? "Güncelle" : "Oluştur"}
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
            <Tabs tabs={editingId ? TABS : TABS.slice(0, 3)} active={tab} onChange={setTab} className="px-6" />

            {tab === "Genel Bilgiler" && (
              <div className="p-6 space-y-4">
                <FormField label="Profil Fotoğrafı">
                  <div className="flex items-center gap-3">
                    {avatarPreview ? (
                      <img src={avatarPreview} alt="" className="w-14 h-14 rounded-full object-cover" />
                    ) : !removeAvatar && detail?.avatar ? (
                      <img src={`/storage/${detail.avatar}`} alt="" className="w-14 h-14 rounded-full object-cover" />
                    ) : (
                      <div className="w-14 h-14 rounded-full bg-gray-100 flex items-center justify-center text-gray-300">
                        <Users size={22} />
                      </div>
                    )}
                    <div className="flex gap-2">
                      <label className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-gray-200 text-xs text-gray-600 cursor-pointer hover:border-blue-300 hover:text-blue-600 transition-colors">
                        <Upload size={13} />Değiştir
                        <input
                          type="file"
                          accept="image/png,image/jpeg,image/jpg,image/gif,image/webp,image/svg+xml"
                          className="hidden"
                          onChange={(e) => { const f = e.target.files?.[0]; if (f) { setAvatarFile(f); setRemoveAvatar(false); } }}
                        />
                      </label>
                      {(avatarPreview || (!removeAvatar && detail?.avatar)) && (
                        <button
                          type="button"
                          onClick={() => { setAvatarFile(null); setRemoveAvatar(true); }}
                          className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-gray-200 text-xs text-gray-400 hover:border-red-200 hover:text-red-500 transition-colors"
                        >
                          <Trash2 size={13} />Kaldır
                        </button>
                      )}
                    </div>
                  </div>
                </FormField>
                <div className="grid grid-cols-2 gap-4">
                  <div className="col-span-2">
                    <FormField label="Hesap Adı" required error={errors.name?.[0]}>
                      <TextInput value={form.name} onChange={(v) => setForm((f) => ({ ...f, name: v }))} placeholder="Şirket adı" error={!!errors.name} />
                    </FormField>
                  </div>
                  <div className="col-span-2">
                    <label className="block text-xs font-medium text-gray-500 mb-1.5">Hesap Türü{errors.account_type_mapping && <span className="text-red-500"> *</span>}</label>
                    <div className="flex flex-wrap gap-1.5">
                      {accountTypes.map((t) => {
                        const id = String(t.id);
                        const active = form.account_type_mapping.includes(id);
                        return (
                          <button
                            key={id}
                            type="button"
                            onClick={() => toggleAccountType(id)}
                            className={`px-3 py-1.5 rounded-full text-xs border transition-colors ${
                              active ? "bg-blue-600 border-blue-600 text-white" : "border-gray-200 text-gray-600 hover:border-blue-300"
                            }`}
                          >
                            {t.name}
                          </button>
                        );
                      })}
                    </div>
                    {errors.account_type_mapping?.[0] && <p className="mt-1 text-xs text-red-500">{errors.account_type_mapping[0]}</p>}
                  </div>
                  <FormField label="Ülke" required error={errors.country_id?.[0]}>
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
                  {editingId && (
                    <FormField label="Muhasebe Kodu">
                      <TextInput value={detail?.accounting_code ?? ""} onChange={() => {}} disabled placeholder="—" />
                    </FormField>
                  )}
                  <FormField label="Vergi No" error={errors.tax_number?.[0]}>
                    <TextInput value={form.tax_number} onChange={(v) => setForm((f) => ({ ...f, tax_number: v }))} placeholder="1234567890" error={!!errors.tax_number} />
                  </FormField>
                  <FormField label="Vergi Dairesi">
                    <SelectInput
                      value={form.tax_office_id}
                      onChange={(v) => setForm((f) => ({ ...f, tax_office_id: v }))}
                      options={[{ value: "", label: "Seçiniz" }, ...taxOffices.map((t) => ({ value: String(t.id), label: t.name }))]}
                    />
                  </FormField>
                  <FormField label="İndirim Tutarı" required error={errors.discount?.[0]}>
                    <TextInput value={form.discount} onChange={(v) => setForm((f) => ({ ...f, discount: v }))} type="number" error={!!errors.discount} />
                  </FormField>
                  <div className="col-span-2">
                    <SelectInput
                      value={form.individual_personal}
                      onChange={(v) => setForm((f) => ({ ...f, individual_personal: v }))}
                      options={[
                        { value: "T", label: "Tüzel" },
                        { value: "S", label: "Şahıs" },
                      ]}
                    />
                  </div>
                </div>
              </div>
            )}

            {tab === "İletişim Bilgileri" && (
              <div className="p-6 space-y-4">
                <FormField label="E-posta" error={errors.email?.[0]}>
                  <TextInput value={form.email} onChange={(v) => setForm((f) => ({ ...f, email: v }))} type="email" placeholder="info@sirket.com" error={!!errors.email} />
                </FormField>
                <div className="grid grid-cols-2 gap-4">
                  <FormField label="Ülke Kodu">
                    <SelectInput
                      value={form.phone_country_id}
                      onChange={(v) => setForm((f) => ({ ...f, phone_country_id: v }))}
                      options={[{ value: "", label: "Seçiniz" }, ...countries.map((c) => ({ value: String(c.id), label: c.name }))]}
                    />
                  </FormField>
                  <FormField label="Telefon Numarası" error={errors.phone?.[0]}>
                    <TextInput value={form.phone} onChange={(v) => setForm((f) => ({ ...f, phone: v }))} placeholder="212 555 0000" error={!!errors.phone} />
                  </FormField>
                </div>
                <FormField label="İletişim Dili">
                  <SelectInput
                    value={form.contact_language_id}
                    onChange={(v) => setForm((f) => ({ ...f, contact_language_id: v }))}
                    options={[{ value: "", label: "Seçiniz" }, ...countries.map((c) => ({ value: String(c.id), label: c.name }))]}
                  />
                </FormField>
                <FormField label="Adres">
                  <TextInput value={form.address} onChange={(v) => setForm((f) => ({ ...f, address: v }))} placeholder="Açık adres" />
                </FormField>

                <div className="pt-2">
                  <div className="flex items-center justify-between mb-2">
                    <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider">İlgili Kişiler ({contactPersons.length})</p>
                    <button type="button" onClick={() => setContactPersons((list) => [...list, { name: "", email: "" }])} className="text-[11px] text-blue-600 hover:underline flex items-center gap-1">
                      <Plus size={12} />Ekle
                    </button>
                  </div>
                  <div className="space-y-2">
                    {contactPersons.map((p, i) => (
                      <div key={i} className="border border-gray-200 rounded-lg p-3 flex items-start gap-3">
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
                  </div>
                </div>
              </div>
            )}

            {tab === "Görevli" && (
              <div className="p-6">
                <div className="flex items-center justify-between mb-2">
                  <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider">Satış Temsilcisi</p>
                  <button type="button" onClick={() => setChargePersons((list) => [...list, { id: 0, name: null, surname: null }])} className="text-[11px] text-blue-600 hover:underline flex items-center gap-1">
                    <Plus size={12} />Ekle
                  </button>
                </div>
                {chargePersons.length === 0 ? (
                  <p className="text-xs text-gray-400 text-center py-8">Henüz satış temsilcisi eklenmedi.</p>
                ) : (
                  chargePersons.map((rep, i) => (
                    <div key={i} className="flex items-start gap-2 mb-2">
                      <div className="flex-1">
                        <UserPicker
                          label={`Satış Temsilcisi ${i + 1}`}
                          value={rep.id ? rep : null}
                          onChange={(v) => setChargePersons((list) => list.map((x, xi) => (xi === i ? (v ?? { id: 0, name: null, surname: null }) : x)))}
                        />
                      </div>
                      <button type="button" onClick={() => setChargePersons((list) => list.filter((_, xi) => xi !== i))} className="mt-6 text-gray-300 hover:text-red-500">
                        <Trash2 size={14} />
                      </button>
                    </div>
                  ))
                )}
              </div>
            )}

            {tab === "İşlem Geçmişi" && (

              <RecordHistoryTab resource="account" recordId={editingId} />

            )}


            {tab === "Faturalar" && (
              <div className="p-6">
                <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider mb-2">Faturalar</p>
                {(detail?.invoice.length ?? 0) === 0 ? (
                  <p className="text-xs text-gray-400 text-center py-8">Bu cariye bağlı fatura bulunamadı.</p>
                ) : (
                  detail!.invoice.map((inv) => (
                    <div key={inv.id} className="border border-gray-200 rounded-lg p-4 mb-2">
                      <div className="grid grid-cols-2 gap-3">
                        <div className="bg-gray-50 rounded-lg p-3">
                          <p className="text-[11px] text-gray-500">Fatura No</p>
                          <p className="text-sm font-medium">{inv.invoice_id ?? "—"}</p>
                        </div>
                        <div className="bg-gray-50 rounded-lg p-3">
                          <p className="text-[11px] text-gray-500">Gelen/Giden</p>
                          <p className="text-sm font-medium">{inv.box_type === 1 ? "Giden Fatura" : "Gelen Fatura"}</p>
                        </div>
                        <div className="bg-gray-50 rounded-lg p-3">
                          <p className="text-[11px] text-gray-500">Fatura Durumu</p>
                          <p className="text-sm font-medium">{inv.invoice_status?.name ?? "—"}</p>
                        </div>
                        <div className="bg-gray-50 rounded-lg p-3">
                          <p className="text-[11px] text-gray-500">Fatura Tipi</p>
                          <p className="text-sm font-medium">{inv.invoice_type?.name ?? "—"}</p>
                        </div>
                        <div className="bg-gray-50 rounded-lg p-3 col-span-2">
                          <p className="text-[11px] text-gray-500">Fatura Ticareti Tipi</p>
                          <p className="text-sm font-medium">{INVOICE_COMMERCIAL_TYPE_LABELS[inv.commercial_type] ?? "—"}</p>
                        </div>
                        <div className="bg-gray-50 rounded-lg p-3">
                          <p className="text-[11px] text-gray-500">Tutar</p>
                          <p className="text-sm font-medium">{invoiceMoney(inv.payable_amount)} {inv.document_currency_code ?? ""}</p>
                        </div>
                        <div className="bg-gray-50 rounded-lg p-3">
                          <p className="text-[11px] text-gray-500">KDV Hariç</p>
                          <p className="text-sm font-medium">{invoiceMoney(inv.tax_exclusive_amount)} {inv.document_currency_code ?? ""}</p>
                        </div>
                        <div className="bg-gray-50 rounded-lg p-3 col-span-2">
                          <p className="text-[11px] text-gray-500">KDV</p>
                          <p className="text-sm font-medium">{invoiceMoney(inv.tax_amount)} ({inv.tax_rate ?? 0}%) {inv.document_currency_code ?? ""}</p>
                        </div>
                      </div>
                    </div>
                  ))
                )}
              </div>
            )}
          </>
        )}
      </Drawer>
    </>
  );
}
