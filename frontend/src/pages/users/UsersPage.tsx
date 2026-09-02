import { useEffect, useState } from "react";
import { motion, AnimatePresence } from "motion/react";
import { clsx } from "clsx";
import { Plus, Shield, Upload, Trash2, Target, Calendar, Pencil, Filter, ChevronDown, X, Phone, Mail } from "lucide-react";
import { api, ApiError, type DataMessage, type Paginated } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useDebouncedValue, useLookupOptions } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { ModulePage } from "@/components/ui/ModulePage";
import { EmptyState, Pagination } from "@/components/ui/DataTable";
import { Drawer, Modal } from "@/components/ui/Overlay";
import { Btn, FormField, SelectInput, Tabs, TextInput } from "@/components/ui/primitives";

interface NamedRef {
  id: string;
  name: string | null;
}

interface UserItem {
  id: number;
  name: string | null;
  surname: string | null;
  email: string | null;
  phone: string | null;
  avatar: string | null;
}

interface UserDetail extends UserItem {
  pkds_id: string | null;
  phone_country_id: NamedRef | null;
  /** Uygulanmış yetki şablonu (bkz. Role). */
  role_id: number | null;
  /** Görme kapsamı: hangi Siber şirketinin yük/seferlerini görür. */
  siber_company_id: string | null;
  /** Siber'deki departman — rolün neden bu olduğunu açıklamak için gösterilir. */
  siber_department_name: string | null;
  siber_blocked: boolean | null;
}

/** sbr_sirket: AVRORA ULUSLARARASI TASIMACILIK LIMITED SIRKETI (bkz. CompanyScope). */
const AVRORA_COMPANY_ID = "46258A01-8D77-4F87-AAF5-6B331DEDD8A7";

interface RoleOption {
  id: number;
  name: string;
  slug: string;
  description: string | null;
  user_count: number;
}

interface UserGoalItem {
  id: number;
  user_id: number | null;
  start_date: string | null;
  end_date: string | null;
  goal_price: number;
}

interface PermissionRow {
  id: number;
  read: 0 | 1;
  create: 0 | 1;
  update: 0 | 1;
  delete: 0 | 1;
  permission_page_name: string;
  permission_page_slug: string;
}

const PER_PAGE = 24;
const PERM_LABELS: Record<"read" | "create" | "update" | "delete", string> = {
  read: "Görüntüle",
  create: "Oluştur",
  update: "Düzenle",
  delete: "Sil",
};

function initials(name: string | null, surname: string | null) {
  return `${(name ?? "?").charAt(0)}${(surname ?? "").charAt(0)}`.toUpperCase();
}

function UserCard({
  row, index, onClick, canDelete, onDelete,
}: {
  row: UserItem; index: number; onClick: () => void; canDelete: boolean; onDelete: () => void;
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
          {row.avatar ? (
            <img src={`/storage/${row.avatar}`} alt="" className="w-9 h-9 rounded-lg object-cover border border-gray-200 shrink-0" />
          ) : (
            <div className="w-9 h-9 rounded-lg bg-blue-100 text-blue-700 flex items-center justify-center text-[11px] font-bold border border-gray-200 shrink-0">
              {initials(row.name, row.surname)}
            </div>
          )}
          <p className="text-sm font-semibold text-gray-900 truncate min-w-0">{row.name} {row.surname}</p>
        </div>
        <div className="flex items-center gap-1 shrink-0">
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
    </motion.div>
  );
}

export function UsersPage() {
  const { user: me, can } = useAuth();
  const { addToast } = useToast();
  const canCreate = can("user_management", "create");
  const canUpdate = can("user_management", "update");
  const canDelete = can("user_management", "delete");
  const canManageRoles = can("role_management", "update");

  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);
  const [page, setPage] = useState(1);
  const [rows, setRows] = useState<UserItem[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);

  const [fPhoneCountry, setFPhoneCountry] = useState("");
  const [fWorkingTracking, setFWorkingTracking] = useState("");
  const [showAdvanced, setShowAdvanced] = useState(false);
  const hasActiveAdvancedFilters = !!(fPhoneCountry || fWorkingTracking);
  const hasActiveFilters = !!(search || hasActiveAdvancedFilters);

  function clearFilters() {
    setSearch("");
    setFPhoneCountry("");
    setFWorkingTracking("");
    setPage(1);
  }

  const [drawerOpen, setDrawerOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [tab, setTab] = useState("Profil");
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  const [form, setForm] = useState({ name: "", surname: "", email: "", phone: "", password: "", password_confirmation: "", phone_country_id: "", pkds_id: "" });
  const [permRows, setPermRows] = useState<PermissionRow[]>([]);

  // ROL KATMANI: yetki tablosu tek tek kutu işaretlemeye izin veriyor, ama 25
  // sayfayı elle işaretlemek pratik değil. Rol seçildiğinde o rolün şablonu
  // sunucuda kullanıcıya uygulanır (user_permissions satırlarına yazılır) ve
  // tablo yeniden okunur. Rol uygulandıktan sonra tek tek değiştirmek serbest —
  // rol bir başlangıç noktası, kilit değil.
  const [roles, setRoles] = useState<RoleOption[]>([]);
  const [detailRoleId, setDetailRoleId] = useState<number | null>(null);
  const [detailDepartment, setDetailDepartment] = useState<string | null>(null);
  const [applyingRole, setApplyingRole] = useState(false);

  // ŞİRKET KAPSAMI — roldan ayrı: rol "ne yapabilir", kapsam "ne görebilir".
  // Boş = Avrora hariç her şey; Avrora = yalnızca Avrora kayıtları.
  const [detailCompany, setDetailCompany] = useState<string>("");
  const [applyingCompany, setApplyingCompany] = useState(false);
  const [permLoading, setPermLoading] = useState(false);
  const [existingAvatar, setExistingAvatar] = useState<string | null>(null);
  const [avatarFile, setAvatarFile] = useState<File | null>(null);
  const [avatarPreview, setAvatarPreview] = useState<string | null>(null);
  const [removeAvatar, setRemoveAvatar] = useState(false);

  const [goals, setGoals] = useState<UserGoalItem[]>([]);
  const [goalsLoading, setGoalsLoading] = useState(false);
  const [goalModalOpen, setGoalModalOpen] = useState(false);
  const [goalSaving, setGoalSaving] = useState(false);
  const [goalForm, setGoalForm] = useState<{ id: number | null; month: string; goal_price: string }>({ id: null, month: "", goal_price: "" });

  const { options: countries } = useLookupOptions("/api/v1/country");

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
      .get<DataMessage<Paginated<UserItem>>>("/api/v1/user", {
        search: debouncedSearch || undefined,
        phone_country_id: fPhoneCountry || undefined,
        working_tracking: fWorkingTracking || undefined,
        per_page: PER_PAGE,
        page,
      })
      .then((res) => {
        setRows(res.data.data);
        setTotal(res.data.total);
      })
      .catch(() => addToast("Kullanıcı listesi yüklenemedi", "error"))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch, page, fPhoneCountry, fWorkingTracking]);

  function openNew() {
    setEditingId(null);
    setForm({ name: "", surname: "", email: "", phone: "", password: "", password_confirmation: "", phone_country_id: "", pkds_id: "" });
    setErrors({});
    setPermRows([]);
    setGoals([]);
    setExistingAvatar(null);
    setAvatarFile(null);
    setRemoveAvatar(false);
    setTab("Profil");
    setDrawerOpen(true);
  }

  async function loadGoals(userId: number) {
    setGoalsLoading(true);
    try {
      const res = await api.get<DataMessage<UserGoalItem[]>>("/api/v1/user_goal", { user_id: userId });
      setGoals(res.data);
    } catch {
      setGoals([]);
    } finally {
      setGoalsLoading(false);
    }
  }

  async function loadPermissions(userId: number) {
    setPermLoading(true);
    try {
      // Bu uç {data,message} zarfı KULLANMAZ — çıplak {id, stats} döner.
      const res = await api.get<{ id: number; stats: { permission_data: PermissionRow[] } }>("/api/v1/role", { id: userId });
      setPermRows(res.stats.permission_data);
    } catch {
      setPermRows([]);
    } finally {
      setPermLoading(false);
    }
  }

  async function openEdit(u: UserItem) {
    setEditingId(u.id);
    setErrors({});
    setTab("Profil");
    setDrawerOpen(true);
    try {
      const res = await api.get<DataMessage<UserDetail>>(`/api/v1/user/${u.id}`);
      const d = res.data;
      setForm({
        name: d.name ?? "", surname: d.surname ?? "", email: d.email ?? "", phone: d.phone ?? "",
        password: "", password_confirmation: "", phone_country_id: d.phone_country_id?.id ?? "", pkds_id: d.pkds_id ?? "",
      });
      setExistingAvatar(d.avatar);
      setDetailRoleId(d.role_id);
      setDetailDepartment(d.siber_department_name);
      setDetailCompany(d.siber_company_id ?? "");
    } catch {
      addToast("Kullanıcı bilgileri yüklenemedi", "error");
    }
    setAvatarFile(null);
    setRemoveAvatar(false);
    await Promise.all([loadPermissions(u.id), loadGoals(u.id)]);
  }

  async function handleSubmit() {
    // Buton disabled={saving} render'a kadar DOM'a yansımıyor — hızlı çift
    // tıklama/tekrar tetiklemeye karşı erken çıkış.
    if (saving) return;
    setSaving(true);
    setErrors({});
    const fd = new FormData();
    if (editingId) fd.append("id", String(editingId));
    fd.append("name", form.name);
    fd.append("surname", form.surname);
    fd.append("email", form.email);
    fd.append("phone", form.phone);
    if (form.phone_country_id) fd.append("phone_country_id", form.phone_country_id);
    if (form.pkds_id) fd.append("pkds_id", form.pkds_id);
    if (form.password) {
      fd.append("password", form.password);
      fd.append("password_confirmation", form.password_confirmation);
    }
    if (avatarFile) fd.append("avatar", avatarFile);
    else if (removeAvatar) fd.append("avatar_remove", "1");
    try {
      if (editingId) {
        await api.postForm("/api/v1/user/update", fd);
        addToast("Kullanıcı güncellendi");
      } else {
        await api.postForm("/api/v1/user", fd);
        addToast("Kullanıcı oluşturuldu");
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

  async function handleDelete(id: number, name: string | null) {
    if (!window.confirm(`"${name ?? id}" silinsin mi?`)) return;
    try {
      await api.delete("/api/v1/user", { deletion_id: [id] });
      addToast("Kullanıcı silindi");
      load();
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Silinemedi", "error");
    }
  }

  // Rol listesi yalnızca rol yönetimi yetkisi olanlara lazım; yetkisiz kullanıcı
  // için uç 403 döner ve liste boş kalır (seçici de gizlenir).
  useEffect(() => {
    if (!canManageRoles) return;
    api
      .get<DataMessage<RoleOption[]>>("/api/v1/roles")
      .then((res) => setRoles(res.data))
      .catch(() => setRoles([]));
  }, [canManageRoles]);

  async function applyRole(roleId: number) {
    if (!editingId) return;
    setApplyingRole(true);
    try {
      await api.post("/api/v1/roles/assign", { user_id: editingId, role_id: roleId });
      setDetailRoleId(roleId);
      await loadPermissions(editingId);
      addToast("Rol uygulandı");
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Rol uygulanamadı", "error");
    } finally {
      setApplyingRole(false);
    }
  }

  async function applyCompanyScope(companyId: string) {
    if (!editingId) return;
    setApplyingCompany(true);
    try {
      await api.post("/api/v1/roles/company-scope", {
        user_id: editingId,
        company_id: companyId || null,
      });
      setDetailCompany(companyId);
      addToast("Şirket kapsamı güncellendi");
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Kapsam güncellenemedi", "error");
    } finally {
      setApplyingCompany(false);
    }
  }

  async function togglePermission(row: PermissionRow, crud: "read" | "create" | "update" | "delete") {
    if (!editingId) return;
    const newValue = row[crud] === 1 ? 0 : 1;
    setPermRows((rowsState) => rowsState.map((r) => (r.id === row.id ? { ...r, [crud]: newValue } : r)));
    try {
      await api.put("/api/v1/role", { crud, is_data: newValue, permission_page_id: row.id });
    } catch {
      addToast("Yetki güncellenemedi", "error");
      setPermRows((rowsState) => rowsState.map((r) => (r.id === row.id ? { ...r, [crud]: row[crud] } : r)));
    }
  }

  // olsold: UserRole.vue "Tümünü Seç" satırı — permission_page_id GÖNDERMEDEN
  // çağırmak backend'de (RoleController) o kullanıcının TÜM satırlarını
  // tek seferde günceller.
  async function toggleAllPermissions(crud: "read" | "create" | "update" | "delete") {
    if (!editingId) return;
    const newValue = permRows.every((r) => r[crud] === 1) ? 0 : 1;
    const previous = permRows;
    setPermRows((rowsState) => rowsState.map((r) => ({ ...r, [crud]: newValue })));
    try {
      await api.put("/api/v1/role", { crud, is_data: newValue, user_id: editingId });
    } catch {
      addToast("Yetki güncellenemedi", "error");
      setPermRows(previous);
    }
  }

  // olsold: UserTarget.vue setNewDate — ayın 1'i / ayın son günü.
  function monthToRange(month: string): { start: string; end: string } | null {
    if (!month) return null;
    const [y, m] = month.split("-").map(Number);
    const fmt = (d: Date) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
    return { start: fmt(new Date(y, m - 1, 1)), end: fmt(new Date(y, m, 0)) };
  }

  function openNewGoal() {
    setGoalForm({ id: null, month: "", goal_price: "" });
    setGoalModalOpen(true);
  }

  function openEditGoal(g: UserGoalItem) {
    setGoalForm({ id: g.id, month: g.start_date ? g.start_date.slice(0, 7) : "", goal_price: g.goal_price != null ? String(g.goal_price) : "" });
    setGoalModalOpen(true);
  }

  async function handleGoalSubmit() {
    if (!editingId) return;
    const range = monthToRange(goalForm.month);
    setGoalSaving(true);
    try {
      const body = {
        user_id: editingId,
        start_date: range?.start ?? null,
        end_date: range?.end ?? null,
        goal_price: goalForm.goal_price ? Number(goalForm.goal_price) : 0,
      };
      if (goalForm.id) {
        await api.put("/api/v1/user_goal", { id: goalForm.id, ...body });
      } else {
        await api.post("/api/v1/user_goal", body);
      }
      addToast(goalForm.id ? "Hedef güncellendi" : "Hedef eklendi");
      setGoalModalOpen(false);
      loadGoals(editingId);
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Hedef kaydedilemedi", "error");
    } finally {
      setGoalSaving(false);
    }
  }

  async function handleDeleteGoal(id: number) {
    if (!editingId || !window.confirm("Bu hedef silinsin mi?")) return;
    try {
      await api.delete("/api/v1/user_goal", { deletion_id: [id] });
      addToast("Hedef silindi");
      loadGoals(editingId);
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Silinemedi", "error");
    }
  }

  return (
    <>
      <ModulePage
        title="Kullanıcılar"
        action={canCreate ? <Btn onClick={openNew}><Plus size={14} />Yeni Kullanıcı</Btn> : undefined}
      >
        <div className="bg-white border-b border-gray-200 px-6 py-4">
          <div className="flex items-center gap-2.5">
            <div className="flex-1 max-w-md">
              <TextInput value={search} onChange={(v) => { setSearch(v); setPage(1); }} placeholder="Genel arama: ad, e-posta..." />
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
                <div className="grid grid-cols-2 gap-3 pt-4 mt-4 border-t border-gray-100">
                  <FormField label="Ülke Kodu">
                    <SelectInput value={fPhoneCountry} onChange={(v) => { setFPhoneCountry(v); setPage(1); }} options={[{ value: "", label: "Seçiniz" }, ...countries.map((c) => ({ value: String(c.id), label: c.phone_code ? `+${c.phone_code}` : c.name }))]} />
                  </FormField>
                  <FormField label="Mesai Takibi">
                    <SelectInput value={fWorkingTracking} onChange={(v) => { setFWorkingTracking(v); setPage(1); }} options={[{ value: "", label: "Seçiniz" }, { value: "true", label: "Evet" }, { value: "false", label: "Hayır" }]} />
                  </FormField>
                </div>
              </motion.div>
            )}
          </AnimatePresence>
        </div>
        <div className="bg-gray-50/70 min-h-full">
          {!loading && rows.length === 0 ? (
            <EmptyState icon={Shield} title="Kullanıcı bulunamadı" desc="Arama kriterlerine uygun kullanıcı bulunamadı." />
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
                      <UserCard
                        key={r.id}
                        row={r}
                        index={i}
                        onClick={() => openEdit(r)}
                        canDelete={canDelete && r.id !== me?.id}
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
        title={editingId ? `${form.name} ${form.surname}` : "Yeni Kullanıcı"}
        subtitle={editingId ? form.email : undefined}
        width="w-[640px]"
        footer={
          tab === "Profil" && (editingId ? canUpdate : canCreate) ? (
            <div className="flex gap-2">
              <Btn onClick={handleSubmit} disabled={saving}>{saving ? "Kaydediliyor..." : "Kaydet"}</Btn>
              <Btn variant="secondary" onClick={() => setDrawerOpen(false)}>İptal</Btn>
            </div>
          ) : undefined
        }
      >
        <Tabs tabs={editingId ? ["Profil", "Yetkiler", "Hedefler"] : ["Profil"]} active={tab} onChange={setTab} className="px-6" />
        {tab === "Profil" && (
          <div className="p-6 grid grid-cols-2 gap-4">
            <div className="col-span-2">
              <FormField label="Profil Fotoğrafı">
                <div className="flex items-center gap-3">
                  {avatarPreview ? (
                    <img src={avatarPreview} alt="" className="w-14 h-14 rounded-full object-cover" />
                  ) : !removeAvatar && existingAvatar ? (
                    <img src={`/storage/${existingAvatar}`} alt="" className="w-14 h-14 rounded-full object-cover" />
                  ) : (
                    <div className="w-14 h-14 rounded-full bg-blue-100 text-blue-700 flex items-center justify-center text-lg font-bold">
                      {initials(form.name, form.surname)}
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
                    {(avatarPreview || (!removeAvatar && existingAvatar)) && (
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
            </div>
            <FormField label="Ad" required error={errors.name?.[0]}>
              <TextInput value={form.name} onChange={(v) => setForm((f) => ({ ...f, name: v }))} error={!!errors.name} />
            </FormField>
            <FormField label="Soyad" required error={errors.surname?.[0]}>
              <TextInput value={form.surname} onChange={(v) => setForm((f) => ({ ...f, surname: v }))} error={!!errors.surname} />
            </FormField>
            <FormField label="E-posta" required error={errors.email?.[0]}>
              <TextInput value={form.email} onChange={(v) => setForm((f) => ({ ...f, email: v }))} type="email" error={!!errors.email} />
            </FormField>
            <FormField label={editingId ? "Yeni Şifre" : "Şifre"} required={!editingId} error={errors.password?.[0]} hint={editingId ? "Boş bırakılırsa mevcut şifre korunur." : undefined}>
              <TextInput value={form.password} onChange={(v) => setForm((f) => ({ ...f, password: v }))} type="password" error={!!errors.password} />
            </FormField>
            <FormField label="Şifre Tekrar" required={!editingId || !!form.password} error={errors.password_confirmation?.[0]}>
              <TextInput value={form.password_confirmation} onChange={(v) => setForm((f) => ({ ...f, password_confirmation: v }))} type="password" error={!!errors.password_confirmation} />
            </FormField>
            <FormField label="Ülke Kodu" required={!editingId} error={errors.phone_country_id?.[0]}>
              <SelectInput
                value={form.phone_country_id}
                onChange={(v) => setForm((f) => ({ ...f, phone_country_id: v }))}
                // olsold: UserFormDrawer.vue — SelectAjax optionLabel="phone_code", "+{{ phone_code }}" olarak gösterir.
                options={[
                  { value: "", label: "Seçiniz" },
                  ...countries.map((c) => ({ value: String(c.id), label: c.phone_code ? `+${c.phone_code}` : c.name })),
                ]}
              />
            </FormField>
            <FormField label="Telefon" required error={errors.phone?.[0]}>
              <TextInput value={form.phone} onChange={(v) => setForm((f) => ({ ...f, phone: v }))} placeholder="5XX XXX XX XX" error={!!errors.phone} />
            </FormField>
            <FormField label="PDKS Numarası">
              <TextInput value={form.pkds_id} onChange={(v) => setForm((f) => ({ ...f, pkds_id: v }))} />
            </FormField>
          </div>
        )}
        {tab === "Yetkiler" && (
          <div className="p-6">
            {canManageRoles && roles.length > 0 && (
              <div className="mb-5 rounded-lg border border-gray-200 bg-gray-50/70 p-4">
                <div className="flex items-center justify-between gap-3 mb-1">
                  <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider">Rol</p>
                  {detailDepartment && (
                    <span className="text-[11px] text-gray-400">
                      Siber departmanı: <b className="text-gray-600">{detailDepartment}</b>
                    </span>
                  )}
                </div>

                <div className="flex flex-wrap items-center gap-2">
                  <select
                    value={detailRoleId ?? ""}
                    disabled={applyingRole}
                    onChange={(e) => {
                      const value = e.target.value;
                      if (value) applyRole(Number(value));
                    }}
                    className="px-3 py-2 text-sm border border-gray-200 rounded-md bg-white focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 disabled:opacity-50"
                  >
                    <option value="">Rol seçin…</option>
                    {roles.map((r) => (
                      <option key={r.id} value={r.id}>{r.name}</option>
                    ))}
                  </select>
                  {applyingRole && <span className="text-xs text-gray-500">Uygulanıyor…</span>}
                </div>

                <div className="mt-3 border-t border-gray-200 pt-3">
                  <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider mb-1">
                    Şirket Kapsamı
                  </p>
                  <div className="flex flex-wrap items-center gap-2">
                    <select
                      value={detailCompany}
                      disabled={applyingCompany}
                      onChange={(e) => applyCompanyScope(e.target.value)}
                      className="px-3 py-2 text-sm border border-gray-200 rounded-md bg-white focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 disabled:opacity-50"
                    >
                      <option value="">Avrora hariç tümü (varsayılan)</option>
                      <option value={AVRORA_COMPANY_ID}>Yalnızca Avrora</option>
                    </select>
                    {applyingCompany && <span className="text-xs text-gray-500">Uygulanıyor…</span>}
                  </div>
                  <p className="mt-1 text-[11px] text-gray-500">
                    Yük ve sefer listelerinde hangi şirketin kayıtlarının görüneceğini belirler.
                    E-postası <b>@avroralog.com</b> olan kullanıcılar bu ayar boş olsa da
                    yalnızca Avrora'yı görür. Yönetim rolü her şeyi görür.
                  </p>
                </div>

                <p className="mt-2 text-[11px] text-gray-500">
                  {roles.find((r) => r.id === detailRoleId)?.description
                    ?? "Rol seçildiğinde o rolün yetki şablonu aşağıdaki tabloya uygulanır."}
                  {" "}Uyguladıktan sonra tek tek değiştirebilirsiniz.
                </p>
              </div>
            )}

            {permLoading ? (
              <p className="text-sm text-gray-400 text-center py-10">Yükleniyor...</p>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-xs border-collapse">
                  <thead>
                    <tr>
                      <th className="text-left py-2 pr-3 text-gray-500 font-semibold uppercase tracking-wide">Modül</th>
                      {(["read", "create", "update", "delete"] as const).map((p) => (
                        <th key={p} className="text-center py-2 px-3 text-gray-500 font-semibold uppercase tracking-wide">
                          {PERM_LABELS[p]}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    <tr className="border-b border-gray-100">
                      <td className="py-2.5 pr-3 font-semibold text-gray-700">Tümünü Seç</td>
                      {(["read", "create", "update", "delete"] as const).map((crud) => (
                        <td key={crud} className="py-2.5 px-3 text-center">
                          <input
                            type="checkbox"
                            checked={permRows.length > 0 && permRows.every((r) => r[crud] === 1)}
                            disabled={!canManageRoles || permRows.length === 0}
                            onChange={() => toggleAllPermissions(crud)}
                            className="rounded border-gray-300 text-blue-600 focus:ring-blue-500 disabled:opacity-50"
                          />
                        </td>
                      ))}
                    </tr>
                    {permRows.map((row, i) => (
                      <tr key={row.id} className={i % 2 === 0 ? "bg-gray-50/50" : ""}>
                        <td className="py-2.5 pr-3 font-medium text-gray-700">{row.permission_page_name}</td>
                        {(["read", "create", "update", "delete"] as const).map((crud) => (
                          <td key={crud} className="py-2.5 px-3 text-center">
                            <input
                              type="checkbox"
                              checked={row[crud] === 1}
                              disabled={!canManageRoles}
                              onChange={() => togglePermission(row, crud)}
                              className="rounded border-gray-300 text-blue-600 focus:ring-blue-500 disabled:opacity-50"
                            />
                          </td>
                        ))}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        )}
        {tab === "Hedefler" && (
          <div className="p-6">
            <div className="flex lg:justify-end mb-4">
              <Btn onClick={openNewGoal} size="sm"><Plus size={14} />Yeni Hedef Ekle</Btn>
            </div>
            {goalsLoading ? (
              <p className="text-sm text-gray-400 text-center py-10">Yükleniyor...</p>
            ) : goals.length === 0 ? (
              <div className="flex items-center justify-center py-16">
                <div className="py-3 px-6 bg-gray-100 text-gray-600 rounded-xl text-sm">Henüz hedef eklenmemiş.</div>
              </div>
            ) : (
              <div className="space-y-2">
                {goals.map((g) => (
                  <div key={g.id} className="flex items-center justify-between gap-4 rounded-xl px-6 py-4 bg-white border border-gray-200 shadow-xs">
                    <div className="space-y-2">
                      <div className="flex items-center gap-3 text-sm">
                        <Target size={16} className="text-gray-400" />
                        <span className="text-gray-500">Hedef :</span>
                        <span className="font-medium">₺{g.goal_price.toLocaleString("tr-TR")}</span>
                      </div>
                      <div className="flex items-center gap-3 text-sm">
                        <Calendar size={16} className="text-gray-400" />
                        <span className="text-gray-500">Tarih :</span>
                        <span className="font-medium">
                          {g.start_date ? new Date(g.start_date).toLocaleDateString("tr-TR") : "—"} - {g.end_date ? new Date(g.end_date).toLocaleDateString("tr-TR") : "—"}
                        </span>
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <Btn variant="secondary" size="sm" onClick={() => openEditGoal(g)}><Pencil size={13} />Düzenle</Btn>
                      <Btn variant="secondary" size="sm" onClick={() => handleDeleteGoal(g.id)}><Trash2 size={13} />Sil</Btn>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
      </Drawer>

      <Modal open={goalModalOpen} onClose={() => setGoalModalOpen(false)} title={goalForm.id ? "Hedefi Düzenle" : "Yeni Hedef Ekle"}>
        <div className="p-6 space-y-4 w-[420px] max-w-full">
          <FormField label="Tarih">
            <TextInput type="month" value={goalForm.month} onChange={(v) => setGoalForm((f) => ({ ...f, month: v }))} />
          </FormField>
          <FormField label="Hedef">
            <TextInput type="number" value={goalForm.goal_price} onChange={(v) => setGoalForm((f) => ({ ...f, goal_price: v }))} />
          </FormField>
        </div>
        <div className="flex justify-end gap-2 px-6 pb-6">
          <Btn variant="secondary" onClick={() => setGoalModalOpen(false)} disabled={goalSaving}>İptal</Btn>
          <Btn onClick={handleGoalSubmit} disabled={goalSaving}>{goalSaving ? "Kaydediliyor..." : "Kaydet"}</Btn>
        </div>
      </Modal>
    </>
  );
}
