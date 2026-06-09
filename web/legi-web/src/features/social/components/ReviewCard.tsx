import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import type { QueryKey } from "@tanstack/react-query";
import { Avatar } from "../../../components/ui/Avatar";
import { StarRating } from "../../../components/ui/StarRating";
import { SpoilerContent } from "../../../components/ui/SpoilerContent";
import { InteractionBar } from "./InteractionBar";
import { parseActivityData } from "../lib/feed";
import { relativeTime } from "../lib/time";
import type { FeedItemDto } from "../types";

interface ReviewCardProps {
  item: FeedItemDto;
  listKey: QueryKey;
}

/**
 * A single book review in the book details page's reviews list. Renders the
 * author, their rating, the review text (spoiler-hidden when flagged), and the
 * like/comment interaction bar — reusing the same Social feed infrastructure.
 */
export function ReviewCard({ item, listKey }: ReviewCardProps) {
  const { t } = useTranslation();
  const data = parseActivityData(item);
  const review = data.kind === "ReviewCreated" ? data : null;

  return (
    <div className="rounded-xl border border-stone-200 dark:border-dark-raised bg-white dark:bg-dark-card p-4">
      <div className="flex items-start gap-3">
        <Link to={`/users/${item.actorId}`}>
          <Avatar src={item.actorAvatarUrl ?? undefined} fallback={item.actorUsername} size="md" />
        </Link>
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
            <Link
              to={`/users/${item.actorId}`}
              className="font-semibold text-stone-800 dark:text-stone-100 hover:text-green-700 transition-colors"
            >
              {item.actorUsername}
            </Link>
            <span className="text-xs text-stone-400 dark:text-stone-500">
              {relativeTime(item.createdAt, t)}
            </span>
          </div>

          {review?.rating != null && (
            <div className="mt-0.5">
              <StarRating rating={review.rating} showValue={false} size={14} />
            </div>
          )}

          {review?.content &&
            (review.isSpoiler ? (
              <SpoilerContent content={review.content} />
            ) : (
              <p className="mt-2 whitespace-pre-wrap break-words text-sm leading-relaxed text-stone-600 dark:text-stone-300">
                {review.content}
              </p>
            ))}
        </div>
      </div>

      <InteractionBar item={item} listKey={listKey} />
    </div>
  );
}
