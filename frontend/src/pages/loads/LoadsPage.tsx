import { useEffect, useState } from "react";
import { Package } from "lucide-react";
import { api, type DataMessage, type Paginated } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useDebouncedValue } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { ModulePage } from "@/components/ui/ModulePage";
import { DataTable, EmptyState, Pagination, type Column } from "@/components/ui/DataTable";
import { Drawer } from "@/components/ui/Overlay";
import { Badge } from "@/components/ui/primitives";

interface NamedRef {
  id: number;
  name: string | null;
}

interface LoadTransferItem {
  id: number;
  load_number: string | null;
  load_number_work_type: string | null;
  total_gross_weight: number | null;
  total_volume: number | null;
  customer_id: NamedRef | null;
  load_status_id: NamedRef | null;
}

interface LoadTransferDetail extends LoadTransferItem {
  sender_id: NamedRef | null;
  receiver_id: NamedRef | null;
  romork_type_id: NamedRef | null;
  department_id: NamedRef | null;
  total_lademeter: number | null;
  weight_fee: number | null;
}

const PER_PAGE = 8;

export function LoadsPage() {
  const { can } = useAuth();
  const { addToast } = useToast();

  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);
  const [page, setPage] = useState(1);
  const [rows, setRows] = useState<LoadTransferItem[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);

  const [drawerOpen, setDrawerOpen] = useState(false);
  const [detail, setDetail] = useState<LoadTransferDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);

  useEffect(() => {
    setLoading(true);
    api
      .get<DataMessage<Paginated<LoadTransferItem>>>("/api/v1/load_transfer", { search: debouncedSearch || undefined, per_page: PER_PAGE, page })
      .then((res) => {
        setRows(res.data.data);
        setTotal(res.data.total);
      })
      .catch(() => addToast("Yük listesi yüklenemedi", "error"))
      .finally(() => setLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch, page]);

  async function openDetail(id: number) {
    setDrawerOpen(true);
    setDetailLoading(true);
    try {
      const res = await api.get<DataMessage<LoadTransferDetail>>(`/api/v1/load_transfer/${id}`);
      setDetail(res.data);
    } catch {
      addToast("Yük bilgileri yüklenemedi", "error");
      setDrawerOpen(false);
    } finally {
      setDetailLoading(false);
    }
  }

  const columns: Column<LoadTransferItem>[] = [
    { key: "load_number", header: "Yük No", sortable: true, render: (r) => <span className="font-mono text-[11px] text-blue-600">{r.load_number_work_type ?? r.load_number ?? `Y${r.id}`}</span> },
    { key: "customer", header: "Müşteri", sortable: true, render: (r) => <span className="font-semibold">{r.customer_id?.name ?? "—"}</span> },
    { key: "weight", header: "Ağırlık", render: (r) => <span className="font-mono text-xs">{r.total_gross_weight != null ? `${r.total_gross_weight} kg` : "—"}</span> },
    { key: "volume", header: "Hacim", render: (r) => <span className="font-mono text-xs">{r.total_volume != null ? `${r.total_volume} m³` : "—"}</span> },
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
              desc="Henüz yüke dönüştürülmüş teklif yok. Yük kaydı, onaylanmış bir teklifin Teklifler ekranından dönüştürülmesiyle oluşur."
            />
          ) : (
            <>
              <DataTable data={rows} columns={columns} loading={loading} onRowClick={(r) => openDetail(r.id)} />
              <Pagination page={page} total={total} perPage={PER_PAGE} onChange={setPage} />
            </>
          )}
        </div>
      </ModulePage>

      <Drawer open={drawerOpen} onClose={() => setDrawerOpen(false)} title={detail?.load_number_work_type ?? detail?.load_number ?? "Yük"} subtitle={detail?.customer_id?.name ?? undefined}>
        {detailLoading ? (
          <div className="p-10 text-center text-sm text-gray-400">Yükleniyor...</div>
        ) : (
          detail && (
            <div className="p-6 grid grid-cols-2 gap-3">
              {[
                ["Müşteri", detail.customer_id?.name ?? "—"],
                ["Gönderici", detail.sender_id?.name ?? "—"],
                ["Alıcı", detail.receiver_id?.name ?? "—"],
                ["Departman", detail.department_id?.name ?? "—"],
                ["Romork Tipi", detail.romork_type_id?.name ?? "—"],
                ["Brüt Ağırlık", detail.total_gross_weight != null ? `${detail.total_gross_weight} kg` : "—"],
                ["Hacim", detail.total_volume != null ? `${detail.total_volume} m³` : "—"],
                ["Lademetre", detail.total_lademeter != null ? String(detail.total_lademeter) : "—"],
              ].map(([k, v]) => (
                <div key={k} className="bg-gray-50 rounded-lg p-3">
                  <p className="text-[11px] text-gray-400 uppercase font-semibold tracking-wide">{k}</p>
                  <p className="text-sm font-medium text-gray-800 mt-0.5">{v}</p>
                </div>
              ))}
            </div>
          )
        )}
      </Drawer>
    </>
  );
}
