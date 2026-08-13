import { useEffect, useState } from "react";
import { Plus, Shield } from "lucide-react";
import { api, ApiError, type DataMessage, type Paginated } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useDebouncedValue } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { ModulePage } from "@/components/ui/ModulePage";
import { DataTable, EmptyState, Pagination, RowActions, type Column } from "@/components/ui/DataTable";
import { Drawer } from "@/components/ui/Overlay";
import { Badge, Btn, FormField, Tabs, TextInput } from "@/components/ui/primitives";

interface UserItem {
  id: number;
  name: string | null;
  surname: string | null;
  email: string | null;
  phone: string | null;
  status: boolean;
  avatar: string | null;
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

const PER_PAGE = 10;
const PERM_LABELS: Record<"read" | "create" | "update" | "delete", string> = {
  read: "Görüntüle",
  create: "Oluştur",
  update: "Düzenle",
  delete: "Sil",
};

function initials(name: string | null, surname: string | null) {
  return `${(name ?? "?").charAt(0)}${(surname ?? "").charAt(0)}`.toUpperCase();
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

  const [drawerOpen, setDrawerOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [tab, setTab] = useState("Profil");
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  const [form, setForm] = useState({ name: "", surname: "", email: "", phone: "", password: "" });
  const [permRows, setPermRows] = useState<PermissionRow[]>([]);
  const [permLoading, setPermLoading] = useState(false);

  function load() {
    setLoading(true);
    api
      .get<DataMessage<Paginated<UserItem>>>("/api/v1/user", { search: debouncedSearch || undefined, per_page: PER_PAGE, page })
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
  }, [debouncedSearch, page]);

  function openNew() {
    setEditingId(null);
    setForm({ name: "", surname: "", email: "", phone: "", password: "" });
    setErrors({});
    setPermRows([]);
    setTab("Profil");
    setDrawerOpen(true);
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
    setForm({ name: u.name ?? "", surname: u.surname ?? "", email: u.email ?? "", phone: u.phone ?? "", password: "" });
    setErrors({});
    setTab("Profil");
    setDrawerOpen(true);
    await loadPermissions(u.id);
  }

  async function handleSubmit() {
    setSaving(true);
    setErrors({});
    const fd = new FormData();
    if (editingId) fd.append("id", String(editingId));
    fd.append("name", form.name);
    fd.append("surname", form.surname);
    fd.append("email", form.email);
    fd.append("phone", form.phone);
    if (form.password) fd.append("password", form.password);
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

  const columns: Column<UserItem>[] = [
    {
      key: "avatar",
      header: "",
      width: "w-10",
      render: (r) =>
        r.avatar ? (
          <img src={`/storage/${r.avatar}`} alt={r.name ?? ""} className="w-7 h-7 rounded-full object-cover" />
        ) : (
          <div className="w-7 h-7 rounded-full bg-blue-100 text-blue-700 flex items-center justify-center text-[10px] font-bold">
            {initials(r.name, r.surname)}
          </div>
        ),
    },
    { key: "name", header: "Ad Soyad", sortable: true, render: (r) => <span className="font-semibold">{r.name} {r.surname}</span> },
    { key: "email", header: "E-posta", render: (r) => <span className="text-xs text-gray-500">{r.email}</span> },
    { key: "phone", header: "Telefon", render: (r) => <span className="font-mono text-xs text-gray-500">{r.phone ?? "—"}</span> },
    { key: "status", header: "Durum", render: (r) => <Badge label={r.status ? "Aktif" : "Pasif"} /> },
  ];

  return (
    <>
      <ModulePage
        title="Kullanıcılar"
        search={search}
        onSearchChange={(v) => { setSearch(v); setPage(1); }}
        searchPlaceholder="Ad, e-posta..."
        action={canCreate ? <Btn onClick={openNew}><Plus size={14} />Yeni Kullanıcı</Btn> : undefined}
      >
        <div className="bg-white">
          {!loading && rows.length === 0 ? (
            <EmptyState icon={Shield} title="Kullanıcı bulunamadı" desc="Arama kriterlerine uygun kullanıcı bulunamadı." />
          ) : (
            <>
              <DataTable
                data={rows}
                columns={columns}
                loading={loading}
                onRowClick={(r) => openEdit(r)}
                actions={
                  canUpdate || canDelete
                    ? (r) => <RowActions onView={() => openEdit(r)} onEdit={canUpdate ? () => openEdit(r) : undefined} onDelete={canDelete && r.id !== me?.id ? () => handleDelete(r.id, r.name) : undefined} />
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
        title={editingId ? `${form.name} ${form.surname}` : "Yeni Kullanıcı"}
        subtitle={editingId ? form.email : undefined}
        width="w-[640px]"
        footer={
          tab !== "Yetkiler" && (editingId ? canUpdate : canCreate) ? (
            <div className="flex gap-2">
              <Btn onClick={handleSubmit} disabled={saving}>{saving ? "Kaydediliyor..." : "Kaydet"}</Btn>
              <Btn variant="secondary" onClick={() => setDrawerOpen(false)}>İptal</Btn>
            </div>
          ) : undefined
        }
      >
        <Tabs tabs={editingId ? ["Profil", "Yetkiler"] : ["Profil"]} active={tab} onChange={setTab} className="px-6" />
        {tab === "Profil" && (
          <div className="p-6 grid grid-cols-2 gap-4">
            <FormField label="Ad" required error={errors.name?.[0]}>
              <TextInput value={form.name} onChange={(v) => setForm((f) => ({ ...f, name: v }))} error={!!errors.name} />
            </FormField>
            <FormField label="Soyad" required error={errors.surname?.[0]}>
              <TextInput value={form.surname} onChange={(v) => setForm((f) => ({ ...f, surname: v }))} error={!!errors.surname} />
            </FormField>
            <FormField label="E-posta" required error={errors.email?.[0]}>
              <TextInput value={form.email} onChange={(v) => setForm((f) => ({ ...f, email: v }))} type="email" error={!!errors.email} />
            </FormField>
            <FormField label="Telefon">
              <TextInput value={form.phone} onChange={(v) => setForm((f) => ({ ...f, phone: v }))} placeholder="+90 5XX XXX XX XX" />
            </FormField>
            <FormField label={editingId ? "Yeni Şifre" : "Şifre"} required={!editingId} error={errors.password?.[0]} hint={editingId ? "Boş bırakılırsa mevcut şifre korunur." : undefined}>
              <TextInput value={form.password} onChange={(v) => setForm((f) => ({ ...f, password: v }))} type="password" error={!!errors.password} />
            </FormField>
          </div>
        )}
        {tab === "Yetkiler" && (
          <div className="p-6">
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
      </Drawer>
    </>
  );
}
