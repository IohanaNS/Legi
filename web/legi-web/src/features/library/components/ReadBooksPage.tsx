import { useTranslation } from "react-i18next";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft } from "lucide-react";
import { Button } from "../../../components/ui/Button";
import { useAuth } from "../../auth/useAuth";
import { useUserLibrary } from "../hooks/useLibraryBooks";
import { BookGridItem } from "./BookGridItem";

/**
 * Full page of a user's read (Finished) books, reached from the profile "Read"
 * counter (/users/:userId/read). Books are editable only when viewing your own.
 */
export default function ReadBooksPage() {
  const { t } = useTranslation();
  const { userId } = useParams<{ userId: string }>();
  const { user } = useAuth();
  const isSelf = !!userId && user?.userId === userId;

  const query = useUserLibrary(userId, "Finished");
  const books = query.data?.pages.flatMap((p) => p.items) ?? [];

  return (
    <div className="mx-auto max-w-4xl">
      <Link
        to={isSelf ? "/profile" : `/users/${userId}`}
        className="mb-6 inline-flex items-center gap-2 text-sm text-stone-500 dark:text-stone-400 hover:text-stone-700 dark:hover:text-stone-200"
      >
        <ArrowLeft size={16} />
        {t("bookDetails.back")}
      </Link>

      <h1 className="mb-4 font-serif text-xl font-semibold text-stone-800 dark:text-stone-100">
        {t("profile.readBooksTitle")}
      </h1>

      {query.isLoading ? (
        <ContentSkeleton />
      ) : query.isError ? (
        <div className="py-10 text-center">
          <p className="mb-3 text-sm text-stone-500">{t("profile.errorLoading")}</p>
          <Button variant="outline" size="sm" onClick={() => query.refetch()}>
            {t("common.retry")}
          </Button>
        </div>
      ) : books.length === 0 ? (
        <p className="py-10 text-center text-sm text-stone-400">{t("profile.emptyTab")}</p>
      ) : (
        <>
          <div className="grid grid-cols-[repeat(auto-fill,minmax(150px,1fr))] gap-4">
            {books.map((ub) => (
              <BookGridItem key={ub.userBookId} userBook={ub} editable={isSelf} />
            ))}
          </div>
          {query.hasNextPage && (
            <div className="mt-4 flex justify-center">
              <Button
                variant="outline"
                onClick={() => query.fetchNextPage()}
                disabled={query.isFetchingNextPage}
              >
                {t("profile.loadMore")}
              </Button>
            </div>
          )}
        </>
      )}
    </div>
  );
}

function ContentSkeleton() {
  return (
    <div className="grid grid-cols-[repeat(auto-fill,minmax(150px,1fr))] gap-4 animate-pulse">
      {Array.from({ length: 8 }).map((_, i) => (
        <div key={i}>
          <div className="aspect-[2/3] rounded-lg bg-stone-200 dark:bg-dark-raised mb-2" />
          <div className="h-4 w-3/4 rounded bg-stone-200 dark:bg-dark-raised" />
          <div className="mt-1 h-3 w-1/2 rounded bg-stone-200 dark:bg-dark-raised" />
        </div>
      ))}
    </div>
  );
}
