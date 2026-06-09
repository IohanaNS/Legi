import { useInfiniteQuery } from "@tanstack/react-query";
import { socialApi } from "../api";
import { feedKeys } from "../queryKeys";

const PAGE_SIZE = 20;

export function useBookReviews(bookId: string | undefined) {
  return useInfiniteQuery({
    queryKey: feedKeys.bookReviews(bookId ?? ""),
    queryFn: ({ pageParam }) => socialApi.getBookReviews(bookId!, pageParam, PAGE_SIZE),
    initialPageParam: 1,
    getNextPageParam: (last) => (last.hasNext ? last.page + 1 : undefined),
    enabled: !!bookId,
  });
}
