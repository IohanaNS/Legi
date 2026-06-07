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
  });
}
