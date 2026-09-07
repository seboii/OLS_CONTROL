import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { LoadCard } from "./LoadCard";
import type { LoadTransferItem } from "./types";

/**
 * Yük listesi kartı.
 *
 * Kartın işi, eksik alanlara rağmen okunur bir özet çıkarmak: canlıdaki
 * kayıtların çoğunda gönderici/alıcı/görevli boş. Numara için üç kademeli
 * geri çekilme var (iş türlü numara -> yük numarası -> `Y<id>`), çünkü
 * uygulamadan yeni açılan yükte numara Siber'den dönene kadar boş kalabiliyor.
 */
describe("LoadCard", () => {
  afterEach(cleanup);

  const bos: LoadTransferItem = {
    id: 42,
    load_number: null,
    load_number_work_type: null,
    created_at: null,
    customer_id: null,
    sender_id: null,
    receiver_id: null,
    load_status_id: null,
    usercode_with_notification: null,
    work_type: null,
  };

  function goster(row: Partial<LoadTransferItem> = {}) {
    const onClick = vi.fn();
    render(<LoadCard row={{ ...bos, ...row }} index={0} onClick={onClick} />);
    return { onClick };
  }

  it("is turlu numara varsa onu gosterir", () => {
    goster({ load_number_work_type: "2600672EX", load_number: "2600672" });

    expect(screen.getByText("2600672EX")).toBeTruthy();
  });

  it("is turlu numara yoksa yuk numarasina duser", () => {
    goster({ load_number: "2600672" });

    expect(screen.getByText("2600672")).toBeTruthy();
  });

  it("iki numara da yoksa kimlikten uretir", () => {
    goster();

    expect(screen.getByText("Y42")).toBeTruthy();
  });

  it("musteri yoksa tire gosterir, kart bos kalmaz", () => {
    goster();

    expect(screen.getByText("Müşteri")).toBeTruthy();
    expect(screen.getAllByText("—").length).toBeGreaterThan(0);
  });

  it("gorevli atanmamissa bunu acikca yazar", () => {
    goster();

    expect(screen.getByText("Görevli atanmadı")).toBeTruthy();
  });

  it("gorevli adi bosluktan ibaretse yine 'atanmadi' sayilir", () => {
    goster({ usercode_with_notification: { id: 1, name: "   " } });

    expect(screen.getByText("Görevli atanmadı")).toBeTruthy();
  });

  it("gonderici/alici yoksa o blok hic cizilmez", () => {
    goster();

    expect(screen.queryByText("Gönderici")).toBeNull();
  });

  it("gonderici varsa blok cizilir", () => {
    goster({ sender_id: { id: 3, name: "ACME GmbH" } });

    expect(screen.getByText("Gönderici")).toBeTruthy();
    expect(screen.getByText("ACME GmbH")).toBeTruthy();
  });

  it("Siber'den silinmis kayit rozetle isaretlenir", () => {
    const { container } = render(
      <LoadCard row={{ ...bos, siber_deleted_at: "2026-09-01T08:34:00" }} index={0} onClick={() => {}} />,
    );

    expect(container.textContent).toContain("Siber'de silinmiş");
  });

  it("tiklama yukari bildirilir", () => {
    const { onClick } = goster({ load_number: "2600672" });

    fireEvent.click(screen.getByText("2600672"));

    expect(onClick).toHaveBeenCalledOnce();
  });
});
