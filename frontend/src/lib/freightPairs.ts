import { useEffect, useState } from "react";
import { api, type DataMessage } from "@/lib/api";
import { parseDecimalInput } from "@/lib/number";

/**
 * NAVLUN ALIŞ → SATIŞ KALEM EŞLEŞMESİ ve otomatik satış fiyatı.
 *
 * Kural (kullanıcı isteği): navlun ALIŞI girilince karşılığındaki SATIŞ satırı
 * kendiliğinden açılsın ve fiyatı alışın %15 fazlası olsun.
 *
 * EŞLEŞME SUNUCUDAN GELİR, adı üzerinden tahmin EDİLMEZ. Siber'de bu bağı tutan
 * hiçbir alan yok ve ad çevirisi en çok kullanılan çiftte yanlış sonuç verir:
 * kara navlununun geliri "KARA NAVLUN GELİRİ" değil "KARA NAVLUN HİZMET
 * BEDELİ"dir (bkz. financial_item_pairs tablosu ve AddFinancialItemPairs
 * migrasyonu).
 *
 * BOŞSA DOLDURUR, ELLE DEĞİŞENİ EZMEZ: otomatik açılan satır `auto: true`
 * taşır; kullanıcı o satırın fiyatına dokunduğu anda bayrak düşer ve alış
 * sonradan değişse bile satır bir daha güncellenmez.
 */
export interface FreightPair {
  purchase_item_id: number;
  purchase_item_name: string | null;
  sale_item_id: number;
  sale_item_name: string | null;
  markup_percent: number;
  sale_item_type: number | null;
  sale_default_account_id: number | null;
  sale_default_account_name: string | null;
}

/**
 * Eşleşme listesi (bugün 11 satır) bir kez okunur. Uç yanıt vermezse liste boş
 * kalır ve otomatik satır HİÇ açılmaz — form elle doldurulabilir durumda kalır,
 * yani hata kullanıcıyı engellemez.
 */
export function useFreightPairs(): FreightPair[] {
  const [pairs, setPairs] = useState<FreightPair[]>([]);

  useEffect(() => {
    let alive = true;

    api.get<DataMessage<FreightPair[]>>("/api/v1/financial_item_pair")
      .then((res) => { if (alive) setPairs(res.data ?? []); })
      .catch(() => { if (alive) setPairs([]); });

    return () => { alive = false; };
  }, []);

  return pairs;
}

/** Verilen ALIŞ kaleminin eşleşmesi; yoksa undefined. */
export function pairForPurchase(
  pairs: FreightPair[], itemId: number | null | undefined,
): FreightPair | undefined {
  return itemId == null ? undefined : pairs.find((p) => p.purchase_item_id === itemId);
}

/**
 * Alış fiyatına kâr yüzdesini ekler ve METİN döndürür (form alanları metin
 * tutuyor). Girdi çözülemezse boş döner — 0 YAZMAZ, çünkü 0 fiyat formda
 * açıklama zorunluluğu tetikliyor.
 *
 * Türkçe sayı girişi sunucuyla aynı kuralla çözülür (bkz. parseDecimalInput):
 * "1.850,75" → 1850.75.
 */
export function markedUpPrice(purchasePrice: string, markupPercent: number): string {
  const value = parseDecimalInput(purchasePrice);
  if (value === null) return "";

  const result = value * (1 + markupPercent / 100);

  // Kuruş hassasiyeti yeter; toFixed'in bilimsel gösterime kaçmaması için
  // sonuç sonlu değilse boş döndürülür.
  return Number.isFinite(result) ? result.toFixed(2) : "";
}

/**
 * Satır biçimi formdan forma değiştiği için senkron bu arayüz üzerinden
 * çalışır. Teklif formu ile yük formlarının satırları farklı alan adları
 * kullanıyor; kuralın kendisi tek yerde kalsın diye yalnızca okuma/yazma
 * adımları dışarıdan veriliyor.
 */
export interface FreightRowAdapter<TRow> {
  itemId: (row: TRow) => number | null;
  /** "1" = alış, "2" = satış. */
  buysell: (row: TRow) => string;
  /** Otomatik açılmış satırın türediği ALIŞ kaleminin kimliği; elle satırda null. */
  autoFromItem: (row: TRow) => number | null;
  netPrice: (row: TRow) => string;
  totalPrice: (row: TRow) => string;
  createSaleRow: (source: TRow, pair: FreightPair, netPrice: string, totalPrice: string) => TRow;
  updateSaleRow: (row: TRow, netPrice: string, totalPrice: string) => TRow;
}

/**
 * Otomatik satış satırlarını alış satırlarıyla eşitler. ÇAĞRILDIĞI HER YERDE
 * AYNI SONUCU ÜRETİR (idempotent): listeyi baştan kurmaz, yalnızca eksik olanı
 * ekler, otomatik olanı günceller, karşılığı kalmayanı düşürür.
 *
 * Kurallar:
 *   • Eşleşmesi olmayan kalemde hiçbir şey yapılmaz.
 *   • AYNI SATIŞ KALEMİNE DÜŞEN BİRDEN ÇOK ALIŞ TEK SATIR ÜRETİR ve fiyatı
 *     İLK GİRİLEN alış satırından gelir (kullanıcı isteği). Navlun alışı
 *     birden fazla satıra bölünebiliyor — her biri için ayrı satış satırı
 *     açmak satışı çoğaltırdı.
 *   • Kullanıcı o satış kalemini ELLE eklemişse otomatik satır AÇILMAZ —
 *     "boşsa doldur" kuralı budur.
 *   • Kullanıcı otomatik satırın fiyatına dokunduysa (adaptör autoFromItem'ı
 *     null döndürür) satır artık güncellenmez.
 *   • Alış satırı silinir ya da kalemi değişirse ona ait otomatik satır da
 *     kalkar; elle düzenlenmiş satır KALIR, çünkü o artık kullanıcınındır.
 */
export function syncFreightSaleRows<TRow>(
  rows: TRow[], pairs: FreightPair[], adapter: FreightRowAdapter<TRow>,
): TRow[] {
  if (pairs.length === 0) return rows;

  const purchases = rows
    .filter((r) => adapter.buysell(r) === "1")
    .map((r) => ({ row: r, pair: pairForPurchase(pairs, adapter.itemId(r)) }))
    .filter((x): x is { row: TRow; pair: FreightPair } => x.pair !== undefined);

  // SATIŞ KALEMİ BAŞINA İLK ALIŞ. Map ekleme sırasını koruyor ve satırlar
  // formdaki giriş sırasında geldiği için "ilk giren" tam olarak bu.
  const firstBySaleItem = new Map<number, { row: TRow; pair: FreightPair }>();

  for (const purchase of purchases)
    if (!firstBySaleItem.has(purchase.pair.sale_item_id))
      firstBySaleItem.set(purchase.pair.sale_item_id, purchase);

  // Karşılığı kalmayan otomatik satırları düşür. Ölçüt, satırı DOĞURAN alış
  // kaleminin hâlâ "ilk" olması: ilk satır silinince yerine geçen alış farklı
  // bir kalem olabilir, o zaman eski otomatik satır kalkar ve yenisi açılır.
  const liveSourceItemIds = new Set(
    [...firstBySaleItem.values()].map((p) => p.pair.purchase_item_id));

  let result = rows.filter((r) => {
    const from = adapter.autoFromItem(r);
    return from === null || liveSourceItemIds.has(from);
  });

  for (const { row: purchase, pair } of firstBySaleItem.values()) {
    const net = markedUpPrice(adapter.netPrice(purchase), pair.markup_percent);
    const total = markedUpPrice(adapter.totalPrice(purchase), pair.markup_percent);

    const autoIndex = result.findIndex((r) => adapter.autoFromItem(r) === pair.purchase_item_id);

    if (autoIndex >= 0) {
      result = result.map((r, i) => (i === autoIndex ? adapter.updateSaleRow(r, net, total) : r));
      continue;
    }

    // Kullanıcı bu satış kalemini zaten elle eklediyse dokunulmaz.
    const manualExists = result.some(
      (r) => adapter.buysell(r) === "2" && adapter.itemId(r) === pair.sale_item_id);

    if (!manualExists)
      result = [...result, adapter.createSaleRow(purchase, pair, net, total)];
  }

  return result;
}
