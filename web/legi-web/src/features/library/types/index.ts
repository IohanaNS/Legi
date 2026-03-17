export interface UserProfile {
  id: string;
  name: string;
  username: string;
  avatarUrl?: string;
  coverUrl?: string;
  bio?: string;
  genres: string[];
  stats: {
    booksRead: number;
    followers: number;
    following: number;
  };
  isVerified?: boolean;
}

export type ReadingStatus = "not_started" | "reading" | "finished" | "abandoned" | "paused";

export type ListVisibility = "public" | "private";

export interface UserBook {
  id: string;
  bookId: string;
  title: string;
  author: string;
  coverUrl?: string;
  rating?: number;
  status: ReadingStatus;
  progress?: number;
}

export interface UserList {
  id: string;
  name: string;
  description?: string;
  visibility: ListVisibility;
  bookCount: number;
  coverUrls: string[];
}

export type ProfileTab = "reading" | "finished" | "paused" | "abandoned" | "lists";

export type ViewMode = "grid" | "list";