import { useTranslation } from "react-i18next";
import { Button } from "../../../components/ui/Button";
import { useToggleFollow } from "../hooks/useToggleFollow";

interface FollowButtonProps {
  userId: string;
  isFollowing: boolean;
  size?: "sm" | "md";
}

export function FollowButton({ userId, isFollowing, size = "md" }: FollowButtonProps) {
  const { t } = useTranslation();
  const toggleFollow = useToggleFollow();

  return (
    <Button
      variant={isFollowing ? "outline" : "primary"}
      size={size}
      disabled={toggleFollow.isPending}
      onClick={() => toggleFollow.mutate({ userId, isFollowing })}
    >
      {isFollowing ? t("feed.unfollow") : t("feed.follow")}
    </Button>
  );
}
