import {
  useMutation,
  useQueryClient,
  type InfiniteData,
  type QueryKey,
} from "@tanstack/react-query";
import { socialApi } from "../api";
import { interactionResource } from "../lib/feed";
import type { FeedItemDto, SocialPaginatedList } from "../types";

type FeedCache = InfiniteData<SocialPaginatedList<FeedItemDto>>;

function patchItem(
  data: FeedCache | undefined,
  id: string,
  patch: (i: FeedItemDto) => FeedItemDto,
): FeedCache | undefined {
  if (!data) return data;
  return {
    ...data,
    pages: data.pages.map((p) => ({
      ...p,
      items: p.items.map((it) => (it.id === id ? patch(it) : it)),
    })),
  };
}

/**
 * Optimistic like/unlike for a feed item. Parametrized with the queryKey of the
 * displayed list (feed or activity) so it patches the correct cache.
 */
export function useToggleLike(listKey: QueryKey) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (item: FeedItemDto) => {
      const resource = interactionResource(item.targetType);
      if (!resource) throw new Error("Not interactable");
      return item.isLikedByMe
        ? socialApi.unlike(resource, item.referenceId)
        : socialApi.like(resource, item.referenceId);
    },
    onMutate: async (item) => {
      await qc.cancelQueries({ queryKey: listKey });
      const prev = qc.getQueryData<FeedCache>(listKey);
      qc.setQueryData<FeedCache>(listKey, (d) =>
        patchItem(d, item.id, (it) => ({
          ...it,
          isLikedByMe: !it.isLikedByMe,
          likesCount: it.likesCount + (it.isLikedByMe ? -1 : 1),
        })),
      );
      return { prev };
    },
    onError: (_e, _item, ctx) => {
      if (ctx?.prev) qc.setQueryData(listKey, ctx.prev);
    },
    onSettled: () => qc.invalidateQueries({ queryKey: listKey }),
  });
}
