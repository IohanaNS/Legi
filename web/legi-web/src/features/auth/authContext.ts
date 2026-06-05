import { createContext } from "react";
import type { StoredUser } from "../../services/authStorage";
import type { LoginRequest, RegisterRequest } from "./types";

export interface AuthContextValue {
  user: StoredUser | null;
  isAuthenticated: boolean;
  login: (body: LoginRequest) => Promise<void>;
  register: (body: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
}

export const AuthContext = createContext<AuthContextValue | null>(null);
