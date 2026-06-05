import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { X } from "lucide-react";
import { Avatar } from "../../../components/ui/Avatar";
import { Button } from "../../../components/ui/Button";
import { FollowButton } from "./FollowButton";
import { useFollowers, useFollowing } from "../hooks/useFollowList";
import { useAuth } from "../../auth/useAuth";
import type { FollowUserDto } from "../types";

interface FollowListModalProps {
  userId: string;
  mode: "followers" | "following";
  onClose: () => void;
}

export function FollowListModal({ userId, mode, onClose }: FollowListModalProps) {
  const { t } = useTranslation();
  const followersQuery = useFollowers(userId, mode === "followers");
  const followingQuery = useFollowing(userId, mode === "following");
  const query = mode === "followers" ? followersQuery : followingQuery;

  const users = query.data?.pages.flatMap((p) => p.items) ?? [];

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      onClick={onClose}
    >
      <div
        className="flex max-h-[80vh] w-full max-w-md flex-col rounded-xl bg-white shadow-lg"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between border-b border-stone-200 p-4">
          <h2 className="text-lg font-semibold text-stone-800">
            {t(mode === "followers" ? "feed.followersTitle" : "feed.followingTitle")}
          </h2>
          <button type="button" onClick={onClose} className="text-stone-400 hover:text-stone-600">
            <X size={18} />
          </button>
        </div>

        <div className="flex-1 overflow-y-auto p-4">
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
                  <FollowRow key={u.userId} user={u} onNavigate={onClose} />
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
      </div>
    </div>
  );
}

function FollowRow({ user, onNavigate }: { user: FollowUserDto; onNavigate: () => void }) {
  const { user: currentUser } = useAuth();
  const isSelf = currentUser?.userId === user.userId;

  return (
    <li className="flex items-center gap-3">
      <Link to={`/users/${user.userId}`} onClick={onNavigate}>
        <Avatar src={user.avatarUrl ?? undefined} fallback={user.username} size="md" />
      </Link>
      <div className="min-w-0 flex-1">
        <Link
          to={`/users/${user.userId}`}
          onClick={onNavigate}
          className="block truncate text-sm font-medium text-stone-800 hover:text-green-700"
        >
          @{user.username}
        </Link>
        {user.bio && <p className="truncate text-xs text-stone-500">{user.bio}</p>}
      </div>
      {!isSelf && (
        <FollowButton userId={user.userId} isFollowing={user.isFollowedByViewer} size="sm" />
      )}
    </li>
  );
}
