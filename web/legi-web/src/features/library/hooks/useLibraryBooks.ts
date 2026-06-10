import { useInfiniteQuery } from "@tanstack/react-query";
import { libraryApi } from "../api";
import { libraryKeys } from "../queryKeys";
import type { BackendReadingStatus } from "../types";

const PAGE_SIZE = 20;

export function useLibraryBooks(status: BackendReadingStatus, enabled = true) {
  return useInfiniteQuery({
    queryKey: libraryKeys.books({ status, pageSize: PAGE_SIZE }),
    queryFn: ({ pageParam }) =>
      libraryApi.getUserBooks({ status, page: pageParam, pageSize: PAGE_SIZE }),
    initialPageParam: 1,
    getNextPageParam: (last) => (last.hasNextPage ? last.pageNumber + 1 : undefined),
    enabled,
  });
}

// Any user's library filtered by status (e.g. the profile "Read" page).
export function useUserLibrary(userId: string | undefined, status: BackendReadingStatus) {
  return useInfiniteQuery({
    queryKey: libraryKeys.userBooks(userId ?? "", status),
    queryFn: ({ pageParam }) => libraryApi.getUserLibrary(userId!, status, pageParam, PAGE_SIZE),
    initialPageParam: 1,
    getNextPageParam: (last) => (last.hasNextPage ? last.pageNumber + 1 : undefined),
    enabled: !!userId,
  });
}
