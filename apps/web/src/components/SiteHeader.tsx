import { getTranslations } from "next-intl/server";
import { MapPinLine, SoccerBall } from "@phosphor-icons/react/dist/ssr";
import { Link } from "@/i18n/navigation";
import { LocaleSwitcher } from "@/components/LocaleSwitcher";
import { ThemeToggle } from "@/components/ThemeToggle";
import { UserMenu } from "@/components/UserMenu";

// Продолжает стиль прежней плавающей пилюли (bg-background/80 +
// backdrop-blur-md), просто на всю ширину и sticky — чтобы не читаться как
// чужеродный блок поверх страницы. Ссылок всего две — бургер не нужен,
// на мобилке прячем только подписи, иконки остаются.
export async function SiteHeader() {
  const t = await getTranslations("Nav");
  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";

  return (
    <header className="sticky top-0 z-50 border-b border-brand-border bg-background/80 backdrop-blur-md">
      <div className="mx-auto flex h-14 max-w-5xl items-center justify-between gap-4 px-6">
        <Link href="/" className="flex shrink-0 items-center gap-2">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src="/icon.svg" alt="" width={24} height={24} />
          <span className="font-heading text-base font-semibold text-foreground">game.org.az</span>
        </Link>

        <nav className="flex items-center gap-5 text-sm font-medium text-foreground/70">
          <Link href="/venues" className="flex items-center gap-1.5 transition-colors duration-200 hover:text-foreground">
            <MapPinLine size={18} weight="bold" />
            <span className="hidden sm:inline">{t("venues")}</span>
          </Link>
          <Link href="/sports" className="flex items-center gap-1.5 transition-colors duration-200 hover:text-foreground">
            <SoccerBall size={18} weight="bold" />
            <span className="hidden sm:inline">{t("sports")}</span>
          </Link>
        </nav>

        <div className="flex shrink-0 items-center gap-3">
          <LocaleSwitcher />
          <span className="h-4 w-px bg-foreground/15" />
          <ThemeToggle />
          <span className="h-4 w-px bg-foreground/15" />
          <UserMenu apiUrl={apiUrl} />
        </div>
      </div>
    </header>
  );
}
