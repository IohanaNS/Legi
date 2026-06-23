import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { isAxiosError } from "axios";
import { QRCodeSVG } from "qrcode.react";
import { Mail, ShieldCheck, ShieldOff, Smartphone } from "lucide-react";
import { Button } from "../../../components/ui/Button";
import { authApi } from "../../auth/api";
import type { MfaSetupResponse } from "../../auth/types";

type Step = "idle" | "choose" | "setup" | "emailSetup" | "recovery" | "disable";

const inputClass =
  "w-full rounded-md border border-stone-300 dark:border-white/20 bg-white dark:bg-white/10 px-3 py-2 text-sm text-stone-800 dark:text-stone-100 placeholder:text-stone-400 dark:placeholder:text-stone-400 focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-600 dark:focus:border-green-500 transition-colors";

export function MfaSection() {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const { data } = useQuery({ queryKey: ["currentUser"], queryFn: authApi.getCurrentUser });
  const mfaEnabled = data?.mfaEnabled ?? false;
  const mfaMethod = data?.mfaMethod ?? "None";

  const [step, setStep] = useState<Step>("idle");
  const [setup, setSetup] = useState<MfaSetupResponse | null>(null);
  const [code, setCode] = useState("");
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>([]);

  const reset = () => {
    setStep("idle");
    setSetup(null);
    setCode("");
  };

  const onEnrolled = (codes: string[]) => {
    setRecoveryCodes(codes);
    setCode("");
    setStep("recovery");
    queryClient.invalidateQueries({ queryKey: ["currentUser"] });
  };

  // --- TOTP (authenticator app) ---
  const setupMutation = useMutation({
    mutationFn: authApi.mfaSetup,
    onSuccess: (res) => {
      setSetup(res);
      setCode("");
      setStep("setup");
    },
  });

  const confirmMutation = useMutation({
    mutationFn: () => authApi.mfaConfirm(code.trim()),
    onSuccess: (res) => onEnrolled(res.recoveryCodes),
  });

  // --- Email codes ---
  const emailSetupMutation = useMutation({
    mutationFn: () => authApi.mfaEmailSetup(i18n.language),
    onSuccess: () => {
      setCode("");
      setStep("emailSetup");
    },
  });

  const emailConfirmMutation = useMutation({
    mutationFn: () => authApi.mfaEmailConfirm(code.trim()),
    onSuccess: (res) => onEnrolled(res.recoveryCodes),
  });

  const disableMutation = useMutation({
    mutationFn: () => authApi.mfaDisable(code.trim()),
    onSuccess: () => {
      reset();
      queryClient.invalidateQueries({ queryKey: ["currentUser"] });
    },
  });

  return (
    <section className="space-y-4">
      <div>
        <h2 className="text-lg font-semibold text-stone-800 dark:text-stone-100">{t("settings.mfaTitle")}</h2>
        <p className="mt-1 text-sm text-stone-500 dark:text-stone-400">{t("settings.mfaSubtitle")}</p>
      </div>

      <div className="rounded-lg border border-stone-200 dark:border-white/10 p-4 space-y-4">
        <div className="flex items-center gap-2 text-sm">
          {mfaEnabled ? (
            <>
              <ShieldCheck className="h-5 w-5 text-green-600 dark:text-green-400" />
              <span className="font-medium text-stone-800 dark:text-stone-100">
                {mfaMethod === "Email" ? t("settings.mfaOnEmail") : t("settings.mfaOnTotp")}
              </span>
            </>
          ) : (
            <>
              <ShieldOff className="h-5 w-5 text-stone-400" />
              <span className="font-medium text-stone-700 dark:text-stone-200">{t("settings.mfaOff")}</span>
            </>
          )}
        </div>

        {/* --- Disabled: offer to enable --- */}
        {!mfaEnabled && step === "idle" && (
          <Button onClick={() => setStep("choose")}>{t("settings.mfaEnable")}</Button>
        )}

        {/* --- Method picker --- */}
        {!mfaEnabled && step === "choose" && (
          <div className="space-y-2">
            <p className="text-sm text-stone-600 dark:text-stone-400">{t("settings.mfaChooseHint")}</p>
            <button
              type="button"
              disabled={setupMutation.isPending}
              onClick={() => setupMutation.mutate()}
              className="flex w-full items-start gap-3 rounded-md border border-stone-200 dark:border-white/10 p-3 text-left hover:border-green-600 dark:hover:border-green-500 transition-colors disabled:opacity-50"
            >
              <Smartphone className="mt-0.5 h-5 w-5 text-green-600 dark:text-green-400 shrink-0" />
              <span>
                <span className="block text-sm font-medium text-stone-800 dark:text-stone-100">{t("settings.mfaMethodTotp")}</span>
                <span className="block text-xs text-stone-500 dark:text-stone-400">{t("settings.mfaMethodTotpHint")}</span>
              </span>
            </button>
            <button
              type="button"
              disabled={emailSetupMutation.isPending}
              onClick={() => emailSetupMutation.mutate()}
              className="flex w-full items-start gap-3 rounded-md border border-stone-200 dark:border-white/10 p-3 text-left hover:border-green-600 dark:hover:border-green-500 transition-colors disabled:opacity-50"
            >
              <Mail className="mt-0.5 h-5 w-5 text-green-600 dark:text-green-400 shrink-0" />
              <span>
                <span className="block text-sm font-medium text-stone-800 dark:text-stone-100">{t("settings.mfaMethodEmail")}</span>
                <span className="block text-xs text-stone-500 dark:text-stone-400">{t("settings.mfaMethodEmailHint")}</span>
              </span>
            </button>
            {emailSetupMutation.isError && (
              <p className="text-sm text-red-600 dark:text-red-400">
                {isAxiosError(emailSetupMutation.error) && emailSetupMutation.error.response?.status === 409
                  ? t("settings.mfaEmailNeedsConfirmedEmail")
                  : t("settings.mfaError")}
              </p>
            )}
            {setupMutation.isError && (
              <p className="text-sm text-red-600 dark:text-red-400">{t("settings.mfaError")}</p>
            )}
            <Button type="button" variant="outline" onClick={reset}>{t("common.cancel")}</Button>
          </div>
        )}

        {/* --- TOTP enroll: QR + secret, then confirm with a code --- */}
        {!mfaEnabled && step === "setup" && setup && (
          <div className="space-y-3">
            <p className="text-sm text-stone-600 dark:text-stone-400">{t("settings.mfaScanHint")}</p>
            <div className="flex justify-center rounded-md bg-white p-3">
              <QRCodeSVG value={setup.otpAuthUri} size={176} />
            </div>
            <p className="text-xs text-stone-500 dark:text-stone-400">{t("settings.mfaManualHint")}</p>
            <code className="block break-all rounded bg-stone-100 dark:bg-white/10 px-2 py-1 text-xs text-stone-700 dark:text-stone-200">
              {setup.secret}
            </code>
            <form className="space-y-2" onSubmit={(e) => { e.preventDefault(); confirmMutation.mutate(); }}>
              <input
                className={inputClass}
                placeholder={t("settings.mfaCodePlaceholder")}
                value={code}
                onChange={(e) => setCode(e.target.value)}
                inputMode="numeric"
                autoComplete="one-time-code"
                autoFocus
              />
              {confirmMutation.isError && (
                <p className="text-sm text-red-600 dark:text-red-400">{t("settings.mfaInvalidCode")}</p>
              )}
              <div className="flex gap-2">
                <Button type="submit" disabled={confirmMutation.isPending || !code.trim()}>
                  {t("settings.mfaConfirm")}
                </Button>
                <Button type="button" variant="outline" onClick={reset}>{t("common.cancel")}</Button>
              </div>
            </form>
          </div>
        )}

        {/* --- Email enroll: enter the emailed code --- */}
        {!mfaEnabled && step === "emailSetup" && (
          <div className="space-y-3">
            <p className="text-sm text-stone-600 dark:text-stone-400">
              {t("settings.mfaEmailSentHint", { email: data?.email ?? "" })}
            </p>
            <form className="space-y-2" onSubmit={(e) => { e.preventDefault(); emailConfirmMutation.mutate(); }}>
              <input
                className={inputClass}
                placeholder={t("settings.mfaCodePlaceholder")}
                value={code}
                onChange={(e) => setCode(e.target.value)}
                inputMode="numeric"
                autoComplete="one-time-code"
                autoFocus
              />
              {emailConfirmMutation.isError && (
                <p className="text-sm text-red-600 dark:text-red-400">{t("settings.mfaInvalidCode")}</p>
              )}
              <div className="flex gap-2">
                <Button type="submit" disabled={emailConfirmMutation.isPending || !code.trim()}>
                  {t("settings.mfaConfirm")}
                </Button>
                <Button type="button" variant="outline" onClick={reset}>{t("common.cancel")}</Button>
              </div>
              <button
                type="button"
                className="text-sm text-green-700 dark:text-green-400 hover:underline disabled:opacity-50"
                disabled={emailSetupMutation.isPending}
                onClick={() => emailSetupMutation.mutate()}
              >
                {t("settings.mfaEmailResend")}
              </button>
            </form>
          </div>
        )}

        {/* --- Recovery codes, shown once --- */}
        {step === "recovery" && (
          <div className="space-y-3">
            <p className="text-sm font-medium text-stone-800 dark:text-stone-100">{t("settings.mfaRecoveryTitle")}</p>
            <p className="text-sm text-stone-600 dark:text-stone-400">{t("settings.mfaRecoveryHint")}</p>
            <ul className="grid grid-cols-2 gap-2 rounded-md bg-stone-100 dark:bg-white/10 p-3 font-mono text-sm text-stone-800 dark:text-stone-100">
              {recoveryCodes.map((rc) => (
                <li key={rc}>{rc}</li>
              ))}
            </ul>
            <div className="flex gap-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => navigator.clipboard?.writeText(recoveryCodes.join("\n"))}
              >
                {t("settings.mfaCopyCodes")}
              </Button>
              <Button type="button" onClick={() => setStep("idle")}>{t("settings.mfaDone")}</Button>
            </div>
          </div>
        )}

        {/* --- Enabled: offer to disable --- */}
        {mfaEnabled && step !== "disable" && step !== "recovery" && (
          <Button variant="outline" onClick={() => setStep("disable")}>
            {t("settings.mfaDisable")}
          </Button>
        )}

        {mfaEnabled && step === "disable" && (
          <form className="space-y-2" onSubmit={(e) => { e.preventDefault(); disableMutation.mutate(); }}>
            <p className="text-sm text-stone-600 dark:text-stone-400">
              {mfaMethod === "Email" ? t("settings.mfaDisableEmailHint") : t("settings.mfaDisableHint")}
            </p>
            <input
              className={inputClass}
              placeholder={mfaMethod === "Email" ? t("settings.mfaRecoveryCodePlaceholder") : t("settings.mfaCodePlaceholder")}
              value={code}
              onChange={(e) => setCode(e.target.value)}
              autoComplete="one-time-code"
              autoFocus
            />
            {disableMutation.isError && (
              <p className="text-sm text-red-600 dark:text-red-400">{t("settings.mfaInvalidCode")}</p>
            )}
            <div className="flex gap-2">
              <Button type="submit" disabled={disableMutation.isPending || !code.trim()}>
                {t("settings.mfaDisable")}
              </Button>
              <Button type="button" variant="outline" onClick={reset}>{t("common.cancel")}</Button>
            </div>
          </form>
        )}
      </div>
    </section>
  );
}
