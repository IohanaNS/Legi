import { useTranslation } from "react-i18next";
import { Globe } from "lucide-react";
import { SUPPORTED_LANGUAGES, type SupportedLanguage } from "../../i18n";
import { cn } from "../../lib/utils";

const LANGUAGE_LABELS: Record<SupportedLanguage, string> = {
  "pt-BR": "PT",
  en: "EN",
};

type Tone = "light" | "sidebar";

interface LanguageToggleProps {
  /** "light" for light backgrounds (auth cards), "sidebar" for the dark nav bar. */
  tone?: Tone;
  className?: string;
}

/** Normalizes regional variants (e.g. "en-US") to a supported language. */
function resolveLanguage(raw: string): SupportedLanguage {
  if (raw === "pt-BR" || raw.startsWith("pt")) return "pt-BR";
  return "en";
}

export function LanguageToggle({ tone = "light", className }: LanguageToggleProps) {
  const { i18n, t } = useTranslation();
  const current = resolveLanguage(i18n.language);

  const container =
    tone === "sidebar"
      ? "bg-black/20 text-green-200"
      : "bg-stone-100 dark:bg-white/10 text-stone-500 dark:text-stone-400";

  const activeBtn =
    tone === "sidebar"
      ? "bg-brand text-white"
      : "bg-white dark:bg-white/15 text-stone-800 dark:text-stone-100 shadow-sm";

  const inactiveBtn =
    tone === "sidebar"
      ? "hover:bg-white/10 hover:text-white"
      : "hover:text-stone-700 dark:hover:text-stone-200";

  return (
    <div
      role="group"
      aria-label={t("language.label")}
      className={cn("flex items-center gap-1 rounded-lg p-1", container, className)}
    >
      <Globe size={14} className="mx-1 shrink-0 opacity-70" aria-hidden />
      {SUPPORTED_LANGUAGES.map((lng) => (
        <button
          key={lng}
          type="button"
          onClick={() => i18n.changeLanguage(lng)}
          aria-pressed={current === lng}
          className={cn(
            "flex-1 rounded-md px-2 py-1 text-xs font-medium transition-colors",
            current === lng ? activeBtn : inactiveBtn,
          )}
        >
          {LANGUAGE_LABELS[lng]}
        </button>
      ))}
    </div>
  );
}
