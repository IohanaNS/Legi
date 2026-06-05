import { useInfiniteQuery } from "@tanstack/react-query";
import { socialApi } from "../api";
import { feedKeys } from "../queryKeys";

const PAGE_SIZE = 20;

export function useFeed() {
  return useInfiniteQuery({
    queryKey: feedKeys.list(),
    queryFn: ({ pageParam }) => socialApi.getFeed(pageParam, PAGE_SIZE),
    initialPageParam: 1,
    getNextPageParam: (last) => (last.hasNext ? last.page + 1 : undefined),
  });
}

export function useUserActivity(userId: string | undefined) {
  return useInfiniteQuery({
    queryKey: feedKeys.activity(userId ?? ""),
    queryFn: ({ pageParam }) => socialApi.getUserActivity(userId!, pageParam, PAGE_SIZE),
    initialPageParam: 1,
    getNextPageParam: (last) => (last.hasNext ? last.page + 1 : undefined),
    enabled: !!userId,
  });
}
