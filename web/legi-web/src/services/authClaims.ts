export const AUTH_CLAIMS = {
  role: "role",
  dotnetRole: "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
} as const;

export const AUTH_ROLES = {
  user: "User",
  admin: "Admin",
  moderator: "Moderator",
} as const;

export type AuthRole = (typeof AUTH_ROLES)[keyof typeof AUTH_ROLES];

export function readRolesFromAccessToken(token: string): string[] {
  const payload = readJwtPayload(token);
  if (!payload) return [];

  const roles = [
    ...readStringClaimValues(payload[AUTH_CLAIMS.role]),
    ...readStringClaimValues(payload[AUTH_CLAIMS.dotnetRole]),
  ];

  return Array.from(new Set(roles));
}

function readStringClaimValues(value: unknown): string[] {
  if (typeof value === "string") return [value];
  if (Array.isArray(value)) return value.filter((role): role is string => typeof role === "string");
  return [];
}

function readJwtPayload(token: string): Record<string, unknown> | null {
  const [, payload] = token.split(".");
  if (!payload) return null;

  try {
    const normalized = payload.replace(/-/g, "+").replace(/_/g, "/");
    const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), "=");
    return JSON.parse(globalThis.atob(padded)) as Record<string, unknown>;
  } catch {
    return null;
  }
}
