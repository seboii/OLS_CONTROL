import { useEffect, useState } from "react";
import { X } from "lucide-react";
import { api, type DataMessage, type Paginated } from "@/lib/api";
import { useDebouncedValue } from "@/lib/hooks";
import { Modal } from "@/components/ui/Overlay";
import { Btn, FormField, SearchInput, TextInput } from "@/components/ui/primitives";

export interface AccountOption {
  id: number;
  name: string | null;
  siber_id?: string | null;
  // Yalnızca list/detay uçlarının AccountRefDto'sunda dolu gelir (bkz. LoadDtos.cs).
  country_id?: { id: string; name: string | null } | null;
}

/**
 * Cari seçici: müşteri/gönderici/alıcı/acente gibi Account referanslı alanlar
 * için ortak bileşen. Ham ID metin kutusu yerine gerçek arama+seçim sunar
 * (backend AccountRefDto id+name döndürüyor, kullanıcı ID ezberlemez).
 */
export function AccountPicker({ label, value, onChange, required, error, accountType }: {
  label: string;
  value: AccountOption | null;
  onChange: (v: AccountOption | null) => void;
  required?: boolean;
  error?: string;
  /** olsold: SelectAjax fetchParams={account_type_id}. Örn. Acente=5, Navlun Ödeyen Firma=1. */
  accountType?: number;
}) {
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search);
  const [results, setResults] = useState<AccountOption[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!open) return;
    setLoading(true);
    api
      .get<DataMessage<Paginated<AccountOption>>>("/api/v1/account", {
        search: debouncedSearch || undefined,
        account_type_id: accountType,
        per_page: 8,
        page: 1,
      })
      .then((res) => setResults(res.data.data))
      .catch(() => setResults([]))
      .finally(() => setLoading(false));
  }, [open, debouncedSearch, accountType]);

  return (
    <FormField label={label} required={required} error={error}>
      <div className="flex gap-1.5">
        <TextInput value={value?.name ?? ""} onChange={() => {}} disabled placeholder="Seçilmedi" />
        <Btn variant="secondary" size="sm" onClick={() => { setSearch(""); setOpen(true); }}>Seç</Btn>
        {value && (
          <button type="button" onClick={() => onChange(null)}
            className="shrink-0 px-2 rounded-lg border border-gray-200 text-gray-400 hover:text-red-500 hover:border-red-200 transition-colors">
            <X size={14} />
          </button>
        )}
      </div>

      <Modal open={open} onClose={() => setOpen(false)} title={`${label} Seç`}>
        <div className="w-[420px] max-w-full">
          <SearchInput value={search} onChange={setSearch} placeholder="Cari adı, vergi no, e-posta..." />
          <div className="mt-3 max-h-80 overflow-y-auto space-y-1">
            {loading ? (
              <p className="text-xs text-gray-400 text-center py-6">Yükleniyor...</p>
            ) : results.length === 0 ? (
              <p className="text-xs text-gray-400 text-center py-6">Sonuç bulunamadı.</p>
            ) : (
              results.map((r) => (
                <button
                  key={r.id}
                  type="button"
                  onClick={() => { onChange(r); setOpen(false); }}
                  className="w-full text-left px-3 py-2 rounded-lg text-sm text-gray-700 hover:bg-blue-50 hover:text-blue-700 transition-colors"
                >
                  {r.name ?? `#${r.id}`}
                </button>
              ))
            )}
          </div>
        </div>
      </Modal>
    </FormField>
  );
}
