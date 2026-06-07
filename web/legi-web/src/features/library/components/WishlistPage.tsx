import { useTranslation } from "react-i18next";
import { Button } from "../../../components/ui/Button";
import { BookGridItem } from "./BookGridItem";
import { useWishlist } from "../hooks/useWishlist";

export default function WishlistPage() {
  const { t } = useTranslation();
  const wishlistQuery = useWishlist();
  const books = wishlistQuery.data?.pages.flatMap((p) => p.items) ?? [];
  const totalCount = wishlistQuery.data?.pages[0]?.totalCount ?? 0;

  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-2xl font-bold text-stone-800 dark:text-stone-100">{t("wishlist.title")}</h1>
        <p className="mt-1 text-sm text-stone-500 dark:text-stone-400">
          {t("wishlist.count", { count: totalCount })}
        </p>
      </header>

      {wishlistQuery.isLoading ? (
        <BookGridSkeleton />
      ) : wishlistQuery.isError ? (
        <ErrorState label={t("common.couldNotLoad")} onRetry={() => wishlistQuery.refetch()} />
      ) : books.length === 0 ? (
        <EmptyState label={t("wishlist.empty")} />
      ) : (
        <>
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5">
            {books.map((userBook) => (
              <BookGridItem key={userBook.userBookId} userBook={userBook} editable />
            ))}
          </div>

          {wishlistQuery.hasNextPage && (
            <div className="flex justify-center">
              <Button
                variant="outline"
                onClick={() => wishlistQuery.fetchNextPage()}
                disabled={wishlistQuery.isFetchingNextPage}
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

function BookGridSkeleton() {
  return (
    <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5">
      {Array.from({ length: 10 }).map((_, index) => (
        <div key={index} className="animate-pulse">
          <div className="mb-2 aspect-[2/3] rounded-lg bg-stone-200 dark:bg-dark-raised" />
          <div className="h-4 w-3/4 rounded bg-stone-200 dark:bg-dark-raised" />
          <div className="mt-1 h-3 w-1/2 rounded bg-stone-200 dark:bg-dark-raised" />
        </div>
      ))}
    </div>
  );
}

function EmptyState({ label }: { label: string }) {
  return <p className="py-10 text-center text-sm text-stone-400">{label}</p>;
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
