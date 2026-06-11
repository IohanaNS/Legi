import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import { useNavigate, useSearchParams } from "react-router-dom";
import { Plus, Search } from "lucide-react";
import { Button } from "../../../components/ui/Button";
import { useLists } from "../hooks/useLists";
import { useDeleteList } from "../hooks/useListMutations";
import type { UserListSummaryDto } from "../types";
import { ListCard } from "./ListCard";
import { useAuth } from "../../auth/useAuth";
import { FollowedListsSection } from "../../social/components/FollowedListsSection";

const EMPTY_LISTS: UserListSummaryDto[] = [];

export default function ListsPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { user } = useAuth();
  const [searchParams, setSearchParams] = useSearchParams();
  const listsQuery = useLists();
  const deleteList = useDeleteList();
  const lists = listsQuery.data ?? EMPTY_LISTS;
  const searchInput = searchParams.get("search") ?? "";
  const normalizedSearch = searchInput.trim().toLowerCase();
  const hasSearch = normalizedSearch.length > 0;
  const visibleLists = useMemo(() => {
    if (!hasSearch) return lists;

    return lists.filter((list) => {
      const name = list.name.toLowerCase();
      const description = list.description?.toLowerCase() ?? "";
      return name.includes(normalizedSearch) || description.includes(normalizedSearch);
    });
  }, [hasSearch, lists, normalizedSearch]);

  const handleSearchChange = (value: string) => {
    const nextParams = new URLSearchParams(searchParams);
    if (value.trim()) {
      nextParams.set("search", value);
    } else {
      nextParams.delete("search");
    }

    setSearchParams(nextParams, { replace: true });
  };

  const handleDelete = (list: UserListSummaryDto) => {
    if (window.confirm(t("lists.confirmDelete", { name: list.name }))) {
      deleteList.mutate(list.listId);
    }
  };

  return (
    <div className="space-y-6">
      <header className="flex items-start justify-between gap-4">
        <div>
          <h1 className="font-serif text-[1.5rem] font-semibold leading-tight text-stone-800 dark:text-stone-100">
            {t("lists.title")}
          </h1>
          <p className="mt-1 text-sm text-stone-500 dark:text-stone-400">
            {hasSearch
              ? t("lists.filteredCount", { count: visibleLists.length, total: lists.length })
              : t("lists.count", { count: lists.length })}
          </p>
        </div>
        <Button onClick={() => navigate("/lists/new")} className="shrink-0">
          <span className="flex items-center gap-1.5">
            <Plus size={16} />
            {t("lists.create")}
          </span>
        </Button>
      </header>

      <div className="relative max-w-xl">
        <Search
          size={18}
          className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-stone-400 dark:text-stone-500"
        />
        <input
          type="search"
          value={searchInput}
          onChange={(event) => handleSearchChange(event.target.value)}
          placeholder={t("lists.searchPlaceholder")}
          className="w-full rounded-lg border border-stone-200 bg-white py-2.5 pl-10 pr-4 text-sm text-stone-800 placeholder:text-stone-400 transition-colors focus:border-green-600 focus:outline-none focus:ring-2 focus:ring-green-600/20 dark:border-dark-raised dark:bg-dark-card dark:text-stone-100 dark:placeholder:text-stone-500"
        />
      </div>

      {listsQuery.isLoading ? (
        <ListGridSkeleton />
      ) : listsQuery.isError ? (
        <ErrorState label={t("common.couldNotLoad")} onRetry={() => listsQuery.refetch()} />
      ) : visibleLists.length === 0 ? (
        <EmptyState label={hasSearch ? t("lists.emptySearch") : t("lists.empty")} />
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {visibleLists.map((list) => (
            <ListCard
              key={list.listId}
              list={list}
              onEdit={(l) => navigate(`/lists/${l.listId}/edit`)}
              onDelete={handleDelete}
            />
          ))}
        </div>
      )}

      <FollowedListsSection
        userId={user?.userId}
        unfollowAsUserId={user?.userId}
        searchTerm={searchInput}
      />
    </div>
  );
}

function ListGridSkeleton() {
  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {Array.from({ length: 6 }).map((_, index) => (
        <div
          key={index}
          className="h-56 animate-pulse rounded-xl border border-stone-200 dark:border-dark-raised bg-white dark:bg-dark-card"
        >
          <div className="aspect-[2/1] rounded-t-xl bg-stone-200 dark:bg-dark-raised" />
          <div className="p-4">
            <div className="mb-3 h-4 w-2/3 rounded bg-stone-200 dark:bg-dark-raised" />
            <div className="h-3 w-full rounded bg-stone-200 dark:bg-dark-raised" />
          </div>
        </div>
      ))}
    </div>
  );
}

function EmptyState({ label }: { label: string }) {
  return <p className="py-10 text-center text-sm text-stone-400">{label}</p>;
}

function ErrorState({ label, onRetry }: { label: string; onRetry: () => void }) {
  const { t } = useTranslation();

  return (
    <div className="py-10 text-center">
      <p className="mb-3 text-sm text-stone-500">{label}</p>
      <Button variant="outline" size="sm" onClick={onRetry}>
        {t("common.retry")}
      </Button>
    </div>
  );
}
