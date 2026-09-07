/**
 * TÜRKÇE SAYI GİRİŞİNİ ÇÖZER — sunucudaki `TurkishDecimal` ile AYNI kural.
 *
 * Neden burada da gerekiyor: teklif formu alanları `FormData` ile METİN
 * gönderiyor ve sunucu tarafında `TurkishDecimal.Parse` çalışıyor. Yük
 * düzenleme ekranı ise JSON gövde gönderiyor ve alanları tarayıcıda SAYIYA
 * çeviriyor — yani sunucudaki ayrıştırıcıya hiç uğramıyor. Oradaki tek
 * ayrıştırıcı şuydu:
 *
 *     const num = (v) => (v.trim() === "" ? null : Number(v.replace(",", ".")));
 *
 * ve üç girişte birden yanlış sonuç veriyordu (ölçüldü):
 *
 * | Giriş | Eski | Doğru |
 * |---|---|---|
 * | `1.850,75` | `null` (fiyat kayboluyor) | 1850.75 |
 * | `1.250` | `1.25` (bin kat hata) | 1250 |
 * | `1,234.56` | `null` | 1234.56 |
 *
 * `Number("1.850.75")` NaN döner ve `JSON.stringify(NaN)` `null` yazar; yani
 * kullanıcı binlik ayraç kullandığında fiyat/ağırlık SESSİZCE boşa düşüyordu.
 * Aynı kusur olsold'da da vardı (`str_replace(',', '.')`) ve sunucu tarafında
 * düzeltilmişti — arayüzün JSON yolu geride kalmıştı.
 *
 * KURAL: son ayraç ondalıktır, öncekiler binlik ayraçtır. Tek ayraçtan sonra
 * TAM 3 hane varsa o ayraç binliktir (`1.250` = bin iki yüz elli).
 *
 * Sunucudaki karşılığı değişirse buradaki de değişmeli; iki uçtaki testler
 * aynı örnekleri kullanıyor (`TurkishDecimalTests` ↔ `number.test.ts`).
 */
export function parseDecimalInput(value: string | null | undefined): number | null {
  if (value == null) return null;

  const text = value.trim();
  if (text === "") return null;

  const separatorIndex = Math.max(text.lastIndexOf(","), text.lastIndexOf("."));

  let normalized: string;

  if (separatorIndex < 0) {
    normalized = text;
  } else {
    const integerPart = text.slice(0, separatorIndex).replace(/[.,]/g, "");
    const fractionPart = text.slice(separatorIndex + 1);
    const separatorCount = (text.match(/[.,]/g) ?? []).length;

    normalized = fractionPart.length === 3 && separatorCount === 1
      ? integerPart + fractionPart
      : `${integerPart}.${fractionPart}`;
  }

  // Number("") 0 döner; boş/eksik girişin 0'a düşmemesi için önce elenir.
  if (normalized === "" || normalized === "." || normalized === "-") return null;

  const parsed = Number(normalized);
  return Number.isFinite(parsed) ? parsed : null;
}

/** Tam sayı alanları (adet vb.) — ondalıklı giriş aşağı yuvarlanır. */
export function parseIntegerInput(value: string | null | undefined): number | null {
  const parsed = parseDecimalInput(value);
  return parsed === null ? null : Math.trunc(parsed);
}

/**
 * En/boy (cm) → lademetre. Referans uygulamayla aynı formül: (en × boy) / 24000.
 *
 * Yük ve teklif ekranlarında AYNI formül iki kez yazılıydı; tek yere alındı.
 */
export function computeLademeter(widthCm: string, lengthCm: string): string {
  const w = parseDecimalInput(widthCm);
  const l = parseDecimalInput(lengthCm);

  return w !== null && l !== null && w > 0 && l > 0
    ? ((w * l) / 24000).toFixed(2)
    : "";
}
