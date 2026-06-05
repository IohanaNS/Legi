import { http } from "../../services/http";
import type {
  AddBookToLibraryResponse,
  BackendReadingStatus,
  PaginatedList,
  ProgressType,
  UserBookDto,
  UserListSummaryDto,
} from "./types";

export interface LibraryQuery {
  status?: BackendReadingStatus;
  wishlist?: boolean;
  search?: string;
  page?: number;
  pageSize?: number;
}

// POST /library/{userBookId}/posts request body.
// IMPORTANT: the Library API has no JsonStringEnumConverter registered, so the
// ProgressType enum must be sent as its INTEGER value (Page=0, Percentage=1),
// not the string. We accept the readable string here and map it on the wire.
export interface CreateReadingPostBody {
  content?: string;
  progressValue?: number;
  progressType?: ProgressType;
}

export interface UpdateUserBookBody {
  status?: BackendReadingStatus;
  wishlist?: boolean;
  progressValue?: number;
  progressType?: ProgressType;
}

const PROGRESS_TYPE_WIRE: Record<ProgressType, number> = {
  Page: 0,
  Percentage: 1,
};

const READING_STATUS_WIRE: Record<BackendReadingStatus, number> = {
  NotStarted: 0,
  Reading: 1,
  Finished: 2,
  Abandoned: 3,
  Paused: 4,
};

export const libraryApi = {
  getUserBooks: (q: LibraryQuery) =>
    http.get<PaginatedList<UserBookDto>>("/library", { params: q }).then((r) => r.data),
  // GET /library/lists returns a plain array (IReadOnlyList<UserListSummaryDto>).
  getLists: () =>
    http.get<UserListSummaryDto[]>("/library/lists").then((r) => r.data),
  createReadingPost: (userBookId: string, body: CreateReadingPostBody) =>
    http.post(`/library/${userBookId}/posts`, {
      content: body.content,
      progressValue: body.progressValue,
      progressType:
        body.progressType !== undefined ? PROGRESS_TYPE_WIRE[body.progressType] : undefined,
    }),
  addBookToLibrary: (bookId: string, wishlist: boolean) =>
    http
      .post<AddBookToLibraryResponse>("/library", { bookId, wishlist })
      .then((r) => r.data),
  updateUserBook: (userBookId: string, body: UpdateUserBookBody) =>
    http.patch(`/library/${userBookId}`, {
      status: body.status !== undefined ? READING_STATUS_WIRE[body.status] : undefined,
      wishlist: body.wishlist,
      progressValue: body.progressValue,
      progressType:
        body.progressType !== undefined ? PROGRESS_TYPE_WIRE[body.progressType] : undefined,
    }),
  rateUserBook: (userBookId: string, stars: number) =>
    http.put(`/library/${userBookId}/rating`, { stars }),
  removeUserBookRating: (userBookId: string) =>
    http.delete(`/library/${userBookId}/rating`),
  removeBookFromLibrary: (userBookId: string) =>
    http.delete(`/library/${userBookId}`),
};
