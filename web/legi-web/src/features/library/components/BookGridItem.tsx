import { useTranslation } from "react-i18next";
import { StarRating } from "../../../components/ui/StarRating";
import { Badge } from "../../../components/ui/Badge";
import { ProgressBar } from "../../../components/ui/ProgressBar";
import { progressPercent, statusI18nKey, statusVariant } from "../lib/mappers";
import type { UserBookDto } from "../types";

interface BookGridItemProps {
  userBook: UserBookDto;
}

export function BookGridItem({ userBook }: BookGridItemProps) {
  const { t } = useTranslation();
  const { book, status, ratingStars } = userBook;

  const percent =
    status === "Reading"
      ? progressPercent(userBook.progressValue, userBook.progressType, book.pageCount)
      : null;

  return (
    <div className="cursor-pointer group">
      <div className="relative aspect-[2/3] bg-stone-200 rounded-lg overflow-hidden mb-2">
        {book.coverUrl && (
          <img src={book.coverUrl} alt={book.title} className="w-full h-full object-cover" />
        )}

        <div className="absolute top-2 left-2">
          <Badge variant={statusVariant(status)}>
            {t(`profile.status.${statusI18nKey(status)}`)}
          </Badge>
        </div>

        {percent !== null && (
          <div className="absolute bottom-0 left-0 right-0 bg-black/60 px-2 py-1.5">
            <span className="text-white text-xs font-medium">{percent}%</span>
            <ProgressBar value={percent} size="sm" className="mt-1" />
          </div>
        )}
      </div>

      <h3 className="text-sm font-medium text-stone-800 truncate group-hover:text-green-700 transition-colors">
        {book.title}
      </h3>
      <p className="text-xs text-stone-500">{book.authorDisplay}</p>
      {ratingStars != null && <StarRating rating={ratingStars} size={12} className="mt-1" />}
    </div>
  );
}
