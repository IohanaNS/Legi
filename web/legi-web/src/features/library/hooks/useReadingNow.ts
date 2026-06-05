import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { libraryApi, type CreateReadingPostBody } from "../api";
import { libraryKeys } from "../queryKeys";

// Fetch only the most recent "Reading" book for the "Reading now" card.
const READING_NOW_QUERY = { status: "Reading" as const, page: 1, pageSize: 1 };

export function useReadingNow() {
  return useQuery({
    queryKey: libraryKeys.books(READING_NOW_QUERY),
    queryFn: () => libraryApi.getUserBooks(READING_NOW_QUERY),
    select: (data) => data.items[0] ?? null,
  });
}

export function useUpdateProgress() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ userBookId, body }: { userBookId: string; body: CreateReadingPostBody }) =>
      libraryApi.createReadingPost(userBookId, body),
    onSuccess: () => {
      // Refresh the "Reading now" card (and any library list) after a progress update.
      qc.invalidateQueries({ queryKey: libraryKeys.all });
    },
  });
}
