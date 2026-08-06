import path from "node:path";
import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Standalone-сборка для Docker (docs/PLAN.md §4.4). outputFileTracingRoot
  // указывает на корень pnpm-монорепо — иначе трассировка файлов не увидит
  // packages/api-client, подключённый через workspace:*.
  output: "standalone",
  outputFileTracingRoot: path.join(__dirname, "../.."),
};

export default nextConfig;
