import { useTranslation } from "react-i18next";
import { Pencil, CheckCircle } from "lucide-react";
import { Avatar } from "../../../components/ui/Avatar";
import { Badge } from "../../../components/ui/Badge";
import { Button } from "../../../components/ui/Button";
import type { UserProfile } from "../types";

interface ProfileHeaderProps {
  profile: UserProfile;
}

export function ProfileHeader({ profile }: ProfileHeaderProps) {
  const { t } = useTranslation();

  return (
    <div>
      {/* Cover */}
      <div className="h-40 bg-stone-300 rounded-xl overflow-hidden">
        {profile.coverUrl && (
          <img src={profile.coverUrl} alt="" className="w-full h-full object-cover" />
        )}
      </div>

      {/* Avatar + Info */}
      <div className="relative px-4">
        <div className="-mt-12">
          <Avatar
            src={profile.avatarUrl}
            fallback={profile.name}
            size="xl"
            className="ring-4 ring-white"
          />
        </div>

        <div className="flex items-start justify-between mt-3">
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-xl font-bold text-stone-800">{profile.name}</h1>
              {profile.isVerified && (
                <CheckCircle size={18} className="text-green-600 fill-green-600" />
              )}
            </div>
            <p className="text-sm text-stone-500">@{profile.username}</p>
          </div>

          <Button variant="outline" size="sm">
            <Pencil size={14} />
            {t("profile.editProfile")}
          </Button>
        </div>

        {/* Bio */}
        {profile.bio && (
          <p className="mt-3 text-sm text-stone-600 leading-relaxed">{profile.bio}</p>
        )}

        {/* Genre tags */}
        <div className="flex flex-wrap gap-2 mt-3">
          {profile.genres.map((genre) => (
            <Badge key={genre} variant="outline">{genre}</Badge>
          ))}
        </div>
      </div>
    </div>
  );
}