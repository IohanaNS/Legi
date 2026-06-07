import { http } from "../../services/http";
import type {
  SearchAuthorsResponse,
  SearchBooksParams,
  SearchBooksResponse,
  SearchTagsResponse,
} from "./types";

export const catalogApi = {
  searchBooks: (params: SearchBooksParams) =>
    http.get<SearchBooksResponse>("/catalog/books", { params }).then((r) => r.data),

  getPopularTags: () =>
    http
      .get<SearchTagsResponse>("/catalog/tags/popular", { params: { limit: 20 } })
      .then((r) => r.data.tags),

  searchAuthors: (searchTerm: string, limit = 10) =>
    http
      .get<SearchAuthorsResponse>("/catalog/authors/search", {
        params: { searchTerm, limit },
      })
      .then((r) => r.data.authors),
};
