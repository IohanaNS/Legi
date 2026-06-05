import { useTranslation } from "react-i18next";
import { UserSearch } from "lucide-react";
import { Button } from "../../../components/ui/Button";
import { Card } from "../../../components/ui/Card";
import { ReadingNowCard } from "./ReadingNowCard";
import { FeedCard } from "./FeedCard";
import { FindPeople } from "./FindPeople";
import { useFeed } from "../hooks/useFeed";
import { feedKeys } from "../queryKeys";
import { useAuth } from "../../auth/useAuth";

export default function FeedPage() {
  const { t } = useTranslation();
  const { user } = useAuth();
  const feed = useFeed();
  const listKey = feedKeys.list();

  const items = feed.data?.pages.flatMap((p) => p.items) ?? [];

  return (
    <div className="flex gap-6">
      {/* Main column */}
      <div className="flex-1 space-y-4">
        <div>
          <h1 className="text-2xl font-bold text-stone-800">
            {t("feed.greeting", { username: user?.username ?? "" })}
          </h1>
          <p className="mt-1 text-stone-500">{t("feed.subtitle")}</p>
        </div>

        <ReadingNowCard />

        {feed.isLoading ? (
          <FeedSkeleton />
        ) : feed.isError ? (
          <ErrorState label={t("common.couldNotLoad")} onRetry={() => feed.refetch()} />
        ) : items.length === 0 ? (
          <FeedEmptyState />
        ) : (
          <>
            {items.map((item) => (
              <FeedCard key={item.id} item={item} listKey={listKey} />
            ))}

            {feed.hasNextPage && (
              <div className="flex justify-center">
                <Button
                  variant="outline"
                  onClick={() => feed.fetchNextPage()}
                  disabled={feed.isFetchingNextPage}
                >
                  {t("common.loadMore")}
                </Button>
              </div>
            )}
          </>
        )}
      </div>

      {/* Right sidebar */}
      <aside className="hidden w-72 flex-shrink-0 lg:block">
        <FindPeople />
      </aside>
    </div>
  );
}

function FeedEmptyState() {
  const { t } = useTranslation();
  return (
    <Card>
      <div className="flex flex-col items-center gap-2 px-4 py-12 text-center">
        <UserSearch size={28} className="text-stone-300" />
        <p className="text-sm font-medium text-stone-600">{t("feed.empty")}</p>
        <p className="text-xs text-stone-400">{t("feed.emptyHint")}</p>
      </div>
    </Card>
  );
}

function FeedSkeleton() {
  return (
    <div className="space-y-4">
      {Array.from({ length: 3 }).map((_, i) => (
        <Card key={i}>
          <div className="animate-pulse p-4">
            <div className="mb-3 flex items-center gap-3">
              <div className="h-10 w-10 rounded-full bg-stone-200" />
              <div className="space-y-2">
                <div className="h-3 w-40 rounded bg-stone-200" />
                <div className="h-2 w-20 rounded bg-stone-200" />
              </div>
            </div>
            <div className="flex gap-3">
              <div className="h-24 w-16 rounded-lg bg-stone-200" />
              <div className="flex-1 space-y-2">
                <div className="h-3 w-3/4 rounded bg-stone-200" />
                <div className="h-3 w-1/2 rounded bg-stone-200" />
              </div>
            </div>
          </div>
        </Card>
      ))}
    </div>
  );
}

function ErrorState({ label, onRetry }: { label: string; onRetry: () => void }) {
  const { t } = useTranslation();
  return (
    <div className="py-10 text-center">
      <p className="mb-3 text-sm text-stone-500">{label}</p>
      <Button variant="outline" size="sm" onClick={onRetry}>
        {t("common.retry")}
      </Button>
    </div>
  );
}
