"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { fetchMe, type MeProfile } from "@/lib/authApi";
import { getEventPayments, updatePaymentStatus, type Payment, type PaymentMethod, type PaymentStatus } from "@/lib/paymentsApi";
import type { EventDetail } from "@/lib/eventsApi";

const STATUS_OPTIONS: PaymentStatus[] = ["Pending", "Paid", "Failed", "Refunded", "Cancelled"];
const METHOD_OPTIONS: PaymentMethod[] = ["Cash", "BankTransfer", "Balance"];

const fieldClass =
  "rounded-xl border border-brand-border bg-background px-2 py-1.5 text-sm text-foreground outline-none transition-colors duration-200 focus:border-brand-primary focus:ring-2 focus:ring-brand-primary/20 dark:bg-brand-muted";

function PaymentRow({ apiUrl, payment, onChanged }: { apiUrl: string; payment: Payment; onChanged: (p: Payment) => void }) {
  const t = useTranslations("Payments");
  const [status, setStatus] = useState(payment.status);
  const [method, setMethod] = useState<PaymentMethod>(payment.method === "CardOnline" ? "Cash" : payment.method);
  const [note, setNote] = useState(payment.note ?? "");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const dirty = status !== payment.status || method !== payment.method || note !== (payment.note ?? "");

  async function handleSave() {
    setBusy(true);
    setError(null);
    try {
      await updatePaymentStatus(apiUrl, payment.id, { status, method, note: note || null });
      onChanged({ ...payment, status, method, note: note || null });
    } catch (err) {
      setError(err instanceof Error ? err.message : t("saveError"));
    } finally {
      setBusy(false);
    }
  }

  return (
    <li className="flex flex-col gap-2 rounded-2xl border border-brand-border bg-background px-5 py-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <span className="font-medium text-foreground">{payment.payerDisplayName}</span>
        <span className="text-sm font-semibold text-foreground">
          {payment.amount} {payment.currency}
        </span>
      </div>
      <div className="flex flex-wrap items-center gap-2">
        <select value={status} onChange={(e) => setStatus(e.target.value as PaymentStatus)} className={fieldClass}>
          {STATUS_OPTIONS.map((s) => (
            <option key={s} value={s}>
              {t(`statusOptions.${s}`)}
            </option>
          ))}
        </select>
        {status === "Paid" && (
          <select value={method} onChange={(e) => setMethod(e.target.value as PaymentMethod)} className={fieldClass}>
            {METHOD_OPTIONS.map((m) => (
              <option key={m} value={m}>
                {t(`methodOptions.${m}`)}
              </option>
            ))}
          </select>
        )}
        <input
          value={note}
          onChange={(e) => setNote(e.target.value)}
          placeholder={t("notePlaceholder")}
          maxLength={300}
          className={`${fieldClass} min-w-40 flex-1`}
        />
        {dirty && (
          <button
            type="button"
            onClick={handleSave}
            disabled={busy}
            className="shrink-0 cursor-pointer rounded-full bg-brand-primary px-4 py-1.5 text-sm font-semibold text-brand-primary-foreground transition-colors duration-200 hover:bg-brand-primary/90 disabled:opacity-50"
          >
            {busy ? t("saving") : t("save")}
          </button>
        )}
      </div>
      {error && <p className="text-sm text-red-600">{error}</p>}
    </li>
  );
}

export function PaymentsSection({ apiUrl, event }: { apiUrl: string; event: EventDetail }) {
  const t = useTranslations("Payments");
  const locale = useLocale();

  const [me, setMe] = useState<MeProfile | null | undefined>(undefined);
  const [payments, setPayments] = useState<Payment[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchMe(apiUrl, locale)
      .then(setMe)
      .catch(() => setMe(null));
  }, [apiUrl, locale]);

  const canManage = !!me && (me.id === event.createdById || me.role === "Moderator" || me.role === "Admin");

  useEffect(() => {
    if (!canManage) return;
    getEventPayments(apiUrl, event.id)
      .then(setPayments)
      .catch((err) => setError(err instanceof Error ? err.message : t("loadError")));
  }, [apiUrl, event.id, canManage, t]);

  if (event.costSplit === "Free" || !canManage) return null;

  return (
    <section className="rounded-2xl border border-brand-border bg-background p-8">
      <h2 className="font-heading text-lg font-semibold text-foreground">{t("heading")}</h2>

      {error && <p className="mt-3 text-sm text-red-600">{error}</p>}

      {payments === null ? (
        <p className="mt-3 text-sm text-foreground/60">{t("loading")}</p>
      ) : payments.length === 0 ? (
        <p className="mt-3 text-sm text-foreground/60">{t("none")}</p>
      ) : (
        <ul className="mt-4 flex flex-col gap-2">
          {payments.map((p) => (
            <PaymentRow
              key={p.id}
              apiUrl={apiUrl}
              payment={p}
              onChanged={(updated) => setPayments((prev) => prev?.map((x) => (x.id === updated.id ? updated : x)) ?? null)}
            />
          ))}
        </ul>
      )}
    </section>
  );
}
