import { Camera, ImagePlus } from "lucide-react";
import { useTranslation } from "react-i18next";
import { Avatar } from "../../../components/ui/Avatar";
import type { UserProfileDto } from "../../social/types";
import type { ReactNode } from "react";

interface ProfileHeaderProps {
  profile: UserProfileDto;
  action?: ReactNode;
  onEditAvatar?: () => void;
  onEditBanner?: () => void;
}

export function ProfileHeader({ profile, action, onEditAvatar, onEditBanner }: ProfileHeaderProps) {
  const { t } = useTranslation();

  return (
    <div>
      {/* Banner */}
      <div className="relative h-40 bg-stone-300 dark:bg-dark-raised rounded-xl overflow-hidden">
        {profile.bannerUrl && (
          <img src={profile.bannerUrl} alt="" className="w-full h-full object-cover" />
        )}
        {onEditBanner && (
          <button
            type="button"
            onClick={onEditBanner}
            aria-label={t("profile.media.editBanner")}
            title={t("profile.media.editBanner")}
            className="absolute right-3 top-3 flex h-8 w-8 items-center justify-center rounded-full border border-white/70 bg-white/90 text-stone-700 shadow-sm transition-colors hover:bg-white hover:text-stone-900 focus:outline-none focus:ring-2 focus:ring-green-600 focus:ring-offset-2 dark:border-dark-raised dark:bg-dark-card/90 dark:text-stone-200 dark:hover:bg-dark-card dark:hover:text-white dark:focus:ring-offset-dark-bg"
          >
            <ImagePlus size={16} />
          </button>
        )}
      </div>

      {/* Avatar + Info */}
      <div className="relative px-4">
        <div className="-mt-12 flex items-end justify-between">
          <div className="relative">
            <Avatar
              src={profile.avatarUrl ?? undefined}
              fallback={profile.username}
              size="xl"
              className="ring-4 ring-white dark:ring-dark-bg"
            />
            {onEditAvatar && (
              <button
                type="button"
                onClick={onEditAvatar}
                aria-label={t("profile.media.editAvatar")}
                title={t("profile.media.editAvatar")}
                className="absolute bottom-0 right-0 flex h-8 w-8 items-center justify-center rounded-full border border-white bg-white text-stone-700 shadow-sm transition-colors hover:bg-stone-50 hover:text-stone-900 focus:outline-none focus:ring-2 focus:ring-green-600 focus:ring-offset-2 dark:border-dark-raised dark:bg-dark-card dark:text-stone-200 dark:hover:bg-dark-raised dark:hover:text-white dark:focus:ring-offset-dark-bg"
              >
                <Camera size={15} />
              </button>
            )}
          </div>
          {action && <div className="mb-1">{action}</div>}
        </div>

        <div className="mt-3">
          <h1 className="font-serif text-xl font-semibold text-stone-800 dark:text-stone-100">
            @{profile.username}
          </h1>
        </div>

        {/* Bio */}
        {profile.bio && (
          <p className="mt-3 text-sm text-stone-600 dark:text-stone-300 leading-relaxed">{profile.bio}</p>
        )}
      </div>
    </div>
  );
}
