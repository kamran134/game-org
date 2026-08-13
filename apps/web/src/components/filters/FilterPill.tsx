import type { ReactNode } from "react";
import { Link } from "@/i18n/navigation";

// Тот же стиль пилюль, что был у вкладок "Мои"/past-upcoming в EventsList/
// ClubsList (Шаг 21) — вынесен сюда, раз Шаг 24 добавляет их ещё в
// нескольких местах (мои клубы, бесплатные, крытые/открытые площадки).
export function FilterPill({ href, active, children }: { href: string; active: boolean; children: ReactNode }) {
  return (
    <Link
      href={href}
      className={`rounded-full border px-4 py-1.5 text-sm font-medium transition-colors duration-200 ${
        active
          ? "border-brand-primary bg-brand-primary/10 text-brand-primary"
          : "border-brand-border text-foreground/60 hover:border-brand-primary/40"
      }`}
    >
      {children}
    </Link>
  );
}
