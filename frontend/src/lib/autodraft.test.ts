import { beforeEach, describe, expect, it, vi } from "vitest";
import { formatDraftTime, listDrafts, newDraftId, removeDraft, saveDraft } from "./autodraft";

/**
 * OTOMATİK TASLAKLAR — ÇOKLU.
 *
 * Bir dönem her ekran TEK bir anahtara yazıyordu ve her yeni form öncekinin
 * üzerine biniyordu: bir yükü yarım bırakıp diğerini açmak mümkün değildi.
 * Kural artık şu ve üçü de burada kilitleniyor:
 *
 *   - "Yeni ..." YENİ kimlik açar, önceki taslak yerinde kalır
 *   - taslaktan devam edilince O kimlik benimsenir (taslak çoğalmaz)
 *   - kayıt başarılı olunca YALNIZCA o kimlik silinir
 *
 * Ayrıca hiçbir depolama hatası sayfayı düşürmemeli: gizli sekmede ya da
 * site verisi kapalıyken localStorage erişimi istisna atabiliyor.
 */
describe("autodraft", () => {
  const KEY = "test.drafts.v2";

  beforeEach(() => {
    localStorage.clear();
  });

  it("bos anahtarda bos liste doner", () => {
    expect(listDrafts(KEY)).toEqual([]);
  });

  it("kaydedilen taslak geri okunur", () => {
    saveDraft(KEY, "d1", { name: "Yuk A" });

    const drafts = listDrafts<{ name: string }>(KEY);

    expect(drafts).toHaveLength(1);
    expect(drafts[0].id).toBe("d1");
    expect(drafts[0].payload).toEqual({ name: "Yuk A" });
  });

  it("ayni kimlige ikinci kayit uzerine yazar, cogaltmaz", () => {
    saveDraft(KEY, "d1", { name: "ilk" });
    saveDraft(KEY, "d1", { name: "guncel" });

    const drafts = listDrafts<{ name: string }>(KEY);

    expect(drafts).toHaveLength(1);
    expect(drafts[0].payload).toEqual({ name: "guncel" });
  });

  it("farkli kimlikler yan yana durur", () => {
    saveDraft(KEY, "d1", { name: "yuk" });
    saveDraft(KEY, "d2", { name: "sefer" });

    expect(listDrafts(KEY).map((d) => d.id)).toEqual(expect.arrayContaining(["d1", "d2"]));
    expect(listDrafts(KEY)).toHaveLength(2);
  });

  it("en yeni taslak basta listelenir", () => {
    vi.useFakeTimers();
    try {
      vi.setSystemTime(new Date("2026-09-01T10:00:00Z"));
      saveDraft(KEY, "eski", {});
      vi.setSystemTime(new Date("2026-09-02T10:00:00Z"));
      saveDraft(KEY, "yeni", {});
    } finally {
      vi.useRealTimers();
    }

    expect(listDrafts(KEY).map((d) => d.id)).toEqual(["yeni", "eski"]);
  });

  it("25 taslak siniri asilinca en eski dusar", () => {
    vi.useFakeTimers();
    try {
      for (let i = 0; i < 26; i++) {
        vi.setSystemTime(new Date(Date.UTC(2026, 8, 1, 0, i)));
        saveDraft(KEY, `d${i}`, { i });
      }
    } finally {
      vi.useRealTimers();
    }

    const ids = listDrafts(KEY).map((d) => d.id);

    expect(ids).toHaveLength(25);
    expect(ids).not.toContain("d0");
    expect(ids).toContain("d25");
  });

  it("silme yalnizca verilen kimligi kaldirir", () => {
    saveDraft(KEY, "d1", {});
    saveDraft(KEY, "d2", {});

    removeDraft(KEY, "d1");

    expect(listDrafts(KEY).map((d) => d.id)).toEqual(["d2"]);
  });

  it("bozuk JSON sayfayi dusurmez, taslak yok sayilir", () => {
    localStorage.setItem(KEY, "{bozuk");

    expect(listDrafts(KEY)).toEqual([]);
  });

  it("dizi olmayan icerik yok sayilir", () => {
    localStorage.setItem(KEY, JSON.stringify({ id: "d1" }));

    expect(listDrafts(KEY)).toEqual([]);
  });

  it("eksik alanli kayitlar elenir", () => {
    localStorage.setItem(KEY, JSON.stringify([
      { id: "saglam", savedAt: "2026-09-01T10:00:00.000Z", payload: {} },
      { id: "yukusuz", savedAt: "2026-09-01T10:00:00.000Z" },
      { savedAt: "2026-09-01T10:00:00.000Z", payload: {} },
      null,
    ]));

    expect(listDrafts(KEY).map((d) => d.id)).toEqual(["saglam"]);
  });

  it("depolama erisilemezse okuma da yazma da sessizce gecer", () => {
    const patlat = () => { throw new DOMException("erisim yok", "SecurityError"); };
    vi.spyOn(Storage.prototype, "getItem").mockImplementation(patlat);
    vi.spyOn(Storage.prototype, "setItem").mockImplementation(patlat);

    expect(() => saveDraft(KEY, "d1", {})).not.toThrow();
    expect(listDrafts(KEY)).toEqual([]);
  });

  it("newDraftId benzersiz uretir", () => {
    const ids = new Set(Array.from({ length: 200 }, newDraftId));

    expect(ids.size).toBe(200);
  });

  it("formatDraftTime gun/ay saat:dakika verir", () => {
    // Ekrandaki "26/08 15:46" bicimi — yil ve saniye GOSTERILMEZ.
    expect(formatDraftTime("2026-08-26T15:46:00")).toBe("26/08 15:46");
  });
});
