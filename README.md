# game.org.az

Спортивная мини-соцсеть: карточка игрока (несколько видов спорта), площадки с
гео-поиском, публичные и приватные игры, рейтинги Glicko-2, репутация надёжности,
подписки и лента.

Полный план работ, все архитектурные решения и их обоснование — в
[docs/PLAN.md](docs/PLAN.md). **Читать перед любыми изменениями структуры проекта.**
Эталонная модель данных — [docs/schema/schema.prisma](docs/schema/schema.prisma)
(референс, не исполняется — переносится на EF Core) +
[docs/schema/001_constraints.sql](docs/schema/001_constraints.sql).

## Стек

- **API**: ASP.NET Core 9, EF Core + Npgsql + NetTopologySuite, Hangfire (фон. задачи
  на Postgres, без Redis)
- **Web**: Next.js 16, TypeScript, Tailwind
- **БД**: PostgreSQL 17 + PostGIS
- Бот `game-organization-bot` — отдельный репозиторий, не трогаем; в будущем станет
  клиентом этого API через `telegram_id`.

## Структура

```
apps/api/     — ASP.NET Core (Domain / Infrastructure / Api, vertical slices)
apps/web/     — Next.js
packages/     — api-client (TS-клиент, генерируется из OpenAPI)
infra/        — docker-compose для local/dev/prod, nginx
docs/         — план, схема БД
```

## Запуск локально

Требуется: .NET 9 SDK, Node 20+, Docker (только для Postgres+PostGIS).

Docker используется исключительно под БД, не под API/web — те бегут нативно ради
hot reload. Причина, почему БД именно в Docker, а не в нативном Postgres (если он
у тебя уже стоит): PostGIS на Windows жёстко привязан к точной minor-версии Postgres
и его версия должна совпадать с прод (`postgis/postgis:17-3.5-alpine`) — иначе
гео-запросы (поиск площадок рядом) могут вести себя по-разному локально и в проде.
Порт **5434** (не 5433) — чтобы не конфликтовать с уже установленным нативным Postgres.

```bash
# 1. Поднять Postgres+PostGIS
docker compose -f infra/compose.local.yml up -d

# 2. Применить миграции
cd apps/api/src/GameOrg.Api
dotnet ef database update

# 3. Запустить API (hot reload)
dotnet watch run
# -> http://localhost:5100/health

# 4. Запустить фронт (в отдельном терминале)
cd apps/web
npm install
npm run dev
# -> http://localhost:3000
```

Секреты — через `dotnet user-secrets`, никогда в файлах. Пример конфигурации —
[.env.example](.env.example).

## Окружения

| | local | dev | prod |
|---|---|---|---|
| Домен | localhost | dev.game.org.az | game.org.az |
| Миграции | вручную | автоматически при старте | вручную, отдельным шагом |

Подробности — §5 в [docs/PLAN.md](docs/PLAN.md).
