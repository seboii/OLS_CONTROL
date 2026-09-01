import { clsx } from "clsx";
import { Trash2, UserRound, PencilLine } from "lucide-react";

/**
 * Bir kaydın Siber izleri: kim açtı, kim en son dokundu, silindi mi.
 *
 * Kullanıcı hem kod hem ad taşır. Siber kullanıcıyı koduyla tutuyor ve bu
 * kodların 91'inden 3'ü yerel kullanıcı tablosunda karşılık bulmuyor
 * (ayrılmış personel, "OLS" sistem hesabı); ad boşsa kod gösterilir ki
 * "kim yaptı" sorusu cevapsız kalmasın.
 *
 * "Kim son dokundu" Siber'de her kayıtta dolu değil (teklif %81, yük %85,
 * sefer %30) — o satır boşsa hiç gösterilmez.
 */
export interface SiberAuditInfo {
  created_by_code: string | null;
  created_by_name: string | null;
  created_at: string | null;
  updated_by_code: string | null;
  updated_by_name: string | null;
  updated_at: string | null;
  deleted_at: string | null;
}

const stamp = (value: string | null) =>
  value
    ? `${new Date(value).toLocaleDateString("tr-TR")} ${new Date(value).toLocaleTimeString("tr-TR", {
        hour: "2-digit",
        minute: "2-digit",
      })}`
    : null;

const who = (name: string | null, code: string | null) => name ?? code ?? null;

/** Silinen kaydı listede/başlıkta işaretleyen rozet. */
export function SiberDeletedBadge({ deletedAt }: { deletedAt: string | null | undefined }) {
  if (!deletedAt) return null;

  return (
    <span
      className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-[11px] font-medium border bg-red-50 text-red-700 border-red-200"
      title={`Siber'de bulunamadı — ${stamp(deletedAt)}`}
    >
      <Trash2 size={11} />
      Siber'de silinmiş
    </span>
  );
}

export function SiberAuditPanel({
  audit,
  className,
}: {
  audit: SiberAuditInfo | null | undefined;
  className?: string;
}) {
  if (!audit) return null;

  const createdBy = who(audit.created_by_name, audit.created_by_code);
  const updatedBy = who(audit.updated_by_name, audit.updated_by_code);

  if (!createdBy && !updatedBy && !audit.deleted_at) return null;

  return (
    <div className={clsx("rounded border border-gray-200 bg-gray-50 p-2.5 space-y-1.5", className)}>
      {audit.deleted_at && (
        <div className="flex items-center gap-1.5 text-xs text-red-700">
          <Trash2 size={12} className="shrink-0" />
          <span>
            Bu kayıt Siber'de bulunamıyor. {stamp(audit.deleted_at)} tarihinde silinmiş olarak
            işaretlendi; geçmişi korumak için burada tutuluyor.
          </span>
        </div>
      )}

      {createdBy && (
        <div className="flex items-center gap-1.5 text-xs text-gray-600">
          <UserRound size={12} className="shrink-0 text-gray-400" />
          <span className="text-gray-500">Açan:</span>
          <span className="font-medium text-gray-800">{createdBy}</span>
          {audit.created_at && <span className="text-gray-400">· {stamp(audit.created_at)}</span>}
        </div>
      )}

      {updatedBy && (
        <div className="flex items-center gap-1.5 text-xs text-gray-600">
          <PencilLine size={12} className="shrink-0 text-gray-400" />
          <span className="text-gray-500">Son işlem:</span>
          <span className="font-medium text-gray-800">{updatedBy}</span>
          {audit.updated_at && <span className="text-gray-400">· {stamp(audit.updated_at)}</span>}
        </div>
      )}
    </div>
  );
}
