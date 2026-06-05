export interface FeedUser {
  id: string;
  name: string;
  username: string;
  avatarUrl?: string;
  booksRead?: number;
}

export type FeedPostType = "progress_update" | "finished" | "started_reading";

export interface FeedPost {
  id: string;
  user: FeedUser;
  type: FeedPostType;
  bookTitle: string;
  bookAuthor: string;
  bookCoverUrl?: string;
  progress?: number;
  totalPages?: number;
  currentPage?: number;
  rating?: number;
  comment?: string;
  likes: number;
  comments: number;
  createdAt: string;
}

export interface TrendingBook {
  id: string;
  title: string;
  author: string;
  coverUrl?: string;
  rating: number;
}

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