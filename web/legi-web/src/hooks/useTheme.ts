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

function resolveIsDark(mode: ThemeMode) {
  return mode === "system" ? systemPrefersDark() : mode === "dark";
}

export function useTheme() {
  const [mode, setModeState] = useState<ThemeMode>(getStoredMode);
  const [isDark, setIsDark] = useState(() => resolveIsDark(getStoredMode()));

  // Apply the resolved theme to the document.
  useEffect(() => {
    document.documentElement.classList.toggle("dark", isDark);
  }, [isDark]);

  // Re-resolve whenever the mode changes.
  useEffect(() => {
    setIsDark(resolveIsDark(mode));
  }, [mode]);

  // Follow the OS live while in "system" mode.
  useEffect(() => {
    if (mode !== "system") return;
    const mq = window.matchMedia("(prefers-color-scheme: dark)");
    const onChange = (e: MediaQueryListEvent) => setIsDark(e.matches);
    mq.addEventListener("change", onChange);
    return () => mq.removeEventListener("change", onChange);
  }, [mode]);

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
