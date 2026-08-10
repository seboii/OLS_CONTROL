import { useEffect, useState } from "react";
import { Headphones } from "lucide-react";
import { api, type Paginated } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useToast } from "@/components/ui/Toast";
import { ModulePage } from "@/components/ui/ModulePage";
import { DataTable, EmptyState, Pagination, type Column } from "@/components/ui/DataTable";
import { Drawer } from "@/components/ui/Overlay";
import { Badge, Btn, SelectInput } from "@/components/ui/primitives";

interface ContactForm {
  id: number;
  first_name: string | null;
  last_name: string | null;
  email: string | null;
  phone: string | null;
  message: string | null;
  is_read: boolean;
  is_answered: boolean;
  created_at: string | null;
}

interface Envelope<T> {
  success: boolean;
  data: T;
  message?: string;
}

const PER_PAGE = 10;

export function SupportPage() {
  const { can } = useAuth();
  const { addToast } = useToast();
  const canRead = can("support_request_management", "read");
  const canUpdate = can("support_request_management", "update");

  const [page, setPage] = useState(1);
  const [rows, setRows] = useState<ContactForm[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [search] = useState("");

  const [drawerOpen, setDrawerOpen] = useState(false);
  const [selected, setSelected] = useState<ContactForm | null>(null);
  const [updating, setUpdating] = useState(false);

  function load() {
    setLoading(true);
    api
      .get<Envelope<Paginated<ContactForm>>>("/api/website/contact/form", { page })
      .then((res) => {
        setRows(res.data.data);
        setTotal(res.data.total);
      })
      .catch(() => addToast("Destek talepleri yüklenemedi", "error"))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page]);

  async function openDetail(row: ContactForm) {
    try {
      const res = await api.get<Envelope<ContactForm>>(`/api/website/contact/form/${row.id}`);
      setSelected(res.data);
      setDrawerOpen(true);
      // GET görüntülemesi kaynak davranışına uygun olarak is_read'i true yapar — listeyi tazele.
      load();
    } catch {
      addToast("Talep detayı yüklenemedi", "error");
    }
  }

  async function handleAnsweredChange(isAnswered: boolean) {
    if (!selected) return;
    setUpdating(true);
    try {
      const res = await api.patch<Envelope<ContactForm>>(`/api/website/contact/form/${selected.id}/answered`, {
        is_answered: isAnswered,
      });
      setSelected(res.data);
      addToast(isAnswered ? "Talep yanıtlandı olarak işaretlendi" : "Talep yanıtlanmadı olarak işaretlendi");
      load();
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Güncellenemedi", "error");
    } finally {
      setUpdating(false);
    }
  }

  const columns: Column<ContactForm>[] = [
    { key: "id", header: "Talep No", sortable: true, render: (r) => <span className="font-mono text-[11px] text-blue-600">SUP-{r.id}</span> },
    {
      key: "from",
      header: "Gönderen",
      render: (r) => (
        <div>
          <p className="font-semibold text-gray-900">{r.first_name} {r.last_name}</p>
          <p className="text-[11px] text-gray-400">{r.email}</p>
        </div>
      ),
    },
    { key: "message", header: "Mesaj", render: (r) => <span className="text-gray-700 line-clamp-1 max-w-xs block">{r.message}</span> },
    { key: "phone", header: "Telefon", render: (r) => <span className="font-mono text-xs text-gray-500">{r.phone ?? "—"}</span> },
    { key: "created_at", header: "Tarih", render: (r) => <span className="font-mono text-[11px] text-gray-500">{r.created_at ? new Date(r.created_at).toLocaleString("tr-TR") : "—"}</span> },
    { key: "is_read", header: "Okundu", render: (r) => <Badge label={r.is_read ? "Okundu" : "Okunmadı"} /> },
    { key: "is_answered", header: "Durum", render: (r) => <Badge label={r.is_answered ? "Çözüldü" : "Açık"} /> },
  ];

  if (!canRead) {
    return <EmptyState icon={Headphones} title="Yetkiniz yok" desc="Bu ekranı görüntülemek için gerekli yetkiye sahip değilsiniz." />;
  }

  return (
    <>
      <ModulePage title="Destek Talepleri" search={search} onSearchChange={() => {}} searchPlaceholder="">
        <div className="bg-white">
          {!loading && rows.length === 0 ? (
            <EmptyState icon={Headphones} title="Talep bulunamadı" desc="Henüz destek talebi gönderilmemiş." />
          ) : (
            <>
              <DataTable data={rows} columns={columns} loading={loading} onRowClick={openDetail} />
              <Pagination page={page} total={total} perPage={PER_PAGE} onChange={setPage} />
            </>
          )}
        </div>
      </ModulePage>

      <Drawer
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        title={selected ? `SUP-${selected.id}` : ""}
        subtitle={selected ? `${selected.first_name} ${selected.last_name}` : undefined}
      >
        {selected && (
          <div className="p-6 space-y-5">
            <div className="grid grid-cols-2 gap-3">
              <div className="bg-gray-50 rounded-lg p-3">
                <p className="text-[11px] text-gray-400 uppercase font-semibold tracking-wide">E-posta</p>
                <p className="text-sm font-medium text-gray-800 mt-0.5">{selected.email}</p>
              </div>
              <div className="bg-gray-50 rounded-lg p-3">
                <p className="text-[11px] text-gray-400 uppercase font-semibold tracking-wide">Telefon</p>
                <p className="text-sm font-medium text-gray-800 mt-0.5">{selected.phone ?? "—"}</p>
              </div>
            </div>
            <div className="p-4 bg-gray-50 border border-gray-200 rounded-lg">
              <p className="text-sm text-gray-700 leading-relaxed whitespace-pre-line">{selected.message}</p>
            </div>
            <FormFieldLike label="Yanıtlanma Durumu">
              <SelectInput
                value={selected.is_answered ? "1" : "0"}
                onChange={(v) => handleAnsweredChange(v === "1")}
                disabled={!canUpdate || updating}
                options={[
                  { value: "0", label: "Yanıtlanmadı" },
                  { value: "1", label: "Yanıtlandı" },
                ]}
              />
            </FormFieldLike>
            <div className="flex justify-end">
              <Btn variant="secondary" onClick={() => setDrawerOpen(false)}>
                Kapat
              </Btn>
            </div>
          </div>
        )}
      </Drawer>
    </>
  );
}

function FormFieldLike({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-1.5">
      <label className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider">{label}</label>
      {children}
    </div>
  );
}
