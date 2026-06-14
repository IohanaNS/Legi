import { useCallback, useEffect, useState } from "react";
import { createPortal } from "react-dom";
import { useMutation } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router-dom";
import { AlertTriangle, CheckCircle2, KeyRound, Mail, Trash2, X } from "lucide-react";
import { Button } from "../../../components/ui/Button";
import { authApi } from "../../auth/api";
import { isTurnstileConfigured } from "../../auth/turnstile";
import { TurnstileBox } from "../../auth/components/TurnstileBox";
import { useAuth } from "../../auth/useAuth";

export default function SettingsPage() {
  const { t, i18n } = useTranslation();
  const { deleteAccount, user } = useAuth();
  const navigate = useNavigate();
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [passwordConfirmOpen, setPasswordConfirmOpen] = useState(false);
  const [turnstileToken, setTurnstileToken] = useState<string | null>(null);
  const [turnstileResetKey, setTurnstileResetKey] = useState(0);
  const turnstileEnabled = isTurnstileConfigured();

  const resetTurnstile = useCallback(() => {
    setTurnstileToken(null);
    setTurnstileResetKey((current) => current + 1);
  }, []);

  const deleteMutation = useMutation({
    mutationFn: deleteAccount,
    onSuccess: () => navigate("/login", { replace: true }),
  });

  const passwordResetMutation = useMutation({
    mutationFn: () =>
      authApi.forgotPassword({
        email: user?.email ?? "",
        turnstileToken: turnstileEnabled ? turnstileToken ?? undefined : undefined,
        language: i18n.language,
      }),
    onError: () => resetTurnstile(),
  });

  return (
    <div className="mx-auto max-w-3xl space-y-8">
      <header>
        <h1 className="font-serif text-3xl font-semibold text-stone-800 dark:text-stone-100">
          {t("settings.title")}
        </h1>
        <p className="mt-1 text-sm text-stone-500 dark:text-stone-400">
          {t("settings.subtitle")}
        </p>
      </header>

      <section className="space-y-4">
        <div>
          <h2 className="text-lg font-semibold text-stone-800 dark:text-stone-100">
            {t("settings.account")}
          </h2>
          <p className="mt-1 text-sm text-stone-500 dark:text-stone-400">
            {t("settings.accountSubtitle")}
          </p>
        </div>

        <div className="rounded-xl border border-stone-200 bg-white p-5 dark:border-dark-raised dark:bg-dark-card">
          <label
            htmlFor="account-email"
            className="text-sm font-medium text-stone-700 dark:text-stone-200"
          >
            {t("settings.emailAddress")}
          </label>
          <div className="mt-2 flex items-center gap-2 rounded-lg border border-stone-200 bg-stone-50 px-3 py-2.5 dark:border-dark-raised dark:bg-dark-bg">
            <Mail
              size={16}
              aria-hidden="true"
              className="shrink-0 text-stone-400 dark:text-stone-500"
            />
            <input
              id="account-email"
              type="email"
              readOnly
              value={user?.email ?? ""}
              className="min-w-0 flex-1 bg-transparent text-sm text-stone-800 outline-none dark:text-stone-100"
            />
          </div>
        </div>

        <div className="rounded-xl border border-stone-200 bg-white p-5 dark:border-dark-raised dark:bg-dark-card">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
            <div>
              <h3 className="font-semibold text-stone-800 dark:text-stone-100">
                {t("settings.passwordSectionTitle")}
              </h3>
              <p className="mt-1 text-sm text-stone-500 dark:text-stone-400">
                {t("settings.passwordDescription")}
              </p>
              {passwordResetMutation.isSuccess && !passwordConfirmOpen && (
                <p className="mt-3 text-sm font-medium text-green-700 dark:text-green-300">
                  {t("settings.passwordEmailSent")}
                </p>
              )}
              {passwordResetMutation.isError && !passwordConfirmOpen && (
                <p className="mt-3 text-sm font-medium text-red-700 dark:text-red-300">
                  {t("settings.passwordEmailError")}
                </p>
              )}
            </div>

            <Button
              type="button"
              variant="outline"
              onClick={() => {
                passwordResetMutation.reset();
                resetTurnstile();
                setPasswordConfirmOpen(true);
              }}
              disabled={passwordResetMutation.isPending || !user?.email}
              className="shrink-0"
            >
              <KeyRound size={16} />
              {t("settings.changePassword")}
            </Button>
          </div>
        </div>

        <div className="rounded-xl border border-stone-200 bg-white p-5 dark:border-dark-raised dark:bg-dark-card">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
            <div>
              <h3 className="font-semibold text-stone-800 dark:text-stone-100">
                {t("settings.deleteSectionTitle")}
              </h3>
              <p className="mt-1 text-sm text-stone-500 dark:text-stone-400">
                {t("settings.deleteDescription")}
              </p>
              {deleteMutation.isError && !confirmOpen && (
                <p className="mt-3 text-sm font-medium text-red-700 dark:text-red-300">
                  {t("settings.deleteError")}
                </p>
              )}
            </div>

            <Button
              type="button"
              variant="outline"
              onClick={() => {
                deleteMutation.reset();
                setConfirmOpen(true);
              }}
              disabled={deleteMutation.isPending}
              className="shrink-0 border-red-200 text-red-700 hover:bg-red-50 hover:text-red-800 dark:border-red-900/70 dark:text-red-300 dark:hover:bg-red-950/30 dark:hover:text-red-200"
            >
              <Trash2 size={16} />
              {t("settings.deleteAccount")}
            </Button>
          </div>
        </div>
      </section>

      {confirmOpen && (
        <DeleteAccountDialog
          isError={deleteMutation.isError}
          isPending={deleteMutation.isPending}
          onCancel={() => setConfirmOpen(false)}
          onConfirm={() => deleteMutation.mutate()}
        />
      )}

      {passwordConfirmOpen && (
        <PasswordResetDialog
          email={user?.email ?? ""}
          isError={passwordResetMutation.isError}
          isPending={passwordResetMutation.isPending}
          isSuccess={passwordResetMutation.isSuccess}
          turnstileEnabled={turnstileEnabled}
          turnstileResetKey={turnstileResetKey}
          turnstileToken={turnstileToken}
          onCancel={() => setPasswordConfirmOpen(false)}
          onConfirm={() => passwordResetMutation.mutate()}
          onTurnstileReset={() => setTurnstileToken(null)}
          onTurnstileVerify={setTurnstileToken}
        />
      )}
    </div>
  );
}

interface PasswordResetDialogProps {
  email: string;
  isError: boolean;
  isPending: boolean;
  isSuccess: boolean;
  turnstileEnabled: boolean;
  turnstileResetKey: number;
  turnstileToken: string | null;
  onCancel: () => void;
  onConfirm: () => void;
  onTurnstileReset: () => void;
  onTurnstileVerify: (token: string) => void;
}

function PasswordResetDialog({
  email,
  isError,
  isPending,
  isSuccess,
  turnstileEnabled,
  turnstileResetKey,
  turnstileToken,
  onCancel,
  onConfirm,
  onTurnstileReset,
  onTurnstileVerify,
}: PasswordResetDialogProps) {
  const { t } = useTranslation();
  const canSubmit = !!email && !isPending && (!turnstileEnabled || !!turnstileToken);

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape" && !isPending) onCancel();
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [isPending, onCancel]);

  useEffect(() => {
    const originalOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = originalOverflow;
    };
  }, []);

  return createPortal(
    <div
      className="fixed inset-0 z-50 bg-black/45 px-4 py-8 backdrop-blur-sm"
      role="presentation"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget && !isPending) onCancel();
      }}
    >
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="password-reset-title"
        aria-describedby="password-reset-message"
        className="mx-auto mt-16 w-full max-w-md overflow-hidden rounded-lg border border-stone-200 bg-white shadow-2xl dark:border-dark-raised dark:bg-dark-card"
      >
        <div className="flex items-center justify-between gap-3 border-b border-stone-200 px-5 py-4 dark:border-dark-raised">
          <h2
            id="password-reset-title"
            className="text-base font-semibold text-stone-900 dark:text-stone-100"
          >
            {isSuccess ? t("settings.passwordEmailSentTitle") : t("settings.passwordConfirmTitle")}
          </h2>
          <button
            type="button"
            onClick={onCancel}
            disabled={isPending}
            aria-label={t("settings.closePasswordDialog")}
            className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-500 transition-colors hover:bg-stone-100 hover:text-stone-800 disabled:pointer-events-none disabled:opacity-50 dark:text-stone-400 dark:hover:bg-dark-raised dark:hover:text-stone-100"
          >
            <X size={17} />
          </button>
        </div>

        <div className="space-y-4 px-5 py-4">
          {isSuccess ? (
            <div className="rounded-lg border border-green-200 bg-green-50 p-4 dark:border-green-950/70 dark:bg-green-950/20">
              <div className="flex gap-3">
                <CheckCircle2
                  size={20}
                  className="mt-0.5 shrink-0 text-green-700 dark:text-green-300"
                />
                <p
                  id="password-reset-message"
                  className="text-sm text-green-900 dark:text-green-100"
                >
                  {t("settings.passwordEmailSentDescription", { email })}
                </p>
              </div>
            </div>
          ) : (
            <>
              <div className="rounded-lg border border-stone-200 bg-stone-50 p-4 dark:border-dark-raised dark:bg-dark-bg">
                <div className="flex gap-3">
                  <Mail
                    size={20}
                    className="mt-0.5 shrink-0 text-green-700 dark:text-green-300"
                  />
                  <p
                    id="password-reset-message"
                    className="text-sm text-stone-700 dark:text-stone-200"
                  >
                    {t("settings.passwordConfirmWarning", { email })}
                  </p>
                </div>
              </div>

              {turnstileEnabled && (
                <TurnstileBox
                  key={turnstileResetKey}
                  action="password_reset"
                  onVerify={onTurnstileVerify}
                  onReset={onTurnstileReset}
                />
              )}

              {isError && (
                <p className="text-sm font-medium text-red-700 dark:text-red-300">
                  {t("settings.passwordEmailError")}
                </p>
              )}
            </>
          )}

          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            {isSuccess ? (
              <Button type="button" onClick={onCancel} className="w-full sm:w-auto">
                {t("common.close")}
              </Button>
            ) : (
              <>
                <Button
                  type="button"
                  variant="outline"
                  onClick={onCancel}
                  disabled={isPending}
                  className="w-full sm:w-auto"
                >
                  {t("common.cancel")}
                </Button>
                <Button
                  type="button"
                  onClick={onConfirm}
                  disabled={!canSubmit}
                  className="w-full sm:w-auto"
                >
                  <Mail size={16} />
                  {isPending ? t("settings.passwordEmailPending") : t("settings.sendPasswordEmail")}
                </Button>
              </>
            )}
          </div>
        </div>
      </section>
    </div>,
    document.body,
  );
}

interface DeleteAccountDialogProps {
  isError: boolean;
  isPending: boolean;
  onCancel: () => void;
  onConfirm: () => void;
}

function DeleteAccountDialog({
  isError,
  isPending,
  onCancel,
  onConfirm,
}: DeleteAccountDialogProps) {
  const { t } = useTranslation();

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape" && !isPending) onCancel();
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [isPending, onCancel]);

  useEffect(() => {
    const originalOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = originalOverflow;
    };
  }, []);

  return createPortal(
    <div
      className="fixed inset-0 z-50 bg-black/45 px-4 py-8 backdrop-blur-sm"
      role="presentation"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget && !isPending) onCancel();
      }}
    >
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="delete-account-title"
        aria-describedby="delete-account-warning"
        className="mx-auto mt-16 w-full max-w-md overflow-hidden rounded-lg border border-stone-200 bg-white shadow-2xl dark:border-dark-raised dark:bg-dark-card"
      >
        <div className="flex items-center justify-between gap-3 border-b border-stone-200 px-5 py-4 dark:border-dark-raised">
          <h2
            id="delete-account-title"
            className="text-base font-semibold text-stone-900 dark:text-stone-100"
          >
            {t("settings.confirmTitle")}
          </h2>
          <button
            type="button"
            onClick={onCancel}
            disabled={isPending}
            aria-label={t("settings.closeDialog")}
            className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-500 transition-colors hover:bg-stone-100 hover:text-stone-800 disabled:pointer-events-none disabled:opacity-50 dark:text-stone-400 dark:hover:bg-dark-raised dark:hover:text-stone-100"
          >
            <X size={17} />
          </button>
        </div>

        <div className="space-y-4 px-5 py-4">
          <div className="rounded-lg border border-red-200 bg-red-50 p-4 dark:border-red-950/70 dark:bg-red-950/20">
            <div className="flex gap-3">
              <AlertTriangle
                size={20}
                className="mt-0.5 shrink-0 text-red-700 dark:text-red-300"
              />
              <p id="delete-account-warning" className="text-sm text-red-900 dark:text-red-100">
                {t("settings.confirmWarning")}
              </p>
            </div>
          </div>
          {isError && (
            <p className="text-sm font-medium text-red-700 dark:text-red-300">
              {t("settings.deleteError")}
            </p>
          )}

          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <Button
              type="button"
              variant="outline"
              onClick={onCancel}
              disabled={isPending}
              className="w-full sm:w-auto"
            >
              {t("common.cancel")}
            </Button>
            <Button
              type="button"
              variant="danger"
              onClick={onConfirm}
              disabled={isPending}
              className="w-full sm:w-auto"
            >
              <Trash2 size={16} />
              {isPending ? t("settings.deletePending") : t("settings.confirmDelete")}
            </Button>
          </div>
        </div>
      </section>
    </div>,
    document.body,
  );
}
