import { LayoutDashboard, Users, FileText, Package, Truck, Receipt, Car, Shield, Headphones, BarChart3, ShieldCheck, Wallet, BookOpen } from "lucide-react";

/**
 * Menü tanımı — Sidebar bileşeninden AYRI dosyada.
 *
 * Bileşenle aynı dosyada dururken oxlint "react(only-export-components)"
 * uyarısı veriyordu: bir modül hem bileşen hem sabit dışa aktarınca Vite'ın
 * hızlı yenilemesi (HMR) o dosyayı tazeleyemiyor, düzenleme sırasında
 * bileşenin durumu sıfırlanıyor.
 */
export interface NavItem {
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

/**
 * Menüde GÖRÜNECEK ögeler. İki farklı kural birden uygulanır ve ikisi
 * karıştırılmamalı:
 *
 *   • YETKİ — "bu kullanıcının bu sayfada hakkı var mı".
 *   • YETENEK — "bu şirket bu iş akışını kullanıyor mu". Teklifler ve Yükler
 *     AYNI yetki sayfasını (load_management) paylaşıyor, dolayısıyla
 *     Teklifler'i yetkiyle gizlemek Yükler'i de gizlerdi.
 *
 * Bileşenden ayrı bir işlev: menü görünürlüğü üç katmanın ilki (menü → rota →
 * uç) ve testi bileşen kurmadan yapılabilsin diye saf tutuldu. GİZLİ MENÜ
 * YETKİ DEĞİLDİR — gerçek karar her durumda sunucuda verilir.
 */
export function visibleNavItems(
  can: (slug: string, action: "read") => boolean,
  usesOffers: boolean,
): NavItem[] {
  return NAV_ITEMS.filter(
    (item) =>
      (!item.permissionSlug || can(item.permissionSlug, "read")) &&
      (!item.requiresOfferModule || usesOffers),
  );
}
