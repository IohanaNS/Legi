import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import LanguageDetector from "i18next-browser-languagedetector";
import ptBR from "./locales/pt-BR.json";
import en from "./locales/en.json";

export const SUPPORTED_LANGUAGES = ["pt-BR", "en"] as const;
export type SupportedLanguage = (typeof SUPPORTED_LANGUAGES)[number];

/**
 * Normalizes any detected code (e.g. "en-US", "en-GB", "pt", "pt-PT") to one of
 * the supported resource keys. Unknown codes pass through and end up on the
 * fallback (pt-BR).
 *
 * NOTE: do not use `nonExplicitSupportedLngs`/`load: "languageOnly"` here — they
 * strip "pt-BR" down to "pt" during the t() lookup, and since there is no "pt"
 * bundle every key would fail and render as its raw key on screen.
 */
function normalizeLanguage(detected: string): string {
  if (detected.toLowerCase().startsWith("pt")) return "pt-BR";
  if (detected.toLowerCase().startsWith("en")) return "en";
  return detected;
}

i18n
  .use(LanguageDetector) // detects language from the browser / localStorage
  .use(initReactI18next)
  .init({
    resources: {
      "pt-BR": { translation: ptBR },
      en: { translation: en },
    },
    // no fixed `lng` — the detector decides; falls back to pt-BR when no match
    fallbackLng: "pt-BR",
    supportedLngs: SUPPORTED_LANGUAGES,
    detection: {
      // prefer the user's saved choice, then the browser language
      order: ["localStorage", "navigator"],
      lookupLocalStorage: "i18nextLng",
      caches: ["localStorage"],
      // maps "en-US" → "en", "pt-BR"/"pt-PT" → "pt-BR" before the lookup
      convertDetectedLanguage: normalizeLanguage,
    },
    interpolation: {
      escapeValue: false, // React already does XSS protection, no need to escape
    },
  });

export default i18n;
