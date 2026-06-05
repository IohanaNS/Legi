import { useTranslation } from "react-i18next";
import { Globe, Lock } from "lucide-react";
import { Card } from "../../../components/ui/Card";
import { Badge } from "../../../components/ui/Badge";
import type { UserListSummaryDto } from "../types";

interface ListCardProps {
  list: UserListSummaryDto;
}

export function ListCard({ list }: ListCardProps) {
  const { t } = useTranslation();

  return (
    <Card className="cursor-pointer hover:border-stone-300 transition-colors">
      <div className="p-4">
        <div className="flex items-center gap-2 mb-1">
          <h3 className="font-semibold text-stone-800 truncate">{list.name}</h3>
          <Badge variant={list.isPublic ? "success" : "secondary"}>
            <span className="flex items-center gap-1">
              {list.isPublic ? <Globe size={10} /> : <Lock size={10} />}
              {list.isPublic ? t("lists.public") : t("lists.private")}
            </span>
          </Badge>
        </div>
        {list.description && (
          <p className="text-xs text-stone-500 line-clamp-2">{list.description}</p>
        )}
        <p className="text-xs text-stone-400 mt-2">
          {t("lists.booksCount", { count: list.booksCount })}
        </p>
      </div>
    </Card>
  );
}
