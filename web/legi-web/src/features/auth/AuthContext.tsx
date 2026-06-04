import {
  createContext, useContext, useEffect, useState, type ReactNode,
} from "react";
import { useQueryClient } from "@tanstack/react-query";
import { authApi } from "./api";
import { authStorage, type StoredUser } from "../../services/authStorage";
import { setOnUnauthorized } from "../../services/http";
import type { AuthResponse, LoginRequest, RegisterRequest } from "./types";

interface AuthContextValue {
  user: StoredUser | null;
  isAuthenticated: boolean;
  login: (body: LoginRequest) => Promise<void>;
  register: (body: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function persist(res: AuthResponse): StoredUser {
  const user: StoredUser = { userId: res.userId, email: res.email, username: res.username };
  authStorage.setSession({ accessToken: res.token, refreshToken: res.refreshToken, user });
  return user;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<StoredUser | null>(() => authStorage.getUser());
  const queryClient = useQueryClient();

  useEffect(() => {
    // When refresh fails inside the interceptor, drop the session.
    setOnUnauthorized(() => setUser(null));
    return () => setOnUnauthorized(null);
  }, []);

  const login = async (body: LoginRequest) => {
    setUser(persist(await authApi.login(body)));
  };

  const register = async (body: RegisterRequest) => {
    setUser(persist(await authApi.register(body)));
  };

  const logout = async () => {
    const rt = authStorage.getRefreshToken();
    try {
      if (rt) await authApi.logout(rt);
    } catch {
      /* best-effort: the local session is dropped regardless */
    }
    authStorage.clear();
    setUser(null);
    queryClient.clear();
  };

  // React Compiler memoizes this; manual useMemo is redundant here.
  const value: AuthContextValue = { user, isAuthenticated: !!user, login, register, logout };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
