"use client";

import { useEffect, useState } from "react";
import { Moon, Sun } from "@phosphor-icons/react";
import { usePathname } from "@/i18n/navigation";

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
  const pathname = usePathname();

  // THEME_INIT_SCRIPT в layout ставит data-theme только один раз, до первой
  // гидратации. При смене локали (LocaleSwitcher) [locale]/layout.tsx —
  // корневой (нет обычного app/layout.tsx над ним), и переход между
  // локалями пересоздаёт <html> без этого атрибута — он тихо пропадает, и
  // CSS откатывается на светлые значения из :root. pathname меняется при
  // любой навигации, включая смену локали, — переустанавливаем атрибут
  // каждый раз, а не только один раз при монтировании.
  useEffect(() => {
    const t = getEffectiveTheme();
    document.documentElement.dataset.theme = t;
    setTheme(t);
  }, [pathname]);

  function toggle() {
    const root = document.documentElement;
    // На странице много transition-colors (кнопки, карточки, hover) — при
    // смене темы они все разом плавно кросс-фейдятся 200мс и ощущаются как
    // лаг вместо мгновенного отклика. На время самого переключения отключаем
    // transition глобально, затем возвращаем — hover вне переключения темы
    // остаётся плавным.
    root.classList.add("theme-switching");
    const next = getEffectiveTheme() === "dark" ? "light" : "dark";
    root.dataset.theme = next;
    localStorage.setItem("theme", next);
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
