import { useTranslation } from "react-i18next";
import { Button } from "../../../components/ui/Button";
import { useLists } from "../hooks/useLists";
import { ListCard } from "./ListCard";

export default function ListsPage() {
  const { t } = useTranslation();
  const listsQuery = useLists();
  const lists = listsQuery.data ?? [];

  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-2xl font-bold text-stone-800">{t("lists.title")}</h1>
        <p className="mt-1 text-sm text-stone-500">
          {t("lists.count", { count: lists.length })}
        </p>
      </header>

      {listsQuery.isLoading ? (
        <ListGridSkeleton />
      ) : listsQuery.isError ? (
        <ErrorState label={t("common.couldNotLoad")} onRetry={() => listsQuery.refetch()} />
      ) : lists.length === 0 ? (
        <EmptyState label={t("lists.empty")} />
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          {lists.map((list) => (
            <ListCard key={list.listId} list={list} />
          ))}
        </div>
      )}
    </div>
  );
}

function ListGridSkeleton() {
  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
      {Array.from({ length: 4 }).map((_, index) => (
        <div
          key={index}
          className="h-28 animate-pulse rounded-xl border border-stone-200 bg-white p-4"
        >
          <div className="mb-3 h-4 w-2/3 rounded bg-stone-200" />
          <div className="h-3 w-full rounded bg-stone-200" />
          <div className="mt-2 h-3 w-1/3 rounded bg-stone-200" />
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
