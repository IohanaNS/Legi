import { useQuery } from "@tanstack/react-query";
import { catalogApi } from "../api";
import { catalogKeys } from "../queryKeys";

const TAG_SEARCH_LIMIT = 20;

export function useSearchTags(searchTerm: string, enabled = true) {
  const normalizedSearch = searchTerm.trim();

  return useQuery({
    queryKey: catalogKeys.tagSearch(normalizedSearch, TAG_SEARCH_LIMIT),
    queryFn: () => catalogApi.searchTags(normalizedSearch, TAG_SEARCH_LIMIT),
    enabled: enabled && normalizedSearch.length > 0,
    staleTime: 30_000,
  });
}
