import { useQuery } from "@tanstack/react-query";
import { libraryApi } from "../api";
import { libraryKeys } from "../queryKeys";

interface UseListsOptions {
  enabled?: boolean;
}

export function useLists({ enabled = true }: UseListsOptions = {}) {
  return useQuery({
    queryKey: libraryKeys.lists(),
    queryFn: libraryApi.getLists,
    enabled,
  });
}
