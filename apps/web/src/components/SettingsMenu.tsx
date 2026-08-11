"use client";

import { useEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { Gear } from "@phosphor-icons/react";
import { LocaleSwitcher } from "@/components/LocaleSwitcher";
import { ThemeToggle } from "@/components/ThemeToggle";

// Схлопывает язык+тему в одну иконку — в шапке уже 4 пункта нава
// (События/Площадки/Клубы/Виды спорта) плюс юзер-меню с колокольчиком,
// места на всех не хватает. Сама логика языка/темы не дублируется —
// LocaleSwitcher/ThemeToggle просто рендерятся внутри дропдауна как есть.
export function SettingsMenu() {
  const t = useTranslations("Nav");
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function onClickOutside(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    }
    document.addEventListener("mousedown", onClickOutside);
    return () => document.removeEventListener("mousedown", onClickOutside);
  }, []);

  return (
    <div className="relative" ref={ref}>
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        aria-label={t("settings")}
        aria-expanded={open}
        className="flex h-8 w-8 cursor-pointer items-center justify-center text-foreground/70 transition-colors duration-200 hover:text-foreground"
      >
        <Gear size={19} weight="bold" />
      </button>

      {open && (
        <div className="absolute right-0 top-10 z-50 w-48 rounded-2xl border border-brand-border bg-background p-4 shadow-lg">
          <div className="flex flex-col gap-2">
            <span className="text-xs font-medium uppercase tracking-wide text-foreground/40">{t("language")}</span>
            <LocaleSwitcher />
          </div>
          <div className="mt-4 flex items-center justify-between border-t border-brand-border pt-4">
            <span className="text-xs font-medium uppercase tracking-wide text-foreground/40">{t("theme")}</span>
            <ThemeToggle />
          </div>
        </div>
      )}
    </div>
  );
}
