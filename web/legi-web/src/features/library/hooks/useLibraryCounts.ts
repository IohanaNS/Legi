import { useQueries } from "@tanstack/react-query";
import { libraryApi } from "../api";
import { libraryKeys } from "../queryKeys";
import type { BackendReadingStatus, PaginatedList, UserBookDto } from "../types";

const STATUSES: BackendReadingStatus[] = [
  "Reading",
  "Finished",
  "Paused",
  "Abandoned",
  "NotStarted",
];

export function useLibraryCounts() {
  const results = useQueries({
    queries: STATUSES.map((status) => ({
      queryKey: libraryKeys.count(status),
      queryFn: () => libraryApi.getUserBooks({ status, page: 1, pageSize: 1 }),
      select: (d: PaginatedList<UserBookDto>) => d.totalCount,
    })),
  });

  const counts: Partial<Record<BackendReadingStatus, number>> = {};
  STATUSES.forEach((s, i) => (counts[s] = results[i].data));
  const isLoading = results.some((r) => r.isLoading);

  return { counts, isLoading };
}
