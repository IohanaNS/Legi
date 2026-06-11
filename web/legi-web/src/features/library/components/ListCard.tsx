import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router-dom";
import { BookOpen, Globe, Lock, Pencil, Trash2 } from "lucide-react";
import { Card } from "../../../components/ui/Card";
import { Badge } from "../../../components/ui/Badge";
import { cn } from "../../../lib/utils";
import type { BookSnapshotDto, UserListSummaryDto } from "../types";

interface ListCardProps {
  list: UserListSummaryDto;
  /** When provided, an edit pencil is shown in the footer (owner only). */
  onEdit?: (list: UserListSummaryDto) => void;
  /** When provided, a delete trash icon is shown in the footer (owner only). */
  onDelete?: (list: UserListSummaryDto) => void;
}

export function ListCard({ list, onEdit, onDelete }: ListCardProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();

  return (
    <Card
      className="cursor-pointer transition-colors hover:border-stone-300 dark:hover:border-stone-600"
      onClick={() => navigate(`/lists/${list.listId}`)}
    >
      <CoverMosaic books={list.previewBooks.slice(0, 4)} />

      <div className="p-4">
        <div className="mb-1 flex items-center gap-2">
          <h3 className="truncate font-semibold text-stone-800 dark:text-stone-100">{list.name}</h3>
          <Badge variant={list.isPublic ? "success" : "secondary"}>
            <span className="flex items-center gap-1">
              {list.isPublic ? <Globe size={10} /> : <Lock size={10} />}
              {list.isPublic ? t("lists.public") : t("lists.private")}
            </span>
          </Badge>
        </div>

        {list.description && (
          <p className="line-clamp-2 text-xs text-stone-500 dark:text-stone-400">{list.description}</p>
        )}

        <div className="mt-2 flex items-center justify-between">
          <p className="text-xs text-stone-400">{t("lists.booksCount", { count: list.booksCount })}</p>

          {(onEdit || onDelete) && (
            <div className="flex items-center gap-1">
              {onEdit && (
                <button
                  type="button"
                  aria-label={t("common.edit")}
                  onClick={(e) => {
                    e.stopPropagation();
                    onEdit(list);
                  }}
                  className={cn(
                    "rounded-md p-1.5 text-stone-400 transition-colors",
                    "hover:bg-stone-100 hover:text-stone-700 dark:hover:bg-dark-raised dark:hover:text-stone-200",
                  )}
                >
                  <Pencil size={15} />
                </button>
              )}
              {onDelete && (
                <button
                  type="button"
                  aria-label={t("common.delete")}
                  onClick={(e) => {
                    e.stopPropagation();
                    onDelete(list);
                  }}
                  className={cn(
                    "rounded-md p-1.5 text-stone-400 transition-colors",
                    "hover:bg-red-50 hover:text-red-600 dark:hover:bg-red-950/40",
                  )}
                >
                  <Trash2 size={15} />
                </button>
              )}
            </div>
          )}
        </div>
      </div>
    </Card>
  );
}

/**
 * Banner of up to four book covers, laid out to fit however many the list has so
 * it never shows empty placeholder tiles next to a single cover:
 *   0 → empty state · 1 → full · 2 → side by side · 3 → one tall + two stacked · 4 → 2×2.
 */
function CoverMosaic({ books }: { books: BookSnapshotDto[] }) {
  if (books.length === 0) {
    return (
      <div className="flex aspect-[5/2] items-center justify-center bg-stone-100 dark:bg-dark-raised">
        <BookOpen size={24} className="text-stone-300 dark:text-stone-600" />
      </div>
    );
  }

  if (books.length === 3) {
    const [first, ...rest] = books;
    return (
      <div className="grid aspect-[5/2] grid-cols-2 gap-px bg-stone-100 dark:bg-dark-raised">
        <CoverTile book={first} />
        <div className="grid grid-rows-2 gap-px">
          {rest.map((book) => (
            <CoverTile key={book.bookId} book={book} />
          ))}
        </div>
      </div>
    );
  }

  const cols = books.length === 1 ? "grid-cols-1" : "grid-cols-2";
  const rows = books.length === 4 ? "grid-rows-2" : "grid-rows-1";

  return (
    <div className={cn("grid aspect-[5/2] gap-px bg-stone-100 dark:bg-dark-raised", cols, rows)}>
      {books.map((book) => (
        <CoverTile key={book.bookId} book={book} />
      ))}
    </div>
  );
}

function CoverTile({ book }: { book: BookSnapshotDto }) {
  return (
    <div className="flex items-center justify-center overflow-hidden bg-stone-200 dark:bg-dark-bg">
      {book.coverUrl ? (
        <img src={book.coverUrl} alt={book.title} className="h-full w-full object-cover" loading="lazy" />
      ) : (
        <BookOpen size={20} className="text-stone-400 dark:text-stone-600" />
      )}
    </div>
  );
}
