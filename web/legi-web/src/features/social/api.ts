import { http } from "../../services/http";
import type {
  CommentDto,
  CreateCommentResponse,
  FeedItemDto,
  FollowedListDto,
  FollowUserDto,
  ListSocialStateDto,
  ProfileImageUploadResponse,
  SocialPaginatedList,
  UserProfileDto,
} from "./types";

export type Resource = "posts" | "lists" | "reviews";

export const socialApi = {
  getUserProfile: (userId: string) =>
    http.get<UserProfileDto>(`/social/users/${userId}`).then((r) => r.data),
  uploadProfileAvatar: (file: File) => {
    const formData = new FormData();
    formData.append("file", file);
    return http
      .post<ProfileImageUploadResponse>("/social/users/me/avatar", formData)
      .then((r) => r.data);
  },
  uploadProfileBanner: (file: File) => {
    const formData = new FormData();
    formData.append("file", file);
    return http
      .post<ProfileImageUploadResponse>("/social/users/me/banner", formData)
      .then((r) => r.data);
  },
  searchUsers: (usernamePrefix: string, limit = 10) =>
    http
      .get<FollowUserDto[]>("/social/users/search", { params: { usernamePrefix, limit } })
      .then((r) => r.data),

  getFeed: (page: number, pageSize: number) =>
    http
      .get<SocialPaginatedList<FeedItemDto>>("/social/feed", { params: { page, pageSize } })
      .then((r) => r.data),

  getUserActivity: (userId: string, page: number, pageSize: number) =>
    http
      .get<SocialPaginatedList<FeedItemDto>>(`/social/users/${userId}/activity`, {
        params: { page, pageSize },
      })
      .then((r) => r.data),

  getBookReviews: (bookId: string, page: number, pageSize: number) =>
    http
      .get<SocialPaginatedList<FeedItemDto>>(`/social/books/${bookId}/reviews`, {
        params: { page, pageSize },
      })
      .then((r) => r.data),

  deleteFeedItem: (feedItemId: string) => http.delete(`/social/feed/${feedItemId}`),

  like: (resource: Resource, id: string) => http.post(`/social/${resource}/${id}/likes`),
  unlike: (resource: Resource, id: string) => http.delete(`/social/${resource}/${id}/likes`),

  getComments: (resource: Resource, id: string, page: number, pageSize: number) =>
    http
      .get<SocialPaginatedList<CommentDto>>(`/social/${resource}/${id}/comments`, {
        params: { page, pageSize },
      })
      .then((r) => r.data),
  addComment: (resource: Resource, id: string, content: string) =>
    http
      .post<CreateCommentResponse>(`/social/${resource}/${id}/comments`, { content })
      .then((r) => r.data),
  deleteComment: (commentId: string) => http.delete(`/social/comments/${commentId}`),

  follow: (followingId: string) => http.post("/social/follows", { followingId }),
  unfollow: (userId: string) => http.delete(`/social/follows/${userId}`),

  // List-specific social state + follow (distinct from user-to-user follow).
  getListSocialState: (listId: string) =>
    http.get<ListSocialStateDto>(`/social/lists/${listId}`).then((r) => r.data),
  followList: (listId: string) => http.post(`/social/lists/${listId}/follows`),
  unfollowList: (listId: string) => http.delete(`/social/lists/${listId}/follows`),
  // The lists a user follows (ids + followedAt only — hydrate via Library by-ids).
  getFollowedLists: (userId: string, page: number, pageSize: number) =>
    http
      .get<SocialPaginatedList<FollowedListDto>>(`/social/users/${userId}/followed-lists`, {
        params: { page, pageSize },
      })
      .then((r) => r.data),
  getFollowers: (userId: string, page: number, pageSize: number) =>
    http
      .get<SocialPaginatedList<FollowUserDto>>(`/social/users/${userId}/followers`, {
        params: { page, pageSize },
      })
      .then((r) => r.data),
  getFollowing: (userId: string, page: number, pageSize: number) =>
    http
      .get<SocialPaginatedList<FollowUserDto>>(`/social/users/${userId}/following`, {
        params: { page, pageSize },
      })
      .then((r) => r.data),
};
