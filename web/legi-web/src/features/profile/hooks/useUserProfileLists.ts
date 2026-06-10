import { useInfiniteQuery } from "@tanstack/react-query";
import { libraryApi } from "../../library/api";
import { libraryKeys } from "../../library/queryKeys";

const PAGE_SIZE = 20;

interface UseUserProfileListsOptions {
  enabled?: boolean;
}

export function useUserProfileLists(
  userId: string | undefined,
  { enabled = true }: UseUserProfileListsOptions = {},
) {
  return useInfiniteQuery({
    queryKey: libraryKeys.userLists(userId ?? ""),
    queryFn: ({ pageParam }) => libraryApi.getUserLists(userId!, pageParam, PAGE_SIZE),
    initialPageParam: 1,
    getNextPageParam: (last) => (last.hasNextPage ? last.pageNumber + 1 : undefined),
    enabled: enabled && !!userId,
  });
}
