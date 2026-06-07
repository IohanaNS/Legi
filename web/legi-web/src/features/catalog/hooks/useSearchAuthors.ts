import { useQuery } from "@tanstack/react-query";
import { catalogApi } from "../api";
import { catalogKeys } from "../queryKeys";

const AUTHOR_SEARCH_LIMIT = 5;

export function useSearchAuthors(searchTerm: string, enabled = true) {
  const normalizedSearch = searchTerm.trim();

  return useQuery({
    queryKey: catalogKeys.authorSearch(normalizedSearch, AUTHOR_SEARCH_LIMIT),
    queryFn: () => catalogApi.searchAuthors(normalizedSearch, AUTHOR_SEARCH_LIMIT),
    enabled: enabled && normalizedSearch.length > 0,
    staleTime: 30_000,
  });
}
