import { useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router-dom";
import { ArrowLeft, BookPlus, LoaderCircle, Plus, Trash2 } from "lucide-react";
import { Button } from "../../../components/ui/Button";
import { cn } from "../../../lib/utils";
import { getExistingBookIdFromConflict, isCreateBookConflict, useCreateBook } from "../hooks/useCreateBook";
import { isValidIsbn, normalizeIsbn } from "../lib/isbn";

interface RegisterBookErrors {
  isbn?: string;
  title?: string;
  synopsis?: string;
  pageCount?: string;
  publisher?: string;
  coverUrl?: string;
  authors?: Array<string | undefined>;
  tags?: Array<string | undefined>;
  form?: string;
}

export default function RegisterBookPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const createBook = useCreateBook();

  const [isbn, setIsbn] = useState("");
  const [title, setTitle] = useState("");
  const [synopsis, setSynopsis] = useState("");
  const [pageCount, setPageCount] = useState("");
  const [publisher, setPublisher] = useState("");
  const [coverUrl, setCoverUrl] = useState("");
  const [authors, setAuthors] = useState([""]);
  const [tags, setTags] = useState([""]);
  const [errors, setErrors] = useState<RegisterBookErrors>({});

  const isSaving = createBook.isPending;

  const validate = () => {
    const nextErrors: RegisterBookErrors = {};
    const authorErrors = authors.map((author) =>
      author.trim().length === 0 ? t("registerBook.errors.authorRequired") : undefined,
    );
    const tagErrors = tags.map((tag) =>
      tag.trim().length === 0 ? t("registerBook.errors.tagRequired") : undefined,
    );
    const parsedPageCount = Number(pageCount);

    if (isbn.trim().length === 0) {
      nextErrors.isbn = t("registerBook.errors.isbnRequired");
    } else if (!isValidIsbn(isbn)) {
      nextErrors.isbn = t("registerBook.errors.isbnInvalid");
    }

    if (title.trim().length === 0) {
      nextErrors.title = t("registerBook.errors.titleRequired");
    }

    if (synopsis.trim().length === 0) {
      nextErrors.synopsis = t("registerBook.errors.synopsisRequired");
    }

    if (pageCount.trim().length === 0) {
      nextErrors.pageCount = t("registerBook.errors.pageCountRequired");
    } else if (!Number.isInteger(parsedPageCount) || parsedPageCount <= 0) {
      nextErrors.pageCount = t("registerBook.errors.pageCountInvalid");
    }

    if (publisher.trim().length === 0) {
      nextErrors.publisher = t("registerBook.errors.publisherRequired");
    }

    if (coverUrl.trim().length === 0) {
      nextErrors.coverUrl = t("registerBook.errors.coverUrlRequired");
    } else if (!isValidUrl(coverUrl)) {
      nextErrors.coverUrl = t("registerBook.errors.coverUrlInvalid");
    }

    if (authorErrors.some(Boolean)) {
      nextErrors.authors = authorErrors;
    }

    if (tagErrors.some(Boolean)) {
      nextErrors.tags = tagErrors;
    }

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (isSaving || !validate()) {
      return;
    }

    try {
      const response = await createBook.mutateAsync({
        isbn: normalizeIsbn(isbn),
        title: title.trim(),
        authors: authors.map((author) => author.trim()),
        synopsis: synopsis.trim(),
        pageCount: Number(pageCount),
        publisher: publisher.trim(),
        coverUrl: coverUrl.trim(),
        tags: tags.map((tag) => tag.trim()),
      });

      navigate(`/books/${response.bookId}`, { state: { bookNotice: "created" } });
    } catch (error) {
      const existingBookId = getExistingBookIdFromConflict(error);
      if (existingBookId) {
        navigate(`/books/${existingBookId}`, { state: { bookNotice: "alreadyExists" } });
        return;
      }

      setErrors((current) => ({
        ...current,
        form: isCreateBookConflict(error)
          ? t("registerBook.errors.duplicate")
          : t("registerBook.errors.saveFailed"),
      }));
    }
  };

  const updateAuthor = (index: number, value: string) => {
    setAuthors((current) => current.map((author, i) => (i === index ? value : author)));
  };

  const addAuthor = () => {
    setAuthors((current) => [...current, ""]);
  };

  const removeAuthor = (index: number) => {
    setAuthors((current) => current.filter((_, i) => i !== index));
  };

  const updateTag = (index: number, value: string) => {
    setTags((current) => current.map((tag, i) => (i === index ? value : tag)));
  };

  const addTag = () => {
    setTags((current) => [...current, ""]);
  };

  const removeTag = (index: number) => {
    setTags((current) => current.filter((_, i) => i !== index));
  };

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <button
        type="button"
        onClick={() => navigate(-1)}
        className="flex items-center gap-2 text-sm text-stone-500 transition-colors hover:text-stone-700 dark:text-stone-400 dark:hover:text-stone-200"
      >
        <ArrowLeft size={16} />
        {t("bookDetails.back")}
      </button>

      <header>
        <h1 className="font-serif text-[1.5rem] font-semibold leading-tight text-stone-800 dark:text-stone-100">
          {t("registerBook.title")}
        </h1>
        <p className="mt-1 text-sm text-stone-500 dark:text-stone-400">
          {t("registerBook.subtitle")}
        </p>
      </header>

      <form className="space-y-5" onSubmit={handleSubmit} noValidate>
        {errors.form && (
          <p className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700 dark:border-red-900/60 dark:bg-red-950/30 dark:text-red-200">
            {errors.form}
          </p>
        )}

        <Field label={t("registerBook.isbn")} error={errors.isbn}>
          <input
            value={isbn}
            onChange={(event) => setIsbn(event.target.value)}
            placeholder={t("registerBook.isbnPlaceholder")}
            className={fieldClass(Boolean(errors.isbn))}
            disabled={isSaving}
          />
        </Field>

        <Field label={t("registerBook.bookTitle")} error={errors.title}>
          <input
            value={title}
            onChange={(event) => setTitle(event.target.value)}
            placeholder={t("registerBook.titlePlaceholder")}
            className={fieldClass(Boolean(errors.title))}
            disabled={isSaving}
          />
        </Field>

        <Field label={t("registerBook.publisher")} error={errors.publisher}>
          <input
            value={publisher}
            onChange={(event) => setPublisher(event.target.value)}
            placeholder={t("registerBook.publisherPlaceholder")}
            className={fieldClass(Boolean(errors.publisher))}
            disabled={isSaving}
          />
        </Field>

        <div className="grid gap-5 sm:grid-cols-2">
          <Field label={t("registerBook.pageCount")} error={errors.pageCount}>
            <input
              type="number"
              min={1}
              step={1}
              value={pageCount}
              onChange={(event) => setPageCount(event.target.value)}
              placeholder={t("registerBook.pageCountPlaceholder")}
              className={fieldClass(Boolean(errors.pageCount))}
              disabled={isSaving}
            />
          </Field>

          <Field label={t("registerBook.coverUrl")} error={errors.coverUrl}>
            <input
              type="url"
              value={coverUrl}
              onChange={(event) => setCoverUrl(event.target.value)}
              placeholder={t("registerBook.coverUrlPlaceholder")}
              className={fieldClass(Boolean(errors.coverUrl))}
              disabled={isSaving}
            />
          </Field>
        </div>

        <div>
          <div className="mb-2 flex items-center justify-between gap-3">
            <label className="block text-sm font-medium text-stone-700 dark:text-stone-300">
              {t("registerBook.authors")}
            </label>
            <button
              type="button"
              onClick={addAuthor}
              disabled={isSaving}
              className="inline-flex items-center gap-1.5 rounded-md px-2 py-1 text-xs font-medium text-green-700 transition-colors hover:bg-green-50 disabled:pointer-events-none disabled:opacity-50 dark:hover:bg-green-950/40"
            >
              <Plus size={14} />
              {t("registerBook.addAuthor")}
            </button>
          </div>

          <div className="space-y-2">
            {authors.map((author, index) => (
              <div key={index}>
                <div className="flex gap-2">
                  <input
                    value={author}
                    onChange={(event) => updateAuthor(index, event.target.value)}
                    placeholder={t("registerBook.authorPlaceholder", { number: index + 1 })}
                    className={fieldClass(Boolean(errors.authors?.[index]))}
                    disabled={isSaving}
                  />
                  <button
                    type="button"
                    aria-label={t("registerBook.removeAuthor")}
                    onClick={() => removeAuthor(index)}
                    disabled={isSaving || authors.length === 1}
                    className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg border border-stone-200 text-stone-500 transition-colors hover:bg-red-50 hover:text-red-600 disabled:pointer-events-none disabled:opacity-40 dark:border-dark-raised dark:hover:bg-red-950/40"
                  >
                    <Trash2 size={16} />
                  </button>
                </div>
                {errors.authors?.[index] && (
                  <p className="mt-1 text-xs text-red-500">{errors.authors[index]}</p>
                )}
              </div>
            ))}
          </div>
        </div>

        <div>
          <div className="mb-2 flex items-center justify-between gap-3">
            <label className="block text-sm font-medium text-stone-700 dark:text-stone-300">
              {t("registerBook.tags")}
            </label>
            <button
              type="button"
              onClick={addTag}
              disabled={isSaving}
              className="inline-flex items-center gap-1.5 rounded-md px-2 py-1 text-xs font-medium text-green-700 transition-colors hover:bg-green-50 disabled:pointer-events-none disabled:opacity-50 dark:hover:bg-green-950/40"
            >
              <Plus size={14} />
              {t("registerBook.addTag")}
            </button>
          </div>

          <div className="space-y-2">
            {tags.map((tag, index) => (
              <div key={index}>
                <div className="flex gap-2">
                  <input
                    value={tag}
                    onChange={(event) => updateTag(index, event.target.value)}
                    placeholder={t("registerBook.tagPlaceholder", { number: index + 1 })}
                    className={fieldClass(Boolean(errors.tags?.[index]))}
                    disabled={isSaving}
                  />
                  <button
                    type="button"
                    aria-label={t("registerBook.removeTag")}
                    onClick={() => removeTag(index)}
                    disabled={isSaving || tags.length === 1}
                    className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg border border-stone-200 text-stone-500 transition-colors hover:bg-red-50 hover:text-red-600 disabled:pointer-events-none disabled:opacity-40 dark:border-dark-raised dark:hover:bg-red-950/40"
                  >
                    <Trash2 size={16} />
                  </button>
                </div>
                {errors.tags?.[index] && (
                  <p className="mt-1 text-xs text-red-500">{errors.tags[index]}</p>
                )}
              </div>
            ))}
          </div>
        </div>

        <Field label={t("registerBook.synopsis")} error={errors.synopsis}>
          <textarea
            value={synopsis}
            onChange={(event) => setSynopsis(event.target.value)}
            placeholder={t("registerBook.synopsisPlaceholder")}
            rows={5}
            className={cn(fieldClass(Boolean(errors.synopsis)), "resize-y")}
            disabled={isSaving}
          />
        </Field>

        <div className="flex justify-end gap-2 border-t border-stone-100 pt-4 dark:border-dark-raised">
          <Button type="button" variant="outline" onClick={() => navigate(-1)} disabled={isSaving}>
            {t("common.cancel")}
          </Button>
          <Button type="submit" disabled={isSaving}>
            {isSaving ? <LoaderCircle size={15} className="animate-spin" /> : <BookPlus size={15} />}
            {isSaving ? t("common.saving") : t("registerBook.save")}
          </Button>
        </div>
      </form>
    </div>
  );
}

function isValidUrl(value: string) {
  try {
    const url = new URL(value);
    return url.protocol === "http:" || url.protocol === "https:";
  } catch {
    return false;
  }
}

function Field({
  label,
  error,
  children,
}: {
  label: string;
  error?: string;
  children: React.ReactNode;
}) {
  return (
    <label className="block">
      <span className="mb-1 block text-sm font-medium text-stone-700 dark:text-stone-300">
        {label}
      </span>
      {children}
      {error && <span className="mt-1 block text-xs text-red-500">{error}</span>}
    </label>
  );
}

function fieldClass(hasError: boolean) {
  return cn(
    "w-full rounded-lg border bg-white px-3 py-2.5 text-sm text-stone-800 transition-colors placeholder:text-stone-400 focus:outline-none focus:ring-2 dark:bg-dark-card dark:text-stone-100",
    hasError
      ? "border-red-300 focus:border-red-500 focus:ring-red-500/20"
      : "border-stone-200 focus:border-green-600 focus:ring-green-600/20 dark:border-dark-raised",
  );
}
