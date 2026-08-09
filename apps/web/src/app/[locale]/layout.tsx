import type { Metadata } from "next";
import { cookies } from "next/headers";
import { notFound } from "next/navigation";
import { Inter, Oswald } from "next/font/google";
import { NextIntlClientProvider, hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { routing } from "@/i18n/routing";
import { SiteHeader } from "@/components/SiteHeader";
import { THEME_COOKIE, isTheme } from "@/lib/theme";
import "../globals.css";

// Фолбэк только для случая «куки ещё нет» (первый визит): резолвит тему из
// prefers-color-scheme и сразу пишет куку, чтобы уже следующий рендер шёл с
// сервера. Когда кука есть — data-theme приходит из JSX ниже, и этот скрипт
// не трогает атрибут вообще.
//
// Почему кука, а не localStorage: [locale]/layout.tsx — корневой layout (нет
// app/layout.tsx над ним), поэтому смена локали пересоздаёт <html>. Атрибут,
// выставленный императивно из JS, при этом теряется, а отрендеренный из JSX —
// нет, потому что им владеет React. Кука — единственный источник темы,
// доступный серверу, и это то, что развязывает тему и язык окончательно.
const THEME_INIT_SCRIPT = `(function(){try{if(/(?:^|;\\s*)${THEME_COOKIE}=(light|dark)/.test(document.cookie))return;var t=window.matchMedia('(prefers-color-scheme: dark)').matches?'dark':'light';document.documentElement.dataset.theme=t;document.cookie='${THEME_COOKIE}='+t+';path=/;max-age=31536000;samesite=lax';}catch(e){}})();`;

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

  // Тема приезжает с сервера отдельно от локали — они никак не связаны:
  // локаль живёт в URL, тема в куке. Смена одного не задевает другое.
  const themeCookie = (await cookies()).get(THEME_COOKIE)?.value;
  const theme = isTheme(themeCookie) ? themeCookie : undefined;

  // suppressHydrationWarning — на самом первом визите (куки ещё нет)
  // THEME_INIT_SCRIPT ставит data-theme до гидратации, React об этом не
  // знает и иначе шумит про несовпадение.
  return (
    <html
      lang={locale}
      data-theme={theme}
      className={`${brandHeading.variable} ${brandSans.variable} h-full antialiased`}
      suppressHydrationWarning
    >
      <head>
        <script dangerouslySetInnerHTML={{ __html: THEME_INIT_SCRIPT }} />
      </head>
      <body className="min-h-full flex flex-col">
        <NextIntlClientProvider>
          <SiteHeader />
          {children}
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
