# Собирается из корня монорепо: docker build -f apps/web/Dockerfile .
# pnpm-воркспейс: apps/web зависит от packages/api-client через workspace:*,
# поэтому контекст сборки — весь монорепозиторий, не apps/web в одиночку.
FROM node:22-alpine AS base
RUN npm install -g pnpm@11.20.0

FROM base AS deps
WORKDIR /repo
COPY pnpm-workspace.yaml package.json pnpm-lock.yaml ./
COPY apps/web/package.json apps/web/package.json
COPY packages/api-client/package.json packages/api-client/package.json
RUN pnpm install --frozen-lockfile

FROM base AS builder
WORKDIR /repo
COPY --from=deps /repo/ ./
COPY . .
# api-client должен быть собран (dist/) до next build — apps/web импортирует
# скомпилированный пакет, не исходники.
RUN pnpm --filter game-org-api-client run build
ENV NEXT_TELEMETRY_DISABLED=1
RUN pnpm --filter web run build

FROM base AS runner
WORKDIR /app
ENV NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1 \
    PORT=3000 \
    HOSTNAME=0.0.0.0

RUN addgroup -g 1001 -S nextjs && adduser -S nextjs -u 1001

COPY --from=builder /repo/apps/web/public ./apps/web/public
COPY --from=builder --chown=nextjs:nextjs /repo/apps/web/.next/standalone ./
COPY --from=builder --chown=nextjs:nextjs /repo/apps/web/.next/static ./apps/web/.next/static

USER nextjs
EXPOSE 3000

# localhost резолвится в ::1 раньше 127.0.0.1 в этом образе, а Next слушает
# только IPv4 (0.0.0.0) — с localhost healthcheck стабильно висел в "starting".
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s \
  CMD wget -qO- http://127.0.0.1:3000/ || exit 1

CMD ["node", "apps/web/server.js"]
