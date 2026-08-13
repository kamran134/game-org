import type { ReactNode } from "react";
import { AdminShell } from "./AdminShell";

export const dynamic = "force-dynamic";

export default function AdminLayout({ children }: { children: ReactNode }) {
  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";
  return <AdminShell apiUrl={apiUrl}>{children}</AdminShell>;
}
