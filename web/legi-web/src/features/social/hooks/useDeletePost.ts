import {
  useMutation,
  useQueryClient,
  type InfiniteData,
  type QueryKey,
} from "@tanstack/react-query";
import { libraryApi } from "../../library/api";
import { catalogKeys } from "../../catalog/queryKeys";
import type { FeedItemDto, SocialPaginatedList } from "../types";

type FeedCache = InfiniteData<SocialPaginatedList<FeedItemDto>>;

/**
 * Deletes a reading post or review (both are ReadingProgress rows; the feed
 * item's referenceId is the postId/reviewId). The Social purge of the FeedItem
 * fans out asynchronously, so we optimistically drop the item from the displayed
 * list (feed or a book's reviews) instead of refetching it back.
 */
export function useDeletePost(listKey: QueryKey) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (item: FeedItemDto) => libraryApi.deleteReadingPost(item.referenceId),
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
      if (item.bookId) {
        qc.invalidateQueries({ queryKey: catalogKeys.bookDetails(item.bookId) });
      }
    },
  });
}
