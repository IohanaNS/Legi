import { http } from "../../services/http";
import type {
  CommentDto,
  CreateCommentResponse,
  FeedItemDto,
  FollowUserDto,
  SocialPaginatedList,
  UserProfileDto,
} from "./types";

export type Resource = "posts" | "lists" | "reviews";

export const socialApi = {
  getUserProfile: (userId: string) =>
    http.get<UserProfileDto>(`/social/users/${userId}`).then((r) => r.data),
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

  follow: (followingId: string) => http.post("/social/follows", { followingId }),
  unfollow: (userId: string) => http.delete(`/social/follows/${userId}`),
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
