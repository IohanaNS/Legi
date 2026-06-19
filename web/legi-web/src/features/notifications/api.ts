import { http } from "../../services/http";
import type { SocialPaginatedList } from "../social/types";
import type { NotificationDto, UnreadCountResponse } from "./types";

export const notificationsApi = {
  getNotifications: (page: number, pageSize: number) =>
    http
      .get<SocialPaginatedList<NotificationDto>>("/social/notifications", {
        params: { page, pageSize },
      })
      .then((r) => r.data),

  getUnreadCount: () =>
    http
      .get<UnreadCountResponse>("/social/notifications/unread-count")
      .then((r) => r.data),

  markAsRead: (notificationId: string) =>
    http.put(`/social/notifications/${notificationId}/read`),

  markAllAsRead: () => http.put("/social/notifications/read-all"),
};
