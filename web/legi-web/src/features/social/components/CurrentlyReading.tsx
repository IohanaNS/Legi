import { useTranslation } from "react-i18next";
import { BookOpen } from "lucide-react";
import { Card } from "../../../components/ui/Card";
import { Button } from "../../../components/ui/Button";
import { ProgressBar } from "../../../components/ui/ProgressBar";

interface CurrentlyReadingProps {
  bookTitle: string;
  bookAuthor: string;
  coverUrl?: string;
  progress: number;
  currentPage: number;
  totalPages: number;
}

export function CurrentlyReading({
  bookTitle,
  bookAuthor,
  progress,
  currentPage,
  totalPages,
}: CurrentlyReadingProps) {
  const { t } = useTranslation();

  return (
    <Card>
      <div className="p-4">
        <div className="flex items-center gap-2 text-sm font-medium text-stone-700 mb-3">
          <BookOpen size={16} />
          {t("feed.readingNow")}
        </div>

        <div className="flex gap-4">
          <div className="w-20 h-28 bg-stone-200 rounded-lg flex-shrink-0" />

          <div className="flex-1">
            <h3 className="font-semibold text-stone-800">{bookTitle}</h3>
            <p className="text-sm text-stone-500">{bookAuthor}</p>

            <div className="mt-3">
              <div className="flex justify-between text-sm mb-1">
                <span className="text-stone-600">{t("feed.progress")}</span>
                <span className="font-medium text-stone-800">{progress}%</span>
              </div>
              <ProgressBar value={progress} />
              <p className="text-xs text-stone-500 mt-1">
                {t("feed.pagesOf", { current: currentPage, total: totalPages })}
              </p>
            </div>

            <Button variant="primary" size="sm" className="mt-3">
              {t("feed.updateProgress")}
            </Button>
          </div>
        </div>
      </div>
    </Card>
  );
}