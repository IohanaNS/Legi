import { useMutation, useQueryClient } from "@tanstack/react-query";
import { socialApi } from "../api";
import { socialKeys } from "../queryKeys";
import type { UserProfileDto } from "../types";

interface ToggleFollowVars {
  userId: string;
  isFollowing: boolean;
}

/**
 * Optimistic follow/unfollow. Patches the cached UserProfileDto (flip isFollowing
 * + followersCount ± 1); rolls back on error; invalidates on settle.
 */
export function useToggleFollow() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ userId, isFollowing }: ToggleFollowVars) =>
      isFollowing ? socialApi.unfollow(userId) : socialApi.follow(userId),
    onMutate: async ({ userId, isFollowing }) => {
      const key = socialKeys.profile(userId);
      await qc.cancelQueries({ queryKey: key });
      const prev = qc.getQueryData<UserProfileDto>(key);
      qc.setQueryData<UserProfileDto>(key, (p) =>
        p
          ? {
              ...p,
              isFollowing: !isFollowing,
              followersCount: p.followersCount + (isFollowing ? -1 : 1),
            }
          : p,
      );
      return { prev, key };
    },
    onError: (_e, _vars, ctx) => {
      if (ctx?.prev) qc.setQueryData(ctx.key, ctx.prev);
    },
    onSettled: (_d, _e, { userId }) => {
      qc.invalidateQueries({ queryKey: socialKeys.profile(userId) });
      qc.invalidateQueries({ queryKey: socialKeys.userSearches() });
    },
  });
}
