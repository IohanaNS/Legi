const ACCESS = "legi.accessToken";
const REFRESH = "legi.refreshToken";
const USER = "legi.user";

export interface StoredUser {
  userId: string;
  email: string;
  username: string;
}

export const authStorage = {
  getAccessToken: () => localStorage.getItem(ACCESS),
  getRefreshToken: () => localStorage.getItem(REFRESH),
  getUser: (): StoredUser | null => {
    const raw = localStorage.getItem(USER);
    return raw ? (JSON.parse(raw) as StoredUser) : null;
  },
  setSession: (s: { accessToken: string; refreshToken: string; user: StoredUser }) => {
    localStorage.setItem(ACCESS, s.accessToken);
    localStorage.setItem(REFRESH, s.refreshToken);
    localStorage.setItem(USER, JSON.stringify(s.user));
  },
  setTokens: (t: { accessToken: string; refreshToken: string }) => {
    localStorage.setItem(ACCESS, t.accessToken);
    localStorage.setItem(REFRESH, t.refreshToken);
  },
  clear: () => {
    localStorage.removeItem(ACCESS);
    localStorage.removeItem(REFRESH);
    localStorage.removeItem(USER);
  },
};
