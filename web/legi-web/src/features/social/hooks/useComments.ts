import {
  useInfiniteQuery,
  useMutation,
  useQueryClient,
  type QueryKey,
} from "@tanstack/react-query";
import { socialApi, type Resource } from "../api";
import { interactionKeys } from "../queryKeys";

const PAGE_SIZE = 20;

export function useComments(resource: Resource, id: string, enabled = true) {
  return useInfiniteQuery({
    queryKey: interactionKeys.comments(resource, id),
    queryFn: ({ pageParam }) => socialApi.getComments(resource, id, pageParam, PAGE_SIZE),
    initialPageParam: 1,
    getNextPageParam: (last) => (last.hasNext ? last.page + 1 : undefined),
    enabled,
  });
}

/**
 * Non-optimistic comment creation. On success, invalidates the comment list and
 * the displayed feed list (so the card's commentsCount re-fetches).
 */
export function useAddComment(resource: Resource, id: string, listKey: QueryKey) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (content: string) => socialApi.addComment(resource, id, content),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: interactionKeys.comments(resource, id) });
      qc.invalidateQueries({ queryKey: listKey });
    },
  });
}

/**
 * Deletes a comment (allowed for its author or the content owner) and refreshes
 * the comment list plus the host list (so the displayed commentsCount updates).
 */
export function useDeleteComment(resource: Resource, id: string, listKey: QueryKey) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (commentId: string) => socialApi.deleteComment(commentId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: interactionKeys.comments(resource, id) });
      qc.invalidateQueries({ queryKey: listKey });
    },
  });
}
