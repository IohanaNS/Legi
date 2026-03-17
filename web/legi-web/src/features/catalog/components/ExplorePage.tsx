import { useState, useMemo } from "react";
import { useTranslation } from "react-i18next";
import { Sparkles } from "lucide-react";
import { BookCard } from "../../../components/ui/BookCard";
import { SearchBar } from "./SearchBar";
import { GenreFilter } from "./GenreFilter";
import { recommendedBooks, allBooks, genres } from "../data/mockCatalogData";
import type { SortOption } from "../types";

export default function ExplorePage() {
  const { t } = useTranslation();

  const [searchQuery, setSearchQuery] = useState("");
  const [selectedGenres, setSelectedGenres] = useState<string[]>([]);
  const [sortBy, setSortBy] = useState<SortOption>("best_rated");

  const handleToggleGenre = (genreId: string) => {
    setSelectedGenres((prev) =>
      prev.includes(genreId)
        ? prev.filter((id) => id !== genreId)
        : [...prev, genreId]
    );
  };

  const filteredBooks = useMemo(() => {
    let books = allBooks;

    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase();
      books = books.filter(
        (book) =>
          book.title.toLowerCase().includes(query) ||
          book.author.toLowerCase().includes(query) ||
          book.genres.some((g) => g.toLowerCase().includes(query))
      );
    }

    if (selectedGenres.length > 0) {
      const selectedGenreNames = selectedGenres.map(
        (id) => genres.find((g) => g.id === id)?.name ?? ""
      );
      books = books.filter((book) =>
        book.genres.some((g) => selectedGenreNames.includes(g))
      );
    }

    switch (sortBy) {
      case "best_rated":
        return [...books].sort((a, b) => b.rating - a.rating);
      case "most_recent":
        return [...books].reverse();
      case "most_popular":
        return [...books].sort((a, b) => b.rating - a.rating);
      default:
        return books;
    }
  }, [searchQuery, selectedGenres, sortBy]);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-stone-800">{t("explore.title")}</h1>
        <p className="text-stone-500 mt-1">{t("explore.subtitle")}</p>
      </div>

      {/* Busca */}
      <SearchBar value={searchQuery} onChange={setSearchQuery} />

      {/* Recomendados */}
      <div>
        <div className="flex items-center gap-2 text-sm font-medium text-stone-700 mb-3">
          <Sparkles size={16} />
          {t("explore.recommendedForYou")}
        </div>
        <div className="grid grid-cols-5 gap-4">
          {recommendedBooks.map((book) => (
            <BookCard
              key={book.id}
              title={book.title}
              author={book.author}
              coverUrl={book.coverUrl}
              rating={book.rating}
            />
          ))}
        </div>
      </div>

      {/* Filtros de gênero */}
      <GenreFilter
        genres={genres}
        selectedGenres={selectedGenres}
        onToggleGenre={handleToggleGenre}
      />

      {/* Resultados */}
      <div>
        <div className="flex items-center justify-between mb-4">
          <p className="text-sm text-stone-600">
            {t("explore.booksFound", { count: filteredBooks.length })}
          </p>
          <select
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value as SortOption)}
            className="text-sm border border-stone-200 rounded-lg px-3 py-1.5 text-stone-700 bg-white focus:outline-none focus:ring-2 focus:ring-green-600/20 cursor-pointer"
          >
            <option value="best_rated">{t("explore.sortBy.bestRated")}</option>
            <option value="most_recent">{t("explore.sortBy.mostRecent")}</option>
            <option value="most_popular">{t("explore.sortBy.mostPopular")}</option>
          </select>
        </div>

        <div className="grid grid-cols-5 gap-4">
          {filteredBooks.map((book) => (
            <BookCard
              key={book.id}
              title={book.title}
              author={book.author}
              coverUrl={book.coverUrl}
              rating={book.rating}
              genres={book.genres}
              showGenres
            />
          ))}
        </div>
      </div>
    </div>
  );
}