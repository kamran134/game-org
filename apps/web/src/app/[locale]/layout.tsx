import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { Inter, Oswald } from "next/font/google";
import { NextIntlClientProvider, hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { routing } from "@/i18n/routing";
import { LocaleSwitcher } from "@/components/LocaleSwitcher";
import { ThemeToggle } from "@/components/ThemeToggle";
import "../globals.css";

// Блокирующий инлайн-скрипт до гидратации — ВСЕГДА проставляет data-theme
// (сохранённый выбор или, если его ещё нет, системный prefers-color-scheme
// через matchMedia). Без этого миг до React был бы не тем, а сам CSS
// намеренно не использует @media (prefers-color-scheme) — см. комментарий
// в globals.css про то, как Tailwind иначе запекает тёмное значение мимо
// data-theme.
const THEME_INIT_SCRIPT = `(function(){try{var s=localStorage.getItem('theme');var t=(s==='light'||s==='dark')?s:(window.matchMedia('(prefers-color-scheme: dark)').matches?'dark':'light');document.documentElement.dataset.theme=t;}catch(e){}})();`;

// Спортивный/атлетический шрифтовой дуэт — единый на весь сайт (не только
// лендинг): Barlow/Barlow Condensed кириллицу не тянут в next/font/google,
// поэтому Oswald (заголовки) + Inter (текст), см. коммит с лендингом.
const brandHeading = Oswald({
  variable: "--font-brand-heading",
  weight: ["600", "700"],
  subsets: ["latin", "cyrillic"],
});

const brandSans = Inter({
  variable: "--font-brand-sans",
  weight: ["400", "500", "600"],
  subsets: ["latin", "cyrillic"],
});

export function generateStaticParams() {
  return routing.locales.map((locale) => ({ locale }));
}

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<Metadata> {
  const { locale } = await params;
  const t = await getTranslations({ locale, namespace: "Metadata" });
  return {
    title: t("title"),
    description: t("description"),
    // Нужен, чтобы Next.js резолвил относительные alternates.languages в
    // [locale]/[handle]/page.tsx в абсолютные URL для hreflang.
    metadataBase: new URL(process.env.NEXT_PUBLIC_SITE_URL ?? "https://game.org.az"),
  };
}

export default async function LocaleLayout({
  children,
  params,
}: {
  children: React.ReactNode;
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();

  // Разрешает next-intl статически рендерить страницы этой локали
  // (generateStaticParams + setRequestLocale — обязательная пара).
  setRequestLocale(locale);

  // suppressHydrationWarning — THEME_INIT_SCRIPT ставит data-theme до
  // гидратации, React об этом не знает и иначе шумит про несовпадение.
  return (
    <html
      lang={locale}
      className={`${brandHeading.variable} ${brandSans.variable} h-full antialiased`}
      suppressHydrationWarning
    >
      <head>
        <script dangerouslySetInnerHTML={{ __html: THEME_INIT_SCRIPT }} />
      </head>
      <body className="min-h-full flex flex-col">
        <NextIntlClientProvider>
          <div className="fixed top-4 right-4 z-50 flex items-center gap-3 rounded-full border border-black/10 bg-background/80 px-3.5 py-2 shadow-sm backdrop-blur-md dark:border-white/10">
            <LocaleSwitcher />
            <span className="h-4 w-px bg-foreground/15" />
            <ThemeToggle />
          </div>
          {children}
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
