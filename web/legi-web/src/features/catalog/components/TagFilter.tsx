import { useEffect, useState } from "react";
import { Filter, Search } from "lucide-react";
import { useTranslation } from "react-i18next";
import { Button } from "../../../components/ui/Button";
import { cn } from "../../../lib/utils";
import { useSearchTags } from "../hooks/useSearchTags";
import type { TagResult } from "../types";

interface TagFilterProps {
  tags: TagResult[];
  selectedTags: TagResult[];
  isLoading: boolean;
  isError: boolean;
  onRetry: () => void;
  onToggleTag: (tag: TagResult) => void;
}

export function TagFilter({
  tags,
  selectedTags,
  isLoading,
  isError,
  onRetry,
  onToggleTag,
}: TagFilterProps) {
  const { t } = useTranslation();
  const [searchInput, setSearchInput] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setDebouncedSearch(searchInput.trim());
    }, 300);

    return () => window.clearTimeout(timeoutId);
  }, [searchInput]);

  const isSearching = debouncedSearch.length > 0;
  const searchQuery = useSearchTags(debouncedSearch, isSearching);

  const baseTags = isSearching ? (searchQuery.data ?? []) : tags;
  const selectedSlugs = new Set(selectedTags.map((tag) => tag.slug));
  // Pin the active filters first (and removable) so they stay visible even when
  // they aren't in the current popular/search results.
  const displayedTags = [
    ...selectedTags,
    ...baseTags.filter((tag) => !selectedSlugs.has(tag.slug)),
  ];
  const displayedLoading = isSearching ? searchQuery.isLoading : isLoading;
  const displayedError = isSearching ? searchQuery.isError : isError;
  const handleRetry = isSearching ? () => void searchQuery.refetch() : onRetry;

  return (
    <div>
      <div className="mb-3 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-2 text-sm text-stone-600 dark:text-stone-300">
          <Filter size={16} />
          {t("explore.filterByTag")}
        </div>

        <div className="relative w-full sm:max-w-xs">
          <Search
            size={16}
            className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-stone-400"
          />
          <input
            type="text"
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
            placeholder={t("explore.searchTagPlaceholder")}
            className="w-full rounded-lg border border-stone-200 dark:border-dark-raised bg-white dark:bg-dark-card py-1.5 pl-9 pr-3 text-sm text-stone-700 dark:text-stone-200 placeholder:text-stone-400 focus:border-green-600 focus:outline-none focus:ring-2 focus:ring-green-600/20"
          />
        </div>
      </div>

      {displayedLoading ? (
        <div className="flex flex-wrap gap-2">
          {Array.from({ length: 8 }).map((_, index) => (
            <div key={index} className="h-8 w-20 animate-pulse rounded-full bg-stone-200 dark:bg-dark-raised" />
          ))}
        </div>
      ) : displayedError ? (
        <div className="flex items-center gap-3">
          <p className="text-sm text-stone-500">{t("explore.tagsError")}</p>
          <Button variant="outline" size="sm" onClick={handleRetry}>
            {t("common.retry")}
          </Button>
        </div>
      ) : displayedTags.length > 0 ? (
        <div className="flex flex-wrap gap-2">
          {displayedTags.map((tag) => {
            const isSelected = selectedSlugs.has(tag.slug);
            return (
              <button
                key={tag.slug}
                type="button"
                onClick={() => onToggleTag(tag)}
                className={cn(
                  "rounded-full border px-3 py-1.5 text-sm transition-colors",
                  isSelected
                    ? "border-green-600 bg-green-600 text-white"
                    : "border-stone-300 dark:border-dark-raised bg-white dark:bg-dark-card text-stone-600 dark:text-stone-300 hover:border-stone-400 dark:hover:border-stone-500",
                )}
              >
                {tag.name}
              </button>
            );
          })}
        </div>
      ) : isSearching ? (
        <p className="text-sm text-stone-400 dark:text-stone-500">
          {t("explore.noTagsFound", { search: debouncedSearch })}
        </p>
      ) : null}
    </div>
  );
}
