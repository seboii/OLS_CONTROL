/**
 * Teklifsiz yük formunun TASLAKLARI — tarayıcı belleğinde (localStorage).
 *
 * NEDEN: form uzun (5 sekme, paketler, mali kalemler) ve tek oturumda
 * doldurulamayabiliyor. Sekme kapanması, sayfa yenilenmesi ya da ani elektrik
 * kesintisi doldurulanı tamamen kaybettiriyordu.
 *
 * İKİ AYRI ŞEY VAR, karıştırılmamalı:
 *   * OTOMATİK taslak — kullanıcı bir şey yapmadan, form her değiştiğinde
 *     yazılır. Amacı kaza kurtarma. Tek tanedir ve kaydedilince silinir.
 *   * ADLI taslaklar — kullanıcının bilerek "Taslak Kaydet" dediği kayıtlar.
 *     BİRDEN FAZLA olabilir; kullanıcı aralarında geçiş yapar.
 *
 * SUNUCUYA YAZILMIYOR: taslak henüz Siber'e gitmemiş, numarası olmayan bir
 * veri. Sunucuda tutulsaydı yarım kayıtlar için ayrı bir yaşam döngüsü ve
 * temizleme işi doğardı. Tarayıcı belleği bu iş için yeterli ve anında.
 */

const STORAGE_KEY = "ols.directLoad.drafts.v1";
const AUTOSAVE_KEY = "ols.directLoad.autosave.v1";

/** Aynı anda tutulacak en fazla adlı taslak. */
const MAX_DRAFTS = 20;

export interface DirectLoadDraft {
  id: string;
  name: string;
  savedAt: string;
  /** Formun tüm durumu — şekli çağıran sayfaya ait, burada opak taşınır. */
  payload: unknown;
}

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

function safeRemove(key: string) {
  try {
    window.localStorage.removeItem(key);
  } catch {
    /* yok sayılır */
  }
}

export function listDrafts(): DirectLoadDraft[] {
  const raw = safeRead(STORAGE_KEY);
  if (!raw) return [];

  try {
    const parsed = JSON.parse(raw);
    if (!Array.isArray(parsed)) return [];
    // En yeni önce.
    return (parsed as DirectLoadDraft[]).sort((a, b) => b.savedAt.localeCompare(a.savedAt));
  } catch {
    return [];
  }
}

export function saveDraft(name: string, payload: unknown, id?: string): DirectLoadDraft | null {
  const drafts = listDrafts();
  const now = new Date().toISOString();

  const draft: DirectLoadDraft = {
    id: id ?? `d${Date.now().toString(36)}${Math.random().toString(36).slice(2, 7)}`,
    name: name.trim() || `Taslak ${new Date().toLocaleString("tr-TR")}`,
    savedAt: now,
    payload,
  };

  // Aynı id varsa güncellenir, yoksa başa eklenir.
  const rest = drafts.filter((d) => d.id !== draft.id);
  const next = [draft, ...rest].slice(0, MAX_DRAFTS);

  return safeWrite(STORAGE_KEY, JSON.stringify(next)) ? draft : null;
}

export function deleteDraft(id: string) {
  const next = listDrafts().filter((d) => d.id !== id);
  safeWrite(STORAGE_KEY, JSON.stringify(next));
}

/**
 * Kaza kurtarma kopyası. Kullanıcı kaydetmeden kapatırsa/kesinti olursa
 * form bir sonraki açılışta buradan geri yüklenir.
 */
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
  safeRemove(AUTOSAVE_KEY);
}

/** Taslağın dolu olup olmadığını kabaca ölçer — boş formu kaydetmemek için. */
export function isPayloadEmpty(payload: Record<string, unknown> | null | undefined): boolean {
  if (!payload) return true;

  return !JSON.stringify(payload).match(/:"[^"]+"/);
}
