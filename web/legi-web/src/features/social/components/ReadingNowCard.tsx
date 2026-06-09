import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { BookOpen, RefreshCw } from "lucide-react";
import { Card } from "../../../components/ui/Card";
import { Button } from "../../../components/ui/Button";
import { ChangeBookModal } from "./ChangeBookModal";
import { ProgressForm } from "../../library/components/ProgressForm";
import { useReadingBooks } from "../../library/hooks/useReadingNow";

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
          <ProgressForm key={userBook.userBookId} userBook={userBook} />
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
