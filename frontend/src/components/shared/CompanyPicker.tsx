import { useAuth } from "@/lib/auth";
import { FormField, SelectInput } from "@/components/ui/primitives";

/**
 * ŞİRKET SEÇİCİ — yalnızca iki şirketi de gören kullanıcıya görünür.
 *
 * Kayıt (yük / sefer / araç / müşteri) hangi şirkete açılacaksa Siber'e o
 * şirketle yazılır ve görünürlük kuralı da ona göre işler. Tek şirkete bağlı
 * kullanıcıda seçici HİÇ görünmez: kaydı kendi şirketine açar, çünkü başka
 * şirkete açsa kendi listesinde göremezdi.
 *
 * Sunucu bu alana körü körüne güvenmez — kapsamı olan kullanıcıda gelen değer
 * yok sayılır (bkz. ICompanyScope.ResolveWriteCompanyAsync).
 */
export function CompanyPicker({ value, onChange, label = "Şirket" }: {
  value: string;
  onChange: (v: string) => void;
  label?: string;
}) {
  const { capabilities } = useAuth();

  if (!capabilities.can_choose_company) return null;

  return (
    <FormField label={label} hint="Kayıt bu şirkete açılır.">
      <SelectInput
        value={value}
        onChange={onChange}
        options={[
          { value: "", label: "Seçiniz" },
          ...capabilities.companies.map((c) => ({ value: c.id, label: c.name })),
        ]}
      />
    </FormField>
  );
}
