import { describe, expect, it } from "vitest";
import { act, renderHook } from "@testing-library/react";
import { stableStringify, useDirty } from "@/lib/dirty";

describe("stableStringify", () => {
  it("anahtar sirasi fark yaratmaz", () => {
    expect(stableStringify({ a: 1, b: 2 })).toBe(stableStringify({ b: 2, a: 1 }));
  });

  it("ic ice nesnelerde de sirayi normallestirir", () => {
    expect(stableStringify({ x: { p: 1, q: 2 }, y: [{ n: 1, m: 2 }] }))
      .toBe(stableStringify({ y: [{ m: 2, n: 1 }], x: { q: 2, p: 1 } }));
  });

  it("undefined ile null ayni sayilir", () => {
    // Sunucudan gelen alan null, hiç dokunulmamış alan undefined olabiliyor;
    // ikisi de "boş" demek ve form bu yüzden değişmiş görünmemeli.
    expect(stableStringify({ a: undefined })).toBe(stableStringify({ a: null }));
  });

  it("gercek degisikligi yakalar", () => {
    expect(stableStringify({ a: 1 })).not.toBe(stableStringify({ a: 2 }));
  });

  it("dizi sirasi ONEMLIDIR", () => {
    // Görevli sırası Siber'de hangi sütuna gideceğini belirliyor; sıralamak
    // gerçek bir değişikliği gizlerdi.
    expect(stableStringify([1, 2])).not.toBe(stableStringify([2, 1]));
  });
});

describe("useDirty", () => {
  it("taban alinmadan once izlemez", () => {
    const { result } = renderHook(() => useDirty({ a: 1 }));

    expect(result.current.tracking).toBe(false);
    expect(result.current.dirty).toBe(false);
  });

  it("taban alindiktan sonra ayni icerik temiz kalir", () => {
    const { result, rerender } = renderHook(({ snap }) => useDirty(snap), {
      initialProps: { snap: { a: 1, b: "x" } as Record<string, unknown> },
    });

    act(() => result.current.reset());
    rerender({ snap: { b: "x", a: 1 } });

    expect(result.current.dirty).toBe(false);
    expect(result.current.tracking).toBe(true);
  });

  it("icerik degisince kirlenir, reset ile yeniden temizlenir", () => {
    const { result, rerender } = renderHook(({ snap }) => useDirty(snap), {
      initialProps: { snap: { a: 1 } as Record<string, unknown> },
    });

    act(() => result.current.reset());
    rerender({ snap: { a: 2 } });
    expect(result.current.dirty).toBe(true);

    act(() => result.current.reset());
    expect(result.current.dirty).toBe(false);
  });

  it("clear izlemeyi birakir", () => {
    const { result, rerender } = renderHook(({ snap }) => useDirty(snap), {
      initialProps: { snap: { a: 1 } as Record<string, unknown> },
    });

    act(() => result.current.reset());
    rerender({ snap: { a: 2 } });
    expect(result.current.dirty).toBe(true);

    act(() => result.current.clear());
    expect(result.current.tracking).toBe(false);
    expect(result.current.dirty).toBe(false);
  });
});
