import type { TFunction } from "i18next";

/**
 * Relative-time formatter (no date library in the project). Returns a localized
 * string like "3h", "2d", or "just now" using the feed.time.* i18n keys.
 */
export function relativeTime(iso: string, t: TFunction): string {
  const then = new Date(iso).getTime();
  if (Number.isNaN(then)) return "";

  const diffSeconds = Math.max(0, Math.floor((Date.now() - then) / 1000));

  if (diffSeconds < 60) return t("feed.time.now");
  const minutes = Math.floor(diffSeconds / 60);
  if (minutes < 60) return t("feed.time.minutes", { count: minutes });
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return t("feed.time.hours", { count: hours });
  const days = Math.floor(hours / 24);
  if (days < 30) return t("feed.time.days", { count: days });
  const months = Math.floor(days / 30);
  if (months < 12) return t("feed.time.months", { count: months });
  const years = Math.floor(months / 12);
  return t("feed.time.years", { count: years });
}
