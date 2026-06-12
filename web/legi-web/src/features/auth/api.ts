import { http, refreshSession } from "../../services/http";
import type { AuthResponse, LoginRequest, RegisterRequest } from "./types";

export const authApi = {
  login: (body: LoginRequest) =>
    http.post<AuthResponse>("/identity/auth/login", body).then((r) => r.data),
  register: (body: RegisterRequest) =>
    http.post<AuthResponse>("/identity/auth/register", body).then((r) => r.data),
  refresh: () => refreshSession(),
  logout: () =>
    http.post("/identity/auth/logout"),
  deleteAccount: () =>
    http.delete("/identity/users/me"),
};
