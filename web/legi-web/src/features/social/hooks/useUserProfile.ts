import { useQuery } from "@tanstack/react-query";
import { socialApi } from "../api";
import { socialKeys } from "../queryKeys";

export function useUserProfile(userId: string | undefined) {
  return useQuery({
    queryKey: socialKeys.profile(userId ?? ""),
    queryFn: () => socialApi.getUserProfile(userId!),
    enabled: !!userId,
  });
}
