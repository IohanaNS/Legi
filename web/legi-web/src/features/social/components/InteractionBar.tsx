import { useState } from "react";
import { useTranslation } from "react-i18next";
import type { QueryKey } from "@tanstack/react-query";
import { Heart, MessageCircle } from "lucide-react";
import { cn } from "../../../lib/utils";
import { CommentThread } from "./CommentThread";
import { useToggleLike } from "../hooks/useToggleLike";
import { interactionResource } from "../lib/feed";
import type { FeedItemDto } from "../types";

interface InteractionBarProps {
  item: FeedItemDto;
  listKey: QueryKey;
}

export function InteractionBar({ item, listKey }: InteractionBarProps) {
  const { t } = useTranslation();
  const [showComments, setShowComments] = useState(false);
  const toggleLike = useToggleLike(listKey);
  const resource = interactionResource(item.targetType);

  return (
    <>
      <div className="flex items-center gap-4 mt-3 pt-3 border-t border-stone-100">
        <button
          type="button"
          onClick={() => toggleLike.mutate(item)}
          aria-pressed={item.isLikedByMe}
          aria-label={t("feed.like")}
          className={cn(
            "flex items-center gap-1.5 text-sm transition-colors",
            item.isLikedByMe ? "text-red-500" : "text-stone-500 hover:text-red-500",
          )}
        >
          <Heart size={16} className={item.isLikedByMe ? "fill-red-500" : ""} />
          {item.likesCount}
        </button>

        <button
          type="button"
          onClick={() => setShowComments((v) => !v)}
          aria-expanded={showComments}
          aria-label={t("feed.comment")}
          className="flex items-center gap-1.5 text-sm text-stone-500 hover:text-stone-700 transition-colors"
        >
          <MessageCircle size={16} />
          {item.commentsCount}
        </button>
      </div>

      {showComments && resource && (
        <CommentThread resource={resource} id={item.referenceId} listKey={listKey} />
      )}
    </>
  );
}
