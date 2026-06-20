import { GoogleLogin } from "@react-oauth/google";
import { useMutation } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { useAuth } from "../useAuth";
import { isGoogleConfigured } from "../google";

interface GoogleSignInButtonProps {
  onSuccess: () => void;
}

export function GoogleSignInButton({ onSuccess }: GoogleSignInButtonProps) {
  const { t } = useTranslation();
  const { loginWithGoogle } = useAuth();

  const mutation = useMutation({
    mutationFn: (idToken: string) => loginWithGoogle(idToken),
    onSuccess,
  });

  if (!isGoogleConfigured()) {
    return null;
  }

  return (
    <div className="space-y-2">
      <div className="relative flex items-center">
        <div className="flex-grow border-t border-stone-200 dark:border-white/10" />
        <span className="mx-3 text-xs uppercase tracking-wide text-stone-400">{t("auth.orDivider")}</span>
        <div className="flex-grow border-t border-stone-200 dark:border-white/10" />
      </div>
      <div className="flex justify-center">
        <GoogleLogin
          text="continue_with"
          onSuccess={(credentialResponse) => {
            if (credentialResponse.credential) {
              mutation.mutate(credentialResponse.credential);
            }
          }}
          onError={() => mutation.reset()}
        />
      </div>
      {mutation.isError && (
        <p className="text-sm text-center text-red-600 dark:text-red-400">{t("auth.genericError")}</p>
      )}
    </div>
  );
}
