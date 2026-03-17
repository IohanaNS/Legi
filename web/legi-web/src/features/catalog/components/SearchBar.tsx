import { Search } from "lucide-react";
import { useTranslation } from "react-i18next";

interface SearchBarProps {
  value: string;
  onChange: (value: string) => void;
}

export function SearchBar({ value, onChange }: SearchBarProps) {
  const { t } = useTranslation();

  return (
    <div className="relative">
      <Search
        size={18}
        className="absolute left-3 top-1/2 -translate-y-1/2 text-stone-400"
      />
      <input
        type="text"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={t("explore.searchPlaceholder")}
        className="w-full pl-10 pr-4 py-2.5 bg-white border border-stone-200 rounded-lg text-sm text-stone-800 placeholder:text-stone-400 focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-600 transition-colors"
      />
    </div>
  );
}