import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type FormEvent,
  type ReactNode,
} from "react";
import { createPortal } from "react-dom";
import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router-dom";
import { BookOpen, List, LoaderCircle, Search, UserRound, X } from "lucide-react";
import { Avatar } from "../../../components/ui/Avatar";
import { cn } from "../../../lib/utils";
import { useSearchAuthors } from "../../catalog/hooks/useSearchAuthors";
import { useSearchBooks } from "../../catalog/hooks/useSearchBooks";
import type { AuthorResult, BookSummaryDto } from "../../catalog/types";
import { useLists } from "../../library/hooks/useLists";
import { useSearchPublicLists } from "../../library/hooks/useSearchPublicLists";
import type { UserListSummaryDto } from "../../library/types";
import {
  canSearchUsersByUsername,
  isUsernameSearchPrefixValid,
  normalizeUsernameSearchPrefix,
  useSearchUsers,
} from "../../social/hooks/useSearchUsers";
import type { FollowUserDto } from "../../social/types";

const MIN_SEARCH_CHARS = 2;
const USER_SEARCH_MIN_CHARS = 3;
const SEARCH_DEBOUNCE_MS = 250;
const RESULT_LIMIT = 5;

export function GlobalSearch() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [isOpen, setIsOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [debouncedQuery, setDebouncedQuery] = useState("");
  const inputRef = useRef<HTMLInputElement>(null);

  const closeSearch = useCallback(() => {
    setIsOpen(false);
    setQuery("");
    setDebouncedQuery("");
  }, []);

  useEffect(() => {
    if (!isOpen) return;

    const timeoutId = window.setTimeout(() => inputRef.current?.focus(), 0);
    return () => window.clearTimeout(timeoutId);
  }, [isOpen]);

  useEffect(() => {
    if (!isOpen) return;

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        closeSearch();
      }
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [closeSearch, isOpen]);

  useEffect(() => {
    if (!isOpen) return;

    const originalOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = originalOverflow;
    };
  }, [isOpen]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setDebouncedQuery(query.trim());
    }, SEARCH_DEBOUNCE_MS);

    return () => window.clearTimeout(timeoutId);
  }, [query]);

  const canSearch = debouncedQuery.length >= MIN_SEARCH_CHARS;
  const normalizedUserQuery = normalizeUsernameSearchPrefix(debouncedQuery);
  const canSearchUsers = canSearchUsersByUsername(normalizedUserQuery);
  const userQueryIsInvalid =
    normalizedUserQuery.length >= USER_SEARCH_MIN_CHARS &&
    !isUsernameSearchPrefixValid(normalizedUserQuery);

  const booksQuery = useSearchBooks({
    searchTerm: debouncedQuery,
    sort: "mostPopular",
    pageSize: RESULT_LIMIT,
    enabled: isOpen && canSearch,
  });
  const authorsQuery = useSearchAuthors(debouncedQuery, isOpen && canSearch);
  const listsQuery = useLists({ enabled: isOpen && canSearch });
  const publicListsQuery = useSearchPublicLists(debouncedQuery, isOpen && canSearch);
  const usersQuery = useSearchUsers(debouncedQuery);

  const bookResults = booksQuery.data?.pages[0]?.books.slice(0, RESULT_LIMIT) ?? [];
  const authorResults = authorsQuery.data ?? [];
  const userResults = usersQuery.data?.slice(0, RESULT_LIMIT) ?? [];

  // Combine the viewer's own lists (public + private, matched client-side) with
  // public lists from any user (server-searched), de-duplicated by id.
  const listResults = useMemo(() => {
    if (!canSearch) return [];

    const listQuery = debouncedQuery.toLowerCase();
    const ownMatches = (listsQuery.data ?? []).filter((list) => {
      const name = list.name.toLowerCase();
      const description = list.description?.toLowerCase() ?? "";
      return name.includes(listQuery) || description.includes(listQuery);
    });
    const publicMatches = publicListsQuery.data?.items ?? [];

    const seen = new Set<string>();
    return [...ownMatches, ...publicMatches]
      .filter((list) => {
        if (seen.has(list.listId)) return false;
        seen.add(list.listId);
        return true;
      })
      .slice(0, RESULT_LIMIT);
  }, [canSearch, debouncedQuery, listsQuery.data, publicListsQuery.data]);

  const listsLoading = listsQuery.isFetching || publicListsQuery.isFetching;
  const listsError = listsQuery.isError && publicListsQuery.isError;

  const openSearch = () => setIsOpen(true);

  const navigateWithSearch = (pathname: string, searchTerm: string) => {
    const params = new URLSearchParams({ search: searchTerm });
    navigate(`${pathname}?${params.toString()}`);
    closeSearch();
  };

  const navigateToAuthor = (author: AuthorResult) => {
    const params = new URLSearchParams({
      authorSlug: author.slug,
      authorName: author.name,
    });
    navigate(`/explore?${params.toString()}`);
    closeSearch();
  };

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!canSearch) return;

    navigateWithSearch("/explore", debouncedQuery);
  };

  return (
    <>
      <button
        type="button"
        onClick={openSearch}
        aria-haspopup="dialog"
        className="flex w-full items-center gap-2 rounded-lg bg-white/10 px-3 py-2 text-left text-sm text-green-300 transition-colors hover:bg-white/15 hover:text-white focus:outline-none focus:ring-2 focus:ring-green-500/50"
      >
        <Search size={16} className="shrink-0" />
        <span className="truncate">{t("common.search")}</span>
      </button>

      {isOpen && createPortal(
        <div
          className="fixed inset-0 z-50 bg-black/45 px-4 py-8 backdrop-blur-sm"
          role="presentation"
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) closeSearch();
          }}
        >
          <section
            role="dialog"
            aria-modal="true"
            aria-labelledby="global-search-title"
            className="mx-auto flex max-h-[min(680px,calc(100vh-4rem))] w-full max-w-2xl flex-col overflow-hidden rounded-lg border border-stone-200 bg-white shadow-2xl dark:border-dark-raised dark:bg-dark-card"
          >
            <div className="border-b border-stone-200 p-4 dark:border-dark-raised">
              <div className="mb-3 flex items-center justify-between gap-3">
                <h2
                  id="global-search-title"
                  className="text-sm font-semibold text-stone-800 dark:text-stone-100"
                >
                  {t("globalSearch.title")}
                </h2>
                <button
                  type="button"
                  onClick={closeSearch}
                  aria-label={t("globalSearch.close")}
                  className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-500 transition-colors hover:bg-stone-100 hover:text-stone-800 focus:outline-none focus:ring-2 focus:ring-green-600/30 dark:text-stone-400 dark:hover:bg-dark-raised dark:hover:text-stone-100"
                >
                  <X size={17} />
                </button>
              </div>

              <form onSubmit={handleSubmit} className="relative">
                <label htmlFor="global-search-input" className="sr-only">
                  {t("globalSearch.inputLabel")}
                </label>
                <Search
                  size={18}
                  className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-stone-400 dark:text-stone-500"
                />
                <input
                  id="global-search-input"
                  ref={inputRef}
                  type="search"
                  value={query}
                  onChange={(event) => setQuery(event.target.value)}
                  placeholder={t("globalSearch.placeholder")}
                  className="w-full rounded-lg border border-stone-300 bg-white py-2.5 pl-10 pr-4 text-sm text-stone-800 placeholder:text-stone-400 transition-colors focus:border-green-600 focus:outline-none focus:ring-2 focus:ring-green-600/20 dark:border-dark-raised dark:bg-dark-raised dark:text-stone-100 dark:placeholder:text-stone-500"
                />
              </form>
            </div>

            <div className="flex-1 overflow-y-auto px-4 py-3">
              {query.trim().length === 0 ? (
                <SearchEmpty icon={<Search size={18} />} label={t("globalSearch.hint")} />
              ) : query.trim().length < MIN_SEARCH_CHARS ? (
                <SearchEmpty label={t("globalSearch.minChars")} />
              ) : (
                <div className="space-y-5">
                  <SearchSection title={t("globalSearch.books")}>
                    {booksQuery.isFetching ? (
                      <LoadingRow label={t("globalSearch.searchingBooks")} />
                    ) : booksQuery.isError ? (
                      <MutedRow label={t("globalSearch.booksError")} />
                    ) : bookResults.length > 0 ? (
                      bookResults.map((book) => (
                        <BookResultRow
                          key={book.id}
                          book={book}
                          onClick={() => navigateWithSearch("/explore", book.title)}
                        />
                      ))
                    ) : (
                      <MutedRow label={t("globalSearch.noBooks")} />
                    )}
                  </SearchSection>

                  <SearchSection title={t("globalSearch.authors")}>
                    {authorsQuery.isFetching ? (
                      <LoadingRow label={t("globalSearch.searchingAuthors")} />
                    ) : authorsQuery.isError ? (
                      <MutedRow label={t("globalSearch.authorsError")} />
                    ) : authorResults.length > 0 ? (
                      authorResults.map((author) => (
                        <AuthorResultRow
                          key={author.slug}
                          author={author}
                          onClick={() => navigateToAuthor(author)}
                        />
                      ))
                    ) : (
                      <MutedRow label={t("globalSearch.noAuthors")} />
                    )}
                  </SearchSection>

                  <SearchSection title={t("globalSearch.lists")}>
                    {listResults.length > 0 ? (
                      listResults.map((list) => (
                        <ListResultRow
                          key={list.listId}
                          list={list}
                          onClick={() => {
                            navigate(`/lists/${list.listId}`);
                            closeSearch();
                          }}
                        />
                      ))
                    ) : listsLoading ? (
                      <LoadingRow label={t("globalSearch.searchingLists")} />
                    ) : listsError ? (
                      <MutedRow label={t("globalSearch.listsError")} />
                    ) : (
                      <MutedRow label={t("globalSearch.noLists")} />
                    )}
                  </SearchSection>

                  <SearchSection title={t("globalSearch.users")}>
                    {normalizedUserQuery.length < USER_SEARCH_MIN_CHARS ? (
                      <MutedRow label={t("globalSearch.usersMinChars")} />
                    ) : userQueryIsInvalid ? (
                      <MutedRow label={t("globalSearch.usersInvalid")} />
                    ) : usersQuery.isFetching && canSearchUsers ? (
                      <LoadingRow label={t("globalSearch.searchingUsers")} />
                    ) : usersQuery.isError && canSearchUsers ? (
                      <MutedRow label={t("globalSearch.usersError")} />
                    ) : userResults.length > 0 ? (
                      userResults.map((user) => (
                        <UserResultRow
                          key={user.userId}
                          user={user}
                          onClick={() => {
                            navigate(`/users/${user.userId}`);
                            closeSearch();
                          }}
                        />
                      ))
                    ) : (
                      <MutedRow label={t("globalSearch.noUsers")} />
                    )}
                  </SearchSection>
                </div>
              )}
            </div>
          </section>
        </div>,
        document.body
      )}
    </>
  );
}

function SearchSection({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section>
      <h3 className="mb-2 text-xs font-semibold uppercase text-stone-500 dark:text-stone-400">
        {title}
      </h3>
      <div className="space-y-1">{children}</div>
    </section>
  );
}

function BookResultRow({ book, onClick }: { book: BookSummaryDto; onClick: () => void }) {
  const { t } = useTranslation();
  const authors = book.authors.map((author) => author.name).join(", ") || t("explore.unknownAuthor");

  return (
    <ResultButton onClick={onClick}>
      <div className="h-14 w-10 shrink-0 overflow-hidden rounded bg-stone-200 dark:bg-dark-raised">
        {book.coverUrl ? (
          <img src={book.coverUrl} alt={book.title} className="h-full w-full object-cover" />
        ) : (
          <div className="flex h-full w-full items-center justify-center text-stone-400">
            <BookOpen size={18} />
          </div>
        )}
      </div>
      <ResultText title={book.title} subtitle={authors} meta={t("globalSearch.bookResult")} />
    </ResultButton>
  );
}

function ListResultRow({ list, onClick }: { list: UserListSummaryDto; onClick: () => void }) {
  const { t } = useTranslation();

  return (
    <ResultButton onClick={onClick}>
      <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-200">
        <List size={18} />
      </div>
      <ResultText
        title={list.name}
        subtitle={list.description ?? t("globalSearch.listFallback")}
        meta={t("lists.booksCount", { count: list.booksCount })}
      />
    </ResultButton>
  );
}

function AuthorResultRow({ author, onClick }: { author: AuthorResult; onClick: () => void }) {
  const { t } = useTranslation();

  return (
    <ResultButton onClick={onClick}>
      <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-200">
        <UserRound size={18} />
      </div>
      <ResultText
        title={author.name}
        subtitle={t("globalSearch.authorResult")}
        meta={t("globalSearch.authorBooksCount", { count: author.booksCount })}
      />
    </ResultButton>
  );
}

function UserResultRow({ user, onClick }: { user: FollowUserDto; onClick: () => void }) {
  return (
    <ResultButton onClick={onClick}>
      <Avatar src={user.avatarUrl ?? undefined} fallback={user.username} size="md" />
      <ResultText title={`@${user.username}`} subtitle={user.bio ?? ""} meta="" />
    </ResultButton>
  );
}

function ResultButton({ onClick, children }: { onClick: () => void; children: ReactNode }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="flex w-full items-center gap-3 rounded-lg px-2.5 py-2 text-left transition-colors hover:bg-stone-100 focus:outline-none focus:ring-2 focus:ring-green-600/20 dark:hover:bg-dark-raised"
    >
      {children}
    </button>
  );
}

function ResultText({ title, subtitle, meta }: { title: string; subtitle: string; meta: string }) {
  return (
    <div className="min-w-0 flex-1">
      <p className="truncate text-sm font-medium text-stone-800 dark:text-stone-100">{title}</p>
      {subtitle && (
        <p className="truncate text-xs text-stone-500 dark:text-stone-400">{subtitle}</p>
      )}
      {meta && <p className="mt-0.5 text-xs text-stone-400 dark:text-stone-500">{meta}</p>}
    </div>
  );
}

function LoadingRow({ label }: { label: string }) {
  return (
    <MutedRow
      label={label}
      icon={<LoaderCircle size={14} className="animate-spin text-green-700 dark:text-green-400" />}
    />
  );
}

function MutedRow({ label, icon }: { label: string; icon?: ReactNode }) {
  return (
    <div className="flex min-h-10 items-center gap-2 rounded-lg px-2.5 py-2 text-xs text-stone-500 dark:text-stone-400">
      {icon}
      <span>{label}</span>
    </div>
  );
}

function SearchEmpty({ label, icon }: { label: string; icon?: ReactNode }) {
  return (
    <div className="flex min-h-48 items-center justify-center">
      <div className="text-center text-sm text-stone-500 dark:text-stone-400">
        <div
          className={cn(
            "mx-auto mb-3 flex h-10 w-10 items-center justify-center rounded-lg bg-stone-100 text-stone-400 dark:bg-dark-raised dark:text-stone-500",
            !icon && "hidden",
          )}
        >
          {icon}
        </div>
        <p>{label}</p>
      </div>
    </div>
  );
}
