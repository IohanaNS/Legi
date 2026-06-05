import { useInfiniteQuery } from "@tanstack/react-query";
import { catalogApi } from "../api";
import { sortOptionToBackend } from "../lib/sort";
import { catalogKeys } from "../queryKeys";
import type { SearchBooksParams, SortOption } from "../types";

const PAGE_SIZE = 20;

interface UseSearchBooksArgs {
  searchTerm?: string;
  tagSlug?: string;
  sort: SortOption;
}

export function useSearchBooks({ searchTerm, tagSlug, sort }: UseSearchBooksArgs) {
  const { sortBy, sortDescending } = sortOptionToBackend[sort];
  const params: SearchBooksParams = {
    searchTerm: searchTerm?.trim() || undefined,
    tagSlug,
    sortBy,
    sortDescending,
    pageSize: PAGE_SIZE,
  };

  return useInfiniteQuery({
    queryKey: catalogKeys.search(params),
    queryFn: ({ pageParam }) =>
      catalogApi.searchBooks({ ...params, pageNumber: pageParam }),
    initialPageParam: 1,
    getNextPageParam: (last) =>
      last.pagination.hasNext ? last.pagination.currentPage + 1 : undefined,
  });
}
