"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { List, X, CalendarBlank, MapPinLine, SoccerBall, UsersThree } from "@phosphor-icons/react";
import { Link } from "@/i18n/navigation";
import { LocaleSwitcher } from "@/components/LocaleSwitcher";
import { ThemeToggle } from "@/components/ThemeToggle";
import { UserMenu } from "@/components/UserMenu";

// Бургер только на мобилке (sm:hidden на корне) — на десктопе весь этот
// компонент не рендерит ничего видимого, полная шапка идёт отдельным блоком
// в SiteHeader. Панель — absolute от <header> (position: sticky уже даёт ему
// containing block, отдельный relative не нужен), поэтому растягивается на
// всю ширину экрана независимо от того, что кнопка сидит внутри узкого
// max-w-5xl контейнера шапки.
export function MobileNav({ apiUrl }: { apiUrl: string }) {
  const t = useTranslations("Nav");
  const [open, setOpen] = useState(false);

  return (
    <div className="sm:hidden">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        aria-label={t(open ? "close" : "menu")}
        aria-expanded={open}
        className="flex h-8 w-8 cursor-pointer items-center justify-center text-foreground/70 transition-colors duration-200 hover:text-foreground"
      >
        {open ? <X size={20} weight="bold" /> : <List size={20} weight="bold" />}
      </button>

      {open && (
        <div className="absolute inset-x-0 top-14 z-40 border-b border-brand-border bg-background/95 px-6 py-4 shadow-md backdrop-blur-md">
          <nav className="flex flex-col gap-4 text-sm font-medium text-foreground/80">
            <Link
              href="/events"
              onClick={() => setOpen(false)}
              className="flex items-center gap-2.5 transition-colors duration-200 hover:text-foreground"
            >
              <CalendarBlank size={18} weight="bold" />
              {t("events")}
            </Link>
            <Link
              href="/venues"
              onClick={() => setOpen(false)}
              className="flex items-center gap-2.5 transition-colors duration-200 hover:text-foreground"
            >
              <MapPinLine size={18} weight="bold" />
              {t("venues")}
            </Link>
            <Link
              href="/clubs"
              onClick={() => setOpen(false)}
              className="flex items-center gap-2.5 transition-colors duration-200 hover:text-foreground"
            >
              <UsersThree size={18} weight="bold" />
              {t("clubs")}
            </Link>
            <Link
              href="/sports"
              onClick={() => setOpen(false)}
              className="flex items-center gap-2.5 transition-colors duration-200 hover:text-foreground"
            >
              <SoccerBall size={18} weight="bold" />
              {t("sports")}
            </Link>
          </nav>

          <div className="mt-4 flex items-center justify-between border-t border-brand-border pt-4">
            <LocaleSwitcher />
            <ThemeToggle />
          </div>

          <div className="mt-4 border-t border-brand-border pt-4" onClick={() => setOpen(false)}>
            <UserMenu apiUrl={apiUrl} />
          </div>
        </div>
      )}
    </div>
  );
}
