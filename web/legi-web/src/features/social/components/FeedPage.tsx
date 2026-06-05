import { useTranslation } from "react-i18next";
import { CurrentlyReading } from "./CurrentlyReading";
import { FeedPostCard } from "./FeedPostCard";
import { FeedSidebar } from "./FeedSidebar";
import { useAuth } from "../../auth/useAuth";
import {
  currentlyReading,
  feedPosts,
  suggestedUsers,
  trendingBooks,
  userGenres,
} from "../data/mockFeedData";

export default function FeedPage() {
  const { t } = useTranslation();
  const { user } = useAuth();

  return (
    <div className="flex gap-6">
      {/* Coluna principal */}
      <div className="flex-1 space-y-4">
        <div>
          <h1 className="text-2xl font-bold text-stone-800">
            {t("feed.greeting", { username: user?.username ?? "" })}
          </h1>
          <p className="text-stone-500 mt-1">{t("feed.subtitle")}</p>
        </div>

        <CurrentlyReading
          bookTitle={currentlyReading.bookTitle}
          bookAuthor={currentlyReading.bookAuthor}
          progress={currentlyReading.progress}
          currentPage={currentlyReading.currentPage}
          totalPages={currentlyReading.totalPages}
        />

        {feedPosts.map((post) => (
          <FeedPostCard key={post.id} post={post} />
        ))}
      </div>

      {/* Sidebar direita */}
      <aside className="w-72 flex-shrink-0 hidden lg:block">
        <FeedSidebar
          suggestedUsers={suggestedUsers}
          trendingBooks={trendingBooks}
          userGenres={userGenres}
        />
      </aside>
    </div>
  );
}
