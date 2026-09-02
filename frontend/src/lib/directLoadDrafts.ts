/**
 * Teklifsiz yük formunun OTOMATİK taslağı — tarayıcı belleğinde (localStorage).
 *
 * NEDEN: form uzun (tanımlar, taraflar, güzergah, paketler, mali kalemler) ve
 * tek oturumda doldurulamayabiliyor. Sekme kapanması, sayfa yenilenmesi ya da
 * ani elektrik kesintisi doldurulanı tamamen kaybettiriyordu.
 *
 * ELLE KAYDETME YOK. Bir ara "Taslak Kaydet" düğmesi ve adlı taslaklar vardı;
 * kullanıcı kaydetmeyi unuttuğunda emeği yine kayboluyordu. Artık form her
 * değiştiğinde kendiliğinden yazılıyor, bir sonraki açılışta "kaldığınız
 * yerden devam" olarak sunuluyor ve istenirse siliniyor. Teklif ekranındaki
 * davranışın aynısı (bkz. lib/autodraft.ts).
 *
 * SUNUCUYA YAZILMIYOR: taslak henüz Siber'e gitmemiş, numarası olmayan bir
 * veri. Sunucuda tutulsaydı yarım kayıtlar için ayrı bir yaşam döngüsü ve
 * temizleme işi doğardı. Bunun kabul edilen bedeli, taslağın yalnızca o
 * tarayıcıda görünmesidir.
 *
 * DOSYALAR TAŞINMAZ: seçilen dosyalar File nesnesi ve localStorage'a
 * yazılamıyor. Taslaktan devam edilirken dosyaların yeniden seçilmesi gerekir.
 */

const AUTOSAVE_KEY = "ols.directLoad.autosave.v1";

/**
 * localStorage her ortamda çalışmayabilir (gizli sekme, site verisi kapalı).
 * Hiçbir okuma/yazma çağrısı sayfayı düşürmemeli.
 */
function safeRead(key: string): string | null {
  try {
    return window.localStorage.getItem(key);
  } catch {
    return null;
  }
}

function safeWrite(key: string, value: string): boolean {
  try {
    window.localStorage.setItem(key, value);
    return true;
  } catch {
    // Kota dolabilir; taslak kaybı işi durdurmamalı.
    return false;
  }
}

/** Form her değiştiğinde çağrılır. */
export function writeAutosave(payload: unknown) {
  safeWrite(AUTOSAVE_KEY, JSON.stringify({ savedAt: new Date().toISOString(), payload }));
}

export function readAutosave(): { savedAt: string; payload: unknown } | null {
  const raw = safeRead(AUTOSAVE_KEY);
  if (!raw) return null;

  try {
    const parsed = JSON.parse(raw);
    return parsed?.payload ? parsed : null;
  } catch {
    return null;
  }
}

export function clearAutosave() {
  try {
    window.localStorage.removeItem(AUTOSAVE_KEY);
  } catch {
    /* yukarıdaki ile aynı */
  }
}

/** Taslağın dolu olup olmadığını kabaca ölçer — boş formu taslak yapmamak için. */
export function isPayloadEmpty(payload: Record<string, unknown> | null | undefined): boolean {
  if (!payload) return true;

  return !JSON.stringify(payload).match(/:"[^"]+"/);
}
