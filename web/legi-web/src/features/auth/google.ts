export const GOOGLE_CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID?.trim() ?? "";

export function isGoogleConfigured() {
  return GOOGLE_CLIENT_ID.length > 0;
}
