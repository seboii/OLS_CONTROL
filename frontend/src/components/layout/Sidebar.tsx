import { motion, AnimatePresence } from "motion/react";
import { NavLink } from "react-router-dom";
import { ChevronLeft, ChevronRight, LayoutDashboard, Users, FileText, Package, Truck, Receipt, Car, Shield, Headphones, Settings, LogOut, BarChart3, ShieldCheck, Wallet, BookOpen } from "lucide-react";
import { clsx } from "clsx";
import { useAuth } from "@/lib/auth";

interface NavItem {
  path: string;
  label: string;
  icon: React.ComponentType<{ size?: number; className?: string }>;
  permissionSlug?: string;
  /**
   * Yetkiye EK olarak şirketin bu iş akışını kullanıyor olmasını şart koşar.
   * Teklifler ve Yükler aynı yetki sayfasını (load_management) paylaştığı
   * için Teklifler'i yetkiyle gizlemek Yükler'i de gizlerdi.
   */
  requiresOfferModule?: boolean;
}

export const NAV_ITEMS: NavItem[] = [
  { path: "/panel", label: "Dashboard", icon: LayoutDashboard },
  { path: "/musteriler", label: "Müşteriler", icon: Users, permissionSlug: "account_management" },
  // Avrora teklif kullanmıyor: yükü doğrudan Yükler ekranından açıyor.
  { path: "/teklifler", label: "Teklifler", icon: FileText, permissionSlug: "load_management", requiresOfferModule: true },
  { path: "/yukler", label: "Yükler", icon: Package, permissionSlug: "load_management" },
  { path: "/seferler", label: "Seferler", icon: Truck, permissionSlug: "expedition_management" },
  { path: "/faturalar", label: "Faturalar", icon: Receipt, permissionSlug: "invoice_management" },
  { path: "/finans", label: "Finans", icon: Wallet, permissionSlug: "finance_management" },
  { path: "/muhasebe", label: "Muhasebe", icon: BookOpen, permissionSlug: "accounting_management" },
  { path: "/araclar", label: "Araçlar", icon: Car, permissionSlug: "car_management" },
  { path: "/kullanicilar", label: "Kullanıcılar", icon: Shield, permissionSlug: "user_management" },
  { path: "/destek-talepleri", label: "Destek Talepleri", icon: Headphones, permissionSlug: "support_request_management" },
  { path: "/raporlama", label: "Raporlama", icon: BarChart3, permissionSlug: "report_management" },
  // Denetim kaydı yalnızca Yönetim rolünde; menü zaten yetkiye göre filtreliyor.
  { path: "/denetim", label: "Denetim Kaydı", icon: ShieldCheck, permissionSlug: "audit_log_management" },
];

export const MODULE_LABELS: Record<string, string> = Object.fromEntries(
  NAV_ITEMS.map((n) => [n.path, n.label]),
);

export function Sidebar({
  collapsed,
  onToggle,
  mobileOpen,
  onMobileClose,
}: {
  collapsed: boolean;
  onToggle: () => void;
  mobileOpen: boolean;
  onMobileClose: () => void;
}) {
  const { can, logout, capabilities } = useAuth();

  // Bilinmeyen/seed edilmemiş sayfa slug'ı varsayılan olarak REDDEDİLİR
  // (bkz. lib/auth.tsx can()) — bu yüzden hiç yetki satırı olmayan bir admin
  // hesabı hiçbir modülü göremez; seed admin kullanıcısı tüm sayfalarda tam
  // yetkiyle geldiği için bu satırlar normal kullanımda tüm menüyü gösterir.
  const visibleItems = NAV_ITEMS.filter(
    (item) =>
      (!item.permissionSlug || can(item.permissionSlug, "read")) &&
      (!item.requiresOfferModule || capabilities.uses_offers),
  );

  const content = (
    <div className="flex flex-col h-full" style={{ backgroundColor: "#0D1B2E" }}>
      <div className="flex items-center gap-3 px-4 py-4 border-b border-white/10 shrink-0">
        <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center shrink-0">
          <span className="text-white text-xs font-bold font-mono">OLS</span>
        </div>
        {!collapsed && (
          <motion.div initial={{ opacity: 0, x: -8 }} animate={{ opacity: 1, x: 0 }} transition={{ duration: 0.15 }}>
            <p className="text-white text-sm font-semibold leading-none">OLS Lojistik</p>
            <p className="text-blue-300/70 text-[10px] mt-0.5 font-mono">Yönetim Sistemi</p>
          </motion.div>
        )}
      </div>

      <nav className="flex-1 overflow-y-auto py-3 px-2 space-y-0.5">
        {visibleItems.map((item) => {
          const Icon = item.icon;
          return (
            <NavLink
              key={item.path}
              to={item.path}
              onClick={onMobileClose}
              title={collapsed ? item.label : undefined}
              className={({ isActive }) =>
                clsx(
                  "w-full flex items-center gap-3 px-2.5 py-2 rounded-lg text-sm font-medium transition-all duration-150 relative group",
                  isActive ? "bg-blue-600 text-white shadow-sm" : "text-slate-400 hover:text-slate-100 hover:bg-white/8",
                )
              }
            >
              <Icon size={17} className="shrink-0" />
              {!collapsed && (
                <span className="flex-1 text-left leading-none whitespace-nowrap overflow-hidden text-ellipsis">
                  {item.label}
                </span>
              )}
              {collapsed && (
                <div className="absolute left-full ml-2 px-2 py-1 bg-gray-900 text-white text-xs rounded whitespace-nowrap opacity-0 group-hover:opacity-100 pointer-events-none transition-opacity z-50">
                  {item.label}
                </div>
              )}
            </NavLink>
          );
        })}
      </nav>

      <div className="px-2 py-3 border-t border-white/10 space-y-1 shrink-0">
        <NavLink
          to="/hesabim"
          onClick={onMobileClose}
          className="w-full flex items-center gap-3 px-2.5 py-2 rounded-lg text-sm text-slate-400 hover:text-slate-100 hover:bg-white/8 transition-all"
        >
          <Settings size={16} className="shrink-0" />
          {!collapsed && <span>Ayarlar</span>}
        </NavLink>
        <button
          onClick={() => logout()}
          className="w-full flex items-center gap-3 px-2.5 py-2 rounded-lg text-sm text-slate-400 hover:text-slate-100 hover:bg-white/8 transition-all"
        >
          <LogOut size={16} className="shrink-0" />
          {!collapsed && <span>Çıkış Yap</span>}
        </button>
        <button
          onClick={onToggle}
          className="w-full flex items-center gap-3 px-2.5 py-2 rounded-lg text-sm text-slate-500 hover:text-slate-300 hover:bg-white/5 transition-all"
        >
          {collapsed ? <ChevronRight size={16} /> : <ChevronLeft size={16} />}
          {!collapsed && <span className="text-xs">Daralt</span>}
        </button>
      </div>
    </div>
  );

  return (
    <>
      <div
        className={clsx(
          "hidden lg:flex flex-col shrink-0 transition-all duration-200 ease-out",
          collapsed ? "w-[60px]" : "w-[220px]",
        )}
      >
        {content}
      </div>

      <AnimatePresence>
        {mobileOpen && (
          <>
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              transition={{ duration: 0.18 }}
              className="fixed inset-0 bg-black/40 z-40 lg:hidden"
              onClick={onMobileClose}
            />
            <motion.div
              initial={{ x: "-100%" }}
              animate={{ x: 0 }}
              exit={{ x: "-100%" }}
              transition={{ duration: 0.22, ease: [0.25, 0.1, 0.25, 1] }}
              className="fixed left-0 top-0 bottom-0 w-[220px] z-50 lg:hidden"
            >
              {content}
            </motion.div>
          </>
        )}
      </AnimatePresence>
    </>
  );
}
