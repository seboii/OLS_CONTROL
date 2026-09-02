/**
 * Kaydedilmemiş form taslakları — tarayıcı belleğinde, ÇOKLU.
 *
 * Sunucudaki "Taslaklar" kavramı yalnızca KAYDEDİLMİŞ ama eksik kalmış
 * kayıtları kapsar (teklifte `is_draft=1`). Buradaki taslak, form değiştikçe
 * kendiliğinden yazılır ve "kaldığı yerden devam" girdisi olarak sunulur.
 *
 * ELLE KAYDETME YOK: kullanıcı kaydetmeyi unutabiliyordu.
 *
 * NEDEN ÇOKLU: bir dönem her ekran TEK bir anahtara yazıyordu ve her yeni form
 * öncekinin üzerine biniyordu — bir yükü yarım bırakıp diğerini açmak mümkün
 * değildi. Artık her düzenleme oturumunun kendi taslak kimliği var:
 *
 *   • "Yeni ..." YENİ bir kimlik açar; önceki taslak yerinde kalır.
 *   • Taslaktan devam edildiğinde O kimlik benimsenir, yani taslak
 *     çoğalmaz, güncellenir.
 *   • Kayıt başarılı olunca yalnızca o kimlik silinir.
 *
 * NEDEN SUNUCUYA DEĞİL TARAYICIYA: yarım bir form zaten sunucu doğrulamasından
 * geçmez; her yarım denemede veritabanına (ve Siber'e) çöp kayıt açmak
 * istemiyoruz. Kabul edilen bedeli, taslağın yalnızca o tarayıcıda görünmesi.
 *
 * DOSYA TAŞINMAZ: `File` nesnesi JSON'a yazılamıyor. Taslaktan devam ederken
 * dosyalar yeniden seçilmeli.
 */

/** Aynı ekranda tutulacak en fazla taslak — sınırsız büyümesin. */
const MAX_DRAFTS = 25;

export interface Draft<T> {
  id: string;
  savedAt: string;
  payload: T;
}

/**
 * localStorage her ortamda çalışmayabilir (gizli sekme, site verisi kapalı).
 * Hiçbir okuma/yazma çağrısı sayfayı düşürmemeli.
 */
function safeRead(key: string): string | null {
  try {
    return localStorage.getItem(key);
  } catch {
    return null;
  }
}

function safeWrite(key: string, value: string) {
  try {
    localStorage.setItem(key, value);
  } catch {
    // Kota dolu veya gizli sekme: otomatik taslak "en iyi çaba", sessizce vazgeç.
  }
}

/** Yeni bir düzenleme oturumu için taslak kimliği. */
export function newDraftId(): string {
  return `d${Date.now().toString(36)}${Math.random().toString(36).slice(2, 7)}`;
}

/** En yeni önce sıralı taslak listesi. */
export function listDrafts<T>(key: string): Draft<T>[] {
  const raw = safeRead(key);
  if (!raw) return [];

  try {
    const parsed = JSON.parse(raw);
    if (!Array.isArray(parsed)) return [];

    return (parsed as Draft<T>[])
      .filter((d) => d && typeof d.id === "string" && d.payload !== undefined)
      .sort((a, b) => b.savedAt.localeCompare(a.savedAt));
  } catch {
    // Bozuk JSON / erişilemeyen depolama: taslak yok say.
    return [];
  }
}

/** Verilen kimlikteki taslağı yazar; yoksa ekler, varsa üzerine yazar. */
export function saveDraft<T>(key: string, id: string, payload: T): Draft<T> {
  const draft: Draft<T> = { id, savedAt: new Date().toISOString(), payload };
  const rest = listDrafts<T>(key).filter((d) => d.id !== id);

  safeWrite(key, JSON.stringify([draft, ...rest].slice(0, MAX_DRAFTS)));
  return draft;
}

export function removeDraft(key: string, id: string) {
  safeWrite(key, JSON.stringify(listDrafts(key).filter((d) => d.id !== id)));
}

/** Taslak girdisinde gösterilen "26/08 15:46" biçimi. */
export function formatDraftTime(savedAt: string): string {
  return new Date(savedAt).toLocaleString("tr-TR", {
    day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit",
  });
}
