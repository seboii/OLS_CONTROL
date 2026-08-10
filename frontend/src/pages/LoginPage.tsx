import { useState, type FormEvent } from "react";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { AlertCircle } from "lucide-react";
import { useAuth, ApiError } from "@/lib/auth";
import { Btn, TextInput, FormField } from "@/components/ui/primitives";

export function LoginPage() {
  const { user, loading, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  const [generalError, setGeneralError] = useState("");
  const [submitting, setSubmitting] = useState(false);

  if (!loading && user) {
    const from = (location.state as { from?: Location })?.from?.pathname ?? "/musteriler";
    return <Navigate to={from} replace />;
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setErrors({});
    setGeneralError("");
    setSubmitting(true);
    try {
      await login(email, password);
      navigate("/musteriler", { replace: true });
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.errors) setErrors(err.errors);
        else setGeneralError(err.message);
      } else {
        setGeneralError("Beklenmeyen bir hata oluştu.");
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div
      className="flex h-screen items-center justify-center px-4"
      style={{ fontFamily: "'Inter', system-ui, sans-serif", backgroundColor: "#0D1B2E" }}
    >
      <div className="w-full max-w-sm bg-white rounded-xl shadow-2xl p-8">
        <div className="flex flex-col items-center mb-6">
          <div className="w-12 h-12 bg-blue-600 rounded-xl flex items-center justify-center mb-3">
            <span className="text-white text-sm font-bold font-mono">OLS</span>
          </div>
          <h1 className="text-lg font-semibold text-gray-900">Giriş Yap</h1>
          <p className="text-xs text-gray-500 mt-1">Giriş bilgilerinizi eksiksiz doldurunuz.</p>
        </div>

        {generalError && (
          <div className="flex items-center gap-2 p-3 mb-4 bg-red-50 border border-red-200 rounded-lg text-xs text-red-700">
            <AlertCircle size={14} className="shrink-0" />
            {generalError}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <FormField label="E-Posta" required error={errors.email?.[0]}>
            <TextInput value={email} onChange={setEmail} type="email" placeholder="ad@olslojistik.com" error={!!errors.email} />
          </FormField>
          <FormField label="Şifre" required error={errors.password?.[0]}>
            <TextInput
              value={password}
              onChange={setPassword}
              type="password"
              placeholder="Parola Giriniz"
              error={!!errors.password}
            />
          </FormField>
          <Btn type="submit" disabled={submitting} className="w-full justify-center">
            {submitting ? "Giriş yapılıyor..." : "Giriş Yap"}
          </Btn>
        </form>

        <p className="text-center text-[11px] text-gray-400 mt-6">OLS Lojistik tarafından geliştirilmiştir.</p>
      </div>
    </div>
  );
}
