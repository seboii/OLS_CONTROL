import { clsx } from "clsx";
import { ChevronDown, ChevronUp, Trash2 } from "lucide-react";

/**
 * Katlanabilir kayıt satırı.
 *
 * Yük kartındaki Paketler ve Finans sekmeleri onlarca satır içerebiliyor ve her
 * satır tam formuyla açık duruyordu — liste okunamaz hâle geliyordu. Artık
 * kapalıyken yalnızca satırı TANIMLAYAN iki bilgi görünür (finansta kalem +
 * fiyat, pakette ürün + adet), tıklanınca form açılır.
 *
 * Silme düğmesi kapalıyken de erişilebilir kalır; başlığa tıklamayla
 * karışmasın diye kendi tıklamasını durdurur.
 *
 * `LoadsPage` içinden buraya alındı: bileşen tamamen prop'lara dayanıyor,
 * 2.500 satırlık sayfada durmasının bir sebebi yoktu ve ayrı dosyada test
 * edilebiliyor.
 */
export function CollapsibleRow({
  title, summary, open, onToggle, onRemove, removeTitle, children,
}: {
  title: string;
  summary: string;
  open: boolean;
  onToggle: () => void;
  onRemove?: () => void;
  removeTitle?: string;
  children: React.ReactNode;
}) {
  return (
    <div className="border border-gray-200 rounded-lg mb-2 overflow-hidden">
      <div
        role="button"
        tabIndex={0}
        onClick={onToggle}
        onKeyDown={(e) => {
          if (e.key === "Enter" || e.key === " ") { e.preventDefault(); onToggle(); }
        }}
        className={clsx(
          "flex items-center gap-2 px-3 py-2 cursor-pointer select-none",
          open ? "bg-gray-50 border-b border-gray-200" : "hover:bg-gray-50",
        )}
      >
        {open ? <ChevronUp size={14} className="text-gray-400 shrink-0" /> : <ChevronDown size={14} className="text-gray-400 shrink-0" />}
        <span className="text-xs font-medium text-gray-800 truncate">{title}</span>
        <span className="ml-auto text-xs font-semibold text-gray-600 shrink-0 tabular-nums">{summary}</span>
        {onRemove && (
          <button
            type="button"
            title={removeTitle}
            onClick={(e) => { e.stopPropagation(); onRemove(); }}
            className="text-gray-300 hover:text-red-500 shrink-0"
          >
            <Trash2 size={13} />
          </button>
        )}
      </div>
      {open && <div className="p-4">{children}</div>}
    </div>
  );
}
