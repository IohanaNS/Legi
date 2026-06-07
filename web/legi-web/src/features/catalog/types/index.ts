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

export interface PaginationMetadata {
  currentPage: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface SearchBooksResponse {
  books: BookSummaryDto[];
  pagination: PaginationMetadata;
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
  tagSlug?: string;
  minRating?: number;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: BookSortBy;
  sortDescending?: boolean;
}

export type SortOption = "bestRated" | "mostRecent" | "mostPopular";
