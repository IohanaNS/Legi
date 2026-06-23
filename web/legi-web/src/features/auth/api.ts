import { http, refreshSession } from "../../services/http";
import type {
  AccountDeletionChallengeRequest,
  AccountDeletionChallengeResponse,
  AuthResponse,
  ConfirmEmailRequest,
  CurrentUserResponse,
  ForgotPasswordRequest,
  LoginRequest,
  LoginResult,
  MfaConfirmResponse,
  MfaSetupResponse,
  RegisterRequest,
  RegisterResponse,
  ResendConfirmationRequest,
  ResetPasswordRequest,
} from "./types";

export const authApi = {
  login: (body: LoginRequest) =>
    http.post<LoginResult>("/identity/auth/login", body).then((r) => r.data),
  mfaLogin: (mfaToken: string, code: string) =>
    http.post<AuthResponse>("/identity/auth/mfa-login", { mfaToken, code }).then((r) => r.data),
  // Login challenge: email/re-send the one-time code for the email-MFA method.
  sendMfaEmailCode: (mfaToken: string, language?: string) =>
    http.post("/identity/auth/mfa-email/send", { mfaToken, language }),
  mfaSetup: () =>
    http.post<MfaSetupResponse>("/identity/mfa/setup").then((r) => r.data),
  mfaConfirm: (code: string) =>
    http.post<MfaConfirmResponse>("/identity/mfa/confirm", { code }).then((r) => r.data),
  // Email-MFA enrollment: emails a code, then confirms it (returns recovery codes).
  mfaEmailSetup: (language?: string) =>
    http.post("/identity/mfa/email/setup", { language }),
  mfaEmailConfirm: (code: string) =>
    http.post<MfaConfirmResponse>("/identity/mfa/email/confirm", { code }).then((r) => r.data),
  mfaDisable: (code: string) =>
    http.post("/identity/mfa/disable", { code }),
  getCurrentUser: () =>
    http.get<CurrentUserResponse>("/identity/users/me").then((r) => r.data),
  googleSignIn: (idToken: string) =>
    http.post<AuthResponse>("/identity/auth/google", { idToken }).then((r) => r.data),
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
  sendAccountDeletionEmailCode: (language?: string) =>
    http.post("/identity/users/me/deletion-email-code", { language }),
  createAccountDeletionChallenge: (body: AccountDeletionChallengeRequest) =>
    http.post<AccountDeletionChallengeResponse>("/identity/users/me/deletion-challenge", body)
      .then((r) => r.data),
  deleteAccount: (deletionToken: string) =>
    http.delete("/identity/users/me", { data: { deletionToken } }),
};
