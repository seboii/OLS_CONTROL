import { useEffect, useState } from "react";
import { clsx } from "clsx";
import { History, Plus, PencilLine, Trash2 } from "lucide-react";
import { api, type DataMessage } from "@/lib/api";
import { EmptyState } from "@/components/ui/DataTable";

/**
 * Bir kaydın TAM işlem geçmişi — Siber'in kendi değişiklik günlüğünden.
 *
 * Kayıt üstündeki "açan / son işlem" bilgisi yalnızca iki noktayı verir;
 * buradaki liste aradaki her işlemi, hangi alanın hangi değerden hangi değere
 * geçtiğiyle gösterir.
 */
export interface RecordFieldChange {
  field: string;
  old_value: string | null;
  new_value: string | null;
}

export interface RecordHistoryEntry {
  id: number;
  changed_at: string | null;
  user_code: string | null;
  user_name: string | null;
  operation: number | null;
  operation_label: string;
  module: string | null;
  changes: RecordFieldChange[];
  changes_unparsed: boolean;
  changed_field_names: string[];
}

/** Siber yapilanislem kodları: 1 ekleme, 2 güncelleme, 3 silme. */
const OP_STYLE: Record<number, string> = {
  1: "bg-emerald-50 text-emerald-700 border-emerald-200",
  2: "bg-blue-50 text-blue-700 border-blue-200",
  3: "bg-red-50 text-red-700 border-red-200",
};

const OP_ICON: Record<number, React.ComponentType<{ size?: number }>> = {
  1: Plus,
  2: PencilLine,
  3: Trash2,
};

const stamp = (value: string | null) =>
  value
    ? `${new Date(value).toLocaleDateString("tr-TR")} ${new Date(value).toLocaleTimeString("tr-TR", {
        hour: "2-digit",
        minute: "2-digit",
      })}`
    : "—";

export function RecordHistoryTab({
  /** Örn. "load_transfer", "load", "expedition". */
  resource,
  recordId,
}: {
  resource: string;
  recordId: number | null;
}) {
  const [entries, setEntries] = useState<RecordHistoryEntry[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (recordId === null) return;

    setLoading(true);
    api
      .get<DataMessage<RecordHistoryEntry[]>>(`/api/v1/${resource}/${recordId}/history`)
      .then((res) => setEntries(res.data))
      .catch(() => setEntries([]))
      .finally(() => setLoading(false));
  }, [resource, recordId]);

  if (loading) {
    return <div className="p-6 text-sm text-gray-400">Yükleniyor…</div>;
  }

  if (entries.length === 0) {
    return (
      <EmptyState
        icon={History}
        title="İşlem kaydı yok"
        desc="Bu kayıt için Siber'de kayıtlı bir işlem geçmişi bulunamadı."
      />
    );
  }

  return (
    <div className="p-6 space-y-3">
      {entries.map((entry) => {
        const Icon = OP_ICON[entry.operation ?? 2] ?? PencilLine;

        return (
          <div key={entry.id} className="border border-gray-200 rounded overflow-hidden">
            <div className="flex items-center gap-2 px-3 py-2 bg-gray-50 border-b border-gray-200 flex-wrap">
              <span
                className={clsx(
                  "inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-[11px] font-medium border",
                  OP_STYLE[entry.operation ?? 2] ?? OP_STYLE[2],
                )}
              >
                <Icon size={11} />
                {entry.operation_label}
              </span>

              {/* Ad yoksa kod gösterilir: ayrılmış personelin kodu yerel
                  kullanıcı tablosunda karşılık bulmuyor. */}
              <span className="text-sm font-medium text-gray-800">
                {entry.user_name ?? entry.user_code ?? "—"}
              </span>

              <span className="text-xs text-gray-400">{stamp(entry.changed_at)}</span>

              {entry.module && (
                <span className="ml-auto text-[11px] text-gray-400">{entry.module}</span>
              )}
            </div>

            {entry.changes_unparsed ? (
              // Değer listeleri alan listesiyle hizalanmadığında (çok satırlı
              // bir metin alanı yüzünden) eşleştirme yapılmaz — yanlış bir
              // "önceki → sonraki" çifti göstermektense alan adları verilir.
              <div className="px-3 py-2 text-xs text-gray-500">
                Değişen alanlar: {entry.changed_field_names.join(", ")}
                <div className="text-[11px] text-gray-400 mt-1">
                  Bu kayıtta değerler alan adlarıyla eşleştirilemedi.
                </div>
              </div>
            ) : entry.changes.length === 0 ? (
              <div className="px-3 py-2 text-xs text-gray-400">Alan değişikliği kaydedilmemiş.</div>
            ) : (
              <table className="w-full text-xs">
                <tbody>
                  {entry.changes.map((change, index) => (
                    <tr key={`${entry.id}-${index}`} className="border-t border-gray-100 first:border-t-0">
                      <td className="px-3 py-1.5 text-gray-500 w-[38%] align-top">{change.field}</td>
                      <td className="px-2 py-1.5 text-gray-400 line-through align-top break-words">
                        {change.old_value ?? "—"}
                      </td>
                      <td className="px-3 py-1.5 text-gray-800 font-medium align-top break-words">
                        {change.new_value ?? "—"}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        );
      })}
    </div>
  );
}
