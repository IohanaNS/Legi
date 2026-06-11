import type { LibraryQuery } from "./api";
import type { BackendReadingStatus } from "./types";

export const libraryKeys = {
  all: ["library"] as const,
  books: (q: LibraryQuery) => [...libraryKeys.all, "books", q] as const,
  count: (status: BackendReadingStatus) => [...libraryKeys.all, "count", status] as const,
  lists: () => [...libraryKeys.all, "lists"] as const,
  listDetail: (listId: string) => [...libraryKeys.all, "listDetail", listId] as const,
  publicListSearch: (search: string) => [...libraryKeys.all, "publicListSearch", search] as const,
  listBooks: (listId: string) => [...libraryKeys.all, "listBooks", listId] as const,
  userBookByBook: (bookId: string) => [...libraryKeys.all, "byBook", bookId] as const,
  userBooks: (userId: string, status: BackendReadingStatus) =>
    [...libraryKeys.all, "userBooks", userId, status] as const,
  userStats: (userId: string) => [...libraryKeys.all, "userStats", userId] as const,
  userLists: (userId: string) => [...libraryKeys.all, "userLists", userId] as const,
  listSummariesByIds: (ids: string[]) =>
    [...libraryKeys.all, "listSummariesByIds", ids] as const,
};
