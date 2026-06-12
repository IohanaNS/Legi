import { StarRating } from "./StarRating";
import { Badge } from "./Badge";
import { BookCover } from "./BookCover";
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
  const interactive = !!onClick;

  if (variant === "horizontal") {
    return (
      <div
        className={cn("flex gap-3 group", interactive && "cursor-pointer", className)}
        onClick={onClick}
      >
        <BookCover
          title={title}
          author={author}
          coverUrl={coverUrl}
          className="w-16 h-22 rounded-lg flex-shrink-0"
        />
        <div className="flex-1 min-w-0">
          <p
            className={cn(
              "text-sm font-medium text-stone-800 truncate",
              interactive && "group-hover:text-green-700 transition-colors",
            )}
          >
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
      className={cn("group", interactive && "cursor-pointer", className)}
      onClick={onClick}
    >
      <BookCover
        title={title}
        author={author}
        coverUrl={coverUrl}
        className="aspect-[2/3] h-auto rounded-lg mb-2"
      />
      <h3
        className={cn(
          "text-sm font-medium text-stone-800 truncate",
          interactive && "group-hover:text-green-700 transition-colors",
        )}
      >
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
