import { useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import type { QueryKey } from "@tanstack/react-query";
import { Avatar } from "../../../components/ui/Avatar";
import { Button } from "../../../components/ui/Button";
import { useComments, useAddComment } from "../hooks/useComments";
import { relativeTime } from "../lib/time";
import type { Resource } from "../api";
import type { CommentDto } from "../types";

interface CommentThreadProps {
  resource: Resource;
  id: string;
  listKey: QueryKey;
}

export function CommentThread({ resource, id, listKey }: CommentThreadProps) {
  const { t } = useTranslation();
  const query = useComments(resource, id);
  const addComment = useAddComment(resource, id, listKey);
  const [content, setContent] = useState("");

  const comments = query.data?.pages.flatMap((p) => p.items) ?? [];

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    const trimmed = content.trim();
    if (!trimmed || addComment.isPending) return;
    addComment.mutate(trimmed, { onSuccess: () => setContent("") });
  };

  return (
    <div className="mt-3 pt-3 border-t border-stone-100 dark:border-dark-raised space-y-3">
      {/* Add comment */}
      <form onSubmit={handleSubmit} className="flex items-start gap-2">
        <input
          value={content}
          onChange={(e) => setContent(e.target.value)}
          maxLength={500}
          placeholder={t("feed.addComment")}
          className="flex-1 rounded-lg border border-stone-300 dark:border-dark-raised bg-white dark:bg-dark-raised text-stone-800 dark:text-stone-100 px-3 py-1.5 text-sm focus:outline-none focus:ring-1 focus:ring-green-600"
        />
        <Button type="submit" size="sm" disabled={!content.trim() || addComment.isPending}>
          {t("feed.send")}
        </Button>
      </form>

      {/* List */}
      {query.isLoading ? (
        <p className="text-xs text-stone-400 dark:text-stone-500">{t("feed.loadingComments")}</p>
      ) : query.isError ? (
        <button
          type="button"
          onClick={() => query.refetch()}
          className="text-xs text-stone-500 dark:text-stone-400 hover:text-stone-700 dark:hover:text-stone-200"
        >
          {t("common.couldNotLoad")} · {t("common.retry")}
        </button>
      ) : comments.length === 0 ? (
        <p className="text-xs text-stone-400 dark:text-stone-500">{t("feed.noComments")}</p>
      ) : (
        <>
          <ul className="space-y-3">
            {comments.map((c) => (
              <CommentRow key={c.id} comment={c} />
            ))}
          </ul>
          {query.hasNextPage && (
            <button
              type="button"
              onClick={() => query.fetchNextPage()}
              disabled={query.isFetchingNextPage}
              className="text-xs text-green-700 hover:underline disabled:opacity-50"
            >
              {t("common.loadMore")}
            </button>
          )}
        </>
      )}
    </div>
  );
}

function CommentRow({ comment }: { comment: CommentDto }) {
  const { t } = useTranslation();
  return (
    <li className="flex items-start gap-2">
      <Link to={`/users/${comment.userId}`}>
        <Avatar src={comment.avatarUrl ?? undefined} fallback={comment.username} size="sm" />
      </Link>
      <div className="min-w-0">
        <p className="text-sm">
          <Link
            to={`/users/${comment.userId}`}
            className="font-semibold text-stone-800 dark:text-stone-100 hover:text-green-700 transition-colors"
          >
            @{comment.username}
          </Link>{" "}
          <span className="text-xs text-stone-400 dark:text-stone-500">{relativeTime(comment.createdAt, t)}</span>
        </p>
        <p className="text-sm text-stone-600 dark:text-stone-300 break-words">{comment.content}</p>
      </div>
    </li>
  );
}
