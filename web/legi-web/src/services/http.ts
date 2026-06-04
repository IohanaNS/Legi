import axios, { AxiosError, type InternalAxiosRequestConfig } from "axios";
import { authStorage } from "./authStorage";

export const http = axios.create({ baseURL: "/api/v1" });

// Registered by the AuthProvider; called when refresh fails for good.
let onUnauthorized: (() => void) | null = null;
export function setOnUnauthorized(fn: (() => void) | null) {
  onUnauthorized = fn;
}

http.interceptors.request.use((config) => {
  const token = authStorage.getAccessToken();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

let refreshPromise: Promise<string> | null = null;

async function refreshAccessToken(): Promise<string> {
  const refreshToken = authStorage.getRefreshToken();
  if (!refreshToken) throw new Error("No refresh token");

  // Raw axios (not `http`) so we don't re-enter this interceptor.
  const { data } = await axios.post("/api/v1/identity/auth/refresh", { refreshToken });
  authStorage.setTokens({ accessToken: data.token, refreshToken: data.refreshToken });
  return data.token;
}

http.interceptors.response.use(
  (res) => res,
  async (error: AxiosError) => {
    const original = error.config as (InternalAxiosRequestConfig & { _retried?: boolean }) | undefined;
    const status = error.response?.status;
    const isAuthCall = original?.url?.includes("/identity/auth/");

    const shouldRefresh =
      status === 401 &&
      original &&
      !original._retried &&
      !isAuthCall &&
      !!authStorage.getRefreshToken();

    if (shouldRefresh) {
      original!._retried = true;
      try {
        refreshPromise ??= refreshAccessToken().finally(() => {
          refreshPromise = null;
        });
        const newToken = await refreshPromise;
        original!.headers.Authorization = `Bearer ${newToken}`;
        return http(original!);
      } catch {
        authStorage.clear();
        onUnauthorized?.();
      }
    }

    return Promise.reject(error);
  },
);
