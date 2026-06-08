import { useMemo, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { BookOpen, EyeOff, RefreshCw } from "lucide-react";
import { Card } from "../../../components/ui/Card";
import { Button } from "../../../components/ui/Button";
import { ChangeBookModal } from "./ChangeBookModal";
import { useReadingBooks, useUpdateProgress } from "../../library/hooks/useReadingNow";
import { progressPercent } from "../../library/lib/mappers";
import type { ProgressType, UserBookDto } from "../../library/types";

export function ReadingNowCard() {
  const { t } = useTranslation();
  const { data: books, isLoading } = useReadingBooks();

  // Ephemeral selection: which in-progress book the card is showing.
  // Defaults to the most-recently-updated one; resets on reload.
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [showSwitcher, setShowSwitcher] = useState(false);

  const userBook = useMemo(() => {
    if (!books || books.length === 0) return null;
    return books.find((b) => b.userBookId === selectedId) ?? books[0];
  }, [books, selectedId]);

  // Hide entirely while loading or when the user has nothing in progress.
  if (isLoading || !userBook) return null;

  return (
    <>
      <Card>
        <div className="p-4">
          <div className="mb-3 flex items-center justify-between">
            <div className="flex items-center gap-2 text-sm font-medium text-stone-700 dark:text-stone-200">
              <BookOpen size={16} />
              {t("feed.readingNow")}
            </div>
            <Button variant="outline" size="sm" onClick={() => setShowSwitcher(true)}>
              <RefreshCw size={14} />
              {t("feed.changeBook")}
            </Button>
          </div>

          {/* Remount the form when the displayed book changes so inputs reset. */}
          <ReadingNowForm key={userBook.userBookId} userBook={userBook} />
        </div>
      </Card>

      {showSwitcher && (
        <ChangeBookModal
          currentUserBookId={userBook.userBookId}
          onSelect={setSelectedId}
          onClose={() => setShowSwitcher(false)}
        />
      )}
    </>
  );
}

function ReadingNowForm({ userBook }: { userBook: UserBookDto }) {
  const { t } = useTranslation();
  const updateProgress = useUpdateProgress();
  const { book } = userBook;

  const [type, setType] = useState<ProgressType>(userBook.progressType ?? "Percentage");
  const [value, setValue] = useState<number>(userBook.progressValue ?? 0);
  const [note, setNote] = useState("");
  const [isSpoiler, setIsSpoiler] = useState(false);

  const sliderMax = type === "Page" && book.pageCount ? book.pageCount : 100;
  const percent = progressPercent(value, type, book.pageCount) ?? 0;
  // Percent is undefined for Page-type books with no page count — hide it then.
  const canShowPercent = type === "Percentage" || book.pageCount != null;
  const pagesRead =
    type === "Page" ? value : book.pageCount ? Math.round((percent / 100) * book.pageCount) : null;

  // Switching unit must keep the reading position: convert the value via pageCount
  // (e.g. 46% of 300 -> 138 pages) instead of reinterpreting the raw number.
  const handleTypeChange = (next: ProgressType) => {
    if (next === type) return;
    if (book.pageCount) {
      setValue(
        next === "Page"
          ? Math.min(book.pageCount, Math.round((value / 100) * book.pageCount))
          : Math.min(100, Math.round((value / book.pageCount) * 100)),
      );
    }
    setType(next);
  };

  // Backend ReadingPost invariant: must have content OR progress.
  const canSubmit = value > 0 || note.trim() !== "";

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    if (!canSubmit || updateProgress.isPending) return;
    updateProgress.mutate(
      {
        userBookId: userBook.userBookId,
        body: {
          content: note.trim() || undefined,
          isSpoiler,
          progressValue: value > 0 ? value : undefined,
          progressType: value > 0 ? type : undefined,
        },
      },
      {
        onSuccess: () => {
          setNote("");
          setIsSpoiler(false);
        },
      },
    );
  };

  return (
    <form onSubmit={handleSubmit}>
      <div className="flex gap-4">
        {book.coverUrl ? (
          <img
            src={book.coverUrl}
            alt={book.title}
            className="h-28 w-20 flex-shrink-0 rounded-lg object-cover bg-stone-200"
          />
        ) : (
          <div className="h-28 w-20 flex-shrink-0 rounded-lg bg-stone-200" />
        )}

        <div className="flex-1 min-w-0">
          <h3 className="truncate font-semibold text-stone-800 dark:text-stone-100">{book.title}</h3>
          <p className="truncate text-sm text-stone-500 dark:text-stone-400">{book.authorDisplay}</p>

          <div className="mt-3">
            <div className="mb-1 flex items-center justify-between text-sm">
              <span className="text-stone-600 dark:text-stone-300">{t("feed.progress")}</span>
              <div className="flex items-center gap-2">
                {canShowPercent && (
                  <span className="font-medium text-stone-800 dark:text-stone-100">{percent}%</span>
                )}
                <select
                  value={type}
                  onChange={(e) => handleTypeChange(e.target.value as ProgressType)}
                  className="rounded border border-stone-300 dark:border-dark-raised bg-white dark:bg-dark-raised text-stone-600 dark:text-stone-300 px-1 py-0.5 text-xs focus:outline-none focus:ring-1 focus:ring-green-600"
                >
                  <option value="Percentage">{t("feed.progressUnit.percentage")}</option>
                  <option value="Page">{t("feed.progressUnit.page")}</option>
                </select>
              </div>
            </div>

            <input
              type="range"
              min={0}
              max={sliderMax}
              value={Math.min(value, sliderMax)}
              onChange={(e) => setValue(Number(e.target.value))}
              className="w-full accent-green-700"
            />

            {type === "Page" && !book.pageCount ? (
              <input
                type="number"
                min={0}
                value={value}
                onChange={(e) => setValue(Math.max(0, Number(e.target.value)))}
                className="mt-1 w-28 rounded-lg border border-stone-300 dark:border-dark-raised bg-white dark:bg-dark-raised text-stone-800 dark:text-stone-100 px-2 py-1 text-sm focus:outline-none focus:ring-1 focus:ring-green-600"
              />
            ) : (
              pagesRead != null &&
              book.pageCount != null && (
                <p className="mt-1 text-xs text-stone-500 dark:text-stone-400">
                  {t("feed.pagesOf", { read: pagesRead, total: book.pageCount })}
                </p>
              )
            )}
          </div>
        </div>
      </div>

      <textarea
        value={note}
        onChange={(e) => setNote(e.target.value)}
        maxLength={2000}
        rows={3}
        placeholder={t("feed.shareImpression")}
        className="mt-4 min-h-20 max-h-64 w-full resize-y rounded-lg border border-stone-300 dark:border-dark-raised bg-white dark:bg-dark-raised text-stone-800 dark:text-stone-100 px-3 py-2 text-sm focus:outline-none focus:ring-1 focus:ring-green-600"
      />

      <button
        type="button"
        role="switch"
        aria-checked={isSpoiler}
        onClick={() => setIsSpoiler((current) => !current)}
        className="mt-2 flex w-full items-center justify-between rounded-lg border border-stone-200 dark:border-dark-raised px-3 py-2 text-sm text-stone-600 dark:text-stone-300 hover:bg-stone-50 dark:hover:bg-dark-raised/70 focus:outline-none focus:ring-1 focus:ring-green-600"
      >
        <span className="flex items-center gap-2">
          <EyeOff size={14} />
          {t("feed.spoiler")}
        </span>
        <span
          className={`relative h-5 w-9 rounded-full transition-colors ${
            isSpoiler ? "bg-green-700" : "bg-stone-300 dark:bg-stone-600"
          }`}
        >
          <span
            className={`absolute left-0.5 top-0.5 h-4 w-4 rounded-full bg-white shadow-sm transition-transform ${
              isSpoiler ? "translate-x-4" : "translate-x-0"
            }`}
          />
        </span>
      </button>

      {updateProgress.isError && (
        <p className="mt-2 text-xs text-red-600">{t("feed.progressError")}</p>
      )}

      <Button
        type="submit"
        className="mt-3 w-full"
        disabled={!canSubmit || updateProgress.isPending}
      >
        {t("feed.updateProgress")}
      </Button>
    </form>
  );
}
