import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Button } from "../../../components/ui/Button";
import { todayIso } from "../lib/mappers";

interface FinishDatePickerProps {
  /** Initial date (yyyy-MM-dd), or null for "unknown". Defaults to today. */
  initialDate?: string | null;
  /**
   * Start with "I don't remember" pre-selected. Used when finishing a book the
   * user is back-cataloging (Explore / not-in-library), where today is no signal.
   */
  defaultUnknown?: boolean;
  isPending?: boolean;
  errorText?: string | null;
  /** Receives the chosen date, or null when "I don't remember" is selected. */
  onConfirm: (finishedReadingAt: string | null) => void;
  onCancel: () => void;
}

/**
 * Compact inline control for choosing when a book was finished. Offers a date
 * input (capped at today) plus an explicit "I don't remember" option that sends
 * null — keeping back-catalogued reads out of date-bucketed statistics.
 */
export function FinishDatePicker({
  initialDate,
  defaultUnknown = false,
  isPending = false,
  errorText,
  onConfirm,
  onCancel,
}: FinishDatePickerProps) {
  const { t } = useTranslation();
  const today = todayIso();
  const [unknown, setUnknown] = useState(defaultUnknown || initialDate === null);
  const [date, setDate] = useState(initialDate ?? today);

  return (
    <div className="space-y-2">
      <label className="block text-xs font-semibold uppercase text-stone-500 dark:text-stone-400">
        {t("finishDate.label")}
      </label>

      <input
        type="date"
        value={date}
        max={today}
        disabled={unknown || isPending}
        onChange={(event) => setDate(event.target.value)}
        className="w-full rounded-md border border-stone-300 dark:border-dark-raised bg-white dark:bg-dark-raised px-2 py-1 text-sm text-stone-800 dark:text-stone-100 focus:outline-none focus:ring-1 focus:ring-green-600 disabled:opacity-50"
      />

      <label className="flex items-center gap-2 text-sm text-stone-600 dark:text-stone-300">
        <input
          type="checkbox"
          checked={unknown}
          disabled={isPending}
          onChange={(event) => setUnknown(event.target.checked)}
          className="accent-green-700"
        />
        {t("finishDate.unknown")}
      </label>

      {errorText && <p className="text-xs text-red-600">{errorText}</p>}

      <div className="flex gap-2 pt-1">
        <Button
          type="button"
          size="sm"
          className="flex-1"
          disabled={isPending || (!unknown && !date)}
          onClick={() => onConfirm(unknown ? null : date)}
        >
          {t("finishDate.confirm")}
        </Button>
        <Button
          type="button"
          size="sm"
          variant="outline"
          className="flex-1"
          disabled={isPending}
          onClick={onCancel}
        >
          {t("common.cancel")}
        </Button>
      </div>
    </div>
  );
}
