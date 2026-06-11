import { useInfiniteQuery, useQuery } from "@tanstack/react-query";
import { libraryApi } from "../../library/api";
import { libraryKeys } from "../../library/queryKeys";
import type { UserListSummaryDto } from "../../library/types";
import { socialApi } from "../api";
import { interactionKeys } from "../queryKeys";

const PAGE_SIZE = 20;

/**
 * The lists a user follows, ready to render. Two-step resolve: Social owns the
 * follow relationship (returns ids + followedAt, paginated), Library owns the
 * list metadata (hydrated via /lists/by-ids, public lists only). The hydrated
 * summaries are reordered to match the Social follow order, and any ids whose
 * list has since gone private/deleted are dropped.
 */
export function useFollowedLists(userId: string | undefined, enabled = true) {
  const idsQuery = useInfiniteQuery({
    queryKey: interactionKeys.followedLists(userId ?? ""),
    queryFn: ({ pageParam }) => socialApi.getFollowedLists(userId!, pageParam, PAGE_SIZE),
    initialPageParam: 1,
    getNextPageParam: (last) => (last.hasNext ? last.page + 1 : undefined),
    enabled: enabled && !!userId,
  });

  const orderedIds = idsQuery.data?.pages.flatMap((p) => p.items.map((i) => i.listId)) ?? [];

  const hydrateQuery = useQuery({
    queryKey: libraryKeys.listSummariesByIds(orderedIds),
    queryFn: () => libraryApi.getListSummariesByIds(orderedIds),
    enabled: enabled && orderedIds.length > 0,
  });

  const byId = new Map((hydrateQuery.data ?? []).map((l) => [l.listId, l]));
  const lists = orderedIds
    .map((id) => byId.get(id))
    .filter((l): l is UserListSummaryDto => l !== undefined);

  const totalItems = idsQuery.data?.pages[0]?.totalItems ?? 0;
  const isLoading =
    idsQuery.isLoading || (orderedIds.length > 0 && hydrateQuery.isLoading);

  return {
    lists,
    totalItems,
    isLoading,
    isError: idsQuery.isError || hydrateQuery.isError,
    refetch: () => {
      void idsQuery.refetch();
      void hydrateQuery.refetch();
    },
    hasNextPage: idsQuery.hasNextPage,
    fetchNextPage: idsQuery.fetchNextPage,
    isFetchingNextPage: idsQuery.isFetchingNextPage,
  };
}
