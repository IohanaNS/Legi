import { useTranslation } from "react-i18next";
import { LayoutGrid, List } from "lucide-react";
import { cn } from "../../../lib/utils";
import type { ProfileTab, ViewMode } from "../types";

interface TabDefinition {
  key: ProfileTab;
  labelKey: string;
  count: number;
}

interface ProfileTabsProps {
  tabs: TabDefinition[];
  activeTab: ProfileTab;
  onTabChange: (tab: ProfileTab) => void;
  viewMode: ViewMode;
  onViewModeChange: (mode: ViewMode) => void;
}

export function ProfileTabs({
  tabs,
  activeTab,
  onTabChange,
  viewMode,
  onViewModeChange,
}: ProfileTabsProps) {
  const { t } = useTranslation();

  return (
    <div className="flex items-center justify-between border-b border-stone-200 dark:border-dark-raised">
      <div className="flex">
        {tabs.map((tab) => (
          <button
            key={tab.key}
            onClick={() => onTabChange(tab.key)}
            className={cn(
              "flex items-center gap-1.5 px-4 py-3 text-sm font-medium border-b-2 transition-colors cursor-pointer",
              activeTab === tab.key
                ? "border-green-600 text-green-600 dark:text-green-400 dark:border-green-400"
                : "border-transparent text-stone-500 dark:text-stone-400 hover:text-stone-700 dark:hover:text-stone-200"
            )}
          >
            {t(tab.labelKey)}
            <span
              className={cn(
                "text-xs px-1.5 py-0.5 rounded-full",
                activeTab === tab.key
                  ? "bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-400"
                  : "bg-stone-100 text-stone-500 dark:bg-dark-raised dark:text-stone-400"
              )}
            >
              {tab.count}
            </span>
          </button>
        ))}
      </div>

      <div className="flex items-center gap-1 pr-2">
        <button
          onClick={() => onViewModeChange("grid")}
          className={cn(
            "p-1.5 rounded transition-colors cursor-pointer",
            viewMode === "grid" ? "text-stone-800 dark:text-stone-100 bg-stone-100 dark:bg-dark-raised" : "text-stone-400 dark:text-stone-500 hover:text-stone-600 dark:hover:text-stone-300"
          )}
        >
          <LayoutGrid size={18} />
        </button>
        <button
          onClick={() => onViewModeChange("list")}
          className={cn(
            "p-1.5 rounded transition-colors cursor-pointer",
            viewMode === "list" ? "text-stone-800 dark:text-stone-100 bg-stone-100 dark:bg-dark-raised" : "text-stone-400 dark:text-stone-500 hover:text-stone-600 dark:hover:text-stone-300"
          )}
        >
          <List size={18} />
        </button>
      </div>
    </div>
  );
}