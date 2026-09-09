import { useCallback, useMemo, useRef, useState, type ReactNode } from "react";
import { RefreshContext, type RefreshHandler } from "@/lib/refresh";

/**
 * Yenileme sağlayıcısı. Bağlam, tipler ve kancalar `refresh.ts`'te — sebep için
 * oraya bakın (hızlı yenileme / only-export-components).
 */
export function RefreshProvider({ children }: { children: ReactNode }) {
  // Yenileyici REF'te tutuluyor: her render'da yeniden kaydediliyor ve state
  // olsaydı bu sonsuz render döngüsü olurdu.
  const handlerRef = useRef<RefreshHandler | null>(null);
  const [refreshing, setRefreshing] = useState(false);

  const register = useCallback((handler: RefreshHandler | null) => {
    handlerRef.current = handler;
  }, []);

  const refresh = useCallback(() => {
    const handler = handlerRef.current;

    // Bu ekran yenileyicisini kaydetmemişse tarayıcı yenilemesine düşülür;
    // düğme hiçbir sayfada "hiçbir şey yapmıyor" görünmemeli.
    if (!handler) {
      window.location.reload();
      return;
    }

    setRefreshing(true);

    let finished = false;
    const done = () => {
      if (finished) return;
      finished = true;
      setRefreshing(false);
    };

    try {
      const result = handler();

      if (result instanceof Promise)
        result.finally(done);
      else
        // Söz döndürmeyen yenileyicilerde (çoğu sayfa `load()` çağırıp geçiyor)
        // dönüş animasyonu kısa bir süre gösterilir — aksi hâlde tıklamanın
        // işe yarayıp yaramadığı anlaşılmıyordu.
        setTimeout(done, 600);
    } catch {
      done();
    }
  }, []);

  const value = useMemo(
    () => ({ register, refresh, refreshing }),
    [register, refresh, refreshing]);

  return <RefreshContext.Provider value={value}>{children}</RefreshContext.Provider>;
}
