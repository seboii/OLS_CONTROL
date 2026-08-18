import { LookupManagerModal, type LookupRecord } from "@/components/shared/LookupManagerModal";
import { FormField, TextInput, SelectInput } from "@/components/ui/primitives";
import type { Column } from "@/components/ui/DataTable";

interface FinancialItemRecord extends LookupRecord {
  name: string;
  type: number;
}

const EMPTY: FinancialItemRecord = { id: 0, name: "", type: 1 };

const TYPE_OPTIONS = [
  { value: "1", label: "Alış" },
  { value: "2", label: "Satış" },
];

function typeLabel(type: number) {
  return type === 1 ? "Alış" : type === 2 ? "Satış" : "—";
}

const COLUMNS: Column<FinancialItemRecord>[] = [
  { key: "name", header: "Adı", render: (r) => <span className="text-sm text-gray-800">{r.name}</span> },
  {
    key: "type",
    header: "Tür",
    render: (r) => (
      <span className="inline-flex items-center gap-1.5 text-xs text-gray-600">
        <span className={`w-1.5 h-1.5 rounded-full ${r.type === 1 ? "bg-green-400" : "bg-red-400"}`} />
        {typeLabel(r.type)}
      </span>
    ),
  },
];

/** olsold: components/FeatureModals/LoadFinancialItem.vue ("Yük Finansal Ürünleri" listesi). */
export function FinancialItemManagerModal({ open, onClose, onSaved }: { open: boolean; onClose: () => void; onSaved?: () => void }) {
  return (
    <LookupManagerModal<FinancialItemRecord>
      open={open}
      onClose={onClose}
      title="Yük Finansal Ürünleri"
      endpoint="/api/v1/financial_item"
      columns={COLUMNS}
      emptyRecord={EMPTY}
      formTitle={(isEdit) => (isEdit ? "Yük Finansal Ürünü Düzenle" : "Yük Finansal Ürünü Oluştur")}
      onSaved={onSaved}
      renderFields={(data, update) => (
        <>
          <FormField label="Adı" required>
            <TextInput value={data.name} onChange={(v) => update({ name: v })} />
          </FormField>
          <FormField label="Tür" required>
            <SelectInput value={String(data.type)} onChange={(v) => update({ type: Number(v) })} options={TYPE_OPTIONS} />
          </FormField>
        </>
      )}
    />
  );
}
