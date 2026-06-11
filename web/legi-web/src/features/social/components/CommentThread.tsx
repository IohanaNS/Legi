import { useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { Trash2 } from "lucide-react";
import type { QueryKey } from "@tanstack/react-query";
import { Avatar } from "../../../components/ui/Avatar";
import { Button } from "../../../components/ui/Button";
import { useAuth } from "../../auth/useAuth";
import { useComments, useAddComment, useDeleteComment } from "../hooks/useComments";
import { relativeTime } from "../lib/time";
import type { Resource } from "../api";
import type { CommentDto } from "../types";

interface CommentThreadProps {
  resource: Resource;
  id: string;
  listKey: QueryKey;
  /** When true, the viewer (content owner) may delete any comment, not just their own. */
  canModerate?: boolean;
}

export function CommentThread({ resource, id, listKey, canModerate = false }: CommentThreadProps) {
  const { t } = useTranslation();
  const { user } = useAuth();
  const query = useComments(resource, id);
  const addComment = useAddComment(resource, id, listKey);
  const deleteComment = useDeleteComment(resource, id, listKey);
  const [content, setContent] = useState("");

  const comments = query.data?.pages.flatMap((p) => p.items) ?? [];

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    const trimmed = content.trim();
    if (!trimmed || addComment.isPending) return;
    addComment.mutate(trimmed, { onSuccess: () => setContent("") });
  };

  const handleDelete = (commentId: string) => {
    if (window.confirm(t("feed.confirmDeleteComment"))) {
      deleteComment.mutate(commentId);
    }
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
              <CommentRow
                key={c.id}
                comment={c}
                canDelete={canModerate || c.userId === user?.userId}
                onDelete={() => handleDelete(c.id)}
              />
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

function CommentRow({
  comment,
  canDelete,
  onDelete,
}: {
  comment: CommentDto;
  canDelete: boolean;
  onDelete: () => void;
}) {
  const { t } = useTranslation();
  return (
    <li className="group flex items-start gap-2">
      <Link to={`/users/${comment.userId}`}>
        <Avatar src={comment.avatarUrl ?? undefined} fallback={comment.username} size="sm" />
      </Link>
      <div className="min-w-0 flex-1">
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
      {canDelete && (
        <button
          type="button"
          onClick={onDelete}
          aria-label={t("feed.deleteComment")}
          title={t("feed.deleteComment")}
          className="shrink-0 text-stone-300 opacity-0 transition-opacity hover:text-red-500 group-hover:opacity-100 dark:text-stone-600"
        >
          <Trash2 size={14} />
        </button>
      )}
    </li>
  );
}
