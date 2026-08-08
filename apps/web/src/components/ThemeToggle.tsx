"use client";

import { useEffect, useState } from "react";
import { Moon, Sun } from "@phosphor-icons/react";

// data-theme стоит на <html> только после ручного выбора (см.
// THEME_INIT_SCRIPT в layout) — пока его нет, реальная тема идёт от
// prefers-color-scheme. Без учёта медиа-запроса при первом клике "next"
// считался от пустого dataset.theme и мог просто повторно выставить то,
// что уже и так показано через media query — переключатель бы не работал.
function getEffectiveTheme(): "light" | "dark" {
  const explicit = document.documentElement.dataset.theme;
  if (explicit === "light" || explicit === "dark") return explicit;
  return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

export function ThemeToggle() {
  // До монтирования не знаем эффективную тему — рендерим пусто, чтобы не
  // мигать не тем значком.
  const [theme, setTheme] = useState<"light" | "dark" | null>(null);

  useEffect(() => {
    setTheme(getEffectiveTheme());
  }, []);

  function toggle() {
    const next = getEffectiveTheme() === "dark" ? "light" : "dark";
    document.documentElement.dataset.theme = next;
    localStorage.setItem("theme", next);
    setTheme(next);
  }

  return (
    <button
      type="button"
      onClick={toggle}
      aria-label={theme === "dark" ? "Светлая тема" : "Тёмная тема"}
      className="flex h-6 w-6 items-center justify-center text-foreground/60 transition-colors duration-200 hover:text-foreground cursor-pointer"
    >
      {theme === "dark" ? <Sun size={17} weight="bold" /> : theme === "light" ? <Moon size={17} weight="bold" /> : null}
    </button>
  );
}
