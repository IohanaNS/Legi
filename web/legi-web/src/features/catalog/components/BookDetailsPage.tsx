import { useState } from "react";
import { useParams, useNavigate, useLocation } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { ArrowLeft, BookOpen, CheckCircle2, MessageSquare, PenLine } from "lucide-react";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import { StarRating } from "../../../components/ui/StarRating";
import { StarRatingInput } from "../../../components/ui/StarRatingInput";
import { useBookDetails } from "../hooks/useBookDetails";
import {
  useMyUserBookByBook,
  useRateBook,
} from "../../library/hooks/useBookLibraryState";
import { useAddToLibrary } from "../hooks/useAddToLibrary";
import {
  useMarkBookAsRead,
  useUpdateBookStatus,
} from "../../library/hooks/useBookLifecycle";
import { FinishDatePicker } from "../../library/components/FinishDatePicker";
import { UpdateProgressModal } from "../../library/components/UpdateProgressModal";
import { ReviewForm } from "../../library/components/ReviewForm";
import { useBookReviews } from "../../social/hooks/useBookReviews";
import { ReviewCard } from "../../social/components/ReviewCard";
import { feedKeys } from "../../social/queryKeys";
import {
  formatFinishDate,
  progressPercent,
  statusI18nKey,
  statusVariant,
} from "../../library/lib/mappers";

const SYNOPSIS_CLAMP = 280;
type BookNotice = "created" | "alreadyExists";

export default function BookDetailsPage() {
  const { bookId } = useParams<{ bookId: string }>();
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const bookNotice = (location.state as { bookNotice?: BookNotice } | null)?.bookNotice;

  const { data: book, isLoading, isError } = useBookDetails(bookId);
  const { data: userBook } = useMyUserBookByBook(bookId);
  const reviewsQuery = useBookReviews(bookId);

  const addToLibrary = useAddToLibrary();
  const rateBook = useRateBook(bookId ?? "", userBook?.userBookId);
  const markAsRead = useMarkBookAsRead();
  const updateStatus = useUpdateBookStatus();

  const [showProgress, setShowProgress] = useState(false);
  const [showReviewForm, setShowReviewForm] = useState(false);
  const [synopsisExpanded, setSynopsisExpanded] = useState(false);
  const [finishMode, setFinishMode] = useState<"edit" | "mark" | null>(null);
  const [finishError, setFinishError] = useState(false);

  if (isLoading) {
    return <p className="p-8 text-center text-stone-400">{t("common.loadMore")}…</p>;
  }
  if (isError || !book) {
    return <p className="p-8 text-center text-red-600">{t("common.couldNotLoad")}</p>;
  }

  const authors = book.authors.map((a) => a.name).join(", ") || t("explore.unknownAuthor");
  const reviews = reviewsQuery.data?.pages.flatMap((p) => p.items) ?? [];
  const reviewsCount = reviewsQuery.data?.pages[0]?.totalItems ?? book.reviewsCount;

  const longSynopsis = (book.synopsis?.length ?? 0) > SYNOPSIS_CLAMP;
  const synopsisText =
    longSynopsis && !synopsisExpanded ? `${book.synopsis!.slice(0, SYNOPSIS_CLAMP)}…` : book.synopsis;

  const percent =
    userBook != null
      ? progressPercent(userBook.progressValue, userBook.progressType, book.pageCount)
      : null;

  const handleMarkAsRead = async (finishedReadingAt: string | null) => {
    setFinishError(false);
    try {
      await markAsRead.mutateAsync({ bookId: book.id, finishedReadingAt });
      setFinishMode(null);
    } catch {
      setFinishError(true);
    }
  };

  const handleEditFinishDate = async (finishedReadingAt: string | null) => {
    if (!userBook) return;
    setFinishError(false);
    try {
      await updateStatus.mutateAsync({
        userBookId: userBook.userBookId,
        status: "Finished",
        finishedReadingAt,
      });
      setFinishMode(null);
    } catch {
      setFinishError(true);
    }
  };

  return (
    <div className="mx-auto max-w-5xl px-4 py-6">
      <button
        type="button"
        onClick={() => navigate(-1)}
        className="mb-6 flex items-center gap-2 text-sm text-stone-500 dark:text-stone-400 hover:text-stone-700 dark:hover:text-stone-200"
      >
        <ArrowLeft size={16} />
        {t("bookDetails.back")}
      </button>

      {bookNotice && (
        <div className="mb-6 flex items-center gap-2 rounded-lg border border-green-200 bg-green-50 px-3 py-2 text-sm text-green-800 dark:border-green-900/60 dark:bg-green-950/30 dark:text-green-100">
          <CheckCircle2 size={16} className="shrink-0" />
          {t(`bookDetails.notice.${bookNotice}`)}
        </div>
      )}

      <div className="grid gap-8 md:grid-cols-[220px_1fr]">
        {/* Left column: cover + actions */}
        <div className="space-y-4">
          <div className="aspect-[2/3] overflow-hidden rounded-xl bg-stone-200 dark:bg-dark-raised">
            {book.coverUrl ? (
              <img src={book.coverUrl} alt={book.title} className="h-full w-full object-cover" />
            ) : (
              <div className="flex h-full w-full items-center justify-center text-stone-400">
                <BookOpen size={40} />
              </div>
            )}
          </div>

          {userBook ? (
            <>
              <Badge variant={statusVariant(userBook.status)} className="w-full justify-center py-1.5">
                {t(`profile.status.${statusI18nKey(userBook.status)}`)}
              </Badge>
              <button
                type="button"
                onClick={() => setShowProgress(true)}
                className="w-full text-center text-sm font-medium text-green-700 hover:underline"
              >
                {t("bookDetails.updateProgress")}
                {percent != null ? ` (${percent}%)` : ""}
              </button>

              {userBook.status === "Finished" &&
                (finishMode === "edit" ? (
                  <FinishDatePicker
                    initialDate={userBook.finishedReadingAt ?? null}
                    isPending={updateStatus.isPending}
                    errorText={finishError ? t("finishDate.error") : null}
                    onConfirm={handleEditFinishDate}
                    onCancel={() => setFinishMode(null)}
                  />
                ) : (
                  <div className="flex items-center justify-between gap-2 text-xs text-stone-500 dark:text-stone-400">
                    <span>
                      {userBook.finishedReadingAt
                        ? t("finishDate.finishedOn", {
                            date: formatFinishDate(userBook.finishedReadingAt, i18n.language),
                          })
                        : t("finishDate.finishedUnknown")}
                    </span>
                    <button
                      type="button"
                      onClick={() => setFinishMode("edit")}
                      className="font-medium text-green-700 hover:underline"
                    >
                      {t("finishDate.edit")}
                    </button>
                  </div>
                ))}
            </>
          ) : finishMode === "mark" ? (
            <FinishDatePicker
              defaultUnknown
              isPending={markAsRead.isPending}
              errorText={finishError ? t("finishDate.error") : null}
              onConfirm={handleMarkAsRead}
              onCancel={() => setFinishMode(null)}
            />
          ) : (
            <>
              <Button
                className="w-full"
                disabled={addToLibrary.isPending}
                onClick={() => addToLibrary.mutate({ bookId: book.id, wishlist: false })}
              >
                {t("explore.addToLibrary")}
              </Button>
              <Button
                variant="outline"
                className="w-full"
                disabled={markAsRead.isPending}
                onClick={() => setFinishMode("mark")}
              >
                {t("finishDate.markAsRead")}
              </Button>
            </>
          )}

          {/* Your rating */}
          <div>
            <p className="mb-1 text-sm text-stone-500 dark:text-stone-400">
              {t("bookDetails.yourRating")}
            </p>
            <StarRatingInput
              value={userBook?.ratingStars ?? 0}
              onChange={(stars) => rateBook.mutate(stars)}
              size={24}
              disabled={!userBook || rateBook.isPending}
            />
            {!userBook && (
              <p className="mt-1 text-xs text-stone-400">{t("bookDetails.ratingNeedsLibrary")}</p>
            )}
          </div>
        </div>

        {/* Right column: details + reviews */}
        <div className="min-w-0">
          <h1 className="font-serif text-3xl font-semibold leading-tight text-stone-800 dark:text-stone-100">
            {book.title}
          </h1>
          <p className="mt-1 text-lg text-stone-500 dark:text-stone-400">{authors}</p>

          <div className="mt-3 flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-stone-500 dark:text-stone-400">
            <span className="flex items-center gap-1">
              <StarRating rating={Number(book.averageRating)} showValue size={16} />
            </span>
            {book.pageCount != null && <span>{t("bookDetails.pages", { count: book.pageCount })}</span>}
          </div>

          {book.publisher && (
            <p className="mt-3 text-sm text-stone-500 dark:text-stone-400">
              <span className="font-medium text-stone-600 dark:text-stone-300">
                {t("bookDetails.publisher")}:
              </span>{" "}
              {book.publisher}
            </p>
          )}

          {book.tags.length > 0 && (
            <div className="mt-3 flex flex-wrap gap-2">
              {book.tags.map((tag) => (
                <Badge key={tag.slug} variant="secondary">
                  {tag.name}
                </Badge>
              ))}
            </div>
          )}

          {book.synopsis && (
            <section className="mt-6">
              <h2 className="mb-2 text-lg font-semibold text-stone-800 dark:text-stone-100">
                {t("bookDetails.synopsis")}
              </h2>
              <p className="whitespace-pre-wrap text-sm leading-relaxed text-stone-600 dark:text-stone-300">
                {synopsisText}
              </p>
              {longSynopsis && (
                <button
                  type="button"
                  onClick={() => setSynopsisExpanded((v) => !v)}
                  className="mt-1 text-sm font-medium text-green-700 hover:underline"
                >
                  {t(synopsisExpanded ? "bookDetails.readLess" : "bookDetails.readMore")}
                </button>
              )}
            </section>
          )}

          {/* Reviews */}
          <section className="mt-8">
            <div className="mb-4 flex items-center justify-between">
              <h2 className="flex items-center gap-2 text-lg font-semibold text-stone-800 dark:text-stone-100">
                <MessageSquare size={18} />
                {t("bookDetails.reviews")} ({reviewsCount})
              </h2>
              {userBook && !showReviewForm && (
                <Button size="sm" onClick={() => setShowReviewForm(true)}>
                  <PenLine size={14} />
                  {t("bookDetails.writeReview")}
                </Button>
              )}
            </div>

            {showReviewForm && userBook && (
              <div className="mb-4">
                <ReviewForm
                  bookId={book.id}
                  bookTitle={book.title}
                  userBookId={userBook.userBookId}
                  onClose={() => setShowReviewForm(false)}
                />
              </div>
            )}

            {reviews.length === 0 ? (
              <p className="rounded-xl border border-dashed border-stone-200 dark:border-dark-raised p-8 text-center text-sm text-stone-400">
                {t("bookDetails.noReviews")}
              </p>
            ) : (
              <div className="space-y-4">
                {reviews.map((item) => (
                  <ReviewCard key={item.id} item={item} listKey={feedKeys.bookReviews(book.id)} />
                ))}
                {reviewsQuery.hasNextPage && (
                  <Button
                    variant="outline"
                    className="w-full"
                    disabled={reviewsQuery.isFetchingNextPage}
                    onClick={() => reviewsQuery.fetchNextPage()}
                  >
                    {t("common.loadMore")}
                  </Button>
                )}
              </div>
            )}
          </section>
        </div>
      </div>

      {showProgress && userBook && (
        <UpdateProgressModal userBook={userBook} onClose={() => setShowProgress(false)} />
      )}
    </div>
  );
}
