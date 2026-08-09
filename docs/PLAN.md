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
| Frontend | **Next.js 16 + TypeScript** — SEO критичен для роста. **Blazor рассматривался и отвергнут** |
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
│   └── web/                             # Next.js 16
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

`apps/web`: Next.js 16, React 19, TypeScript 5, Tailwind, shadcn/ui, next-intl.

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
| Postgres | контейнер, порт **5434** наружу (5433 занят нативным Postgres на машине разработчика) | контейнер, наружу **не публиковать** | то же |
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
  ports: ["5434:5432"]  # 5433 занят нативным Postgres 15 на машине разработчика
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

**Статус: развёрнуто и работает, 2026-08-07.**
`https://dev.game.org.az` (веб) и `https://dev.game.org.az/api/*` (API) отвечают
публично по HTTPS, сквозной путь проверен вручную через curl. Первый деплой был
сделан вручную через SSH, но теперь **CI/CD настроен и рабочий**:
`.github/workflows/deploy-dev.yml` собирает образы, пушит в GHCR и деплоит на
сервер при пуше — автодеплой подтверждён на практике. `gameorg-dev` добавлена в
`/root/pg_backup.sh` и проверена. Найденный и исправленный по пути баг:
health-эндпоинт API живёт на `/health` без префикса `/api`, добавлен отдельный
`location = /api/health` в nginx с переписыванием пути.

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

### Шаг 7.5 — Мультиязычный пользовательский контент ⚠️ делать до Шага 8

Сейчас мультиязычны только справочники (`Sport`, `City`, `SportPosition`,
`Achievement` — поле `NameI18n` jsonb). Всё, что вводит пользователь
(название/описание/адрес площадки, имя и bio профиля), — одна строка на одном
языке. Нужно дать заполнять на трёх языках и показывать с фолбэком, чтобы
страница не выглядела пустой.

**Решения приняты пользователем (не пересматривать):**

| Вопрос | Решение |
|---|---|
| Поля | Площадка: `Name`, `Description`, `Address`. Профиль: `DisplayName`, `Bio` |
| Клуб / Событие | Механизм строим сейчас, применяем когда дойдём до сущностей |
| Отзывы (`VenueReview.Text`) | **НЕ** мультиязычные — личное мнение, пишется один раз |
| Порядок фолбэка | запрошенный → `az` → `en` → `ru` → первый непустой |
| Пометка о подмене языка | **Не показывать.** Просто текст, без бейджа |
| Валидация при создании | Хотя бы один любой язык непустой |

#### Контракт

Единственное место, где решается «какой текст показать», — **бэкенд**.
Клиенты (web, будущий бот, мобилка) не реализуют фолбэк каждый у себя.

- Язык запроса — заголовок **`Accept-Language`**, первый поддерживаемый из
  `az/ru/en`, иначе `az`. Ответы отдавать с `Vary: Accept-Language`.
- **Display-DTO отдают уже готовую строку** и **не меняют форму**
  (`displayName: string`, `bio: string?`, `name: string`, `address: string?`).
  Это важно: значит `packages/api-client` (Kiota) **перегенерировать не нужно** —
  а он в текущей среде и не генерируется (нужен живой `/openapi/v1.json`).
- **Edit-DTO дополнительно** несут сырые словари (`nameI18n`, `bioI18n`, …),
  чтобы форма редактирования показала все три языка. Эти DTO ходят через
  рукописный fetch (`authApi.ts`, `venuesApi.ts`), Kiota их не касается.

#### Хранение

Как у справочников: `Dictionary<string, string>` → колонка `jsonb`, конвертер
`JsonConversions.For<Dictionary<string,string>>()` (см. `CityConfiguration`).
Старые скалярные колонки **удаляются**, не дублируются — один источник правды.

| Сущность | Было | Стало |
|---|---|---|
| `Venue` | `Name`, `Description`, `Address` | `NameI18n`, `DescriptionI18n`, `AddressI18n` |
| `User` | `DisplayName`, `Bio` | `DisplayNameI18n`, `BioI18n` |

В DTO словарь — **типизированная запись**, а не `Dictionary`:
`record LocalizedTextDto(string? Az, string? Ru, string? En)`. Причина: Kiota на
`Dictionary` выдаёт `additionalData`-мешок, и на фронте получается
`x.nameI18n?.additionalData?.[locale]` (так сейчас читаются города). Хранилище
при этом остаётся словарём — добавить четвёртый язык можно без миграции.

**Поиск.** В `001_constraints.sql` есть `venues_search_trgm` — GIN-индекс по
`(name, address)`. Колонок не станет, поэтому вместо него:

```sql
ALTER TABLE venues ADD COLUMN search_text text GENERATED ALWAYS AS (
  coalesce(name_i18n->>'az','')    || ' ' || coalesce(name_i18n->>'ru','')    || ' ' ||
  coalesce(name_i18n->>'en','')    || ' ' || coalesce(address_i18n->>'az','') || ' ' ||
  coalesce(address_i18n->>'ru','') || ' ' || coalesce(address_i18n->>'en','')
) STORED;
CREATE INDEX venues_search_trgm ON venues USING GIN (search_text gin_trgm_ops);
```

В EF — computed-колонка (`ValueGeneratedOnAddOrUpdate`, никогда не пишем).
Самого поиска по имени в `GET /api/venues` пока нет — индекс готовим на будущее,
эндпоинт не трогаем.

Тот же трюк — и с `users_search_trgm` (обнаружился по ходу работы, в исходном
плане не был явно упомянут): индекс висел на `(display_name, handle)`,
`display_name` тоже уходит в jsonb. `users.search_text` — склейка
`display_name_i18n` по az/ru/en + `handle`.

#### Побочный фикс: транслитерация слагов

`VenueSlugGenerator` выбрасывает всё, кроме `[a-z0-9]`, поэтому реальная
площадка на dev получила слаг `132-134-n-li-m-kt-b-0c9f` из «132-134 N-li
məktəb» — азербайджанские буквы просто выпали. `HandleGenerator` страдает тем же.

Завести общий `GameOrg.Api/Common/Transliterator.cs` (az: `ə→e ğ→g ı→i İ→i ö→o
ş→s ü→u ç→c`, ru: кириллица→латиница) и использовать в обоих генераторах.
Тогда выйдет `132-134-n-li-mekteb-0c9f`.

Слаг генерируется из **первого непустого имени по порядку фолбэка** и, как и
раньше, **не меняется при редактировании** — ломать URL нельзя.

#### Миграция (одна, `MultilingualUserContent`)

Прода нет, на dev одна площадка и один пользователь — делаем чисто, без
переходного периода с двумя колонками:

1. Добавить `*_i18n` jsonb (nullable).
2. Бэкфилл: `name_i18n = jsonb_build_object('az', name)` и так же для остальных.
   Ключ `az` выбран как дефолт сайта; на dev это ещё и фактически верно.
3. Удалить старые скалярные колонки.
4. Добавить `search_text` + пересоздать `venues_search_trgm` и `users_search_trgm`.
5. Отдельным шагом (не в миграции, а разово по факту деплоя) — перегенерировать
   слаг существующей площадки новым транслитератором.

#### Frontend

Общий компонент `apps/web/src/components/I18nField.tsx`: подпись, три вкладки
`AZ | RU | EN` с точкой-индикатором «заполнено», под ними один `input`/`textarea`
активной вкладки. Пропсы: `label`, `value: LocalizedText`, `onChange`,
`multiline`, `maxLength`. Стилизовать по бренд-системе (`bg-background`,
`border-brand-border`, активная вкладка — `text-brand-primary`).

Используется в `VenueForm.tsx` (name, description, address) и `MeEditor.tsx`
(displayName, bio). Отображение (`/venues`, `/venues/[slug]`, `/[handle]`)
**не меняется** — бэкенд уже отдал готовую строку.

Каждый вызов API из web должен нести `Accept-Language`: на сервере локаль есть
в `params`, на клиенте — `useLocale()`. Хелперы в `venuesApi.ts`/`authApi.ts`
принимают `locale` явным аргументом.

### Шаг 7.6 — Навигация и выход из аккаунта

Шапки нет вообще: единственный глобальный UI — плавающая пилюля с языком и
темой. Из `/me` никуда не уйти, `logout()` в `authApi.ts` написан, но нигде
не вызывается.

**Смежный баг, чинится здесь же:** `POST /api/auth/refresh` фронтенд не
дёргает никогда. Access-токен живёт 15 минут → через 15 минут сессия молча
умирает, и шапка будет показывать «вы не вошли», пока человек реально вошёл.

**Решения (приняты, не пересматривать):**

| Что | Как |
|---|---|
| Шапка | Одна на все страницы, sticky, `bg-background/80 backdrop-blur-md` + тонкая нижняя граница — продолжение языка существующей пилюли, не чужеродный блок. Высота `h-14` |
| Пилюля | Убрать из layout, её содержимое (язык, тема) переезжает в шапку |
| Меню пользователя | **Без дропдауна:** имя — просто ссылка на `/me`, рядом иконка выхода. Нет click-outside/Escape/фокуса — меньше кода и на клик меньше до профиля |
| Мобилка | **Без бургера:** ссылок всего две, на узком экране прячем подписи (`hidden sm:inline`), оставляем иконки |
| Состояние авторизации | Клиентский компонент через `fetchMe`. Сервером нельзя: layout не умеет обновить протухший токен и врал бы «не вошли» |

**Файлы:**

- `apps/web/src/lib/fetchWithRefresh.ts` (новый) — обёртка над `fetch`: на 401
  один раз дёргает `POST /api/auth/refresh` (`credentials: "include"`) и
  повторяет запрос. Внутри `authApi.ts` и `venuesApi.ts` заменить голый
  `fetch(` на неё — сигнатуры функций не меняются, места вызова не трогаются.
  **Не оборачивать:** `uploadToPresignedUrl` (льёт в R2, не в наш API),
  `logout`, и сам вызов refresh (рекурсия).
- `apps/web/src/components/SiteHeader.tsx` (новый, серверный) — слева лого
  (`/icon.svg`, он уже отдаётся) + «game.org.az» → ссылка на `/`; справа
  ссылки Площадки (`/venues`) и Виды спорта (`/sports`), затем
  `<LocaleSwitcher/>`, `<ThemeToggle/>`, `<UserMenu/>`. `apiUrl` читает из
  `process.env.NEXT_PUBLIC_API_URL` и отдаёт пропом — как остальные страницы.
- `apps/web/src/components/UserMenu.tsx` (новый, клиентский) — `fetchMe`;
  вошёл → имя-ссылка на `/me` + кнопка выхода (`SignOut` из
  `@phosphor-icons/react`, зовёт `logout()` → `router.push("/")` +
  `router.refresh()`); не вошёл → кнопка «Войти» на `/login`. Пока грузится —
  заглушка фиксированной ширины, чтобы шапка не прыгала.
- `apps/web/src/app/[locale]/layout.tsx` — вместо плавающей пилюли отрендерить
  `<SiteHeader/>` перед `{children}`.
- `apps/web/messages/{az,ru,en}.json` — неймспейс `Nav`: `venues`, `sports`,
  `myProfile`, `login`, `logout`.

**Проверка:** только `pnpm exec tsc --noEmit` и `pnpm run build`. Бэкенд не
трогается, `dotnet build` не нужен. Руками в браузере и на dev **не тестировать
и на сервер не ходить** — пользователь проверит сам после пуша.

### Шаг 8 — События

Самый крупный шаг из всех: новая сущность с кучей состояний + первая фоновая
задача в проекте. Домен уже полностью портирован с Шага 2 — таблицы `events`,
`event_participants`, все констрейнты (`events_time_order`, `events_capacity`,
`events_has_place`, `participants_user_xor_guest`, `participants_waitlist_order`
и т.д.), все enum'ы (`EventType/Status/ParticipationStatus/GenderPolicy/
CostSplit`) уже в БД и в `GameOrg.Domain`, просто ничего из этого не
подключено ни к одному эндпоинту. `Hangfire.AspNetCore`/`Hangfire.PostgreSql`
уже в `GameOrg.Api.csproj` (Шаг 2), но не сконфигурированы в `Program.cs`.

**Разбито на 5 фаз с чёткими границами.** Каждая фаза — самостоятельный
коммит(ы), после которого `dotnet build`/`pnpm run build` зелёные и ничего не
полуработает. Если бюджет токенов кончится — останавливаться строго на
границе фазы, не посередине.

**Явно вне охвата Шага 8** (домен есть, эндпоинтов не будет): `EventTeam`
(разделение на команды), `EventResult`/`MvpVote` (результаты, рейтинги
Glicko-2) — это отдельные, ещё не запланированные шаги.

**Решение пользователя:** напоминания шлёт **сам новый API** напрямую через
Telegram Bot API (`sendMessage`), тем же `TELEGRAM_BOT_TOKEN`, который сейчас
используется только для проверки Login Widget. Старый бот
(`game-organization-bot`) в это не вовлечён и не трогается.

#### Фаза 8.1 — Домен под мультиязычность + миграция

`Event.Title`/`Description` → `TitleI18n`/`DescriptionI18n`
(`Dictionary<string,string>?`, `TitleI18n` optional — у события заголовок
не обязателен, в отличие от названия площадки) — механизм из Шага 7.5
(`Localized.Resolve`, `LocalizedTextDto`, jsonb + `JsonConversions.For<>()`).
Миграция простая (в отличие от `MultilingualUserContent` — тут поля и
раньше были nullable, backfill без раздумий: `title_i18n = jsonb_build_object
('az', title) WHERE title IS NOT NULL`).

#### Фаза 8.2 — CRUD + запись/выход, без вейтлиста и дедлайна

- `PublicId` — короткий случайный id для `/e/{PublicId}`, генератор по
  образцу `VenueSlugGenerator` (только random, без транслитерации — это не
  человекочитаемый слаг).
- `EventService`/`EventsEndpoints`/`EventDtos` — по образцу `VenueService`/
  `VenuesEndpoints`/`VenueDtos`: `EventDto` (список, резолвнутые
  `title`/`description`), `EventDetailDto` (+ `titleI18n`/`descriptionI18n`
  для формы редактирования), `CreateEventRequest`/`UpdateEventRequest`.
- `GET /api/events` (фильтры: sportId, upcoming/past, cityId через Venue),
  `GET /api/events/{publicId}`, `POST /api/events`, `PATCH /api/events/{id}`
  (только создатель — тот же паттерн владения, что у Venue).
- `POST /api/events/{id}/participants` (self: `{status: Confirmed|Maybe}`),
  `DELETE /api/events/{id}/participants/me`. Гость: тот же POST, но
  `guestName` вместо статуса, `InvitedById` = текущий юзер (по констрейнту
  `participants_user_xor_guest` — либо `UserId`, либо `GuestName`+
  `InvitedById`, третьего не дано).
- Валидация как в `VenueService`: `events_has_place` (`VenueId` или
  `CustomLocation`) и `events_club_visibility` (Club-видимость требует
  `ClubId`) — простые проверки, до похода в БД, а не полагаться на то, что
  Postgres вернёт constraint violation.

#### Фаза 8.3 — Вейтлист, дедлайн записи, отмена

- Вейтлист: `ConfirmedCount >= MaxParticipants` → новая запись уходит в
  `Waitlisted` (если `WaitlistEnabled`), иначе 400. Приватный метод
  `PromoteFromWaitlistAsync` (по образцу `RecomputeRatingAsync` в
  `VenueService` — отдельный шаг после любого изменения состава): при
  выходе/отказе Confirmed-участника первый по `WaitlistOrder` становится
  Confirmed → пишет `Notification` (`WaitlistPromoted`).
- `RegistrationClosesAt` считается при создании из `LockHoursBeforeStart`
  (`StartsAt - N часов`) — не на лету при каждом запросе (комментарий в
  домене это уже фиксирует). После дедлайна — `POST .../participants`
  отвечает 400.
- Отмена: `POST /api/events/{id}/cancel` (только создатель) — `Status =
  Cancelled`, `CancelledAt`, `CancelReason`. Пишет `Notification` (
  `EventCancelled`) на всех текущих участников и сразу отправляет
  (см. 8.4 — тот же `TelegramSender`, что и у напоминаний, отмена не ждёт
  расписания).

#### Фаза 8.4 — Hangfire + напоминания в Telegram

- `Program.cs`: `AddHangfire` на `UsePostgreSqlStorage` (та же строка
  подключения, что и EF) + `AddHangfireServer`. Дашборд (`/hangfire`) —
  **не открывать наружу** без авторизации (пока не решено, как её приделать
  быстро — либо не подключать `UseHangfireDashboard` вообще на dev/prod,
  либо за `RequireAuthorization()`, решить в моменте).
  `AutoMigrate`/health-check паттерн не трогать.
- `GameOrg.Infrastructure/Notifications/TelegramSender.cs` — тонкая обёртка
  над `POST https://api.telegram.org/bot{token}/sendMessage`
  (`chat_id` = `Account.ProviderUserId` для `Provider = Telegram`, это и
  есть numeric telegram user id, годится как chat_id для приватного чата).
  Failure (юзер не открывал бота / заблокировал) — не кидать исключение,
  писать `Notification.Status = Failed` + `Error`, идти дальше. Тот же
  сервис переиспользуется и для немедленной отправки отмены (8.3), и для
  запланированных напоминаний — не дублировать HTTP-вызов.
  `builder.Services.AddSingleton<R2StorageService>()` — образец для
  регистрации (тоже "может быть не настроен, тогда просто не работает",
  тот же `IsConfigured`-паттерн, если `TELEGRAM_BOT_TOKEN` пуст).
- Recurring job (`RecurringJob.AddOrUpdate`, раз в 10–15 минут): выбирает
  `Event` с `StartsAt` через ~24ч/~2ч и `Status IN (Scheduled, Confirmed)`,
  для каждого `Confirmed`-участника с `DedupeKey =
  "EVENT_REMINDER_24H:{eventId}:{userId}"` (уже задокументированная в
  `Notification` дедупликация) — если такой `Notification` ещё нет, создать
  + отправить. Идемпотентно при повторном срабатывании.

#### Фаза 8.5 — Frontend

`apps/web/src/lib/eventsApi.ts` (по образцу `venuesApi.ts` — locale-параметр
у каждой функции, `Accept-Language`, `fetchWithRefresh` на авторизованных
вызовах). Страницы: `/events` (список, фильтр по виду спорта), `/events/[publicId]`
(детальная: место/время/участники/кнопка записи), `/events/new`,
`/events/[publicId]/edit`. `SiteHeader`/`MobileNav` — добавить пункт
"События" в навигацию рядом с Площадками/Видами спорта (третья ссылка,
шапка и так уже на грани — возможно, тесно, проверить при реализации).

### Шаг 9 — Роли и модерация

**Тема в плане не поднималась ни разу** (и в эталонной `schema.prisma` тоже —
там единственный `Role` это `ClubRole` внутри клуба). Итог: сейчас модель прав
= «залогинен» + «ты автор объекта», больше ничего. Любой человек с Telegram
создаёт площадки в общем каталоге, а убрать их некому — `DELETE` площадки и
события **не существует вообще ни у кого**.

Что уже лежит в домене мёртвым грузом: `UserStatus.Suspended` (проверяется
ровно в одном месте — скрывает публичный профиль, но забаненный по-прежнему
логинится и всё создаёт), `VenueStatus.Draft/Hidden` (площадка всегда
создаётся `Published`), `VenueClaim`, `Report`, `AuditLog`.

**Решения приняты пользователем (не пересматривать):**

| Вопрос | Решение |
|---|---|
| Роли | `UserRole { User, Moderator, Admin }` на `User`. Модератор ⊂ Админ |
| Модерация площадок | **Премодерация:** новая площадка → `Draft`, в каталог после проверки |
| Первый админ | env-переменная `ADMIN_TELEGRAM_IDS` (список telegram_id через запятую) |
| Удаление | Автор **и** модератор, всегда soft-delete (`DeletedAt`) |

**Вне охвата Шага 9** (домен есть, кода не будет): `VenueClaim` (заявка «это
моя площадка» → права владельца на карточку), `Report` (жалобы), `AuditLog`.
Это отдельный шаг — иначе текущий раздувается вдвое.

#### Фаза 9.1 — Роли, JWT, бан

- `UserRole` в `Enums.cs`; `User.Role` (default `User`), маппинг
  `HasConversion<string>().HasMaxLength(32)` как у остальных enum'ов; миграция
  с `defaultValue: "User"`.
- `TokenService.CreateAccessToken` — claim `role`. В
  `TokenValidationParameters` выставить `RoleClaimType = "role"`: у нас
  `MapInboundClaims = false`, поэтому без этого `RequireRole` искал бы
  длинный `ClaimTypes.Role` URI и никогда не находил (та же ловушка, что уже
  ловили с `sub`).
- Политики: `"Moderator"` → `RequireRole("Moderator", "Admin")`, `"Admin"` →
  `RequireRole("Admin")`. Админ всегда может то же, что модератор.
- `ADMIN_TELEGRAM_IDS`: в `IdentityService.SignInAsync` — если
  `ProviderUserId` в списке, ставим `Admin`. Проверять **и при создании, и при
  каждом логине** (иначе добавление себя в список не подействует на уже
  существующий аккаунт). Только повышать, никогда не понижать — иначе роль,
  выданную вручную, затрёт при следующем входе.
- **Бан начинает работать:** `SignInAsync` и `TokenService.RefreshAsync` при
  `Status != Active` не выдают токены. Проверять на каждом запросе не будем —
  это поход в БД на каждый вызов; лаг до 15 минут (жизнь access-токена) здесь
  приемлем.
- Конфиги: `.env.example`, `infra/compose.dev.yml`, `deploy-dev.yml`.
  **Ручной шаг пользователя:** добавить GitHub Secret `ADMIN_TELEGRAM_IDS`
  со своим telegram_id.

#### Фаза 9.2 — Премодерация площадок

- `VenueService.CreateAsync` → `Status = Draft`. Исключение: создаёт
  модератор/админ → сразу `Published` (не плодить очередь себе же).
- `GET /api/venues` — фильтр `Status == Published` (сейчас фильтра по статусу
  нет вообще). `GET /api/venues/{slug}` — `Draft` видят только автор и
  модератор, иначе автор после создания попадал бы на 404.
- `POST /api/venues/{id}/publish` и `/hide` — политика `"Moderator"`.
- `GET /api/moderation/venues` — очередь на проверку.
- Уже существующие площадки остаются `Published`, миграция данных не нужна.

#### Фаза 9.3 — Удаление

`DELETE /api/venues/{id}` и `DELETE /api/events/{id}` — автор или модератор,
проставляют `DeletedAt`. Глобальный `HasQueryFilter(e => e.DeletedAt == null)`
уже стоит у обеих сущностей, так что из выдач пропадут сами.

У события остаётся и `cancel`, и `delete` — это разные вещи: отмена уведомляет
участников («не состоится»), удаление молча убирает мусор.

#### Фаза 9.4 — Frontend

`role` в `MeProfileDto` (только там — `PublicProfileDto` не трогать, он идёт
через Kiota, который в этой среде не перегенерировать). Кнопка «Удалить» у
автора и модератора. Плашка «На модерации» на своей `Draft`-площадке.
Страница `/moderation` — очередь с кнопками Опубликовать/Скрыть, пункт в
`SiteHeader`/`MobileNav` виден только модератору/админу.

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

Спросить, когда дойдёт до соответствующего шага, не решать самостоятельно.

**Все закрыты:**

1. ~~**Шаг 4:** A-запись `dev.game.org.az` в Cloudflare~~ — создана, dev работает.
2. ~~**Шаг 7:** отдельный R2-бакет под медиа~~ — создан `game-org-media`, токен и
   публичный `r2.dev`-URL заведены, 5 секретов добавлены в GitHub Actions.
3. ~~**Шаг 5:** какой Telegram-бот для логина на dev~~ — существующий
   `@gameorganizationbot` (домен бота остаётся `webapp.game.org.az`).

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
