import { useState } from "react";
import { useTranslation } from "react-i18next";
import { AlertCircle, BookOpen, BookPlus, Check, Gift, LoaderCircle } from "lucide-react";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { StarRating } from "../../../components/ui/StarRating";
import {
  isAlreadyInLibrary,
  isMissingLibrarySnapshot,
  useAddToLibrary,
} from "../hooks/useAddToLibrary";
import type { BookSummaryDto } from "../types";

interface BookSummaryCardProps {
  book: BookSummaryDto;
}

type Feedback = "added" | "alreadyInLibrary" | "missingSnapshot" | "addError";
type ActionTarget = "library" | "wishlist";

export function BookSummaryCard({ book }: BookSummaryCardProps) {
  const { t } = useTranslation();
  const addToLibrary = useAddToLibrary();
  const [feedback, setFeedback] = useState<Feedback | null>(null);
  const [pendingTarget, setPendingTarget] = useState<ActionTarget | null>(null);
  const [coverFailed, setCoverFailed] = useState(false);

  const authors = book.authors.map((author) => author.name).join(", ") || t("explore.unknownAuthor");
  const isKnownPresent = feedback === "added" || feedback === "alreadyInLibrary";

  const handleAdd = async (target: ActionTarget) => {
    setPendingTarget(target);
    setFeedback(null);

    try {
      await addToLibrary.mutateAsync({
        bookId: book.id,
        wishlist: target === "wishlist",
      });
      setFeedback("added");
    } catch (error) {
      if (isAlreadyInLibrary(error)) {
        setFeedback("alreadyInLibrary");
      } else if (isMissingLibrarySnapshot(error)) {
        setFeedback("missingSnapshot");
      } else {
        setFeedback("addError");
      }
    } finally {
      setPendingTarget(null);
    }
  };

  return (
    <article className="flex h-full flex-col rounded-lg border border-stone-200 dark:border-dark-raised bg-white dark:bg-dark-card p-3">
      <div className="mb-3 aspect-[2/3] overflow-hidden rounded-lg bg-stone-200 dark:bg-dark-raised">
        {book.coverUrl && !coverFailed ? (
          <img
            src={book.coverUrl}
            alt={book.title}
            className="h-full w-full object-cover"
            onError={() => setCoverFailed(true)}
          />
        ) : (
          <div className="flex h-full w-full items-center justify-center text-stone-400 dark:text-stone-500">
            <BookOpen size={34} />
          </div>
        )}
      </div>

      <div className="flex flex-1 flex-col">
        <h3 className="line-clamp-2 min-h-10 text-sm font-medium text-stone-800 dark:text-stone-100">
          {book.title}
        </h3>
        <p className="mt-1 truncate text-xs text-stone-500 dark:text-stone-400">{authors}</p>

        <div className="mt-2 flex flex-wrap items-center gap-x-2 gap-y-1">
          <StarRating rating={Number(book.averageRating)} size={12} />
          <span className="text-xs text-stone-500 dark:text-stone-400">
            {t("explore.ratingsCount", { count: book.ratingsCount })}
          </span>
        </div>

        {book.tags.length > 0 && (
          <div className="mt-2 flex min-h-6 flex-wrap gap-1">
            {book.tags.slice(0, 3).map((tag) => (
              <Badge key={tag.slug} variant="secondary" className="px-1.5 py-0 text-[10px]">
                {tag.name}
              </Badge>
            ))}
          </div>
        )}

        <div className="mt-3 grid gap-2">
          <Button
            size="sm"
            onClick={() => handleAdd("library")}
            disabled={addToLibrary.isPending || isKnownPresent}
            className="w-full"
          >
            {pendingTarget === "library" ? (
              <LoaderCircle size={14} className="animate-spin" />
            ) : (
              <BookPlus size={14} />
            )}
            {t("explore.addToLibrary")}
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => handleAdd("wishlist")}
            disabled={addToLibrary.isPending || isKnownPresent}
            className="w-full"
          >
            {pendingTarget === "wishlist" ? (
              <LoaderCircle size={14} className="animate-spin" />
            ) : (
              <Gift size={14} />
            )}
            {t("explore.addToWishlist")}
          </Button>
        </div>

        {feedback && (
          <p className="mt-2 flex min-h-4 items-center gap-1 text-xs text-stone-500">
            {feedback === "added" ? (
              <Check size={13} className="text-green-700" />
            ) : (
              <AlertCircle size={13} className="text-amber-600" />
            )}
            {t(`explore.${feedback}`)}
          </p>
        )}
      </div>
    </article>
  );
}
