import { useTranslation } from "react-i18next";
import { StarRating } from "../../../components/ui/StarRating";
import { Badge } from "../../../components/ui/Badge";
import { ProgressBar } from "../../../components/ui/ProgressBar";
import type { UserBook } from "../types";

interface BookGridItemProps {
  book: UserBook;
}

export function BookGridItem({ book }: BookGridItemProps) {
  const { t } = useTranslation();

  const statusVariant = {
    not_started: "secondary" as const,
    reading: "primary" as const,
    finished: "success" as const,
    abandoned: "danger" as const,
    paused: "warning" as const,
  };

  return (
    <div className="cursor-pointer group">
      <div className="relative aspect-[2/3] bg-stone-200 rounded-lg overflow-hidden mb-2">
        {book.coverUrl && (
          <img src={book.coverUrl} alt={book.title} className="w-full h-full object-cover" />
        )}

        <div className="absolute top-2 left-2">
          <Badge variant={statusVariant[book.status]}>
            {t(`profile.status.${book.status}`)}
          </Badge>
        </div>

        {book.status === "reading" && book.progress !== undefined && (
          <div className="absolute bottom-0 left-0 right-0 bg-black/60 px-2 py-1.5">
            <span className="text-white text-xs font-medium">{book.progress}%</span>
            <ProgressBar value={book.progress} size="sm" className="mt-1" />
          </div>
        )}
      </div>

      <h3 className="text-sm font-medium text-stone-800 truncate group-hover:text-green-700 transition-colors">
        {book.title}
      </h3>
      <p className="text-xs text-stone-500">{book.author}</p>
      {book.rating && <StarRating rating={book.rating} size={12} className="mt-1" />}
    </div>
  );
}