import { http } from "../../services/http";
import type {
  BackendReadingStatus, PaginatedList, UserBookDto, UserListSummaryDto,
} from "./types";

export interface LibraryQuery {
  status?: BackendReadingStatus;
  wishlist?: boolean;
  search?: string;
  page?: number;
  pageSize?: number;
}

export const libraryApi = {
  getUserBooks: (q: LibraryQuery) =>
    http.get<PaginatedList<UserBookDto>>("/library", { params: q }).then((r) => r.data),
  // GET /library/lists returns a plain array (IReadOnlyList<UserListSummaryDto>).
  getLists: () =>
    http.get<UserListSummaryDto[]>("/library/lists").then((r) => r.data),
};
