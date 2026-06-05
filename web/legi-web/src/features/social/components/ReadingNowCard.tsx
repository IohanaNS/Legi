import { useState } from "react";
import { useTranslation } from "react-i18next";
import { BookOpen } from "lucide-react";
import { Card } from "../../../components/ui/Card";
import { Button } from "../../../components/ui/Button";
import { ProgressBar } from "../../../components/ui/ProgressBar";
import { UpdateProgressModal } from "./UpdateProgressModal";
import { useUpdateBookStatus } from "../../library/hooks/useBookLifecycle";
import { useReadingNow } from "../../library/hooks/useReadingNow";
import { progressPercent } from "../../library/lib/mappers";

export function ReadingNowCard() {
  const { t } = useTranslation();
  const { data: userBook, isLoading } = useReadingNow();
  const updateStatus = useUpdateBookStatus();
  const [showModal, setShowModal] = useState(false);
  const [markFinishedError, setMarkFinishedError] = useState(false);

  // Hide entirely while loading or when the user has nothing in progress.
  if (isLoading || !userBook) return null;

  const { book } = userBook;
  const percent = progressPercent(userBook.progressValue, userBook.progressType, book.pageCount);

  return (
    <>
      <Card>
        <div className="p-4">
          <div className="mb-3 flex items-center gap-2 text-sm font-medium text-stone-700">
            <BookOpen size={16} />
            {t("feed.readingNow")}
          </div>

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
              <h3 className="font-semibold text-stone-800">{book.title}</h3>
              <p className="text-sm text-stone-500">{book.authorDisplay}</p>

              {percent !== null && (
                <div className="mt-3">
                  <div className="mb-1 flex justify-between text-sm">
                    <span className="text-stone-600">{t("feed.progress")}</span>
                    <span className="font-medium text-stone-800">{percent}%</span>
                  </div>
                  <ProgressBar value={percent} />
                </div>
              )}

              <div className="mt-3 flex flex-wrap gap-2">
                <Button variant="primary" size="sm" onClick={() => setShowModal(true)}>
                  {t("feed.updateProgress")}
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={updateStatus.isPending}
                  onClick={() => {
                    setMarkFinishedError(false);
                    updateStatus.mutate(
                      { userBookId: userBook.userBookId, status: "Finished" },
                      {
                        onError: () => setMarkFinishedError(true),
                      },
                    );
                  }}
                >
                  {t("libraryActions.markAsFinished")}
                </Button>
              </div>
              {markFinishedError && (
                <p className="mt-2 text-xs text-red-600">{t("libraryActions.statusError")}</p>
              )}
            </div>
          </div>
        </div>
      </Card>

      {showModal && (
        <UpdateProgressModal userBook={userBook} onClose={() => setShowModal(false)} />
      )}
    </>
  );
}
