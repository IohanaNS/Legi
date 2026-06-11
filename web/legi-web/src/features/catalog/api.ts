import { http } from "../../services/http";
import type {
  BookDetailsDto,
  CreateBookRequest,
  CreateBookResponse,
  SearchAuthorsResponse,
  SearchBooksParams,
  SearchBooksResponse,
  SearchTagsResponse,
} from "./types";

export const catalogApi = {
  searchBooks: (params: SearchBooksParams) =>
    http
      .get<SearchBooksResponse>("/catalog/books", {
        params,
        // Repeat array keys without brackets (tagSlugs=a&tagSlugs=b) so ASP.NET
        // binds them to a string[] parameter.
        paramsSerializer: { indexes: null },
      })
      .then((r) => r.data),

  getBookDetails: (bookId: string) =>
    http.get<BookDetailsDto>(`/catalog/books/${bookId}`).then((r) => r.data),

  createBook: (body: CreateBookRequest) =>
    http.post<CreateBookResponse>("/catalog/books", body).then((r) => r.data),

  getPopularTags: () =>
    http
      .get<SearchTagsResponse>("/catalog/tags/popular", { params: { limit: 20 } })
      .then((r) => r.data.tags),

  searchTags: (searchTerm: string, limit = 20) =>
    http
      .get<SearchTagsResponse>("/catalog/tags/search", {
        params: { searchTerm, limit },
      })
      .then((r) => r.data.tags),

  searchAuthors: (searchTerm: string, limit = 10) =>
    http
      .get<SearchAuthorsResponse>("/catalog/authors/search", {
        params: { searchTerm, limit },
      })
      .then((r) => r.data.authors),
};
