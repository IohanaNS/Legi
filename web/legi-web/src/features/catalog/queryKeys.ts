import type { SearchBooksParams } from "./types";

export const catalogKeys = {
  all: ["catalog"] as const,
  search: (params: SearchBooksParams) => [...catalogKeys.all, "search", params] as const,
  popularTags: () => [...catalogKeys.all, "tags", "popular"] as const,
};
