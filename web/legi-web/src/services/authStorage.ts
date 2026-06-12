const USER = "legi.user";
const LEGACY_ACCESS = "legi.accessToken";
const LEGACY_REFRESH = "legi.refreshToken";

let accessToken: string | null = null;

function clearLegacyBearerSecrets() {
  localStorage.removeItem(LEGACY_ACCESS);
  localStorage.removeItem(LEGACY_REFRESH);
}

export interface StoredUser {
  userId: string;
  email: string;
  username: string;
}

export const authStorage = {
  getAccessToken: () => accessToken,
  getUser: (): StoredUser | null => {
    const raw = localStorage.getItem(USER);
    return raw ? (JSON.parse(raw) as StoredUser) : null;
  },
  setSession: (s: { accessToken: string; user: StoredUser }) => {
    accessToken = s.accessToken;
    clearLegacyBearerSecrets();
    localStorage.setItem(USER, JSON.stringify(s.user));
  },
  setTokens: (t: { accessToken: string }) => {
    accessToken = t.accessToken;
    clearLegacyBearerSecrets();
  },
  clear: () => {
    accessToken = null;
    clearLegacyBearerSecrets();
    localStorage.removeItem(USER);
  },
};
