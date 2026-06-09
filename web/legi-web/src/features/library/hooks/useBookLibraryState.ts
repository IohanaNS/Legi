import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { libraryApi, type CreateBookReviewBody } from "../api";
import { libraryKeys } from "../queryKeys";
import { catalogKeys } from "../../catalog/queryKeys";
import { feedKeys } from "../../social/queryKeys";

/**
 * The viewer's UserBook for a given book (status/rating/progress/userBookId), or
 * null when the book isn't in their library. Drives the book details page header.
 */
export function useMyUserBookByBook(bookId: string | undefined) {
  return useQuery({
    queryKey: libraryKeys.userBookByBook(bookId ?? ""),
    queryFn: () => libraryApi.getMyUserBookByBook(bookId!),
    enabled: !!bookId,
  });
}

/**
 * Sets the viewer's rating for a book (the standalone "your rating" widget). On
 * success refreshes the book details (average) and the viewer's UserBook.
 */
export function useRateBook(bookId: string, userBookId: string | undefined) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (stars: number) => {
      if (!userBookId) throw new Error("Book must be in your library to rate it.");
      return libraryApi.rateUserBook(userBookId, stars);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: catalogKeys.bookDetails(bookId) });
      qc.invalidateQueries({ queryKey: libraryKeys.userBookByBook(bookId) });
      qc.invalidateQueries({ queryKey: feedKeys.all });
    },
  });
}

/**
 * Submits a book review (rating + content + spoiler). On success refreshes the
 * book details (average rating + reviews count), the reviews list, the viewer's
 * UserBook (rating), and the feed (the review fans out).
 */
export function useCreateReview(bookId: string, userBookId: string | undefined) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateBookReviewBody) => {
      if (!userBookId) throw new Error("Book must be in your library to review it.");
      return libraryApi.createBookReview(userBookId, body);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: catalogKeys.bookDetails(bookId) });
      qc.invalidateQueries({ queryKey: feedKeys.bookReviews(bookId) });
      qc.invalidateQueries({ queryKey: libraryKeys.userBookByBook(bookId) });
      qc.invalidateQueries({ queryKey: feedKeys.all });
    },
  });
}
