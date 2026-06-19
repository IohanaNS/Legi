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
