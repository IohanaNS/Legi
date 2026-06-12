export const TURNSTILE_SITE_KEY = import.meta.env.VITE_TURNSTILE_SITE_KEY?.trim() ?? "";

export function isTurnstileConfigured() {
  return TURNSTILE_SITE_KEY.length > 0;
}
