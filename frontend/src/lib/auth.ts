import { createContext, useContext } from "react";

/**
 * Oturum bağlamı, tipleri ve `useAuth` kancası — SAĞLAYICI BİLEŞENİ
 * `AuthProvider.tsx` dosyasında.
 *
 * Bir modül hem bileşen hem başka bir şey dışa aktarınca Vite'ın hızlı
 * yenilemesi o dosyayı tazeleyemiyor (oxlint `react(only-export-components)`).
 * Ayrıştırma yönü bilinçli: `useAuth` 20 dosyadan, sağlayıcı yalnızca
 * App.tsx'ten çağrılıyor — taşınan bileşen oldu.
 */

export interface AuthUser {
  id: number;
  name: string;
  surname: string;
  email: string;
  phone: string | null;
  avatar: string | null;
  status: boolean;
  /**
   * Kullanıcının Siber karşılığı. Siber'den senkronlanan kullanıcılarda dolu,
   * yalnızca uygulamada açılmış hesaplarda (kurulum admini gibi) NULL'dur.
   * Teklif görevlilerinin nasıl doldurulacağı buna bakar — bkz. QuotesPage.
   */
  siber_code: string | null;
  siber_name: string | null;
}

export interface PermissionRow {
  id: number;
  read: 0 | 1;
  create: 0 | 1;
  update: 0 | 1;
  delete: 0 | 1;
  permission_page_name: string;
  permission_page_slug: string;
}

export interface RoleResponse {
  id: number;
  stats: {
    permission_data: PermissionRow[];
    user_name: string;
  };
}

export type PermissionAction = "read" | "create" | "update" | "delete";

/**
 * Kullanıcının ŞİRKETİNE bağlı açık/kapalı iş akışları — yetkiden AYRI.
 *
 * OLS ve Avrora bu noktada iki ayrı şirket: yük açma yolları birbirini
 * dışlıyor. Avrora teklif kullanmıyor (sekme hiç görünmez, yükü doğrudan
 * açar); OLS teklifle çalışıyor (her yük bir teklifin dönüşümü, teklifsiz
 * açma düğmesi yok).
 *
 * Yetkiyle ifade EDİLEMİYOR çünkü Teklifler ve Yükler ekranları aynı yetki
 * sayfasını (load_management) paylaşıyor — Teklifler'i yetkiyle gizlemek
 * Yükler'i de gizlerdi.
 */
export interface CompanyOption {
  id: string;
  name: string;
}

export interface Capabilities {
  uses_offers: boolean;
  can_create_direct_load: boolean;
  /**
   * Kullanıcı kaydın hangi şirkete açılacağını seçebilir mi. Yalnızca iki
   * şirketi de gören kullanıcıda (süper admin) true; tek şirkete bağlı
   * kullanıcıda seçici hiç gösterilmez, kayıt daima kendi şirketine gider.
   */
  can_choose_company: boolean;
  companies: CompanyOption[];
}

export interface AuthContextValue {
  user: AuthUser | null;
  loading: boolean;
  permissions: Record<string, PermissionRow>;
  capabilities: Capabilities;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  can: (slug: string, action: PermissionAction) => boolean;
  refresh: () => Promise<void>;
}

/**
 * Yetenekler gelmeden önceki hâl. Teklif AÇIK varsayılır: kullanıcıların
 * ezici çoğunluğu OLS tarafında (128/130) ve sekmenin bir an görünüp
 * kaybolması, olması gerekirken hiç görünmemesinden iyidir.
 */
export const DEFAULT_CAPABILITIES: Capabilities = {
  uses_offers: true,
  can_create_direct_load: false,
  // Seçici, yetenekler gelene kadar GİZLİ kalır: yanlışlıkla görünüp
  // kapanmasındansa bir kare geç gelmesi iyidir.
  can_choose_company: false,
  companies: [],
};

export const AuthContext = createContext<AuthContextValue | null>(null);

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}

