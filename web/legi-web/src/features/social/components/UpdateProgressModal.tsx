import { useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { X } from "lucide-react";
import { Button } from "../../../components/ui/Button";
import { useUpdateProgress } from "../../library/hooks/useReadingNow";
import type { ProgressType, UserBookDto } from "../../library/types";

interface UpdateProgressModalProps {
  userBook: UserBookDto;
  onClose: () => void;
}

export function UpdateProgressModal({ userBook, onClose }: UpdateProgressModalProps) {
  const { t } = useTranslation();
  const updateProgress = useUpdateProgress();

  const [value, setValue] = useState<string>(
    userBook.progressValue != null ? String(userBook.progressValue) : "",
  );
  const [type, setType] = useState<ProgressType>(userBook.progressType ?? "Percentage");
  const [note, setNote] = useState("");

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    const numeric = value.trim() === "" ? undefined : Number(value);
    if (numeric !== undefined && (Number.isNaN(numeric) || numeric < 0)) return;
    // Must have content OR progress (backend ReadingPost invariant).
    if (numeric === undefined && note.trim() === "") return;

    updateProgress.mutate(
      {
        userBookId: userBook.userBookId,
        body: {
          content: note.trim() || undefined,
          progressValue: numeric,
          progressType: numeric !== undefined ? type : undefined,
        },
      },
      { onSuccess: onClose },
    );
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      onClick={onClose}
    >
      <div
        className="w-full max-w-md rounded-xl bg-white p-5 shadow-lg"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-stone-800">
            {t("feed.updateProgressModal.title")}
          </h2>
          <button type="button" onClick={onClose} className="text-stone-400 hover:text-stone-600">
            <X size={18} />
          </button>
        </div>

        <p className="mb-4 text-sm text-stone-500">{userBook.book.title}</p>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="flex gap-2">
            <div className="flex-1">
              <label className="mb-1 block text-xs font-medium text-stone-600">
                {t("feed.updateProgressModal.value")}
              </label>
              <input
                type="number"
                min={0}
                value={value}
                onChange={(e) => setValue(e.target.value)}
                className="w-full rounded-lg border border-stone-300 px-3 py-1.5 text-sm focus:outline-none focus:ring-1 focus:ring-green-600"
              />
            </div>
            <div className="flex-1">
              <label className="mb-1 block text-xs font-medium text-stone-600">
                {t("feed.updateProgressModal.type")}
              </label>
              <select
                value={type}
                onChange={(e) => setType(e.target.value as ProgressType)}
                className="w-full rounded-lg border border-stone-300 px-3 py-1.5 text-sm focus:outline-none focus:ring-1 focus:ring-green-600"
              >
                <option value="Percentage">{t("feed.updateProgressModal.percentage")}</option>
                <option value="Page">{t("feed.updateProgressModal.page")}</option>
              </select>
            </div>
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-stone-600">
              {t("feed.updateProgressModal.note")}
            </label>
            <textarea
              value={note}
              onChange={(e) => setNote(e.target.value)}
              maxLength={2000}
              rows={3}
              placeholder={t("feed.updateProgressModal.notePlaceholder")}
              className="w-full resize-none rounded-lg border border-stone-300 px-3 py-1.5 text-sm focus:outline-none focus:ring-1 focus:ring-green-600"
            />
          </div>

          {updateProgress.isError && (
            <p className="text-xs text-red-600">{t("feed.updateProgressModal.error")}</p>
          )}

          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" size="sm" onClick={onClose}>
              {t("feed.updateProgressModal.cancel")}
            </Button>
            <Button type="submit" size="sm" disabled={updateProgress.isPending}>
              {t("feed.updateProgressModal.save")}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
