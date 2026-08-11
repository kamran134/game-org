import { getTranslations, setRequestLocale } from "next-intl/server";
import { Users, Lock } from "@phosphor-icons/react/dist/ssr";
import { Link } from "@/i18n/navigation";
import { getClubs } from "@/lib/clubsApi";

export const dynamic = "force-dynamic";

// Тонкая обёртка над каталогом клубов, закреплённая на kind=Group (Шаг 20)
// — сама сущность и все связанные операции (роли, заявки, инвайты) общие с
// /clubs, разница только в фильтре списка и лейблах.
export default async function GroupsPage({
  params,
  searchParams,
}: {
  params: Promise<{ locale: string }>;
  searchParams: Promise<{ cityId?: string; sportId?: string }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Clubs");
  const sp = await searchParams;

  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";

  const groups = await getClubs(apiUrl, locale, { cityId: sp.cityId, sportId: sp.sportId, kind: "Group" }).catch(() => []);

  return (
    <main className="flex-1 bg-brand-background px-6 py-16">
      <div className="mx-auto max-w-3xl">
        <div className="mb-6 flex flex-wrap items-center justify-between gap-4">
          <h1 className="font-heading text-2xl font-semibold text-foreground">{t("pageTitleGroups")}</h1>
          <Link
            href="/groups/new"
            className="inline-flex items-center rounded-full bg-brand-primary px-5 py-2.5 text-sm font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90 cursor-pointer"
          >
            {t("addGroup")}
          </Link>
        </div>

        {groups.length === 0 ? (
          <p className="text-foreground/70">{t("emptyGroups")}</p>
        ) : (
          <ul className="flex flex-col gap-3">
            {groups.map((c) => (
              <li key={c.id}>
                <Link
                  href={`/clubs/${c.slug}`}
                  className="flex items-center justify-between gap-4 rounded-2xl border border-brand-border bg-background px-5 py-4 transition-colors duration-200 hover:border-brand-primary/40"
                >
                  <div className="flex items-center gap-2">
                    {c.visibility === "Private" && <Lock size={16} weight="bold" className="text-foreground/40" />}
                    <div>
                      <p className="font-medium text-foreground">{c.name}</p>
                      {c.city && <p className="mt-1 text-sm text-foreground/60">{c.city.nameI18n[locale] ?? c.city.slug}</p>}
                    </div>
                  </div>
                  <span className="flex shrink-0 items-center gap-1 text-sm text-foreground/50">
                    <Users size={14} weight="bold" />
                    {c.membersCount}
                  </span>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </div>
    </main>
  );
}
