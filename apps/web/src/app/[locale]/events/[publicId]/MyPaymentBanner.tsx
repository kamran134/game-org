"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { getEventAuthed } from "@/lib/eventsApi";
import type { PaymentSummary } from "@/lib/paymentsApi";

/** Дозапрашивает событие с cookie — анонимный SSR-рендер всегда отдаёт myPayment пустым. */
export function MyPaymentBanner({ apiUrl, publicId }: { apiUrl: string; publicId: string }) {
  const t = useTranslations("Payments");
  const locale = useLocale();

  const [payment, setPayment] = useState<PaymentSummary | null | undefined>(undefined);

  useEffect(() => {
    getEventAuthed(apiUrl, locale, publicId)
      .then((event) => setPayment(event?.myPayment ?? null))
      .catch(() => setPayment(null));
  }, [apiUrl, locale, publicId]);

  if (!payment || payment.status !== "Pending") return null;

  return (
    <div className="rounded-xl border border-amber-500/30 bg-amber-500/10 px-4 py-3 text-sm text-amber-700 dark:text-amber-400">
      {t("youOwe", { amount: payment.amount, currency: payment.currency })}
    </div>
  );
}
