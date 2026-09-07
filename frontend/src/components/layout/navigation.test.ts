import { describe, expect, it } from "vitest";
import { MODULE_LABELS, NAV_ITEMS, visibleNavItems } from "./navigation";

/**
 * MENÜ GÖRÜNÜRLÜĞÜ — İKİ AYRI KURAL.
 *
 * YETKİ "bu kullanıcının bu sayfada hakkı var mı", YETENEK "bu şirket bu iş
 * akışını kullanıyor mu" sorusunu sorar ve ikisi karıştırılamaz: Teklifler ve
 * Yükler AYNI yetki sayfasını (load_management) paylaşıyor, dolayısıyla
 * Teklifler'i yetkiyle gizlemek Yükler'i de gizlerdi. Avrora teklif modülünü
 * kullanmadığı için sekme yalnızca yetenekle kapanabiliyor.
 *
 * Menü üç katmanın yalnızca İLKİ — GİZLİ MENÜ YETKİ DEĞİLDİR. Gerçek karar
 * sunucuda ([RequiresOfferModule], [RequiresPermission]) veriliyor ve orada
 * ayrıca test ediliyor.
 */
describe("menu gorunurlugu", () => {
  const hepsineIzin = () => true;
  const hicbirineIzin = () => false;

  it("yetkisi olmayan kullanici yalnizca yetki istemeyen ogeleri gorur", () => {
    const items = visibleNavItems(hicbirineIzin, true);

    expect(items.every((i) => !i.permissionSlug)).toBe(true);
    // Panel yetki istemiyor: giris yapan herkes gorur.
    expect(items.map((i) => i.path)).toContain("/panel");
  });

  it("teklif modulu kapaliyken Teklifler gizlenir ama Yukler kalir", () => {
    const paths = visibleNavItems(hepsineIzin, false).map((i) => i.path);

    expect(paths).not.toContain("/teklifler");
    expect(paths).toContain("/yukler");
  });

  it("teklif modulu acikken Teklifler gorunur", () => {
    expect(visibleNavItems(hepsineIzin, true).map((i) => i.path)).toContain("/teklifler");
  });

  it("Teklifler ve Yukler ayni yetki sayfasini paylasir", () => {
    // Bu yuzden ayrim yetkiyle DEGIL yetenekle yapiliyor; slug'lar ayrilirsa
    // yetenek katmanina gerek kalmazdi ve bu test uyarir.
    const teklif = NAV_ITEMS.find((i) => i.path === "/teklifler");
    const yuk = NAV_ITEMS.find((i) => i.path === "/yukler");

    expect(teklif?.permissionSlug).toBe("load_management");
    expect(yuk?.permissionSlug).toBe("load_management");
    expect(teklif?.requiresOfferModule).toBe(true);
    expect(yuk?.requiresOfferModule).toBeUndefined();
  });

  it("tek bir sayfaya yetki verilince yalnizca o oge eklenir", () => {
    const paths = visibleNavItems((slug) => slug === "car_management", true).map((i) => i.path);

    expect(paths).toContain("/araclar");
    expect(paths).not.toContain("/musteriler");
    expect(paths).not.toContain("/muhasebe");
  });

  it("MODULE_LABELS her menu ogesini kapsar", () => {
    for (const item of NAV_ITEMS)
      expect(MODULE_LABELS[item.path]).toBe(item.label);
  });

  it("menu yollari benzersiz", () => {
    const paths = NAV_ITEMS.map((i) => i.path);

    expect(new Set(paths).size).toBe(paths.length);
  });
});
