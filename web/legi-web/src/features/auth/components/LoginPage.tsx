import { useCallback, useEffect, useState } from "react";
import { useNavigate, useLocation, Link } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { isAxiosError } from "axios";
import { authApi } from "../api";
import { useAuth } from "../useAuth";
import { Button } from "../../../components/ui/Button";
import { Card } from "../../../components/ui/Card";
import { Logo } from "../../../components/ui/Logo";
import { LanguageToggle } from "../../../components/ui/LanguageToggle";
import type { MfaMethod } from "../types";
import { isTurnstileConfigured } from "../turnstile";
import { TurnstileBox } from "./TurnstileBox";
import { GoogleSignInButton } from "./GoogleSignInButton";

export default function LoginPage() {
  const { t, i18n } = useTranslation();
  const { isAuthenticated, isLoading, login, completeMfaLogin, sendMfaEmailCode } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: { pathname?: string } } | null)?.from?.pathname ?? "/feed";

  const [emailOrUsername, setEmailOrUsername] = useState("");
  const [password, setPassword] = useState("");
  const [failedAttempts, setFailedAttempts] = useState(0);
  const [turnstileRequired, setTurnstileRequired] = useState(false);
  const [turnstileToken, setTurnstileToken] = useState<string | null>(null);
  const [turnstileResetKey, setTurnstileResetKey] = useState(0);
  const [emailConfirmationRequired, setEmailConfirmationRequired] = useState(false);
  const [mfaToken, setMfaToken] = useState<string | null>(null);
  const [mfaMethod, setMfaMethod] = useState<MfaMethod | null>(null);
  const [mfaCode, setMfaCode] = useState("");
  const [resendCooldown, setResendCooldown] = useState(0);
  const [resendTurnstileRequired, setResendTurnstileRequired] = useState(false);
  const [resendTurnstileToken, setResendTurnstileToken] = useState<string | null>(null);
  const [resendTurnstileResetKey, setResendTurnstileResetKey] = useState(0);
  const turnstileEnabled = isTurnstileConfigured();
  const shouldShowTurnstile = turnstileEnabled && (turnstileRequired || failedAttempts >= 2);
  const shouldShowResendTurnstile = turnstileEnabled && resendTurnstileRequired;

  const resetTurnstile = useCallback(() => {
    setTurnstileToken(null);
    setTurnstileResetKey((current) => current + 1);
  }, []);

  const resetResendTurnstile = useCallback(() => {
    setResendTurnstileToken(null);
    setResendTurnstileResetKey((current) => current + 1);
  }, []);

  const mutation = useMutation({
    mutationFn: () => login({
      emailOrUsername,
      password,
      turnstileToken: shouldShowTurnstile ? turnstileToken ?? undefined : undefined,
    }),
    onMutate: () => {
      setEmailConfirmationRequired(false);
    },
    onSuccess: (result) => {
      setFailedAttempts(0);
      setTurnstileRequired(false);
      if (result.mfaRequired && result.mfaToken) {
        setMfaToken(result.mfaToken);
        setMfaMethod(result.mfaMethod ?? "Totp");
        // Email method: the user has no code yet, so request one immediately.
        if (result.mfaMethod === "Email") {
          emailCodeMutation.mutate(result.mfaToken);
        }
        return;
      }
      navigate(from, { replace: true });
    },
    onError: (error) => {
      if (!isAxiosError(error)) return;

      if (error.response?.status === 403 && hasEmailConfirmationRequired(error.response.data)) {
        setEmailConfirmationRequired(true);
        setTurnstileRequired(false);
        resetTurnstile();
        return;
      }

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

  const resendMutation = useMutation({
    mutationFn: () => authApi.resendConfirmation({
      emailOrUsername,
      turnstileToken: shouldShowResendTurnstile ? resendTurnstileToken ?? undefined : undefined,
      language: i18n.language,
    }),
    onSuccess: () => {
      setResendTurnstileRequired(false);
      setResendTurnstileToken(null);
    },
    onError: (error) => {
      if (isAxiosError(error) && error.response?.status === 403 && hasTurnstileRequired(error.response.data)) {
        setResendTurnstileRequired(true);
        resetResendTurnstile();
        return;
      }

      resetResendTurnstile();
    },
  });

  const mfaMutation = useMutation({
    mutationFn: () => completeMfaLogin(mfaToken!, mfaCode.trim()),
    onSuccess: () => navigate(from, { replace: true }),
  });

  const emailCodeMutation = useMutation({
    mutationFn: (token: string) => sendMfaEmailCode(token, i18n.language),
    // Start a 60s cooldown after each send (auto-send on entry counts too) so the
    // resend button can't be spammed and stays under the server-side send limit.
    onSuccess: () => setResendCooldown(60),
  });

  useEffect(() => {
    if (!isLoading && isAuthenticated) {
      navigate(from, { replace: true });
    }
  }, [from, isAuthenticated, isLoading, navigate]);

  useEffect(() => {
    if (resendCooldown <= 0) return;
    const id = setTimeout(() => setResendCooldown((s) => s - 1), 1000);
    return () => clearTimeout(id);
  }, [resendCooldown]);

  const errorMessage = mutation.isError
    ? emailConfirmationRequired
      ? t("auth.emailConfirmationRequired")
      : isAxiosError(mutation.error) && mutation.error.response?.status === 401
      ? t("auth.invalidCredentials")
      : isAxiosError(mutation.error) && mutation.error.response?.status === 403
        ? t("auth.turnstileRequired")
      : t("auth.genericError")
    : null;

  if (isLoading) {
    return <div className="min-h-screen bg-parchment dark:bg-dark-bg" />;
  }

  if (mfaToken) {
    return (
      <div className="min-h-screen flex items-center justify-center p-4 bg-parchment dark:bg-dark-bg">
        <Card className="w-full max-w-sm p-6 space-y-4">
          <div className="flex flex-col items-center gap-2 pb-1">
            <Logo variant="default" className="h-10 w-auto dark:hidden" />
            <Logo variant="cream" className="hidden h-10 w-auto dark:block" />
            <span className="font-serif text-2xl font-semibold text-stone-800 dark:text-stone-100">BukiHub</span>
          </div>
          <h1 className="font-serif text-xl font-semibold text-stone-800 dark:text-stone-100">{t("auth.mfaTitle")}</h1>
          <p className="text-sm text-stone-600 dark:text-stone-400">
            {mfaMethod === "Email" ? t("auth.mfaEmailHint") : t("auth.mfaHint")}
          </p>
          <form className="space-y-3" onSubmit={(e) => { e.preventDefault(); mfaMutation.mutate(); }}>
            <input
              className="w-full rounded-md border border-stone-300 dark:border-white/20 bg-white dark:bg-white/10 px-3 py-2 text-sm text-stone-800 dark:text-stone-100 placeholder:text-stone-400 dark:placeholder:text-stone-400 focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-600 dark:focus:border-green-500 transition-colors"
              placeholder={t("auth.mfaCodePlaceholder")}
              value={mfaCode}
              onChange={(e) => setMfaCode(e.target.value)}
              inputMode="numeric"
              autoComplete="one-time-code"
              autoFocus
            />
            {mfaMutation.isError && <p className="text-sm text-red-600 dark:text-red-400">{t("auth.mfaInvalidCode")}</p>}
            <Button type="submit" disabled={mfaMutation.isPending || !mfaCode.trim()} className="w-full">
              {t("auth.mfaVerify")}
            </Button>
            {mfaMethod === "Email" && (
              <div className="space-y-1 text-center">
                <p className="text-sm text-stone-500 dark:text-stone-400">
                  {t("auth.mfaEmailNotReceived")}{" "}
                  <button
                    type="button"
                    className="text-green-700 dark:text-green-400 hover:underline disabled:opacity-50 disabled:no-underline"
                    disabled={emailCodeMutation.isPending || resendCooldown > 0}
                    onClick={() => emailCodeMutation.mutate(mfaToken!)}
                  >
                    {resendCooldown > 0
                      ? t("auth.mfaEmailResendIn", { seconds: resendCooldown })
                      : t("auth.mfaEmailResend")}
                  </button>
                </p>
                {emailCodeMutation.isSuccess && (
                  <p className="text-sm text-green-700 dark:text-green-400">{t("auth.mfaEmailResent")}</p>
                )}
                {emailCodeMutation.isError && (
                  <p className="text-sm text-red-600 dark:text-red-400">
                    {isAxiosError(emailCodeMutation.error) && emailCodeMutation.error.response?.status === 429
                      ? t("auth.mfaEmailRateLimited")
                      : t("auth.mfaEmailSendError")}
                  </p>
                )}
                <p className="text-xs text-stone-400 dark:text-stone-500">{t("auth.mfaEmailRecoveryFallback")}</p>
              </div>
            )}
            <button
              type="button"
              className="w-full text-sm text-stone-500 dark:text-stone-400 hover:underline"
              onClick={() => { setMfaToken(null); setMfaMethod(null); setMfaCode(""); setResendCooldown(0); mfaMutation.reset(); emailCodeMutation.reset(); }}
            >
              {t("auth.mfaBack")}
            </button>
          </form>
        </Card>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center p-4 bg-parchment dark:bg-dark-bg">
      <Card className="w-full max-w-sm p-6 space-y-4">
        <div className="flex justify-end">
          <LanguageToggle />
        </div>
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
            onChange={(e) => {
              setEmailOrUsername(e.target.value);
              setEmailConfirmationRequired(false);
              setResendTurnstileRequired(false);
              setResendTurnstileToken(null);
              resendMutation.reset();
            }}
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
          <div className="text-right">
            <Link to="/forgot-password" className="text-sm text-green-700 dark:text-green-400">
              {t("auth.forgotPassword")}
            </Link>
          </div>
          {shouldShowTurnstile && (
            <TurnstileBox
              key={turnstileResetKey}
              action="login"
              onVerify={setTurnstileToken}
              onReset={() => setTurnstileToken(null)}
            />
          )}
          {errorMessage && <p className="text-sm text-red-600 dark:text-red-400">{errorMessage}</p>}
          {emailConfirmationRequired && (
            <div className="space-y-3 rounded-md border border-amber-200 bg-amber-50 p-3 text-sm dark:border-amber-400/30 dark:bg-amber-400/10">
              <p className="text-stone-700 dark:text-stone-200">{t("auth.emailConfirmationHint")}</p>
              {shouldShowResendTurnstile && (
                <>
                  <p className="text-amber-800 dark:text-amber-200">{t("auth.resendTurnstileRequired")}</p>
                  <TurnstileBox
                    key={resendTurnstileResetKey}
                    action="email_confirmation"
                    onVerify={setResendTurnstileToken}
                    onReset={() => setResendTurnstileToken(null)}
                  />
                </>
              )}
              {resendMutation.isSuccess && (
                <p className="text-green-700 dark:text-green-400">{t("auth.confirmationResent")}</p>
              )}
              {resendMutation.isError &&
                !(isAxiosError(resendMutation.error) && resendMutation.error.response?.status === 403 && hasTurnstileRequired(resendMutation.error.response.data)) && (
                  <p className="text-red-600 dark:text-red-400">{t("auth.genericError")}</p>
                )}
              <Button
                type="button"
                variant="outline"
                disabled={
                  resendMutation.isPending ||
                  resendMutation.isSuccess ||
                  !emailOrUsername ||
                  (shouldShowResendTurnstile && !resendTurnstileToken)
                }
                onClick={() => resendMutation.mutate()}
                className="w-full"
              >
                {t("auth.resendConfirmation")}
              </Button>
            </div>
          )}
          <Button type="submit" disabled={mutation.isPending || (shouldShowTurnstile && !turnstileToken)} className="w-full">
            {t("auth.signIn")}
          </Button>
        </form>
        <GoogleSignInButton onSuccess={() => navigate(from, { replace: true })} />
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

function hasEmailConfirmationRequired(value: unknown): value is { emailConfirmationRequired: true } {
  return typeof value === "object" &&
    value !== null &&
    "emailConfirmationRequired" in value &&
    value.emailConfirmationRequired === true;
}
