import { useInfiniteQuery } from "@tanstack/react-query";
import { catalogApi } from "../api";
import { sortOptionToBackend } from "../lib/sort";
import { catalogKeys } from "../queryKeys";
import type { SearchBooksParams, SortOption } from "../types";

const PAGE_SIZE = 20;

interface UseSearchBooksArgs {
  searchTerm?: string;
  authorSlug?: string;
  tagSlug?: string;
  sort: SortOption;
  pageSize?: number;
  enabled?: boolean;
}

export function useSearchBooks({
  searchTerm,
  authorSlug,
  tagSlug,
  sort,
  pageSize = PAGE_SIZE,
  enabled = true,
}: UseSearchBooksArgs) {
  const { sortBy, sortDescending } = sortOptionToBackend[sort];
  const params: SearchBooksParams = {
    searchTerm: searchTerm?.trim() || undefined,
    authorSlug,
    tagSlug,
    sortBy,
    sortDescending,
    pageSize,
  };

  return useInfiniteQuery({
    queryKey: catalogKeys.search(params),
    queryFn: ({ pageParam }) =>
      catalogApi.searchBooks({ ...params, pageNumber: pageParam }),
    enabled,
    initialPageParam: 1,
    getNextPageParam: (last) =>
      last.pagination.hasNext ? last.pagination.currentPage + 1 : undefined,
    // While the backend is fetching/importing books from external providers
    // (Open Library / Google Books) the first response comes back with an
    // enrichment status of Queued/AlreadyQueued. Poll until the job settles so
    // the freshly imported books show up without the user re-searching.
    refetchInterval: (query) => {
      const enrichment = query.state.data?.pages[0]?.enrichment;
      if (enrichment?.status === "Queued" || enrichment?.status === "AlreadyQueued") {
        return Math.max(1, enrichment.refreshAfterSeconds ?? 5) * 1000;
      }
      return false;
    },
  });
}
