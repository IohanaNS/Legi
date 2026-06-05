import { useState } from "react";
import { useTranslation } from "react-i18next";
import { useParams } from "react-router-dom";
import { Avatar } from "../../../components/ui/Avatar";
import { Button } from "../../../components/ui/Button";
import { FeedCard } from "./FeedCard";
import { FollowButton } from "./FollowButton";
import { FollowListModal } from "./FollowListModal";
import { useUserProfile } from "../hooks/useUserProfile";
import { useUserActivity } from "../hooks/useFeed";
import { feedKeys } from "../queryKeys";
import { useAuth } from "../../auth/useAuth";

export default function UserProfilePage() {
  const { t } = useTranslation();
  const { userId } = useParams<{ userId: string }>();
  const { user: currentUser } = useAuth();

  const profileQuery = useUserProfile(userId);
  const activity = useUserActivity(userId);
  const [followModal, setFollowModal] = useState<"followers" | "following" | null>(null);

  const isSelf = !!userId && currentUser?.userId === userId;
  const items = activity.data?.pages.flatMap((p) => p.items) ?? [];
  const listKey = feedKeys.activity(userId ?? "");

  if (profileQuery.isLoading) return <ProfileSkeleton />;

  if (profileQuery.isError || !profileQuery.data) {
    return (
      <div className="py-10 text-center">
        <p className="mb-3 text-sm text-stone-500">{t("common.couldNotLoad")}</p>
        <Button variant="outline" size="sm" onClick={() => profileQuery.refetch()}>
          {t("common.retry")}
        </Button>
      </div>
    );
  }

  const profile = profileQuery.data;

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      {/* Header */}
      <div>
        <div className="h-40 overflow-hidden rounded-xl bg-stone-300">
          {profile.bannerUrl && (
            <img src={profile.bannerUrl} alt="" className="h-full w-full object-cover" />
          )}
        </div>

        <div className="px-4">
          <div className="-mt-12 flex items-end justify-between">
            <Avatar
              src={profile.avatarUrl ?? undefined}
              fallback={profile.username}
              size="xl"
              className="ring-4 ring-white"
            />
            {!isSelf && (
              <div className="mb-1">
                <FollowButton userId={profile.userId} isFollowing={profile.isFollowing} />
              </div>
            )}
          </div>

          <h1 className="mt-3 text-xl font-bold text-stone-800">@{profile.username}</h1>
          {profile.bio && (
            <p className="mt-3 text-sm leading-relaxed text-stone-600">{profile.bio}</p>
          )}

          <div className="mt-3 flex gap-6 text-sm">
            <button
              type="button"
              onClick={() => setFollowModal("followers")}
              className="hover:text-green-700"
            >
              <span className="font-semibold text-stone-800">{profile.followersCount}</span>{" "}
              <span className="text-stone-500">{t("profile.stats.followers")}</span>
            </button>
            <button
              type="button"
              onClick={() => setFollowModal("following")}
              className="hover:text-green-700"
            >
              <span className="font-semibold text-stone-800">{profile.followingCount}</span>{" "}
              <span className="text-stone-500">{t("profile.stats.following")}</span>
            </button>
          </div>
        </div>
      </div>

      {/* Activity */}
      <div className="space-y-4">
        <h2 className="px-1 text-sm font-semibold text-stone-700">{t("feed.activityTitle")}</h2>

        {activity.isLoading ? (
          <p className="py-6 text-center text-sm text-stone-400">{t("common.couldNotLoad")}</p>
        ) : activity.isError ? (
          <div className="py-6 text-center">
            <p className="mb-2 text-sm text-stone-500">{t("common.couldNotLoad")}</p>
            <Button variant="outline" size="sm" onClick={() => activity.refetch()}>
              {t("common.retry")}
            </Button>
          </div>
        ) : items.length === 0 ? (
          <p className="py-6 text-center text-sm text-stone-400">{t("feed.activityEmpty")}</p>
        ) : (
          <>
            {items.map((item) => (
              <FeedCard key={item.id} item={item} listKey={listKey} />
            ))}
            {activity.hasNextPage && (
              <div className="flex justify-center">
                <Button
                  variant="outline"
                  onClick={() => activity.fetchNextPage()}
                  disabled={activity.isFetchingNextPage}
                >
                  {t("common.loadMore")}
                </Button>
              </div>
            )}
          </>
        )}
      </div>

      {followModal && userId && (
        <FollowListModal userId={userId} mode={followModal} onClose={() => setFollowModal(null)} />
      )}
    </div>
  );
}

function ProfileSkeleton() {
  return (
    <div className="mx-auto max-w-2xl">
      <div className="animate-pulse">
        <div className="h-40 rounded-xl bg-stone-200" />
        <div className="px-4">
          <div className="-mt-12 h-24 w-24 rounded-full bg-stone-300 ring-4 ring-white" />
          <div className="mt-4 h-5 w-40 rounded bg-stone-200" />
          <div className="mt-2 h-3 w-64 rounded bg-stone-200" />
        </div>
      </div>
    </div>
  );
}
