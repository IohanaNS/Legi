import { RouterProvider } from "react-router-dom";
import { QueryClientProvider } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import { GoogleOAuthProvider } from "@react-oauth/google";
import { queryClient } from "./queryClient";
import { AuthProvider } from "../features/auth/AuthContext";
import { GOOGLE_CLIENT_ID, isGoogleConfigured } from "../features/auth/google";
import { router } from "./routes";

export default function App() {
  const tree = (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <RouterProvider router={router} />
      </AuthProvider>
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  );

  // Only mount the Google provider when a client id is configured, so the app
  // still boots (without Google sign-in) when the env var is unset.
  return isGoogleConfigured() ? (
    <GoogleOAuthProvider clientId={GOOGLE_CLIENT_ID}>{tree}</GoogleOAuthProvider>
  ) : (
    tree
  );
}
