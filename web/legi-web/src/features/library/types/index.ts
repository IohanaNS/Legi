// ---- Library DTOs (camelCase JSON, mirror the backend records) ----

// Mirrors Legi.Library.Application.Common.DTOs.PaginatedList<T>.
// NOTE: property names follow the backend exactly (pageNumber/totalCount/hasNextPage),
// not the generic { page, totalItems, hasNext } shape.
export interface PaginatedList<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export type BackendReadingStatus =
  | "NotStarted" | "Reading" | "Finished" | "Abandoned" | "Paused";
export type ProgressType = "Page" | "Percentage";

export interface BookSnapshotDto {
  bookId: string;
  title: string;
  authorDisplay: string;
  coverUrl?: string | null;
  pageCount?: number | null;
}

export interface UserBookDto {
  userBookId: string;
  bookId: string;
  status: BackendReadingStatus;
  progressValue?: number | null;
  progressType?: ProgressType | null;
  wishlist: boolean;
  ratingStars?: number | null; // 0.5–5.0, half-star steps
  finishedReadingAt?: string | null; // yyyy-MM-dd, null = finished but date unknown
  book: BookSnapshotDto;
  createdAt: string;
  updatedAt: string;
}

export interface AddBookToLibraryResponse {
  userBookId: string;
  bookId: string;
  status: BackendReadingStatus;
  wishlist: boolean;
  createdAt: string;
}

export interface UserListSummaryDto {
  listId: string;
  name: string;
  description?: string | null;
  isPublic: boolean;
  booksCount: number;
  likesCount: number;
  createdAt: string;
}

// UI tab keys (i18n)
export type ProfileTab =
  | "activity"
  | "reading"
  | "finished"
  | "paused"
  | "abandoned"
  | "not_started"
  | "lists";
export type ViewMode = "grid" | "list";
