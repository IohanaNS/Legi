import { useCallback, useEffect, useState, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { authApi } from "./api";
import { authStorage, type StoredUser } from "../../services/authStorage";
import { setOnUnauthorized } from "../../services/http";
import { AuthContext, type AuthContextValue } from "./authContext";
import type { AuthResponse, LoginRequest, RegisterRequest } from "./types";

function persist(res: AuthResponse): StoredUser {
  const user: StoredUser = { userId: res.userId, email: res.email, username: res.username };
  authStorage.setSession({ accessToken: res.token, user });
  return user;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<StoredUser | null>(() => authStorage.getUser());
  const [isLoading, setIsLoading] = useState(true);
  const queryClient = useQueryClient();

  const clearLocalSession = useCallback(() => {
    authStorage.clear();
    setUser(null);
    queryClient.clear();
  }, [queryClient]);

  useEffect(() => {
    // When refresh fails inside the interceptor, drop the session.
    setOnUnauthorized(clearLocalSession);
    return () => setOnUnauthorized(null);
  }, [clearLocalSession]);

  useEffect(() => {
    let cancelled = false;

    authApi.refresh()
      .then((session) => {
        if (cancelled) return;
        setUser(persist(session));
      })
      .catch(() => {
        if (cancelled) return;
        clearLocalSession();
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [clearLocalSession]);

  const login = async (body: LoginRequest) => {
    setUser(persist(await authApi.login(body)));
  };

  const register = async (body: RegisterRequest) => {
    return authApi.register(body);
  };

  const logout = async () => {
    try {
      await authApi.logout();
    } catch {
      /* best-effort: the local session is dropped regardless */
    }
    clearLocalSession();
  };

  const deleteAccount = async () => {
    await authApi.deleteAccount();
    clearLocalSession();
  };

  // React Compiler memoizes this; manual useMemo is redundant here.
  const value: AuthContextValue = {
    user,
    isAuthenticated: !!user,
    isLoading,
    login,
    register,
    logout,
    deleteAccount,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
