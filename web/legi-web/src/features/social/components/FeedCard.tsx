import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import type { QueryKey } from "@tanstack/react-query";
import { BarChart3, CheckCircle, BookOpen, BookPlus, Star, ListPlus, PenLine } from "lucide-react";
import { Card } from "../../../components/ui/Card";
import { Avatar } from "../../../components/ui/Avatar";
import { ProgressBar } from "../../../components/ui/ProgressBar";
import { StarRating } from "../../../components/ui/StarRating";
import { InteractionBar } from "./InteractionBar";
import { parseActivityData, feedProgressPercent, isInteractable } from "../lib/feed";
import { relativeTime } from "../lib/time";
import type { ActivityType, FeedItemDto } from "../types";

interface FeedCardProps {
  item: FeedItemDto;
  listKey: QueryKey;
}

const ACTIVITY_ICON: Record<ActivityType, React.ReactNode> = {
  ProgressPosted: <BarChart3 size={14} className="text-green-600" />,
  BookFinished: <CheckCircle size={14} className="text-green-600" />,
  BookStarted: <BookOpen size={14} className="text-green-600" />,
  BookAdded: <BookPlus size={14} className="text-green-600" />,
  BookRated: <Star size={14} className="text-green-600" />,
  ReviewCreated: <PenLine size={14} className="text-green-600" />,
  ListCreated: <ListPlus size={14} className="text-green-600" />,
};

const ACTIVITY_I18N: Record<ActivityType, string> = {
  ProgressPosted: "activity.progressPosted",
  BookFinished: "activity.bookFinished",
  BookStarted: "activity.bookStarted",
  BookAdded: "activity.bookAdded",
  BookRated: "activity.bookRated",
  ReviewCreated: "activity.reviewCreated",
  ListCreated: "activity.listCreated",
};

export function FeedCard({ item, listKey }: FeedCardProps) {
  const { t } = useTranslation();
  const data = parseActivityData(item);

  return (
    <Card>
      <div className="p-4">
        {/* Header: avatar + verb + book/list reference */}
        <div className="flex items-center gap-3">
          <Link to={`/users/${item.actorId}`}>
            <Avatar
              src={item.actorAvatarUrl ?? undefined}
              fallback={item.actorUsername}
              size="md"
            />
          </Link>
          <div className="min-w-0">
            <p className="text-sm">
              <Link
                to={`/users/${item.actorId}`}
                className="font-semibold text-stone-800 dark:text-stone-100 hover:text-green-700 transition-colors"
              >
                @{item.actorUsername}
              </Link>{" "}
              <span className="inline-flex items-center gap-1 text-stone-500 dark:text-stone-400">
                {ACTIVITY_ICON[item.activityType]}
                {t(ACTIVITY_I18N[item.activityType])}
              </span>{" "}
              {data.kind === "ListCreated" ? (
                <span className="font-semibold text-stone-800 dark:text-stone-100">{data.name}</span>
              ) : (
                item.bookTitle && (
                  <span className="font-semibold text-stone-800 dark:text-stone-100">{item.bookTitle}</span>
                )
              )}
            </p>
            <p className="text-xs text-stone-400 dark:text-stone-500">{relativeTime(item.createdAt, t)}</p>
          </div>
        </div>

        {/* Body */}
        <div className="mt-3 flex gap-3">
          {item.bookCoverUrl ? (
            <img
              src={item.bookCoverUrl}
              alt={item.bookTitle ?? ""}
              className="w-16 h-24 rounded-lg object-cover flex-shrink-0 bg-stone-200"
            />
          ) : (
            item.activityType !== "ListCreated" && (
              <div className="w-16 h-24 bg-stone-200 rounded-lg flex-shrink-0" />
            )
          )}

          <div className="flex-1 min-w-0">
            {item.bookAuthor && (
              <p className="text-xs text-stone-500 dark:text-stone-400 mb-1">{item.bookAuthor}</p>
            )}

            {data.kind === "ProgressPosted" && <ProgressBody data={data} />}

            {(data.kind === "BookFinished" || data.kind === "ReviewCreated") &&
              data.rating != null && (
                <div className="mb-1">
                  <StarRating rating={data.rating} showValue={false} size={16} />
                </div>
              )}

            {data.kind === "BookRated" && data.rating != null && (
              <StarRating rating={data.rating} showValue={false} size={16} />
            )}

            {data.kind === "ListCreated" && data.description && (
              <p className="text-sm text-stone-600 dark:text-stone-300">{data.description}</p>
            )}

            {"content" in data && data.content && (
              <p className="text-sm text-stone-600 dark:text-stone-300 leading-relaxed mt-1">"{data.content}"</p>
            )}
          </div>
        </div>

        {/* Interaction bar — only for interactable items (Post/List). */}
        {isInteractable(item) && <InteractionBar item={item} listKey={listKey} />}
      </div>
    </Card>
  );
}

function ProgressBody({
  data,
}: {
  data: Extract<ReturnType<typeof parseActivityData>, { kind: "ProgressPosted" }>;
}) {
  const { t } = useTranslation();
  const percent = feedProgressPercent(data);

  if (percent != null) {
    return (
      <div className="mb-1">
        <div className="flex justify-between text-sm mb-1">
          <span className="text-stone-600 dark:text-stone-300">{t("feed.progress")}</span>
          <span className="font-medium text-stone-800 dark:text-stone-100">{percent}%</span>
        </div>
        <ProgressBar value={percent} />
      </div>
    );
  }

  // Page progress: no pageCount in the feed payload, degrade to "page N".
  if (data.progressType === "Page" && data.progress != null) {
    return (
      <p className="text-sm text-stone-600 dark:text-stone-300 mb-1">{t("feed.pageN", { page: data.progress })}</p>
    );
  }

  return null;
}
