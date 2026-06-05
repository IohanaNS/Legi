import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { Button } from "../../../components/ui/Button";
import { BookSummaryCard } from "./BookSummaryCard";
import { SearchBar } from "./SearchBar";
import { TagFilter } from "./TagFilter";
import { usePopularTags } from "../hooks/usePopularTags";
import { useSearchBooks } from "../hooks/useSearchBooks";
import type { SortOption } from "../types";

export default function ExplorePage() {
  const { t } = useTranslation();

  const [searchInput, setSearchInput] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [selectedTagSlug, setSelectedTagSlug] = useState<string | undefined>();
  const [sort, setSort] = useState<SortOption>("mostPopular");

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setDebouncedSearch(searchInput.trim());
    }, 300);

    return () => window.clearTimeout(timeoutId);
  }, [searchInput]);

  const tagsQuery = usePopularTags();
  const booksQuery = useSearchBooks({
    searchTerm: debouncedSearch,
    tagSlug: selectedTagSlug,
    sort,
  });

  const books = booksQuery.data?.pages.flatMap((page) => page.books) ?? [];
  const totalCount = booksQuery.data?.pages[0]?.pagination.totalCount ?? 0;

  const handleToggleTag = (tagSlug: string) => {
    setSelectedTagSlug((current) => (current === tagSlug ? undefined : tagSlug));
  };

  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-2xl font-bold text-stone-800">{t("explore.title")}</h1>
        <p className="mt-1 text-stone-500">{t("explore.subtitle")}</p>
      </header>

      <SearchBar value={searchInput} onChange={setSearchInput} />

      <TagFilter
        tags={tagsQuery.data ?? []}
        selectedTagSlug={selectedTagSlug}
        isLoading={tagsQuery.isLoading}
        isError={tagsQuery.isError}
        onRetry={() => {
          void tagsQuery.refetch();
        }}
        onToggleTag={handleToggleTag}
      />

      <section>
        <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-sm text-stone-600">
            {t("explore.booksFound", { count: totalCount })}
          </p>

          <label className="flex items-center gap-2 text-sm text-stone-600">
            {t("explore.sortLabel")}
            <select
              value={sort}
              onChange={(event) => setSort(event.target.value as SortOption)}
              className="cursor-pointer rounded-lg border border-stone-200 bg-white px-3 py-1.5 text-sm text-stone-700 focus:border-green-600 focus:outline-none focus:ring-2 focus:ring-green-600/20"
            >
              <option value="bestRated">{t("explore.sortBy.bestRated")}</option>
              <option value="mostRecent">{t("explore.sortBy.mostRecent")}</option>
              <option value="mostPopular">{t("explore.sortBy.mostPopular")}</option>
            </select>
          </label>
        </div>

        {booksQuery.isLoading ? (
          <BookGridSkeleton />
        ) : booksQuery.isError ? (
          <ErrorState
            label={t("explore.error")}
            onRetry={() => {
              void booksQuery.refetch();
            }}
          />
        ) : books.length === 0 ? (
          <EmptyState label={t("explore.empty")} />
        ) : (
          <>
            <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
              {books.map((book) => (
                <BookSummaryCard key={book.id} book={book} />
              ))}
            </div>

            {booksQuery.hasNextPage && (
              <div className="mt-5 flex justify-center">
                <Button
                  variant="outline"
                  onClick={() => booksQuery.fetchNextPage()}
                  disabled={booksQuery.isFetchingNextPage}
                >
                  {booksQuery.isFetchingNextPage
                    ? t("explore.loadingMore")
                    : t("common.loadMore")}
                </Button>
              </div>
            )}
          </>
        )}
      </section>
    </div>
  );
}

function BookGridSkeleton() {
  return (
    <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
      {Array.from({ length: 10 }).map((_, index) => (
        <div key={index} className="animate-pulse rounded-lg border border-stone-200 bg-white p-3">
          <div className="mb-3 aspect-[2/3] rounded-lg bg-stone-200" />
          <div className="h-4 w-3/4 rounded bg-stone-200" />
          <div className="mt-2 h-3 w-1/2 rounded bg-stone-200" />
          <div className="mt-3 h-8 rounded bg-stone-200" />
          <div className="mt-2 h-8 rounded bg-stone-200" />
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
