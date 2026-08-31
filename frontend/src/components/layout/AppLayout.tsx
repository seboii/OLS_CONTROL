import { useState } from "react";
import { Outlet, useLocation, Navigate } from "react-router-dom";
import { motion, AnimatePresence, MotionConfig } from "motion/react";
import { Sidebar, MODULE_LABELS } from "./Sidebar";
import { TopBar } from "./TopBar";
import { useAuth } from "@/lib/auth";

export function AppLayout() {
  const { user, loading } = useAuth();
  const [collapsed, setCollapsed] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const location = useLocation();

  if (loading) {
    return (
      <div className="flex h-screen items-center justify-center" style={{ backgroundColor: "#EEF1F6" }}>
        <div className="w-6 h-6 border-2 border-blue-600 border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (!user) {
    return <Navigate to="/giris" state={{ from: location }} replace />;
  }

  const moduleLabel = MODULE_LABELS[location.pathname] ?? "OLS Lojistik";

  return (
    // reducedMotion="never": bu geçişler/kart girişleri çok küçük (4-8px, ~0.2s) -
    // motion kütüphanesinin varsayılan "user" modu, OS'te "Animasyonları Göster"
    // kapalıyken elemanları initial (opacity:0) durumunda TAKILI bırakıyor
    // (final duruma hiç atlamıyor) - içeriğin görünmez kalması, hafif bir
    // animasyonu atlamaktan çok daha kötü bir sonuç.
    <MotionConfig reducedMotion="never">
      <div
        className="flex h-screen overflow-hidden"
        style={{ fontFamily: "'Inter', system-ui, sans-serif", backgroundColor: "#EEF1F6" }}
      >
        <Sidebar
          collapsed={collapsed}
          onToggle={() => setCollapsed((c) => !c)}
          mobileOpen={mobileOpen}
          onMobileClose={() => setMobileOpen(false)}
        />

        <div className="flex flex-col flex-1 min-w-0 overflow-hidden">
          <TopBar moduleLabel={moduleLabel} onMenuToggle={() => setMobileOpen(true)} />

          <main className="flex-1 overflow-hidden">
            <AnimatePresence mode="wait">
              <motion.div
                key={location.pathname}
                initial={{ opacity: 0, y: 8 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -4 }}
                transition={{ duration: 0.2, ease: "easeOut" }}
                className="h-full"
              >
                <Outlet />
              </motion.div>
            </AnimatePresence>
          </main>
        </div>
      </div>
    </MotionConfig>
  );
}
