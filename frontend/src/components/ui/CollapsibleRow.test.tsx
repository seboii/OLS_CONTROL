import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { CollapsibleRow } from "./CollapsibleRow";

/**
 * Katlanabilir satırın sözleşmesi.
 *
 * Kritik davranış: SİLME DÜĞMESİ kapalıyken de erişilebilir olmalı ama
 * tıklaması satırı açıp kapatmamalı. İkisi aynı alanda olduğu için
 * `stopPropagation` unutulursa kullanıcı silmeye basınca satır da açılıyor.
 */
describe("CollapsibleRow", () => {
  afterEach(cleanup);

  function setup(props: Partial<Parameters<typeof CollapsibleRow>[0]> = {}) {
    const onToggle = vi.fn();
    const onRemove = vi.fn();

    render(
      <CollapsibleRow
        title="Navlun"
        summary="1.850,75 EUR"
        open={false}
        onToggle={onToggle}
        onRemove={onRemove}
        removeTitle="Satırı sil"
        {...props}
      >
        <p>form icerigi</p>
      </CollapsibleRow>,
    );

    return { onToggle, onRemove };
  }

  it("kapaliyken baslik ve ozet gorunur, govde gizlidir", () => {
    setup();

    expect(screen.getByText("Navlun")).toBeTruthy();
    expect(screen.getByText("1.850,75 EUR")).toBeTruthy();
    expect(screen.queryByText("form icerigi")).toBeNull();
  });

  it("acikken govde gorunur", () => {
    setup({ open: true });

    expect(screen.getByText("form icerigi")).toBeTruthy();
  });

  it("basliga tiklayinca acilip kapanir", () => {
    const { onToggle } = setup();

    fireEvent.click(screen.getByRole("button", { name: /Navlun/ }));

    expect(onToggle).toHaveBeenCalledOnce();
  });

  it("Enter ve Space ile de acilip kapanir (klavye erisimi)", () => {
    const { onToggle } = setup();
    const header = screen.getByRole("button", { name: /Navlun/ });

    fireEvent.keyDown(header, { key: "Enter" });
    fireEvent.keyDown(header, { key: " " });

    expect(onToggle).toHaveBeenCalledTimes(2);
  });

  it("baska tuslar acip kapatmaz", () => {
    const { onToggle } = setup();

    fireEvent.keyDown(screen.getByRole("button", { name: /Navlun/ }), { key: "a" });

    expect(onToggle).not.toHaveBeenCalled();
  });

  it("silme dugmesi satiri ACMAZ — tiklama yukari yayilmaz", () => {
    const { onToggle, onRemove } = setup();

    fireEvent.click(screen.getByTitle("Satırı sil"));

    expect(onRemove).toHaveBeenCalledOnce();
    expect(onToggle).not.toHaveBeenCalled();
  });

  it("onRemove verilmezse silme dugmesi hic cizilmez", () => {
    render(
      <CollapsibleRow title="Navlun" summary="—" open={false} onToggle={() => {}}>
        <p>form icerigi</p>
      </CollapsibleRow>,
    );

    expect(screen.queryByTitle("Satırı sil")).toBeNull();
  });
});
