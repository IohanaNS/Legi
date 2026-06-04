export interface LoginRequest {
  emailOrUsername: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  username: string;
  password: string;
}

// Mirrors Identity RefreshTokenResponse: { token, refreshToken, expiresAt }.
export interface RefreshResponse {
  token: string;
  refreshToken: string;
  expiresAt: string;
}

// Mirrors Identity Login/RegisterResponse: refresh fields + user identity.
export interface AuthResponse extends RefreshResponse {
  userId: string;
  email: string;
  username: string;
}
