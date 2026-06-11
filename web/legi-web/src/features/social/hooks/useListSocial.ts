import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { socialApi } from "../api";
import { interactionKeys } from "../queryKeys";
import type { ListSocialStateDto } from "../types";

/**
 * Live social state of a list (counts + the viewer's like/follow flags), with
 * optimistic like and follow toggles. Distinct from the user-to-user follow
 * hooks — a list follow targets the list, not its owner.
 */
export function useListSocial(listId: string, enabled = true) {
  const qc = useQueryClient();
  const key = interactionKeys.listSocial(listId);

  const query = useQuery({
    queryKey: key,
    queryFn: () => socialApi.getListSocialState(listId),
    enabled,
  });

  function patch(partial: (s: ListSocialStateDto) => ListSocialStateDto) {
    qc.setQueryData<ListSocialStateDto>(key, (prev) => (prev ? partial(prev) : prev));
  }

  const toggleLike = useMutation({
    mutationFn: (liked: boolean) =>
      liked ? socialApi.unlike("lists", listId) : socialApi.like("lists", listId),
    onMutate: async (liked) => {
      await qc.cancelQueries({ queryKey: key });
      const prev = qc.getQueryData<ListSocialStateDto>(key);
      patch((s) => ({
        ...s,
        isLikedByMe: !liked,
        likesCount: s.likesCount + (liked ? -1 : 1),
      }));
      return { prev };
    },
    onError: (_e, _v, ctx) => {
      if (ctx?.prev) qc.setQueryData(key, ctx.prev);
    },
    onSettled: () => qc.invalidateQueries({ queryKey: key }),
  });

  const toggleFollow = useMutation({
    mutationFn: (following: boolean) =>
      following ? socialApi.unfollowList(listId) : socialApi.followList(listId),
    onMutate: async (following) => {
      await qc.cancelQueries({ queryKey: key });
      const prev = qc.getQueryData<ListSocialStateDto>(key);
      patch((s) => ({
        ...s,
        isFollowedByMe: !following,
        followersCount: s.followersCount + (following ? -1 : 1),
      }));
      return { prev };
    },
    onError: (_e, _v, ctx) => {
      if (ctx?.prev) qc.setQueryData(key, ctx.prev);
    },
    onSettled: () => qc.invalidateQueries({ queryKey: key }),
  });

  return { query, toggleLike, toggleFollow };
}
