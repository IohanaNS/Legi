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
    <div className="flex items-center justify-between border-b border-stone-200">
      <div className="flex">
        {tabs.map((tab) => (
          <button
            key={tab.key}
            onClick={() => onTabChange(tab.key)}
            className={cn(
              "flex items-center gap-1.5 px-4 py-3 text-sm font-medium border-b-2 transition-colors cursor-pointer",
              activeTab === tab.key
                ? "border-green-700 text-green-700"
                : "border-transparent text-stone-500 hover:text-stone-700"
            )}
          >
            {t(tab.labelKey)}
            <span
              className={cn(
                "text-xs px-1.5 py-0.5 rounded-full",
                activeTab === tab.key
                  ? "bg-green-100 text-green-700"
                  : "bg-stone-100 text-stone-500"
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
            viewMode === "grid" ? "text-stone-800 bg-stone-100" : "text-stone-400 hover:text-stone-600"
          )}
        >
          <LayoutGrid size={18} />
        </button>
        <button
          onClick={() => onViewModeChange("list")}
          className={cn(
            "p-1.5 rounded transition-colors cursor-pointer",
            viewMode === "list" ? "text-stone-800 bg-stone-100" : "text-stone-400 hover:text-stone-600"
          )}
        >
          <List size={18} />
        </button>
      </div>
    </div>
  );
}