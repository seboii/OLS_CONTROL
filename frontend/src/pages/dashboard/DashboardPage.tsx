import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Truck, Package, TrendingUp, FileText, Users, Target, Plus, Receipt,
  ChevronRight, AlertTriangle, CheckCircle,
} from "lucide-react";
import {
  AreaChart, Area, BarChart, Bar, PieChart, Pie, Cell,
  XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
} from "recharts";
import { clsx } from "clsx";
import { api, type DataMessage } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { Btn, Badge } from "@/components/ui/primitives";

interface DashboardMetrics {
  active_expeditions: number;
  active_expeditions_delta: number;
  load_transfers_this_month: number;
  load_transfers_this_month_change_percent: number;
  revenue_this_month: number;
  revenue_this_month_change_percent: number;
  pending_quotes: number;
  pending_quotes_delta: number;
  active_customers: number;
  active_customers_delta: number;
  completion_rate_percent: number;
  completion_rate_delta_points: number;
}

interface MonthlyPoint { month: string; shipment_count: number; revenue: number }
interface DistributionSlice { name: string; count: number; percent: number }
interface WeeklyPoint { day: string; completed_count: number }
interface ActivityItem { kind: string; text: string; sub: string; at: string }
interface UpcomingTrip {
  id: number; expedition_number: string | null; route: string | null;
  plate_number: string | null; date: string | null;
}

interface DashboardData {
  metrics: DashboardMetrics;
  monthly_shipments: MonthlyPoint[];
  work_type_distribution: DistributionSlice[];
  weekly_completed_trips: WeeklyPoint[];
  recent_activity: ActivityItem[];
  upcoming_trips: UpcomingTrip[];
}

const TOOLTIP_STYLE = {
  contentStyle: { background: "#fff", border: "1px solid #E5E7EB", borderRadius: 8, fontSize: 12, boxShadow: "0 4px 12px rgba(0,0,0,0.08)" },
  labelStyle: { fontWeight: 600, color: "#374151" },
  itemStyle: { color: "#6B7280" },
};

const SLICE_COLORS = ["#2563EB", "#7C3AED", "#0891B2", "#059669", "#D97706"];

function MetricCard({ label, value, sub, trend, trendUp, icon: Icon, color }: {
  label: string; value: string; sub: string; trend?: string; trendUp?: boolean;
  icon: React.ComponentType<{ size?: number; className?: string }>; color: string;
}) {
  return (
    <div className="bg-white rounded-xl p-4 border border-gray-200 shadow-sm hover:shadow-md transition-shadow">
      <div className="flex items-start justify-between mb-3">
        <div className={clsx("w-9 h-9 rounded-lg flex items-center justify-center", color)}>
          <Icon size={17} />
        </div>
        {trend && (
          <div className={clsx("flex items-center gap-1 text-[11px] font-semibold px-1.5 py-0.5 rounded-md",
            trendUp ? "text-emerald-700 bg-emerald-50" : "text-red-600 bg-red-50")}>
            {trend}
          </div>
        )}
      </div>
      <p className="text-2xl font-bold text-gray-900 font-mono tracking-tight">{value}</p>
      <p className="text-xs font-semibold text-gray-700 mt-0.5">{label}</p>
      <p className="text-[11px] text-gray-400 mt-0.5">{sub}</p>
    </div>
  );
}

function formatEuroLike(n: number) {
  return `₺${Math.round(n).toLocaleString("tr-TR")}`;
}

function formatSigned(n: number, suffix: string) {
  if (n === 0) return undefined;
  return `${n > 0 ? "+" : ""}${n}${suffix}`;
}

export function DashboardPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [data, setData] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.get<DataMessage<DashboardData>>("/api/v1/dashboard")
      .then((res) => setData(res.data))
      .finally(() => setLoading(false));
  }, []);

  const today = new Date().toLocaleDateString("tr-TR", { day: "2-digit", month: "long", year: "numeric" });

  if (loading) {
    return <div className="p-6 text-sm text-gray-400">Yükleniyor...</div>;
  }

  if (!data) {
    return <div className="p-6 text-sm text-gray-400">Panel verisi yüklenemedi.</div>;
  }

  const m = data.metrics;

  const METRICS = [
    { label: "Aktif Seferler", value: String(m.active_expeditions), sub: "Dönüşü girilmemiş", trend: formatSigned(m.active_expeditions_delta, ""), trendUp: m.active_expeditions_delta >= 0, icon: Truck, color: "bg-indigo-50 text-indigo-600" },
    { label: "Bu Ay Yükler", value: String(m.load_transfers_this_month), sub: "Bu ay oluşturulan", trend: formatSigned(m.load_transfers_this_month_change_percent, "%"), trendUp: m.load_transfers_this_month_change_percent >= 0, icon: Package, color: "bg-blue-50 text-blue-600" },
    { label: "Aylık Gelir", value: formatEuroLike(m.revenue_this_month), sub: "Faturalanan tutar", trend: formatSigned(m.revenue_this_month_change_percent, "%"), trendUp: m.revenue_this_month_change_percent >= 0, icon: TrendingUp, color: "bg-emerald-50 text-emerald-600" },
    { label: "Bekleyen Teklifler", value: String(m.pending_quotes), sub: "Teklif durumunda", trend: undefined, trendUp: true, icon: FileText, color: "bg-amber-50 text-amber-600" },
    { label: "Aktif Müşteriler", value: String(m.active_customers), sub: "Toplam kayıtlı", trend: formatSigned(m.active_customers_delta, ""), trendUp: m.active_customers_delta >= 0, icon: Users, color: "bg-violet-50 text-violet-600" },
    { label: "Tamamlanma Oranı", value: `%${m.completion_rate_percent}`, sub: "Bu ay, dönüşü girilenler", trend: undefined, trendUp: true, icon: Target, color: "bg-rose-50 text-rose-600" },
  ];

  const QUICK_LINKS = [
    { label: "Müşteriler", path: "/musteriler", icon: Users, desc: `${m.active_customers} kayıtlı` },
    { label: "Teklifler", path: "/teklifler", icon: FileText, desc: `${m.pending_quotes} beklemede` },
    { label: "Yükler", path: "/yukler", icon: Package, desc: `${m.load_transfers_this_month} bu ay` },
    { label: "Faturalar", path: "/faturalar", icon: Receipt, desc: "Görüntüle" },
  ];

  return (
    <div className="overflow-y-auto h-full">
      <div className="p-6 space-y-6 max-w-[1400px]">
        {/* Welcome bar */}
        <div className="flex items-center justify-between flex-wrap gap-3">
          <div>
            <h2 className="text-lg font-bold text-gray-900">
              Merhaba, {user?.name ?? "Kullanıcı"} 👋
            </h2>
            <p className="text-sm text-gray-500 mt-0.5">
              {today} · Şu an {m.active_expeditions} aktif sefer var
            </p>
          </div>
          <div className="flex gap-2">
            <Btn variant="secondary" size="sm" onClick={() => navigate("/teklifler")}><Plus size={13} />Yeni Teklif</Btn>
            <Btn size="sm" onClick={() => navigate("/seferler")}><Truck size={13} />Sefer Ekle</Btn>
          </div>
        </div>

        {/* KPI row */}
        <div className="grid grid-cols-2 sm:grid-cols-3 xl:grid-cols-6 gap-3">
          {METRICS.map((metric) => (
            <MetricCard key={metric.label} {...metric} />
          ))}
        </div>

        {/* Charts row 1 */}
        <div className="grid grid-cols-1 xl:grid-cols-3 gap-4">
          <div className="xl:col-span-2 bg-white rounded-xl border border-gray-200 shadow-sm p-5">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h3 className="text-sm font-semibold text-gray-900">Aylık Sevkiyat & Gelir</h3>
                <p className="text-[11px] text-gray-400 mt-0.5">Son 6 ay · Adet ve TL</p>
              </div>
              <div className="flex items-center gap-3 text-[11px]">
                <span className="flex items-center gap-1.5"><span className="w-3 h-0.5 bg-blue-500 rounded inline-block" />Sevkiyat</span>
                <span className="flex items-center gap-1.5"><span className="w-3 h-0.5 bg-emerald-500 rounded inline-block" />Gelir</span>
              </div>
            </div>
            <ResponsiveContainer width="100%" height={190}>
              <AreaChart data={data.monthly_shipments} margin={{ top: 4, right: 4, bottom: 0, left: -16 }}>
                <defs>
                  <linearGradient id="gradBlue" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stopColor="#2563EB" stopOpacity={0.15} />
                    <stop offset="100%" stopColor="#2563EB" stopOpacity={0} />
                  </linearGradient>
                  <linearGradient id="gradGreen" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stopColor="#10B981" stopOpacity={0.12} />
                    <stop offset="100%" stopColor="#10B981" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="#F3F4F6" vertical={false} />
                <XAxis dataKey="month" tick={{ fontSize: 11, fill: "#9CA3AF" }} axisLine={false} tickLine={false} />
                <YAxis tick={{ fontSize: 11, fill: "#9CA3AF" }} axisLine={false} tickLine={false} />
                <Tooltip {...TOOLTIP_STYLE} />
                <Area type="monotone" dataKey="shipment_count" stroke="#2563EB" strokeWidth={2} fill="url(#gradBlue)" name="Sevkiyat" />
                <Area type="monotone" dataKey="revenue" stroke="#10B981" strokeWidth={2} fill="url(#gradGreen)" name="Gelir" />
              </AreaChart>
            </ResponsiveContainer>
          </div>

          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
            <div className="mb-4">
              <h3 className="text-sm font-semibold text-gray-900">İş Tipi Dağılımı</h3>
              <p className="text-[11px] text-gray-400 mt-0.5">Yük bazında</p>
            </div>
            {data.work_type_distribution.length === 0 ? (
              <p className="text-xs text-gray-400 text-center py-10">Henüz yük kaydı yok.</p>
            ) : (
              <>
                <ResponsiveContainer width="100%" height={140}>
                  <PieChart>
                    <Pie data={data.work_type_distribution} cx="50%" cy="50%" innerRadius={42} outerRadius={62}
                      paddingAngle={3} dataKey="count" nameKey="name" stroke="none">
                      {data.work_type_distribution.map((_, i) => (
                        <Cell key={i} fill={SLICE_COLORS[i % SLICE_COLORS.length]} />
                      ))}
                    </Pie>
                    <Tooltip {...TOOLTIP_STYLE} formatter={(v) => [`%${v}`, ""]} />
                  </PieChart>
                </ResponsiveContainer>
                <div className="space-y-2 mt-2">
                  {data.work_type_distribution.map((item, i) => (
                    <div key={item.name} className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <span className="w-2.5 h-2.5 rounded-full shrink-0" style={{ backgroundColor: SLICE_COLORS[i % SLICE_COLORS.length] }} />
                        <span className="text-[11px] text-gray-600">{item.name}</span>
                      </div>
                      <span className="text-[11px] font-semibold font-mono text-gray-800">%{item.percent}</span>
                    </div>
                  ))}
                </div>
              </>
            )}
          </div>
        </div>

        {/* Charts row 2 */}
        <div className="grid grid-cols-1 xl:grid-cols-3 gap-4">
          <div className="xl:col-span-2 bg-white rounded-xl border border-gray-200 shadow-sm p-5">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h3 className="text-sm font-semibold text-gray-900">Bu Hafta Tamamlanan Seferler</h3>
                <p className="text-[11px] text-gray-400 mt-0.5">Dönüş tarihi bu haftaya düşenler</p>
              </div>
            </div>
            <ResponsiveContainer width="100%" height={175}>
              <BarChart data={data.weekly_completed_trips} margin={{ top: 4, right: 4, bottom: 0, left: -16 }} barGap={3}>
                <CartesianGrid strokeDasharray="3 3" stroke="#F3F4F6" vertical={false} />
                <XAxis dataKey="day" tick={{ fontSize: 11, fill: "#9CA3AF" }} axisLine={false} tickLine={false} />
                <YAxis tick={{ fontSize: 11, fill: "#9CA3AF" }} axisLine={false} tickLine={false} allowDecimals={false} />
                <Tooltip {...TOOLTIP_STYLE} />
                <Bar dataKey="completed_count" fill="#2563EB" radius={[4, 4, 0, 0]} name="Tamamlanan" maxBarSize={28} />
              </BarChart>
            </ResponsiveContainer>
          </div>

          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-sm font-semibold text-gray-900">Son Aktiviteler</h3>
            </div>
            {data.recent_activity.length === 0 ? (
              <p className="text-xs text-gray-400 text-center py-10">Henüz aktivite yok.</p>
            ) : (
              <div className="space-y-3">
                {data.recent_activity.map((act, i) => (
                  <div key={i} className="flex items-start gap-2.5">
                    <div className="w-7 h-7 rounded-lg flex items-center justify-center shrink-0 mt-0.5 bg-blue-50 text-blue-600">
                      <CheckCircle size={12} />
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-[11px] font-medium text-gray-800 leading-snug">{act.text}</p>
                      <p className="text-[10px] text-gray-400 mt-0.5 truncate">{act.sub}</p>
                    </div>
                    <span className="text-[10px] text-gray-400 font-mono shrink-0">
                      {new Date(act.at).toLocaleDateString("tr-TR", { day: "2-digit", month: "2-digit" })}
                    </span>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Bottom row */}
        <div className="grid grid-cols-1 xl:grid-cols-3 gap-4 pb-2">
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
            <h3 className="text-sm font-semibold text-gray-900 mb-3">Hızlı Erişim</h3>
            <div className="grid grid-cols-2 gap-2">
              {QUICK_LINKS.map((link) => {
                const Icon = link.icon;
                return (
                  <button key={link.path} onClick={() => navigate(link.path)}
                    className="flex items-center gap-2.5 p-3 rounded-lg border border-gray-100 hover:border-blue-200 hover:bg-blue-50/30 transition-all group text-left">
                    <div className="w-7 h-7 rounded-lg bg-gray-100 group-hover:bg-blue-100 flex items-center justify-center transition-colors">
                      <Icon size={13} className="text-gray-500 group-hover:text-blue-600 transition-colors" />
                    </div>
                    <div>
                      <p className="text-xs font-semibold text-gray-800">{link.label}</p>
                      <p className="text-[10px] text-gray-400">{link.desc}</p>
                    </div>
                  </button>
                );
              })}
            </div>
          </div>

          <div className="xl:col-span-2 bg-white rounded-xl border border-gray-200 shadow-sm p-5">
            <div className="flex items-center justify-between mb-3">
              <h3 className="text-sm font-semibold text-gray-900">Yaklaşan Seferler</h3>
              <button onClick={() => navigate("/seferler")}
                className="text-[11px] text-blue-600 hover:underline flex items-center gap-0.5">
                Tümünü gör <ChevronRight size={11} />
              </button>
            </div>
            {data.upcoming_trips.length === 0 ? (
              <div className="flex items-center gap-2 text-xs text-gray-400 py-6 justify-center">
                <AlertTriangle size={13} />Yaklaşan sefer yok.
              </div>
            ) : (
              <div className="space-y-2.5">
                {data.upcoming_trips.map((t) => (
                  <div key={t.id}
                    className="flex items-center gap-3 p-3 rounded-lg border border-gray-100 hover:border-gray-200 hover:bg-gray-50/60 transition-all cursor-pointer"
                    onClick={() => navigate("/seferler")}>
                    <div className="w-8 h-8 rounded-lg bg-indigo-50 flex items-center justify-center shrink-0">
                      <Truck size={14} className="text-indigo-600" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <span className="font-mono text-[11px] text-blue-600">{t.expedition_number ?? `SEF-${t.id}`}</span>
                        {t.route && <span className="text-[11px] text-gray-700 font-medium truncate">{t.route}</span>}
                      </div>
                      {t.plate_number && <p className="text-[11px] text-gray-400 mt-0.5">{t.plate_number}</p>}
                    </div>
                    <div className="text-right shrink-0">
                      {t.date && <p className="text-[11px] font-mono text-gray-500">{t.date}</p>}
                      <Badge label="Beklemede" />
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
