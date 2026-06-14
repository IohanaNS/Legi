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

export interface RefreshResponse {
  token: string;
  expiresAt: string;
}

export interface AuthResponse extends RefreshResponse {
  userId: string;
  email: string;
  username: string;
}
