import type { BookSortBy, SortOption } from "../types";

export const sortOptionToBackend: Record<
  SortOption,
  { sortBy: BookSortBy; sortDescending: boolean }
> = {
  bestRated: { sortBy: "AverageRating", sortDescending: true },
  mostRecent: { sortBy: "CreatedAt", sortDescending: true },
  mostPopular: { sortBy: "RatingsCount", sortDescending: true },
};
