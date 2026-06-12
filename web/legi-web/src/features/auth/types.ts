export interface LoginRequest {
  emailOrUsername: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  username: string;
  password: string;
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
