import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { Bell, Heart, MessageCircle } from "lucide-react";
import { Avatar } from "../../../components/ui/Avatar";
import { relativeTime } from "../../social/lib/time";
import { useAuth } from "../../auth/useAuth";
import {
  useMarkAllNotificationsRead,
  useMarkNotificationRead,
  useNotifications,
  useUnreadNotificationCount,
} from "../hooks";
import type { NotificationDto } from "../types";

// Deep-link target for a notification. Lists have their own page; posts and
// reviews are viewable on the owner's (the recipient's) activity page, since the
// app has no standalone post/review route.
function notificationTarget(n: NotificationDto, myUserId: string | undefined): string {
  if (n.targetType === "List") return `/lists/${n.targetId}`;
  return myUserId ? `/users/${myUserId}` : "/feed";
}

const MESSAGE_KEY: Record<string, string> = {
  "Like:Post": "notifications.likedPost",
  "Like:Review": "notifications.likedReview",
  "Like:List": "notifications.likedList",
  "Comment:Post": "notifications.commentedPost",
  "Comment:Review": "notifications.commentedReview",
  "Comment:List": "notifications.commentedList",
};

export function NotificationBell() {
  const { t } = useTranslation();
  const { user } = useAuth();
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  const unreadCount = useUnreadNotificationCount();
  const notifications = useNotifications(open);
  const markRead = useMarkNotificationRead();
  const markAllRead = useMarkAllNotificationsRead();

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    }
    if (open) document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [open]);

  const items = notifications.data?.pages.flatMap((p) => p.items) ?? [];
  const count = unreadCount.data ?? 0;

  const handleClickItem = (n: NotificationDto) => {
    if (!n.isRead) markRead.mutate(n.id);
    setOpen(false);
  };

  return (
    <div className="relative px-3 mb-2" ref={ref}>
      <button
        onClick={() => setOpen((o) => !o)}
        aria-label={t("notifications.title")}
        className="relative flex w-full items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium text-green-200 hover:bg-white/10 hover:text-white transition-colors"
      >
        <span className="relative">
          <Bell size={18} />
          {count > 0 && (
            <span className="absolute -top-2 -right-2 min-w-4 h-4 px-1 flex items-center justify-center rounded-full bg-red-500 text-[10px] font-semibold text-white">
              {count > 9 ? "9+" : count}
            </span>
          )}
        </span>
        {t("notifications.title")}
      </button>

      {open && (
        <div className="absolute top-full left-2 right-2 mt-1 max-h-[28rem] flex flex-col rounded-xl bg-forest-900 dark:bg-dark-card border border-white/10 shadow-2xl overflow-hidden z-50">
          <div className="flex items-center justify-between px-4 py-3 border-b border-white/10">
            <p className="text-sm font-semibold text-white">{t("notifications.title")}</p>
            {count > 0 && (
              <button
                onClick={() => markAllRead.mutate()}
                disabled={markAllRead.isPending}
                className="text-xs text-green-400 hover:text-white transition-colors disabled:opacity-50"
              >
                {t("notifications.markAllAsRead")}
              </button>
            )}
          </div>

          <div className="flex-1 overflow-y-auto">
            {notifications.isLoading ? (
              <p className="px-4 py-6 text-center text-sm text-green-400">{t("common.loading")}</p>
            ) : items.length === 0 ? (
              <p className="px-4 py-6 text-center text-sm text-green-400">{t("notifications.empty")}</p>
            ) : (
              items.map((n) => (
                <Link
                  key={n.id}
                  to={notificationTarget(n, user?.userId)}
                  onClick={() => handleClickItem(n)}
                  className={`flex gap-3 px-4 py-3 border-b border-white/5 transition-colors hover:bg-white/10 ${
                    n.isRead ? "" : "bg-white/5"
                  }`}
                >
                  <div className="relative shrink-0">
                    <Avatar
                      src={n.actorAvatarUrl ?? undefined}
                      fallback={n.actorUsername}
                      size="sm"
                    />
                    <span className="absolute -bottom-1 -right-1 flex h-4 w-4 items-center justify-center rounded-full bg-forest-900 dark:bg-dark-card">
                      {n.notificationType === "Like" ? (
                        <Heart size={11} className="fill-red-500 text-red-500" />
                      ) : (
                        <MessageCircle size={11} className="text-green-400" />
                      )}
                    </span>
                  </div>

                  <div className="min-w-0 flex-1">
                    <p className="text-sm text-green-100">
                      <span className="font-semibold text-white">@{n.actorUsername}</span>{" "}
                      {t(MESSAGE_KEY[`${n.notificationType}:${n.targetType}`] ?? "notifications.likedPost")}
                    </p>
                    {n.commentPreview && (
                      <p className="mt-0.5 truncate text-xs italic text-green-300">
                        "{n.commentPreview}"
                      </p>
                    )}
                    <p className="mt-0.5 text-xs text-green-500">{relativeTime(n.createdAt, t)}</p>
                  </div>

                  {!n.isRead && (
                    <span className="mt-1 h-2 w-2 shrink-0 rounded-full bg-green-400" />
                  )}
                </Link>
              ))
            )}

            {notifications.hasNextPage && (
              <button
                onClick={() => notifications.fetchNextPage()}
                disabled={notifications.isFetchingNextPage}
                className="w-full px-4 py-3 text-center text-xs text-green-400 hover:text-white transition-colors disabled:opacity-50"
              >
                {t("common.loadMore")}
              </button>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
