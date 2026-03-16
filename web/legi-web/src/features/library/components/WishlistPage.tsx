import { useTranslation } from "react-i18next";

export default function WishlistPage() {
  const { t } = useTranslation();

  return (
    <div>
      <h1 className="text-2xl font-bold text-stone-800">{t("wishlist.title")}</h1>
      <p className="mt-2 text-stone-600">{t("wishlist.subtitle")}</p>
    </div>
  );
}