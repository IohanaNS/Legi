import { useQuery } from "@tanstack/react-query";
import { socialApi } from "../api";
import { socialKeys } from "../queryKeys";

const USER_SEARCH_LIMIT = 10;
const USERNAME_PREFIX_RE = /^[a-z0-9_]+$/i;

export function normalizeUsernameSearchPrefix(value: string) {
  return value.trim().toLowerCase();
}

export function isUsernameSearchPrefixValid(value: string) {
  return USERNAME_PREFIX_RE.test(value);
}

export function canSearchUsersByUsername(value: string) {
  const usernamePrefix = normalizeUsernameSearchPrefix(value);
  return usernamePrefix.length >= 3 && isUsernameSearchPrefixValid(usernamePrefix);
}

export function useSearchUsers(usernamePrefix: string) {
  const normalizedPrefix = normalizeUsernameSearchPrefix(usernamePrefix);

  return useQuery({
    queryKey: socialKeys.userSearch(normalizedPrefix, USER_SEARCH_LIMIT),
    queryFn: () => socialApi.searchUsers(normalizedPrefix, USER_SEARCH_LIMIT),
    enabled: canSearchUsersByUsername(normalizedPrefix),
    staleTime: 30_000,
  });
}
