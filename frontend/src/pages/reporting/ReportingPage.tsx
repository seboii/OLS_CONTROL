import { useEffect, useMemo, useState } from "react";
import { clsx } from "clsx";
import { BarChart3, FileText, Package, Truck, Users, Receipt, UserCheck, MapPin, Building2, TrendingUp, TrendingDown } from "lucide-react";
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
} from "recharts";
import { api, type DataMessage } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { ModulePage } from "@/components/ui/ModulePage";
import { DataTable, EmptyState, type Column } from "@/components/ui/DataTable";
import { SelectInput, Tabs, TextInput } from "@/components/ui/primitives";
import { Drawer } from "@/components/ui/Overlay";

interface ReportingKpi {
  total_offers: number;
  total_loads: number;
  total_expeditions: number;
  total_accounts: number;
  total_invoice_amount: number;
  total_users: number;
  expected_income_try: number;
  expected_expense_try: number;
  realized_income_try: number;
  realized_expense_try: number;
}

interface TrendPoint {
  bucket: string;
  offer_count: number;
  load_count: number;
}

interface UserReportRow {
  user_id: number;
  name: string | null;
  surname: string | null;
  email: string | null;
  avatar: string | null;
  offer_count: number;
  load_count: number;
  expedition_movement_count: number;
  account_count: number;
}

interface ReportingData {
  kpi: ReportingKpi;
  trend_granularity: "day" | "week" | "month";
  trend: TrendPoint[];
  users: UserReportRow[];
}

interface UserActivityRow {
  id: number;
  number: string | null;
  customer_name: string | null;
  created_at: string | null;
  status_name: string | null;
}

interface UserMovementRow {
  id: number;
  expedition_number: string | null;
  destination_name: string | null;
  status_name: string | null;
  created_at: string | null;
}

interface UserAccountRow {
  id: number;
  name: string | null;
}

interface UserReportDetail {
  summary: UserReportRow;
  recent_offers: UserActivityRow[];
  recent_loads: UserActivityRow[];
  recent_movements: UserMovementRow[];
  accounts: UserAccountRow[];
}

function initials(name: string | null, surname: string | null) {
  return `${(name ?? "?").charAt(0)}${(surname ?? "").charAt(0)}`.toUpperCase();
}

function KpiCard({
  label, value, icon: Icon, color,
}: {
  label: string; value: string; icon: React.ComponentType<{ size?: number; className?: string }>; color: string;
}) {
  return (
    <div className="bg-white rounded-xl p-4 border border-gray-200 shadow-sm hover:shadow-md transition-shadow">
      <div className={clsx("w-9 h-9 rounded-lg flex items-center justify-center mb-3", color)}>
        <Icon size={17} />
      </div>
      <p className="text-2xl font-bold text-gray-900 font-mono tracking-tight">{value}</p>
      <p className="text-xs font-semibold text-gray-700 mt-0.5">{label}</p>
    </div>
  );
}

const TOOLTIP_STYLE = {
  contentStyle: { background: "#fff", border: "1px solid #E5E7EB", borderRadius: 8, fontSize: 12, boxShadow: "0 4px 12px rgba(0,0,0,0.08)" },
  labelStyle: { fontWeight: 600, color: "#374151" },
  itemStyle: { color: "#6B7280" },
};

const SORT_OPTIONS = [
  { value: "activity", label: "Sırala: Toplam Aktivite" },
  { value: "offers", label: "Sırala: Teklif Sayısı" },
  { value: "loads", label: "Sırala: Yük Sayısı" },
  { value: "movements", label: "Sırala: Sefer Hareketi" },
  { value: "accounts", label: "Sırala: Sorumlu Müşteri" },
  { value: "name", label: "Sırala: İsim (A-Z)" },
];

const PERIOD_TABS = ["Tüm Zamanlar", "Bugün", "Bu Hafta", "Bu Ay", "Bu Yıl"];

function isoDate(d: Date) {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

function presetRange(label: string): { from: string; to: string } {
  const today = new Date();
  if (label === "Bugün") return { from: isoDate(today), to: isoDate(today) };
  if (label === "Bu Hafta") {
    const day = today.getDay();
    const diffToMonday = day === 0 ? 6 : day - 1;
    const monday = new Date(today);
    monday.setDate(today.getDate() - diffToMonday);
    return { from: isoDate(monday), to: isoDate(today) };
  }
  if (label === "Bu Ay") return { from: isoDate(new Date(today.getFullYear(), today.getMonth(), 1)), to: isoDate(today) };
  if (label === "Bu Yıl") return { from: isoDate(new Date(today.getFullYear(), 0, 1)), to: isoDate(today) };
  return { from: "", to: "" };
}

function formatBucket(bucket: string, granularity: "day" | "week" | "month") {
  const d = new Date(`${bucket}T00:00:00`);
  if (granularity === "month") return d.toLocaleDateString("tr-TR", { month: "short", year: "2-digit" });
  return d.toLocaleDateString("tr-TR", { day: "2-digit", month: "2-digit" });
}

// olsold'da bu modülün karşılığı yok (bkz. Dashboard'daki aynı kapsam-dışı-
// sonra-eklendi notu) — kullanıcı isteğiyle eklendi. Sayaçlar gerçek veri
// modelindeki tek bağlantı noktalarından hesaplanır (bkz. ReportingService.cs):
// Teklif -> load_charge_people (DISTINCT load_id), Yük -> usercode_with_notification,
// Sefer Hareketi -> expedition_movements.user_id, Sorumlu Müşteri -> user_account_mappings
// (bu sonuncusu güncel bir atama olduğu için dönem filtresinden etkilenmez).
export function ReportingPage() {
  const { can } = useAuth();
  const canRead = can("report_management", "read");

  const [data, setData] = useState<ReportingData | null>(null);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [sortBy, setSortBy] = useState("activity");
  const [periodTab, setPeriodTab] = useState(PERIOD_TABS[0]);
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");

  const [drawerOpen, setDrawerOpen] = useState(false);
  const [detail, setDetail] = useState<UserReportDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);

  function openDetail(userId: number) {
    setDrawerOpen(true);
    setDetail(null);
    setDetailLoading(true);
    api
      .get<DataMessage<UserReportDetail>>(`/api/v1/reporting/users/${userId}`, {
        date_from: dateFrom || undefined,
        date_to: dateTo || undefined,
      })
      .then((res) => setDetail(res.data))
      .finally(() => setDetailLoading(false));
  }

  function applyPreset(label: string) {
    const { from, to } = presetRange(label);
    setPeriodTab(label);
    setDateFrom(from);
    setDateTo(to);
  }

  useEffect(() => {
    if (!canRead) return;
    setLoading(true);
    api
      .get<DataMessage<ReportingData>>("/api/v1/reporting", {
        date_from: dateFrom || undefined,
        date_to: dateTo || undefined,
      })
      .then((res) => setData(res.data))
      .finally(() => setLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [dateFrom, dateTo]);

  const filteredRows = useMemo(() => {
    if (!data) return [];
    const q = search.trim().toLocaleLowerCase("tr");
    const rows = q
      ? data.users.filter((u) =>
          `${u.name ?? ""} ${u.surname ?? ""} ${u.email ?? ""}`.toLocaleLowerCase("tr").includes(q))
      : data.users;

    return [...rows].sort((a, b) => {
      switch (sortBy) {
        case "offers":
          return b.offer_count - a.offer_count;
        case "loads":
          return b.load_count - a.load_count;
        case "movements":
          return b.expedition_movement_count - a.expedition_movement_count;
        case "accounts":
          return b.account_count - a.account_count;
        case "name":
          return `${a.name ?? ""} ${a.surname ?? ""}`.localeCompare(`${b.name ?? ""} ${b.surname ?? ""}`, "tr");
        default: {
          const at = a.offer_count + a.load_count + a.expedition_movement_count;
          const bt = b.offer_count + b.load_count + b.expedition_movement_count;
          return bt - at;
        }
      }
    });
  }, [data, search, sortBy]);

  const trendChartData = useMemo(() => {
    if (!data) return [];
    return data.trend.map((t) => ({
      ...t,
      label: formatBucket(t.bucket, data.trend_granularity),
    }));
  }, [data]);

  const topUsers = useMemo(() => {
    if (!data) return [];
    return [...data.users]
      .sort((a, b) => (b.offer_count + b.load_count) - (a.offer_count + a.load_count))
      .slice(0, 8)
      .map((u) => ({
        name: `${u.name ?? ""} ${u.surname ?? ""}`.trim() || "—",
        offer_count: u.offer_count,
        load_count: u.load_count,
      }))
      .reverse(); // yatay barda en yüksek değer en üstte görünsün
  }, [data]);

  const columns: Column<UserReportRow & { id: number }>[] = [
    {
      key: "name",
      header: "Kullanıcı",
      render: (r) => (
        <div className="flex items-center gap-3">
          {r.avatar ? (
            <img src={`/storage/${r.avatar}`} alt="" className="w-8 h-8 rounded-lg object-cover border border-gray-200" />
          ) : (
            <div className="w-8 h-8 rounded-lg bg-blue-100 text-blue-700 flex items-center justify-center text-[10px] font-bold border border-gray-200">
              {initials(r.name, r.surname)}
            </div>
          )}
          <div>
            <div className="font-semibold text-gray-900">{r.name} {r.surname}</div>
            <div className="text-[11px] text-gray-400">{r.email}</div>
          </div>
        </div>
      ),
    },
    { key: "offer_count", header: "Teklif Sayısı", render: (r) => <span className="font-mono text-sm font-semibold text-gray-900">{r.offer_count}</span> },
    { key: "load_count", header: "Yük Sayısı", render: (r) => <span className="font-mono text-sm font-semibold text-gray-900">{r.load_count}</span> },
    { key: "expedition_movement_count", header: "Sefer Hareketi", render: (r) => <span className="font-mono text-xs text-gray-600">{r.expedition_movement_count}</span> },
    { key: "account_count", header: "Sorumlu Müşteri", render: (r) => <span className="font-mono text-xs text-gray-600">{r.account_count}</span> },
  ];

  if (!canRead) {
    return <EmptyState icon={BarChart3} title="Yetkiniz yok" desc="Bu ekranı görüntülemek için gerekli yetkiye sahip değilsiniz." />;
  }

  const kpi = data?.kpi;
  const money = (v: number) => `₺${Math.round(v).toLocaleString("tr-TR")}`;
  const periodActive = !!(dateFrom || dateTo);

  return (
    <>
    <ModulePage title="Raporlama">
      <div className="bg-gray-50/70 min-h-full p-6 space-y-6">
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-4">
          <div className="flex items-center justify-between gap-3 flex-wrap mb-3">
            <div>
              <h2 className="text-sm font-semibold text-gray-800">Dönem</h2>
              <p className="text-[11px] text-gray-400 mt-0.5">
                {periodActive
                  ? `${dateFrom || "…"} – ${dateTo || "…"} arası gösteriliyor (Sorumlu Müşteri hariç — o her zaman güncel).`
                  : "Tüm zamanlar gösteriliyor. Trend grafiği son 12 ayı özetler."}
              </p>
            </div>
            <div className="flex items-center gap-2">
              <TextInput type="date" value={dateFrom} onChange={(v) => { setDateFrom(v); setPeriodTab(""); }} />
              <span className="text-xs text-gray-400">–</span>
              <TextInput type="date" value={dateTo} onChange={(v) => { setDateTo(v); setPeriodTab(""); }} />
            </div>
          </div>
          <Tabs tabs={PERIOD_TABS} active={periodTab} onChange={applyPreset} />
        </div>

        <div className="grid grid-cols-2 sm:grid-cols-3 xl:grid-cols-6 gap-3">
          <KpiCard label="Teklif" value={loading ? "—" : String(kpi?.total_offers ?? 0)} icon={FileText} color="bg-blue-50 text-blue-600" />
          <KpiCard label="Yük" value={loading ? "—" : String(kpi?.total_loads ?? 0)} icon={Package} color="bg-indigo-50 text-indigo-600" />
          <KpiCard label="Sefer" value={loading ? "—" : String(kpi?.total_expeditions ?? 0)} icon={Truck} color="bg-cyan-50 text-cyan-600" />
          <KpiCard label="Toplam Müşteri" value={loading ? "—" : String(kpi?.total_accounts ?? 0)} icon={Users} color="bg-emerald-50 text-emerald-600" />
          <KpiCard label="Fatura Tutarı" value={loading ? "—" : money(kpi?.total_invoice_amount ?? 0)} icon={Receipt} color="bg-orange-50 text-orange-600" />
          <KpiCard label="Kullanıcı" value={loading ? "—" : String(kpi?.total_users ?? 0)} icon={UserCheck} color="bg-purple-50 text-purple-600" />
        </div>

        <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-4">
          <div className="mb-3">
            <h2 className="text-sm font-semibold text-gray-800">Gelir / Gider</h2>
            <p className="text-[11px] text-gray-400 mt-0.5">Siber'in kendi maliyet/ciro muhasebesinden (sbr_kzgelirgider), seçili döneme göre.</p>
          </div>
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
            <KpiCard label="Beklenen Gelir" value={loading ? "—" : money(kpi?.expected_income_try ?? 0)} icon={TrendingUp} color="bg-emerald-50 text-emerald-600" />
            <KpiCard label="Beklenen Gider" value={loading ? "—" : money(kpi?.expected_expense_try ?? 0)} icon={TrendingDown} color="bg-rose-50 text-rose-600" />
            <KpiCard label="Gerçekleşen Gelir" value={loading ? "—" : money(kpi?.realized_income_try ?? 0)} icon={TrendingUp} color="bg-emerald-50 text-emerald-600" />
            <KpiCard label="Gerçekleşen Gider" value={loading ? "—" : money(kpi?.realized_expense_try ?? 0)} icon={TrendingDown} color="bg-rose-50 text-rose-600" />
          </div>
        </div>

        <div className="grid grid-cols-1 xl:grid-cols-3 gap-4">
          <div className="xl:col-span-2 bg-white rounded-xl border border-gray-200 shadow-sm p-5">
            <div className="mb-4">
              <h3 className="text-sm font-semibold text-gray-900">Teklif &amp; Yük Trendi</h3>
              <p className="text-[11px] text-gray-400 mt-0.5">
                {data?.trend_granularity === "month" ? "Aylık" : data?.trend_granularity === "week" ? "Haftalık" : "Günlük"} açılan kayıt sayısı
              </p>
            </div>
            {!loading && trendChartData.length === 0 ? (
              <p className="text-xs text-gray-400 text-center py-16">Bu dönemde veri yok.</p>
            ) : (
              <ResponsiveContainer width="100%" height={220}>
                <BarChart data={trendChartData} margin={{ top: 4, right: 4, bottom: 0, left: -16 }} barGap={2}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#F3F4F6" vertical={false} />
                  <XAxis dataKey="label" tick={{ fontSize: 11, fill: "#9CA3AF" }} axisLine={false} tickLine={false} />
                  <YAxis tick={{ fontSize: 11, fill: "#9CA3AF" }} axisLine={false} tickLine={false} allowDecimals={false} />
                  <Tooltip {...TOOLTIP_STYLE} />
                  <Bar dataKey="offer_count" fill="#2563EB" radius={[3, 3, 0, 0]} name="Teklif" maxBarSize={22} />
                  <Bar dataKey="load_count" fill="#059669" radius={[3, 3, 0, 0]} name="Yük" maxBarSize={22} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </div>

          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
            <div className="mb-4">
              <h3 className="text-sm font-semibold text-gray-900">En Aktif 8 Kullanıcı</h3>
              <p className="text-[11px] text-gray-400 mt-0.5">Teklif + Yük toplamına göre</p>
            </div>
            {!loading && topUsers.length === 0 ? (
              <p className="text-xs text-gray-400 text-center py-16">Bu dönemde veri yok.</p>
            ) : (
              <ResponsiveContainer width="100%" height={260}>
                <BarChart data={topUsers} layout="vertical" margin={{ top: 4, right: 12, bottom: 0, left: 8 }} barGap={2}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#F3F4F6" horizontal={false} />
                  <XAxis type="number" tick={{ fontSize: 10, fill: "#9CA3AF" }} axisLine={false} tickLine={false} allowDecimals={false} />
                  <YAxis type="category" dataKey="name" width={100} tick={{ fontSize: 10, fill: "#374151" }} axisLine={false} tickLine={false} />
                  <Tooltip {...TOOLTIP_STYLE} />
                  <Bar dataKey="offer_count" fill="#2563EB" radius={[0, 3, 3, 0]} name="Teklif" maxBarSize={12} />
                  <Bar dataKey="load_count" fill="#059669" radius={[0, 3, 3, 0]} name="Yük" maxBarSize={12} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </div>
        </div>

        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <div className="flex items-center justify-between gap-3 px-4 py-3 border-b border-gray-100 flex-wrap">
            <h2 className="text-sm font-semibold text-gray-800">Kullanıcı Bazlı Aktivite</h2>
            <div className="flex items-center gap-2">
              <div className="w-56">
                <TextInput value={search} onChange={setSearch} placeholder="Kullanıcı ara: ad, e-posta..." />
              </div>
              <div className="w-56">
                <SelectInput value={sortBy} onChange={setSortBy} options={SORT_OPTIONS} />
              </div>
            </div>
          </div>
          {!loading && filteredRows.length === 0 ? (
            <EmptyState icon={Users} title="Kullanıcı bulunamadı" desc="Arama kriterlerine veya seçili döneme uygun kullanıcı bulunamadı." />
          ) : (
            <DataTable
              data={filteredRows.map((r) => ({ ...r, id: r.user_id }))}
              columns={columns}
              loading={loading}
              onRowClick={(r) => openDetail(r.user_id)}
            />
          )}
        </div>
      </div>
    </ModulePage>

    <Drawer
      open={drawerOpen}
      onClose={() => setDrawerOpen(false)}
      title={detail ? `${detail.summary.name ?? ""} ${detail.summary.surname ?? ""}`.trim() : "Kullanıcı Ayrıntısı"}
      subtitle={detail?.summary.email ?? undefined}
      width="w-[640px]"
    >
      {detailLoading ? (
        <div className="p-10 text-center text-sm text-gray-400">Yükleniyor...</div>
      ) : detail && (
        <div className="p-6 space-y-6">
          <div className="grid grid-cols-4 gap-3">
            <div className="bg-gray-50 rounded-lg p-3 text-center">
              <p className="text-lg font-bold font-mono text-gray-900">{detail.summary.offer_count}</p>
              <p className="text-[10px] font-semibold text-gray-500 uppercase tracking-wider mt-0.5">Teklif</p>
            </div>
            <div className="bg-gray-50 rounded-lg p-3 text-center">
              <p className="text-lg font-bold font-mono text-gray-900">{detail.summary.load_count}</p>
              <p className="text-[10px] font-semibold text-gray-500 uppercase tracking-wider mt-0.5">Yük</p>
            </div>
            <div className="bg-gray-50 rounded-lg p-3 text-center">
              <p className="text-lg font-bold font-mono text-gray-900">{detail.summary.expedition_movement_count}</p>
              <p className="text-[10px] font-semibold text-gray-500 uppercase tracking-wider mt-0.5">Sefer Hareketi</p>
            </div>
            <div className="bg-gray-50 rounded-lg p-3 text-center">
              <p className="text-lg font-bold font-mono text-gray-900">{detail.summary.account_count}</p>
              <p className="text-[10px] font-semibold text-gray-500 uppercase tracking-wider mt-0.5">Sorumlu Müşteri</p>
            </div>
          </div>

          <UserActivitySection
            icon={FileText}
            title="Son Teklifler"
            emptyText="Bu dönemde teklif bulunamadı."
            rows={detail.recent_offers}
          />
          <UserActivitySection
            icon={Package}
            title="Son Yükler"
            emptyText="Bu dönemde yük bulunamadı."
            rows={detail.recent_loads}
          />

          <div>
            <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider mb-2 flex items-center gap-1.5">
              <Truck size={12} />Son Sefer Hareketleri
            </p>
            {detail.recent_movements.length === 0 ? (
              <p className="text-xs text-gray-400 text-center py-6 bg-gray-50 rounded-lg">Bu dönemde sefer hareketi bulunamadı.</p>
            ) : (
              <div className="space-y-1.5">
                {detail.recent_movements.map((m) => (
                  <div key={m.id} className="border border-gray-100 rounded-lg px-3 py-2 flex items-center justify-between gap-3">
                    <div className="min-w-0">
                      <p className="text-xs font-semibold text-gray-800 truncate">{m.expedition_number ?? `#${m.id}`}</p>
                      <p className="text-[11px] text-gray-500 flex items-center gap-1 mt-0.5">
                        <MapPin size={10} className="shrink-0" />
                        {m.destination_name ?? "—"} {m.status_name ? `· ${m.status_name}` : ""}
                      </p>
                    </div>
                    <span className="text-[10px] text-gray-400 font-mono shrink-0">
                      {m.created_at ? new Date(m.created_at).toLocaleDateString("tr-TR") : "—"}
                    </span>
                  </div>
                ))}
              </div>
            )}
          </div>

          <div>
            <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider mb-2 flex items-center gap-1.5">
              <Building2 size={12} />Sorumlu Müşteriler
            </p>
            {detail.accounts.length === 0 ? (
              <p className="text-xs text-gray-400 text-center py-6 bg-gray-50 rounded-lg">Sorumlu olduğu müşteri bulunamadı.</p>
            ) : (
              <div className="flex flex-wrap gap-1.5">
                {detail.accounts.map((a) => (
                  <span key={a.id} className="text-[11px] px-2.5 py-1 rounded-full bg-gray-100 text-gray-700">{a.name}</span>
                ))}
              </div>
            )}
          </div>
        </div>
      )}
    </Drawer>
    </>
  );
}

function UserActivitySection({
  icon: Icon, title, emptyText, rows,
}: {
  icon: React.ComponentType<{ size?: number; className?: string }>;
  title: string;
  emptyText: string;
  rows: UserActivityRow[];
}) {
  return (
    <div>
      <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider mb-2 flex items-center gap-1.5">
        <Icon size={12} />{title}
      </p>
      {rows.length === 0 ? (
        <p className="text-xs text-gray-400 text-center py-6 bg-gray-50 rounded-lg">{emptyText}</p>
      ) : (
        <div className="space-y-1.5">
          {rows.map((r) => (
            <div key={r.id} className="border border-gray-100 rounded-lg px-3 py-2 flex items-center justify-between gap-3">
              <div className="min-w-0">
                <p className="text-xs font-semibold text-gray-800 truncate">{r.number ?? `#${r.id}`}</p>
                <p className="text-[11px] text-gray-500 truncate mt-0.5">{r.customer_name ?? "—"} {r.status_name ? `· ${r.status_name}` : ""}</p>
              </div>
              <span className="text-[10px] text-gray-400 font-mono shrink-0">
                {r.created_at ? new Date(r.created_at).toLocaleDateString("tr-TR") : "—"}
              </span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
