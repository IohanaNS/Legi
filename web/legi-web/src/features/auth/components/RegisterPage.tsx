import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { isAxiosError } from "axios";
import { useAuth } from "../useAuth";
import { Button } from "../../../components/ui/Button";
import { Card } from "../../../components/ui/Card";
import { Logo } from "../../../components/ui/Logo";

export default function RegisterPage() {
  const { t } = useTranslation();
  const { register } = useAuth();
  const navigate = useNavigate();

  const [email, setEmail] = useState("");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");

  const mutation = useMutation({
    mutationFn: () => register({ email, username, password }),
    onSuccess: () => navigate("/feed", { replace: true }),
  });

  const errorMessage = mutation.isError
    ? isAxiosError(mutation.error) && mutation.error.response?.status === 409
      ? t("auth.userExists")
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
        <h1 className="font-serif text-xl font-semibold text-stone-800 dark:text-stone-100">{t("auth.registerTitle")}</h1>
        <form className="space-y-3" onSubmit={(e) => { e.preventDefault(); mutation.mutate(); }}>
          <input
            type="email"
            className="w-full rounded-md border border-stone-300 dark:border-white/20 bg-white dark:bg-white/10 px-3 py-2 text-sm text-stone-800 dark:text-stone-100 placeholder:text-stone-400 dark:placeholder:text-stone-400 focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-600 dark:focus:border-green-500 transition-colors"
            placeholder={t("auth.email")}
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            autoComplete="email"
          />
          <input
            className="w-full rounded-md border border-stone-300 dark:border-white/20 bg-white dark:bg-white/10 px-3 py-2 text-sm text-stone-800 dark:text-stone-100 placeholder:text-stone-400 dark:placeholder:text-stone-400 focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-600 dark:focus:border-green-500 transition-colors"
            placeholder={t("auth.username")}
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            autoComplete="username"
          />
          <input
            type="password"
            className="w-full rounded-md border border-stone-300 dark:border-white/20 bg-white dark:bg-white/10 px-3 py-2 text-sm text-stone-800 dark:text-stone-100 placeholder:text-stone-400 dark:placeholder:text-stone-400 focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-600 dark:focus:border-green-500 transition-colors"
            placeholder={t("auth.password")}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="new-password"
          />
          {errorMessage && <p className="text-sm text-red-600 dark:text-red-400">{errorMessage}</p>}
          <Button type="submit" disabled={mutation.isPending} className="w-full">
            {t("auth.signUp")}
          </Button>
        </form>
        <p className="text-sm text-center text-stone-600 dark:text-stone-400">
          {t("auth.haveAccount")}{" "}
          <Link to="/login" className="text-green-700 dark:text-green-400">{t("auth.signIn")}</Link>
        </p>
      </Card>
    </div>
  );
}
