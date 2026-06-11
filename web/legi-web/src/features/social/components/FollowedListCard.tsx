import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { ListCard } from "../../library/components/ListCard";
import type { UserListSummaryDto } from "../../library/types";
import { Button } from "../../../components/ui/Button";
import { socialApi } from "../api";
import { interactionKeys } from "../queryKeys";

interface FollowedListCardProps {
  list: UserListSummaryDto;
  /**
   * When set, an unfollow button is shown. Only pass this for the viewer's own
   * followed lists — `collectionUserId` is whose followed-lists cache to refresh
   * after unfollowing (i.e. the current user's id).
   */
  collectionUserId?: string;
}

/**
 * A followed list rendered as a {@link ListCard} (no owner edit/delete controls,
 * since it isn't the viewer's list) with an optional inline unfollow button.
 */
export function FollowedListCard({ list, collectionUserId }: FollowedListCardProps) {
  const { t } = useTranslation();
  const qc = useQueryClient();

  const unfollow = useMutation({
    mutationFn: () => socialApi.unfollowList(list.listId),
    onSuccess: () => {
      if (collectionUserId) {
        void qc.invalidateQueries({ queryKey: interactionKeys.followedLists(collectionUserId) });
      }
      void qc.invalidateQueries({ queryKey: interactionKeys.listSocial(list.listId) });
    },
  });

  return (
    <ListCard
      list={list}
      footerAction={
        collectionUserId ? (
          <Button
            variant="outline"
            size="sm"
            disabled={unfollow.isPending}
            onClick={(e) => {
              e.stopPropagation();
              unfollow.mutate();
            }}
          >
            {t("lists.unfollow")}
          </Button>
        ) : undefined
      }
    />
  );
}
