import { useEffect, useRef, useState, type KeyboardEvent, type UIEvent } from "react";
import { clsx } from "clsx";
import { api, type DataMessage, type Paginated } from "@/lib/api";
import { useDebouncedValue } from "@/lib/hooks";
import { FormField } from "@/components/ui/primitives";

const PAGE_SIZE = 20;

export interface CarOption {
  id: number;
  plate_number: string | null;
  car_type?: { id: number; name: string | null } | null;
  romork_type?: { id: number; name: string | null } | null;
}

function displayPlate(c: CarOption): string {
  return c.plate_number?.trim() || `#${c.id}`;
}

/**
 * Araç seçici — PLAKADAN arar.
 *
 * Sefer formundaki "Araç (Plaka)" alanı eskiden düz bir metin kutusuydu ve
 * kullanıcıdan aracın YEREL SAYISAL ID'sini yazmasını bekliyordu ("Araç ID"
 * placeholder'ı). Ekranda plaka yerine bir sayı görünmesinin ve aracın elle
 * seçilememesinin sebebi buydu; Siber'de bu alan eşlenmiş araçlardan seçiliyor.
 *
 * /api/v1/car ucu `search` parametresini plakada ILIKE ile uyguluyor
 * (bkz. CarService: cars.WhereILike(c => c.PlateNumber, query.Search)), bu yüzden
 * yazdıkça arama doğrudan plaka üzerinden çalışır. Araç tipi/römork cinsi ikincil
 * satırda gösterilir: canlıda 21 plaka birden fazla araç kaydında tekrarlıyor,
 * aynı plakadan iki sonuç çıktığında kullanıcının doğru olanı ayırt etmesi gerekir.
 */
export function CarPicker({ label, value, onChange, required, error, carTypeId }: {
  label: string;
  value: CarOption | null;
  onChange: (v: CarOption | null) => void;
  required?: boolean;
  error?: string;
  /**
   * Verilirse arama yalnızca bu araç tipinde yapılır (car_types.id). Sefer
   * formunda çekici alanı bunu kullanıyor: 4.249 aracın 3.891'i römork,
   * yalnızca 111'i çekici — süzmeden çekici bulmak neredeyse imkânsızdı.
   */
  carTypeId?: number;
}) {
  const [query, setQuery] = useState(value ? displayPlate(value) : "");
  const [open, setOpen] = useState(false);
  const debouncedQuery = useDebouncedValue(query);
  const [results, setResults] = useState<CarOption[]>([]);
  const [loading, setLoading] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(false);
  const [highlighted, setHighlighted] = useState(0);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    setQuery(value ? displayPlate(value) : "");
  }, [value?.id, value?.plate_number]);

  useEffect(() => {
    if (!open) return;
    setLoading(true);
    api
      .get<DataMessage<Paginated<CarOption>>>("/api/v1/car", {
        search: debouncedQuery || undefined,
        car_type_id: carTypeId,
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
  }, [open, debouncedQuery, carTypeId]);

  function loadMore() {
    if (loading || loadingMore || !hasMore) return;
    const nextPage = page + 1;
    setLoadingMore(true);
    api
      .get<DataMessage<Paginated<CarOption>>>("/api/v1/car", {
        search: debouncedQuery || undefined,
        car_type_id: carTypeId,
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
        setQuery(value ? displayPlate(value) : "");
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [value]);

  function select(item: CarOption) {
    onChange(item);
    setQuery(displayPlate(item));
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
      setQuery(value ? displayPlate(value) : "");
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
          placeholder="Plaka yazarak ara..."
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
                {results.map((r, i) => {
                  const detail = [r.car_type?.name, r.romork_type?.name].filter(Boolean).join(" · ");
                  return (
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
                      <span className="font-medium">{displayPlate(r)}</span>
                      {detail && <span className="block text-[11px] text-gray-400">{detail}</span>}
                    </button>
                  );
                })}
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
