import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import { api, clearToken, getToken, setToken, setUnauthorizedHandler } from "./api";
import {
  AuthContext,
  DEFAULT_CAPABILITIES,
  type AuthContextValue,
  type AuthUser,
  type Capabilities,
  type PermissionAction,
  type PermissionRow,
  type RoleResponse,
} from "./auth";

/**
 * Oturum sağlayıcısı. Bağlam, tipler ve `useAuth` kancası `auth.tsx`'te —
 * sebep için oraya bakın (hızlı yenileme / only-export-components).
 */
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

  // Sunucu 401 dondugunde yerel oturumu da dusur. Bu olmadan jeton silinip
  // ekran acik kaliyordu: kullanici calisiyormus gibi gorunen bir arayuzde
  // her istekte hata aliyordu.
  useEffect(() => {
    setUnauthorizedHandler(() => {
      setUser(null);
      setPermissions({});
      setCapabilities(DEFAULT_CAPABILITIES);
    });

    return () => setUnauthorizedHandler(null);
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
