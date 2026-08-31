import { useEffect, useRef, useState, type KeyboardEvent, type UIEvent } from "react";
import { clsx } from "clsx";
import { api, type DataMessage, type Paginated } from "@/lib/api";
import { useDebouncedValue } from "@/lib/hooks";
import { FormField } from "@/components/ui/primitives";

const PAGE_SIZE = 20;

export interface FinancialItemOption {
  id: number;
  name: string | null;
  /** LookupService bit maskesi: 1=Alış/Gider, 2=Satış/Gelir, 3=ikisi de. */
  type: number;
  /**
   * Bu kalem için varsayılan cari — Siber'deki kullanım geçmişinden türetilir
   * (bkz. FinancialItem.DefaultAccountId). Yalnızca kalemin satırlarının ezici
   * çoğunluğu tek firmaya aitse dolu gelir; dağınık kalemlerde null'dur.
   */
  default_account_id?: number | null;
  default_account_name?: string | null;
}

function typeBadge(type: number) {
  if (type === 3) return <span className="text-[10px] text-gray-400 shrink-0">Alış+Satış</span>;
  if (type === 2) return <span className="text-[10px] text-red-500 shrink-0">Satış</span>;
  return <span className="text-[10px] text-green-600 shrink-0">Alış</span>;
}

/**
 * Mali kalem seçici: ayrı bir pencere açmadan, doğrudan yazarak arayan satır-içi
 * bileşen (combobox). Yazıldıkça öneriler input'un altında listelenir; ok
 * tuşlarıyla gezilebilir, Enter vurgulanan öneriyi doğrudan seçer, Escape veya
 * dışarı tıklama listeyi kapatıp önceki seçime döner.
 *
 * Alış/Satış'a göre ÖNCEDEN filtrelenmez (kullanıcı isteği: "tüm kalemler
 * olsun, alış satış ise otomatik belirlensin") — tüm kalemler aranabilir,
 * hangi kalemin seçildiğine göre Alış/Satış çağıran taraftan (satırın kendi
 * onChange'inde item.type okunarak) otomatik ayarlanır.
 */
export function FinancialItemPicker({ label, value, onChange, required, error }: {
  label: string;
  value: FinancialItemOption | null;
  onChange: (v: FinancialItemOption | null) => void;
  required?: boolean;
  error?: string;
}) {
  const [query, setQuery] = useState(value?.name ?? "");
  const [open, setOpen] = useState(false);
  const debouncedQuery = useDebouncedValue(query);
  const [results, setResults] = useState<FinancialItemOption[]>([]);
  const [loading, setLoading] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(false);
  const [highlighted, setHighlighted] = useState(0);
  const containerRef = useRef<HTMLDivElement>(null);

  // Dışarıdan value değişirse (ör. form resetlenirse, satır Alış/Satış değiştirirse)
  // metin kutusunu senkronla.
  useEffect(() => {
    setQuery(value?.name ?? "");
  }, [value?.id, value?.name]);

  // Siber senkronundan sonra bu tablo (ve Cari/Kullanıcı) yüzlerce/binlerce satır
  // içerebiliyor — arama sonucu tek sayfada sığmayabilir. Sorgu değiştiğinde
  // 1. sayfadan başlanır; devamı aşağı kaydırınca loadMore() ile eklenir.
  useEffect(() => {
    if (!open) return;
    setLoading(true);
    api
      .get<DataMessage<Paginated<FinancialItemOption>>>("/api/v1/financial_item", {
        search: debouncedQuery || undefined,
        per_page: PAGE_SIZE,
        page: 1,
      })
      .then((res) => {
        setResults(res.data.data);
        setPage(1);
        setHasMore(res.data.current_page < res.data.last_page);
        setHighlighted(0);
      })
      .catch(() => {
        setResults([]);
        setHasMore(false);
      })
      .finally(() => setLoading(false));
  }, [open, debouncedQuery]);

  function loadMore() {
    if (loading || loadingMore || !hasMore) return;
    const nextPage = page + 1;
    setLoadingMore(true);
    api
      .get<DataMessage<Paginated<FinancialItemOption>>>("/api/v1/financial_item", {
        search: debouncedQuery || undefined,
        per_page: PAGE_SIZE,
        page: nextPage,
      })
      .then((res) => {
        setResults((prev) => [...prev, ...res.data.data]);
        setPage(nextPage);
        setHasMore(res.data.current_page < res.data.last_page);
      })
      .catch(() => setHasMore(false))
      .finally(() => setLoadingMore(false));
  }

  function handleResultsScroll(e: UIEvent<HTMLDivElement>) {
    const el = e.currentTarget;
    if (el.scrollHeight - el.scrollTop - el.clientHeight < 48) loadMore();
  }

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false);
        setQuery(value?.name ?? "");
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [value]);

  function select(item: FinancialItemOption) {
    onChange(item);
    setQuery(item.name ?? "");
    setOpen(false);
  }

  function handleKeyDown(e: KeyboardEvent<HTMLInputElement>) {
    if (!open) return;
    if (e.key === "ArrowDown") {
      e.preventDefault();
      setHighlighted((h) => Math.min(h + 1, results.length - 1));
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setHighlighted((h) => Math.max(h - 1, 0));
    } else if (e.key === "Enter") {
      e.preventDefault();
      if (results[highlighted]) select(results[highlighted]);
    } else if (e.key === "Escape") {
      setOpen(false);
      setQuery(value?.name ?? "");
    }
  }

  return (
    <FormField label={label} required={required} error={error}>
      <div className="relative" ref={containerRef}>
        <input
          type="text"
          value={query}
          onChange={(e) => {
            setQuery(e.target.value);
            setOpen(true);
            if (value) onChange(null);
          }}
          onFocus={() => setOpen(true)}
          onKeyDown={handleKeyDown}
          placeholder="Yazarak ara..."
          className={clsx(
            "px-3 py-2 text-sm border rounded-md bg-white transition-all w-full",
            "focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 focus-visible:border-blue-400",
            error ? "border-red-400 bg-red-50/30" : "border-gray-200",
          )}
        />
        {open && (
          <div
            onScroll={handleResultsScroll}
            className="absolute z-20 mt-1 w-full bg-white border border-gray-200 rounded-md shadow-lg max-h-64 overflow-y-auto"
          >
            {loading ? (
              <p className="text-xs text-gray-400 text-center py-4">Yükleniyor...</p>
            ) : results.length === 0 ? (
              <p className="text-xs text-gray-400 text-center py-4">Sonuç bulunamadı.</p>
            ) : (
              <>
                {results.map((r, i) => (
                  <button
                    key={r.id}
                    type="button"
                    onClick={() => select(r)}
                    onMouseEnter={() => setHighlighted(i)}
                    className={clsx(
                      "w-full flex items-center justify-between gap-2 text-left px-3 py-2 text-sm transition-colors",
                      i === highlighted ? "bg-blue-50 text-blue-700" : "text-gray-700 hover:bg-gray-50",
                    )}
                  >
                    <span className="truncate">{r.name ?? `#${r.id}`}</span>
                    {typeBadge(r.type)}
                  </button>
                ))}
                {loadingMore && (
                  <p className="text-xs text-gray-400 text-center py-2">Daha fazla yükleniyor...</p>
                )}
              </>
            )}
          </div>
        )}
      </div>
    </FormField>
  );
}
