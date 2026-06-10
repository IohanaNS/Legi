import { useQuery } from "@tanstack/react-query";
import { libraryApi } from "../../library/api";
import { libraryKeys } from "../../library/queryKeys";

interface UseUserLibraryStatsOptions {
  enabled?: boolean;
}

export function useUserLibraryStats(
  userId: string | undefined,
  { enabled = true }: UseUserLibraryStatsOptions = {},
) {
  return useQuery({
    queryKey: libraryKeys.userStats(userId ?? ""),
    queryFn: () => libraryApi.getUserLibraryStats(userId!),
    enabled: enabled && !!userId,
  });
}
