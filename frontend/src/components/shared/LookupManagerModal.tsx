import { useEffect, useState, type ReactNode } from "react";
import { api, type DataMessage, type Paginated } from "@/lib/api";
import { useDebouncedValue } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { Modal } from "@/components/ui/Overlay";
import { DataTable, Pagination, type Column } from "@/components/ui/DataTable";
import { Btn, SearchInput } from "@/components/ui/primitives";

/**
 * olsold: components/FeatureModals/*.vue (Department.vue, LoadFinancialItem.vue,
 * vb.) — "Yeni Ekle" düğmesi aslında doğrudan bir oluşturma formu DEĞİL, tam bir
 * liste-yönetim penceresi açar (ara + listele + satıra tıkla=düzenle + kendi
 * içinde "Yeni Ekle"), o da AYRI bir iç oluştur/düzenle diyaloğu açar. Kaynağın
 * PrimeVue `DatatableAjax`'ının satır-içi (`rowEditor`) düzenlemesi yerine bu
 * portun her yerde kullandığı "satıra tıkla → diyalog aç" deseni izlendi.
 *
 * Kaynakta bu iç form KAYDEDİLDİKTEN SONRA dış listeyi tazeler ama otomatik
 * olarak orijinal alanı YENİ kayda çevirmez/pencereleri kapatmaz — kullanıcı
 * pencereleri kapatıp orijinal dropdown'dan yeniden aramalı. Aynen korundu;
 * tek eklenen şey `onSaved` callback'i — orijinal sayfanın dropdown'ını
 * (`useLookupOptions(...).refresh()`) tazelemesi için, aksi hâlde kaynağın
 * SelectAjax'ının her açılışta arayan mimarisinin aksine burada dropdown bir
 * kez çekilip önbelleklendiğinden yeni kayıt hiç görünmezdi.
 */
export interface LookupRecord {
  id: number;
  name: string | null;
}

export function LookupManagerModal<T extends LookupRecord>({
  open,
  onClose,
  title,
  endpoint,
  columns,
  emptyRecord,
  renderFields,
  formTitle,
  onSaved,
}: {
  open: boolean;
  onClose: () => void;
  title: string;
  endpoint: string;
  columns: Column<T>[];
  emptyRecord: T;
  renderFields: (data: T, update: (patch: Partial<T>) => void) => ReactNode;
  formTitle: (isEdit: boolean) => string;
  onSaved?: () => void;
}) {
  const { addToast } = useToast();

  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);
  const [page, setPage] = useState(1);
  const [rows, setRows] = useState<T[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(false);

  const [editOpen, setEditOpen] = useState(false);
  const [editData, setEditData] = useState<T>(emptyRecord);
  const [saving, setSaving] = useState(false);

  function load() {
    if (!open) return;
    setLoading(true);
    api
      .get<DataMessage<Paginated<T>>>(endpoint, { search: debouncedSearch || undefined, per_page: 5, page })
      .then((res) => {
        setRows(res.data.data);
        setTotal(res.data.total);
      })
      .catch(() => addToast(`${title} yüklenemedi`, "error"))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, debouncedSearch, page]);

  useEffect(() => {
    if (!open) {
      setSearch("");
      setPage(1);
    }
  }, [open]);

  function openCreate() {
    setEditData(emptyRecord);
    setEditOpen(true);
  }

  function openEditRow(row: T) {
    setEditData(row);
    setEditOpen(true);
  }

  async function handleSubmit() {
    setSaving(true);
    try {
      if (editData.id) {
        await api.put(endpoint, editData);
        addToast(`${title} güncellendi`);
      } else {
        await api.post(endpoint, editData);
        addToast(`${title} oluşturuldu`);
      }
      setEditOpen(false);
      load();
      onSaved?.();
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Kaydedilemedi", "error");
    } finally {
      setSaving(false);
    }
  }

  return (
    <>
      <Modal open={open} onClose={onClose} title={title}>
        <div className="w-[560px] max-w-full">
          <div className="flex items-center justify-between gap-2 mb-3">
            <SearchInput value={search} onChange={(v) => { setSearch(v); setPage(1); }} placeholder="Ara..." />
            <Btn size="sm" onClick={openCreate}>Yeni Ekle</Btn>
          </div>
          <div className="border border-gray-200 rounded-lg overflow-hidden">
            <DataTable data={rows} columns={columns} loading={loading} onRowClick={openEditRow} />
          </div>
          <div className="mt-2">
            <Pagination page={page} total={total} perPage={5} onChange={setPage} />
          </div>
        </div>
      </Modal>

      <Modal open={editOpen} onClose={() => setEditOpen(false)} title={formTitle(!!editData.id)}>
        <div className="w-[420px] max-w-full space-y-4">
          {renderFields(editData, (patch) => setEditData((d) => ({ ...d, ...patch })))}
          <div className="grid grid-cols-2 gap-2">
            <Btn variant="secondary" onClick={() => setEditOpen(false)}>İptal</Btn>
            <Btn onClick={handleSubmit} disabled={saving}>{saving ? "Kaydediliyor..." : "Kaydet"}</Btn>
          </div>
        </div>
      </Modal>
    </>
  );
}
