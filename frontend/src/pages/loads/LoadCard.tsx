import { motion } from "motion/react";
import { CalendarDays, Package, User } from "lucide-react";
import { Badge } from "@/components/ui/primitives";
import { SiberDeletedBadge } from "@/components/shared/SiberAudit";
import type { LoadTransferItem } from "./types";

/**
 * Yük listesi kartı. Tamamen prop'a dayanan gösterim bileşeni; 2.500 satırlık
 * sayfanın içinde durmasının bir sebebi yoktu ve ayrı dosyada test edilebiliyor.
 */
export function LoadCard({ row, index, onClick }: { row: LoadTransferItem; index: number; onClick: () => void }) {
  const loadNumber = row.load_number_work_type ?? row.load_number ?? `Y${row.id}`;
  const date = row.created_at ? new Date(row.created_at).toLocaleDateString("tr-TR") : null;
  const assigned = row.usercode_with_notification?.name?.trim();

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
          <div className="w-9 h-9 rounded-lg bg-blue-50 text-blue-600 flex items-center justify-center shrink-0">
            <Package size={16} />
          </div>
          <div className="min-w-0">
            <p className="font-mono text-xs font-semibold text-blue-600 truncate">{loadNumber}</p>
            {row.siber_deleted_at && <div className="mt-1"><SiberDeletedBadge deletedAt={row.siber_deleted_at} /></div>}
            {date && (
              <p className="text-[10px] text-gray-400 mt-0.5 flex items-center gap-1">
                <CalendarDays size={10} />
                {date}
              </p>
            )}
          </div>
        </div>
        <div className="flex items-center gap-1 shrink-0">
          {row.work_type?.name && (
            <span className="text-[10px] font-medium px-2 py-0.5 rounded-full bg-gray-100 text-gray-600">
              {row.work_type.name}
            </span>
          )}
          {row.load_status_id?.name && <Badge label={row.load_status_id.name} />}
        </div>
      </div>

      <div className="pt-3 border-t border-gray-100">
        <p className="text-[10px] font-semibold text-gray-400 uppercase tracking-wider mb-0.5">Müşteri</p>
        <p className="text-sm font-semibold text-gray-900 truncate">{row.customer_id?.name ?? "—"}</p>
      </div>

      {(row.sender_id?.name || row.receiver_id?.name) && (
        <div className="grid grid-cols-2 gap-3 pt-2.5 border-t border-gray-100">
          <div className="min-w-0">
            <p className="text-[10px] font-semibold text-gray-400 uppercase tracking-wider mb-0.5">Gönderici</p>
            <p className="text-xs text-gray-700 truncate">{row.sender_id?.name ?? "—"}</p>
          </div>
          <div className="min-w-0">
            <p className="text-[10px] font-semibold text-gray-400 uppercase tracking-wider mb-0.5">Alıcı</p>
            <p className="text-xs text-gray-700 truncate">{row.receiver_id?.name ?? "—"}</p>
          </div>
        </div>
      )}

      <div className="flex items-center gap-1.5 text-[11px] text-gray-500 pt-2.5 border-t border-gray-100 min-w-0">
        <User size={12} className="text-gray-400 shrink-0" />
        <span className="truncate">{assigned || "Görevli atanmadı"}</span>
      </div>
    </motion.div>
  );
}
