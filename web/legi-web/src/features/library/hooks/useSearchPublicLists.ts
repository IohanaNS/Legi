import { useQuery } from "@tanstack/react-query";
import { libraryApi } from "../api";
import { libraryKeys } from "../queryKeys";

/**
 * Searches PUBLIC lists from any user (global search). Complements `useLists`,
 * which only returns the current user's own lists.
 */
export function useSearchPublicLists(search: string, enabled = true) {
  const term = search.trim();
  return useQuery({
    queryKey: libraryKeys.publicListSearch(term),
    queryFn: () => libraryApi.searchPublicLists(term),
    enabled: enabled && term.length > 0,
  });
}
