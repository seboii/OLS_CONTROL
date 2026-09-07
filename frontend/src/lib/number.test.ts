import { describe, expect, it } from "vitest";
import { computeLademeter, parseDecimalInput, parseIntegerInput } from "./number";

/**
 * SUNUCUDAKİ `TurkishDecimalTests` İLE AYNI ÖRNEKLER.
 *
 * Yük düzenleme ekranı JSON gövde gönderiyor ve sayıları tarayıcıda çeviriyor,
 * yani sunucudaki `TurkishDecimal.Parse` bu yolda hiç çalışmıyor. İki
 * ayrıştırıcının ayrışması, kullanıcının hangi ekranı kullandığına göre farklı
 * fiyat kaydedilmesi demek olurdu — bu yüzden örnekler bilerek aynı.
 */
describe("parseDecimalInput", () => {
  it.each([
    ["1234.56", 1234.56],
    ["1234,56", 1234.56],
    ["32,5", 32.5],
    ["0,5", 0.5],
    ["100", 100],
  ])("duz ondalik: %s -> %s", (input, expected) => {
    expect(parseDecimalInput(input)).toBe(expected);
  });

  it.each([
    ["1.234,56", 1234.56],
    ["1,234.56", 1234.56],
  ])("binlik ayrac + ondalik: %s -> %s", (input, expected) => {
    expect(parseDecimalInput(input)).toBe(expected);
  });

  it("tek ayrac + tam 3 hane binlik ayractir", () => {
    // "1.250" bin iki yuz elli demek, 1.25 DEGIL.
    expect(parseDecimalInput("1.250")).toBe(1250);
    expect(parseDecimalInput("1,250")).toBe(1250);
  });

  it("tek ayrac + 3'ten az hane ondaliktir", () => {
    expect(parseDecimalInput("1.25")).toBe(1.25);
    expect(parseDecimalInput("1,25")).toBe(1.25);
  });

  it("negatif deger cozulur", () => {
    expect(parseDecimalInput("-45,90")).toBe(-45.9);
  });

  it.each([null, undefined, "", "   "])("bos giris null doner: %s", (input) => {
    expect(parseDecimalInput(input)).toBeNull();
  });

  it("sayi olmayan giris null doner, patlamaz", () => {
    expect(parseDecimalInput("abc")).toBeNull();
    expect(parseDecimalInput(",")).toBeNull();
    expect(parseDecimalInput("-")).toBeNull();
  });

  /**
   * DÜZELTİLEN HATANIN KENDİSİ. Eski ayrıştırıcı
   * `Number(v.replace(",", "."))` idi ve üç girişte birden yanlıştı; NaN da
   * `JSON.stringify` ile `null` olarak gidiyordu, yani fiyat sessizce
   * kayboluyordu.
   */
  it.each([
    ["1.850,75", 1850.75],
    ["1.250", 1250],
    ["1,234.56", 1234.56],
  ])("eski ayristiricinin bozdugu giris artik dogru: %s -> %s", (input, expected) => {
    const eski = (v: string) => (v.trim() === "" ? null : Number(v.replace(",", ".")));

    expect(parseDecimalInput(input)).toBe(expected);
    // Eski davranış: ya NaN (JSON'da null) ya da bin kat sapma.
    expect(eski(input)).not.toBe(expected);
  });
});

describe("parseIntegerInput", () => {
  it("tam sayiyi dogrudan cozer", () => {
    expect(parseIntegerInput("42")).toBe(42);
  });

  it("ondalikli girisi asagi yuvarlar", () => {
    expect(parseIntegerInput("1.234,56")).toBe(1234);
  });

  it("bos giris null doner", () => {
    expect(parseIntegerInput("")).toBeNull();
  });
});

describe("computeLademeter", () => {
  it("(en x boy) / 24000 formulunu uygular", () => {
    // 240 x 100 cm -> 1.00 lademetre
    expect(computeLademeter("240", "100")).toBe("1.00");
  });

  it("virgullu ve binlik ayracli girisi de cozer", () => {
    expect(computeLademeter("240,5", "100")).toBe("1.00");
    expect(computeLademeter("1.200", "100")).toBe("5.00");
  });

  it("eksik veya sifir olcude bos doner", () => {
    expect(computeLademeter("", "100")).toBe("");
    expect(computeLademeter("240", "")).toBe("");
    expect(computeLademeter("0", "100")).toBe("");
    expect(computeLademeter("abc", "100")).toBe("");
  });
});
