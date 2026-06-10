import { useTranslation } from "react-i18next";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft } from "lucide-react";
import { Button } from "../../../components/ui/Button";
import { UserListRow } from "./UserListRow";
import { useFollowers, useFollowing } from "../hooks/useFollowList";

interface FollowListPageProps {
  mode: "followers" | "following";
}

/**
 * Full page listing a user's followers or following. Reached from the profile
 * counters (/users/:userId/followers|following). Replaces the old modal so the
 * list is linkable and back-button friendly.
 */
export default function FollowListPage({ mode }: FollowListPageProps) {
  const { t } = useTranslation();
  const { userId } = useParams<{ userId: string }>();

  const followers = useFollowers(userId, mode === "followers");
  const following = useFollowing(userId, mode === "following");
  const query = mode === "followers" ? followers : following;

  const users = query.data?.pages.flatMap((p) => p.items) ?? [];

  return (
    <div className="mx-auto max-w-2xl">
      <Link
        to={`/users/${userId}`}
        className="mb-6 inline-flex items-center gap-2 text-sm text-stone-500 dark:text-stone-400 hover:text-stone-700 dark:hover:text-stone-200"
      >
        <ArrowLeft size={16} />
        {t("bookDetails.back")}
      </Link>

      <h1 className="mb-4 font-serif text-xl font-semibold text-stone-800 dark:text-stone-100">
        {t(mode === "followers" ? "feed.followersTitle" : "feed.followingTitle")}
      </h1>

      {query.isLoading ? (
        <p className="py-6 text-center text-sm text-stone-400">{t("common.couldNotLoad")}</p>
      ) : query.isError ? (
        <div className="py-6 text-center">
          <p className="mb-2 text-sm text-stone-500">{t("common.couldNotLoad")}</p>
          <Button variant="outline" size="sm" onClick={() => query.refetch()}>
            {t("common.retry")}
          </Button>
        </div>
      ) : users.length === 0 ? (
        <p className="py-6 text-center text-sm text-stone-400">{t("feed.followEmpty")}</p>
      ) : (
        <>
          <ul className="space-y-3">
            {users.map((u) => (
              <UserListRow key={u.userId} user={u} />
            ))}
          </ul>
          {query.hasNextPage && (
            <div className="mt-4 flex justify-center">
              <Button
                variant="outline"
                size="sm"
                onClick={() => query.fetchNextPage()}
                disabled={query.isFetchingNextPage}
              >
                {t("common.loadMore")}
              </Button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
