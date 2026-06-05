import { Filter } from "lucide-react";
import { useTranslation } from "react-i18next";
import { Button } from "../../../components/ui/Button";
import { cn } from "../../../lib/utils";
import type { TagResult } from "../types";

interface TagFilterProps {
  tags: TagResult[];
  selectedTagSlug?: string;
  isLoading: boolean;
  isError: boolean;
  onRetry: () => void;
  onToggleTag: (tagSlug: string) => void;
}

export function TagFilter({
  tags,
  selectedTagSlug,
  isLoading,
  isError,
  onRetry,
  onToggleTag,
}: TagFilterProps) {
  const { t } = useTranslation();

  return (
    <div>
      <div className="mb-3 flex items-center gap-2 text-sm text-stone-600">
        <Filter size={16} />
        {t("explore.filterByTag")}
      </div>

      {isLoading ? (
        <div className="flex flex-wrap gap-2">
          {Array.from({ length: 8 }).map((_, index) => (
            <div key={index} className="h-8 w-20 animate-pulse rounded-full bg-stone-200" />
          ))}
        </div>
      ) : isError ? (
        <div className="flex items-center gap-3">
          <p className="text-sm text-stone-500">{t("explore.tagsError")}</p>
          <Button variant="outline" size="sm" onClick={onRetry}>
            {t("common.retry")}
          </Button>
        </div>
      ) : tags.length > 0 ? (
        <div className="flex flex-wrap gap-2">
          {tags.map((tag) => {
            const isSelected = selectedTagSlug === tag.slug;
            return (
              <button
                key={tag.slug}
                type="button"
                onClick={() => onToggleTag(tag.slug)}
                className={cn(
                  "rounded-full border px-3 py-1.5 text-sm transition-colors",
                  isSelected
                    ? "border-green-700 bg-green-700 text-white"
                    : "border-stone-300 bg-white text-stone-600 hover:border-stone-400",
                )}
              >
                {tag.name}
              </button>
            );
          })}
        </div>
      ) : null}
    </div>
  );
}
