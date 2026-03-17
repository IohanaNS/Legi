import { useTranslation } from "react-i18next";

interface ProfileStatsProps {
  booksRead: number;
  followers: number;
  following: number;
}

export function ProfileStats({ booksRead, followers, following }: ProfileStatsProps) {
  const { t } = useTranslation();

  const stats = [
    { value: booksRead, label: t("profile.stats.read") },
    { value: followers, label: t("profile.stats.followers") },
    { value: following, label: t("profile.stats.following") },
  ];

  return (
    <div className="flex gap-6 px-4 py-4 border-b border-stone-200">
      {stats.map((stat) => (
        <div key={stat.label} className="text-center">
          <p className="text-lg font-bold text-stone-800">{stat.value}</p>
          <p className="text-xs text-stone-500">{stat.label}</p>
        </div>
      ))}
    </div>
  );
}