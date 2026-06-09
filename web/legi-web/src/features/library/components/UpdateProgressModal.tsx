import { useTranslation } from "react-i18next";
import { X } from "lucide-react";
import { ProgressForm } from "./ProgressForm";
import type { UserBookDto } from "../types";

interface UpdateProgressModalProps {
  userBook: UserBookDto;
  onClose: () => void;
}

/**
 * Modal wrapper around the shared ProgressForm. Used by the book details page's
 * "Update progress" button; replicates the feed's Reading-now progress flow.
 */
export function UpdateProgressModal({ userBook, onClose }: UpdateProgressModalProps) {
  const { t } = useTranslation();

  return (
    <div
      className="fixed inset-0 z-50 flex items-start justify-center bg-black/40 p-4 pt-24"
      onClick={onClose}
    >
      <div
        className="w-full max-w-lg overflow-hidden rounded-xl bg-white dark:bg-dark-card shadow-lg dark:shadow-black/40"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between border-b border-stone-100 dark:border-dark-raised px-5 py-3">
          <h2 className="text-base font-semibold text-stone-800 dark:text-stone-100">
            {t("feed.updateProgress")}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="text-stone-400 dark:text-stone-500 hover:text-stone-600 dark:hover:text-stone-300"
          >
            <X size={18} />
          </button>
        </div>

        <div className="px-5 py-4">
          <ProgressForm userBook={userBook} onSuccess={onClose} />
        </div>
      </div>
    </div>
  );
}
