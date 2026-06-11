import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import { Button } from "../../../components/ui/Button";
import { useFollowedLists } from "../hooks/useFollowedLists";
import { FollowedListCard } from "./FollowedListCard";

interface FollowedListsSectionProps {
  /** Whose followed lists to show. */
  userId: string | undefined;
  /**
   * When this matches the viewer, an unfollow button is shown on each card.
   * Pass the current user's id when viewing their own followed lists; leave
   * undefined on other people's profiles (read-only). Also drives whether the
   * heading reads in the first person ("Lists you follow").
   */
  unfollowAsUserId?: string;
  /**
   * The collection owner's username, used for the third-person heading when this
   * isn't the viewer's own collection ("Lists {username} follows").
   */
  ownerUsername?: string;
  /** Optional client-side filter applied to the section (mirrors the page search). */
  searchTerm?: string;
}

/**
 * "Lists you follow" block: a divider + heading + grid of followed-list cards.
 * Renders nothing until there is at least one followed list, so it stays out of
 * the way for users who follow none. Used on both the Lists page and profiles.
 */
export function FollowedListsSection({
  userId,
  unfollowAsUserId,
  ownerUsername,
  searchTerm,
}: FollowedListsSectionProps) {
  const { t } = useTranslation();
  const isOwnCollection = !!unfollowAsUserId;
  const { lists, isLoading, hasNextPage, fetchNextPage, isFetchingNextPage } =
    useFollowedLists(userId);

  const normalized = searchTerm?.trim().toLowerCase() ?? "";
  const visible = useMemo(() => {
    if (!normalized) return lists;
    return lists.filter(
      (l) =>
        l.name.toLowerCase().includes(normalized) ||
        (l.description?.toLowerCase().includes(normalized) ?? false),
    );
  }, [lists, normalized]);

  // Additive section: stay invisible while loading or when there is nothing to show.
  if (isLoading || visible.length === 0) return null;

  return (
    <section className="space-y-4">
      <div className="border-t border-stone-200 pt-6 dark:border-dark-raised">
        <h2 className="font-serif text-lg font-semibold text-stone-800 dark:text-stone-100">
          {isOwnCollection
            ? t("lists.followingTitle")
            : t("lists.followingTitleOther", { username: ownerUsername })}
        </h2>
        <p className="mt-0.5 text-sm text-stone-500 dark:text-stone-400">
          {isOwnCollection
            ? t("lists.followingSubtitle")
            : t("lists.followingSubtitleOther", { username: ownerUsername })}
        </p>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {visible.map((list) => (
          <FollowedListCard key={list.listId} list={list} collectionUserId={unfollowAsUserId} />
        ))}
      </div>

      {hasNextPage && !normalized && (
        <div className="flex justify-center">
          <Button
            variant="outline"
            size="sm"
            onClick={() => fetchNextPage()}
            disabled={isFetchingNextPage}
          >
            {t("common.loadMore")}
          </Button>
        </div>
      )}
    </section>
  );
}
