import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { useNavigate, useSearchParams } from "react-router-dom";
import { Loader2, Plus, X } from "lucide-react";
import { Button } from "../../../components/ui/Button";
import { BookSummaryCard } from "./BookSummaryCard";
import { SearchBar } from "./SearchBar";
import { TagFilter } from "./TagFilter";
import { usePopularTags } from "../hooks/usePopularTags";
import { useSearchBooks } from "../hooks/useSearchBooks";
import type { SortOption, TagResult } from "../types";

export default function ExplorePage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();

  const searchInput = searchParams.get("search") ?? "";
  const authorSlug = searchParams.get("authorSlug") ?? undefined;
  const authorName = searchParams.get("authorName");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [selectedTags, setSelectedTags] = useState<TagResult[]>([]);
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
    authorSlug,
    tagSlugs: selectedTags.map((tag) => tag.slug),
    sort,
  });

  const books = booksQuery.data?.pages.flatMap((page) => page.books) ?? [];
  const totalCount = booksQuery.data?.pages[0]?.pagination.totalCount ?? 0;
  const enrichmentStatus = booksQuery.data?.pages[0]?.enrichment?.status;
  const isSearchingExternal =
    enrichmentStatus === "Queued" || enrichmentStatus === "AlreadyQueued";

  const handleToggleTag = (tag: TagResult) => {
    setSelectedTags((current) =>
      current.some((t) => t.slug === tag.slug)
        ? current.filter((t) => t.slug !== tag.slug)
        : [...current, tag],
    );
  };

  const handleSearchChange = (value: string) => {
    const nextParams = new URLSearchParams(searchParams);
    if (value.trim()) {
      nextParams.set("search", value);
    } else {
      nextParams.delete("search");
    }

    setSearchParams(nextParams, { replace: true });
  };

  const clearAuthorFilter = () => {
    const nextParams = new URLSearchParams(searchParams);
    nextParams.delete("authorSlug");
    nextParams.delete("authorName");
    setSearchParams(nextParams, { replace: true });
  };

  return (
    <div className="space-y-6">
      <header className="flex items-start justify-between gap-4">
        <div>
          <h1 className="font-serif text-[1.5rem] font-semibold leading-tight text-stone-800 dark:text-stone-100">
            {t("explore.title")}
          </h1>
          <p className="mt-1 text-sm text-stone-500 dark:text-stone-400">{t("explore.subtitle")}</p>
        </div>
        <Button onClick={() => navigate("/books/new")} className="shrink-0">
          <span className="flex items-center gap-1.5">
            <Plus size={16} />
            {t("explore.registerNewBook")}
          </span>
        </Button>
      </header>

      <SearchBar value={searchInput} onChange={handleSearchChange} />

      {authorSlug && (
        <div className="flex">
          <button
            type="button"
            onClick={clearAuthorFilter}
            className="inline-flex max-w-full items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 px-3 py-1.5 text-sm text-amber-900 transition-colors hover:bg-amber-100 focus:outline-none focus:ring-2 focus:ring-amber-500/20 dark:border-amber-900/50 dark:bg-amber-900/20 dark:text-amber-100 dark:hover:bg-amber-900/30"
          >
            <span className="truncate">
              {t("explore.authorFilter", {
                author: authorName ?? authorSlug,
              })}
            </span>
            <X size={14} className="shrink-0" />
          </button>
        </div>
      )}

      <TagFilter
        tags={tagsQuery.data ?? []}
        selectedTags={selectedTags}
        isLoading={tagsQuery.isLoading}
        isError={tagsQuery.isError}
        onRetry={() => {
          void tagsQuery.refetch();
        }}
        onToggleTag={handleToggleTag}
      />

      <section>
        <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-sm text-stone-600 dark:text-stone-300">
            {t("explore.booksFound", { count: totalCount })}
          </p>

          <label className="flex items-center gap-2 text-sm text-stone-600 dark:text-stone-300">
            {t("explore.sortLabel")}
            <select
              value={sort}
              onChange={(event) => setSort(event.target.value as SortOption)}
              className="cursor-pointer rounded-lg border border-stone-200 dark:border-dark-raised bg-white dark:bg-dark-card px-3 py-1.5 text-sm text-stone-700 dark:text-stone-200 focus:border-green-600 focus:outline-none focus:ring-2 focus:ring-green-600/20"
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
        ) : books.length === 0 && isSearchingExternal ? (
          <SearchingExternalState
            label={t("explore.searchingExternal")}
            hint={t("explore.searchingExternalHint")}
          />
        ) : books.length === 0 ? (
          <EmptyState label={t("explore.empty")} />
        ) : (
          <>
            {isSearchingExternal && (
              <div className="mb-4 flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-900 dark:border-amber-900/50 dark:bg-amber-900/20 dark:text-amber-100">
                <Loader2 size={16} className="shrink-0 animate-spin" />
                <span>{t("explore.searchingMore")}</span>
              </div>
            )}

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
        <div key={index} className="animate-pulse rounded-lg border border-stone-200 dark:border-dark-raised bg-white dark:bg-dark-card p-3">
          <div className="mb-3 aspect-[2/3] rounded-lg bg-stone-200 dark:bg-dark-raised" />
          <div className="h-4 w-3/4 rounded bg-stone-200 dark:bg-dark-raised" />
          <div className="mt-2 h-3 w-1/2 rounded bg-stone-200 dark:bg-dark-raised" />
          <div className="mt-3 h-8 rounded bg-stone-200 dark:bg-dark-raised" />
          <div className="mt-2 h-8 rounded bg-stone-200 dark:bg-dark-raised" />
        </div>
      ))}
    </div>
  );
}

function EmptyState({ label }: { label: string }) {
  return <p className="py-10 text-center text-sm text-stone-400 dark:text-stone-500">{label}</p>;
}

function SearchingExternalState({ label, hint }: { label: string; hint: string }) {
  return (
    <div className="flex flex-col items-center gap-3 py-10 text-center">
      <Loader2 size={24} className="animate-spin text-green-600" />
      <p className="text-sm font-medium text-stone-600 dark:text-stone-300">{label}</p>
      <p className="text-xs text-stone-400 dark:text-stone-500">{hint}</p>
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
