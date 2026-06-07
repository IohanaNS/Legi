import { useState } from "react";
import { useNavigate, useLocation, Link } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { isAxiosError } from "axios";
import { useAuth } from "../useAuth";
import { Button } from "../../../components/ui/Button";
import { Card } from "../../../components/ui/Card";

export default function LoginPage() {
  const { t } = useTranslation();
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: { pathname?: string } } | null)?.from?.pathname ?? "/feed";

  const [emailOrUsername, setEmailOrUsername] = useState("");
  const [password, setPassword] = useState("");

  const mutation = useMutation({
    mutationFn: () => login({ emailOrUsername, password }),
    onSuccess: () => navigate(from, { replace: true }),
  });

  const errorMessage = mutation.isError
    ? isAxiosError(mutation.error) && mutation.error.response?.status === 401
      ? t("auth.invalidCredentials")
      : t("auth.genericError")
    : null;

  return (
    <div className="min-h-screen flex items-center justify-center p-4 bg-parchment dark:bg-dark-bg">
      <Card className="w-full max-w-sm p-6 space-y-4">
        <h1 className="text-xl font-semibold text-stone-800">{t("auth.loginTitle")}</h1>
        <form className="space-y-3" onSubmit={(e) => { e.preventDefault(); mutation.mutate(); }}>
          <input
            className="w-full rounded-md border border-stone-300 px-3 py-2"
            placeholder={t("auth.emailOrUsername")}
            value={emailOrUsername}
            onChange={(e) => setEmailOrUsername(e.target.value)}
            autoComplete="username"
          />
          <input
            type="password"
            className="w-full rounded-md border border-stone-300 px-3 py-2"
            placeholder={t("auth.password")}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="current-password"
          />
          {errorMessage && <p className="text-sm text-red-600">{errorMessage}</p>}
          <Button type="submit" disabled={mutation.isPending} className="w-full">
            {t("auth.signIn")}
          </Button>
        </form>
        <p className="text-sm text-center text-stone-600">
          {t("auth.noAccount")}{" "}
          <Link to="/register" className="text-green-700">{t("auth.signUp")}</Link>
        </p>
      </Card>
    </div>
  );
}
