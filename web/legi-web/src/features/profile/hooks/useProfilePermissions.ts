import { useMemo } from "react";
import { useAuth } from "../../auth/useAuth";

export interface ProfilePermissions {
  isOwnProfile: boolean;
  canEditProfile: boolean;
  canEditLibrary: boolean;
  canEditLists: boolean;
  canFollow: boolean;
  canReactToActivity: boolean;
}

export function getProfilePermissions(
  targetUserId: string | undefined,
  viewerUserId: string | undefined,
): ProfilePermissions {
  const isOwnProfile = !!targetUserId && !!viewerUserId && targetUserId === viewerUserId;

  return {
    isOwnProfile,
    canEditProfile: isOwnProfile,
    canEditLibrary: isOwnProfile,
    canEditLists: isOwnProfile,
    canFollow: !!targetUserId && !!viewerUserId && !isOwnProfile,
    canReactToActivity: !!viewerUserId,
  };
}

export function useProfilePermissions(targetUserId: string | undefined) {
  const { user } = useAuth();

  return useMemo(
    () => getProfilePermissions(targetUserId, user?.userId),
    [targetUserId, user?.userId],
  );
}
