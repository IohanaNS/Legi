import { useInfiniteQuery } from "@tanstack/react-query";
import { socialApi } from "../api";
import { interactionKeys } from "../queryKeys";

const PAGE_SIZE = 20;

export function useFollowers(userId: string | undefined, enabled = true) {
  return useInfiniteQuery({
    queryKey: interactionKeys.followers(userId ?? ""),
    queryFn: ({ pageParam }) => socialApi.getFollowers(userId!, pageParam, PAGE_SIZE),
    initialPageParam: 1,
    getNextPageParam: (last) => (last.hasNext ? last.page + 1 : undefined),
    enabled: !!userId && enabled,
  });
}

export function useFollowing(userId: string | undefined, enabled = true) {
  return useInfiniteQuery({
    queryKey: interactionKeys.following(userId ?? ""),
    queryFn: ({ pageParam }) => socialApi.getFollowing(userId!, pageParam, PAGE_SIZE),
    initialPageParam: 1,
    getNextPageParam: (last) => (last.hasNext ? last.page + 1 : undefined),
    enabled: !!userId && enabled,
  });
}
