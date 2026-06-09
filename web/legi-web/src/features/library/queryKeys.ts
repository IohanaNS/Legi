import type { LibraryQuery } from "./api";
import type { BackendReadingStatus } from "./types";

export const libraryKeys = {
  all: ["library"] as const,
  books: (q: LibraryQuery) => [...libraryKeys.all, "books", q] as const,
  count: (status: BackendReadingStatus) => [...libraryKeys.all, "count", status] as const,
  lists: () => [...libraryKeys.all, "lists"] as const,
  userBookByBook: (bookId: string) => [...libraryKeys.all, "byBook", bookId] as const,
};
