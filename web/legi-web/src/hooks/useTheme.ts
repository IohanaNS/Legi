import { useState, useEffect } from "react";

export type ThemeMode = "light" | "dark" | "system";

function getStoredMode(): ThemeMode {
  const saved = localStorage.getItem("legi.theme");
  if (saved === "light" || saved === "dark") return saved;
  return "system";
}

function systemPrefersDark() {
  return window.matchMedia("(prefers-color-scheme: dark)").matches;
}

export function useTheme() {
  const [mode, setModeState] = useState<ThemeMode>(getStoredMode);
  const [systemDark, setSystemDark] = useState(systemPrefersDark);

  // Derived during render — no state to sync via an effect.
  const isDark = mode === "system" ? systemDark : mode === "dark";

  // Apply the resolved theme to the document.
  useEffect(() => {
    document.documentElement.classList.toggle("dark", isDark);
  }, [isDark]);

  // Track the OS preference live (only affects the UI while in "system" mode).
  useEffect(() => {
    const mq = window.matchMedia("(prefers-color-scheme: dark)");
    const onChange = (e: MediaQueryListEvent) => setSystemDark(e.matches);
    mq.addEventListener("change", onChange);
    return () => mq.removeEventListener("change", onChange);
  }, []);

  const setMode = (next: ThemeMode) => {
    if (next === "system") localStorage.removeItem("legi.theme");
    else localStorage.setItem("legi.theme", next);
    setModeState(next);
  };

  return {
    isDark,
    mode,
    setMode,
    toggle: () => setMode(isDark ? "light" : "dark"),
  };
}
