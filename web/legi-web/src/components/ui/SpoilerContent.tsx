import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Eye, EyeOff } from "lucide-react";

interface SpoilerContentProps {
  content: string;
}

/**
 * Renders text that is hidden behind a spoiler warning by default and toggles
 * open on click. Shared by the feed and the book reviews list so spoiler
 * behaviour stays identical across the app.
 */
export function SpoilerContent({ content }: SpoilerContentProps) {
  const { t } = useTranslation();
  const [isRevealed, setIsRevealed] = useState(false);

  return (
    <button
      type="button"
      onClick={() => setIsRevealed((current) => !current)}
      className={`mt-1 block w-full rounded-lg border px-3 py-2 text-left text-sm transition-colors focus:outline-none focus:ring-1 focus:ring-green-600 ${
        isRevealed
          ? "border-stone-200 bg-stone-50 text-stone-600 hover:bg-stone-100 dark:border-dark-raised dark:bg-dark-raised/50 dark:text-stone-300 dark:hover:bg-dark-raised"
          : "border-amber-200 bg-amber-50 text-amber-900 hover:bg-amber-100 dark:border-amber-900/50 dark:bg-amber-950/30 dark:text-amber-200"
      }`}
    >
      {isRevealed ? (
        <>
          <span className="whitespace-pre-wrap break-words leading-relaxed">"{content}"</span>
          <span className="mt-2 flex items-center gap-1 text-xs font-medium text-stone-500 dark:text-stone-400">
            <Eye size={13} />
            {t("feed.spoilerHide")}
          </span>
        </>
      ) : (
        <>
          <span className="flex items-center gap-2 font-medium">
            <EyeOff size={14} />
            {t("feed.spoilerWarning")}
          </span>
          <span className="mt-1 block text-xs text-amber-800 dark:text-amber-300">
            {t("feed.spoilerReveal")}
          </span>
        </>
      )}
    </button>
  );
}
