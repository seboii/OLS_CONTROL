import { createContext, useContext, useEffect } from "react";

/**
 * SAYFA YENİLEME — üst bardaki düğme ile sayfanın kendi listesi arasındaki bağ.
 *
 * Neden bağlam: yenileme düğmesi TopBar'da, veriyi çeken kod ise her sayfanın
 * kendi `load()` fonksiyonunda. Düğmenin hangi sayfada olduğunu bilmesi
 * gerekmiyor; açık olan sayfa kendi yenileyicisini KAYDEDİYOR, düğme onu
 * çağırıyor.
 *
 * Kayıtlı yenileyici yoksa (henüz bağlanmamış bir ekran) tarayıcı yenilemesine
 * düşülür — düğme her durumda bir şey yapar, sessiz kalmaz.
 */
export interface RefreshContextValue {
  /** Açık sayfa kendi yenileyicisini kaydeder; ayrılırken null geçer. */
  register: (handler: RefreshHandler | null) => void;

  /** Üst bardaki düğme bunu çağırır. */
  refresh: () => void;

  /** Yenileme sürüyor mu — düğmedeki dönen ikon için. */
  refreshing: boolean;
}

/** Yenileyici bir söz döndürürse beklenir; döndürmezse kısa bir dönüş gösterilir. */
export type RefreshHandler = () => void | Promise<unknown>;

export const RefreshContext = createContext<RefreshContextValue | null>(null);

export function useRefresh(): RefreshContextValue {
  const value = useContext(RefreshContext);
  if (!value) throw new Error("useRefresh, RefreshProvider içinde kullanılmalı");
  return value;
}

/**
 * Sayfanın yenileyicisini kaydeder.
 *
 * `handler` her render'da yeniden oluşuyor olabilir (çoğu sayfada `load` düz bir
 * fonksiyon); bu yüzden bağımlılık listesine KONMAZ, her render'da güncel hâli
 * yazılır. Böylece düğme daima en son filtrelerle çalışan sürümü çağırır.
 */
export function useRegisterRefresh(handler: RefreshHandler): void {
  const { register } = useRefresh();

  useEffect(() => {
    register(handler);
    return () => register(null);
  });
}
