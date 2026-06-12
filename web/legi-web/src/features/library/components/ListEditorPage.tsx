import { useEffect, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { useNavigate, useParams } from "react-router-dom";
import { Check, Globe, Lock, Plus, Search, X } from "lucide-react";
import { BookCover } from "../../../components/ui/BookCover";
import { Button } from "../../../components/ui/Button";
import { useSearchBooks } from "../../catalog/hooks/useSearchBooks";
import { cn } from "../../../lib/utils";
import { useCreateList, useListBooks, useListDetail, useUpdateList } from "../hooks/useListMutations";

interface SelectedBook {
  bookId: string;
  title: string;
  authorDisplay: string;
  coverUrl?: string | null;
}

interface InitialList {
  name: string;
  description: string;
  isPublic: boolean;
  books: SelectedBook[];
}

const EMPTY_INITIAL: InitialList = { name: "", description: "", isPublic: true, books: [] };

/**
 * Route entry for both create (`/lists/new`) and edit (`/lists/:listId/edit`).
 * For edit it waits for the list data, then mounts <ListForm> with the loaded
 * values as initial state — so the form initializes from props and needs no
 * prefill effect.
 */
export default function ListEditorPage() {
  const { t } = useTranslation();
  const { listId } = useParams<{ listId: string }>();
  const isEdit = Boolean(listId);

  const detailQuery = useListDetail(listId ?? "", isEdit);
  const booksQuery = useListBooks(listId ?? "", isEdit);

  if (!isEdit) {
    return <ListForm mode="create" initial={EMPTY_INITIAL} />;
  }

  if (detailQuery.isLoading || booksQuery.isLoading) {
    return <p className="py-10 text-center text-sm text-stone-400">{t("common.loading")}</p>;
  }
  if (detailQuery.isError || !detailQuery.data) {
    return <p className="py-10 text-center text-sm text-stone-500">{t("common.couldNotLoad")}</p>;
  }

  const initial: InitialList = {
    name: detailQuery.data.name,
    description: detailQuery.data.description ?? "",
    isPublic: detailQuery.data.isPublic,
    books: (booksQuery.data?.items ?? []).map((i) => ({
      bookId: i.book.bookId,
      title: i.book.title,
      authorDisplay: i.book.authorDisplay,
      coverUrl: i.book.coverUrl,
    })),
  };

  return <ListForm mode="edit" listId={listId} initial={initial} />;
}

function ListForm({
  mode,
  listId,
  initial,
}: {
  mode: "create" | "edit";
  listId?: string;
  initial: InitialList;
}) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const createList = useCreateList();
  const updateList = useUpdateList(listId ?? "");

  const [name, setName] = useState(initial.name);
  const [description, setDescription] = useState(initial.description);
  const [isPublic, setIsPublic] = useState(initial.isPublic);
  const [selected, setSelected] = useState<SelectedBook[]>(initial.books);

  // Book search (reuses the Explore search), debounced.
  const [searchInput, setSearchInput] = useState("");
  const [debounced, setDebounced] = useState("");
  useEffect(() => {
    const id = window.setTimeout(() => setDebounced(searchInput.trim()), 300);
    return () => window.clearTimeout(id);
  }, [searchInput]);

  const searchQuery = useSearchBooks({
    searchTerm: debounced,
    sort: "mostPopular",
    enabled: debounced.length > 0,
  });
  const results = searchQuery.data?.pages.flatMap((p) => p.books) ?? [];
  const selectedIds = useMemo(() => new Set(selected.map((b) => b.bookId)), [selected]);

  const addBook = (book: SelectedBook) => {
    if (selectedIds.has(book.bookId)) return;
    setSelected((prev) => [...prev, book]);
  };
  const removeBook = (bookId: string) =>
    setSelected((prev) => prev.filter((b) => b.bookId !== bookId));

  const trimmedName = name.trim();
  const nameError = trimmedName.length === 0;
  const isSaving = createList.isPending || updateList.isPending;

  const handleSave = () => {
    if (nameError || isSaving) return;
    const body = {
      name: trimmedName,
      description: description.trim() || null,
      isPublic,
      bookIds: selected.map((b) => b.bookId),
    };

    if (mode === "edit" && listId) {
      updateList.mutate(body, { onSuccess: () => navigate(`/lists/${listId}`) });
    } else {
      createList.mutate(body, { onSuccess: (res) => navigate(`/lists/${res.listId}`) });
    }
  };

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <h1 className="font-serif text-[1.5rem] font-semibold text-stone-800 dark:text-stone-100">
        {mode === "edit" ? t("lists.editTitle") : t("lists.createTitle")}
      </h1>

      {/* Name */}
      <div>
        <label className="mb-1 block text-sm font-medium text-stone-700 dark:text-stone-300">
          {t("lists.form.name")}
        </label>
        <input
          value={name}
          onChange={(e) => setName(e.target.value)}
          maxLength={50}
          placeholder={t("lists.form.namePlaceholder")}
          className={cn(
            "w-full rounded-lg border bg-white px-3 py-2.5 text-sm text-stone-800 transition-colors focus:outline-none focus:ring-2 dark:bg-dark-card dark:text-stone-100",
            nameError
              ? "border-red-300 focus:border-red-500 focus:ring-red-500/20"
              : "border-stone-200 focus:border-green-600 focus:ring-green-600/20 dark:border-dark-raised",
          )}
        />
        {nameError && <p className="mt-1 text-xs text-red-500">{t("lists.form.nameRequired")}</p>}
      </div>

      {/* Description */}
      <div>
        <label className="mb-1 block text-sm font-medium text-stone-700 dark:text-stone-300">
          {t("lists.form.description")}
        </label>
        <textarea
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          maxLength={500}
          rows={3}
          placeholder={t("lists.form.descriptionPlaceholder")}
          className="w-full resize-none rounded-lg border border-stone-200 bg-white px-3 py-2.5 text-sm text-stone-800 transition-colors focus:border-green-600 focus:outline-none focus:ring-2 focus:ring-green-600/20 dark:border-dark-raised dark:bg-dark-card dark:text-stone-100"
        />
      </div>

      {/* Visibility */}
      <div className="flex gap-2">
        <VisibilityButton
          active={isPublic}
          onClick={() => setIsPublic(true)}
          icon={<Globe size={15} />}
          label={t("lists.public")}
        />
        <VisibilityButton
          active={!isPublic}
          onClick={() => setIsPublic(false)}
          icon={<Lock size={15} />}
          label={t("lists.private")}
        />
      </div>

      {/* Selected books */}
      <div>
        <h2 className="mb-2 text-sm font-medium text-stone-700 dark:text-stone-300">
          {t("lists.form.selectedBooks", { count: selected.length })}
        </h2>
        {selected.length === 0 ? (
          <p className="rounded-lg border border-dashed border-stone-200 py-6 text-center text-xs text-stone-400 dark:border-dark-raised">
            {t("lists.form.noBooks")}
          </p>
        ) : (
          <ul className="space-y-2">
            {selected.map((book) => (
              <li
                key={book.bookId}
                className="flex items-center gap-3 rounded-lg border border-stone-200 bg-white p-2 dark:border-dark-raised dark:bg-dark-card"
              >
                <Cover url={book.coverUrl} title={book.title} />
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium text-stone-800 dark:text-stone-100">{book.title}</p>
                  <p className="truncate text-xs text-stone-500">{book.authorDisplay}</p>
                </div>
                <button
                  type="button"
                  aria-label={t("common.remove")}
                  onClick={() => removeBook(book.bookId)}
                  className="rounded-md p-1.5 text-stone-400 hover:bg-red-50 hover:text-red-600 dark:hover:bg-red-950/40"
                >
                  <X size={16} />
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>

      {/* Book search */}
      <div>
        <h2 className="mb-2 text-sm font-medium text-stone-700 dark:text-stone-300">
          {t("lists.form.addBooks")}
        </h2>
        <div className="relative">
          <Search
            size={18}
            className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-stone-400"
          />
          <input
            type="search"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            placeholder={t("lists.form.searchPlaceholder")}
            className="w-full rounded-lg border border-stone-200 bg-white py-2.5 pl-10 pr-4 text-sm text-stone-800 placeholder:text-stone-400 focus:border-green-600 focus:outline-none focus:ring-2 focus:ring-green-600/20 dark:border-dark-raised dark:bg-dark-card dark:text-stone-100"
          />
        </div>

        {debounced.length > 0 && (
          <div className="mt-2 space-y-2">
            {searchQuery.isLoading ? (
              <p className="py-4 text-center text-xs text-stone-400">{t("common.loading")}</p>
            ) : results.length === 0 ? (
              <p className="py-4 text-center text-xs text-stone-400">{t("lists.form.noResults")}</p>
            ) : (
              results.map((book) => {
                const added = selectedIds.has(book.id);
                const authorDisplay = book.authors.map((a) => a.name).join(", ");
                return (
                  <div
                    key={book.id}
                    className="flex items-center gap-3 rounded-lg border border-stone-200 bg-white p-2 dark:border-dark-raised dark:bg-dark-card"
                  >
                    <Cover url={book.coverUrl} title={book.title} />
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-medium text-stone-800 dark:text-stone-100">{book.title}</p>
                      <p className="truncate text-xs text-stone-500">{authorDisplay}</p>
                    </div>
                    <button
                      type="button"
                      disabled={added}
                      aria-label={t("lists.form.addBook")}
                      onClick={() => addBook({ bookId: book.id, title: book.title, authorDisplay, coverUrl: book.coverUrl })}
                      className={cn(
                        "flex h-8 w-8 items-center justify-center rounded-md transition-colors",
                        added
                          ? "text-green-600"
                          : "text-stone-500 hover:bg-green-50 hover:text-green-700 dark:hover:bg-green-950/40",
                      )}
                    >
                      {added ? <Check size={16} /> : <Plus size={16} />}
                    </button>
                  </div>
                );
              })
            )}
          </div>
        )}
      </div>

      {/* Actions */}
      <div className="flex justify-end gap-2 border-t border-stone-100 pt-4 dark:border-dark-raised">
        <Button variant="outline" onClick={() => navigate(-1)} disabled={isSaving}>
          {t("common.cancel")}
        </Button>
        <Button onClick={handleSave} disabled={nameError || isSaving}>
          {isSaving ? t("common.saving") : t("common.save")}
        </Button>
      </div>
    </div>
  );
}

function VisibilityButton({
  active,
  onClick,
  icon,
  label,
}: {
  active: boolean;
  onClick: () => void;
  icon: React.ReactNode;
  label: string;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-pressed={active}
      className={cn(
        "flex items-center gap-1.5 rounded-lg border px-3 py-2 text-sm transition-colors",
        active
          ? "border-green-600 bg-green-50 text-green-700 dark:bg-green-950/40"
          : "border-stone-200 text-stone-600 hover:bg-stone-50 dark:border-dark-raised dark:text-stone-300 dark:hover:bg-dark-raised",
      )}
    >
      {icon}
      {label}
    </button>
  );
}

function Cover({ url, title }: { url?: string | null; title: string }) {
  return <BookCover title={title} coverUrl={url} className="h-14 w-10 shrink-0 rounded" />;
}
