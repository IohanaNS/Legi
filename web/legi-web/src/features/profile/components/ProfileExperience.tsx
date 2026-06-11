import { useState } from "react";
import { useTranslation } from "react-i18next";
import { useSearchParams } from "react-router-dom";
import { Button } from "../../../components/ui/Button";
import { BookGridItem } from "../../library/components/BookGridItem";
import { ListCard } from "../../library/components/ListCard";
import { ProfileHeader } from "../../library/components/ProfileHeader";
import { ProfileStats } from "../../library/components/ProfileStats";
import { ProfileTabs } from "../../library/components/ProfileTabs";
import { tabToStatus } from "../../library/lib/mappers";
import type { ProfileTab, ViewMode } from "../../library/types";
import { FeedCard } from "../../social/components/FeedCard";
import { FollowButton } from "../../social/components/FollowButton";
import { useUserActivity } from "../../social/hooks/useFeed";
import { useUserProfile } from "../../social/hooks/useUserProfile";
import { feedKeys } from "../../social/queryKeys";
import { useProfilePermissions } from "../hooks/useProfilePermissions";
import { useUserLibraryStats } from "../hooks/useUserLibraryStats";
import { useUserProfileBooks } from "../hooks/useUserProfileBooks";
import { useUserProfileLists } from "../hooks/useUserProfileLists";

interface ProfileExperienceProps {
  targetUserId: string | undefined;
}

const PROFILE_TABS: ReadonlySet<ProfileTab> = new Set([
  "activity",
  "reading",
  "finished",
  "paused",
  "abandoned",
  "not_started",
  "lists",
]);

export function ProfileExperience({ targetUserId }: ProfileExperienceProps) {
  const { t } = useTranslation();
  const [searchParams, setSearchParams] = useSearchParams();
  const [viewMode, setViewMode] = useState<ViewMode>("grid");

  // Drive the active tab from the URL (?tab=) so links — e.g. "back to lists"
  // from a list detail page — can land directly on the right tab.
  const tabParam = searchParams.get("tab") as ProfileTab | null;
  const activeTab: ProfileTab = tabParam && PROFILE_TABS.has(tabParam) ? tabParam : "reading";
  const setActiveTab = (tab: ProfileTab) => {
    const next = new URLSearchParams(searchParams);
    next.set("tab", tab);
    setSearchParams(next, { replace: true });
  };

  const permissions = useProfilePermissions(targetUserId);
  const profileQuery = useUserProfile(targetUserId);
  const statsQuery = useUserLibraryStats(targetUserId);
  const activity = useUserActivity(targetUserId);

  // useUserProfileBooks must run unconditionally; gate the fetch to status tabs.
  const isBooksTab = activeTab !== "lists" && activeTab !== "activity";
  const booksStatus = isBooksTab ? tabToStatus(activeTab) : "Reading";
  const booksQuery = useUserProfileBooks(targetUserId, booksStatus, { enabled: isBooksTab });
  const listsQuery = useUserProfileLists(targetUserId, { enabled: activeTab === "lists" });

  const stats = statsQuery.data;
  const tabs = [
    {
      key: "activity" as const,
      labelKey: "profile.tabs.activity",
      count: activity.data?.pages[0]?.totalItems ?? 0,
    },
    { key: "reading" as const, labelKey: "profile.tabs.reading", count: stats?.reading ?? 0 },
    { key: "finished" as const, labelKey: "profile.tabs.finished", count: stats?.finished ?? 0 },
    { key: "paused" as const, labelKey: "profile.tabs.paused", count: stats?.paused ?? 0 },
    { key: "abandoned" as const, labelKey: "profile.tabs.abandoned", count: stats?.abandoned ?? 0 },
    {
      key: "not_started" as const,
      labelKey: "profile.tabs.not_started",
      count: stats?.notStarted ?? 0,
    },
    { key: "lists" as const, labelKey: "profile.tabs.lists", count: stats?.lists ?? 0 },
  ];

  const books = booksQuery.data?.pages.flatMap((p) => p.items) ?? [];
  const lists = listsQuery.data?.pages.flatMap((p) => p.items) ?? [];
  const activityItems = activity.data?.pages.flatMap((p) => p.items) ?? [];
  const activityKey = feedKeys.activity(targetUserId ?? "");

  if (!targetUserId || profileQuery.isLoading) return <HeaderSkeleton />;

  if (profileQuery.isError || !profileQuery.data) {
    return (
      <ErrorState
        label={t("profile.errorLoading")}
        onRetry={() => {
          void profileQuery.refetch();
        }}
      />
    );
  }

  const profile = profileQuery.data;

  return (
    <div>
      <ProfileHeader
        profile={profile}
        action={
          permissions.canFollow ? (
            <FollowButton userId={profile.userId} isFollowing={profile.isFollowing} />
          ) : undefined
        }
      />
      <ProfileStats
        userId={profile.userId}
        booksRead={stats?.finished ?? 0}
        followers={profile.followersCount}
        following={profile.followingCount}
      />

      <ProfileTabs
        tabs={tabs}
        activeTab={activeTab}
        onTabChange={setActiveTab}
        viewMode={viewMode}
        onViewModeChange={setViewMode}
      />

      <div className="mt-4">
        {activeTab === "activity" ? (
          activity.isLoading ? (
            <ContentSkeleton />
          ) : activity.isError ? (
            <ErrorState
              label={t("profile.errorLoading")}
              onRetry={() => {
                void activity.refetch();
              }}
            />
          ) : activityItems.length === 0 ? (
            <EmptyState label={t("feed.activityEmpty")} />
          ) : (
            <div className="space-y-4">
              {activityItems.map((item) => (
                <FeedCard key={item.id} item={item} listKey={activityKey} />
              ))}
              {activity.hasNextPage && (
                <LoadMoreButton
                  onClick={() => activity.fetchNextPage()}
                  disabled={activity.isFetchingNextPage}
                />
              )}
            </div>
          )
        ) : activeTab === "lists" ? (
          listsQuery.isLoading ? (
            <ContentSkeleton />
          ) : listsQuery.isError ? (
            <ErrorState
              label={t("profile.errorLoading")}
              onRetry={() => {
                void listsQuery.refetch();
              }}
            />
          ) : lists.length === 0 ? (
            <EmptyState label={t("profile.emptyTab")} />
          ) : (
            <>
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
                {lists.map((list) => (
                  <ListCard key={list.listId} list={list} />
                ))}
              </div>
              {listsQuery.hasNextPage && (
                <LoadMoreButton
                  onClick={() => listsQuery.fetchNextPage()}
                  disabled={listsQuery.isFetchingNextPage}
                />
              )}
            </>
          )
        ) : booksQuery.isLoading ? (
          <ContentSkeleton />
        ) : booksQuery.isError ? (
          <ErrorState
            label={t("profile.errorLoading")}
            onRetry={() => {
              void booksQuery.refetch();
            }}
          />
        ) : books.length === 0 ? (
          <EmptyState label={t("profile.emptyTab")} />
        ) : (
          <>
            <div
              className={
                viewMode === "grid"
                  ? "grid grid-cols-[repeat(auto-fill,minmax(150px,1fr))] gap-4"
                  : "space-y-3"
              }
            >
              {books.map((ub) => (
                <BookGridItem
                  key={ub.userBookId}
                  userBook={ub}
                  editable={permissions.canEditLibrary}
                />
              ))}
            </div>
            {booksQuery.hasNextPage && (
              <LoadMoreButton
                onClick={() => booksQuery.fetchNextPage()}
                disabled={booksQuery.isFetchingNextPage}
              />
            )}
          </>
        )}
      </div>
    </div>
  );
}

function HeaderSkeleton() {
  return (
    <div className="animate-pulse">
      <div className="h-40 bg-stone-200 rounded-xl dark:bg-dark-raised" />
      <div className="px-4">
        <div className="-mt-12 w-24 h-24 rounded-full bg-stone-300 ring-4 ring-white dark:bg-dark-raised dark:ring-dark-bg" />
        <div className="mt-3 h-5 w-40 bg-stone-200 rounded dark:bg-dark-raised" />
        <div className="mt-3 h-4 w-64 bg-stone-200 rounded dark:bg-dark-raised" />
        <div className="flex gap-6 py-4 mt-2">
          <div className="h-8 w-12 bg-stone-200 rounded dark:bg-dark-raised" />
          <div className="h-8 w-12 bg-stone-200 rounded dark:bg-dark-raised" />
          <div className="h-8 w-12 bg-stone-200 rounded dark:bg-dark-raised" />
        </div>
      </div>
    </div>
  );
}

function ContentSkeleton() {
  return (
    <div className="grid grid-cols-[repeat(auto-fill,minmax(150px,1fr))] gap-4 animate-pulse">
      {Array.from({ length: 8 }).map((_, i) => (
        <div key={i}>
          <div className="aspect-[2/3] bg-stone-200 rounded-lg mb-2 dark:bg-dark-raised" />
          <div className="h-4 w-3/4 bg-stone-200 rounded dark:bg-dark-raised" />
          <div className="h-3 w-1/2 bg-stone-200 rounded mt-1 dark:bg-dark-raised" />
        </div>
      ))}
    </div>
  );
}

function EmptyState({ label }: { label: string }) {
  return <p className="text-sm text-stone-400 text-center py-10">{label}</p>;
}

function ErrorState({ label, onRetry }: { label: string; onRetry: () => void }) {
  const { t } = useTranslation();
  return (
    <div className="text-center py-10">
      <p className="text-sm text-stone-500 mb-3">{label}</p>
      <Button variant="outline" size="sm" onClick={onRetry}>
        {t("common.retry")}
      </Button>
    </div>
  );
}

function LoadMoreButton({
  onClick,
  disabled,
}: {
  onClick: () => void;
  disabled: boolean;
}) {
  const { t } = useTranslation();
  return (
    <div className="flex justify-center mt-4">
      <Button variant="outline" onClick={onClick} disabled={disabled}>
        {t("profile.loadMore")}
      </Button>
    </div>
  );
}
