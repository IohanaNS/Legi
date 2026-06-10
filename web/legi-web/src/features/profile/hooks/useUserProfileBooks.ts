import { useUserLibrary } from "../../library/hooks/useLibraryBooks";
import type { BackendReadingStatus } from "../../library/types";

interface UseUserProfileBooksOptions {
  enabled?: boolean;
}

export function useUserProfileBooks(
  userId: string | undefined,
  status: BackendReadingStatus,
  { enabled = true }: UseUserProfileBooksOptions = {},
) {
  return useUserLibrary(userId, status, enabled);
}
