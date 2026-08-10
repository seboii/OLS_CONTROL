import { useEffect, useRef, useState, type ReactNode } from "react";
import { motion, AnimatePresence } from "motion/react";
import { ArrowUpDown, ChevronLeft, ChevronRight, Edit2, Eye, MoreHorizontal, Trash2 } from "lucide-react";
import { clsx } from "clsx";

export interface Column<T> {
  key: string;
  header: string;
  sortable?: boolean;
  render: (row: T) => ReactNode;
  width?: string;
}

export function DataTable<T extends { id: string | number }>({
  data,
  columns,
  onRowClick,
  actions,
  loading,
}: {
  data: T[];
  columns: Column<T>[];
  onRowClick?: (row: T) => void;
  actions?: (row: T) => ReactNode;
  loading?: boolean;
}) {
  const [sortKey, setSortKey] = useState("");
  const [sortDir, setSortDir] = useState<"asc" | "desc">("asc");

  const sorted = [...data].sort((a, b) => {
    if (!sortKey) return 0;
    const av = String((a as Record<string, unknown>)[sortKey] ?? "");
    const bv = String((b as Record<string, unknown>)[sortKey] ?? "");
    return sortDir === "asc" ? av.localeCompare(bv, "tr") : bv.localeCompare(av, "tr");
  });

  if (loading) {
    return (
      <div className="overflow-x-auto">
        <table className="w-full border-collapse">
          <thead>
            <tr className="border-b border-gray-200 bg-gray-50/80">
              {columns.map((c) => (
                <th
                  key={c.key}
                  className="text-left py-2.5 px-3 text-[11px] font-semibold text-gray-400 uppercase tracking-wider"
                >
                  {c.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {Array.from({ length: 5 }).map((_, i) => (
              <tr key={i} className="border-b border-gray-100">
                {columns.map((c) => (
                  <td key={c.key} className="py-3 px-3">
                    <div
                      className="h-3 bg-gray-200 rounded animate-pulse"
                      style={{ width: `${60 + Math.random() * 30}%` }}
                    />
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    );
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full border-collapse">
        <thead>
          <tr className="border-b border-gray-200 bg-gray-50/80">
            {columns.map((c) => (
              <th
                key={c.key}
                className={clsx(
                  "text-left py-2.5 px-3 text-[11px] font-semibold text-gray-500 uppercase tracking-wider select-none",
                  c.sortable && "cursor-pointer hover:text-gray-700 hover:bg-gray-100 transition-colors",
                  c.width,
                )}
                onClick={() => {
                  if (!c.sortable) return;
                  if (sortKey === c.key) setSortDir((d) => (d === "asc" ? "desc" : "asc"));
                  else {
                    setSortKey(c.key);
                    setSortDir("asc");
                  }
                }}
              >
                <span className="flex items-center gap-1">
                  {c.header}
                  {c.sortable && (
                    <ArrowUpDown size={10} className={clsx(sortKey === c.key ? "text-blue-600" : "text-gray-300")} />
                  )}
                </span>
              </th>
            ))}
            {actions && <th className="w-10 py-2.5 px-3" />}
          </tr>
        </thead>
        <tbody>
          {sorted.map((row, i) => (
            <motion.tr
              key={row.id}
              initial={{ opacity: 0, y: 4 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.15, delay: i * 0.02 }}
              className={clsx(
                "border-b border-gray-100 transition-colors",
                onRowClick && "cursor-pointer",
                i % 2 === 0 ? "bg-white" : "bg-gray-50/40",
                onRowClick && "hover:bg-blue-50/40",
              )}
              onClick={() => onRowClick?.(row)}
            >
              {columns.map((c) => (
                <td key={c.key} className="py-2.5 px-3 text-sm text-gray-800">
                  {c.render(row)}
                </td>
              ))}
              {actions && (
                <td className="py-2 px-3" onClick={(e) => e.stopPropagation()}>
                  {actions(row)}
                </td>
              )}
            </motion.tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function EmptyState({
  icon: Icon,
  title,
  desc,
}: {
  icon: React.ComponentType<{ size?: number; className?: string }>;
  title: string;
  desc: string;
}) {
  return (
    <div className="flex flex-col items-center justify-center py-20 text-center px-4">
      <div className="p-4 bg-gray-100 rounded-full mb-3">
        <Icon size={26} className="text-gray-400" />
      </div>
      <h3 className="text-sm font-semibold text-gray-700 mb-1">{title}</h3>
      <p className="text-xs text-gray-400 max-w-xs">{desc}</p>
    </div>
  );
}

export function Pagination({
  page,
  total,
  perPage,
  onChange,
}: {
  page: number;
  total: number;
  perPage: number;
  onChange: (p: number) => void;
}) {
  const pages = Math.max(1, Math.ceil(total / perPage));
  const start = total === 0 ? 0 : (page - 1) * perPage + 1;
  const end = Math.min(page * perPage, total);
  const pageNums = Array.from({ length: pages }, (_, i) => i + 1).filter(
    (p) => p === 1 || p === pages || Math.abs(p - page) <= 1,
  );

  return (
    <div className="flex items-center justify-between px-4 py-3 border-t border-gray-200 bg-white shrink-0">
      <span className="text-xs text-gray-500 font-mono">
        {total} kayıt · {start}–{end} gösteriliyor
      </span>
      <div className="flex items-center gap-1">
        <button
          onClick={() => onChange(page - 1)}
          disabled={page === 1}
          className="p-1.5 rounded text-gray-500 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
        >
          <ChevronLeft size={13} />
        </button>
        {pageNums.map((p, i) => (
          <span key={p}>
            {i > 0 && pageNums[i - 1] !== p - 1 && <span className="px-1 text-gray-300 text-xs">…</span>}
            <button
              onClick={() => onChange(p)}
              className={clsx(
                "w-7 h-7 text-xs rounded font-mono transition-colors",
                p === page ? "bg-blue-600 text-white shadow-sm" : "text-gray-600 hover:bg-gray-100",
              )}
            >
              {p}
            </button>
          </span>
        ))}
        <button
          onClick={() => onChange(page + 1)}
          disabled={page === pages}
          className="p-1.5 rounded text-gray-500 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
        >
          <ChevronRight size={13} />
        </button>
      </div>
    </div>
  );
}

export function RowActions({
  onView,
  onEdit,
  onDelete,
}: {
  onView?: () => void;
  onEdit?: () => void;
  onDelete?: () => void;
}) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const h = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", h);
    return () => document.removeEventListener("mousedown", h);
  }, []);

  return (
    <div className="relative" ref={ref}>
      <button
        onClick={() => setOpen((o) => !o)}
        className="p-1.5 rounded text-gray-400 hover:bg-gray-100 hover:text-gray-700 transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500"
      >
        <MoreHorizontal size={14} />
      </button>
      <AnimatePresence>
        {open && (
          <motion.div
            initial={{ opacity: 0, scale: 0.92, y: -4 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.92 }}
            transition={{ duration: 0.12, ease: "easeOut" }}
            className="absolute right-0 top-8 w-36 bg-white rounded-lg shadow-lg border border-gray-200 py-1 z-20"
          >
            {onView && (
              <button
                onClick={() => {
                  onView();
                  setOpen(false);
                }}
                className="flex items-center gap-2 w-full px-3 py-1.5 text-xs text-gray-700 hover:bg-gray-50 transition-colors"
              >
                <Eye size={12} />
                Görüntüle
              </button>
            )}
            {onEdit && (
              <button
                onClick={() => {
                  onEdit();
                  setOpen(false);
                }}
                className="flex items-center gap-2 w-full px-3 py-1.5 text-xs text-gray-700 hover:bg-gray-50 transition-colors"
              >
                <Edit2 size={12} />
                Düzenle
              </button>
            )}
            {onDelete && (
              <button
                onClick={() => {
                  onDelete();
                  setOpen(false);
                }}
                className="flex items-center gap-2 w-full px-3 py-1.5 text-xs text-red-600 hover:bg-red-50 transition-colors"
              >
                <Trash2 size={12} />
                Sil
              </button>
            )}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
