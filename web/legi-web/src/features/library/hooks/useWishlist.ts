import { useInfiniteQuery } from "@tanstack/react-query";
import { libraryApi } from "../api";
import { libraryKeys } from "../queryKeys";

const PAGE_SIZE = 20;

export function useWishlist() {
  return useInfiniteQuery({
    queryKey: libraryKeys.books({ wishlist: true, pageSize: PAGE_SIZE }),
    queryFn: ({ pageParam }) =>
      libraryApi.getUserBooks({ wishlist: true, page: pageParam, pageSize: PAGE_SIZE }),
    initialPageParam: 1,
    getNextPageParam: (last) => (last.hasNextPage ? last.pageNumber + 1 : undefined),
  });
}
