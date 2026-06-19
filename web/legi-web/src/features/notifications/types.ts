import type { TargetType } from "../social/types";

// notificationType is serialized via enum .ToString() (PascalCase).
export type NotificationType = "Like" | "Comment";

// Mirrors Legi.Social.Application.Common.DTOs.NotificationDto.
export interface NotificationDto {
  id: string;
  actorId: string;
  actorUsername: string;
  actorAvatarUrl?: string | null;
  notificationType: NotificationType;
  // What content was reacted to (Post / Review / List).
  targetType: TargetType;
  // Id of the content reacted to — the deep-link target.
  targetId: string;
  // Comment text preview; null for like notifications.
  commentPreview?: string | null;
  isRead: boolean;
  readAt?: string | null;
  createdAt: string;
}

// Mirrors the response of GET /social/notifications/unread-count.
export interface UnreadCountResponse {
  count: number;
}
