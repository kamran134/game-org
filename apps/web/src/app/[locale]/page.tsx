import { getTranslations } from "next-intl/server";
import {
  ArrowRight,
  MapPinLine,
  ShieldCheck,
  Trophy,
  UserCircle,
} from "@phosphor-icons/react/dist/ssr";
import { Link } from "@/i18n/navigation";

const FEATURES = [
  { key: "playerCard", icon: UserCircle },
  { key: "venues", icon: MapPinLine },
  { key: "events", icon: Trophy },
  { key: "rating", icon: ShieldCheck },
] as const;

const STEPS = ["login", "profile", "findGame"] as const;

export default async function Home() {
  const t = await getTranslations("Landing");

  return (
    <div className="flex flex-1 flex-col">
      {/* Hero */}
      <section className="bg-brand-background dark:bg-brand-background">
        <div className="mx-auto flex max-w-5xl flex-col items-start gap-8 px-6 py-24 sm:py-32">
          <span className="rounded-full border border-brand-accent/40 bg-brand-accent/10 px-4 py-1.5 text-sm font-medium text-brand-foreground">
            {t("badge")}
          </span>
          <h1 className="font-heading max-w-3xl text-5xl font-semibold leading-[1.05] tracking-tight text-brand-foreground sm:text-7xl">
            {t("heroTitleLine1")}
            <br />
            {t("heroTitleLine2")}
          </h1>
          <p className="max-w-xl text-lg leading-8 text-brand-foreground/80 sm:text-xl">{t("heroSubtitle")}</p>
          <div className="flex flex-col gap-4 sm:flex-row">
            <Link
              href="/login"
              className="group inline-flex h-14 items-center justify-center gap-2 rounded-full bg-brand-primary px-8 text-base font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90 cursor-pointer"
            >
              {t("ctaLogin")}
              <ArrowRight
                size={20}
                weight="bold"
                className="transition-transform duration-200 group-hover:translate-x-1"
              />
            </Link>
            <Link
              href="/sports"
              className="inline-flex h-14 items-center justify-center rounded-full border border-brand-border px-8 text-base font-semibold text-brand-foreground transition-colors duration-200 hover:bg-brand-muted cursor-pointer"
            >
              {t("ctaSports")}
            </Link>
          </div>
        </div>
      </section>

      {/* Features */}
      <section className="bg-background">
        <div className="mx-auto max-w-5xl px-6 py-24">
          <h2 className="font-heading mb-12 text-3xl font-semibold tracking-tight text-foreground sm:text-4xl">
            {t("featuresHeading")}
          </h2>
          <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
            {FEATURES.map(({ key, icon: Icon }) => (
              <div
                key={key}
                className="group rounded-2xl border border-black/10 p-8 transition-colors duration-200 hover:border-brand-primary/40 hover:bg-brand-background dark:border-white/10 dark:hover:bg-brand-background/10"
              >
                <div className="mb-5 flex h-12 w-12 items-center justify-center rounded-xl bg-brand-primary/10 text-brand-primary">
                  <Icon size={26} weight="bold" />
                </div>
                <h3 className="mb-2 text-xl font-semibold text-foreground">{t(`features.${key}.title`)}</h3>
                <p className="text-base leading-7 text-foreground/70">{t(`features.${key}.description`)}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* How it works */}
      <section className="bg-brand-muted/40 dark:bg-brand-background/20">
        <div className="mx-auto max-w-5xl px-6 py-24">
          <h2 className="font-heading mb-12 text-3xl font-semibold tracking-tight text-brand-foreground sm:text-4xl">
            {t("howItWorksHeading")}
          </h2>
          <div className="grid grid-cols-1 gap-10 sm:grid-cols-3">
            {STEPS.map((key, index) => (
              <div key={key}>
                <div className="font-heading mb-4 text-5xl font-bold text-brand-accent/70">{index + 1}</div>
                <h3 className="mb-2 text-lg font-semibold text-brand-foreground">{t(`steps.${key}.title`)}</h3>
                <p className="text-base leading-7 text-brand-foreground/70">{t(`steps.${key}.description`)}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Footer CTA */}
      <section className="bg-brand-primary">
        <div className="mx-auto flex max-w-5xl flex-col items-start gap-6 px-6 py-20 sm:flex-row sm:items-center sm:justify-between">
          <h2 className="font-heading max-w-md text-3xl font-semibold leading-tight text-brand-primary-foreground sm:text-4xl">
            {t("footerCtaTitle")}
          </h2>
          <Link
            href="/login"
            className="inline-flex h-14 shrink-0 items-center justify-center gap-2 rounded-full bg-white px-8 text-base font-semibold text-brand-primary transition-colors duration-200 hover:bg-white/90 cursor-pointer"
          >
            {t("ctaLogin")}
            <ArrowRight size={20} weight="bold" />
          </Link>
        </div>
      </section>
    </div>
  );
}
