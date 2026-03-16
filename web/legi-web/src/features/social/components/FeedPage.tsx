
import { useTranslation } from "react-i18next";
export default function FeedPage() {
  const { t } = useTranslation();

  return (
    <div>
      <h1 className="text-2xl font-bold text-stone-800">{t("feed.title")}</h1>
      <p className="mt-2 text-stone-600">{t("feed.subtitle")}</p>
    </div>
  );
}