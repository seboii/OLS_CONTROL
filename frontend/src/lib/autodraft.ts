/**
 * Kaydedilmemiş form taslakları (otomatik taslak).
 *
 * Sunucudaki "Taslaklar" kavramı yalnızca KAYDEDİLMİŞ ama eksik kalmış kayıtları
 * kapsar — kullanıcı formu doldururken kaydetmeden çıkarsa her şey kayboluyordu.
 * Buradaki taslak, form değiştikçe tarayıcıya yazılır ve "kaldığı yerden devam"
 * girdisi olarak sunulur.
 *
 * Neden sunucuya değil tarayıcıya: yarım bir form zaten sunucu doğrulamasından
 * geçmez; her yarım denemede veritabanına (ve Siber'e) çöp kayıt açmak istemiyoruz.
 * Bunun kabul edilen bedeli, taslağın yalnızca o tarayıcıda görünmesidir.
 */

export function readDraft<T>(key: string): T | null {
  try {
    const raw = localStorage.getItem(key);
    return raw ? (JSON.parse(raw) as T) : null;
  } catch {
    // Bozuk JSON / erişilemeyen depolama: taslak yok say.
    return null;
  }
}

export function writeDraft<T>(key: string, draft: T) {
  try {
    localStorage.setItem(key, JSON.stringify(draft));
  } catch {
    // Kota dolu veya gizli sekme: otomatik taslak "en iyi çaba", sessizce vazgeç.
  }
}

export function clearDraft(key: string) {
  try {
    localStorage.removeItem(key);
  } catch {
    /* yukarıdaki ile aynı */
  }
}

/** Taslak girdisinde gösterilen "26/08 15:46" biçimi. */
export function formatDraftTime(savedAt: string): string {
  return new Date(savedAt).toLocaleString("tr-TR", {
    day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit",
  });
}
