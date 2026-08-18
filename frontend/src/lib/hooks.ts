import { useEffect, useState } from "react";
import { api } from "./api";

export interface NamedOption {
  id: string | number;
  name: string;
  // Yalnızca /api/v1/country gibi bazı uçlarda dolu gelir (bkz. Country entity).
  phone_code?: string | null;
}

/**
 * Basit tanım/lookup verisi için dropdown seçenekleri çeker
 * (work_type, currency, car_type, account_type, vb.). Bu tablolar küçük
 * olduğundan sayfalama olmadan (per_page verilmeden) çağrılır — backend
 * bu durumda düz dizi döner (bkz. QueryableExtensions.ToPagedOrListAsync).
 */
export function useLookupOptions(path: string | null, query?: Record<string, string>) {
  const [options, setOptions] = useState<NamedOption[]>([]);
  const [loading, setLoading] = useState(!!path);
  // olsold'un SelectAjax'ı her açılışta arar (her zaman taze); burada bir kez
  // çekilip önbelleklendiğinden, bir LookupManagerModal ile yeni kayıt
  // eklendikten sonra dropdown'ın güncel kalması için manuel bir yeniden-çekim
  // tetikleyicisi gerekiyor (bkz. components/shared/LookupManagerModal.tsx).
  const [refreshToken, setRefreshToken] = useState(0);

  useEffect(() => {
    if (!path) {
      setOptions([]);
      return;
    }
    let cancelled = false;
    setLoading(true);
    api
      .get<{ data: NamedOption[] }>(path, query)
      .then((res) => {
        if (!cancelled) setOptions(Array.isArray(res.data) ? res.data : []);
      })
      .catch(() => {
        if (!cancelled) setOptions([]);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
    // query kasıtlı olarak referans eşitliğiyle izleniyor; çağıran taraf
    // stabil bir nesne geçirmeli (örn. useMemo) — aksi halde sonsuz döngü olur.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [path, JSON.stringify(query ?? {}), refreshToken]);

  return { options, loading, refresh: () => setRefreshToken((n) => n + 1) };
}

export function useDebouncedValue<T>(value: T, delayMs = 300): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const t = setTimeout(() => setDebounced(value), delayMs);
    return () => clearTimeout(t);
  }, [value, delayMs]);
  return debounced;
}
