import { setRequestLocale, getTranslations } from "next-intl/server";
import { UsersSection } from "./UsersSection";

export const dynamic = "force-dynamic";

export default async function AdminUsersPage({
  params,
  searchParams,
}: {
  params: Promise<{ locale: string }>;
  searchParams: Promise<{ query?: string; role?: string; banned?: string }>;
}) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("Admin");
  const sp = await searchParams;
  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";

  return (
    <div className="flex flex-col gap-6">
      <h1 className="font-heading text-2xl font-semibold text-foreground">{t("sidebar.users")}</h1>
      <UsersSection apiUrl={apiUrl} query={sp.query} role={sp.role} banned={sp.banned} search={sp} />
    </div>
  );
}
