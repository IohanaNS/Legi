import { useEffect, useMemo, useState } from "react";
import { createPortal } from "react-dom";
import type { TFunction } from "i18next";
import { useTranslation } from "react-i18next";
import { isAxiosError } from "axios";
import { Upload, X } from "lucide-react";
import { Avatar } from "../../../components/ui/Avatar";
import { Button } from "../../../components/ui/Button";
import { cn } from "../../../lib/utils";
import type { ProfileImageUploadKind } from "../../social/hooks/useProfileImageUpload";

interface ProfileImageUploadModalProps {
  kind: ProfileImageUploadKind;
  username: string;
  currentUrl?: string | null;
  isSaving: boolean;
  error: unknown;
  onCancel: () => void;
  onFileSelected?: () => void;
  onSave: (file: File) => Promise<void>;
}

interface ApiErrorBody {
  error?: string;
  detail?: string;
  title?: string;
  errors?: Record<string, string[]>;
}

const ALLOWED_TYPES = new Set(["image/jpeg", "image/png", "image/webp"]);
const MAX_BYTES: Record<ProfileImageUploadKind, number> = {
  avatar: 2 * 1024 * 1024,
  banner: 5 * 1024 * 1024,
};

export function ProfileImageUploadModal({
  kind,
  username,
  currentUrl,
  isSaving,
  error,
  onCancel,
  onFileSelected,
  onSave,
}: ProfileImageUploadModalProps) {
  const { t } = useTranslation();
  const [file, setFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [validationError, setValidationError] = useState<string | null>(null);
  const maxMb = MAX_BYTES[kind] / (1024 * 1024);
  const titleId = `profile-${kind}-upload-title`;
  const fileInputId = `profile-${kind}-upload-file`;
  const title = t(kind === "avatar" ? "profile.media.avatarTitle" : "profile.media.bannerTitle");
  const previewAlt = t(kind === "avatar" ? "profile.media.avatarPreviewAlt" : "profile.media.bannerPreviewAlt");
  const serverError = useMemo(() => getServerErrorMessage(error, kind, t), [error, kind, t]);
  const selectedPreview = previewUrl ?? currentUrl ?? null;

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape" && !isSaving) onCancel();
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [isSaving, onCancel]);

  useEffect(() => {
    const originalOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = originalOverflow;
    };
  }, []);

  useEffect(() => {
    return () => {
      if (previewUrl) URL.revokeObjectURL(previewUrl);
    };
  }, [previewUrl]);

  const handleFileChange = (nextFile: File | null) => {
    if (previewUrl) URL.revokeObjectURL(previewUrl);
    setPreviewUrl(null);
    setFile(nextFile);
    setValidationError(null);
    onFileSelected?.();

    if (!nextFile) return;

    const nextError = validateFile(nextFile, kind, t);
    if (nextError) {
      setValidationError(nextError);
      return;
    }

    setPreviewUrl(URL.createObjectURL(nextFile));
  };

  const handleSave = async () => {
    if (!file) {
      setValidationError(t("profile.media.errors.required"));
      return;
    }

    const nextError = validateFile(file, kind, t);
    if (nextError) {
      setValidationError(nextError);
      return;
    }

    await onSave(file);
  };

  return createPortal(
    <div
      className="fixed inset-0 z-50 bg-black/45 px-4 py-8 backdrop-blur-sm"
      role="presentation"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget && !isSaving) onCancel();
      }}
    >
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className="mx-auto mt-10 w-full max-w-lg overflow-hidden rounded-lg border border-stone-200 bg-white shadow-2xl dark:border-dark-raised dark:bg-dark-card"
      >
        <div className="flex items-center justify-between gap-3 border-b border-stone-200 px-5 py-4 dark:border-dark-raised">
          <h2 id={titleId} className="text-base font-semibold text-stone-900 dark:text-stone-100">
            {title}
          </h2>
          <button
            type="button"
            onClick={onCancel}
            disabled={isSaving}
            aria-label={t("profile.media.closeDialog")}
            className="flex h-8 w-8 items-center justify-center rounded-lg text-stone-500 transition-colors hover:bg-stone-100 hover:text-stone-800 disabled:pointer-events-none disabled:opacity-50 dark:text-stone-400 dark:hover:bg-dark-raised dark:hover:text-stone-100"
          >
            <X size={17} />
          </button>
        </div>

        <div className="space-y-4 px-5 py-4">
          <div
            className={cn(
              "overflow-hidden border border-stone-200 bg-stone-100 dark:border-dark-raised dark:bg-dark-raised",
              kind === "avatar" ? "mx-auto h-32 w-32 rounded-full" : "aspect-[3/1] w-full rounded-lg",
            )}
          >
            {selectedPreview ? (
              <img src={selectedPreview} alt={previewAlt} className="h-full w-full object-cover" />
            ) : kind === "avatar" ? (
              <Avatar fallback={username} size="xl" className="h-full w-full rounded-full" />
            ) : null}
          </div>

          <div>
            <label
              htmlFor={fileInputId}
              className="mb-1 block text-sm font-medium text-stone-700 dark:text-stone-200"
            >
              {t("profile.media.fileLabel")}
            </label>
            <input
              id={fileInputId}
              type="file"
              accept="image/jpeg,image/png,image/webp"
              disabled={isSaving}
              onChange={(event) => handleFileChange(event.target.files?.[0] ?? null)}
              className="block w-full rounded-lg border border-stone-300 bg-white px-3 py-2 text-sm text-stone-800 file:mr-3 file:rounded-md file:border-0 file:bg-stone-100 file:px-3 file:py-1.5 file:text-sm file:font-medium file:text-stone-700 hover:file:bg-stone-200 focus:outline-none focus:ring-1 focus:ring-green-600 disabled:opacity-60 dark:border-dark-raised dark:bg-dark-raised dark:text-stone-100 dark:file:bg-dark-card dark:file:text-stone-200 dark:hover:file:bg-dark-raised"
            />
            <p className="mt-1 text-xs text-stone-500 dark:text-stone-400">
              {t("profile.media.fileHelp", { size: maxMb })}
            </p>
          </div>

          {(validationError || serverError) && (
            <p className="text-sm font-medium text-red-700 dark:text-red-300">
              {validationError ?? serverError}
            </p>
          )}

          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <Button
              type="button"
              variant="outline"
              onClick={onCancel}
              disabled={isSaving}
              className="w-full sm:w-auto"
            >
              {t("common.cancel")}
            </Button>
            <Button
              type="button"
              onClick={handleSave}
              disabled={!file || !!validationError || isSaving}
              className="w-full sm:w-auto"
            >
              <Upload size={16} />
              {isSaving ? t("common.saving") : t("common.save")}
            </Button>
          </div>
        </div>
      </section>
    </div>,
    document.body,
  );
}

function validateFile(
  file: File,
  kind: ProfileImageUploadKind,
  t: TFunction,
) {
  if (file.size === 0) return t("profile.media.errors.empty");
  if (!ALLOWED_TYPES.has(file.type)) return t("profile.media.errors.unsupported");
  if (file.size > MAX_BYTES[kind]) {
    return t("profile.media.errors.tooLarge", { size: MAX_BYTES[kind] / (1024 * 1024) });
  }
  return null;
}

function getServerErrorMessage(
  error: unknown,
  kind: ProfileImageUploadKind,
  t: TFunction,
) {
  if (!error) return null;

  if (isAxiosError<ApiErrorBody>(error)) {
    const status = error.response?.status;

    if (status === 413) {
      return t("profile.media.errors.requestTooLarge", { size: maxMbForKind(kind) });
    }

    const body = error.response?.data;
    const rawMessage = getApiErrorMessage(body);
    const localizedMessage = localizeServerMessage(rawMessage, kind, t);
    if (localizedMessage) return localizedMessage;

    if (status) {
      return t("profile.media.errors.uploadFailedWithStatus", { status });
    }
  }

  return t("profile.media.errors.uploadFailed");
}

function getApiErrorMessage(body: ApiErrorBody | string | undefined) {
  if (!body) return undefined;
  if (typeof body === "string") return stripHtml(body);

  const firstValidationMessage = body.errors
    ? Object.values(body.errors).flat()[0]
    : undefined;

  return firstValidationMessage ?? body.error ?? body.detail ?? body.title;
}

function localizeServerMessage(
  message: string | undefined,
  kind: ProfileImageUploadKind,
  t: TFunction,
) {
  if (!message) return null;

  const normalized = message.toLowerCase();
  if (normalized.includes("non-empty")) return t("profile.media.errors.empty");
  if (
    normalized.includes("not exceed") ||
    normalized.includes("too large") ||
    normalized.includes("request entity too large")
  ) {
    return t("profile.media.errors.tooLarge", { size: extractSizeMb(message) ?? maxMbForKind(kind) });
  }
  if (normalized.includes("jpg") || normalized.includes("png") || normalized.includes("webp")) {
    return t("profile.media.errors.unsupported");
  }
  if (normalized.includes("not a valid image")) return t("profile.media.errors.invalidImage");

  if (normalized.includes("unexpected error occurred")) return null;

  return t("profile.media.errors.uploadFailedWithDetail", { detail: message });
}

function extractSizeMb(message: string) {
  const match = message.match(/(\d+(?:[.,]\d+)?)\s*mb/i);
  if (!match) return null;
  return match[1].replace(",", ".");
}

function maxMbForKind(kind: ProfileImageUploadKind) {
  return MAX_BYTES[kind] / (1024 * 1024);
}

function stripHtml(value: string) {
  return value.replace(/<[^>]*>/g, " ").replace(/\s+/g, " ").trim();
}
