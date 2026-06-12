import { useMutation, useQueryClient } from "@tanstack/react-query";
import { socialApi } from "../api";
import { socialKeys } from "../queryKeys";
import type { UserProfileDto } from "../types";

export type ProfileImageUploadKind = "avatar" | "banner";

interface UploadProfileImageVars {
  userId: string;
  kind: ProfileImageUploadKind;
  file: File;
}

export function useProfileImageUpload() {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: ({ kind, file }: UploadProfileImageVars) =>
      kind === "avatar"
        ? socialApi.uploadProfileAvatar(file)
        : socialApi.uploadProfileBanner(file),
    onSuccess: (data, { userId, kind }) => {
      qc.setQueryData<UserProfileDto>(socialKeys.profile(userId), (profile) =>
        profile
          ? {
              ...profile,
              avatarUrl: kind === "avatar" ? data.url : profile.avatarUrl,
              bannerUrl: kind === "banner" ? data.url : profile.bannerUrl,
            }
          : profile,
      );

      qc.invalidateQueries({ queryKey: socialKeys.profile(userId) });
      qc.invalidateQueries({ queryKey: socialKeys.userSearches() });
    },
  });
}
