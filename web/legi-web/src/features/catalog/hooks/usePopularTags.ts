import { useQuery } from "@tanstack/react-query";
import { catalogApi } from "../api";
import { catalogKeys } from "../queryKeys";

export function usePopularTags() {
  return useQuery({
    queryKey: catalogKeys.popularTags(),
    queryFn: catalogApi.getPopularTags,
  });
}
