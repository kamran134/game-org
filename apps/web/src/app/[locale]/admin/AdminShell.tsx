"use client";

import { useEffect, useState, type ReactNode } from "react";
import { useLocale, useTranslations } from "next-intl";
import { ChartBar, Users, MapPinLine, Flag, FileText } from "@phosphor-icons/react";
import { Link, usePathname, useRouter } from "@/i18n/navigation";
import { fetchMe, type MeProfile } from "@/lib/authApi";
import { getAdminOverview, type AdminOverview } from "@/lib/adminApi";
import { AdminRoleProvider } from "./AdminRoleContext";

const SECTIONS = [
  { href: "/admin", key: "overview", icon: ChartBar },
  { href: "/admin/users", key: "users", icon: Users },
  { href: "/admin/venues", key: "venues", icon: MapPinLine },
  { href: "/admin/reports", key: "reports", icon: Flag },
  { href: "/admin/claims", key: "claims", icon: FileText },
] as const;

// Двухколоночный layout вместо вкладок ModerationTabs — та же причина, что
// в Шаге 21 для фильтров списков: активный раздел живёт в URL (сама
// маршрутизация Next.js), а не в локальном useState, поэтому ссылка на
// конкретный раздел всегда открывает именно его. Сайдбар превращается
// в <select> ниже md, а не прячется — на панели модератора она нужна
// всегда, не только на десктопе.
export function AdminShell({ apiUrl, children }: { apiUrl: string; children: ReactNode }) {
  const t = useTranslations("Admin");
  const locale = useLocale();
  const pathname = usePathname();
  const router = useRouter();

  const [me, setMe] = useState<MeProfile | null | undefined>(undefined);
  const [overview, setOverview] = useState<AdminOverview | null>(null);

  useEffect(() => {
    fetchMe(apiUrl, locale)
      .then(setMe)
      .catch(() => setMe(null));
  }, [apiUrl, locale]);

  const isAuthorized = !!me && (me.role === "Moderator" || me.role === "Admin");

  useEffect(() => {
    if (!isAuthorized) return;
    getAdminOverview(apiUrl)
      .then(setOverview)
      .catch(() => setOverview(null));
  }, [apiUrl, isAuthorized, pathname]);

  if (me === undefined) return <p className="px-6 py-16 text-foreground/70">{t("loading")}</p>;
  if (!isAuthorized) return <p className="px-6 py-16 text-foreground/70">{t("noAccess")}</p>;

  const badgeFor = (key: (typeof SECTIONS)[number]["key"]): number | undefined => {
    if (!overview) return undefined;
    if (key === "venues") return overview.pendingVenues || undefined;
    if (key === "reports") return overview.pendingReports || undefined;
    if (key === "claims") return overview.pendingClaims || undefined;
    return undefined;
  };

  const activeSection = SECTIONS.find((s) => (s.href === "/admin" ? pathname === "/admin" : pathname.startsWith(s.href)));

  return (
    <AdminRoleProvider value={{ userId: me.id, role: me.role }}>
      <div className="mx-auto flex max-w-5xl flex-col gap-8 px-6 py-16 md:flex-row">
        {/* Мобилка/tablet: <select> вместо колонки — по требованию Шага 23. */}
        <select
          value={activeSection?.href ?? "/admin"}
          onChange={(e) => router.push(e.target.value)}
          className="rounded-full border border-brand-border bg-background px-4 py-2.5 text-sm font-medium text-foreground md:hidden"
        >
          {SECTIONS.map((s) => {
            const badge = badgeFor(s.key);
            return (
              <option key={s.href} value={s.href}>
                {t(`sidebar.${s.key}`)}
                {badge ? ` (${badge})` : ""}
              </option>
            );
          })}
        </select>

        <nav className="hidden w-52 shrink-0 flex-col gap-1 md:flex">
          {SECTIONS.map((s) => {
            const active = s.href === activeSection?.href;
            const badge = badgeFor(s.key);
            const Icon = s.icon;
            return (
              <Link
                key={s.href}
                href={s.href}
                className={`flex items-center justify-between gap-2 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors duration-200 ${
                  active ? "bg-brand-primary/10 text-brand-primary" : "text-foreground/70 hover:bg-brand-muted hover:text-foreground"
                }`}
              >
                <span className="flex items-center gap-2.5">
                  <Icon size={17} weight="bold" />
                  {t(`sidebar.${s.key}`)}
                </span>
                {!!badge && (
                  <span className="flex h-5 min-w-5 items-center justify-center rounded-full bg-brand-primary px-1.5 text-xs font-semibold text-brand-primary-foreground">
                    {badge}
                  </span>
                )}
              </Link>
            );
          })}
        </nav>

        <div className="min-w-0 flex-1">{children}</div>
      </div>
    </AdminRoleProvider>
  );
}
