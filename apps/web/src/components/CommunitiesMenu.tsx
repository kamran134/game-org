"use client";

import { useEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { CaretDown, UsersThree, UsersFour } from "@phosphor-icons/react";
import { Link } from "@/i18n/navigation";

// Клубы и Группы — одна и та же сущность (Club.Kind), различается только
// формальность/каталог (Шаг 20). В шапке десктопа сворачиваем их в один
// пункт с выпадающим списком, чтобы не плодить кнопки нава; на мобилке
// (MobileNav) оба пункта остаются плоскими — там и так список, разворачивать
// нечего.
export function CommunitiesMenu() {
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
        aria-expanded={open}
        className="flex cursor-pointer items-center gap-1.5 transition-colors duration-200 hover:text-foreground"
      >
        <UsersThree size={18} weight="bold" />
        {t("communities")}
        <CaretDown size={12} weight="bold" className={`transition-transform duration-200 ${open ? "rotate-180" : ""}`} />
      </button>

      {open && (
        <div className="absolute left-0 top-8 z-50 w-44 rounded-2xl border border-brand-border bg-background p-2 shadow-lg">
          <Link
            href="/clubs"
            onClick={() => setOpen(false)}
            className="flex items-center gap-2.5 rounded-xl px-3 py-2 text-sm text-foreground/80 transition-colors duration-200 hover:bg-brand-muted hover:text-foreground"
          >
            <UsersThree size={17} weight="bold" />
            {t("clubs")}
          </Link>
          <Link
            href="/groups"
            onClick={() => setOpen(false)}
            className="flex items-center gap-2.5 rounded-xl px-3 py-2 text-sm text-foreground/80 transition-colors duration-200 hover:bg-brand-muted hover:text-foreground"
          >
            <UsersFour size={17} weight="bold" />
            {t("groups")}
          </Link>
        </div>
      )}
    </div>
  );
}
