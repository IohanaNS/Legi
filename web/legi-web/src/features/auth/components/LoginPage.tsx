import { useCallback, useEffect, useState } from "react";
import { useNavigate, useLocation, Link } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { isAxiosError } from "axios";
import { useAuth } from "../useAuth";
import { Button } from "../../../components/ui/Button";
import { Card } from "../../../components/ui/Card";
import { Logo } from "../../../components/ui/Logo";
import { isTurnstileConfigured } from "../turnstile";
import { TurnstileBox } from "./TurnstileBox";

export default function LoginPage() {
  const { t } = useTranslation();
  const { isAuthenticated, isLoading, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: { pathname?: string } } | null)?.from?.pathname ?? "/feed";

  const [emailOrUsername, setEmailOrUsername] = useState("");
  const [password, setPassword] = useState("");
  const [failedAttempts, setFailedAttempts] = useState(0);
  const [turnstileRequired, setTurnstileRequired] = useState(false);
  const [turnstileToken, setTurnstileToken] = useState<string | null>(null);
  const [turnstileResetKey, setTurnstileResetKey] = useState(0);
  const turnstileEnabled = isTurnstileConfigured();
  const shouldShowTurnstile = turnstileEnabled && (turnstileRequired || failedAttempts >= 2);

  const resetTurnstile = useCallback(() => {
    setTurnstileToken(null);
    setTurnstileResetKey((current) => current + 1);
  }, []);

  const mutation = useMutation({
    mutationFn: () => login({
      emailOrUsername,
      password,
      turnstileToken: shouldShowTurnstile ? turnstileToken ?? undefined : undefined,
    }),
    onSuccess: () => {
      setFailedAttempts(0);
      setTurnstileRequired(false);
      navigate(from, { replace: true });
    },
    onError: (error) => {
      if (!isAxiosError(error)) return;

      if (error.response?.status === 403 && hasTurnstileRequired(error.response.data)) {
        setTurnstileRequired(true);
        resetTurnstile();
        return;
      }

      if (error.response?.status === 401) {
        setFailedAttempts((current) => current + 1);
        resetTurnstile();
      }
    },
  });

  useEffect(() => {
    if (!isLoading && isAuthenticated) {
      navigate(from, { replace: true });
    }
  }, [from, isAuthenticated, isLoading, navigate]);

  const errorMessage = mutation.isError
    ? isAxiosError(mutation.error) && mutation.error.response?.status === 401
      ? t("auth.invalidCredentials")
      : isAxiosError(mutation.error) && mutation.error.response?.status === 403
        ? t("auth.turnstileRequired")
      : t("auth.genericError")
    : null;

  if (isLoading) {
    return <div className="min-h-screen bg-parchment dark:bg-dark-bg" />;
  }

  return (
    <div className="min-h-screen flex items-center justify-center p-4 bg-parchment dark:bg-dark-bg">
      <Card className="w-full max-w-sm p-6 space-y-4">
        <div className="flex flex-col items-center gap-2 pb-1">
          <Logo variant="default" className="h-10 w-auto dark:hidden" />
          <Logo variant="cream" className="hidden h-10 w-auto dark:block" />
          <span className="font-serif text-2xl font-semibold text-stone-800 dark:text-stone-100">BukiHub</span>
        </div>
        <h1 className="font-serif text-xl font-semibold text-stone-800 dark:text-stone-100">{t("auth.loginTitle")}</h1>
        <form className="space-y-3" onSubmit={(e) => { e.preventDefault(); mutation.mutate(); }}>
          <input
            className="w-full rounded-md border border-stone-300 dark:border-white/20 bg-white dark:bg-white/10 px-3 py-2 text-sm text-stone-800 dark:text-stone-100 placeholder:text-stone-400 dark:placeholder:text-stone-400 focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-600 dark:focus:border-green-500 transition-colors"
            placeholder={t("auth.emailOrUsername")}
            value={emailOrUsername}
            onChange={(e) => setEmailOrUsername(e.target.value)}
            autoComplete="username"
          />
          <input
            type="password"
            className="w-full rounded-md border border-stone-300 dark:border-white/20 bg-white dark:bg-white/10 px-3 py-2 text-sm text-stone-800 dark:text-stone-100 placeholder:text-stone-400 dark:placeholder:text-stone-400 focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-600 dark:focus:border-green-500 transition-colors"
            placeholder={t("auth.password")}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="current-password"
          />
          {shouldShowTurnstile && (
            <TurnstileBox
              key={turnstileResetKey}
              action="login"
              onVerify={setTurnstileToken}
              onReset={() => setTurnstileToken(null)}
            />
          )}
          {errorMessage && <p className="text-sm text-red-600 dark:text-red-400">{errorMessage}</p>}
          <Button type="submit" disabled={mutation.isPending || (shouldShowTurnstile && !turnstileToken)} className="w-full">
            {t("auth.signIn")}
          </Button>
        </form>
        <p className="text-sm text-center text-stone-600 dark:text-stone-400">
          {t("auth.noAccount")}{" "}
          <Link to="/register" className="text-green-700 dark:text-green-400">{t("auth.signUp")}</Link>
        </p>
      </Card>
    </div>
  );
}

function hasTurnstileRequired(value: unknown): value is { captchaRequired: true } {
  return typeof value === "object" &&
    value !== null &&
    "captchaRequired" in value &&
    value.captchaRequired === true;
}
