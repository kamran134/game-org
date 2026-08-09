"use client";

import { useEffect, useState } from "react";
import { Moon, Sun } from "@phosphor-icons/react";
import { THEME_COOKIE, THEME_COOKIE_MAX_AGE, isTheme, type Theme } from "@/lib/theme";

// Источник правды — data-theme на <html>: на сервере он отрендерен из куки
// ([locale]/layout.tsx), на самом первом визите его успевает проставить
// THEME_INIT_SCRIPT из prefers-color-scheme. matchMedia здесь — только
// подстраховка на случай, если ни того, ни другого не случилось.
function getEffectiveTheme(): Theme {
  const explicit = document.documentElement.dataset.theme;
  if (isTheme(explicit)) return explicit;
  return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

export function ThemeToggle() {
  // До монтирования не знаем эффективную тему — рендерим пусто, чтобы не
  // мигать не тем значком.
  const [theme, setTheme] = useState<Theme | null>(null);

  // Только для иконки. Восстанавливать тему при навигации не нужно:
  // data-theme приходит с сервера в JSX, поэтому переживает смену локали
  // сам по себе — никакой завязки на pathname/локаль здесь нет и быть не
  // должно.
  useEffect(() => {
    setTheme(getEffectiveTheme());
  }, []);

  function toggle() {
    const root = document.documentElement;
    // На странице много transition-colors (кнопки, карточки, hover) — при
    // смене темы они все разом плавно кросс-фейдятся 200мс и ощущаются как
    // лаг вместо мгновенного отклика. На время самого переключения отключаем
    // transition глобально, затем возвращаем — hover вне переключения темы
    // остаётся плавным.
    root.classList.add("theme-switching");
    const next: Theme = getEffectiveTheme() === "dark" ? "light" : "dark";
    root.dataset.theme = next;
    document.cookie = `${THEME_COOKIE}=${next};path=/;max-age=${THEME_COOKIE_MAX_AGE};samesite=lax`;
    setTheme(next);
    requestAnimationFrame(() => {
      requestAnimationFrame(() => root.classList.remove("theme-switching"));
    });
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
