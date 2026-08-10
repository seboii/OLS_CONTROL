import { useState } from "react";
import { CheckCircle } from "lucide-react";
import { api, ApiError } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useToast } from "@/components/ui/Toast";
import { Btn, FormField, Tabs, TextInput } from "@/components/ui/primitives";

export function ProfilePage() {
  const { user } = useAuth();
  const { addToast } = useToast();
  const [tab, setTab] = useState("Genel");
  const [saving, setSaving] = useState(false);

  const [general, setGeneral] = useState({ name: user?.name ?? "", surname: user?.surname ?? "" });
  const [contact, setContact] = useState({ email: user?.email ?? "", phone: user?.phone ?? "" });
  const [pw, setPw] = useState({ current_password: "", new_password: "", new_password_confirmation: "" });
  const [pwError, setPwError] = useState("");
  const [generalErrors, setGeneralErrors] = useState<Record<string, string[]>>({});

  async function saveGeneral() {
    setSaving(true);
    setGeneralErrors({});
    const fd = new FormData();
    fd.append("name", general.name);
    fd.append("surname", general.surname);
    try {
      await api.postForm("/api/v1/profile/general/update", fd);
      addToast("Profil güncellendi");
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
