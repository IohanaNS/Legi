import { useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { AlertTriangle, X } from "lucide-react";
import { Button } from "../../../components/ui/Button";
import { StarRatingInput } from "../../../components/ui/StarRatingInput";
import { useCreateReview } from "../hooks/useBookLibraryState";

const MIN_CONTENT = 20;
const MAX_CONTENT = 2000;

interface ReviewFormProps {
  bookId: string;
  bookTitle: string;
  userBookId: string | undefined;
  onClose: () => void;
}

/**
 * Inline review composer shown on the book details page: star rating + text +
 * spoiler toggle. Mirrors the reference design's "Escrever resenha" card.
 */
export function ReviewForm({ bookId, bookTitle, userBookId, onClose }: ReviewFormProps) {
  const { t } = useTranslation();
  const createReview = useCreateReview(bookId, userBookId);

  const [stars, setStars] = useState(0);
  const [content, setContent] = useState("");
  const [isSpoiler, setIsSpoiler] = useState(false);

  const trimmedLength = content.trim().length;
  const remaining = MIN_CONTENT - trimmedLength;
  const canSubmit = stars > 0 && trimmedLength >= MIN_CONTENT && !createReview.isPending;

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    if (!canSubmit) return;
    createReview.mutate(
      { content: content.trim(), stars, isSpoiler },
      { onSuccess: onClose },
    );
  };

  return (
    <form
      onSubmit={handleSubmit}
      className="rounded-xl border border-stone-200 dark:border-dark-raised bg-white dark:bg-dark-card p-4"
    >
      <div className="mb-3 flex items-start justify-between">
        <div>
          <h3 className="font-semibold text-stone-800 dark:text-stone-100">
            {t("bookDetails.writeReview")}
          </h3>
          <p className="text-sm text-stone-500 dark:text-stone-400">{bookTitle}</p>
        </div>
        <button
          type="button"
          onClick={onClose}
          className="text-stone-400 dark:text-stone-500 hover:text-stone-600 dark:hover:text-stone-300"
        >
          <X size={18} />
        </button>
      </div>

      <label className="mb-1 block text-sm text-stone-600 dark:text-stone-300">
        {t("bookDetails.yourRating")} <span className="text-red-500">*</span>
      </label>
      <StarRatingInput value={stars} onChange={setStars} className="mb-4" />

      <label className="mb-1 block text-sm text-stone-600 dark:text-stone-300">
        {t("bookDetails.reviewLabel")} <span className="text-red-500">*</span>{" "}
        <span className="text-xs text-stone-400">
          {t("bookDetails.reviewMinChars", { count: MIN_CONTENT })}
        </span>
      </label>
      <textarea
        value={content}
        onChange={(e) => setContent(e.target.value)}
        maxLength={MAX_CONTENT}
        rows={5}
        placeholder={t("bookDetails.reviewPlaceholder")}
        className="min-h-32 w-full resize-y rounded-lg border border-stone-300 dark:border-dark-raised bg-white dark:bg-dark-raised text-stone-800 dark:text-stone-100 px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-green-600"
      />
      <div className="mt-1 flex items-center justify-between text-xs">
        <span className={trimmedLength > 0 && remaining > 0 ? "text-red-500" : "text-stone-400"}>
          {t("bookDetails.reviewCharCount", { count: content.length, max: MAX_CONTENT })}
        </span>
        {remaining > 0 && (
          <span className="text-stone-400">{t("bookDetails.reviewRemaining", { count: remaining })}</span>
        )}
      </div>

      <button
        type="button"
        role="switch"
        aria-checked={isSpoiler}
        onClick={() => setIsSpoiler((current) => !current)}
        className="mt-3 flex items-center gap-2 text-sm text-stone-600 dark:text-stone-300"
      >
        <span
          className={`flex h-4 w-4 items-center justify-center rounded border ${
            isSpoiler
              ? "border-green-700 bg-green-700 text-white"
              : "border-stone-300 dark:border-stone-600"
          }`}
        >
          {isSpoiler && "✓"}
        </span>
        <AlertTriangle size={14} className="text-amber-500" />
        {t("bookDetails.reviewSpoiler")}
      </button>

      {createReview.isError && (
        <p className="mt-2 text-xs text-red-600">{t("bookDetails.reviewError")}</p>
      )}

      <div className="mt-4 flex gap-2">
        <Button type="button" variant="outline" onClick={onClose} className="flex-1">
          {t("common.cancel")}
        </Button>
        <Button type="submit" disabled={!canSubmit} className="flex-1">
          {t("bookDetails.publishReview")}
        </Button>
      </div>
    </form>
  );
}
