import axios, { AxiosError, type InternalAxiosRequestConfig } from "axios";
import { authStorage } from "./authStorage";

interface RefreshResponse {
  token: string;
  expiresAt: string;
}

export interface RefreshSessionResponse extends RefreshResponse {
  userId: string;
  email: string;
  username: string;
}

const REFRESH_LOCK_NAME = "legi.refreshToken";
const REFRESH_LOCK_KEY = "legi.refreshTokenLock";
const REFRESH_LOCK_TTL_MS = 15_000;
const REFRESH_LOCK_POLL_MS = 75;

// Cap request duration so a stalled backend surfaces an error (and the UI's
// retry state) instead of leaving the page stuck on loading skeletons forever.
export const http = axios.create({ baseURL: "/api/v1", timeout: 30_000, withCredentials: true });

// Registered by the AuthProvider; called when refresh fails for good.
let onUnauthorized: (() => void) | null = null;
export function setOnUnauthorized(fn: (() => void) | null) {
  onUnauthorized = fn;
}

let onSessionRefreshed: ((session: RefreshSessionResponse) => void) | null = null;
export function setOnSessionRefreshed(fn: ((session: RefreshSessionResponse) => void) | null) {
  onSessionRefreshed = fn;
}

http.interceptors.request.use((config) => {
  const token = authStorage.getAccessToken();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

let refreshPromise: Promise<string> | null = null;

function delay(ms: number) {
  return new Promise((resolve) => window.setTimeout(resolve, ms));
}

function readRefreshLock() {
  const raw = window.localStorage.getItem(REFRESH_LOCK_KEY);
  if (!raw) return null;

  try {
    return JSON.parse(raw) as { owner?: string; expiresAt?: number };
  } catch {
    window.localStorage.removeItem(REFRESH_LOCK_KEY);
    return null;
  }
}

async function withStorageRefreshLock<T>(operation: () => Promise<T>): Promise<T> {
  if (typeof window === "undefined") return operation();

  const owner = `${Date.now()}:${crypto.randomUUID()}`;

  while (true) {
    const now = Date.now();
    const current = readRefreshLock();

    if (!current?.expiresAt || current.expiresAt <= now) {
      window.localStorage.setItem(
        REFRESH_LOCK_KEY,
        JSON.stringify({ owner, expiresAt: now + REFRESH_LOCK_TTL_MS }),
      );

      const claimed = JSON.parse(window.localStorage.getItem(REFRESH_LOCK_KEY) ?? "{}") as { owner?: string };
      if (claimed.owner === owner) break;
    }

    await delay(REFRESH_LOCK_POLL_MS);
  }

  try {
    return await operation();
  } finally {
    const current = readRefreshLock();
    if (current?.owner === owner) {
      window.localStorage.removeItem(REFRESH_LOCK_KEY);
    }
  }
}

async function withRefreshLock<T>(operation: () => Promise<T>): Promise<T> {
  if (typeof navigator !== "undefined" && "locks" in navigator) {
    return navigator.locks.request(REFRESH_LOCK_NAME, { mode: "exclusive" }, operation);
  }

  return withStorageRefreshLock(operation);
}

export async function refreshSession(): Promise<RefreshSessionResponse> {
  return withRefreshLock(async () => {
    // Raw axios (not `http`) so we don't re-enter this interceptor.
    const { data } = await axios.post<RefreshSessionResponse>(
      "/api/v1/identity/auth/refresh",
      undefined,
      { timeout: 30_000, withCredentials: true },
    );
    authStorage.setTokens({ accessToken: data.token });
    onSessionRefreshed?.(data);
    return data;
  });
}

async function refreshAccessToken(): Promise<string> {
  // Raw axios (not `http`) so we don't re-enter this interceptor.
  const data = await refreshSession();
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
