import { readRolesFromAccessToken } from "./authClaims";

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
  roles: string[];
}

function normalizeUser(value: unknown): StoredUser | null {
  if (!value || typeof value !== "object") return null;

  const candidate = value as Partial<StoredUser>;
  if (
    typeof candidate.userId !== "string" ||
    typeof candidate.email !== "string" ||
    typeof candidate.username !== "string"
  ) {
    return null;
  }

  return {
    userId: candidate.userId,
    email: candidate.email,
    username: candidate.username,
    roles: Array.isArray(candidate.roles)
      ? candidate.roles.filter((role): role is string => typeof role === "string")
      : [],
  };
}

function readStoredUser(): StoredUser | null {
  const raw = localStorage.getItem(USER);
  if (!raw) return null;

  try {
    return normalizeUser(JSON.parse(raw));
  } catch {
    localStorage.removeItem(USER);
    return null;
  }
}

function writeStoredUser(user: StoredUser) {
  localStorage.setItem(USER, JSON.stringify(user));
}

export const authStorage = {
  getAccessToken: () => accessToken,
  getUser: readStoredUser,
  setSession: (s: { accessToken: string; user: StoredUser }) => {
    accessToken = s.accessToken;
    clearLegacyBearerSecrets();
    writeStoredUser(s.user);
  },
  setTokens: (t: { accessToken: string }) => {
    accessToken = t.accessToken;
    clearLegacyBearerSecrets();
    const user = readStoredUser();
    if (user) {
      writeStoredUser({
        ...user,
        roles: readRolesFromAccessToken(t.accessToken),
      });
    }
  },
  clear: () => {
    accessToken = null;
    clearLegacyBearerSecrets();
    localStorage.removeItem(USER);
  },
};
