import { describe, expect, it } from "vitest";
import {
  markedUpPrice, pairForPurchase, syncFreightSaleRows,
  type FreightPair, type FreightRowAdapter,
} from "./freightPairs";

/**
 * NAVLUN ALIŞ → SATIŞ OTOMATİĞİ.
 *
 * Kural: navlun alışı girilince eşleşen satış satırı açılır ve fiyatı alışın
 * %15 fazlası olur; kullanıcı elle değiştirdiyse bir daha ezilmez.
 *
 * Eşleşme addan türetilemez — kara navlununun geliri "KARA NAVLUN GELİRİ"
 * değil "KARA NAVLUN HİZMET BEDELİ"dir ve o ad Siber'de hiç yoktur. Bu yüzden
 * testler de gerçek çiftleri kullanıyor.
 */

const KARA: FreightPair = {
  purchase_item_id: 31772, purchase_item_name: "KARA NAVLUN GİDERİ",
  sale_item_id: 31773, sale_item_name: "KARA NAVLUN HİZMET BEDELİ",
  markup_percent: 15, sale_item_type: 2,
  sale_default_account_id: null, sale_default_account_name: null,
};

const HAVA: FreightPair = {
  purchase_item_id: 30573, purchase_item_name: "HAVA NAVLUN GİDERİ",
  sale_item_id: 30572, sale_item_name: "HAVA NAVLUN GELİRİ",
  markup_percent: 15, sale_item_type: 2,
  sale_default_account_id: null, sale_default_account_name: null,
};

const PAIRS = [KARA, HAVA];

type Row = {
  itemId: number | null; buysell: string; net: string; total: string;
  autoFrom: number | null; etiket?: string;
};

const ADAPTER: FreightRowAdapter<Row> = {
  itemId: (r) => r.itemId,
  buysell: (r) => r.buysell,
  autoFromItem: (r) => r.autoFrom,
  netPrice: (r) => r.net,
  totalPrice: (r) => r.total,
  createSaleRow: (_source, pair, net, total) => ({
    itemId: pair.sale_item_id, buysell: "2", net, total, autoFrom: pair.purchase_item_id,
  }),
  updateSaleRow: (row, net, total) => ({ ...row, net, total }),
};

const alis = (itemId: number, net: string, total = net): Row =>
  ({ itemId, buysell: "1", net, total, autoFrom: null });

const sync = (rows: Row[]) => syncFreightSaleRows(rows, PAIRS, ADAPTER);

describe("markedUpPrice", () => {
  it("alışın %15 fazlasını yazar", () => {
    expect(markedUpPrice("100", 15)).toBe("115.00");
  });

  it("Türkçe sayı girişini sunucuyla aynı kuralla çözer", () => {
    // "1.850,75" binlik ayraçlı; naif Number() burada NaN üretirdi.
    expect(markedUpPrice("1.850,75", 15)).toBe("2128.36");
    expect(markedUpPrice("1.250", 15)).toBe("1437.50");
  });

  it("boş/çözülemeyen girişte 0 DEĞİL boş döner", () => {
    // 0 yazmak formda "açıklama zorunlu" kuralını tetiklerdi.
    expect(markedUpPrice("", 15)).toBe("");
    expect(markedUpPrice("abc", 15)).toBe("");
  });

  it("çift başına farklı oran uygulanabilir", () => {
    expect(markedUpPrice("200", 10)).toBe("220.00");
  });
});

describe("pairForPurchase", () => {
  it("kara navlununu HİZMET BEDELİ'ne eşler", () => {
    expect(pairForPurchase(PAIRS, 31772)?.sale_item_name).toBe("KARA NAVLUN HİZMET BEDELİ");
  });

  it("eşleşmesi olmayan kalemde undefined döner", () => {
    expect(pairForPurchase(PAIRS, 99999)).toBeUndefined();
  });
});

describe("syncFreightSaleRows", () => {
  it("navlun alışı girilince satış satırı açar ve %15 ekler", () => {
    const rows = sync([alis(31772, "100")]);

    expect(rows).toHaveLength(2);
    expect(rows[1]).toMatchObject({
      itemId: 31773, buysell: "2", net: "115.00", autoFrom: 31772,
    });
  });

  it("eşleşmesi olmayan kalemde satır AÇMAZ", () => {
    expect(sync([alis(99999, "100")])).toHaveLength(1);
  });

  it("alış fiyatı değişince otomatik satır güncellenir", () => {
    const once = sync([alis(31772, "100")]);
    const sonra = sync([{ ...once[0], net: "200", total: "200" }, once[1]]);

    expect(sonra[1].net).toBe("230.00");
  });

  it("kullanıcı fiyata dokunduysa bir daha EZİLMEZ", () => {
    const once = sync([alis(31772, "100")]);
    // Elle düzenleme: bağ kopar (autoFrom null) ve fiyat kullanıcınındır.
    const elle = { ...once[1], net: "999", autoFrom: null };

    const sonra = sync([{ ...once[0], net: "500", total: "500" }, elle]);

    expect(sonra).toHaveLength(2);
    expect(sonra[1].net).toBe("999");
  });

  it("kullanıcı satış kalemini ELLE eklemişse otomatik satır açılmaz", () => {
    const elleSatis: Row = { itemId: 31773, buysell: "2", net: "50", total: "50", autoFrom: null };

    const rows = sync([alis(31772, "100"), elleSatis]);

    expect(rows).toHaveLength(2);
    expect(rows[1].net).toBe("50");
  });

  it("alış satırı silinince otomatik satır da kalkar", () => {
    const rows = sync([alis(31772, "100")]);

    expect(sync(rows.filter((r) => r.buysell !== "1"))).toHaveLength(0);
  });

  it("elle düzenlenmiş satır, alışı silinse bile KALIR", () => {
    const rows = sync([alis(31772, "100")]);
    const elle = { ...rows[1], net: "999", autoFrom: null };

    expect(sync([elle])).toEqual([elle]);
  });

  it("birden çok navlun alışını ayrı ayrı eşler", () => {
    const rows = sync([alis(31772, "100"), alis(30573, "200")]);

    expect(rows.filter((r) => r.buysell === "2").map((r) => r.itemId)).toEqual([31773, 30572]);
  });

  it("iki kez çalıştırmak aynı sonucu verir (idempotent)", () => {
    const bir = sync([alis(31772, "100")]);

    expect(sync(bir)).toEqual(bir);
  });

  it("eşleşme listesi boşsa hiçbir şey yapmaz", () => {
    const rows = [alis(31772, "100")];

    expect(syncFreightSaleRows(rows, [], ADAPTER)).toBe(rows);
  });
});
