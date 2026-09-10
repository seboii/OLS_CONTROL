import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Truck, Package, Target, Plus, ChevronRight, Users, FileText,
  Receipt, MapPin, Clock, ArrowUpRight, ArrowDownRight, Minus, Link2,
} from "lucide-react";
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Cell,
} from "recharts";
import { clsx } from "clsx";
import { api, type DataMessage } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { Btn } from "@/components/ui/primitives";
import { useRegisterRefresh } from "@/lib/refresh";

/**
 * OPERASYON PANELİ.
 *
 * Kullanıcı isteği: operasyoncu için en kullanışlı veriler; PARA İLE İŞİ YOK
 * (yalnızca yükün mali kalemini girerken finansa dokunuyor). Yöneticinin
 * kendi paneli ayrıca yapılacak.
 *
 * Üç sayı öne çıkıyor ve ÜÇÜ DE TIKLANABİLİR — sayıyı gören kişinin ilk isteği
 * "hangileri?" olduğu için kart doğrudan o listeye götürüyor:
 *   • Yoldaki Seferler  → /seferler?durum=yolda
 *   • Bu Ay Yükler      → /yukler?donem=bu-ay
 *   • Tamamlanma Oranı  → /seferler
 *
 * Gelir kartı YALNIZCA sunucu izin verirse gelir: tutar, yetkisi olmayana hiç
 * gönderilmiyor (bkz. DashboardMetricsDto.CanSeeRevenue).
 */

interface DashboardMetrics {
  active_expeditions: number;
  on_road_expeditions: number;
  load_transfers_this_month: number;
  load_transfers_this_month_change_percent: number;
  revenue_this_month: number;
  revenue_this_month_change_percent: number;
  pending_quotes: number;
  active_customers: number;
  completion_rate_percent: number;
  expeditions_this_week: number;
  expeditions_last_week: number;
  expeditions_week_change_percent: number;
  can_see_revenue: boolean;
}

interface WeeklyPoint { day: string; completed_count: number }
interface ActivityItem { kind: string; text: string; sub: string; at: string }
interface UpcomingTrip {
  id: number; expedition_number: string | null; route: string | null;
  plate_number: string | null; date: string | null;
  days_left: number | null; status: string | null; driver: string | null;
}

interface DashboardData {
  metrics: DashboardMetrics;
  weekly_completed_trips: WeeklyPoint[];
  recent_activity: ActivityItem[];
  upcoming_trips: UpcomingTrip[];
}

const TOOLTIP_STYLE = {
  contentStyle: {
    background: "#fff", border: "1px solid #E5E7EB", borderRadius: 10,
    fontSize: 12, boxShadow: "0 8px 24px rgba(15,23,42,0.08)",
  },
  labelStyle: { fontWeight: 600, color: "#334155" },
  itemStyle: { color: "#64748B" },
};

/** Aktivite türüne göre renk — göz, listede türü okumadan ayırsın. */
const ACTIVITY_STYLE: Record<string, { dot: string; label: string }> = {
  load_transfer: { dot: "bg-blue-500", label: "text-blue-700 bg-blue-50" },
  offer: { dot: "bg-amber-500", label: "text-amber-700 bg-amber-50" },
  expedition: { dot: "bg-indigo-500", label: "text-indigo-700 bg-indigo-50" },
  account: { dot: "bg-violet-500", label: "text-violet-700 bg-violet-50" },
};

function Trend({ percent }: { percent: number }) {
  const rounded = Math.round(percent);

  if (rounded === 0)
    return (
      <span className="inline-flex items-center gap-0.5 text-[11px] font-semibold text-gray-500">
        <Minus size={11} />değişim yok
      </span>
    );

  const up = rounded > 0;
  const Icon = up ? ArrowUpRight : ArrowDownRight;

  return (
    <span className={clsx(
      "inline-flex items-center gap-0.5 text-[11px] font-semibold",
      up ? "text-emerald-600" : "text-rose-600")}>
      <Icon size={12} />%{Math.abs(rounded)}
    </span>
  );
}

/**
 * Büyük, tıklanabilir sayı kartı. Tıklanabilirliği görünür yapıyor (imleç,
 * kenar vurgusu ve ok) — aksi hâlde kullanıcı kartın bir yere götürdüğünü
 * fark etmiyordu.
 */
function StatCard({ label, value, hint, icon: Icon, tone, onClick, footer }: {
  label: string; value: string; hint: string;
  icon: React.ComponentType<{ size?: number; className?: string }>;
  tone: string; onClick?: () => void; footer?: React.ReactNode;
}) {
  const Tag = onClick ? "button" : "div";

  return (
    <Tag
      onClick={onClick}
      className={clsx(
        "group relative text-left w-full rounded-2xl border border-gray-200 bg-white p-5",
        "shadow-sm transition-all",
        onClick && "hover:border-blue-300 hover:shadow-md cursor-pointer")}
    >
      <div className="flex items-start justify-between">
        <div className={clsx("w-11 h-11 rounded-xl flex items-center justify-center", tone)}>
          <Icon size={20} />
        </div>
        {onClick && (
          <ChevronRight
            size={16}
            className="text-gray-300 group-hover:text-blue-500 group-hover:translate-x-0.5 transition-all"
          />
        )}
      </div>

      <p className="mt-4 text-3xl font-bold tracking-tight text-gray-900 font-mono">{value}</p>
      <p className="mt-1 text-sm font-semibold text-gray-800">{label}</p>
      <p className="mt-0.5 text-[11px] text-gray-400">{hint}</p>

      {footer && <div className="mt-3 pt-3 border-t border-gray-100">{footer}</div>}
    </Tag>
  );
}

export function DashboardPage() {
  const { user, capabilities } = useAuth();
  const navigate = useNavigate();
  const [data, setData] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(true);

  function load() {
    return api.get<DataMessage<DashboardData>>("/api/v1/dashboard")
      .then((res) => setData(res.data))
      .finally(() => setLoading(false));
  }

  useEffect(() => { load(); }, []);

  // Üstteki yenileme düğmesi paneli de tazelesin (bkz. lib/refresh.ts).
  useRegisterRefresh(load);

  const today = new Date().toLocaleDateString("tr-TR", { day: "2-digit", month: "long", year: "numeric" });

  if (loading)
    return <div className="p-6 text-sm text-gray-400">Yükleniyor...</div>;

  if (!data)
    return <div className="p-6 text-sm text-gray-400">Panel verisi yüklenemedi.</div>;

  const m = data.metrics;

  // HIZLI ERİŞİM SABİT: sıra ve içerik veriye göre değişmiyor, kullanıcı kas
  // hafızasıyla aynı yere basabilsin. Yalnızca şirketin kullanmadığı modül
  // (Avrora'da teklif) ve para yetkisi olmayan için finans çıkarılıyor.
  const QUICK_LINKS = [
    { label: "Seferler", path: "/seferler", icon: Truck, desc: `${m.on_road_expeditions} yolda` },
    { label: "Yükler", path: "/yukler", icon: Package, desc: `${m.load_transfers_this_month} bu ay` },
    ...(capabilities.uses_offers
      ? [{ label: "Teklifler", path: "/teklifler", icon: FileText, desc: `${m.pending_quotes} beklemede` }]
      : []),
    { label: "Müşteriler", path: "/musteriler", icon: Users, desc: `${m.active_customers} kayıtlı` },
    ...(m.can_see_revenue
      ? [{ label: "Faturalar", path: "/faturalar", icon: Receipt, desc: "Görüntüle" }]
      : []),
  ];

  const weekBars = data.weekly_completed_trips;
  const weekMax = Math.max(...weekBars.map((d) => d.completed_count), 1);

  return (
    <div className="overflow-y-auto h-full bg-gray-50/60">
      <div className="p-6 space-y-5 max-w-[1400px]">

        {/* Başlık */}
        <div className="flex items-end justify-between flex-wrap gap-3">
          <div>
            <h2 className="text-xl font-bold text-gray-900">
              Merhaba, {user?.name ?? "Kullanıcı"}
            </h2>
            <p className="text-sm text-gray-500 mt-1">
              {today} · şu an <b className="text-gray-700">{m.on_road_expeditions} sefer yolda</b>
            </p>
          </div>
          <div className="flex gap-2">
            {capabilities.uses_offers && (
              <Btn variant="secondary" size="sm" onClick={() => navigate("/teklifler")}>
                <Plus size={13} />Yeni Teklif
              </Btn>
            )}
            <Btn size="sm" onClick={() => navigate("/seferler")}><Truck size={13} />Sefer Aç</Btn>
          </div>
        </div>

        {/* Üç sayı — hepsi tıklanabilir */}
        <div className={clsx("grid gap-4", m.can_see_revenue
          ? "grid-cols-1 sm:grid-cols-2 xl:grid-cols-4"
          : "grid-cols-1 sm:grid-cols-3")}>

          <StatCard
            label="Yoldaki Seferler"
            value={String(m.on_road_expeditions)}
            hint="Çıkmış, henüz boşaltılmadı"
            icon={Truck}
            tone="bg-indigo-50 text-indigo-600"
            onClick={() => navigate("/seferler?durum=yolda")}
          />

          <StatCard
            label="Bu Ay Yükler"
            value={String(m.load_transfers_this_month)}
            hint="Bu ay açılan yük"
            icon={Package}
            tone="bg-blue-50 text-blue-600"
            onClick={() => navigate("/yukler?donem=bu-ay")}
            footer={
              <div className="flex items-center justify-between">
                <span className="text-[11px] text-gray-400">Geçen aya göre</span>
                <Trend percent={m.load_transfers_this_month_change_percent} />
              </div>
            }
          />

          <StatCard
            label="Tamamlanma Oranı"
            value={`%${m.completion_rate_percent}`}
            hint="Bu ayki seferlerden boşaltılanlar"
            icon={Target}
            tone="bg-emerald-50 text-emerald-600"
            onClick={() => navigate("/seferler")}
          />

          {m.can_see_revenue && (
            <StatCard
              label="Aylık Gelir"
              value={`₺${Math.round(m.revenue_this_month).toLocaleString("tr-TR")}`}
              hint="Bu ay faturalanan"
              icon={Receipt}
              tone="bg-amber-50 text-amber-600"
              onClick={() => navigate("/faturalar")}
              footer={
                <div className="flex items-center justify-between">
                  <span className="text-[11px] text-gray-400">Geçen aya göre</span>
                  <Trend percent={m.revenue_this_month_change_percent} />
                </div>
              }
            />
          )}
        </div>

        <div className="grid grid-cols-1 xl:grid-cols-3 gap-4">

          {/* Haftalık sefer oranı */}
          <div className="xl:col-span-2 bg-white rounded-2xl border border-gray-200 shadow-sm p-5">
            <div className="flex items-start justify-between mb-1">
              <div>
                <h3 className="text-sm font-semibold text-gray-900">Bu Haftaki Seferler</h3>
                <p className="text-[11px] text-gray-400 mt-0.5">Gün gün tamamlanan sefer</p>
              </div>
              <div className="text-right">
                <p className="text-2xl font-bold font-mono text-gray-900 leading-none">
                  {m.expeditions_this_week}
                </p>
                <div className="mt-1 flex items-center justify-end gap-1.5">
                  <span className="text-[11px] text-gray-400">
                    geçen hafta {m.expeditions_last_week}
                  </span>
                  <Trend percent={m.expeditions_week_change_percent} />
                </div>
              </div>
            </div>

            <ResponsiveContainer width="100%" height={200}>
              <BarChart data={weekBars} margin={{ top: 16, right: 4, bottom: 0, left: -20 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#F1F5F9" vertical={false} />
                <XAxis dataKey="day" tick={{ fontSize: 11, fill: "#94A3B8" }} axisLine={false} tickLine={false} />
                <YAxis tick={{ fontSize: 11, fill: "#94A3B8" }} axisLine={false} tickLine={false} allowDecimals={false} />
                <Tooltip {...TOOLTIP_STYLE} cursor={{ fill: "#F8FAFC" }} />
                <Bar dataKey="completed_count" radius={[6, 6, 0, 0]} name="Tamamlanan" maxBarSize={38}>
                  {/* En yoğun gün vurgulanıyor — haftanın şeklini tek bakışta okutuyor. */}
                  {weekBars.map((d, i) => (
                    <Cell key={i} fill={d.completed_count === weekMax && weekMax > 0 ? "#2563EB" : "#BFDBFE"} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </div>

          {/* Hızlı erişim — sabit */}
          <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5">
            <h3 className="text-sm font-semibold text-gray-900 mb-3">Hızlı Erişim</h3>
            <div className="space-y-2">
              {QUICK_LINKS.map((link) => {
                const Icon = link.icon;
                return (
                  <button
                    key={link.path}
                    onClick={() => navigate(link.path)}
                    className="w-full flex items-center gap-3 p-3 rounded-xl border border-gray-100 hover:border-blue-200 hover:bg-blue-50/40 transition-all group text-left"
                  >
                    <div className="w-9 h-9 rounded-lg bg-gray-100 group-hover:bg-blue-100 flex items-center justify-center transition-colors shrink-0">
                      <Icon size={15} className="text-gray-500 group-hover:text-blue-600 transition-colors" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-xs font-semibold text-gray-800">{link.label}</p>
                      <p className="text-[10px] text-gray-400">{link.desc}</p>
                    </div>
                    <ChevronRight size={14} className="text-gray-300 group-hover:text-blue-500 transition-colors shrink-0" />
                  </button>
                );
              })}
            </div>
          </div>
        </div>

        <div className="grid grid-cols-1 xl:grid-cols-3 gap-4 pb-2">

          {/* Yaklaşan seferler */}
          <div className="xl:col-span-2 bg-white rounded-2xl border border-gray-200 shadow-sm p-5">
            <div className="flex items-center justify-between mb-3">
              <div>
                <h3 className="text-sm font-semibold text-gray-900">Yaklaşan Seferler</h3>
                <p className="text-[11px] text-gray-400 mt-0.5">Çıkışı en yakın olanlar</p>
              </div>
              <button
                onClick={() => navigate("/seferler")}
                className="text-[11px] text-blue-600 hover:underline flex items-center gap-0.5"
              >
                Tümü <ChevronRight size={11} />
              </button>
            </div>

            {data.upcoming_trips.length === 0 ? (
              <p className="text-xs text-gray-400 text-center py-10">
                Çıkışı planlanmış sefer yok.
              </p>
            ) : (
              <div className="space-y-2">
                {data.upcoming_trips.map((t) => (
                  <button
                    key={t.id}
                    onClick={() => navigate(`/seferler?sefer=${t.id}`)}
                    className="w-full flex items-center gap-3 p-3 rounded-xl border border-gray-100 hover:border-blue-200 hover:bg-blue-50/30 transition-all text-left"
                  >
                    {/* KALAN GÜN önde: operasyoncunun ilk baktığı şey aciliyet. */}
                    <div className={clsx(
                      "w-12 h-12 rounded-xl flex flex-col items-center justify-center shrink-0",
                      t.days_left === 0 ? "bg-rose-50 text-rose-600"
                        : (t.days_left ?? 99) <= 2 ? "bg-amber-50 text-amber-600"
                        : "bg-gray-100 text-gray-500")}>
                      <span className="text-base font-bold leading-none font-mono">
                        {t.days_left ?? "—"}
                      </span>
                      <span className="text-[9px] leading-none mt-0.5">
                        {t.days_left === 0 ? "bugün" : "gün"}
                      </span>
                    </div>

                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <span className="font-mono text-[11px] font-semibold text-blue-600">
                          {t.expedition_number ?? `SEF-${t.id}`}
                        </span>
                        {t.status && (
                          <span className="text-[10px] px-1.5 py-0.5 rounded bg-gray-100 text-gray-600 font-medium">
                            {t.status}
                          </span>
                        )}
                      </div>
                      <div className="flex items-center gap-3 mt-1 text-[11px] text-gray-500">
                        {t.route && (
                          <span className="flex items-center gap-1 truncate">
                            <MapPin size={11} className="shrink-0" />{t.route}
                          </span>
                        )}
                        {t.plate_number && <span className="font-mono shrink-0">{t.plate_number}</span>}
                        {t.driver && <span className="truncate shrink-0">{t.driver}</span>}
                      </div>
                    </div>

                    <div className="text-right shrink-0">
                      {t.date && (
                        <p className="text-[11px] font-mono text-gray-500">
                          {new Date(t.date).toLocaleDateString("tr-TR", { day: "2-digit", month: "short" })}
                        </p>
                      )}
                    </div>
                  </button>
                ))}
              </div>
            )}
          </div>

          {/* Son aktiviteler */}
          <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5">
            <div className="flex items-center justify-between mb-3">
              <h3 className="text-sm font-semibold text-gray-900">Son Hareketler</h3>
              <Clock size={13} className="text-gray-300" />
            </div>

            {data.recent_activity.length === 0 ? (
              <p className="text-xs text-gray-400 text-center py-10">Henüz hareket yok.</p>
            ) : (
              <div className="space-y-3 max-h-[420px] overflow-y-auto pr-1">
                {data.recent_activity.map((act, i) => {
                  const style = ACTIVITY_STYLE[act.kind] ?? ACTIVITY_STYLE.account;
                  return (
                    <div key={i} className="flex gap-2.5">
                      <div className="flex flex-col items-center shrink-0 pt-1">
                        <span className={clsx("w-2 h-2 rounded-full", style.dot)} />
                        {i < data.recent_activity.length - 1 && (
                          <span className="w-px flex-1 bg-gray-100 mt-1" />
                        )}
                      </div>
                      <div className="flex-1 min-w-0 pb-1">
                        <p className="text-[11px] font-medium text-gray-800 leading-snug">{act.text}</p>
                        <div className="flex items-center gap-2 mt-0.5">
                          <span className="text-[10px] text-gray-400 truncate">{act.sub}</span>
                          <span className="text-[10px] text-gray-300 font-mono shrink-0 ml-auto">
                            {new Date(act.at).toLocaleString("tr-TR", {
                              day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit",
                            })}
                          </span>
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </div>

        {/* Yönetici paneli ayrıca yapılacak (kullanıcı isteği). */}
        <p className="text-[11px] text-gray-400 flex items-center gap-1.5 pb-2">
          <Link2 size={11} />Kartlara tıklayarak ilgili listeye gidebilirsiniz.
        </p>
      </div>
    </div>
  );
}
