import { createContext, useContext } from "react";

/**
 * Bildirim (toast) bağlamı ve kancası — SAĞLAYICI BİLEŞENİ AYRI DOSYADA.
 *
 * Bir modül hem bileşen hem başka bir şey (kanca/sabit/tip) dışa aktarınca
 * Vite'ın hızlı yenilemesi o dosyayı tazeleyemiyor ve düzenleme sırasında
 * bileşen durumu sıfırlanıyor (oxlint `react(only-export-components)`).
 *
 * AYRIŞTIRMA YÖNÜ BİLİNÇLİ: kanca 12 dosyadan, sağlayıcı yalnızca App.tsx'ten
 * çağrılıyor. Bu yüzden `useToast` burada KALDI ve taşınan bileşen oldu —
 * tersi 12 dosyada import değiştirmek demekti.
 */
export type ToastType = "success" | "error" | "info";

export interface ToastData {
  id: number;
  message: string;
  type: ToastType;
}

export interface ToastContextValue {
  addToast: (message: string, type?: ToastType) => void;
}

export const ToastContext = createContext<ToastContextValue | null>(null);

export function useToast() {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error("useToast must be used within ToastProvider");
  return ctx;
}
