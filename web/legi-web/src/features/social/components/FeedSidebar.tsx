import { useTranslation } from "react-i18next";
import { Users, TrendingUp, Sparkles } from "lucide-react";
import { Card } from "../../../components/ui/Card";
import { Button } from "../../../components/ui/Button";
import { Avatar } from "../../../components/ui/Avatar";
import { Badge } from "../../../components/ui/Badge";
import { StarRating } from "../../../components/ui/StarRating";
import type { FeedUser, TrendingBook } from "../types";

interface FeedSidebarProps {
  suggestedUsers: FeedUser[];
  trendingBooks: TrendingBook[];
  userGenres: string[];
}

export function FeedSidebar({ suggestedUsers, trendingBooks, userGenres }: FeedSidebarProps) {
  const { t } = useTranslation();

  return (
    <div className="space-y-4">
      {/* Sugestões de amigos */}
      <Card>
        <div className="p-4">
          <div className="flex items-center gap-2 text-sm font-medium text-stone-700 mb-3">
            <Users size={16} />
            {t("feed.suggestionsForYou")}
          </div>

          <div className="space-y-3">
            {suggestedUsers.map((user) => (
              <div key={user.id} className="flex items-center gap-3">
                <Avatar fallback={user.name} size="md" />
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-stone-800 truncate">{user.name}</p>
                  <p className="text-xs text-stone-500">
                    {t("feed.booksRead", { count: user.booksRead })}
                  </p>
                </div>
                <Button variant="primary" size="sm">{t("common.follow")}</Button>
              </div>
            ))}
          </div>
        </div>
      </Card>

      {/* Trending */}
      <Card>
        <div className="p-4">
          <div className="flex items-center gap-2 text-sm font-medium text-stone-700 mb-3">
            <TrendingUp size={16} />
            {t("feed.trendingThisWeek")}
          </div>

          <div className="space-y-3">
            {trendingBooks.map((book) => (
              <div key={book.id} className="flex items-center gap-3">
                <div className="w-10 h-14 bg-stone-200 rounded flex-shrink-0" />
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-stone-800 truncate">{book.title}</p>
                  <p className="text-xs text-stone-500">{book.author}</p>
                  <StarRating rating={book.rating} size={12} />
                </div>
              </div>
            ))}
          </div>
        </div>
      </Card>

      {/* Gêneros do usuário */}
      <Card>
        <div className="p-4">
          <div className="flex items-center gap-2 text-sm font-medium text-stone-700 mb-3">
            <Sparkles size={16} />
            {t("feed.yourGenres")}
          </div>
          <div className="flex flex-wrap gap-2">
            {userGenres.map((genre) => (
              <Badge key={genre} variant="outline">{genre}</Badge>
            ))}
          </div>
        </div>
      </Card>
    </div>
  );
}