# game.org.az — план реализации (handoff для Sonnet)

> Этот документ — единственный источник правды по решениям.
> **Решения ниже уже приняты и обсуждены. Не пересматривать, не предлагать альтернативы.**
> Если что-то в плане противоречит реальности — сообщить пользователю, а не решать самому.

---

## 0. Контекст за одну минуту

Строим спортивную мини-соцсеть для Азербайджана: карточка игрока с несколькими видами
спорта, каталог площадок с гео-поиском, публичные и приватные игры, рейтинги Glicko-2,
репутация надёжности, подписки и лента.

Существующий Telegram-бот (`game-organization-bot`, TypeScript/Telegraf) **не трогаем**.
В будущем он станет клиентом этого API. Мост — `telegram_id`.

**Принятые архитектурные решения (обсуждены, закрыты):**

| Решение | Выбор |
|---|---|
| Архитектура | Модульный монолит, API-first (клиенты: web, bot, будущий mobile) |
| Backend | **C# / ASP.NET Core 9** — меньше поверхность зависимостей, батарейки в коробке, EF Core сильнее на агрегациях |
| Frontend | **Next.js 15 + TypeScript** — SEO критичен для роста. **Blazor рассматривался и отвергнут** |
| ORM | EF Core 9 + Npgsql + NetTopologySuite |
| Фон. задачи | Hangfire на Postgres-хранилище — **Redis на старте НЕ нужен** |
| БД | PostgreSQL 17 + PostGIS |
| i18n | ru / az / en с первого дня |

---

## 1. Что сделать до всего остального

Пользователь передаст два файла (они уже у него на руках):
- `schema.prisma` — эталонная модель данных, 34 сущности
- `001_constraints.sql` — CHECK-констрейнты, partial-индексы, PostGIS

**Положить их в новый репозиторий как `docs/schema/schema.prisma` и
`docs/schema/001_constraints.sql`.** Это референс для переноса на EF Core.
`schema.prisma` в проекте не исполняется — он только описание модели.

---

## 2. Расположение и именование

| Что | Значение |
|---|---|
| Локальный путь | `C:\Users\kazim\Work\hobby\game-org` |
| GitHub | `kamran134/game-org` |
| Docker-образы | `ghcr.io/kamran134/game-org/api`, `ghcr.io/kamran134/game-org/web` |
| Путь на сервере (dev) | `/opt/game-org` |
| C# namespace | `GameOrg.*` |

---

## 3. Структура репозитория

```
game-org/
├── GameOrg.sln
├── apps/
│   ├── api/
│   │   ├── src/
│   │   │   ├── GameOrg.Domain/          # сущности, enum'ы, доменная логика. Без зависимостей.
│   │   │   ├── GameOrg.Infrastructure/  # DbContext, EF-конфигурации, миграции, внешние сервисы
│   │   │   └── GameOrg.Api/             # host, эндпоинты, DTO, валидаторы (vertical slices)
│   │   └── tests/
│   │       ├── GameOrg.UnitTests/
│   │       └── GameOrg.IntegrationTests/
│   └── web/                             # Next.js 15
├── packages/
│   └── api-client/                      # TS-клиент, генерируется из OpenAPI. НЕ РЕДАКТИРОВАТЬ РУКАМИ.
├── infra/
│   ├── compose.local.yml
│   ├── compose.dev.yml
│   ├── compose.prod.yml
│   └── nginx/dev.game.org.az.conf
├── docs/
│   └── schema/                          # schema.prisma + 001_constraints.sql
├── .github/workflows/deploy-dev.yml
├── .env.example
└── README.md
```

**Три C#-проекта, не четыре.** Слоя Application нет намеренно — логика живёт в
vertical slices внутри `GameOrg.Api/Features/`. У пользователя в CLAUDE.md записано:
«без лишних абстракций, три похожих строки лучше преждевременной обёртки». Соблюдать.

Внутри `GameOrg.Api/Features/` — папка на модуль: `Identity`, `Profiles`, `Sports`,
`Venues`, `Clubs`, `Events`, `Payments`, `Social`, `Reputation`, `Notifications`.
В каждой: эндпоинты, DTO, валидаторы, обработчики этого модуля.

---

## 4. Версии и пакеты

**.NET 9 SDK.**

`GameOrg.Api`:
```
Microsoft.AspNetCore.OpenApi              9.*
FluentValidation.AspNetCore               11.*
Hangfire.AspNetCore                       1.8.*
Hangfire.PostgreSql                       1.20.*
Serilog.AspNetCore                        8.*
Microsoft.AspNetCore.Authentication.JwtBearer 9.*
```

`GameOrg.Infrastructure`:
```
Microsoft.EntityFrameworkCore                                9.*
Microsoft.EntityFrameworkCore.Design                         9.*
Npgsql.EntityFrameworkCore.PostgreSQL                        9.*
Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite       9.*
EFCore.NamingConventions                                     9.*
```

`apps/web`: Next.js 15, React 19, TypeScript 5, Tailwind, shadcn/ui, next-intl.

**Не добавлять:** Redis, MediatR, AutoMapper, Elasticsearch, микросервисы.
Если кажется, что нужны — сначала спросить пользователя.

---

## 5. Три окружения

| | local | dev | prod (будущее) |
|---|---|---|---|
| Где | машина разработчика | сервер `kamran` (37.27.45.67) | новый сервер |
| Домен | `localhost` | `dev.game.org.az` | `game.org.az` |
| API-порт (loopback) | 5100 | **3100** | 3100 |
| Web-порт (loopback) | 3000 | **3101** | 3101 |
| Postgres | контейнер, порт 5433 наружу | контейнер, наружу **не публиковать** | то же |
| БД / юзер | `gameorg_local` / `gameorg` | `gameorg_dev` / `gameorg` | `gameorg` / `gameorg` |
| Миграции | `dotnet ef database update` вручную | автоматически при старте API | **только вручную**, отдельным шагом |
| `ASPNETCORE_ENVIRONMENT` | `Development` | `Development` | `Production` |

Порты 3000/3001/3002/8080 на сервере **заняты** другими проектами — не использовать.

**Локально** API и web запускаются нативно (`dotnet watch`, `next dev`) ради hot reload,
в Docker поднимается только Postgres. `compose.local.yml` содержит только БД.

**Конфигурация:** всё через переменные окружения. В C# — `appsettings.json` +
`appsettings.Development.json` + env-переменные (env перекрывают). Секреты локально —
через `dotnet user-secrets`, никогда в файлах.
`.env.local` / `.env.dev` / `.env.prod` — **в `.gitignore`**, в репозитории только
`.env.example`. На сервере `.env` генерируется из GitHub Secrets в CI.

---

## 6. Перенос схемы на EF Core

Источник — `docs/schema/schema.prisma`. Перенос механический, но с тремя
**сознательными отличиями** от Prisma-версии:

### 6.1. Enum'ы → `varchar` со строковой конверсией (было: нативные PG enum)

```csharp
builder.Property(e => e.Status)
       .HasConversion<string>()
       .HasMaxLength(32);
```

Причина: в этом домене enum'ы будут активно расти (`NotificationType`, `ActivityVerb`,
статусы). С нативными PG-enum каждое добавление значения — миграция с `ALTER TYPE`.
Со строками — бесплатно. CHECK-констрейнты из `001_constraints.sql` работают одинаково
с обоими вариантами, целостность в критичных местах сохраняется.

### 6.2. Гео: одна колонка `Point` вместо `lat`/`lng` + триггер

В Prisma-схеме было три поля (`lat`, `lng`, `geom`) плюс триггер синхронизации.
С NetTopologySuite это не нужно:

```csharp
// Domain
public Point Location { get; set; } = null!;   // NetTopologySuite.Geometries

// Configuration
builder.Property(v => v.Location)
       .HasColumnType("geography (Point, 4326)")
       .IsRequired();
```

Создание: `new Point(longitude, latitude) { SRID = 4326 }` — **долгота первая**.
Чтение: `v.Location.Y` — широта, `v.Location.X` — долгота.
Радиусный поиск становится типизированным LINQ, без сырого SQL:

```csharp
var origin = new Point(lng, lat) { SRID = 4326 };
var nearby = await db.Venues
    .Where(v => v.Location.IsWithinDistance(origin, radiusMeters))
    .OrderBy(v => v.Location.Distance(origin))
    .ToListAsync();
```

Из `001_constraints.sql` **пропустить** блок 1 (триггер `venues_sync_geom` и
`ALTER COLUMN geom`) — он больше не нужен. GIST-индекс создать через EF:
`builder.HasIndex(v => v.Location).HasMethod("gist");`

### 6.3. Всё остальное — 1:1

Имена таблиц и колонок из `@@map`/`@map` в Prisma — snake_case. Обеспечивается
пакетом `EFCore.NamingConventions`, руками не прописывать:

```csharp
options.UseNpgsql(cs, o => o.UseNetTopologySuite())
       .UseSnakeCaseNamingConvention();
```

### 6.4. Обязательные конвенции DbContext

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.HasPostgresExtension("citext");
    modelBuilder.HasPostgresExtension("postgis");
    modelBuilder.HasPostgresExtension("pg_trgm");

    modelBuilder.ApplyConfigurationsFromAssembly(typeof(GameOrgDbContext).Assembly);

    // Soft delete: глобальный фильтр на каждой сущности с DeletedAt
    // (применять в конфигурации каждой такой сущности:
    //  builder.HasQueryFilter(e => e.DeletedAt == null); )
}

protected override void ConfigureConventions(ModelConfigurationBuilder b)
{
    // Все DateTime — UTC. Защита от случайного local-time.
    b.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    b.Properties<decimal>().HavePrecision(10, 2);
}
```

- **ID:** `Guid`, генерация в приложении через `Guid.CreateVersion7()` (.NET 9) — не в БД.
  Time-ordered, важно для локальности B-tree индексов.
- **citext-поля** (`handle`, `slug`, `email`): `.HasColumnType("citext")`.
- **Timestamps:** `DateTime` с `Kind=Utc` → Npgsql пишет `timestamptz` автоматически.

### 6.5. Порядок миграций

1. `dotnet ef migrations add InitialSchema` — все таблицы, FK, обычные индексы.
2. `dotnet ef migrations add Constraints` — **пустая**, вручную вписать в `Up()`
   содержимое `001_constraints.sql` через `migrationBuilder.Sql(...)`, **пропустив блок 1**
   (см. 6.2) и блок 12 (партиционирование — это план на будущее, не выполнять).
3. `dotnet ef migrations add SeedReference` — справочники (см. §7).

**Правило прода из CLAUDE.md пользователя:** новые колонки всегда nullable или с default.

---

## 7. Seed-данные

Идемпотентный сидер, безопасно запускать повторно. Выполняется при старте в
Development, на проде — отдельной командой.

- **Sports:** футбол, мини-футбол, волейбол, баскетбол, теннис, настольный теннис,
  бадминтон. С `nameI18n` на ru/az/en и эмодзи.
- **SportPositions:** для футбола (GK, CB, LB, RB, CDM, CM, CAM, LW, RW, ST),
  волейбола (SETTER, OUTSIDE, MIDDLE, OPPOSITE, LIBERO), баскетбола (PG, SG, SF, PF, C).
- **Cities:** Баку, Гянджа, Сумгайыт, Мингечевир, Ширван, Нахчыван, Шеки, Евлах,
  Ленкорань, Мингячевир. `countryCode = "AZ"`, `timezone = "Asia/Baku"`.
- **Achievements:** FIRST_GAME, TEN_GAMES, FIFTY_GAMES, IRON_MAN (4 недели подряд),
  MVP_FIRST, RELIABLE (20 игр без пропусков), MULTI_SPORT (2+ вида спорта).

---

## 8. Пошаговый план

Каждый шаг заканчивается коммитом и проверкой. Не переходить дальше, пока проверка не прошла.

### Шаг 1 — Скелет и локальный запуск

- `git init`, `.gitignore` (dotnet + node + `.env*`), README
- Solution + три C#-проекта + два тестовых
- `apps/web` — `create-next-app` (TypeScript, Tailwind, App Router)
- `infra/compose.local.yml` — только Postgres:
  ```yaml
  image: postgis/postgis:17-3.5-alpine
  ports: ["5433:5432"]
  ```
- Эндпоинт `GET /health` → `{ status, version, env }`
- Serilog в консоль (JSON на dev/prod, читаемый текст локально)

**Проверка:** `docker compose -f infra/compose.local.yml up -d`, `dotnet run` →
`curl localhost:5100/health` отвечает 200; `npm run dev` → страница открывается.

### Шаг 2 — Схема БД

- Перенести все 34 сущности из `schema.prisma` в `GameOrg.Domain/Entities/`
- Конфигурации в `GameOrg.Infrastructure/Configurations/` — по файлу на сущность,
  через `IEntityTypeConfiguration<T>`
- Три миграции по порядку из §6.5
- Сидер из §7

**Проверка:** `dotnet ef database update` проходит на чистой БД; в `psql`
`\dt` показывает все таблицы в snake_case; `SELECT * FROM sports;` возвращает засеянное;
CHECK-констрейнты на месте (`\d+ events`).

### Шаг 3 — Первые эндпоинты и генерация клиента

- `GET /api/sports`, `GET /api/cities`, `GET /api/venues?cityId=&sportId=&near=lat,lng&radius=`
- OpenAPI-документ на `/openapi/v1.json` (встроен в .NET 9)
- Rate limiting: встроенный `AddRateLimiter`, 60 req/min на IP (как в текущем боте)
- CORS: только известные origin'ы из конфигурации
- Генерация TS-клиента в `packages/api-client` через **Kiota**, скриптом `npm run gen:api`
- Next.js: страница со списком видов спорта через сгенерированный клиент

**Проверка:** `/openapi/v1.json` валиден; генерация клиента проходит; страница
показывает данные из API; гео-фильтр возвращает корректные площадки.

### Шаг 4 — Деплой на dev.game.org.az ⚠️ ключевая веха

Деплой делается **рано и на тонком срезе** — чтобы инфраструктурные проблемы
всплыли до того, как накопятся фичи.

**4.1. DNS (делает пользователь).** В Cloudflare: A-запись `dev` → `37.27.45.67`,
proxy включён (оранжевое облако).

**4.2. TLS — сертификат уже есть.** Существующий Cloudflare Origin Certificate —
wildcard `*.game.org.az`, действует до 2041 года. **Новый выпускать не нужно**,
переиспользовать файлы:
```
/etc/ssl/cloudflare/webapp.game.org.az.pem
/etc/ssl/cloudflare/webapp.game.org.az.key
```

**4.3. nginx** — `infra/nginx/dev.game.org.az.conf`, положить в
`/etc/nginx/sites-available/` и слинковать. Паттерн взят из существующих vhost'ов:

```nginx
server {
    listen 80;
    server_name dev.game.org.az;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl;
    server_name dev.game.org.az;

    ssl_certificate     /etc/ssl/cloudflare/webapp.game.org.az.pem;
    ssl_certificate_key /etc/ssl/cloudflare/webapp.game.org.az.key;
    ssl_protocols       TLSv1.2 TLSv1.3;

    client_max_body_size 10M;

    location /api/ {
        proxy_pass         http://127.0.0.1:3100;
        proxy_http_version 1.1;
        proxy_set_header   Host $host;
        proxy_set_header   X-Real-IP $remote_addr;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }

    location / {
        proxy_pass         http://127.0.0.1:3101;
        proxy_http_version 1.1;
        proxy_set_header   Host $host;
        proxy_set_header   X-Real-IP $remote_addr;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection "upgrade";
    }
}
```
Перед перезагрузкой обязательно `nginx -t`. На сервере крутятся четыре чужих
проекта — сломанный конфиг положит их все.

**4.4. Dockerfile'ы.** API — multi-stage на `mcr.microsoft.com/dotnet/sdk:9.0` →
`aspnet:9.0-alpine`. Web — multi-stage Node 22 alpine, `output: 'standalone'` в
`next.config.js`. **Оба — с непривилегированным пользователем** (как в Dockerfile бота).
Native AOT не использовать — EF Core его полноценно не поддерживает.

**4.5. `compose.dev.yml`.** Три сервиса: `api`, `web`, `db`. Порты **только на
loopback**: `"127.0.0.1:3100:8080"`, `"127.0.0.1:3101:3000"`. БД наружу не публиковать.
Обязательно на каждом сервисе — как в текущем compose пользователя:
```yaml
restart: unless-stopped
logging:
  driver: "json-file"
  options: { max-size: "10m", max-file: "3" }
```
Плюс **лимиты памяти** (`mem_limit`) — на сервере живут четыре чужих проекта,
новый не должен их выдавить: api 512M, web 768M, db 1G.

**4.6. CI/CD** — `.github/workflows/deploy-dev.yml`, скопировать структуру из
`deploy.yml` бота (тот же паттерн: buildx → ghcr → `appleboy/scp-action` →
`appleboy/ssh-action`). Отличия: собираются **два** образа, ветка-триггер `develop`,
`DEPLOY_PATH=/opt/game-org`.

GitHub Secrets (переиспользуются существующие `SSH_*`): `SSH_HOST`, `SSH_USERNAME`,
`SSH_PRIVATE_KEY`, `SSH_PORT`, `DEPLOY_PATH_GAMEORG`, `DB_PASSWORD_GAMEORG`,
`JWT_SIGNING_KEY`, `TELEGRAM_BOT_TOKEN`.

**Проверка:** `https://dev.game.org.az/api/health` отвечает 200 по HTTPS;
`https://dev.game.org.az` показывает страницу; четыре старых проекта работают;
`free -h` показывает разумный расход.

### Шаг 5 — Auth

- Валидация Telegram Login Widget (HMAC-SHA256 от `bot_token`, проверка `auth_date`)
- Регистрация/связывание через `Account(provider=TELEGRAM, providerUserId=telegram_id)`
- JWT access (15 мин) + refresh (30 дней) в **httpOnly Secure SameSite=Lax cookie**
- `Session` с хранением **sha256-хеша** refresh-токена, не самого токена
- Эндпоинты: `POST /api/auth/telegram`, `POST /api/auth/refresh`, `POST /api/auth/logout`, `GET /api/me`

### Шаг 6 — Профиль игрока

`GET/PATCH /api/me`, управление `UserSport` (уровень, позиции, видимость),
публичный профиль `GET /api/users/{handle}`, страницы в Next.js с SSR и корректными
OG-тегами.

### Шаг 7 — Площадки

CRUD, гео-поиск, фото через Cloudflare R2 (аккаунт и бакет `kamran-backups` уже
существуют — для медиа завести **отдельный** бакет), отзывы, страницы с SSR и
schema.org разметкой для SEO.

### Шаг 8 — События

Создание, публичные/приватные, запись, гости, лист ожидания, дедлайн записи,
отмена. Hangfire для напоминаний.

---

## 9. Конвенции

**C#:** nullable reference types включены; `sealed` по умолчанию; async/await везде,
`.Result`/`.Wait()` запрещены; CancellationToken пробрасывается во все async-методы;
DTO — `record`; валидация через FluentValidation на входе каждого эндпоинта.

**Комментарии:** только там, где неочевиден *why*. Из CLAUDE.md пользователя.

**Тексты для пользователя:** только через i18n-ресурсы, ru/az/en. Никаких строк в коде.

**Время:** UTC в БД и API, конвертация в таймзону — на клиенте.

**Деньги:** `decimal(10,2)` + код валюты. `double` для денег — запрещён.

**Тесты:** интеграционные на Testcontainers с реальным PostGIS (не in-memory —
гео-запросы и констрейнты на нём не проверить). Юнит-тесты на расчёты:
Glicko-2, надёжность, деление стоимости, продвижение из листа ожидания.

**Git:** ветки `main` (prod) и `develop` (dev-сервер). Коммиты осмысленные,
не «wip». Секреты не коммитить — проверять `git diff` перед коммитом.

---

## 10. Что НЕ делать

- Не поднимать Redis — Hangfire работает на Postgres
- Не заводить Kafka, микросервисы, GraphQL, MediatR, AutoMapper
- Не делать fan-out on write для ленты — на старте выборка по подпискам
- Не применять партиционирование (блок 12 в SQL) — это план на будущее
- Не трогать репозиторий `game-organization-bot` и его контейнеры на сервере
- Не публиковать порты новых контейнеров наружу — только на `127.0.0.1`
- Не менять существующие nginx-конфиги, только добавить новый
- Не переписывать бота на C#
- Не коммитить `.env*`

---

## 11. Открытые вопросы к пользователю

Спросить, когда дойдёт до соответствующего шага, не решать самостоятельно:

1. **Шаг 4:** создана ли A-запись `dev.game.org.az` в Cloudflare.
2. **Шаг 7:** создать отдельный R2-бакет под медиа (`game-org-media`) — нужны новый
   токен и публичный домен для раздачи.
3. **Шаг 5:** какой Telegram-бот использовать для логина на dev — существующий
   или завести отдельного тестового.

---

## 12. Незакрытое по инфраструктуре (не блокирует, но помнить)

На сервере `kamran` открыт вопрос: обнаружено 13.3 ТБ исходящего трафика с Docker-сети
`kamrankazimi_network` при том, что через nginx проходит 0.14 ГБ за две недели.
Пользователь проверяет график в консоли Hetzner. **Если подтвердится — контейнер
`kamrankazimi-next` считается скомпрометированным**, и разворачивать dev на этой же
машине нужно с осторожностью (изолированная docker-сеть, лимиты памяти — уже в плане).
Самостоятельно этот вопрос не расследовать, диагностика уже проведена.

Swap (4 ГБ) и бэкапы Postgres (cron 03:17 → локально + Cloudflare R2) на сервере
уже настроены. **Добавить новую БД `gameorg_dev` в `/root/pg_backup.sh`** — там массив
`TARGETS`, дописать строку с новым контейнером.
