import type { ReactNode } from "react";
import { clsx } from "clsx";

/**
 * Uzun süren işlemler için ortak bekleme göstergeleri.
 *
 * Neden gerekli: Siber'e aktarma, Yük oluşturma, kaydetme ve silme adımları
 * uzak SQL Server'a birden fazla gidiş-dönüş yapıyor ve saniyeler sürebiliyor.
 * Önceden bu sırada ekranda HİÇBİR gösterge yoktu; kullanıcı işlemin başlayıp
 * başlamadığını anlayamıyor, çoğu zaman düğmeye tekrar basıyordu.
 */

/** Basit dönen halka — düğme içinde ve katmanda kullanılır. */
export function Spinner({ className }: { className?: string }) {
  return (
    <span
      role="status"
      aria-label="Yükleniyor"
      className={clsx(
        "inline-block rounded-full border-2 border-current border-r-transparent animate-spin",
        className ?? "w-4 h-4",
      )}
    />
  );
}

/**
 * İçeriğin üzerini kaplayan yarı saydam bekleme katmanı. Kapsayıcı öğede
 * `relative` olmalı. Katman açıkken altındaki alan tıklanamaz — böylece aynı
 * işlem yanlışlıkla iki kez tetiklenmez.
 */
export function BusyOverlay({ show, label }: { show: boolean; label?: string }) {
  if (!show) return null;

  return (
    <div className="absolute inset-0 z-40 flex items-center justify-center bg-white/70 backdrop-blur-[1px]">
      <div className="flex flex-col items-center gap-2.5 rounded-lg bg-white px-6 py-4 shadow-lg border border-gray-200">
        <Spinner className="w-6 h-6 text-blue-600" />
        <p className="text-sm font-medium text-gray-700">{label ?? "İşleniyor..."}</p>
        <p className="text-[11px] text-gray-400">Lütfen bekleyin, sayfadan ayrılmayın.</p>
      </div>
    </div>
  );
}

/**
 * Tüm ekranı kaplayan bekleme katmanı — çekmece dışından tetiklenen
 * (kart üzerindeki) uzun işlemler için.
 */
export function FullScreenBusy({ show, label }: { show: boolean; label?: string }) {
  if (!show) return null;

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center bg-gray-900/20 backdrop-blur-[2px]">
      <div className="flex flex-col items-center gap-3 rounded-xl bg-white px-8 py-6 shadow-2xl border border-gray-200">
        <Spinner className="w-7 h-7 text-blue-600" />
        <p className="text-sm font-semibold text-gray-800">{label ?? "İşleniyor..."}</p>
        <p className="text-[11px] text-gray-400">Siber ile iletişim kuruluyor, lütfen bekleyin.</p>
      </div>
    </div>
  );
}

/** Düğme etiketini bekleme durumuna göre değiştirir (spinner + metin). */
export function BusyLabel({ busy, busyText, children }: {
  busy: boolean; busyText: string; children: ReactNode;
}) {
  return busy ? (
    <>
      <Spinner className="w-3.5 h-3.5" />
      {busyText}
    </>
  ) : (
    <>{children}</>
  );
}
