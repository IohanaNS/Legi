import { createContext } from "react";
import type { StoredUser } from "../../services/authStorage";
import type { LoginRequest, MfaMethod, RegisterRequest, RegisterResponse } from "./types";

export interface AuthContextValue {
  user: StoredUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (body: LoginRequest) => Promise<{ mfaRequired: boolean; mfaToken?: string; mfaMethod?: MfaMethod }>;
  completeMfaLogin: (mfaToken: string, code: string) => Promise<void>;
  sendMfaEmailCode: (mfaToken: string, language?: string) => Promise<void>;
  loginWithGoogle: (idToken: string) => Promise<void>;
  register: (body: RegisterRequest) => Promise<RegisterResponse>;
  logout: () => Promise<void>;
  deleteAccount: (deletionToken: string) => Promise<void>;
}

export const AuthContext = createContext<AuthContextValue | null>(null);
