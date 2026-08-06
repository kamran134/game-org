import { AnonymousAuthenticationProvider } from "@microsoft/kiota-abstractions";
import { FetchRequestAdapter } from "@microsoft/kiota-http-fetchlibrary";
import { createGameOrgApiClient } from "game-org-api-client";

/**
 * Серверный клиент: вызывается только из Server Components/route handlers,
 * поэтому URL берётся из обычной (не NEXT_PUBLIC_) переменной, если она задана,
 * с фоллбэком на NEXT_PUBLIC_API_URL для локальной разработки.
 */
export function createApiClient() {
  const baseUrl =
    process.env.API_URL ?? process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";

  const authProvider = new AnonymousAuthenticationProvider();
  const adapter = new FetchRequestAdapter(authProvider);
  adapter.baseUrl = baseUrl;

  return createGameOrgApiClient(adapter);
}
