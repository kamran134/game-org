"use client";

import { useLocale } from "next-intl";
import { routing } from "@/i18n/routing";
import { usePathname, useRouter } from "@/i18n/navigation";

const LABELS: Record<(typeof routing.locales)[number], string> = {
  az: "AZ",
  ru: "RU",
  en: "EN",
};

export function LocaleSwitcher() {
  const locale = useLocale();
  const pathname = usePathname();
  const router = useRouter();

  return (
    <div className="fixed top-4 right-4 z-50 flex items-center gap-2 rounded-full border border-black/10 bg-background/80 px-3.5 py-2 text-sm font-medium shadow-sm backdrop-blur-md dark:border-white/10">
      {routing.locales.map((l, i) => (
        <span key={l} className="flex items-center gap-2">
          {i > 0 && <span className="text-foreground/25">·</span>}
          <button
            type="button"
            onClick={() => router.replace(pathname, { locale: l })}
            aria-current={l === locale ? "true" : undefined}
            className={`cursor-pointer transition-colors duration-200 ${
              l === locale ? "text-foreground" : "text-foreground/50 hover:text-foreground"
            }`}
          >
            {LABELS[l]}
          </button>
        </span>
      ))}
    </div>
  );
}
