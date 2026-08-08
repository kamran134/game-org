import { defineRouting } from "next-intl/routing";

export const routing = defineRouting({
  locales: ["az", "ru", "en"],
  defaultLocale: "az",
  localePrefix: "as-needed",
  // По умолчанию next-intl сам подбирает локаль из Accept-Language браузера,
  // из-за чего "/" открывался на английском вместо defaultLocale — az должен
  // быть дефолтом всегда, а не только когда браузер об этом не попросил.
  // Явный выбор пользователя (LocaleSwitcher) при этом всё равно работает —
  // next-intl помнит его через cookie NEXT_LOCALE.
  localeDetection: false,
});
