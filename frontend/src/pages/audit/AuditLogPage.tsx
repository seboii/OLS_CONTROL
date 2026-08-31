import { useCallback, useEffect, useRef, useState } from "react";
import { clsx } from "clsx";
import { ShieldCheck, X, Search } from "lucide-react";
import { api, type DataMessage, type Paginated } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useDebouncedValue } from "@/lib/hooks";
import { ModulePage } from "@/components/ui/ModulePage";
import { EmptyState } from "@/components/ui/DataTable";

interface AuditLogRow {
  id: number;
  user_id: number | null;
  user_name: string | null;
  action: string;
  entity_type: string;
  entity_id: string | null;
  entity_label: string | null;
  changes: string | null;
  ip_address: string | null;
  created_at: string;
}

interface AuditTarget {
  label: string;
  type: string;
  hint: string | null;
}

/** Anlık takip aralığı. */
const POLL_MS = 5000;
const PAGE_SIZE = 50;

const ACTION_LABELS: Record<string, string> = {
  created: "Oluşturdu",
  updated: "Güncelledi",
  deleted: "Sildi",
};

const ACTION_STYLES: Record<string, string> = {
  created: "bg-emerald-50 text-emerald-700 border-emerald-200",
  updated: "bg-blue-50 text-blue-700 border-blue-200",
  deleted: "bg-red-50 text-red-700 border-red-200",
};

function formatTime(value: string): string {
  const date = new Date(value);
  return `${date.toLocaleDateString("tr-TR")} ${date.toLocaleTimeString("tr-TR", {
    hour: "2-digit", minute: "2-digit", second: "2-digit",
  })}`;
}

/**
 * Değişiklik özeti. Sunucu ham JSON tutuyor ({alan: {onceki, sonraki}} ya da
 * oluşturmada {alan: deger}); burada okunabilir tek satıra indirgenir. Tüm
 * alanları basmak yerine ilk birkaçı gösterilir — denetim listesi taranmak için
 * var, ayrıntı satırın kendisinde açılıyor.
 */
function summarizeChanges(raw: string | null): { short: string; full: string[] } {
  if (!raw) return { short: "—", full: [] };

  try {
    const parsed = JSON.parse(raw) as Record<string, unknown>;
    const lines = Object.entries(parsed).map(([field, value]) => {
      if (value && typeof value === "object" && "onceki" in (value as object)) {
        const v = value as { onceki?: string | null; sonraki?: string | null };
        return `${field}: ${v.onceki ?? "—"} → ${v.sonraki ?? "—"}`;
      }
      return `${field}: ${String(value)}`;
    });

    return {
      short: lines.slice(0, 2).join(" · ") + (lines.length > 2 ? ` (+${lines.length - 2})` : ""),
      full: lines,
    };
  } catch {
    return { short: "—", full: [] };
  }
}

export function AuditLogPage() {
  const { can } = useAuth();
  const canRead = can("audit_log_management", "read");

  const [rows, setRows] = useState<AuditLogRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [live, setLive] = useState(true);
  const [expanded, setExpanded] = useState<number | null>(null);

  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);
  const [targets, setTargets] = useState<AuditTarget[]>([]);
  const [selectedTarget, setSelectedTarget] = useState<AuditTarget | null>(null);
  const [suggestOpen, setSuggestOpen] = useState(false);

  // En büyük id ref'te tutuluyor: poll döngüsü her turda state'e bağımlı
  // olmadan "bundan sonrasını getir" diyebilsin (aksi hâlde effect her yeni
  // satırda yeniden kurulur ve zamanlayıcı sürekli sıfırlanırdı).
  const newestIdRef = useRef<number>(0);

  const loadInitial = useCallback(async () => {
    setLoading(true);
    try {
      const res = await api.get<DataMessage<Paginated<AuditLogRow>>>("/api/v1/audit_log", {
        entity_label: selectedTarget?.label || undefined,
        per_page: PAGE_SIZE,
        page: 1,
      });
      setRows(res.data.data);
      newestIdRef.current = res.data.data[0]?.id ?? 0;
    } catch {
      setRows([]);
    } finally {
      setLoading(false);
    }
  }, [selectedTarget]);

  useEffect(() => {
    if (canRead) loadInitial();
  }, [canRead, loadInitial]);

  // Anlık takip: yalnızca yeni satırları ister (after_id), listenin başına ekler.
  useEffect(() => {
    if (!canRead || !live) return;

    const timer = setInterval(async () => {
      try {
        const res = await api.get<DataMessage<AuditLogRow[]>>("/api/v1/audit_log", {
          entity_label: selectedTarget?.label || undefined,
          after_id: newestIdRef.current || undefined,
        });

        const fresh = res.data;
        if (!Array.isArray(fresh) || fresh.length === 0) return;

        newestIdRef.current = Math.max(newestIdRef.current, ...fresh.map((r) => r.id));
        setRows((current) => [...fresh, ...current].slice(0, 300));
      } catch {
        // Sessiz geç: anlık takip kesintisi ekranı bozmamalı.
      }
    }, POLL_MS);

    return () => clearInterval(timer);
  }, [canRead, live, selectedTarget]);

  // Arama önerileri: yük numarası / sefer numarası / kullanıcı / cari.
  useEffect(() => {
    if (!canRead || selectedTarget || debouncedSearch.trim().length < 2) {
      setTargets([]);
      return;
    }

    api
      .get<DataMessage<AuditTarget[]>>("/api/v1/audit_log/targets", { search: debouncedSearch })
      .then((res) => setTargets(res.data))
      .catch(() => setTargets([]));
  }, [canRead, debouncedSearch, selectedTarget]);

  if (!canRead) {
    return (
      <EmptyState
        icon={ShieldCheck}
        title="Yetkiniz yok"
        desc="Denetim kaydı yalnızca yöneticiye açıktır."
      />
    );
  }

  return (
    <ModulePage title="Denetim Kaydı">
      <div className="p-6 space-y-4">
        <div className="flex flex-wrap items-center gap-3">
          <div className="relative flex-1 min-w-[260px]">
            {selectedTarget ? (
              <div className="flex items-center gap-2 rounded-md border border-blue-200 bg-blue-50 px-3 py-2">
                <span className="text-[11px] font-semibold uppercase tracking-wider text-blue-600">
                  {selectedTarget.type}
                </span>
                <span className="text-sm font-medium text-blue-900">{selectedTarget.label}</span>
                <button
                  type="button"
                  onClick={() => { setSelectedTarget(null); setSearch(""); }}
                  className="ml-auto text-blue-400 hover:text-blue-700"
                  title="Seçimi kaldır"
                >
                  <X size={14} />
                </button>
              </div>
            ) : (
              <>
                <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
                <input
                  type="text"
                  value={search}
                  onChange={(e) => { setSearch(e.target.value); setSuggestOpen(true); }}
                  onFocus={() => setSuggestOpen(true)}
                  placeholder="Yük no, sefer no, kullanıcı veya cari ara…"
                  className="w-full pl-9 pr-3 py-2 text-sm border border-gray-200 rounded-md bg-white focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500"
                />
                {suggestOpen && targets.length > 0 && (
                  <div className="absolute z-20 mt-1 w-full bg-white border border-gray-200 rounded-md shadow-lg max-h-64 overflow-y-auto">
                    {targets.map((t) => (
                      <button
                        key={`${t.type}-${t.label}`}
                        type="button"
                        onClick={() => { setSelectedTarget(t); setSuggestOpen(false); }}
                        className="w-full text-left px-3 py-2 text-sm text-gray-700 hover:bg-gray-50"
                      >
                        <span className="font-medium">{t.label}</span>
                        <span className="ml-2 text-[11px] text-gray-400">{t.type}</span>
                        {t.hint && <span className="block text-[11px] text-gray-400">{t.hint}</span>}
                      </button>
                    ))}
                  </div>
                )}
              </>
            )}
          </div>

          <label className="flex items-center gap-2 text-xs text-gray-600 select-none">
            <input
              type="checkbox"
              checked={live}
              onChange={(e) => setLive(e.target.checked)}
              className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
            />
            Anlık takip
            <span className={clsx(
              "inline-block w-2 h-2 rounded-full",
              live ? "bg-emerald-500 animate-pulse" : "bg-gray-300",
            )} />
          </label>
        </div>

        {loading ? (
          <p className="text-sm text-gray-400 text-center py-12">Yükleniyor…</p>
        ) : rows.length === 0 ? (
          <EmptyState
            icon={ShieldCheck}
            title={selectedTarget ? "Bu kayıtta işlem yok" : "Henüz kayıt yok"}
            desc={selectedTarget
              ? `${selectedTarget.label} üzerinde kullanıcı işlemi bulunamadı.`
              : "Kullanıcılar işlem yaptıkça burada anlık olarak görünecek."}
          />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-xs border-collapse">
              <thead>
                <tr className="border-b border-gray-200">
                  <th className="text-left py-2 pr-3 text-gray-500 font-semibold uppercase tracking-wide">Zaman</th>
                  <th className="text-left py-2 pr-3 text-gray-500 font-semibold uppercase tracking-wide">Kullanıcı</th>
                  <th className="text-left py-2 pr-3 text-gray-500 font-semibold uppercase tracking-wide">İşlem</th>
                  <th className="text-left py-2 pr-3 text-gray-500 font-semibold uppercase tracking-wide">Kayıt</th>
                  <th className="text-left py-2 pr-3 text-gray-500 font-semibold uppercase tracking-wide">Değişiklik</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => {
                  const changes = summarizeChanges(row.changes);
                  const open = expanded === row.id;
                  return (
                    <tr
                      key={row.id}
                      onClick={() => setExpanded(open ? null : row.id)}
                      className="border-b border-gray-100 hover:bg-gray-50 cursor-pointer align-top"
                    >
                      <td className="py-2 pr-3 whitespace-nowrap text-gray-500 tabular-nums">
                        {formatTime(row.created_at)}
                      </td>
                      <td className="py-2 pr-3 text-gray-800">
                        {row.user_name ?? `#${row.user_id ?? "?"}`}
                        {row.ip_address && (
                          <span className="block text-[10px] text-gray-400">{row.ip_address}</span>
                        )}
                      </td>
                      <td className="py-2 pr-3">
                        <span className={clsx(
                          "inline-block rounded border px-1.5 py-0.5 text-[10px] font-medium",
                          ACTION_STYLES[row.action] ?? "bg-gray-50 text-gray-600 border-gray-200",
                        )}>
                          {ACTION_LABELS[row.action] ?? row.action}
                        </span>
                      </td>
                      <td className="py-2 pr-3">
                        <span className="font-medium text-gray-800">{row.entity_label ?? "—"}</span>
                        <span className="block text-[10px] text-gray-400">{row.entity_type}</span>
                      </td>
                      <td className="py-2 pr-3 text-gray-600">
                        {open && changes.full.length > 0 ? (
                          <ul className="space-y-0.5">
                            {changes.full.map((line, i) => (
                              <li key={i} className="font-mono text-[11px]">{line}</li>
                            ))}
                          </ul>
                        ) : (
                          <span className="font-mono text-[11px]">{changes.short}</span>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </ModulePage>
  );
}
