import { Avatar } from "../../../components/ui/Avatar";
import type { UserProfileDto } from "../../social/types";

interface ProfileHeaderProps {
  profile: UserProfileDto;
}

export function ProfileHeader({ profile }: ProfileHeaderProps) {
  return (
    <div>
      {/* Banner */}
      <div className="h-40 bg-stone-300 dark:bg-dark-raised rounded-xl overflow-hidden">
        {profile.bannerUrl && (
          <img src={profile.bannerUrl} alt="" className="w-full h-full object-cover" />
        )}
      </div>

      {/* Avatar + Info */}
      <div className="relative px-4">
        <div className="-mt-12">
          <Avatar
            src={profile.avatarUrl ?? undefined}
            fallback={profile.username}
            size="xl"
            className="ring-4 ring-white dark:ring-dark-bg"
          />
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
