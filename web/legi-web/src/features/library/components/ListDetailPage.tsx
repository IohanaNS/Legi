import { useTranslation } from "react-i18next";
import { Link, useNavigate, useParams } from "react-router-dom";
import { ArrowLeft, Globe, Heart, Lock, Pencil, Trash2, UserPlus } from "lucide-react";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { BookCover } from "../../../components/ui/BookCover";
import { cn } from "../../../lib/utils";
import { CommentThread } from "../../social/components/CommentThread";
import { useListSocial } from "../../social/hooks/useListSocial";
import { interactionKeys } from "../../social/queryKeys";
import { useDeleteList, useListBooks, useListDetail } from "../hooks/useListMutations";
import type { BookSnapshotDto } from "../types";

export default function ListDetailPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { listId = "" } = useParams<{ listId: string }>();

  const detailQuery = useListDetail(listId);
  const booksQuery = useListBooks(listId);
  const deleteList = useDeleteList();
  const social = useListSocial(listId, Boolean(listId));

  if (detailQuery.isLoading) {
    return <p className="py-10 text-center text-sm text-stone-400">{t("common.loading")}</p>;
  }
  if (detailQuery.isError || !detailQuery.data) {
    return <p className="py-10 text-center text-sm text-stone-500">{t("common.couldNotLoad")}</p>;
  }

  const list = detailQuery.data;
  const books = booksQuery.data?.items ?? [];
  const state = social.query.data;
  // A public list is interactable: everyone (owner included) sees its likes,
  // followers and comments. Only the like/follow *actions* are owner-gated —
  // you can't follow or like your own list.
  const isPublic = Boolean(state?.isInteractable);
  const canInteract = isPublic && !list.isOwner;

  // Owners came from their own lists hub; for someone else's list, go back to
  // that owner's profile on the lists tab.
  const backTarget = list.isOwner ? "/lists" : `/users/${list.userId}?tab=lists`;

  const handleDelete = () => {
    if (window.confirm(t("lists.confirmDelete", { name: list.name }))) {
      deleteList.mutate(listId, { onSuccess: () => navigate("/lists") });
    }
  };

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <button
        type="button"
        onClick={() => navigate(backTarget)}
        className="flex items-center gap-1 text-sm text-stone-500 hover:text-stone-700 dark:hover:text-stone-300"
      >
        <ArrowLeft size={16} />
        {t("lists.backToLists")}
      </button>

      <header className="space-y-3">
        <div className="flex items-start justify-between gap-4">
          <div className="min-w-0">
            <div className="flex items-center gap-2">
              <h1 className="font-serif text-2xl font-semibold text-stone-800 dark:text-stone-100">{list.name}</h1>
              <Badge variant={list.isPublic ? "success" : "secondary"}>
                <span className="flex items-center gap-1">
                  {list.isPublic ? <Globe size={10} /> : <Lock size={10} />}
                  {list.isPublic ? t("lists.public") : t("lists.private")}
                </span>
              </Badge>
            </div>
            <p className="mt-1 text-sm text-stone-400">{t("lists.booksCount", { count: list.booksCount })}</p>
          </div>

          <div className="flex shrink-0 items-center gap-2">
            {list.isOwner ? (
              <>
                <Button variant="outline" size="sm" onClick={() => navigate(`/lists/${listId}/edit`)}>
                  <span className="flex items-center gap-1.5">
                    <Pencil size={14} />
                    {t("common.edit")}
                  </span>
                </Button>
                <Button variant="danger" size="sm" onClick={handleDelete}>
                  <span className="flex items-center gap-1.5">
                    <Trash2 size={14} />
                    {t("common.delete")}
                  </span>
                </Button>
              </>
            ) : (
              canInteract && (
                <Button
                  variant={state?.isFollowedByMe ? "outline" : "primary"}
                  size="sm"
                  onClick={() => social.toggleFollow.mutate(Boolean(state?.isFollowedByMe))}
                >
                  <span className="flex items-center gap-1.5">
                    <UserPlus size={14} />
                    {state?.isFollowedByMe ? t("lists.following") : t("lists.follow")}
                  </span>
                </Button>
              )
            )}
          </div>
        </div>

        {list.description && (
          <p className="text-sm text-stone-600 dark:text-stone-300">{list.description}</p>
        )}

        {isPublic && (
          <div className="flex items-center gap-4 border-t border-stone-100 pt-3 dark:border-dark-raised">
            <button
              type="button"
              disabled={!canInteract}
              onClick={() => social.toggleLike.mutate(Boolean(state?.isLikedByMe))}
              aria-pressed={state?.isLikedByMe}
              className={cn(
                "flex items-center gap-1.5 text-sm transition-colors",
                state?.isLikedByMe ? "text-red-500" : "text-stone-500",
                canInteract && "hover:text-red-500",
                !canInteract && "cursor-default",
              )}
            >
              <Heart size={16} className={state?.isLikedByMe ? "fill-red-500" : ""} />
              {state?.likesCount ?? 0}
            </button>
            <span className="text-sm text-stone-400">
              {t("lists.followersCount", { count: state?.followersCount ?? 0 })}
            </span>
          </div>
        )}
      </header>

      {/* Books */}
      {booksQuery.isLoading ? (
        <p className="py-6 text-center text-sm text-stone-400">{t("common.loading")}</p>
      ) : books.length === 0 ? (
        <p className="py-6 text-center text-sm text-stone-400">{t("lists.detailEmpty")}</p>
      ) : (
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 md:grid-cols-4">
          {books.map((item) => (
            <ListBookTile key={item.bookId} book={item.book} />
          ))}
        </div>
      )}

      {/* Comments (public lists only) */}
      {isPublic && (
        <CommentThread
          resource="lists"
          id={listId}
          listKey={interactionKeys.listSocial(listId)}
          canModerate={list.isOwner}
        />
      )}
    </div>
  );
}

function ListBookTile({ book }: { book: BookSnapshotDto }) {
  return (
    <Link to={`/books/${book.bookId}`} className="group block">
      <div className="aspect-[2/3] overflow-hidden rounded-lg">
        <BookCover
          title={book.title}
          author={book.authorDisplay}
          coverUrl={book.coverUrl}
          className="transition-transform group-hover:scale-105"
        />
      </div>
      <p className="mt-1.5 truncate text-sm font-medium text-stone-800 dark:text-stone-100">{book.title}</p>
      <p className="truncate text-xs text-stone-500">{book.authorDisplay}</p>
    </Link>
  );
}
