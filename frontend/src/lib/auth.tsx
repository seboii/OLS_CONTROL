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

interface AuthContextValue {
  user: AuthUser | null;
  loading: boolean;
  permissions: Record<string, PermissionRow>;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  can: (slug: string, action: PermissionAction) => boolean;
  refresh: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [permissions, setPermissions] = useState<Record<string, PermissionRow>>({});
  const [loading, setLoading] = useState(true);

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
        await loadPermissions(res.data.id);
      } else {
        setUser(null);
      }
    } catch {
      setUser(null);
    } finally {
      setLoading(false);
    }
  }, [loadPermissions]);

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
      await loadPermissions(res.data.user.id);
    },
    [loadPermissions],
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
    () => ({ user, loading, permissions, login, logout, can, refresh }),
    [user, loading, permissions, login, logout, can, refresh],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}

export { ApiError };
