export interface Book {
  id: string;
  title: string;
  author: string;
  coverUrl?: string;
  rating: number;
  genres: string[];
  description?: string;
  pageCount?: number;
}

export interface Genre {
  id: string;
  name: string;
  nameKey: string;
}

export type SortOption = "best_rated" | "most_recent" | "most_popular";