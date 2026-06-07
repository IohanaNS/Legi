import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { Search, UserSearch } from "lucide-react";
import { Avatar } from "../../../components/ui/Avatar";
import { Button } from "../../../components/ui/Button";
import { Card } from "../../../components/ui/Card";
import { useAuth } from "../../auth/useAuth";
import { FollowButton } from "./FollowButton";
import {
  canSearchUsersByUsername,
  isUsernameSearchPrefixValid,
  normalizeUsernameSearchPrefix,
  useSearchUsers,
} from "../hooks/useSearchUsers";
import type { FollowUserDto } from "../types";

export function FindPeople() {
  const { t } = useTranslation();
  const [searchInput, setSearchInput] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setDebouncedSearch(searchInput);
    }, 300);

    return () => window.clearTimeout(timeoutId);
  }, [searchInput]);

  const trimmedInput = searchInput.trim();
  const normalizedSearch = normalizeUsernameSearchPrefix(debouncedSearch);
  const canSearch = canSearchUsersByUsername(normalizedSearch);
  const isTooShort = trimmedInput.length > 0 && trimmedInput.length < 3;
  const isInvalid = trimmedInput.length >= 3 &&
    !isUsernameSearchPrefixValid(normalizeUsernameSearchPrefix(searchInput));
  const usersQuery = useSearchUsers(debouncedSearch);
  const users = usersQuery.data ?? [];

  return (
    <Card>
      <div className="p-4">
        <div className="mb-3 flex items-center gap-2 text-sm font-medium text-stone-700 dark:text-stone-200">
          <UserSearch size={16} />
          {t("feed.findPeople")}
        </div>

        <div className="relative">
          <Search
            size={15}
            className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-stone-400 dark:text-stone-500"
          />
          <input
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            placeholder={t("feed.findPeoplePlaceholder")}
            className="w-full rounded-lg border border-stone-300 dark:border-dark-raised bg-white dark:bg-dark-raised text-stone-800 dark:text-stone-100 py-1.5 pl-9 pr-3 text-sm focus:outline-none focus:ring-1 focus:ring-green-600"
          />
        </div>

        <div className="mt-3 min-h-10">
          {isTooShort ? (
            <p className="text-xs text-stone-400 dark:text-stone-500">{t("feed.findPeopleMinChars")}</p>
          ) : isInvalid ? (
            <p className="text-xs text-red-600">{t("feed.findPeopleInvalid")}</p>
          ) : usersQuery.isLoading ? (
            <p className="text-xs text-stone-400 dark:text-stone-500">{t("feed.findPeopleLoading")}</p>
          ) : usersQuery.isError ? (
            <div className="space-y-2">
              <p className="text-xs text-stone-500 dark:text-stone-400">{t("feed.findPeopleError")}</p>
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  void usersQuery.refetch();
                }}
              >
                {t("common.retry")}
              </Button>
            </div>
          ) : canSearch && users.length === 0 ? (
            <p className="text-xs text-stone-400 dark:text-stone-500">{t("feed.findPeopleEmpty")}</p>
          ) : users.length > 0 ? (
            <ul className="space-y-3">
              {users.map((user) => (
                <UserSearchRow key={user.userId} user={user} />
              ))}
            </ul>
          ) : (
            <p className="text-xs text-stone-400 dark:text-stone-500">{t("feed.findPeopleHint")}</p>
          )}
        </div>
      </div>
    </Card>
  );
}

function UserSearchRow({ user }: { user: FollowUserDto }) {
  const { user: currentUser } = useAuth();
  const isSelf = currentUser?.userId === user.userId;

  return (
    <li className="flex items-center gap-3">
      <Link to={`/users/${user.userId}`} className="flex-shrink-0">
        <Avatar src={user.avatarUrl ?? undefined} fallback={user.username} size="md" />
      </Link>

      <div className="min-w-0 flex-1">
        <Link
          to={`/users/${user.userId}`}
          className="block truncate text-sm font-medium text-stone-800 dark:text-stone-100 hover:text-green-700"
        >
          @{user.username}
        </Link>
        {user.bio && <p className="truncate text-xs text-stone-500 dark:text-stone-400">{user.bio}</p>}
      </div>

      {!isSelf && (
        <div className="flex-shrink-0">
          <FollowButton userId={user.userId} isFollowing={user.isFollowedByViewer} size="sm" />
        </div>
      )}
    </li>
  );
}
