import { useEffect, useState } from "react";
import { CheckCircle, Trash2, Upload } from "lucide-react";
import { api, ApiError } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useToast } from "@/components/ui/Toast";
import { Btn, FormField, Tabs, TextInput } from "@/components/ui/primitives";

function initials(name: string, surname: string) {
  return `${name.charAt(0)}${surname.charAt(0)}`.toUpperCase();
}

export function ProfilePage() {
  const { user, refresh } = useAuth();
  const { addToast } = useToast();
  const [tab, setTab] = useState("Genel");
  const [saving, setSaving] = useState(false);

  const [general, setGeneral] = useState({ name: user?.name ?? "", surname: user?.surname ?? "" });
  const [contact, setContact] = useState({ email: user?.email ?? "", phone: user?.phone ?? "" });
  const [pw, setPw] = useState({ current_password: "", new_password: "", new_password_confirmation: "" });
  const [pwError, setPwError] = useState("");
  const [generalErrors, setGeneralErrors] = useState<Record<string, string[]>>({});

  const [avatarFile, setAvatarFile] = useState<File | null>(null);
  const [avatarPreview, setAvatarPreview] = useState<string | null>(null);
  const [removeAvatar, setRemoveAvatar] = useState(false);

  useEffect(() => {
    if (!avatarFile) {
      setAvatarPreview(null);
      return;
    }
    const url = URL.createObjectURL(avatarFile);
    setAvatarPreview(url);
    return () => URL.revokeObjectURL(url);
  }, [avatarFile]);

  function pickAvatar(file: File) {
    setAvatarFile(file);
    setRemoveAvatar(false);
  }

  function clearAvatar() {
    setAvatarFile(null);
    setRemoveAvatar(true);
  }

  async function saveGeneral() {
    setSaving(true);
    setGeneralErrors({});
    const fd = new FormData();
    fd.append("name", general.name);
    fd.append("surname", general.surname);
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
    setSaving(true);
    const fd = new FormData();
    fd.append("email", contact.email);
    fd.append("phone", contact.phone);
    try {
      await api.postForm("/api/v1/profile/contact/update", fd);
      addToast("İletişim bilgileri güncellendi");
    } catch (err) {
      addToast(err instanceof Error ? err.message : "Güncellenemedi", "error");
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

  return (
    <div className="flex flex-col h-full">
      <div className="px-6 py-3.5 bg-white border-b border-gray-200 shrink-0">
        <h1 className="text-base font-semibold text-gray-900">Hesabım</h1>
      </div>
      <div className="flex-1 overflow-y-auto bg-white">
        <Tabs tabs={["Genel", "İletişim", "Şifre"]} active={tab} onChange={setTab} className="px-6" />
        {tab === "Genel" && (
          <div className="p-6 max-w-md space-y-4">
            <FormField label="Profil Fotoğrafı">
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
                      accept="image/png,image/jpeg,image/jpg,image/gif,image/webp,image/svg+xml"
                      className="hidden"
                      onChange={(e) => { const f = e.target.files?.[0]; if (f) pickAvatar(f); }}
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
            <FormField label="Ad" error={generalErrors.name?.[0]}>
              <TextInput value={general.name} onChange={(v) => setGeneral((f) => ({ ...f, name: v }))} />
            </FormField>
            <FormField label="Soyad" error={generalErrors.surname?.[0]}>
              <TextInput value={general.surname} onChange={(v) => setGeneral((f) => ({ ...f, surname: v }))} />
            </FormField>
            <Btn onClick={saveGeneral} disabled={saving}>
              <CheckCircle size={14} />
              Kaydet
            </Btn>
          </div>
        )}
        {tab === "İletişim" && (
          <div className="p-6 max-w-md space-y-4">
            <FormField label="E-posta">
              <TextInput value={contact.email} onChange={(v) => setContact((f) => ({ ...f, email: v }))} type="email" />
            </FormField>
            <FormField label="Telefon">
              <TextInput value={contact.phone} onChange={(v) => setContact((f) => ({ ...f, phone: v }))} />
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
      </div>
    </div>
  );
}
