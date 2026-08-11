// Как и остальные *Api.ts в проекте — Kiota здесь не используется, всё через fetch.

import { fetchWithRefresh } from "@/lib/fetchWithRefresh";

export type PaymentMethod = "Cash" | "BankTransfer" | "CardOnline" | "Balance";
export type PaymentStatus = "Pending" | "Paid" | "Failed" | "Refunded" | "Cancelled";

export type Payment = {
  id: string;
  payerId: string;
  payerDisplayName: string;
  amount: number;
  currency: string;
  method: PaymentMethod;
  status: PaymentStatus;
  dueAt?: string | null;
  paidAt?: string | null;
  note?: string | null;
};

export type PaymentSummary = {
  id: string;
  amount: number;
  currency: string;
  method: PaymentMethod;
  status: PaymentStatus;
  dueAt?: string | null;
};

export type MyPayment = {
  id: string;
  eventId?: string | null;
  eventPublicId?: string | null;
  eventTitle?: string | null;
  amount: number;
  currency: string;
  method: PaymentMethod;
  status: PaymentStatus;
  dueAt?: string | null;
};

export type UpdatePaymentStatusRequest = { status: PaymentStatus; method?: PaymentMethod | null; note?: string | null };

function apiBase(apiUrl: string): string {
  return apiUrl.replace(/\/api\/?$/, "");
}

async function errorMessage(res: Response, fallback: string): Promise<string> {
  const problem = await res.json().catch(() => null);
  return problem?.detail ?? fallback;
}

export async function getEventPayments(apiUrl: string, eventId: string): Promise<Payment[]> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/events/${eventId}/payments`, {
    credentials: "include",
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось загрузить платежи (${res.status})`));
  return res.json();
}

export async function updatePaymentStatus(apiUrl: string, paymentId: string, body: UpdatePaymentStatusRequest): Promise<void> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/payments/${paymentId}`, {
    method: "PATCH",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(await errorMessage(res, `Не удалось изменить статус платежа (${res.status})`));
}

export async function getMyPayments(apiUrl: string, locale: string): Promise<MyPayment[]> {
  const res = await fetchWithRefresh(apiUrl, `${apiBase(apiUrl)}/api/me/payments`, {
    credentials: "include",
    headers: { "Accept-Language": locale },
  });
  if (!res.ok) throw new Error(`Не удалось загрузить платежи (${res.status})`);
  return res.json();
}
