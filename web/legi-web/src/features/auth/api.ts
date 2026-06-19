import { http, refreshSession } from "../../services/http";
import type {
  AuthResponse,
  ConfirmEmailRequest,
  ForgotPasswordRequest,
  LoginRequest,
  RegisterRequest,
  RegisterResponse,
  ResendConfirmationRequest,
  ResetPasswordRequest,
} from "./types";

export const authApi = {
  login: (body: LoginRequest) =>
    http.post<AuthResponse>("/identity/auth/login", body).then((r) => r.data),
  register: (body: RegisterRequest) =>
    http.post<RegisterResponse>("/identity/auth/register", body).then((r) => r.data),
  forgotPassword: (body: ForgotPasswordRequest) =>
    http.post("/identity/auth/forgot-password", body),
  resetPassword: (body: ResetPasswordRequest) =>
    http.post("/identity/auth/reset-password", body),
  confirmEmail: (body: ConfirmEmailRequest) =>
    http.post("/identity/auth/confirm-email", body),
  resendConfirmation: (body: ResendConfirmationRequest) =>
    http.post("/identity/auth/resend-confirmation", body),
  refresh: () => refreshSession(),
  logout: () =>
    http.post("/identity/auth/logout"),
  deleteAccount: () =>
    http.delete("/identity/users/me"),
};
