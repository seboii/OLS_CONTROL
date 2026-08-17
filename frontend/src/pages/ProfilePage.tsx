import { useEffect, useState } from "react";
import { CheckCircle, Trash2, Upload } from "lucide-react";
import { api, ApiError, type DataMessage } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useToast } from "@/components/ui/Toast";
import { useLookupOptions } from "@/lib/hooks";
import { Btn, FormField, Tabs, TextInput, SelectInput } from "@/components/ui/primitives";
import { activeColorName, primary_colors, setThemeColor } from "@/config/theme-config";

function initials(name: string, surname: string) {
  return `${name.charAt(0)}${surname.charAt(0)}`.toUpperCase();
}

interface ProfileCountry {
  id: string;
  name: string | null;
}

interface ProfileDetail {
  name: string | null;
  surname: string | null;
  email: string | null;
  phone: string | null;
  country_id: ProfileCountry | null;
  phone_country_id: ProfileCountry | null;
}

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function ProfilePage() {
  const { user, refresh } = useAuth();
  const { addToast } = useToast();
  const [tab, setTab] = useState("Genel");
  const [saving, setSaving] = useState(false);

  const [general, setGeneral] = useState({ name: user?.name ?? "", surname: user?.surname ?? "", country_id: "" });
  const [contact, setContact] = useState({ email: user?.email ?? "", phone: user?.phone ?? "", phone_country_id: "" });
  const [pw, setPw] = useState({ current_password: "", new_password: "", new_password_confirmation: "" });
  const [pwError, setPwError] = useState("");
  const [generalErrors, setGeneralErrors] = useState<Record<string, string[]>>({});
  const [contactErrors, setContactErrors] = useState<Record<string, string[]>>({});

  const [avatarFile, setAvatarFile] = useState<File | null>(null);
  const [avatarPreview, setAvatarPreview] = useState<string | null>(null);
  const [removeAvatar, setRemoveAvatar] = useState(false);
  const [avatarError, setAvatarError] = useState("");

  const { options: countries } = useLookupOptions("/api/v1/country");

  useEffect(() => {
    api
      .get<DataMessage<ProfileDetail>>("/api/v1/profile")
      .then((res) => {
        const d = res.data;
        setGeneral({ name: d.name ?? "", surname: d.surname ?? "", country_id: d.country_id?.id ?? "" });
        setContact({ email: d.email ?? "", phone: d.phone ?? "", phone_country_id: d.phone_country_id?.id ?? "" });
      })
      .catch(() => {
        // GET /api/v1/profile başarısız olursa AuthContext'teki user (daha az alan) korunur.
      });
  }, []);

  useEffect(() => {
    if (!avatarFile) {
      setAvatarPreview(null);
      return;
    }
    const url = URL.createObjectURL(avatarFile);
    setAvatarPreview(url);
    return () => URL.revokeObjectURL(url);
  }, [avatarFile]);

  // olsold: AvatarFile.vue — yalnızca resim MIME tipi ve en fazla 5MB.
  function pickAvatar(file: File) {
    if (!file.type.startsWith("image/")) {
      setAvatarError("Lütfen sadece resim dosyası yükleyin.");
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      setAvatarError("Dosya boyutu 5MB'dan küçük olmalıdır.");
      return;
    }
    setAvatarError("");
    setAvatarFile(file);
    setRemoveAvatar(false);
  }

  function clearAvatar() {
    setAvatarError("");
    setAvatarFile(null);
    setRemoveAvatar(true);
  }

  // olsold: AccountGeneralFormModal.vue Vuelidate kuralları.
  function validateGeneral(): Record<string, string[]> {
    const errs: Record<string, string[]> = {};
    if (!general.name.trim()) errs.name = ["İsim alanı boş bırakılamaz."];
    if (!general.surname.trim()) errs.surname = ["Soyisim alanı boş bırakılamaz."];
    if (!general.country_id) errs.country_id = ["Ülke alanı boş bırakılamaz."];
    return errs;
  }

  // olsold: AccountContactFormModal.vue Vuelidate kuralları.
  function validateContact(): Record<string, string[]> {
    const errs: Record<string, string[]> = {};
    if (!contact.email.trim()) errs.email = ["E-Posta alanı boş bırakılamaz."];
    else if (!EMAIL_PATTERN.test(contact.email)) errs.email = ["Geçerli bir e-posta adresi giriniz."];
    if (!contact.phone.trim()) errs.phone = ["Telefon alanı boş bırakılamaz."];
    else if (!/^\d+$/.test(contact.phone)) errs.phone = ["Telefon numarası sadece rakamlardan oluşmalıdır."];
    if (!contact.phone_country_id) errs.phone_country_id = ["Ülke kodu boş bırakılamaz."];
    return errs;
  }

  async function saveGeneral() {
    const clientErrors = validateGeneral();
    if (Object.keys(clientErrors).length > 0) {
      setGeneralErrors(clientErrors);
      return;
    }

    setSaving(true);
    setGeneralErrors({});
    const fd = new FormData();
    fd.append("name", general.name);
    fd.append("surname", general.surname);
    fd.append("country_id", general.country_id);
    if (avatarFile) fd.append("avatar", avatarFile);
    else if (removeAvatar) fd.append("avatar_remove", "1");
    try {
      await api.postForm("/api/v1/profile/general/update", fd);
      addToast("Profil güncellendi");
      setAvatarFile(null);
      setRemoveAvatar(false);
      await refresh();
    } catch (err) {
      if (err instanceof ApiError && err.errors) setGeneralErrors(err.errors);
      else addToast(err instanceof Error ? err.message : "Güncellenemedi", "error");
    } finally {
      setSaving(false);
    }
  }

  async function saveContact() {
    const clientErrors = validateContact();
    if (Object.keys(clientErrors).length > 0) {
      setContactErrors(clientErrors);
      return;
    }

    setSaving(true);
    setContactErrors({});
    const fd = new FormData();
    fd.append("email", contact.email);
    fd.append("phone", contact.phone);
    fd.append("phone_country_id", contact.phone_country_id);
    try {
      await api.postForm("/api/v1/profile/contact/update", fd);
      addToast("İletişim bilgileri güncellendi");
      await refresh();
    } catch (err) {
      if (err instanceof ApiError && err.errors) setContactErrors(err.errors);
      else addToast(err instanceof Error ? err.message : "Güncellenemedi", "error");
    } finally {
      setSaving(false);
    }
  }

  async function savePassword() {
    setSaving(true);
    setPwError("");
    if (pw.new_password !== pw.new_password_confirmation) {
      setPwError("Yeni şifreler eşleşmiyor.");
      setSaving(false);
      return;
    }
    const fd = new FormData();
    fd.append("current_password", pw.current_password);
    fd.append("new_password", pw.new_password);
    fd.append("new_password_confirmation", pw.new_password_confirmation);
    try {
      await api.postForm("/api/v1/profile/password/update", fd);
      addToast("Şifre güncellendi");
      setPw({ current_password: "", new_password: "", new_password_confirmation: "" });
    } catch (err) {
      setPwError(err instanceof Error ? err.message : "Şifre güncellenemedi");
    } finally {
      setSaving(false);
    }
  }

  const countryOptions = [{ value: "", label: "Seçiniz" }, ...countries.map((c) => ({ value: String(c.id), label: c.name }))];
  // olsold: AccountContactFormModal.vue — SelectAjax optionLabel="phone_code", "+{{ phone_code }}" olarak gösterir (ülke adı değil).
  const phoneCodeOptions = [
    { value: "", label: "Seçiniz" },
    ...countries.map((c) => ({ value: String(c.id), label: c.phone_code ? `+${c.phone_code}` : c.name })),
  ];

  return (
    <div className="flex flex-col h-full">
      <div className="px-6 py-3.5 bg-white border-b border-gray-200 shrink-0">
        <h1 className="text-base font-semibold text-gray-900">Hesabım</h1>
      </div>
      <div className="flex-1 overflow-y-auto bg-white">
        <Tabs tabs={["Genel", "İletişim", "Şifre", "Arayüz"]} active={tab} onChange={setTab} className="px-6" />
        {tab === "Genel" && (
          <div className="p-6 max-w-md space-y-4">
            <FormField label="Profil Fotoğrafı" error={avatarError}>
              <div className="flex items-center gap-3">
                {avatarPreview ? (
                  <img src={avatarPreview} alt="" className="w-14 h-14 rounded-full object-cover" />
                ) : !removeAvatar && user?.avatar ? (
                  <img src={`/storage/${user.avatar}`} alt="" className="w-14 h-14 rounded-full object-cover" />
                ) : (
                  <div className="w-14 h-14 rounded-full bg-blue-600 flex items-center justify-center text-lg text-white font-bold">
                    {initials(user?.name ?? "", user?.surname ?? "")}
                  </div>
                )}
                <div className="flex gap-2">
                  <label className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-gray-200 text-xs text-gray-600 cursor-pointer hover:border-blue-300 hover:text-blue-600 transition-colors">
                    <Upload size={13} />Değiştir
                    <input
                      type="file"
                      accept="image/*"
                      className="hidden"
                      onChange={(e) => { const f = e.target.files?.[0]; if (f) pickAvatar(f); e.target.value = ""; }}
                    />
                  </label>
                  {(avatarPreview || (!removeAvatar && user?.avatar)) && (
                    <button
                      type="button"
                      onClick={clearAvatar}
                      className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-gray-200 text-xs text-gray-400 hover:border-red-200 hover:text-red-500 transition-colors"
                    >
                      <Trash2 size={13} />Kaldır
                    </button>
                  )}
                </div>
              </div>
            </FormField>
            <FormField label="Ad" required error={generalErrors.name?.[0]}>
              <TextInput value={general.name} onChange={(v) => setGeneral((f) => ({ ...f, name: v }))} error={!!generalErrors.name} />
            </FormField>
            <FormField label="Soyad" required error={generalErrors.surname?.[0]}>
              <TextInput value={general.surname} onChange={(v) => setGeneral((f) => ({ ...f, surname: v }))} error={!!generalErrors.surname} />
            </FormField>
            <FormField label="Ülke" required error={generalErrors.country_id?.[0]}>
              <SelectInput value={general.country_id} onChange={(v) => setGeneral((f) => ({ ...f, country_id: v }))} options={countryOptions} />
            </FormField>
            <Btn onClick={saveGeneral} disabled={saving}>
              <CheckCircle size={14} />
              Kaydet
            </Btn>
          </div>
        )}
        {tab === "İletişim" && (
          <div className="p-6 max-w-md space-y-4">
            <FormField label="E-posta" required error={contactErrors.email?.[0]}>
              <TextInput value={contact.email} onChange={(v) => setContact((f) => ({ ...f, email: v }))} type="email" error={!!contactErrors.email} />
            </FormField>
            <FormField label="Ülke Kodu" required error={contactErrors.phone_country_id?.[0]}>
              <SelectInput value={contact.phone_country_id} onChange={(v) => setContact((f) => ({ ...f, phone_country_id: v }))} options={phoneCodeOptions} />
            </FormField>
            <FormField label="Telefon" required error={contactErrors.phone?.[0]}>
              <TextInput value={contact.phone} onChange={(v) => setContact((f) => ({ ...f, phone: v }))} error={!!contactErrors.phone} />
            </FormField>
            <Btn onClick={saveContact} disabled={saving}>
              <CheckCircle size={14} />
              Kaydet
            </Btn>
          </div>
        )}
        {tab === "Şifre" && (
          <div className="p-6 max-w-sm space-y-4">
            {pwError && <p className="text-xs text-red-600">{pwError}</p>}
            <FormField label="Mevcut Şifre" required>
              <TextInput value={pw.current_password} onChange={(v) => setPw((f) => ({ ...f, current_password: v }))} type="password" />
            </FormField>
            <FormField label="Yeni Şifre" required>
              <TextInput value={pw.new_password} onChange={(v) => setPw((f) => ({ ...f, new_password: v }))} type="password" />
            </FormField>
            <FormField label="Yeni Şifre Tekrar" required>
              <TextInput value={pw.new_password_confirmation} onChange={(v) => setPw((f) => ({ ...f, new_password_confirmation: v }))} type="password" />
            </FormField>
            <Btn onClick={savePassword} disabled={saving}>
              <CheckCircle size={14} />
              Şifreyi Güncelle
            </Btn>
          </div>
        )}
        {tab === "Arayüz" && (
          <div className="p-6 max-w-md space-y-4">
            <div>
              <div className="font-semibold text-gray-800 mb-1">Renk</div>
              <p className="text-xs text-gray-500 mb-3">Uygulamanın arayüz ayarlarını düzenleyebilirsiniz.</p>
              <div className="flex flex-wrap gap-2">
                {primary_colors.map((color) => (
                  <button
                    key={color.name}
                    type="button"
                    title={color.name}
                    onClick={() => setThemeColor(color.name)}
                    className={
                      "w-5 h-5 rounded-full transition-shadow " +
                      (activeColorName === color.name ? "ring-2 ring-offset-2 ring-gray-400" : "")
                    }
                    style={{ backgroundColor: color.colors.default }}
                  />
                ))}
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
