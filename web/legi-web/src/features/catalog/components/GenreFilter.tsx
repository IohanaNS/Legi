import { useTranslation } from "react-i18next";
import { Filter } from "lucide-react";
import { cn } from "../../../lib/utils";
import type { Genre } from "../types";

interface GenreFilterProps {
  genres: Genre[];
  selectedGenres: string[];
  onToggleGenre: (genreId: string) => void;
}

export function GenreFilter({ genres, selectedGenres, onToggleGenre }: GenreFilterProps) {
  const { t } = useTranslation();

  return (
    <div>
      <div className="flex items-center gap-2 text-sm text-stone-600 mb-3">
        <Filter size={16} />
        {t("explore.filterByGenre")}
      </div>
      <div className="flex flex-wrap gap-2">
        {genres.map((genre) => {
          const isSelected = selectedGenres.includes(genre.id);
          return (
            <button
              key={genre.id}
              onClick={() => onToggleGenre(genre.id)}
              className={cn(
                "px-3 py-1.5 rounded-full text-sm border transition-colors cursor-pointer",
                isSelected
                  ? "bg-green-700 text-white border-green-700"
                  : "bg-white text-stone-600 border-stone-300 hover:border-stone-400"
              )}
            >
              {t(genre.nameKey)}
            </button>
          );
        })}
      </div>
    </div>
  );
}