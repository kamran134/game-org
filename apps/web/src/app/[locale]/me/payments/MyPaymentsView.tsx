"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { getMyPayments, type MyPayment } from "@/lib/paymentsApi";

export function MyPaymentsView({ apiUrl }: { apiUrl: string }) {
  const t = useTranslations("Payments");
  const locale = useLocale();

  const [payments, setPayments] = useState<MyPayment[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getMyPayments(apiUrl, locale)
      .then(setPayments)
      .catch((err) => setError(err instanceof Error ? err.message : t("loadError")));
  }, [apiUrl, locale, t]);

  return (
    <div className="flex flex-col gap-6">
      <Link href="/me" className="text-sm text-foreground/60 transition-colors duration-200 hover:text-foreground">
        ← {t("backToProfile")}
      </Link>

      <h1 className="font-heading text-2xl font-semibold text-foreground">{t("myPaymentsHeading")}</h1>

      {error && <p className="text-sm text-red-600">{error}</p>}

      {payments === null ? (
        <p className="text-foreground/70">{t("loading")}</p>
      ) : payments.length === 0 ? (
        <p className="text-foreground/70">{t("noPayments")}</p>
      ) : (
        <ul className="flex flex-col gap-2">
          {payments.map((p) => (
            <li key={p.id} className="flex items-center justify-between rounded-2xl border border-brand-border bg-background px-5 py-4">
              <div>
                {p.eventPublicId ? (
                  <Link href={`/events/${p.eventPublicId}`} className="font-medium text-foreground hover:underline">
                    {p.eventTitle ?? t("untitledEvent")}
                  </Link>
                ) : (
                  <span className="font-medium text-foreground">{p.eventTitle ?? t("untitledEvent")}</span>
                )}
                <p className="mt-0.5 text-sm text-foreground/60">{t(`statusOptions.${p.status}`)}</p>
              </div>
              <span className="text-sm font-semibold text-foreground">
                {p.amount} {p.currency}
              </span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
