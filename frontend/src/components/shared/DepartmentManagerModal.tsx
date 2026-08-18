import { LookupManagerModal, type LookupRecord } from "@/components/shared/LookupManagerModal";
import { FormField, TextInput } from "@/components/ui/primitives";
import type { Column } from "@/components/ui/DataTable";

interface DepartmentRecord extends LookupRecord {
  name: string;
}

const EMPTY: DepartmentRecord = { id: 0, name: "" };

const COLUMNS: Column<DepartmentRecord>[] = [
  { key: "name", header: "Adı", render: (r) => <span className="text-sm text-gray-800">{r.name}</span> },
];

/**
 * olsold: components/FeatureModals/Department.vue ("Departmanlar" listesi).
 * NOT: kaynakta SET_DEPARTMENTS_MODAL_STATUS çağrısı birçok modülde (Araç'ın
 * Römork Cinsi, Sefer'in Römork/Sefer Durumu/Sefer Tipi/Çalışma Tipi/Departman
 * alanları) kopyala-yapıştır sonucu YANLIŞ alana bağlanmış — o alanların
 * "Yeni Ekle" düğmesi de (kendi tipiyle ilgisiz) bu AYNI Departmanlar
 * penceresini açıyor. Kullanıcı isteğiyle bu hata da birebir taşındı.
 */
export function DepartmentManagerModal({ open, onClose, onSaved }: { open: boolean; onClose: () => void; onSaved?: () => void }) {
  return (
    <LookupManagerModal<DepartmentRecord>
      open={open}
      onClose={onClose}
      title="Departmanlar"
      endpoint="/api/v1/department"
      columns={COLUMNS}
      emptyRecord={EMPTY}
      formTitle={(isEdit) => (isEdit ? "Departman Düzenle" : "Departman Oluştur")}
      onSaved={onSaved}
      renderFields={(data, update) => (
        <FormField label="Adı" required>
          <TextInput value={data.name} onChange={(v) => update({ name: v })} />
        </FormField>
      )}
    />
  );
}
