import { useQuery } from "@tanstack/react-query";
import { catalogApi } from "../api";
import { catalogKeys } from "../queryKeys";

export function useBookDetails(bookId: string | undefined) {
  return useQuery({
    queryKey: catalogKeys.bookDetails(bookId ?? ""),
    queryFn: () => catalogApi.getBookDetails(bookId!),
    enabled: !!bookId,
  });
}
