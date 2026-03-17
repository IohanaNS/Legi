import { useState, useMemo } from "react";
import { ProfileHeader } from "./ProfileHeader";
import { ProfileStats } from "./ProfileStats";
import { ProfileTabs } from "./ProfileTabs";
import { BookGridItem } from "./BookGridItem";
import { Card } from "../../../components/ui/Card";
import { Badge } from "../../../components/ui/Badge";
import { useTranslation } from "react-i18next";
import { Globe, Lock } from "lucide-react";
import { userProfile, userBooks, userLists } from "../data/mockProfileData";
import type { ProfileTab, ViewMode } from "../types";

export default function ProfilePage() {
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState<ProfileTab>("reading");
  const [viewMode, setViewMode] = useState<ViewMode>("grid");

  const booksByStatus = useMemo(() => ({
    reading: userBooks.filter((b) => b.status === "reading"),
    finished: userBooks.filter((b) => b.status === "finished"),
    paused: userBooks.filter((b) => b.status === "paused"),
    abandoned: userBooks.filter((b) => b.status === "abandoned"),
  }), []);

  const tabs = [
    { key: "reading" as const, labelKey: "profile.tabs.reading", count: booksByStatus.reading.length },
    { key: "finished" as const, labelKey: "profile.tabs.finished", count: booksByStatus.finished.length },
    { key: "paused" as const, labelKey: "profile.tabs.paused", count: booksByStatus.paused.length },
    { key: "abandoned" as const, labelKey: "profile.tabs.abandoned", count: booksByStatus.abandoned.length },
    { key: "lists" as const, labelKey: "profile.tabs.lists", count: userLists.length },
  ];

  const currentBooks = activeTab === "lists" ? [] : booksByStatus[activeTab] ?? [];

  return (
    <div className="max-w-3xl">
      <ProfileHeader profile={userProfile} />

      <ProfileStats
        booksRead={userProfile.stats.booksRead}
        followers={userProfile.stats.followers}
        following={userProfile.stats.following}
      />

      <ProfileTabs
        tabs={tabs}
        activeTab={activeTab}
        onTabChange={setActiveTab}
        viewMode={viewMode}
        onViewModeChange={setViewMode}
      />

      <div className="mt-4">
        {activeTab !== "lists" ? (
          <div className={
            viewMode === "grid"
              ? "grid grid-cols-4 gap-4"
              : "space-y-3"
          }>
            {currentBooks.map((book) => (
              <BookGridItem key={book.id} book={book} />
            ))}

            {currentBooks.length === 0 && (
              <p className="text-sm text-stone-400 col-span-4 text-center py-8">
                Nenhum livro nesta categoria.
              </p>
            )}
          </div>
        ) : (
          <div className="grid grid-cols-2 gap-4">
            {userLists.map((list) => (
              <Card key={list.id} className="cursor-pointer hover:border-stone-300 transition-colors">
                <div className="h-32 bg-stone-200 rounded-t-xl" />
                <div className="p-4">
                  <div className="flex items-center gap-2 mb-1">
                    <h3 className="font-semibold text-stone-800">{list.name}</h3>
                    <Badge variant={list.visibility === "public" ? "success" : "secondary"}>
                      {list.visibility === "public" ? (
                        <span className="flex items-center gap-1">
                          <Globe size={10} />
                          {t("lists.public")}
                        </span>
                      ) : (
                        <span className="flex items-center gap-1">
                          <Lock size={10} />
                          {t("lists.private")}
                        </span>
                      )}
                    </Badge>
                  </div>
                  {list.description && (
                    <p className="text-xs text-stone-500 line-clamp-2">{list.description}</p>
                  )}
                  <p className="text-xs text-stone-400 mt-2">
                    {t("lists.booksCount", { count: list.bookCount })}
                  </p>
                </div>
              </Card>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}