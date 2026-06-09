import {
  useMutation,
  useQueryClient,
  type InfiniteData,
  type QueryKey,
} from "@tanstack/react-query";
import { libraryApi } from "../../library/api";
import { catalogKeys } from "../../catalog/queryKeys";
import { socialApi } from "../api";
import { isContentBackedDeletable } from "../lib/feed";
import type { FeedItemDto, SocialPaginatedList } from "../types";

type FeedCache = InfiniteData<SocialPaginatedList<FeedItemDto>>;

/**
 * Deletes manual post/review content through Library, or owner-only automatic
 * activity rows directly through Social. Both paths optimistically drop the
 * displayed feed item.
 */
export function useDeleteFeedItem(listKey: QueryKey) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (item: FeedItemDto) =>
      isContentBackedDeletable(item)
        ? libraryApi.deleteReadingPost(item.referenceId)
        : socialApi.deleteFeedItem(item.id),
    onSuccess: (_data, item) => {
      qc.setQueryData<FeedCache>(listKey, (data) =>
        data && {
          ...data,
          pages: data.pages.map((p, i) => ({
            ...p,
            items: p.items.filter((it) => it.id !== item.id),
            totalItems: i === 0 ? Math.max(0, p.totalItems - 1) : p.totalItems,
          })),
        },
      );
      // Reviews count on the book details page is recomputed by Catalog (async).
      if (isContentBackedDeletable(item) && item.bookId) {
        qc.invalidateQueries({ queryKey: catalogKeys.bookDetails(item.bookId) });
      }
    },
  });
}
