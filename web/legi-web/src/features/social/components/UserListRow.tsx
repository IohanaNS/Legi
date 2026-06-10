import { Link } from "react-router-dom";
import { Avatar } from "../../../components/ui/Avatar";
import { FollowButton } from "./FollowButton";
import { useAuth } from "../../auth/useAuth";
import type { FollowUserDto } from "../types";

/**
 * One user row in a followers/following list: avatar + @username (+ optional bio),
 * linking to the user's profile, with an inline follow button for everyone but self.
 */
export function UserListRow({ user }: { user: FollowUserDto }) {
  const { user: currentUser } = useAuth();
  const isSelf = currentUser?.userId === user.userId;

  return (
    <li className="flex items-center gap-3">
      <Link to={`/users/${user.userId}`}>
        <Avatar src={user.avatarUrl ?? undefined} fallback={user.username} size="md" />
      </Link>
      <div className="min-w-0 flex-1">
        <Link
          to={`/users/${user.userId}`}
          className="block truncate text-sm font-semibold text-stone-800 dark:text-stone-100 hover:text-green-700"
        >
          @{user.username}
        </Link>
        {user.bio && (
          <p className="truncate text-xs text-stone-500 dark:text-stone-400">{user.bio}</p>
        )}
      </div>
      {!isSelf && (
        <FollowButton userId={user.userId} isFollowing={user.isFollowedByViewer} size="sm" />
      )}
    </li>
  );
}
