import { useTranslation } from "react-i18next";
import { Heart, MessageCircle, Share2, BarChart3, CheckCircle, BookOpen } from "lucide-react";
import { Card } from "../../../components/ui/Card";
import { Avatar } from "../../../components/ui/Avatar";
import { ProgressBar } from "../../../components/ui/ProgressBar";
import { StarRating } from "../../../components/ui/StarRating";
import type { FeedPost } from "../types";

interface FeedPostCardProps {
  post: FeedPost;
}

export function FeedPostCard({ post }: FeedPostCardProps) {
  const { t } = useTranslation();

  const actionIcon = {
    progress_update: <BarChart3 size={14} className="text-green-600" />,
    finished: <CheckCircle size={14} className="text-green-600" />,
    started_reading: <BookOpen size={14} className="text-green-600" />,
  };

  const actionText = {
    progress_update: t("feed.updatedProgress"),
    finished: t("feed.finished"),
    started_reading: t("feed.startedReading"),
  };

  return (
    <Card>
      <div className="p-4">
        {/* Header: avatar + ação */}
        <div className="flex items-center gap-3 mb-3">
          <Avatar fallback={post.user.name} size="md" />
          <div>
            <p className="text-sm">
              <span className="font-semibold text-stone-800">{post.user.name}</span>
              {" "}
              <span className="inline-flex items-center gap-1 text-stone-500">
                {actionIcon[post.type]}
                {actionText[post.type]}
              </span>
              {" "}
              <span className="font-semibold text-stone-800">{post.bookTitle}</span>
            </p>
            <p className="text-xs text-stone-400">{post.createdAt}</p>
          </div>
        </div>

        {/* Conteúdo do livro */}
        <div className="flex gap-3 ml-13">
          <div className="w-16 h-22 bg-stone-200 rounded-lg flex-shrink-0" />

          <div className="flex-1">
            {/* Progresso ou Rating */}
            {post.type === "progress_update" && post.progress !== undefined && (
              <div className="mb-2">
                <div className="flex justify-between text-sm mb-1">
                  <span className="text-stone-600">{t("feed.progress")}</span>
                  <span className="font-medium text-stone-800">{post.progress}%</span>
                </div>
                <ProgressBar value={post.progress} />
              </div>
            )}

            {post.type === "finished" && post.rating !== undefined && (
              <div className="mb-2">
                <StarRating rating={post.rating} showValue={false} size={16} />
              </div>
            )}

            {/* Comentário */}
            {post.comment && (
              <p className="text-sm text-stone-600 leading-relaxed">"{post.comment}"</p>
            )}
          </div>
        </div>

        {/* Ações: like, comment, share */}
        <div className="flex items-center gap-4 mt-3 pt-3 border-t border-stone-100">
          <button className="flex items-center gap-1.5 text-sm text-stone-500 hover:text-red-500 transition-colors">
            <Heart size={16} />
            {post.likes}
          </button>
          <button className="flex items-center gap-1.5 text-sm text-stone-500 hover:text-stone-700 transition-colors">
            <MessageCircle size={16} />
            {post.comments}
          </button>
          <button className="ml-auto text-stone-400 hover:text-stone-600 transition-colors">
            <Share2 size={16} />
          </button>
        </div>
      </div>
    </Card>
  );
}