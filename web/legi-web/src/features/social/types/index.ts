// ---- Social DTOs (camelCase JSON, mirror the backend) ----

// Mirrors Legi.Social.Application.Common.DTOs.UserProfileDto.
export interface UserProfileDto {
  userId: string;
  username: string;
  bio?: string | null;
  avatarUrl?: string | null;
  bannerUrl?: string | null;
  followersCount: number;
  followingCount: number;
  isFollowing: boolean;
  createdAt: string;
}

// Mirrors Legi.Social.Application.Common.DTOs.PaginatedList<T>.
// IMPORTANT: the Social API's PaginatedList is NOT the same shape as Library's.
// Social serializes { items, page, pageSize, totalItems, totalPages, hasNext, hasPrevious }
// (computed HasNext/HasPrevious), whereas Library uses
// { pageNumber, totalCount, hasNextPage, hasPreviousPage }. Keep this separate.
export interface SocialPaginatedList<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
}

// activityType is serialized via enum .ToString() (PascalCase).
// Only ProgressPosted/BookFinished/BookStarted/BookRated are produced by the
// backend today; ReviewCreated/ListCreated exist in the enum but no handler emits
// them, so they never appear in the feed. Kept for forward-compatibility.
export type ActivityType =
  | "ProgressPosted"
  | "BookFinished"
  | "BookStarted"
  | "BookAdded"
  | "BookRated"
  | "ReviewCreated"
  | "ListCreated";

// targetType is serialized via enum .ToString() (PascalCase) or null.
// In practice the backend only emits "Post" (ProgressPosted) or null; "List"
// and "Review" are reserved for future activity types.
export type TargetType = "Post" | "Review" | "List";

// Mirrors Legi.Social.Application.Common.DTOs.FeedItemDto.
export interface FeedItemDto {
  id: string;
  actorId: string;
  actorUsername: string;
  actorAvatarUrl?: string | null;
  activityType: ActivityType;
  targetType?: TargetType | null;
  referenceId: string;
  bookId?: string | null;
  bookTitle?: string | null;
  bookAuthor?: string | null;
  bookCoverUrl?: string | null;
  data?: string | null; // JSON string, parsed via lib/feed.ts
  likesCount: number;
  commentsCount: number;
  isLikedByMe: boolean;
  createdAt: string;
}

// Mirrors Legi.Social.Application.Common.DTOs.CommentDto.
export interface CommentDto {
  id: string;
  userId: string;
  username: string;
  avatarUrl?: string | null;
  content: string;
  createdAt: string;
}

// Mirrors Legi.Social.Application.Common.DTOs.FollowUserDto.
export interface FollowUserDto {
  userId: string;
  username: string;
  avatarUrl?: string | null;
  bio?: string | null;
  isFollowedByViewer: boolean;
}

// Mirrors Legi.Social.Application.Common.DTOs.CreateCommentResponse.
export interface CreateCommentResponse {
  commentId: string;
  createdAt: string;
}

// Discriminated union of FeedItemDto.data after JSON parse + discrimination by
// activityType. Fields are optional because the backend omits null keys.
// NOTE: ProgressPosted carries the RAW progress value (page number or %),
// discriminated by progressType. BookFinished/BookStarted carry no data today.
export type ActivityData =
  | { kind: "ProgressPosted"; progress?: number; progressType?: "Page" | "Percentage"; content?: string; isSpoiler?: boolean }
  | { kind: "BookFinished"; rating?: number; content?: string }
  | { kind: "BookRated"; rating?: number }
  | { kind: "BookStarted"; content?: string }
  | { kind: "BookAdded" }
  | { kind: "ReviewCreated"; rating?: number; content?: string; isSpoiler?: boolean }
  | { kind: "ListCreated"; name?: string; description?: string };
