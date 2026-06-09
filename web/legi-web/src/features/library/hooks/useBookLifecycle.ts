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
    ({
      userBookId,
      status,
      finishedReadingAt,
    }: {
      userBookId: string;
      status: BackendReadingStatus;
      // Only meaningful for status "Finished": yyyy-MM-dd, or null = date unknown.
      finishedReadingAt?: string | null;
    }) => libraryApi.updateUserBook(userBookId, { status, finishedReadingAt }),
  );
}

/**
 * Marks a book the user already read as Finished in one go: adds it to the library,
 * then sets status Finished with an (optional) finish date. Used from Explore and
 * the book details page when the book isn't in the library yet.
 */
export function useMarkBookAsRead() {
  return useLibraryMutation(
    async ({
      bookId,
      finishedReadingAt,
    }: {
      bookId: string;
      finishedReadingAt?: string | null;
    }) => {
      const added = await libraryApi.addBookToLibrary(bookId, false);
      await libraryApi.updateUserBook(added.userBookId, {
        status: "Finished",
        finishedReadingAt,
      });
    },
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
