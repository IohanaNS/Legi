import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { libraryApi, type CreateReadingPostBody, type LibraryQuery } from "../api";
import { libraryKeys } from "../queryKeys";

// In-progress books, most-recently-updated first (backend orders by UpdatedAt desc).
// Powers both the "Reading now" card and the "change book" picker.
export function useReadingBooks(search?: string) {
  const query: LibraryQuery = {
    status: "Reading",
    search: search?.trim() || undefined,
    page: 1,
    pageSize: 50,
  };
  return useQuery({
    queryKey: libraryKeys.books(query),
    queryFn: () => libraryApi.getUserBooks(query),
    select: (data) => data.items,
  });
}

export function useUpdateProgress() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ userBookId, body }: { userBookId: string; body: CreateReadingPostBody }) =>
      libraryApi.createReadingPost(userBookId, body),
    onSuccess: () => {
      // Refresh the "Reading now" card/library lists and the feed (the new post fans out).
      qc.invalidateQueries({ queryKey: libraryKeys.all });
      qc.invalidateQueries({ queryKey: ["feed"] });
    },
  });
}
