import { useCallback, useMemo, useState } from "react";

/**
 * "DEĞİŞİKLİK VAR MI?" — Kaydet düğmesini yöneten tek kural.
 *
 * Üç ekranda da (yük detayı, teklif, sefer detayı) Kaydet düğmesi koşulsuz
 * aktifti. Değişiklik olmadan basıldığında kayıt bütün alanlarıyla yeniden
 * gönderiliyor, Siber'e gereksiz bir UPDATE atılıyor ve kaydın `upduser` /
 * `updtime` damgası hiçbir şey değişmediği hâlde tazeleniyordu — yani denetim
 * izi kirleniyordu. Üstelik o gereksiz yazım Siber'in iş kurallarına
 * (ör. tarih tetikleyicisi) takılırsa kullanıcı, hiçbir şey yapmadığı hâlde
 * hata görüyordu.
 *
 * Karşılaştırma, formun tüm durumunun ANLIK GÖRÜNTÜSÜ üzerinden yapılıyor:
 * kayıt yüklendiğinde bir taban alınıyor, sonraki her render'da güncel görüntü
 * onunla karşılaştırılıyor. Alan alan izlemeye göre tek üstünlüğü var ama
 * belirleyici: yeni bir alan eklendiğinde burada hiçbir şey değişmiyor,
 * kural kendiliğinden onu da kapsıyor.
 */

/**
 * Anahtar sırasından bağımsız JSON.
 *
 * `JSON.stringify` nesne anahtarlarını ekleme sırasına göre yazıyor; aynı
 * içerik farklı sırayla üretildiğinde metin farklı çıkar ve form "değişmiş"
 * görünürdü. Anahtarlar sıralanarak bu yanlış pozitif kapatılıyor.
 *
 * `undefined` ile `null` de eşitleniyor: sunucudan gelen alan `null`, formda
 * hiç dokunulmamış alan `undefined` olabiliyor ve ikisi de "boş" demek.
 */
export function stableStringify(value: unknown): string {
  return JSON.stringify(normalize(value));
}

function normalize(value: unknown): unknown {
  if (value === undefined) return null;
  if (value === null || typeof value !== "object") return value;
  if (Array.isArray(value)) return value.map(normalize);

  const source = value as Record<string, unknown>;

  return Object.keys(source)
    .sort()
    .reduce<Record<string, unknown>>((acc, key) => {
      acc[key] = normalize(source[key]);
      return acc;
    }, {});
}

export interface DirtyState {
  /** Taban alındıktan sonra form değiştiyse true. */
  dirty: boolean;
  /** Taban alınmış mı — kayıt henüz yüklenmediyse false. */
  tracking: boolean;
  /** Güncel görüntüyü yeni taban yapar: kayıt yüklendiğinde ve kayıttan sonra. */
  reset: () => void;
  /** Tabanı bırakır; form kapandığında çağrılır, sonraki açılış temiz başlar. */
  clear: () => void;
}

/**
 * @param snapshot Formun tüm durumunu taşıyan nesne. Her render'da yeniden
 * kurulabilir — karşılaştırma içeriğe göre yapılıyor, kimliğe göre değil.
 */
export function useDirty(snapshot: unknown): DirtyState {
  const [baseline, setBaseline] = useState<string | null>(null);

  const current = useMemo(() => stableStringify(snapshot), [snapshot]);

  const reset = useCallback(() => setBaseline(stableStringify(snapshot)), [snapshot]);
  const clear = useCallback(() => setBaseline(null), []);

  return {
    dirty: baseline !== null && baseline !== current,
    tracking: baseline !== null,
    reset,
    clear,
  };
}
