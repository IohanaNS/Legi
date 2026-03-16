import { NavLink, Outlet } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  Newspaper,
  Compass,
  List,
  Gift,
  User,
  Search,
  Settings,
  LogOut
} from "lucide-react";

const navItems = [
  { to: "/feed", labelKey: "nav.feed", icon: Newspaper },
  { to: "/explore", labelKey: "nav.explore", icon: Compass },
  { to: "/lists", labelKey: "nav.lists", icon: List },
  { to: "/wishlist", labelKey: "nav.wishlist", icon: Gift },
  { to: "/profile", labelKey: "nav.profile", icon: User },
];

export default function Layout() {
  const { t, i18n } = useTranslation();

  const toggleLanguage = () => {
    const newLang = i18n.language === "pt-BR" ? "en" : "pt-BR";
    i18n.changeLanguage(newLang);
  };

  return (
    <div className="flex min-h-screen bg-stone-50">
      {/* Sidebar */}
      <aside className="w-56 bg-white border-r border-stone-200 flex flex-col fixed h-full">
        {/* Logo */}
        <div className="p-4">
          <h1 className="text-xl font-bold text-green-800">📖 Legi</h1>
        </div>

        {/* Busca */}
        <div className="px-3 mb-2">
          <div className="flex items-center gap-2 bg-stone-100 rounded-lg px-3 py-2 text-stone-500 text-sm">
            <Search size={16} />
            <span>{t("common.search")}</span>
          </div>
        </div>

        {/* Navegação */}
        <nav className="flex-1 px-3">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                `flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors ${
                  isActive
                    ? "bg-green-700 text-white"
                    : "text-stone-700 hover:bg-stone-100"
                }`
              }
            >
              <item.icon size={18} />
              {t(item.labelKey)}
            </NavLink>
          ))}
        </nav>

        {/* Troca de idioma */}
        <div className="px-3 mb-2">
          <button
            onClick={toggleLanguage}
            className="w-full text-left px-3 py-2 text-xs text-stone-500 hover:text-stone-700 hover:bg-stone-100 rounded-lg transition-colors"
          >
            {i18n.language === "pt-BR" ? "🇺🇸 English" : "🇧🇷 Português"}
          </button>
        </div>

        {/* Usuário no rodapé */}
        <div className="border-t border-stone-200 p-3">
          <div className="flex items-center gap-3 px-3 py-2">
            <div className="w-8 h-8 bg-stone-300 rounded-full" />
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-stone-800 truncate">Ana Silva</p>
              <p className="text-xs text-stone-500 truncate">@anasilva</p>
            </div>
            <Settings size={16} className="text-stone-400" />
          </div>
          <button className="flex items-center gap-2 px-3 py-1.5 text-xs text-stone-500 hover:text-stone-700">
            <LogOut size={14} />
            {t("common.logout")}
          </button>
        </div>
      </aside>

      {/* Conteúdo principal */}
      <main className="flex-1 ml-56 p-8">
        <Outlet />
      </main>
    </div>
  );
}