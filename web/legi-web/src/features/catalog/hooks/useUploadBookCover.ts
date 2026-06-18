import { useMutation, useQueryClient } from "@tanstack/react-query";
import { catalogApi } from "../api";
import { catalogKeys } from "../queryKeys";
import type { BookDetailsDto } from "../types";

/**
 * Manual cover upload — the escape hatch for cover-less books. On success we
 * optimistically patch the cached book detail with the new (owned) cover URL and
 * refetch so the placeholder is replaced immediately.
 */
export function useUploadBookCover(bookId: string) {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: (file: File) => catalogApi.uploadBookCover(bookId, file),
    onSuccess: (data) => {
      qc.setQueryData<BookDetailsDto>(catalogKeys.bookDetails(bookId), (book) =>
        book ? { ...book, coverUrl: data.coverUrl } : book,
      );
      qc.invalidateQueries({ queryKey: catalogKeys.bookDetails(bookId) });
    },
  });
}
