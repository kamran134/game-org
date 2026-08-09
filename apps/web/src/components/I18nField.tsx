"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import type { LocalizedText } from "@/lib/localized";

const TABS = ["az", "ru", "en"] as const;
type Tab = (typeof TABS)[number];

const fieldClass =
  "w-full rounded-xl border border-brand-border bg-background px-3 py-2 text-foreground outline-none transition-colors duration-200 focus:border-brand-primary focus:ring-2 focus:ring-brand-primary/20 dark:bg-brand-muted";

export function I18nField({
  label,
  value,
  onChange,
  multiline,
  maxLength,
  placeholder,
}: {
  label: string;
  value: LocalizedText;
  onChange: (next: LocalizedText) => void;
  multiline?: boolean;
  maxLength?: number;
  placeholder?: string;
}) {
  const t = useTranslations("I18nField");
  const [active, setActive] = useState<Tab>("az");

  function setLang(lang: Tab, text: string) {
    onChange({ ...value, [lang]: text });
  }

  return (
    <div className="flex flex-col gap-1">
      <span className="text-sm font-medium text-foreground">{label}</span>

      <div className="flex gap-1.5" role="tablist">
        {TABS.map((tab) => {
          const filled = !!value[tab];
          return (
            <button
              key={tab}
              type="button"
              role="tab"
              aria-selected={active === tab}
              onClick={() => setActive(tab)}
              className={`flex cursor-pointer items-center gap-1.5 rounded-full border px-2.5 py-1 text-xs font-semibold uppercase transition-colors duration-200 ${
                active === tab
                  ? "border-brand-primary bg-brand-primary/10 text-brand-primary"
                  : "border-brand-border text-foreground/60 hover:text-foreground"
              }`}
            >
              {tab}
              <span
                className={`h-1.5 w-1.5 rounded-full ${filled ? "bg-brand-primary" : "bg-foreground/25"}`}
                aria-label={filled ? t("filled") : t("empty")}
              />
            </button>
          );
        })}
      </div>

      {multiline ? (
        <textarea
          className={fieldClass}
          value={value[active] ?? ""}
          onChange={(e) => setLang(active, e.target.value)}
          maxLength={maxLength}
          placeholder={placeholder}
          rows={3}
        />
      ) : (
        <input
          className={fieldClass}
          value={value[active] ?? ""}
          onChange={(e) => setLang(active, e.target.value)}
          maxLength={maxLength}
          placeholder={placeholder}
        />
      )}
    </div>
  );
}
