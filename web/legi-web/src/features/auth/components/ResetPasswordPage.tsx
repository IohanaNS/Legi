import { useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { isAxiosError } from "axios";
import { authApi } from "../api";
import { Button } from "../../../components/ui/Button";
import { Card } from "../../../components/ui/Card";
import { Logo } from "../../../components/ui/Logo";

const MIN_PASSWORD_LENGTH = 8;

export default function ResetPasswordPage() {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const token = searchParams.get("token") ?? "";

  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [clientError, setClientError] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: () => authApi.resetPassword({ token, newPassword: password }),
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setClientError(null);

    if (password.length < MIN_PASSWORD_LENGTH) {
      setClientError(t("auth.passwordTooShort"));
      return;
    }
    if (password !== confirmPassword) {
      setClientError(t("auth.passwordsDoNotMatch"));
      return;
    }
    mutation.mutate();
  };

  const invalidToken =
    !token ||
    (mutation.isError && isAxiosError(mutation.error) &&
      (mutation.error.response?.status === 404 || mutation.error.response?.status === 400));

  const serverError = mutation.isError
    ? isAxiosError(mutation.error) && mutation.error.response?.status === 422
      ? t("auth.passwordRequirements")
      : t("auth.genericError")
    : null;

  return (
    <div className="min-h-screen flex items-center justify-center p-4 bg-parchment dark:bg-dark-bg">
      <Card className="w-full max-w-sm p-6 space-y-4">
        <div className="flex flex-col items-center gap-2 pb-1">
          <Logo variant="default" className="h-10 w-auto dark:hidden" />
          <Logo variant="cream" className="hidden h-10 w-auto dark:block" />
          <span className="font-serif text-2xl font-semibold text-stone-800 dark:text-stone-100">BukiHub</span>
        </div>
        <h1 className="font-serif text-xl font-semibold text-stone-800 dark:text-stone-100">{t("auth.resetPasswordTitle")}</h1>

        {mutation.isSuccess ? (
          <>
            <p className="text-sm text-stone-600 dark:text-stone-400">{t("auth.resetPasswordSuccess")}</p>
            <p className="text-sm text-center text-stone-600 dark:text-stone-400">
              <Link to="/login" className="text-green-700 dark:text-green-400">{t("auth.backToLogin")}</Link>
            </p>
          </>
        ) : invalidToken ? (
          <>
            <p className="text-sm text-red-600 dark:text-red-400">{t("auth.resetLinkInvalid")}</p>
            <p className="text-sm text-center text-stone-600 dark:text-stone-400">
              <Link to="/forgot-password" className="text-green-700 dark:text-green-400">{t("auth.requestNewLink")}</Link>
            </p>
          </>
        ) : (
          <>
            <p className="text-sm text-stone-600 dark:text-stone-400">{t("auth.resetPasswordPrompt")}</p>
            <form className="space-y-3" onSubmit={handleSubmit}>
              <input
                type="password"
                className="w-full rounded-md border border-stone-300 dark:border-white/20 bg-white dark:bg-white/10 px-3 py-2 text-sm text-stone-800 dark:text-stone-100 placeholder:text-stone-400 dark:placeholder:text-stone-400 focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-600 dark:focus:border-green-500 transition-colors"
                placeholder={t("auth.newPassword")}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                autoComplete="new-password"
              />
              <input
                type="password"
                className="w-full rounded-md border border-stone-300 dark:border-white/20 bg-white dark:bg-white/10 px-3 py-2 text-sm text-stone-800 dark:text-stone-100 placeholder:text-stone-400 dark:placeholder:text-stone-400 focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-600 dark:focus:border-green-500 transition-colors"
                placeholder={t("auth.confirmPassword")}
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                autoComplete="new-password"
              />
              {(clientError ?? serverError) && (
                <p className="text-sm text-red-600 dark:text-red-400">{clientError ?? serverError}</p>
              )}
              <Button type="submit" disabled={mutation.isPending || !password || !confirmPassword} className="w-full">
                {t("auth.resetPasswordSubmit")}
              </Button>
            </form>
          </>
        )}
      </Card>
    </div>
  );
}
