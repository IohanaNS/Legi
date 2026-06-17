export type BookSortBy = "Relevance" | "Title" | "AverageRating" | "RatingsCount" | "CreatedAt";

export interface AuthorDto {
  name: string;
  slug: string;
}

export interface TagDto {
  name: string;
  slug: string;
}

export interface TagResult extends TagDto {
  usageCount: number;
}

export interface AuthorResult extends AuthorDto {
  booksCount: number;
}

export interface BookSummaryDto {
  id: string;
  isbn: string;
  title: string;
  authors: AuthorDto[];
  coverUrl?: string | null;
  averageRating: number;
  ratingsCount: number;
  tags: TagDto[];
}

// Mirrors Legi.Catalog.Application.Books.Queries.GetBookDetails.GetBookDetailsResponse.
export interface BookDetailsDto {
  id: string;
  isbn: string;
  title: string;
  authors: AuthorDto[];
  synopsis?: string | null;
  pageCount?: number | null;
  publisher?: string | null;
  coverUrl?: string | null;
  averageRating: number;
  ratingsCount: number;
  reviewsCount: number;
  tags: TagDto[];
  createdByUserId?: string | null;
  createdAt: string;
  updatedAt: string;
  workId: string;
  editions: EditionSummaryDto[];
}

// Mirrors Legi.Catalog.Application.Books.DTOs.EditionSummaryDto.
export interface EditionSummaryDto {
  id: string;
  isbn: string;
  title: string;
  coverUrl?: string | null;
  publisher?: string | null;
  pageCount?: number | null;
}

export interface PaginationMetadata {
  currentPage: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export type ExternalSearchStatus =
  | "NotApplicable"
  | "NotNeeded"
  | "Queued"
  | "AlreadyQueued"
  | "RecentlyCompleted"
  | "FailedRecently";

export interface ExternalBookSearchEnrichment {
  status: ExternalSearchStatus;
  message?: string | null;
  refreshAfterSeconds?: number | null;
}

export interface SearchBooksResponse {
  books: BookSummaryDto[];
  pagination: PaginationMetadata;
  enrichment: ExternalBookSearchEnrichment;
}

export interface SearchTagsResponse {
  tags: TagResult[];
}

export interface SearchAuthorsResponse {
  authors: AuthorResult[];
}

export interface SearchBooksParams {
  searchTerm?: string;
  authorSlug?: string;
  tagSlugs?: string[];
  minRating?: number;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: BookSortBy;
  sortDescending?: boolean;
}

export type SortOption = "bestRated" | "mostRecent" | "mostPopular";

export interface CreateBookRequest {
  isbn: string;
  title: string;
  authors: string[];
  synopsis: string;
  pageCount: number;
  publisher: string;
  coverUrl: string;
  tags: string[];
}

export interface CreateBookResponse {
  bookId: string;
  isbn: string;
  title: string;
  authors: AuthorDto[];
  synopsis?: string | null;
  pageCount?: number | null;
  publisher?: string | null;
  coverUrl?: string | null;
  averageRating: number;
  ratingsCount: number;
  tags: TagDto[];
  createdByUserId?: string | null;
  createdAt: string;
}
