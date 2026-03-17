import { StarRating } from "./StarRating";
import { Badge } from "./Badge";
import { cn } from "../../lib/utils";

interface BookCardProps {
  title: string;
  author: string;
  coverUrl?: string;
  rating?: number;
  genres?: string[];
  variant?: "vertical" | "horizontal";
  showGenres?: boolean;
  className?: string;
  onClick?: () => void;
}

export function BookCard({
  title,
  author,
  coverUrl,
  rating,
  genres,
  variant = "vertical",
  showGenres = false,
  className,
  onClick,
}: BookCardProps) {
  if (variant === "horizontal") {
    return (
      <div
        className={cn("flex gap-3 cursor-pointer group", className)}
        onClick={onClick}
      >
        <div className="w-16 h-22 bg-stone-200 rounded-lg flex-shrink-0 overflow-hidden">
          {coverUrl && (
            <img src={coverUrl} alt={title} className="w-full h-full object-cover" />
          )}
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium text-stone-800 truncate group-hover:text-green-700 transition-colors">
            {title}
          </p>
          <p className="text-xs text-stone-500">{author}</p>
          {rating !== undefined && <StarRating rating={rating} size={12} className="mt-1" />}
        </div>
      </div>
    );
  }

  return (
    <div
      className={cn("cursor-pointer group", className)}
      onClick={onClick}
    >
      <div className="aspect-[2/3] bg-stone-200 rounded-lg overflow-hidden mb-2">
        {coverUrl && (
          <img src={coverUrl} alt={title} className="w-full h-full object-cover" />
        )}
      </div>
      <h3 className="text-sm font-medium text-stone-800 truncate group-hover:text-green-700 transition-colors">
        {title}
      </h3>
      <p className="text-xs text-stone-500">{author}</p>
      {rating !== undefined && <StarRating rating={rating} size={12} className="mt-1" />}
      {showGenres && genres && genres.length > 0 && (
        <div className="flex flex-wrap gap-1 mt-1.5">
          {genres.map((genre) => (
            <Badge key={genre} variant="secondary" className="text-[10px] px-1.5 py-0">
              {genre}
            </Badge>
          ))}
        </div>
      )}
    </div>
  );
}