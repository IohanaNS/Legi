import { useEffect, useRef, useState } from "react";
import { NavLink, Outlet } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  Newspaper,
  Compass,
  List,
  Gift,
  User,
  LogOut,
  Moon,
  ChevronUp,
} from "lucide-react";
import { useAuth } from "../features/auth/useAuth";
import { useTheme } from "../hooks/useTheme";
import { GlobalSearch } from "../features/search/components/GlobalSearch";

const navItems = [
  { to: "/feed", labelKey: "nav.feed", icon: Newspaper },
  { to: "/explore", labelKey: "nav.explore", icon: Compass },
  { to: "/lists", labelKey: "nav.lists", icon: List },
  { to: "/wishlist", labelKey: "nav.wishlist", icon: Gift },
  { to: "/profile", labelKey: "nav.profile", icon: User },
];

export default function Layout() {
  const { t, i18n } = useTranslation();
  const { user, logout } = useAuth();
  const { isDark, toggle: toggleTheme } = useTheme();
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setUserMenuOpen(false);
      }
    }
    if (userMenuOpen) document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [userMenuOpen]);

  const toggleLanguage = () => {
    i18n.changeLanguage(i18n.language === "pt-BR" ? "en" : "pt-BR");
  };

  return (
    <div className="flex min-h-screen bg-parchment dark:bg-dark-bg">
      {/* Sidebar */}
      <aside className="w-56 bg-forest-950 dark:bg-dark-sidebar flex flex-col fixed h-full">
        {/* Logo */}
        <div className="p-4">
          <h1 className="font-serif text-xl font-semibold text-green-300">📖 Legi</h1>
        </div>

        {/* Busca */}
        <div className="px-3 mb-2">
          <GlobalSearch />
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
                    ? "bg-green-600 text-white"
                    : "text-green-200 hover:bg-white/10 hover:text-white"
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
            className="w-full text-left px-3 py-2 text-xs text-green-400 hover:text-white hover:bg-white/10 rounded-lg transition-colors"
          >
            {i18n.language === "pt-BR" ? "🇺🇸 English" : "🇧🇷 Português"}
          </button>
        </div>

        {/* Usuário no rodapé */}
        <div className="relative p-3 border-t border-white/10" ref={menuRef}>
          {/* Popover */}
          {userMenuOpen && (
            <div className="absolute bottom-full left-2 right-2 mb-1 rounded-xl bg-forest-900 dark:bg-dark-card border border-white/10 shadow-2xl overflow-hidden">
              {/* User info */}
              <div className="flex items-center gap-3 px-4 py-3 border-b border-white/10">
                <div className="w-10 h-10 bg-green-800 rounded-full flex items-center justify-center text-sm font-semibold text-green-100 uppercase shrink-0">
                  {user?.username?.charAt(0) ?? "?"}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-semibold text-white truncate">
                    {user?.username ?? ""}
                  </p>
                  <p className="text-xs text-green-400 truncate">{user?.email ?? ""}</p>
                </div>
              </div>

              {/* Dark mode toggle */}
              <button
                onClick={toggleTheme}
                className="flex w-full items-center justify-between px-4 py-3 text-sm text-green-200 hover:bg-white/10 transition-colors"
              >
                <span className="flex items-center gap-3">
                  <Moon size={16} />
                  {t("theme.darkMode")}
                </span>
                {/* Toggle pill */}
                <div
                  className={`relative w-9 h-5 rounded-full transition-colors ${
                    isDark ? "bg-brand" : "bg-white/20"
                  }`}
                >
                  <span
                    className={`absolute top-0.5 left-0.5 w-4 h-4 bg-white rounded-full shadow-sm transition-transform ${
                      isDark ? "translate-x-4" : "translate-x-0"
                    }`}
                  />
                </div>
              </button>

              {/* Logout */}
              <button
                onClick={() => {
                  logout();
                  setUserMenuOpen(false);
                }}
                className="flex w-full items-center gap-3 px-4 py-3 text-sm text-green-200 hover:bg-white/10 transition-colors border-t border-white/10"
              >
                <LogOut size={16} />
                {t("common.logout")}
              </button>
            </div>
          )}

          {/* Trigger */}
          <button
            onClick={() => setUserMenuOpen((o) => !o)}
            className="flex w-full items-center gap-3 px-3 py-2 rounded-lg hover:bg-white/10 transition-colors"
          >
            <div className="w-8 h-8 bg-green-800 rounded-full flex items-center justify-center text-xs font-semibold text-green-100 uppercase shrink-0">
              {user?.username?.charAt(0) ?? "?"}
            </div>
            <div className="flex-1 min-w-0 text-left">
              <p className="text-sm font-medium text-green-100 truncate">
                @{user?.username ?? ""}
              </p>
            </div>
            <ChevronUp
              size={14}
              className={`text-green-500 shrink-0 transition-transform ${
                userMenuOpen ? "" : "rotate-180"
              }`}
            />
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
