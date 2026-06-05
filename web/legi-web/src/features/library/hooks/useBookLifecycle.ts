import { useMutation, useQueryClient } from "@tanstack/react-query";
import { libraryApi } from "../api";
import { libraryKeys } from "../queryKeys";
import type { BackendReadingStatus } from "../types";

function useLibraryMutation<TArgs>(mutationFn: (args: TArgs) => Promise<unknown>) {
  const qc = useQueryClient();

  return useMutation({
    mutationFn,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: libraryKeys.all });
    },
  });
}

export function useUpdateBookStatus() {
  return useLibraryMutation(
    ({ userBookId, status }: { userBookId: string; status: BackendReadingStatus }) =>
      libraryApi.updateUserBook(userBookId, { status }),
  );
}

export function useRateBook() {
  return useLibraryMutation(
    ({ userBookId, stars }: { userBookId: string; stars: number }) =>
      libraryApi.rateUserBook(userBookId, stars),
  );
}

export function useRemoveRating() {
  return useLibraryMutation(({ userBookId }: { userBookId: string }) =>
    libraryApi.removeUserBookRating(userBookId),
  );
}

export function useRemoveBook() {
  return useLibraryMutation(({ userBookId }: { userBookId: string }) =>
    libraryApi.removeBookFromLibrary(userBookId),
  );
}
