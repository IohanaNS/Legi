import type { Resource } from "../api";
import type { ActivityData, FeedItemDto, TargetType } from "../types";

/**
 * REST resource for like/comment routes, or null if non-interactable.
 * Driven by targetType at runtime (never hardcoded by activityType):
 *   Post -> posts, Review -> reviews, List -> lists, null -> non-interactable.
 */
export function interactionResource(targetType?: TargetType | null): Resource | null {
  if (targetType === "Post") return "posts";
  if (targetType === "Review") return "reviews";
  if (targetType === "List") return "lists";
  return null;
}

export const isInteractable = (item: FeedItemDto): boolean =>
  interactionResource(item.targetType) !== null;

/** Safely parses FeedItemDto.data and discriminates it by activityType. */
export function parseActivityData(item: FeedItemDto): ActivityData {
  let raw: Record<string, unknown> = {};
  if (item.data) {
    try {
      const parsed = JSON.parse(item.data);
      if (parsed && typeof parsed === "object") raw = parsed as Record<string, unknown>;
    } catch {
      raw = {};
    }
  }
  return { kind: item.activityType, ...raw } as ActivityData;
}

/**
 * Display progress percentage for a ProgressPosted card, or null.
 * Only Percentage progress maps to a %; Page progress has no pageCount in the
 * feed payload, so it degrades to "page N" in the card (see §0.5).
 */
export function feedProgressPercent(d: ActivityData): number | null {
  if (d.kind !== "ProgressPosted") return null;
  if (d.progressType === "Percentage" && d.progress != null) return d.progress;
  return null;
}
