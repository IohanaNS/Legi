import type { BackendReadingStatus, ProfileTab, ProgressType } from "../types";

type StatusTab = Exclude<ProfileTab, "lists" | "activity">;

const TAB_TO_STATUS: Record<StatusTab, BackendReadingStatus> = {
  reading: "Reading",
  finished: "Finished",
  paused: "Paused",
  abandoned: "Abandoned",
  not_started: "NotStarted",
};

export const tabToStatus = (tab: StatusTab) => TAB_TO_STATUS[tab];

// Backend PascalCase status -> i18n key under `profile.status.*`.
const STATUS_I18N_KEY: Record<BackendReadingStatus, string> = {
  NotStarted: "not_started",
  Reading: "reading",
  Finished: "finished",
  Abandoned: "abandoned",
  Paused: "paused",
};

export const statusI18nKey = (status: BackendReadingStatus) => STATUS_I18N_KEY[status];

// Badge variant per status (matches components/ui/Badge variants).
type BadgeVariant = "secondary" | "primary" | "success" | "danger" | "warning";

const STATUS_VARIANT: Record<BackendReadingStatus, BadgeVariant> = {
  NotStarted: "secondary",
  Reading: "primary",
  Finished: "success",
  Abandoned: "danger",
  Paused: "warning",
};

export const statusVariant = (status: BackendReadingStatus) => STATUS_VARIANT[status];

export function progressPercent(
  value?: number | null,
  type?: ProgressType | null,
  pageCount?: number | null,
): number | null {
  if (value == null || type == null) return null;
  if (type === "Percentage") return value;
  if (type === "Page" && pageCount) return Math.round((value / pageCount) * 100);
  return null;
}
