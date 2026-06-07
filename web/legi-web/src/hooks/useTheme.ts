import { useState, useEffect } from "react";

export function useTheme() {
  const [isDark, setIsDark] = useState(
    () => document.documentElement.classList.contains("dark"),
  );

  useEffect(() => {
    if (isDark) {
      document.documentElement.classList.add("dark");
      localStorage.setItem("legi.theme", "dark");
    } else {
      document.documentElement.classList.remove("dark");
      localStorage.setItem("legi.theme", "light");
    }
  }, [isDark]);

  return { isDark, toggle: () => setIsDark((d) => !d) };
}
