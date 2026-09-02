import { useState, type FormEvent } from "react";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { motion } from "motion/react";
import { clsx } from "clsx";
import {
  Mail, Lock, RefreshCw, AlertCircle, Truck, FileText, Users, BarChart2, Eye, EyeOff,
} from "lucide-react";
import { useAuth } from "@/lib/auth";

const FEATURES = [
  { icon: Truck, text: "Sefer & yük yönetimi" },
  { icon: FileText, text: "Teklif → fatura akışı" },
  { icon: Users, text: "Müşteri & kullanıcı portalı" },
  { icon: BarChart2, text: "Canlı raporlama ve metrikler" },
];

export function LoginPage() {
  const { user, loading, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [focused, setFocused] = useState<string | null>(null);
  const [generalError, setGeneralError] = useState("");
  const [submitting, setSubmitting] = useState(false);

  if (!loading && user) {
    const from = (location.state as { from?: Location })?.from?.pathname ?? "/panel";
    return <Navigate to={from} replace />;
  }

  // olsold: GuestLogin.vue — tüm giriş hatalarında (doğrulama, 401, ağ hatası
  // fark etmeksizin) TEK genel mesaj gösterilir; hangi alanın yanlış olduğu
  // (e-posta mı şifre mi) bilinçli olarak belirtilmez.
  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setGeneralError("");

    if (!email || !password) {
      setGeneralError("Lütfen tüm alanları doldurun.");
      return;
    }

    setSubmitting(true);
    try {
      await login(email, password);
      navigate("/panel", { replace: true });
    } catch {
      setGeneralError("Giriş bilgileri hatalı.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="flex h-screen overflow-hidden" style={{ fontFamily: "'Inter', system-ui, sans-serif" }}>
      {/* Left branding panel */}
      <motion.div
        initial={{ x: -60, opacity: 0 }}
        animate={{ x: 0, opacity: 1 }}
        transition={{ duration: 0.55, ease: [0.25, 0.1, 0.25, 1] }}
        className="hidden lg:flex flex-col w-[440px] xl:w-[520px] shrink-0 relative overflow-hidden"
        style={{ backgroundColor: "#0D1B2E" }}
      >
        <div
          className="absolute inset-0 opacity-[0.04]"
          style={{ backgroundImage: "linear-gradient(#fff 1px, transparent 1px), linear-gradient(90deg, #fff 1px, transparent 1px)", backgroundSize: "48px 48px" }}
        />
        <motion.div
          animate={{ scale: [1, 1.08, 1], opacity: [0.12, 0.2, 0.12] }}
          transition={{ duration: 6, repeat: Infinity, ease: "easeInOut" }}
          className="absolute -top-32 -left-32 w-[500px] h-[500px] rounded-full"
          style={{ background: "radial-gradient(circle, #2563EB 0%, transparent 70%)" }}
        />
        <motion.div
          animate={{ scale: [1, 1.12, 1], opacity: [0.08, 0.15, 0.08] }}
          transition={{ duration: 8, repeat: Infinity, ease: "easeInOut", delay: 2 }}
          className="absolute -bottom-40 -right-20 w-[400px] h-[400px] rounded-full"
          style={{ background: "radial-gradient(circle, #3B82F6 0%, transparent 70%)" }}
        />

        <div className="relative z-10 flex flex-col h-full p-10 xl:p-12">
          <motion.div
            initial={{ opacity: 0, y: -20 }} animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.4, delay: 0.25 }}
            className="flex items-center gap-3 mb-14"
          >
            <div className="w-10 h-10 bg-blue-600 rounded-xl flex items-center justify-center shadow-lg shadow-blue-900/40">
              <span className="text-white text-sm font-bold font-mono tracking-tight">OLS</span>
            </div>
            <div>
              <p className="text-white text-base font-semibold leading-none">OLS Lojistik</p>
              <p className="text-blue-400/70 text-[11px] font-mono mt-0.5">Yönetim Sistemi</p>
            </div>
          </motion.div>

          <motion.div
            initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.4, delay: 0.35 }}
            className="mb-10"
          >
            <h1 className="text-3xl xl:text-4xl font-bold text-white leading-tight tracking-tight">
              Lojistiğinizi<br />
              <span className="text-blue-400">tek ekrandan</span><br />
              yönetin.
            </h1>
            <p className="text-slate-400 text-sm mt-4 leading-relaxed max-w-xs">
              Seferden faturaya, tekliften teslimata — tüm operasyonunuz OLS&apos;te.
            </p>
          </motion.div>

          <div className="space-y-3 mb-auto">
            {FEATURES.map((f, i) => {
              const Icon = f.icon;
              return (
                <motion.div key={f.text}
                  initial={{ opacity: 0, x: -16 }} animate={{ opacity: 1, x: 0 }}
                  transition={{ duration: 0.3, delay: 0.45 + i * 0.08 }}
                  className="flex items-center gap-3"
                >
                  <div className="w-8 h-8 rounded-lg bg-blue-600/20 border border-blue-500/20 flex items-center justify-center shrink-0">
                    <Icon size={14} className="text-blue-400" />
                  </div>
                  <span className="text-slate-300 text-sm">{f.text}</span>
                </motion.div>
              );
            })}
          </div>
        </div>
      </motion.div>

      {/* Right form panel */}
      <div className="flex-1 flex items-center justify-center bg-[#F4F6FA] p-6 sm:p-10">
        <motion.div
          initial={{ opacity: 0, y: 28, scale: 0.97 }}
          animate={{ opacity: 1, y: 0, scale: 1 }}
          transition={{ duration: 0.45, delay: 0.15, ease: [0.25, 0.1, 0.25, 1] }}
          className="w-full max-w-[380px]"
        >
          <div className="flex items-center gap-2.5 mb-8 lg:hidden">
            <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
              <span className="text-white text-xs font-bold font-mono">OLS</span>
            </div>
            <span className="font-semibold text-gray-800">OLS Lojistik</span>
          </div>

          <div className="mb-8">
            <h2 className="text-2xl font-bold text-gray-900 tracking-tight">Giriş Yap</h2>
            <p className="text-sm text-gray-500 mt-1">Giriş bilgilerinizi eksiksiz doldurunuz.</p>
          </div>

          {generalError && (
            <div className="flex items-center gap-2 p-3 mb-4 bg-red-50 border border-red-200 rounded-xl text-xs text-red-700">
              <AlertCircle size={14} className="shrink-0" />
              {generalError}
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider block mb-1.5">
                E-Posta veya Siber Kullanıcı Kodu
              </label>
              <div className={clsx(
                "relative border rounded-xl bg-white transition-all duration-150 overflow-hidden",
                focused === "email" ? "border-blue-500 ring-2 ring-blue-500/20" : "border-gray-200",
              )}>
                <Mail size={14} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-gray-400" />
                {/* type="text": Siber kullanıcı kodu ("FATIHT") e-posta biçiminde
                    değil; type="email" bu girişleri tarayıcıda daha gönderilmeden
                    reddediyordu. autoCapitalize kapalı — kod büyük harfli ama
                    doğrulama zaten harfe duyarsız, mobilde otomatik büyütme
                    e-posta yazan kullanıcıyı yanıltırdı. */}
                <input type="text" inputMode="email" autoCapitalize="none" autoCorrect="off"
                  autoComplete="username" spellCheck={false}
                  value={email} onChange={(e) => setEmail(e.target.value)}
                  onFocus={() => setFocused("email")} onBlur={() => setFocused(null)}
                  className="w-full pl-9 pr-4 py-3 text-sm bg-transparent focus:outline-none text-gray-800"
                  placeholder="ad@olslojistik.com veya SIBER KODU"
                />
              </div>
            </div>

            <div>
              <label className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider block mb-1.5">Şifre</label>
              <div className={clsx(
                "relative border rounded-xl bg-white transition-all duration-150 overflow-hidden",
                focused === "pass" ? "border-blue-500 ring-2 ring-blue-500/20" : "border-gray-200",
              )}>
                <Lock size={14} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-gray-400" />
                <input type={showPassword ? "text" : "password"} value={password} onChange={(e) => setPassword(e.target.value)}
                  onFocus={() => setFocused("pass")} onBlur={() => setFocused(null)}
                  className="w-full pl-9 pr-10 py-3 text-sm bg-transparent focus:outline-none text-gray-800"
                  placeholder="Şifre Giriniz"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((v) => !v)}
                  tabIndex={-1}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
                  aria-label={showPassword ? "Şifreyi gizle" : "Şifreyi göster"}
                >
                  {showPassword ? <EyeOff size={14} /> : <Eye size={14} />}
                </button>
              </div>
            </div>

            <motion.button
              type="submit"
              disabled={submitting}
              whileTap={{ scale: 0.98 }}
              className={clsx(
                "w-full flex items-center justify-center gap-2 py-3 rounded-xl text-sm font-semibold transition-all duration-200",
                "focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 focus-visible:ring-offset-2",
                submitting
                  ? "bg-blue-400 text-white cursor-not-allowed"
                  : "bg-blue-600 text-white hover:bg-blue-700 active:bg-blue-800 shadow-md shadow-blue-600/25",
              )}
            >
              {submitting ? (
                <span className="flex items-center gap-2">
                  <RefreshCw size={14} className="animate-spin" />Giriş yapılıyor...
                </span>
              ) : (
                "Giriş Yap"
              )}
            </motion.button>
          </form>

          <div className="mt-6 p-3.5 bg-blue-50 border border-blue-100 rounded-xl">
            <p className="text-[11px] text-blue-700 font-medium">Geliştirme ortamı girişi</p>
            <p className="text-[11px] text-blue-600/80 mt-0.5 font-mono">admin@ols-scoped.local</p>
          </div>

          <p className="text-[11px] text-gray-400 text-center mt-8">
            © {new Date().getFullYear()} OLS Lojistik A.Ş. · Tüm hakları saklıdır.
          </p>
        </motion.div>
      </div>
    </div>
  );
}
