import { http } from "../../services/http";
import type { UserProfileDto } from "./types";

export const socialApi = {
  getUserProfile: (userId: string) =>
    http.get<UserProfileDto>(`/social/users/${userId}`).then((r) => r.data),
};
