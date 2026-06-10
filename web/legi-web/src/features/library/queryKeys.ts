import type { LibraryQuery } from "./api";
import type { BackendReadingStatus } from "./types";

export const libraryKeys = {
  all: ["library"] as const,
  books: (q: LibraryQuery) => [...libraryKeys.all, "books", q] as const,
  count: (status: BackendReadingStatus) => [...libraryKeys.all, "count", status] as const,
  lists: () => [...libraryKeys.all, "lists"] as const,
  userBookByBook: (bookId: string) => [...libraryKeys.all, "byBook", bookId] as const,
  userBooks: (userId: string, status: BackendReadingStatus) =>
    [...libraryKeys.all, "userBooks", userId, status] as const,
  userStats: (userId: string) => [...libraryKeys.all, "userStats", userId] as const,
  userLists: (userId: string) => [...libraryKeys.all, "userLists", userId] as const,
};
