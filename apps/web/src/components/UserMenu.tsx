"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { SignOut } from "@phosphor-icons/react";
import { Link, useRouter } from "@/i18n/navigation";
import { fetchMe, logout, type MeProfile } from "@/lib/authApi";

// undefined — ещё грузим (заглушка ниже, чтобы шапка не прыгала), null — не
// вошёл, MeProfile — вошёл. Три состояния, три разных исхода, boolean/null
// тут было бы неоднозначно.
export function UserMenu({ apiUrl }: { apiUrl: string }) {
  const t = useTranslations("Nav");
  const locale = useLocale();
  const router = useRouter();
  const [profile, setProfile] = useState<MeProfile | null | undefined>(undefined);

  useEffect(() => {
    fetchMe(apiUrl, locale)
      .then(setProfile)
      .catch(() => setProfile(null));
  }, [apiUrl, locale]);

  async function handleLogout() {
    await logout(apiUrl);
    router.push("/");
    router.refresh();
  }

  if (profile === undefined) {
    return <div className="h-8 w-24" aria-hidden />;
  }

  if (!profile) {
    return (
      <Link
        href="/login"
        className="cursor-pointer rounded-full bg-brand-primary px-4 py-1.5 text-sm font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90"
      >
        {t("login")}
      </Link>
    );
  }

  return (
    <div className="flex items-center gap-3">
      <Link
        href="/me"
        className="text-sm font-medium text-foreground transition-colors duration-200 hover:text-brand-primary"
      >
        {profile.displayName}
      </Link>
      <button
        type="button"
        onClick={handleLogout}
        aria-label={t("logout")}
        title={t("logout")}
        className="flex h-6 w-6 cursor-pointer items-center justify-center text-foreground/60 transition-colors duration-200 hover:text-foreground"
      >
        <SignOut size={17} weight="bold" />
      </button>
    </div>
  );
}
