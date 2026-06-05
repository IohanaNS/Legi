import { http } from "../../services/http";
import type { SearchBooksParams, SearchBooksResponse, SearchTagsResponse } from "./types";

export const catalogApi = {
  searchBooks: (params: SearchBooksParams) =>
    http.get<SearchBooksResponse>("/catalog/books", { params }).then((r) => r.data),

  getPopularTags: () =>
    http
      .get<SearchTagsResponse>("/catalog/tags/popular", { params: { limit: 20 } })
      .then((r) => r.data.tags),
};
