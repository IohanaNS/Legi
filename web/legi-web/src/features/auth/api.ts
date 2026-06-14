import { http, refreshSession } from "../../services/http";
import type {
  AuthResponse,
  ForgotPasswordRequest,
  LoginRequest,
  RegisterRequest,
  ResetPasswordRequest,
} from "./types";

export const authApi = {
  login: (body: LoginRequest) =>
    http.post<AuthResponse>("/identity/auth/login", body).then((r) => r.data),
  register: (body: RegisterRequest) =>
    http.post<AuthResponse>("/identity/auth/register", body).then((r) => r.data),
  forgotPassword: (body: ForgotPasswordRequest) =>
    http.post("/identity/auth/forgot-password", body),
  resetPassword: (body: ResetPasswordRequest) =>
    http.post("/identity/auth/reset-password", body),
  refresh: () => refreshSession(),
  logout: () =>
    http.post("/identity/auth/logout"),
  deleteAccount: () =>
    http.delete("/identity/users/me"),
};
