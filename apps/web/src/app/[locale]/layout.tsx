import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { Geist, Geist_Mono } from "next/font/google";
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

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
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

  return (
    <html lang={locale} className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}>
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
