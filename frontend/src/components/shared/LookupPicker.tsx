import { useEffect, useRef, useState, type KeyboardEvent, type UIEvent } from "react";
import { clsx } from "clsx";
import { api, type DataMessage, type Paginated } from "@/lib/api";
import { useDebouncedValue } from "@/lib/hooks";
import { FormField } from "@/components/ui/primitives";

const PAGE_SIZE = 20;

export interface LookupOption {
  id: number;
  name: string | null;
}

/**
 * AccountPicker/FinancialItemPicker ile aynı satır-içi arama (combobox) davranışı,
 * herhangi bir generic lookup uç noktası (LookupControllerBase — /api/v1/<kaynak>
 * ?search=&per_page=&page=) için. Ürün Tipi/Kap Tipi gibi tanım tabloları
 * Siber'in tam referans-veri senkronundan sonra yüzlerce satıra çıkabiliyor
 * (ör. Kap Tipi 9→125, Ürün Tipi →223) — düz bir &lt;select&gt; ile aranamaz hâle
 * geliyor, bu yüzden bu bileşen kullanılır.
 */
export function LookupPicker({ label, endpoint, value, onChange, required, error }: {
  label: string;
  endpoint: string;
  value: LookupOption | null;
  onChange: (v: LookupOption | null) => void;
  required?: boolean;
  error?: string;
}) {
  const [query, setQuery] = useState(value?.name ?? "");
  const [open, setOpen] = useState(false);
  const debouncedQuery = useDebouncedValue(query);
  const [results, setResults] = useState<LookupOption[]>([]);
  const [loading, setLoading] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(false);
  const [highlighted, setHighlighted] = useState(0);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    setQuery(value?.name ?? "");
  }, [value?.id, value?.name]);

  useEffect(() => {
    if (!open) return;
    setLoading(true);
    api
      .get<DataMessage<Paginated<LookupOption>>>(endpoint, {
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
  }, [open, debouncedQuery, endpoint]);

  function loadMore() {
    if (loading || loadingMore || !hasMore) return;
    const nextPage = page + 1;
    setLoadingMore(true);
    api
      .get<DataMessage<Paginated<LookupOption>>>(endpoint, {
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

  function select(item: LookupOption) {
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
                      "w-full text-left px-3 py-2 text-sm transition-colors",
                      i === highlighted ? "bg-blue-50 text-blue-700" : "text-gray-700 hover:bg-gray-50",
                    )}
                  >
                    {r.name ?? `#${r.id}`}
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
