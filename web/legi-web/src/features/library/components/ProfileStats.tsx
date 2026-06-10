import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";

interface ProfileStatsProps {
  userId: string;
  booksRead: number;
  followers: number;
  following: number;
}

export function ProfileStats({ userId, booksRead, followers, following }: ProfileStatsProps) {
  const { t } = useTranslation();

  const stats = [
    { value: booksRead, label: t("profile.stats.read"), to: `/users/${userId}/read` },
    { value: followers, label: t("profile.stats.followers"), to: `/users/${userId}/followers` },
    { value: following, label: t("profile.stats.following"), to: `/users/${userId}/following` },
  ];

  return (
    <div className="flex gap-6 px-4 py-4 border-b border-stone-200 dark:border-dark-raised">
      {stats.map((stat) => (
        <Link
          key={stat.label}
          to={stat.to}
          className="rounded-md text-center transition-colors hover:text-green-700"
        >
          <p className="text-lg font-bold text-stone-800 dark:text-stone-100">{stat.value}</p>
          <p className="text-xs text-stone-500 dark:text-stone-400">{stat.label}</p>
        </Link>
      ))}
    </div>
  );
}
