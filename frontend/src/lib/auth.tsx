import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { api, ApiError, clearToken, getToken, setToken } from "./api";

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

interface PermissionRow {
  id: number;
  read: 0 | 1;
  create: 0 | 1;
  update: 0 | 1;
  delete: 0 | 1;
  permission_page_name: string;
  permission_page_slug: string;
}

interface RoleResponse {
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

interface AuthContextValue {
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
const DEFAULT_CAPABILITIES: Capabilities = {
  uses_offers: true,
  can_create_direct_load: false,
  // Seçici, yetenekler gelene kadar GİZLİ kalır: yanlışlıkla görünüp
  // kapanmasındansa bir kare geç gelmesi iyidir.
  can_choose_company: false,
  companies: [],
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [permissions, setPermissions] = useState<Record<string, PermissionRow>>({});
  const [capabilities, setCapabilities] = useState<Capabilities>(DEFAULT_CAPABILITIES);
  const [loading, setLoading] = useState(true);

  const loadCapabilities = useCallback(async () => {
    try {
      const res = await api.get<{ data: Capabilities }>("/api/v1/capabilities");
      setCapabilities(res.data);
    } catch {
      // Uç yanıt vermezse varsayılana dönülür; gerçek karar her durumda
      // sunucuda (RequiresOfferModule) veriliyor.
      setCapabilities(DEFAULT_CAPABILITIES);
    }
  }, []);

  const loadPermissions = useCallback(async (userId: number) => {
    try {
      // DİKKAT: bu uç {data,message} zarfı KULLANMAZ — RoleController bilinçli
      // olarak base.Ok() ile çıplak {id, stats} döner (frontend data_store.js
      // ile birebir aynı sözleşme). Diğer uçlarla karıştırmayın.
      const res = await api.get<RoleResponse>("/api/v1/role", { id: userId });
      const map: Record<string, PermissionRow> = {};
      for (const row of res.stats.permission_data) {
        map[row.permission_page_slug] = row;
      }
      setPermissions(map);
    } catch {
      setPermissions({});
    }
  }, []);

  const refresh = useCallback(async () => {
    if (!getToken()) {
      setUser(null);
      setLoading(false);
      return;
    }
    try {
      const res = await api.get<{ data: AuthUser | null; authenticated: boolean }>("/api/v1/auth");
      if (res.authenticated && res.data) {
        setUser(res.data);
        await Promise.all([loadPermissions(res.data.id), loadCapabilities()]);
      } else {
        setUser(null);
      }
    } catch {
      setUser(null);
    } finally {
      setLoading(false);
    }
  }, [loadPermissions, loadCapabilities]);

  useEffect(() => {
    refresh();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const login = useCallback(
    async (email: string, password: string) => {
      const res = await api.post<{ data: { user: AuthUser; token: string }; message: string }>(
        "/api/v1/login",
        { email, password },
      );
      setToken(res.data.token);
      setUser(res.data.user);
      await Promise.all([loadPermissions(res.data.user.id), loadCapabilities()]);
    },
    [loadPermissions, loadCapabilities],
  );

  const logout = useCallback(async () => {
    try {
      await api.post("/api/v1/logout");
    } catch {
      // token zaten geçersizse sorun değil — yerel oturumu yine de temizle.
    } finally {
      clearToken();
      setUser(null);
      setPermissions({});
      setCapabilities(DEFAULT_CAPABILITIES);
    }
  }, []);

  const can = useCallback(
    (slug: string, action: PermissionAction) => {
      const row = permissions[slug];
      // Bilinmeyen slug -> reddet (PermissionService.HasPermissionAsync ile
      // aynı varsayılan DEĞİL: backend bilinmeyen slug'ı serbest bırakır ama
      // frontend'de görünürlük için güvenli taraf reddetmektir; gerçek karar
      // her durumda backend'de verilir).
      if (!row) return false;
      return row[action] === 1;
    },
    [permissions],
  );

  const value = useMemo<AuthContextValue>(
    () => ({ user, loading, permissions, capabilities, login, logout, can, refresh }),
    [user, loading, permissions, capabilities, login, logout, can, refresh],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}

export { ApiError };
