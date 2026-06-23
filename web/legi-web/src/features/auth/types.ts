export interface LoginRequest {
  emailOrUsername: string;
  password: string;
  turnstileToken?: string;
}

export interface RegisterRequest {
  email: string;
  username: string;
  password: string;
  turnstileToken?: string;
  language?: string;
}

export interface ForgotPasswordRequest {
  email: string;
  turnstileToken?: string;
  language?: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
}

export interface ConfirmEmailRequest {
  token: string;
}

export interface ResendConfirmationRequest {
  emailOrUsername: string;
  turnstileToken?: string;
  language?: string;
}

export interface RefreshResponse {
  token: string;
  expiresAt: string;
}

export interface AuthResponse extends RefreshResponse {
  userId: string;
  email: string;
  username: string;
}

export interface RegisterResponse {
  userId: string;
  email: string;
  username: string;
  emailConfirmationRequired: true;
}

// The second-factor method a user has enrolled.
export type MfaMethod = "Totp" | "Email";

// Login returns either a session or, when MFA is enabled, a challenge to complete.
export interface MfaChallenge {
  mfaRequired: true;
  mfaToken: string;
  mfaMethod: MfaMethod;
}

export type LoginResult = AuthResponse | MfaChallenge;

export function isMfaChallenge(result: LoginResult): result is MfaChallenge {
  return (result as MfaChallenge).mfaRequired === true;
}

export interface MfaSetupResponse {
  secret: string;
  otpAuthUri: string;
}

export interface MfaConfirmResponse {
  recoveryCodes: string[];
}

export interface CurrentUserResponse {
  userId: string;
  email: string;
  username: string;
  createdAt: string;
  mfaEnabled: boolean;
  mfaMethod: "None" | MfaMethod;
}
