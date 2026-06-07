import { useState } from "react";
import { useTranslation } from "react-i18next";
import { useAuth } from "../../auth/useAuth";
import { useUserProfile } from "../../social/hooks/useUserProfile";
import { useUserActivity } from "../../social/hooks/useFeed";
import { feedKeys } from "../../social/queryKeys";
import { FeedCard } from "../../social/components/FeedCard";
import { useLibraryCounts } from "../hooks/useLibraryCounts";
import { useLibraryBooks } from "../hooks/useLibraryBooks";
import { useLists } from "../hooks/useLists";
import { ProfileHeader } from "./ProfileHeader";
import { ProfileStats } from "./ProfileStats";
import { ProfileTabs } from "./ProfileTabs";
import { BookGridItem } from "./BookGridItem";
import { ListCard } from "./ListCard";
import { Button } from "../../../components/ui/Button";
import { tabToStatus } from "../lib/mappers";
import type { ProfileTab, ViewMode } from "../types";

export default function ProfilePage() {
  const { t } = useTranslation();
  const { user } = useAuth();
  const [activeTab, setActiveTab] = useState<ProfileTab>("reading");
  const [viewMode, setViewMode] = useState<ViewMode>("grid");

  const profileQuery = useUserProfile(user?.userId);
  const { counts } = useLibraryCounts();
  const listsQuery = useLists();
  const activity = useUserActivity(user?.userId);

  // useLibraryBooks must run unconditionally; gate the fetch to the status tabs only.
  const isBooksTab = activeTab !== "lists" && activeTab !== "activity";
  const booksStatus = isBooksTab ? tabToStatus(activeTab) : "Reading";
  const booksQuery = useLibraryBooks(booksStatus, isBooksTab);

  const tabs = [
    {
      key: "activity" as const,
      labelKey: "profile.tabs.activity",
      count: activity.data?.pages[0]?.totalItems ?? 0,
    },
    { key: "reading" as const, labelKey: "profile.tabs.reading", count: counts.Reading ?? 0 },
    { key: "finished" as const, labelKey: "profile.tabs.finished", count: counts.Finished ?? 0 },
    { key: "paused" as const, labelKey: "profile.tabs.paused", count: counts.Paused ?? 0 },
    { key: "abandoned" as const, labelKey: "profile.tabs.abandoned", count: counts.Abandoned ?? 0 },
    { key: "lists" as const, labelKey: "profile.tabs.lists", count: listsQuery.data?.length ?? 0 },
  ];

  const books = booksQuery.data?.pages.flatMap((p) => p.items) ?? [];
  const activityItems = activity.data?.pages.flatMap((p) => p.items) ?? [];
  const activityKey = feedKeys.activity(user?.userId ?? "");

  return (
    <div>
      {/* Header (Social) + Stats (Social follows + Library Finished count) */}
      {profileQuery.isLoading ? (
        <HeaderSkeleton />
      ) : profileQuery.isError ? (
        <ErrorState label={t("profile.errorLoading")} onRetry={() => profileQuery.refetch()} />
      ) : profileQuery.data ? (
        <>
          <ProfileHeader profile={profileQuery.data} />
          <ProfileStats
            booksRead={counts.Finished ?? 0}
            followers={profileQuery.data.followersCount}
            following={profileQuery.data.followingCount}
          />
        </>
      ) : null}

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
            <ErrorState label={t("profile.errorLoading")} onRetry={() => activity.refetch()} />
          ) : activityItems.length === 0 ? (
            <EmptyState label={t("feed.activityEmpty")} />
          ) : (
            <div className="space-y-4">
              {activityItems.map((item) => (
                <FeedCard key={item.id} item={item} listKey={activityKey} />
              ))}
              {activity.hasNextPage && (
                <div className="flex justify-center mt-4">
                  <Button
                    variant="outline"
                    onClick={() => activity.fetchNextPage()}
                    disabled={activity.isFetchingNextPage}
                  >
                    {t("profile.loadMore")}
                  </Button>
                </div>
              )}
            </div>
          )
        ) : activeTab === "lists" ? (
          listsQuery.isLoading ? (
            <ContentSkeleton />
          ) : listsQuery.isError ? (
            <ErrorState label={t("profile.errorLoading")} onRetry={() => listsQuery.refetch()} />
          ) : (listsQuery.data?.length ?? 0) === 0 ? (
            <EmptyState label={t("profile.emptyTab")} />
          ) : (
            <div className="grid grid-cols-2 gap-4">
              {listsQuery.data!.map((list) => (
                <ListCard key={list.listId} list={list} />
              ))}
            </div>
          )
        ) : booksQuery.isLoading ? (
          <ContentSkeleton />
        ) : booksQuery.isError ? (
          <ErrorState label={t("profile.errorLoading")} onRetry={() => booksQuery.refetch()} />
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
                <BookGridItem key={ub.userBookId} userBook={ub} editable />
              ))}
            </div>
            {booksQuery.hasNextPage && (
              <div className="flex justify-center mt-4">
                <Button
                  variant="outline"
                  onClick={() => booksQuery.fetchNextPage()}
                  disabled={booksQuery.isFetchingNextPage}
                >
                  {t("profile.loadMore")}
                </Button>
              </div>
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
      <div className="h-40 bg-stone-200 rounded-xl" />
      <div className="px-4">
        <div className="-mt-12 w-24 h-24 rounded-full bg-stone-300 ring-4 ring-white" />
        <div className="mt-3 h-5 w-40 bg-stone-200 rounded" />
        <div className="mt-3 h-4 w-64 bg-stone-200 rounded" />
        <div className="flex gap-6 py-4 mt-2">
          <div className="h-8 w-12 bg-stone-200 rounded" />
          <div className="h-8 w-12 bg-stone-200 rounded" />
          <div className="h-8 w-12 bg-stone-200 rounded" />
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
          <div className="aspect-[2/3] bg-stone-200 rounded-lg mb-2" />
          <div className="h-4 w-3/4 bg-stone-200 rounded" />
          <div className="h-3 w-1/2 bg-stone-200 rounded mt-1" />
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
