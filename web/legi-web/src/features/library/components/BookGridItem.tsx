import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { StarRating } from "../../../components/ui/StarRating";
import { BookCover } from "../../../components/ui/BookCover";
import { Badge } from "../../../components/ui/Badge";
import { ProgressBar } from "../../../components/ui/ProgressBar";
import { BookLifecycleActions } from "./BookLifecycleActions";
import { progressPercent, statusI18nKey, statusVariant } from "../lib/mappers";
import type { UserBookDto } from "../types";

interface BookGridItemProps {
  userBook: UserBookDto;
  editable?: boolean;
}

export function BookGridItem({ userBook, editable = false }: BookGridItemProps) {
  const { t } = useTranslation();
  const { book, status, ratingStars } = userBook;

  const percent =
    status === "Reading"
      ? progressPercent(userBook.progressValue, userBook.progressType, book.pageCount)
      : null;

  return (
    <div className="group">
      <div className="relative mb-2">
        <Link
          to={`/books/${book.bookId}`}
          className="relative block aspect-[2/3] overflow-hidden rounded-lg bg-stone-200 dark:bg-dark-raised"
        >
          <BookCover title={book.title} author={book.authorDisplay} coverUrl={book.coverUrl} />

          <div className="absolute left-2 top-2">
            <Badge variant={statusVariant(status)}>
              {t(`profile.status.${statusI18nKey(status)}`)}
            </Badge>
          </div>

          {percent !== null && (
            <div className="absolute bottom-0 left-0 right-0 bg-black/60 px-2 py-1.5">
              <span className="text-xs font-medium text-white">{percent}%</span>
              <ProgressBar value={percent} size="sm" className="mt-1" />
            </div>
          )}
        </Link>

        {editable && (
          <div className="absolute right-2 top-2">
            <BookLifecycleActions userBook={userBook} />
          </div>
        )}
      </div>

      <Link
        to={`/books/${book.bookId}`}
        className="block truncate text-sm font-medium text-stone-800 transition-colors group-hover:text-green-700 dark:text-stone-100 dark:group-hover:text-green-400"
      >
        {book.title}
      </Link>
      <p className="truncate text-xs text-stone-500 dark:text-stone-400">{book.authorDisplay}</p>
      {ratingStars != null && <StarRating rating={ratingStars} size={12} className="mt-1" />}
    </div>
  );
}
