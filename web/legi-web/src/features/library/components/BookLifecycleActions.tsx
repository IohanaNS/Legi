import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Check, LoaderCircle, MoreHorizontal, Star, Trash2 } from "lucide-react";
import { Button } from "../../../components/ui/Button";
import { StarRating } from "../../../components/ui/StarRating";
import {
  useRateBook,
  useRemoveBook,
  useRemoveRating,
  useUpdateBookStatus,
} from "../hooks/useBookLifecycle";
import { statusI18nKey } from "../lib/mappers";
import type { BackendReadingStatus, UserBookDto } from "../types";

const STATUSES: BackendReadingStatus[] = [
  "NotStarted",
  "Reading",
  "Finished",
  "Paused",
  "Abandoned",
];

interface BookLifecycleActionsProps {
  userBook: UserBookDto;
}

export function BookLifecycleActions({ userBook }: BookLifecycleActionsProps) {
  const { t } = useTranslation();
  const updateStatus = useUpdateBookStatus();
  const rateBook = useRateBook();
  const removeRating = useRemoveRating();
  const removeBook = useRemoveBook();
  const [isOpen, setIsOpen] = useState(false);
  const [ratingValue, setRatingValue] = useState(userBook.ratingStars ?? 0);
  const [errorKey, setErrorKey] = useState<string | null>(null);

  const isPending =
    updateStatus.isPending || rateBook.isPending || removeRating.isPending || removeBook.isPending;

  const handleStatusChange = async (status: BackendReadingStatus) => {
    if (status === userBook.status) return;

    setErrorKey(null);
    try {
      await updateStatus.mutateAsync({ userBookId: userBook.userBookId, status });
      setIsOpen(false);
    } catch {
      setErrorKey("libraryActions.statusError");
    }
  };

  const handleSaveRating = async () => {
    const currentRating = userBook.ratingStars ?? 0;
    if (ratingValue === currentRating) {
      setIsOpen(false);
      return;
    }

    setErrorKey(null);
    try {
      if (ratingValue === 0) {
        if (userBook.ratingStars != null) {
          await removeRating.mutateAsync({ userBookId: userBook.userBookId });
        }
      } else {
        await rateBook.mutateAsync({ userBookId: userBook.userBookId, stars: ratingValue });
      }
      setIsOpen(false);
    } catch {
      setErrorKey("libraryActions.ratingError");
    }
  };

  const handleRemoveBook = async () => {
    const confirmed = window.confirm(
      t("libraryActions.confirmRemove", { title: userBook.book.title }),
    );
    if (!confirmed) return;

    setErrorKey(null);
    try {
      await removeBook.mutateAsync({ userBookId: userBook.userBookId });
      setIsOpen(false);
    } catch {
      setErrorKey("libraryActions.removeError");
    }
  };

  return (
    <div className="relative" onClick={(event) => event.stopPropagation()}>
      <button
        type="button"
        aria-label={t("libraryActions.openMenu")}
        onClick={() => {
          setErrorKey(null);
          if (!isOpen) setRatingValue(userBook.ratingStars ?? 0);
          setIsOpen(!isOpen);
        }}
        className="flex h-8 w-8 items-center justify-center rounded-full bg-white/90 text-stone-700 shadow-sm ring-1 ring-stone-200 transition-colors hover:bg-white"
        disabled={isPending}
      >
        {isPending ? <LoaderCircle size={16} className="animate-spin" /> : <MoreHorizontal size={17} />}
      </button>

      {isOpen && (
        <div className="absolute right-0 top-9 z-20 w-64 rounded-lg border border-stone-200 bg-white p-3 shadow-lg">
          <section>
            <p className="mb-2 text-xs font-semibold uppercase text-stone-500">
              {t("libraryActions.changeStatus")}
            </p>
            <div className="space-y-1">
              {STATUSES.map((status) => {
                const isCurrent = status === userBook.status;

                return (
                  <button
                    key={status}
                    type="button"
                    onClick={() => handleStatusChange(status)}
                    disabled={isPending || isCurrent}
                    className="flex w-full items-center justify-between rounded-md px-2 py-1.5 text-left text-sm text-stone-700 hover:bg-stone-50 disabled:cursor-default disabled:text-stone-400"
                  >
                    {t(`profile.status.${statusI18nKey(status)}`)}
                    {isCurrent && <Check size={14} />}
                  </button>
                );
              })}
            </div>
          </section>

          <section className="mt-3 border-t border-stone-100 pt-3">
            <div className="mb-2 flex items-center justify-between gap-2">
              <p className="text-xs font-semibold uppercase text-stone-500">
                {t("libraryActions.rate")}
              </p>
              <span className="text-xs font-medium text-stone-600">
                {ratingValue === 0
                  ? t("libraryActions.noRating")
                  : t("libraryActions.ratingValue", { rating: ratingValue.toFixed(1) })}
              </span>
            </div>

            <div className="mb-2 min-h-5">
              {ratingValue > 0 && <StarRating rating={ratingValue} size={13} showValue={false} />}
            </div>

            <input
              type="range"
              min={0}
              max={5}
              step={0.5}
              value={ratingValue}
              onChange={(event) => setRatingValue(Number(event.target.value))}
              disabled={isPending}
              aria-label={t("libraryActions.rate")}
              className="w-full accent-green-700"
            />

            <Button
              type="button"
              size="sm"
              variant="outline"
              className="mt-2 w-full"
              onClick={handleSaveRating}
              disabled={isPending}
            >
              <Star size={14} />
              {ratingValue === 0 && userBook.ratingStars != null
                ? t("libraryActions.removeRating")
                : t("libraryActions.saveRating")}
            </Button>
          </section>

          <section className="mt-3 border-t border-stone-100 pt-3">
            <Button
              type="button"
              size="sm"
              variant="danger"
              className="w-full"
              onClick={handleRemoveBook}
              disabled={isPending}
            >
              <Trash2 size={14} />
              {t("libraryActions.removeFromLibrary")}
            </Button>
          </section>

          {errorKey && <p className="mt-2 text-xs text-red-600">{t(errorKey)}</p>}
        </div>
      )}
    </div>
  );
}
