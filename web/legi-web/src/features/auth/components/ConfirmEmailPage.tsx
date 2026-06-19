import { useEffect, useRef } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { isAxiosError } from "axios";
import { authApi } from "../api";
import { Card } from "../../../components/ui/Card";
import { Logo } from "../../../components/ui/Logo";

export default function ConfirmEmailPage() {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const token = searchParams.get("token") ?? "";
  const submittedTokenRef = useRef<string | null>(null);

  const confirmation = useMutation({
    mutationFn: async (tokenToConfirm: string) => {
      await authApi.confirmEmail({ token: tokenToConfirm });
      return true;
    },
    retry: false,
  });

  useEffect(() => {
    if (!token || submittedTokenRef.current === token) return;

    submittedTokenRef.current = token;
    confirmation.mutate(token);
  }, [confirmation, token]);

  const invalidToken =
    !token ||
    (confirmation.isError && isAxiosError(confirmation.error) &&
      (confirmation.error.response?.status === 404 || confirmation.error.response?.status === 400));

  return (
    <div className="min-h-screen flex items-center justify-center p-4 bg-parchment dark:bg-dark-bg">
      <Card className="w-full max-w-sm p-6 space-y-4">
        <div className="flex flex-col items-center gap-2 pb-1">
          <Logo variant="default" className="h-10 w-auto dark:hidden" />
          <Logo variant="cream" className="hidden h-10 w-auto dark:block" />
          <span className="font-serif text-2xl font-semibold text-stone-800 dark:text-stone-100">BukiHub</span>
        </div>
        <h1 className="font-serif text-xl font-semibold text-stone-800 dark:text-stone-100">
          {t("auth.confirmEmailTitle")}
        </h1>

        {confirmation.isPending ? (
          <p className="text-sm text-stone-600 dark:text-stone-400">{t("auth.confirmEmailLoading")}</p>
        ) : confirmation.isSuccess ? (
          <>
            <p className="text-sm text-stone-600 dark:text-stone-400">{t("auth.confirmEmailSuccess")}</p>
            <p className="text-sm text-center text-stone-600 dark:text-stone-400">
              <Link to="/login" className="text-green-700 dark:text-green-400">{t("auth.backToLogin")}</Link>
            </p>
          </>
        ) : invalidToken ? (
          <>
            <p className="text-sm text-red-600 dark:text-red-400">{t("auth.confirmEmailInvalid")}</p>
            <p className="text-sm text-center text-stone-600 dark:text-stone-400">
              <Link to="/login" className="text-green-700 dark:text-green-400">{t("auth.backToLogin")}</Link>
            </p>
          </>
        ) : (
          <>
            <p className="text-sm text-red-600 dark:text-red-400">{t("auth.confirmEmailError")}</p>
            <p className="text-sm text-center text-stone-600 dark:text-stone-400">
              <Link to="/login" className="text-green-700 dark:text-green-400">{t("auth.backToLogin")}</Link>
            </p>
          </>
        )}
      </Card>
    </div>
  );
}
