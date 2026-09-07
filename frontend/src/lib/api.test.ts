import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError, api, clearToken, getToken, setToken, setUnauthorizedHandler } from "./api";

/**
 * API KATMANI — oturum düşmesi ve hata mesajı ayıklama.
 *
 * Canlıda bulunan davranış: 401 yanıtı yalnızca jetonu siliyor, React
 * durumuna dokunmuyordu. Ekran oturum açıkmış gibi durmaya devam ediyor,
 * kullanıcı jeton dolduğunda her tıklamasında "İstek başarısız (401)"
 * alıyordu. Artık kayıtlı bir işleyici çağrılıyor ve AuthProvider oturumu
 * düşürüyor.
 *
 * Kritik istisna: GİRİŞ ucunun kendi 401'i oturum düşmesi DEĞİL — yanlış
 * şifre de 401 döner ve o an zaten oturum yok.
 */
describe("api", () => {
  function mockResponse(status: number, body: unknown, contentType = "application/json") {
    return new Response(body === null ? null : JSON.stringify(body), {
      status,
      headers: { "content-type": contentType },
    });
  }

  beforeEach(() => {
    clearToken();
    sessionStorage.clear();
    setUnauthorizedHandler(null);
  });

  afterEach(() => {
    setUnauthorizedHandler(null);
  });

  describe("401 davranisi", () => {
    it("oturum duserse isleyici cagrilir, jeton silinir, sebep isaretlenir", async () => {
      setToken("jeton");
      const dusur = vi.fn();
      setUnauthorizedHandler(dusur);
      vi.spyOn(globalThis, "fetch").mockResolvedValue(mockResponse(401, { message: "Yetkisiz" }));

      await expect(api.get("/api/v1/load")).rejects.toBeInstanceOf(ApiError);

      expect(dusur).toHaveBeenCalledOnce();
      expect(getToken()).toBeUndefined();
      expect(sessionStorage.getItem("session_expired")).toBe("1");
    });

    it("GIRIS ucunun 401'i oturum dusurmez", async () => {
      const dusur = vi.fn();
      setUnauthorizedHandler(dusur);
      vi.spyOn(globalThis, "fetch").mockResolvedValue(
        mockResponse(401, { message: "Hatalı şifre" }));

      await expect(api.post("/api/v1/login", { email: "a", password: "b" }))
        .rejects.toBeInstanceOf(ApiError);

      expect(dusur).not.toHaveBeenCalled();
      expect(sessionStorage.getItem("session_expired")).toBeNull();
    });

    it("401 disindaki hatalar oturumu dusurmez", async () => {
      setToken("jeton");
      const dusur = vi.fn();
      setUnauthorizedHandler(dusur);
      vi.spyOn(globalThis, "fetch").mockResolvedValue(mockResponse(403, { message: "Yasak" }));

      await expect(api.get("/api/v1/load")).rejects.toBeInstanceOf(ApiError);

      expect(dusur).not.toHaveBeenCalled();
      expect(getToken()).toBe("jeton");
    });
  });

  describe("hata mesaji ayiklama", () => {
    it("message alani kullanilir", async () => {
      vi.spyOn(globalThis, "fetch").mockResolvedValue(
        mockResponse(422, { message: "Departman boş olamaz" }));

      await expect(api.post("/api/v1/load", {}))
        .rejects.toThrowError("Departman boş olamaz");
    });

    it("errors dizisinin ilk ogesi kullanilir", async () => {
      vi.spyOn(globalThis, "fetch").mockResolvedValue(
        mockResponse(400, { errors: ["Kayıt bulunamadı"] }));

      await expect(api.get("/api/v1/load/1")).rejects.toThrowError("Kayıt bulunamadı");
    });

    it("alan bazli errors nesnesinden ilk mesaj alinir ve alanlar korunur", async () => {
      vi.spyOn(globalThis, "fetch").mockResolvedValue(
        mockResponse(422, { errors: { email: ["E-posta zorunlu"] } }));

      const hata = await api.post("/api/v1/user", {}).catch((e: unknown) => e as ApiError);

      expect(hata).toBeInstanceOf(ApiError);
      expect((hata as ApiError).message).toBe("E-posta zorunlu");
      expect((hata as ApiError).errors).toEqual({ email: ["E-posta zorunlu"] });
    });

    it("govde yoksa durum koduyla genel mesaj uretilir", async () => {
      vi.spyOn(globalThis, "fetch").mockResolvedValue(
        new Response(null, { status: 500 }));

      await expect(api.get("/api/v1/load")).rejects.toThrowError("İstek başarısız (500)");
    });
  });

  describe("sorgu dizesi", () => {
    it("bos, null ve undefined degerler atlanir", async () => {
      const fetchSpy = vi.spyOn(globalThis, "fetch")
        .mockResolvedValue(mockResponse(200, { data: [] }));

      await api.get("/api/v1/load", {
        search: "taşıma",
        status: "",
        page: 2,
        only_deleted: undefined,
        customer_id: null,
        include_deleted: false,
      });

      const url = fetchSpy.mock.calls[0][0] as string;

      expect(url).toContain("search=");
      expect(url).toContain("page=2");
      // false ATLANMAZ: "silinmisleri gizle" gibi bilincli bir secim olabilir.
      expect(url).toContain("include_deleted=false");
      expect(url).not.toContain("status=");
      expect(url).not.toContain("only_deleted");
      expect(url).not.toContain("customer_id");
    });
  });

  it("jetonu Authorization basligiyla gonderir", async () => {
    setToken("abc123");
    const fetchSpy = vi.spyOn(globalThis, "fetch")
      .mockResolvedValue(mockResponse(200, { data: null }));

    await api.get("/api/v1/auth");

    const init = fetchSpy.mock.calls[0][1] as RequestInit;

    expect((init.headers as Record<string, string>).Authorization).toBe("Bearer abc123");
  });
});
