import { useCallback, useState } from "react";
import { Link } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { authApi } from "../api";
import { Button } from "../../../components/ui/Button";
import { Card } from "../../../components/ui/Card";
import { Logo } from "../../../components/ui/Logo";
import { isTurnstileConfigured } from "../turnstile";
import { TurnstileBox } from "./TurnstileBox";

export default function ForgotPasswordPage() {
  const { t, i18n } = useTranslation();

  const [email, setEmail] = useState("");
  const [turnstileToken, setTurnstileToken] = useState<string | null>(null);
  const [turnstileResetKey, setTurnstileResetKey] = useState(0);
  const turnstileEnabled = isTurnstileConfigured();

  const resetTurnstile = useCallback(() => {
    setTurnstileToken(null);
    setTurnstileResetKey((current) => current + 1);
  }, []);

  const mutation = useMutation({
    mutationFn: () => authApi.forgotPassword({
      email,
      turnstileToken: turnstileEnabled ? turnstileToken ?? undefined : undefined,
      language: i18n.language,
    }),
    onError: () => resetTurnstile(),
  });

  return (
    <div className="min-h-screen flex items-center justify-center p-4 bg-parchment dark:bg-dark-bg">
      <Card className="w-full max-w-sm p-6 space-y-4">
        <div className="flex flex-col items-center gap-2 pb-1">
          <Logo variant="default" className="h-10 w-auto dark:hidden" />
          <Logo variant="cream" className="hidden h-10 w-auto dark:block" />
          <span className="font-serif text-2xl font-semibold text-stone-800 dark:text-stone-100">BukiHub</span>
        </div>
        <h1 className="font-serif text-xl font-semibold text-stone-800 dark:text-stone-100">{t("auth.forgotPasswordTitle")}</h1>

        {mutation.isSuccess ? (
          <>
            <p className="text-sm text-stone-600 dark:text-stone-400">{t("auth.forgotPasswordSent")}</p>
            <p className="text-sm text-center text-stone-600 dark:text-stone-400">
              <Link to="/login" className="text-green-700 dark:text-green-400">{t("auth.backToLogin")}</Link>
            </p>
          </>
        ) : (
          <>
            <p className="text-sm text-stone-600 dark:text-stone-400">{t("auth.forgotPasswordPrompt")}</p>
            <form className="space-y-3" onSubmit={(e) => { e.preventDefault(); mutation.mutate(); }}>
              <input
                type="email"
                className="w-full rounded-md border border-stone-300 dark:border-white/20 bg-white dark:bg-white/10 px-3 py-2 text-sm text-stone-800 dark:text-stone-100 placeholder:text-stone-400 dark:placeholder:text-stone-400 focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-600 dark:focus:border-green-500 transition-colors"
                placeholder={t("auth.email")}
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                autoComplete="email"
              />
              {turnstileEnabled && (
                <TurnstileBox
                  key={turnstileResetKey}
                  action="password_reset"
                  onVerify={setTurnstileToken}
                  onReset={() => setTurnstileToken(null)}
                />
              )}
              {mutation.isError && <p className="text-sm text-red-600 dark:text-red-400">{t("auth.genericError")}</p>}
              <Button type="submit" disabled={mutation.isPending || !email || (turnstileEnabled && !turnstileToken)} className="w-full">
                {t("auth.sendResetLink")}
              </Button>
            </form>
            <p className="text-sm text-center text-stone-600 dark:text-stone-400">
              <Link to="/login" className="text-green-700 dark:text-green-400">{t("auth.backToLogin")}</Link>
            </p>
          </>
        )}
      </Card>
    </div>
  );
}
