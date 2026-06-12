import axios, { AxiosError, type InternalAxiosRequestConfig } from "axios";
import { authStorage } from "./authStorage";

interface RefreshResponse {
  token: string;
  expiresAt: string;
}

// Cap request duration so a stalled backend surfaces an error (and the UI's
// retry state) instead of leaving the page stuck on loading skeletons forever.
export const http = axios.create({ baseURL: "/api/v1", timeout: 30_000, withCredentials: true });

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
  // Raw axios (not `http`) so we don't re-enter this interceptor.
  const { data } = await axios.post<RefreshResponse>(
    "/api/v1/identity/auth/refresh",
    undefined,
    { timeout: 30_000, withCredentials: true },
  );
  authStorage.setTokens({ accessToken: data.token });
  return data.token;
}

http.interceptors.response.use(
  (res) => res,
  async (error: AxiosError) => {
    const original = error.config as (InternalAxiosRequestConfig & { _retried?: boolean }) | undefined;
    const status = error.response?.status;
    const isTokenCall =
      original?.url?.includes("/identity/auth/login") ||
      original?.url?.includes("/identity/auth/register") ||
      original?.url?.includes("/identity/auth/refresh");

    const shouldRefresh =
      status === 401 &&
      original &&
      !original._retried &&
      !isTokenCall &&
      !!authStorage.getUser();

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
