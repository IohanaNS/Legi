import type { SearchBooksParams } from "./types";

export const catalogKeys = {
  all: ["catalog"] as const,
  search: (params: SearchBooksParams) => [...catalogKeys.all, "search", params] as const,
  bookDetails: (bookId: string) => [...catalogKeys.all, "book", bookId] as const,
  popularTags: () => [...catalogKeys.all, "tags", "popular"] as const,
  authorSearch: (searchTerm: string, limit: number) =>
    [...catalogKeys.all, "authors", "search", searchTerm, limit] as const,
};
