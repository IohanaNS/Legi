import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { Search, X } from "lucide-react";
import { useReadingBooks } from "../../library/hooks/useReadingNow";

interface ChangeBookModalProps {
  currentUserBookId: string;
  onSelect: (userBookId: string) => void;
  onClose: () => void;
}

export function ChangeBookModal({ currentUserBookId, onSelect, onClose }: ChangeBookModalProps) {
  const { t } = useTranslation();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");

  // Debounce the search before hitting GET /library?status=Reading&search=.
  useEffect(() => {
    const id = setTimeout(() => setDebounced(search), 300);
    return () => clearTimeout(id);
  }, [search]);

  const { data: books, isLoading, isError } = useReadingBooks(debounced);

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
            {t("feed.changeBookModal.title")}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="text-stone-400 dark:text-stone-500 hover:text-stone-600 dark:hover:text-stone-300"
          >
            <X size={18} />
          </button>
        </div>

        <div className="px-5 py-3">
          <div className="relative">
            <Search
              size={16}
              className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-stone-400"
            />
            <input
              autoFocus
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder={t("feed.changeBookModal.placeholder")}
              className="w-full rounded-lg border border-stone-300 dark:border-dark-raised bg-white dark:bg-dark-raised text-stone-800 dark:text-stone-100 py-2 pl-9 pr-3 text-sm focus:outline-none focus:ring-1 focus:ring-green-600"
            />
          </div>
        </div>

        <div className="max-h-80 overflow-y-auto px-2 pb-3">
          {isLoading ? (
            <p className="px-3 py-6 text-center text-sm text-stone-400">
              {t("feed.changeBookModal.loading")}
            </p>
          ) : isError ? (
            <p className="px-3 py-6 text-center text-sm text-red-600">
              {t("feed.changeBookModal.error")}
            </p>
          ) : !books || books.length === 0 ? (
            <p className="px-3 py-6 text-center text-sm text-stone-400">
              {t(debounced ? "feed.changeBookModal.noResults" : "feed.changeBookModal.empty")}
            </p>
          ) : (
            books.map((ub) => {
              const isCurrent = ub.userBookId === currentUserBookId;
              return (
                <button
                  key={ub.userBookId}
                  type="button"
                  onClick={() => {
                    onSelect(ub.userBookId);
                    onClose();
                  }}
                  className="flex w-full items-center gap-3 rounded-lg px-3 py-2 text-left hover:bg-stone-50 dark:hover:bg-dark-raised"
                >
                  {ub.book.coverUrl ? (
                    <img
                      src={ub.book.coverUrl}
                      alt={ub.book.title}
                      className="h-14 w-10 flex-shrink-0 rounded object-cover bg-stone-200"
                    />
                  ) : (
                    <div className="h-14 w-10 flex-shrink-0 rounded bg-stone-200" />
                  )}
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-sm font-medium text-stone-800 dark:text-stone-100">
                      {ub.book.title}
                    </p>
                    <p className="truncate text-xs text-stone-500 dark:text-stone-400">
                      {ub.book.authorDisplay}
                    </p>
                    {ub.book.pageCount != null && (
                      <p className="text-xs text-stone-400 dark:text-stone-500">
                        {t("feed.changeBookModal.pagesCount", { count: ub.book.pageCount })}
                      </p>
                    )}
                  </div>
                  {isCurrent && (
                    <span className="flex-shrink-0 text-xs font-medium text-stone-400 dark:text-stone-500">
                      {t("feed.changeBookModal.current")}
                    </span>
                  )}
                </button>
              );
            })
          )}
        </div>
      </div>
    </div>
  );
}
