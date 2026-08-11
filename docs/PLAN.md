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

### Шаг 10 — Жалобы, заявки на площадки, аудит-лог

Вынесено из Шага 9 (§703, «вне охвата»): `VenueClaim`, `Report`, `AuditLog` —
домен и EF-конфиги есть с Шага 2, кода нет нигде (проверено — ни одного
упоминания вне `Domain/`/`Infrastructure/Configurations`/миграций).

**Решения приняты пользователем (не пересматривать):**

| Вопрос | Решение |
|---|---|
| Одобрение `VenueClaim` | Владение переходит заявителю: `Venue.CreatedById = claim.UserId` |
| Рассмотрение `Report` | Только очередь + статус (`Resolved`/`Rejected` + заметка), без авто-действий |
| Цели жалоб | Все четыре: `Venue`, `Event`, `User` (профиль), `VenueReview` |
| `AuditLog` | Только запись в этом шаге, страницы просмотра нет |

Раз жалоба на пользователя не запускает никакого действия автоматически, а
только очередь — модератору нужен *хоть какой-то* рычаг после её рассмотрения.
Эндпоинта бана сейчас не существует (Шаг 9.1 добавил только *проверку* статуса
при логине, не способ его выставить) — добавляется здесь же, иначе жалоба на
юзера полностью бесполезна.

#### Фаза 10.1 — Домен

- `VenueClaim`: добавить `Guid? ResolvedById` + `User? ResolvedBy` (симметрично
  `Report.ResolvedBy` — сейчас чинить некому). `HasOne(e => e.ResolvedBy)...
  OnDelete(SetNull)`. Заодно сделать явной связь `Venue` в
  `VenueClaimConfiguration` (сейчас неявная, по конвенции EF) — не трогать
  поведение, просто явно.
- Миграция `AddVenueClaimResolvedBy`, новая колонка nullable — по правилу
  проекта не ломает существующие строки.
- Новый enum не нужен — статусы `ReportStatus` уже подходят обеим сущностям.

#### Фаза 10.2 — `AuditLogService` + ретрофит на Шаг 9

- `Features/Moderation/AuditLogService.cs` (новый слайс — Report/AuditLog
  режут поперёк Venues/Events/Profiles, в отличие от VenueClaim): `Task
  LogAsync(Guid? actorId, string action, string entityType, Guid entityId,
  object? before, object? after, CancellationToken ct)`. `before`/`after` —
  JSON-круговорот через `System.Text.Json` в `Dictionary<string, object>?`
  (колонка уже `jsonb`, конвертер общий `JsonConversions.For<T>()` уже есть в
  Infrastructure — переиспользуется, не пишется заново).
- `AddScoped<AuditLogService>()` в `Program.cs`.
- Ретрофит вызовов в уже существующий код Шага 9 (не меняет их поведение,
  только логирует): `VenueService.SetStatusAsync` → `venue.publish`/
  `venue.hide`; `VenueService.DeleteAsync` → `venue.delete`;
  `EventService.DeleteAsync` → `event.delete`.

#### Фаза 10.3 — Жалобы (`Report`)

- `Features/Moderation/ReportDtos.cs`: `ReportDto`, `CreateReportRequest`
  (клиентский `TargetType` enum `Venue|Event|User|Review` + `TargetId` —
  API-эргономика поверх денормализованных FK; `Reason: ReportReason`,
  `Comment?`), `ResolveReportRequest` (`Status: Resolved|Rejected`,
  `ResolutionNote?`).
- `Features/Moderation/ReportService.cs`: `CreateAsync` (маппит `TargetType`
  в нужную FK-колонку, `Status = Open`), `GetQueueAsync` (только
  Moderator/Admin, `Open`/`InReview`, с человекочитаемым именем цели для
  очереди), `ResolveAsync` (статус + `ResolvedById` + `ResolvedAt` +
  `ResolutionNote`, лог `report.resolve`).
- Минимальный рычаг для жалоб на пользователя — бана сейчас нет вообще:
  `POST /api/moderation/users/{id}/ban` / `/unban`, `RequireAuthorization
  ("Moderator")`, выставляет `UserStatus.Suspended`/`Active`, лог
  `user.ban`/`user.unban`. Забанить Moderator/Admin может только Admin —
  простая проверка роли цели перед сменой статуса.
- `Features/Moderation/ModerationEndpoints.cs` (новый файл): `POST
  /api/reports` (любой залогиненный), `GET /api/moderation/reports`
  (Moderator), `POST /api/moderation/reports/{id}/resolve` (Moderator), плюс
  бан/анбан выше.

#### Фаза 10.4 — Заявки на площадки (`VenueClaim`)

- `Features/Venues/VenueClaimDtos.cs` + методы в `VenueService.cs` (не новый
  сервис — claim привязан к площадке, тот же слайс, что publish/hide из
  Шага 9.2): `CreateClaimAsync` (409, если пара `VenueId+UserId` уже есть —
  unique-индекс это и так не пустит, отдаём понятную ошибку вместо 500),
  `GetClaimQueueAsync` (Moderator, `Open`), `ApproveClaimAsync` (статус
  `Resolved` + `ResolvedById`/`ResolvedAt`, `venue.CreatedById =
  claim.UserId`, лог `venueclaim.approve`), `RejectClaimAsync` (статус
  `Rejected` + резолюция, лог `venueclaim.reject`).
- Эндпоинты в `VenuesEndpoints.cs`: `POST /api/venues/{id}/claims`
  (`RequireAuthorization()`), `GET /api/moderation/venue-claims` /
  `POST .../{id}/approve` / `POST .../{id}/reject` (`"Moderator"`).

#### Фаза 10.5 — Frontend

- `apps/web/src/lib/moderationApi.ts` (новый) — жалобы + бан/анбан.
  `venuesApi.ts` — claim-функции (по аналогии с уже существующим разделением
  файлов по бэкенд-слайсам).
- `ReportButton.tsx` — переиспользуемый клиентский компонент (кнопка +
  инлайн-форма: причина из `ReportReason` + комментарий), подключается на
  `/venues/[slug]`, `/events/[publicId]`, `/[handle]`, и в `ReviewsSection.tsx`
  на каждый отзыв.
- `VenueActions.tsx` — кнопка «Заявить права на площадку» для залогиненных
  не-владельцев.
- `/moderation` — расширяется вкладками/секциями: очередь жалоб (с
  кнопкой бан/анбан на жалобах на пользователя) и очередь заявок на площадки
  (Одобрить/Отклонить), рядом с уже существующей очередью публикации.
- i18n-строки (az/ru/en): причины жалоб, статусы, заявка на площадку, бан.

---

### Шаг 11 — Уведомления: in-app список, настройки, новые типы событий

Домен готов с Шага 2 (`Notification`, `NotificationPreference`, `DeviceToken`,
`NotificationType`/`NotificationChannel`/`DeliveryStatus`), но: `(DedupeKey,
Channel)` unique в БД никем не используется — `NotificationSender` пишет
только `Channel = Telegram`; `NotificationPreference` никто не читает; из
~15 значений `NotificationType` реально вызываются 4
(`EventReminder24h/2h`, `EventCancelled`, `WaitlistPromoted`); читать
уведомления (страница/список на сайте) негде — эндпоинта нет вообще.

**Решения приняты пользователем (не пересматривать):**

| Вопрос | Решение |
|---|---|
| Новые типы | `EventUpdated`, `ParticipantJoined`/`ParticipantLeft` + авто-переход в `EventConfirmed` |
| In-app список | Делать сейчас — это главное в шаге |
| Настройки (preferences) | Делать сейчас — простая страница вкл/выкл по типам |
| Push (`DeviceToken`) | Вне охвата — отдельная инфраструктура (VAPID, service worker), не в этом шаге |

`ClubInvite`/`ClubJoinRequest`/`NewFollower`/`MvpVoteOpen`/`ResultPosted` —
не добавляются, для них нет ни `Clubs`, ни `Follow`, ни `MvpVote`/
`EventResult` (см. Шаг 8, «вне охвата»). `PaymentDue`/`PaymentConfirmed` —
аналогично, `Payment`-фичи ещё нет.

`EventConfirmed` — **только вперёд**, без отката: если участник вышел и
`ConfirmedCount` упал ниже `MinParticipants`, статус остаётся `Confirmed`.
Причина: не дёргать статус туда-сюда и не спамить «событие снова не
подтверждено» — организатор увидит упавший счётчик на странице и решит сам
(отменить или ждать).

#### Фаза 11.1 — `NotificationSender`: мультиканальность + preferences

- `SendAsync` пишет **две** записи `Notification` на один повод: `InApp`
  (всегда, сразу `Status = Sent` — читать её будет фронт, внешней доставки
  не требует) и `Telegram` (как раньше, но только если
  `NotificationPreference` не выключил её явно). `(DedupeKey, Channel)` —
  разные `Channel` у двух строк с одинаковым `DedupeKey`, unique-индекс
  этому не мешает, схема именно под это и рассчитана.
- Новый приватный метод `IsChannelEnabledAsync(userId, type, channel)` —
  **opt-out**: строки в `NotificationPreference` нет → включено по
  умолчанию; есть строка с `Enabled = false` → выключено.

#### Фаза 11.2 — Новые типы уведомлений в `EventService`

- `RecomputeCountsAsync` — после пересчёта `ConfirmedCount`: если
  `ev.Status == Scheduled`, `ev.MinParticipants != null` и `ConfirmedCount
  >= MinParticipants` → `ev.Status = Confirmed`, разослать
  `NotificationType.EventConfirmed` всем текущим `Confirmed`-участникам
  (`DedupeKey: EVENT_CONFIRMED:{eventId}:{userId}`).
- `JoinAsync` — после успешной записи уведомить создателя события
  (`ParticipantJoined`, `DedupeKey` по `participant.Id` — свежий Guid на
  каждый join, так что натурально не дублируется), кроме случая
  «создатель записался сам на себя».
- `LeaveAsync` — captured `participant.Id` до `Remove`, уведомить создателя
  (`ParticipantLeft`), тот же принцип пропуска self-notify.
- `UpdateAsync` — захватить `StartsAt`/`EndsAt`/`VenueId`/`CustomLocation`
  до применения патча; если хоть одно поменялось — после сохранения
  уведомить всех `Confirmed`+`Maybe` участников (`EventUpdated`,
  `DedupeKey` с `ev.UpdatedAt.Ticks`, чтобы повторные правки тоже слали).

#### Фаза 11.3 — Backend: чтение уведомлений

- `Features/Notifications/` (новый слайс): `NotificationDtos.cs`,
  `NotificationService.cs`, `NotificationsEndpoints.cs`.
- `GET /api/notifications?unreadOnly=&skip=&take=` — только `Channel =
  InApp`, свои, `CreatedAt desc`.
- `GET /api/notifications/unread-count` — для бейджа в шапке.
- `POST /api/notifications/{id}/read`, `POST /api/notifications/read-all`.

#### Фаза 11.4 — Backend: настройки

- `GET /api/me/notification-preferences` — все значения `NotificationType`
  с текущим состоянием `Telegram`-канала (`true`, если строки нет).
- `PUT /api/me/notification-preferences/{type}` — `{ enabled: bool }` для
  канала `Telegram`, upsert.

#### Фаза 11.5 — Frontend

- `notificationsApi.ts` — список/непрочитанные/read/read-all/preferences.
- `NotificationBell.tsx` в `UserMenu` — бейдж непрочитанных, дропдаун с
  последними уведомлениями и ссылкой «Все уведомления».
- `/notifications` — полный список с пагинацией, клик — mark read.
- `/me/notifications` — переключатели по каждому `NotificationType`
  (только `Telegram`-канал, `InApp` всегда включён — его нельзя выключить,
  иначе колокольчик станет бесполезным).
- i18n: человекочитаемые названия типов уведомлений (ru/az/en) — используются
  и в списке, и в настройках.

---

### Шаг 12 — Клубы (приватные группы)

Домен готов с Шага 2 (`Club`, `ClubSport`, `ClubMember`,
`ClubVisibility{Public,RequestOnly,Private}`, `ClubRole{Owner,Admin,Member}`,
`MembershipStatus{Pending,Active,Banned,Left}`), кода нет вообще. Взято по
списку модулей §3 (`Clubs` — первый из оставшихся: `Clubs → Payments →
Social → Reputation`).

**Решения приняты пользователем (не пересматривать):**

| Вопрос | Решение |
|---|---|
| `Club.Name`/`Description` | Перевести в i18n (`NameI18n`/`DescriptionI18n`), как `Venue`/`Event`/`User` |
| Интеграция с событиями | Включена: выбор клуба в форме события, `EventVisibility.Club` видна только участникам |
| Кто создаёт клуб | Любой залогиненный (как площадки, Шаг 7) — создатель становится `Owner` |
| `TelegramChatId` | Вне охвата — поле остаётся `null`, флоу привязки TG-группы не строим (не трогаем `game-organization-bot`) |

**Вступление — по `ClubVisibility`, не вопрос, а прямое следствие названий
enum'а:**
- `Public` — вступление сразу `Active`.
- `RequestOnly` — заявка создаёт `Pending`, `Owner`/`Admin` одобряет/отклоняет.
- `Private` — только приглашение `Owner`/`Admin`, самостоятельного запроса
  нет вообще; клуб не показывается в общем каталоге не-участникам (как
  `VenueStatus.Draft` в Шаге 9.2 — те же `viewerId`-aware правила чтения).

Один активный `Owner` на клуб — уже гарантировано partial-unique индексом
`club_members_owner_uq` в `001_constraints.sql`, ничего добавлять не нужно.

#### Фаза 12.1 — Домен: i18n для `Club` + миграция

- `Club.Name` → `NameI18n` (`Dictionary<string,string>`), `Description` →
  `DescriptionI18n` (`Dictionary<string,string>?`). Переиспользовать
  существующие `LocalizedTextDto`/`Localized.Resolve`/`ToDict()` — ничего
  нового не писать, тот же набор, что уже используют Venue/Event/User.
- Миграция — **тем же ручным приёмом**, что `MultilingualUserContent`
  (Шаг 7.5): добавить `*_i18n` nullable → бэкфилл под `'az'` → `NOT NULL`
  на `NameI18n` → дропнуть старые `name`/`description` → заменить
  `clubs_search_trgm` (сейчас на голом `name`) на `search_text`
  (`GENERATED ALWAYS AS (...) STORED` из `name_i18n->>'az'/'ru'/'en'`) +
  новый `GIN`-индекс. EF-скаффолд *дропнет колонки раньше бэкфилла* — как
  и в прошлый раз, писать `Up()`/`Down()` руками, не доверять
  автогенерации порядка операций.

#### Фаза 12.2 — Backend: CRUD клуба

- `Features/Clubs/ClubSlugGenerator.cs` — копия `VenueSlugGenerator`
  (транслитерация + случайный суффикс), с фолбэком `club-{suffix}` —
  по конвенции проекта мелкий дублирующийся хелпер лучше общей абстракции.
- `Features/Clubs/ClubDtos.cs`, `ClubService.cs`: `CreateAsync` (создатель
  сразу пишется `ClubMember{Role=Owner, Status=Active}` в той же
  транзакции), `UpdateAsync` (`Owner`/`Admin`), `GetBySlugAsync(slug,
  locale, viewerId, ct)` — `Private` невидим не-участникам (404, не 403 —
  не палим сам факт существования), `GetListAsync` — `Public`/`RequestOnly`
  всем, `Private` только клубы viewer'а, `DeleteAsync` (soft-delete,
  `Owner` — то же самое, что «только автор» у площадок).
- `Features/Clubs/ClubsEndpoints.cs`: `GET /api/clubs`, `GET
  /api/clubs/{slug}`, `POST /api/clubs`, `PATCH /api/clubs/{id}`, `DELETE
  /api/clubs/{id}` — по образцу `VenuesEndpoints.cs`.
- Фото (avatar/cover) — **вне охвата Шага 12**, пересмотрено по ходу: поля
  `AvatarId`/`CoverId` в домене остаются, но отдельный presign/attach-флоу
  под них — не основная часть «приватных групп», раздувает шаг. `MediaAsset`
  никуда не убегает, можно добавить отдельным заходом по образцу фото
  площадок из Шага 7.

#### Фаза 12.3 — Backend: участники

- `ClubMemberDtos.cs` + методы в `ClubService.cs`: `JoinAsync`
  (маршрутизация по `Visibility`, см. решение выше), `LeaveAsync`,
  `InviteAsync` (`Owner`/`Admin`, создаёт `Pending`-приглашение — шлёт
  `NotificationType.ClubInvite`), `ApproveJoinRequestAsync`/
  `RejectJoinRequestAsync` (`Owner`/`Admin`), `SetRoleAsync` (только
  `Owner` назначает `Admin`; смена `Owner` — отдельный `TransferOwnershipAsync`,
  раз в БД допустим только один активный `Owner`), `RemoveMemberAsync`
  (`Owner`/`Admin`, себя убрать нельзя — `Owner` сначала передаёт
  владение), `JoinByInviteCodeAsync`, `RegenerateInviteCodeAsync`.
- Заявка на вступление в `RequestOnly` шлёт `NotificationType
  .ClubJoinRequest` всем `Owner`/`Admin` клуба — **первые реальные
  вызовы** этих двух типов (раньше в Шаге 11 они не входили в
  `NotificationService.WiredTypes` именно потому, что Clubs не было —
  добавить оба в список).
- Эндпоинты в `ClubsEndpoints.cs`: `POST /api/clubs/{id}/join`, `POST
  /api/clubs/{id}/leave`, `POST /api/clubs/{id}/invite`, `POST
  /api/clubs/{id}/join-requests/{userId}/approve`|`/reject`, `PATCH
  /api/clubs/{id}/members/{userId}`, `DELETE
  /api/clubs/{id}/members/{userId}`, `POST /api/clubs/join/{inviteCode}`,
  `POST /api/clubs/{id}/invite-code/regenerate`.

#### Фаза 12.4 — Backend: интеграция с событиями

- `CreateEventRequest`/`UpdateEventRequest` уже принимают `ClubId` — добавить
  проверку в `EventService`: `ClubId` можно поставить только на клуб, где
  создатель — активный участник (иначе 403, понятная ошибка вместо голого
  `events_club_visibility` constraint violation).
- `GetByPublicIdAsync`/`GetListAsync` — `Visibility == Club` событие видно
  только активным участникам этого клуба (тот же `viewerId`-aware приём,
  что `VenueService.GetBySlugAsync` для `Draft`).

#### Фаза 12.5 — Frontend

- `clubsApi.ts` — CRUD, участники, инвайт-код (по образцу `venuesApi.ts`).
- `/clubs` — каталог (фильтр по городу/спорту, как `/venues`), `/clubs/new`,
  `/clubs/[slug]` (детальная + список участников для членов, кнопки
  Вступить/Запросить/Покинуть по статусу), `/clubs/[slug]/edit`,
  `/clubs/[slug]/members` (управление для `Owner`/`Admin`: одобрить/отклонить
  заявку, роль, удалить, инвайт-код).
- `EventForm.tsx` — селектор клуба (только те, где участник), делает
  `EventVisibility.Club` наконец выбираемым (сейчас форма принудительно
  сводит его к `Public`).
- Пункт «Клубы» в `SiteHeader`/`MobileNav`.
- i18n: статусы участников, роли, причины отказа.

### Шаг 13 — Подписки и лента (Social)

Домен готов с Шага 2 (`Follow` — три nullable FK на User/Club/Venue,
CHECK «ровно одна цель» + «нельзя подписаться на себя» в
`001_constraints.sql`; `Activity` — `ActivityVerb`, `Audience`, jsonb
`Payload`, индексы под выборку по автору/аудитории/клубу), кода нет
вообще. Взято по списку модулей §3 (`Social` — следующий из оставшихся:
`Payments → Social → Reputation`, но пользователь выбрал Social раньше
Payments — деньги решили отложить до выбора провайдера).

**Решения по умолчанию (прямые следствия уже принятой схемы, не вопрос
пользователю):**

- API поверх полиморфного `Follow` — не три отдельных набора эндпоинтов
  на User/Club/Venue, а один `Features/Social/` с новым API-only enum'ом
  `FollowTargetType{User,Club,Venue}` (в `Follow`-таблице остаётся как
  есть — три FK; enum только для формы запроса/ответа).
- **Лента = fan-out on read** (§10 «Что НЕ делать» это уже требует):
  `GetFeedAsync(userId)` — объединение (OR) трёх условий: автор входит в
  число тех, на кого подписан viewer; ИЛИ `ClubId` записи входит в число
  подписок viewer'а; ИЛИ `VenueId` записи входит в число подписок. Без
  отдельной feed_entries-таблицы, без фонового job'а.
- **Приватность записи ленты решается в момент записи, не чтения:**
  `ActivityService.EmitAsync` пишет строку, только если сущность-повод
  публично видна (`Club.Visibility != Private`, `Event.Visibility ==
  Public`, `Venue.Status == Published`, для профильных действий —
  `ProfileVisibility == Public`) — иначе не пишет вообще. `Audience`
  всегда остаётся `Public` на уже отфильтрованных записях; вариант
  `Visibility.Followers` в поле `Audience` не используется — усложнение
  без проверенного сценария, поле в схеме просто зарезервировано на будущее.
- **Какие `ActivityVerb` реально подключаются в этом шаге:** `CreatedEvent`,
  `JoinedEvent` (только при переходе в `Confirmed`, не `Maybe`/`Waitlisted`),
  `CreatedClub`, `JoinedClub`, `ReviewedVenue`, `AddedSport`. Не
  подключаются (нет данных/шага под них): `CompletedEvent` (нет флоу
  завершения игры), `EarnedAchievement`/`RatingMilestone` (Achievement и
  Reputation — не построенные пока куски генплана) — как `WiredTypes` в
  `NotificationService` уже разделяет «в enum'е есть» и «реально шлётся».
- Счётчики (`FollowersCount`/`FollowingCount`, `ViewerIsFollowing`) —
  только в *Detail-DTO (`PublicProfileDto`/`MeProfileDto`,
  `ClubDetailDto`, `VenueDetailDto`), не в списках — там это негде
  показывать сейчас, а лишний count-запрос на каждую карточку списка не
  нужен.

#### Фаза 13.1 — Backend: подписки (Follow)

- `Domain/Enums.cs`: `FollowTargetType{User,Club,Venue}`.
- `Features/Social/SocialDtos.cs`: `FollowRequest(FollowTargetType,
  Guid)`, `FollowSummaryDto(FollowTargetType, Guid Id, string Slug,
  string Name, Guid? AvatarId)` — общий для «подписчики» (всегда
  пользователи) и «подписки» (User/Club/Venue вперемешку).
- `Features/Social/FollowService.cs`: `FollowAsync` (проверка что цель
  существует и публично видна — на приватный клуб/venue-в-Draft/приватный
  профиль подписаться нельзя; self-follow → 400; гонка на unique-индексе
  → трактовать как успех, идемпотентно), `UnfollowAsync`,
  `IsFollowingAsync`, `GetFollowersAsync`/`GetFollowingAsync` (пагинация
  Skip/Take как у `NotificationService`), `CountFollowersAsync` (вызывается
  из Profile/Club/Venue сервисов). `FollowAsync` на `TargetType.User`
  шлёт `NotificationType.NewFollower` — **первый реальный вызов** этого
  типа, добавить в `NotificationService.WiredTypes`.
- `Features/Social/SocialEndpoints.cs`: `POST /api/social/follow`,
  `DELETE /api/social/follow`, `GET /api/social/followers?targetType=&targetId=`,
  `GET /api/social/following/{userId}` — все кроме чтения
  `.RequireAuthorization()`.
- Добавить `FollowersCount`, `ViewerIsFollowing` в `PublicProfileDto`/
  `MeProfileDto` (свой `FollowersCount`/`FollowingCount`),
  `ClubDetailDto`, `VenueDetailDto` — через вызов `FollowService` из
  `ProfileEndpoints`/`ClubService`/`VenueService` (конструкторная DI, как
  `ClubService` уже вызывается из `EventService`).
- `Program.cs`: `AddScoped<FollowService>()`.

#### Фаза 13.2 — Backend: лента (Activity)

- `Features/Social/ActivityDtos.cs`: `ActivityActorDto(Guid,string
  Handle,string Name,Guid? AvatarId)`, `ActivityEventDto(Guid,string
  PublicId,string? Title,DateTime StartsAt)`, `ActivityClubDto(Guid,string
  Slug,string Name)`, `ActivityVenueDto(Guid,string Slug,string Name)`,
  `ActivityDto(Guid Id, ActivityActorDto Actor, ActivityVerb Verb,
  ActivityEventDto? Event, ActivityClubDto? Club, ActivityVenueDto?
  Venue, ActivityActorDto? TargetUser, DateTime CreatedAt)`.
- `Features/Social/ActivityService.cs`: `EmitAsync(actorId, verb,
  eventId?, clubId?, venueId?, targetUserId?, ct)` — внутри сам решает
  писать или нет по правилу видимости выше; `GetFeedAsync(userId, skip,
  take, ct)` — fan-out on read запрос, `OrderByDescending(CreatedAt)`.
- Проводка вызовов `EmitAsync`: `EventService.CreateAsync`
  (`CreatedEvent`, если `Visibility==Public`), `EventService.JoinAsync`
  (`JoinedEvent`, только момент перехода в `Confirmed`),
  `ClubService.CreateAsync` (`CreatedClub`, если `Visibility!=Private`),
  `ClubService.JoinAsync`+`ApproveJoinRequestAsync` (`JoinedClub`, тот же
  фильтр), `VenueService.UpsertReviewAsync` (`ReviewedVenue`, если
  `Status==Published`), `ProfileService` upsert вида спорта
  (`AddedSport`, только при первом добавлении конкретного спорта, не при
  апдейте, и только если `ProfileVisibility==Public` и `UserSport.Visibility
  ==Public`).
- `GET /api/social/feed` в `SocialEndpoints.cs`, `.RequireAuthorization()`.
- `Program.cs`: `AddScoped<ActivityService>()`, добавить в конструкторы
  `EventService`/`ClubService`/`VenueService`/`ProfileService`.

#### Фаза 13.3 — Regenerate Kiota + `socialApi.ts`

- `pnpm gen:api` после того, как бэкенд собран.
- `apps/web/src/lib/socialApi.ts` (по образцу `clubsApi.ts`) — `follow`,
  `unfollow`, `getFollowers`, `getFollowing`, `getFeed`, все через
  `fetchWithRefresh`.

#### Фаза 13.4 — Frontend

- `FollowButton.tsx` (клиентский, переключает подписку, принимает
  `targetType`/`targetId`/`initialIsFollowing`) — на `/[handle]`,
  `/clubs/[slug]`, `/venues/[slug]`.
- `/feed` — SSR-каркас + список карточек ленты
  (`ActivityFeedItem.tsx` — по глаголу рендерит разный текст:
  «X создал(а) событие Y», «X вступил(а) в клуб Y» и т.д., ссылка на
  событие/клуб/площадку/актора), пустое состояние «подпишитесь на
  кого-то» со ссылками на `/venues`/`/clubs`.
- Счётчики подписчиков/подписок на `/[handle]` (кликабельные →
  `/[handle]/followers`, `/[handle]/following` — простые списки,
  без функций управления, по образцу `ClubMembersView.tsx` без
  admin-кнопок) и `FollowersCount` на `/clubs/[slug]`, `/venues/[slug]`.
- Пункт «Лента» в `SiteHeader`/`MobileNav` — шапка после Шага 12 уже
  на пределе по ширине (см. `SettingsMenu` — язык+тема уже схлопнуты в
  одну иконку), пятый пункт скорее всего потребует вынести
  Спорт/Площадки во второстепенный dropdown («Ещё») — решить по месту
  при реализации, не отдельным вопросом пользователю.
- i18n: `Nav.feed`, namespace `Social` (тексты по каждому `ActivityVerb`,
  «Подписаться»/«Отписаться», «Подписчики»/«Подписки», пустое состояние).

#### Фаза 13.5 — Сборка и проверка

- `dotnet build` — чисто. `dotnet test` (unit) — 26/26 зелёных, без Docker
  (интеграционные на Testcontainers/PostGIS локально не поднимались —
  Docker Desktop не запущен, пользователь проверит на dev-сервере сам).
- `pnpm --filter web run build` — чисто, все новые роуты (`/feed`,
  `/[handle]/followers`, `/[handle]/following`) собрались. `pnpm run lint` —
  без новых ошибок (2 существующих error/2 warning в чужом коде —
  `ReviewsSection.tsx`/`ThemeToggle.tsx`/`apple-icon.tsx` — не менялись).
- В браузере не проверялось (см. выше) — честно, не выдаю сборку за
  визуальную проверку.

### Шаг 14 — Результаты игр: команды, результат, MVP

Домен готов с Шага 2 (`EventTeam`, `EventResult`, `MvpVote`,
`EventParticipant.TeamId`, `mvp_no_self` в `001_constraints.sql`), кода нет
вообще — явно вынесено «вне охвата» в Шаге 8 (§8, "Явно вне охвата Шага 8").
Взято по выбору пользователя из оставшихся кусков генплана.

**Решения по умолчанию (не вопрос пользователю):**

- Живёт в `Features/Events/` — это под-ресурсы события, не отдельный модуль
  (`EventTeamDto`/`EventResultDto`/`MvpVote*` — в `EventDtos.cs`/новом
  `EventResultDtos.cs`, методы — в существующем `EventService`).
- **`SportRating` (Glicko-2) — сюда не входит.** `EventResult.RatingsApplied`
  остаётся `false` всегда — пересчёт рейтинга по результату будет отдельным
  шагом («Рейтинг и надёжность»), который явно требует уже записанных
  `EventResult`, поэтому логичен только после этого шага. Здесь только
  фиксация факта: кто с кем в команде, какой счёт, кто MVP — задел под
  будущий пересчёт, не сам пересчёт.
- **Завершение события** — новое действие `CompleteAsync`: создатель или
  модератор, только из `Scheduled`/`Confirmed`, только после `EndsAt`
  (иначе понятная ошибка вместо "результата ещё нет"). Открывает MVP-
  голосование и разрешает запись результата — до этого момента ни то, ни
  другое недоступно.
- **MVP — голосование, а не только ручной выбор.** Участники (только те, у
  кого `UserId` — не гости) голосуют друг за друга после `Completed` (не
  за себя — `mvp_no_self` в БД, дублируем проверку с понятным сообщением).
  Голос уникален на пару (EventId, VoterId) — повторное голосование меняет
  цель, не плодит вторую запись. `RecordResultAsync` фиксирует
  `EventResult.MvpUserId` как победителя голосования на момент записи,
  если организатор явно не передал `MvpUserId` — переопределение вручную
  всегда в приоритете (свежее авторское решение важнее голосов).
- **Явка** (`ParticipationStatus.Attended`/`NoShow`/`LateCancel` — есть в
  enum'е с Шага 8, нигде не выставлялись) проставляется тут же, при записи
  результата — естественный момент, когда организатор и так подводит
  итоги. Без этого шага эти три статуса участника были бы мёртвым кодом.
- Команды и результат читаются вместе с событием — расширение
  `EventDetailDto` (`Teams`/`Result`/`MvpTally`/`MyMvpVote`), не отдельные
  GET-эндпоинты — та же логика видимости (`Public`/`Club`-геттинг), что уже
  есть у `GetByPublicIdAsync`, не нужно дублировать.
- `NotificationType.MvpVoteOpen`/`ResultPosted` — **первые реальные
  вызовы** этих двух типов (до сих пор числились в enum'е под ещё не
  построенный кусок, см. `NotificationService.WiredTypes`). `ActivityVerb
  .CompletedEvent` — тот же самый gate по `Visibility == Public`, что уже
  есть у `CreatedEvent`/`JoinedEvent` (Шаг 13).

#### Фаза 14.1 — Backend: завершение события, команды

- `EventService.CompleteAsync(eventId, userId, isModerator, ct)` —
  проверки выше, эмитит `ActivityVerb.CompletedEvent` (если `Visibility
  == Public`), шлёт `NotificationType.MvpVoteOpen` всем `Confirmed`/
  `Attended`-участникам с `UserId`.
- `EventService.SetTeamsAsync(eventId, userId, isModerator, request, ct)`
  — создатель/модератор; полная замена состава команд (как
  `club.Sports.Clear()+Add` в `ClubService`) — удаляет старые
  `EventTeam`, обнуляет `EventParticipant.TeamId` через `ExecuteUpdateAsync`,
  создаёт новые команды, расставляет `TeamId` по присланным спискам
  участников; 400 с понятным текстом, если участник не из этого события
  или встречается в двух командах.
- `Features/Events/EventResultDtos.cs`: `EventTeamDto`, `TeamInput`,
  `SetTeamsRequest`.
- `EventDetailDto` — добавить `Teams: List<EventTeamDto>`.
- `EventsEndpoints.cs`: `POST /api/events/{id:guid}/complete`, `PUT
  /api/events/{id:guid}/teams` — оба `.RequireAuthorization()`.

#### Фаза 14.2 — Backend: результат и MVP-голосование

- `EventResultDtos.cs`: `StandingEntry`, `TeamScoreEntry`,
  `AttendanceEntry`, `RecordResultRequest`, `EventResultDto`,
  `MvpVoteRequest`, `MvpTallyEntryDto`.
- `EventService.VoteMvpAsync(eventId, voterId, targetUserId, ct)` —
  только участник события голосует за другого участника события, только
  после `Completed`; upsert по `(EventId, VoterId)`.
- `EventService.RecordResultAsync(eventId, userId, isModerator, request, ct)`
  — создатель/модератор, только для `Completed`; применяет
  `TeamScores` (`ExecuteUpdateAsync` на `EventTeam.Score`), `Attendance`
  (только `Attended`/`NoShow`/`LateCancel`, иначе 400), резолвит
  `MvpUserId` (явный или победитель голосования), upsert `EventResult`
  (PK — `EventId`, поэтому повторный вызов — редактирование, не 409), шлёт
  `NotificationType.ResultPosted` всем участникам с `UserId`.
- `EventDetailDto` — добавить `Result: EventResultDto?`, `MvpTally:
  List<MvpTallyEntryDto>`, `MyMvpVote: Guid?` (голос текущего viewer'а).
- `EventsEndpoints.cs`: `POST /api/events/{id:guid}/mvp-vote`, `POST
  /api/events/{id:guid}/result` — оба `.RequireAuthorization()`.

#### Фаза 14.3 — Frontend: завершение события и команды

- `eventsApi.ts` — расширить типы (`EventTeam`, `EventResult`,
  `MvpTallyEntry` в `EventDetail`), `completeEvent`, `setEventTeams`.
- `EventActions.tsx` (или новый `EventCompleteAction.tsx`) — кнопка
  «Завершить событие» для создателя/модератора, видна после `EndsAt` и
  только для `Scheduled`/`Confirmed`.
- `TeamsSection.tsx` (новый, только для создателя/модератора, только пока
  не `Completed` — после результата состав команд не трогаем) — форма:
  добавить/убрать команду (название, цвет), распределить участников
  drag-free (простые `<select>` на участника — без drag&drop, это не
  входит в объём).

#### Фаза 14.4 — Frontend: MVP-голосование и результат

- `eventsApi.ts` — `voteMvp`, `recordResult`.
- `MvpVoteSection.tsx` (новый, для участников события после `Completed`,
  своего голоса не показываем как отдельный вариант) — список участников,
  кнопка «Голосовать», текущий тэлли.
- `RecordResultSection.tsx` (новый, для создателя/модератора после
  `Completed`) — summary, по команде — поле счёта (если есть команды) или
  по участнику — место/очки (если команд нет), явка (чекбоксы
  Attended/NoShow/LateCancel), MVP — dropdown с преднабранным победителем
  голосования, можно переопределить.
- Отображение результата (все, после записи) — summary, счёт по
  командам/расстановка, бейдж MVP на участнике.
- i18n: `Events.teams.*`, `Events.result.*`, `Events.mvp.*`.

#### Фаза 14.5 — Сборка и проверка

- `dotnet build`/`dotnet test`, `pnpm --filter web run build`, `pnpm run lint`.
- В браузере не проверялось без локального Docker/Postgres — см. тот же
  честный disclaimer, что в Шаге 13.

### Шаг 15 — Рейтинг и надёжность (Glicko-2)

Домен готов с Шага 2 (`SportRating` — по каждому виду спорта отдельно,
`ReliabilityStat` — глобальная), кода нет вообще. Разблокировано Шагом 14
(`EventResult` — раньше эту фичу нельзя было строить, не на чем считать).
Выбор пользователя из оставшихся кусков генплана, с рекомендацией — как
логичное продолжение Шага 14.

**Решения по умолчанию (не вопрос пользователю — ни разу не заходила
речь о формулах в обсуждениях с пользователем, всё ниже — инженерные
решения по остаточному принципу из уже существующих полей):**

- **Триггер пересчёта** — `EventService.RecordResultAsync`, после
  сохранения `EventResult`: если `!result.RatingsApplied`, зовёт новый
  `RatingService.ApplyResultAsync(eventId, ct)`, тот сам ставит
  `RatingsApplied = true` в конце. **Только один раз** — повторный вызов
  `RecordResultAsync` (правка результата) пересчёт не триггерит, ровно как
  и задумано комментарием в домене ("идемпотентность"). Пересматривать
  задним числом уже применённый рейтинг — отдельная, не запланированная
  здесь фича.
- **Glicko-2 — чистая математика**, `Features/Reputation/Glicko2.cs`, без
  зависимости от БД (как `TelegramLoginValidator` в Шаге 5) — юнит-тесты
  напрямую, без Testcontainers. Стандартный алгоритм (Glickman, 2013):
  один Event = один "rating period" на игрока, все его результаты в этом
  событии сворачиваются в одно обновление μ/φ/σ.
- **Как считаются пары "игра" для Glicko-2:**
  - Командный спорт (`event.Teams` — 2+ команды с проставленным `Score`):
    каждая пара игроков из **разных** команд — один результат
    (`Score` команды A > B → win для A vs B, равенство → draw). Игроки
    внутри одной команды друг с другом не сравниваются — Glicko-2 не
    моделирует "игру с союзником".
  - Без команд, есть `Standings` (место): каждая пара участников с
    указанным местом — win/loss по разнице `Place` (меньше — выше).
  - Ни того, ни другого (организатор записал только текст без счёта) —
    Glicko-2 не считается вообще, `ReliabilityStat` всё равно обновляется
    (см. ниже) — это раздельные заботы.
  - Считаются только участники со статусом `Attended` или `Confirmed` (не
    `NoShow`/`LateCancel`/`Waitlisted`/`Declined`/`Maybe`) и с `UserId`
    (гости — вне рейтинга, ставить не на что).
- **`ReliabilityStat` — тоже в `ApplyResultAsync`, но независимо от
  наличия счёта:**
  - `Signups`/`Attended`/`NoShows`/`LateCancels` — инкремент по
    фактическому `ParticipationStatus` участника на момент записи
    результата. `HostedEvents` — инкремент создателю события (если у
    него есть `UserId`).
  - **`Score` (0-100) — не "штраф с забыванием старых нарушений" в
    буквальном смысле комментария в домене** (для честного затухания
    нужна история инцидентов с датами, которой в схеме нет — заводить
    новую таблицу ради этого не входит в объём). Вместо этого — доля:
    `100 × (1 − (NoShows + LateCancels×0.5) / (Attended + NoShows +
    LateCancels))`. Тот же практический эффект (старое единичное
    нарушение теряет вес по мере роста знаменателя — игрок "отыгрывает"
    репутацию явками), без отдельной таблицы истории.
  - `CurrentStreak`/`LongestStreak` ("недель подряд с игрой") — **не
    инкремент, а пересчёт с нуля** по факту: выборка всех `Attended`-
    участий пользователя по `EventParticipants` (across sports —
    надёжность глобальная), недели по `Event.StartsAt`, чистая функция
    `ReliabilityCalculator.ComputeStreaks(List<DateTime>, DateTime now)`
    — тоже юнит-тестируемая без БД.
- **Чтение** — не отдельный профиль, расширение уже существующих
  ответов: `UserSportDto`/`PublicUserSportDto` (в `ProfileDtos.cs`)
  получают `Rating`/`GamesPlayed`/`Wins`/`Draws`/`Losses` (уже есть в
  `SportRating`, просто не отдавались); `MeProfileDto`/`PublicProfileDto`
  — `ReliabilityScore`. Плюс один новый эндпоинт — лидерборд по виду
  спорта (нигде так и не появился до этого шага): `GET
  /api/sports/{slug}/leaderboard`.

#### Фаза 15.1 — Backend: Glicko-2

- `Features/Reputation/Glicko2.cs` — `Rating(double Mu, double Phi, double
  Sigma)`, `Calculate(Rating player, List<(Rating Opponent, double Score)>
  games) → Rating` — конвертация в/из шкалы Glicko-2, вычисление `v`,
  `delta`, новой волатильности (итерация Illinois), новых `φ`/`μ`. Без
  игр за период — `φ` растёт (неопределённость), `μ`/`σ` не трогаются
  (стандартное поведение алгоритма).
- Юнит-тесты — из официального примера Glickman'а (известные
  контрольные числа) + вырожденные случаи (нет игр, только победы/только
  поражения).

#### Фаза 15.2 — Backend: надёжность

- `Features/Reputation/ReliabilityCalculator.cs` — чистые функции:
  `ComputeScore(int attended, int noShows, int lateCancels) → int`,
  `ComputeStreaks(List<DateTime> attendedDates, DateTime now) → (int
  Current, int Longest)`.
- Юнит-тесты: score — граничные комбинации (0 игр → 100, только NoShow →
  низкий скор); streaks — соседние недели, разрыв, несколько игр в одной
  неделе (считается как одна), текущая неделя без игры пока не рвёт
  `CurrentStreak` (если последняя игра была на прошлой неделе — стрик
  жив, "подряд" считаем по неделям с игрой, не по календарным дням).

#### Фаза 15.3 — Backend: применение и чтение

- `Features/Reputation/RatingService.cs`: `ApplyResultAsync(eventId, ct)`
  — грузит участников (`Attended`/`Confirmed`, есть `UserId`), команды
  или standings, строит пары, зовёт `Glicko2.Calculate` на каждого
  участника по его виду спорта (`event.SportId`), апсертит `SportRating`
  (`GamesPlayed`/`Wins`/`Draws`/`Losses`/`PeakRating`/`LastPlayedAt`),
  параллельно апсертит `ReliabilityStat` всем участникам (не только
  сыгравшим в рейтинге), выставляет `EventResult.RatingsApplied = true`.
  `GetLeaderboardAsync(sportSlug, take, ct)` — топ `SportRating.Rating`
  по виду спорта.
- `EventService.RecordResultAsync` — зовёт `ApplyResultAsync` после
  сохранения результата (см. решение выше).
- `Features/Reputation/ReputationDtos.cs`: `LeaderboardEntryDto(Guid
  UserId, string Handle, string DisplayName, double Rating, int
  GamesPlayed, int Wins, int Draws, int Losses)`.
- `Features/Reputation/ReputationEndpoints.cs`: `GET
  /api/sports/{slug}/leaderboard` (анонимный).
- `ProfileDtos.cs`: `UserSportDto`/`PublicUserSportDto` — добавить
  `Rating`/`GamesPlayed`/`Wins`/`Draws`/`Losses`; `MeProfileDto`/
  `PublicProfileDto` — добавить `ReliabilityScore`. `ProfileEndpoints.cs`
  — подтянуть эти поля при маппинге (join на `SportRating`/
  `ReliabilityStat`).
- `Program.cs`: `AddScoped<RatingService>()`, `MapReputationEndpoints()`.

#### Фаза 15.4 — Frontend

- `eventsApi.ts`/новый `reputationApi.ts` — типы под расширенные
  `UserSport`/профиль, `getLeaderboard`.
- `/me`, `/[handle]` — рейтинг и W-D-L рядом с каждым видом спорта в
  списке, бейдж надёжности (`ReliabilityScore`) в шапке профиля.
- `/sports/[slug]/leaderboard` (новый маршрут — у `/sports` до сих пор не
  было страницы отдельного вида спорта) — таблица топ-игроков, ссылка с
  карточки вида спорта на `/sports`.
- i18n: `Reputation.*` (или `Profile.rating*`/`Sports.leaderboard*` —
  решить по месту, не разводить лишний namespace ради трёх строк).

#### Фаза 15.5 — Сборка и проверка

- `dotnet build`/`dotnet test` (новые юниты на Glicko-2/надёжность —
  обязаны быть зелёными, это единственная проверка правильности формул
  без реального прогона игр), `pnpm --filter web run build`, `pnpm run
  lint`.
- В браузере не проверялось — тот же disclaimer, что в Шагах 13/14.

### Шаг 16 — Достижения (Achievement)

Домен и сид-данные готовы с Шага 2 (`Achievement`/`UserAchievement`,
все 7 ачивок из §7 — `FIRST_GAME`/`TEN_GAMES`/`FIFTY_GAMES`/`IRON_MAN`/
`MVP_FIRST`/`RELIABLE`/`MULTI_SPORT` — уже сидируются `DataSeeder`),
кода на выдачу нет вообще. Разблокировано Шагами 14-15 (`EventResult`,
`SportRating`, `ReliabilityStat` — есть на чём проверять условия).
Выбор пользователя из оставшихся кусков генплана, с рекомендацией.

**Решения по умолчанию (не вопрос пользователю — условия ачивок описаны
в §7 названием и одной фразой, конкретные пороги/интерпретация — инженерное
решение по остаточному принципу):**

- **Единая точка проверки** — `AchievementService.CheckAndAwardAsync(userId,
  eventId, ct)`, вызывается из `RatingService.ApplyResultAsync` для
  каждого участника события (тот же момент, где уже свежие `SportRating`/
  `ReliabilityStat`/`EventResult.MvpUserId` — не нужно тащить данные
  отдельными запросами). Перепроверяет **все** условия при каждом вызове,
  выдаёт только то, чего ещё нет (`UserAchievements` — источник истины,
  не кэш).
- **Условия:**
  - `FIRST_GAME`/`TEN_GAMES`/`FIFTY_GAMES` — сумма `SportRating.GamesPlayed`
    по всем видам спорта пользователя ≥ 1/10/50.
  - `IRON_MAN` — `ReliabilityStat.CurrentStreak` ≥ 4 (недели подряд с игрой,
    Шаг 15).
  - `MVP_FIRST` — пользователь — `EventResult.MvpUserId` **этого** события
    (проверка "не первый ли раз" не нужна отдельно: `AwardIfNotEarned`
    сама не выдаст повторно, а значит достаточно факта "MVP сейчас").
  - `RELIABLE` — `ReliabilityStat.Attended` ≥ 20 **и** `NoShows == 0`
    (за всё время, не скользящее окно — в схеме нет истории по датам для
    честного "20 подряд", тот же компромисс, что в `ComputeScore`,
    Шаг 15).
  - `MULTI_SPORT` — `SportRating.GamesPlayed > 0` у пользователя для 2+
    разных видов спорта (реально сыграл, не просто добавил вид спорта в
    профиль — иначе достижение обесценивается).
- **Уведомление** — `NotificationType.AchievementEarned`, **новое
  значение enum'а** (не было зарезервировано в Шаге 2, в отличие от
  `MvpVoteOpen`/`ResultPosted`). Обоснованно: `NotificationType` хранится
  строкой в БД (`HasConversion<string>()`), добавление значения — не
  breaking change, миграция не нужна; комментарий в `Enums.cs`
  ("Причина: в этом домене enum'ы будут активно расти") — прямая
  санкция на такое расширение.
- **Лента** — `ActivityVerb.EarnedAchievement` (уже в enum'е), тот же
  gate, что `AddedSport` в Шаге 13: эмитится только если
  `User.ProfileVisibility == Public`. Код ачивки кладём в
  `Activity.Payload` (jsonb, `{"achievementCode": "..."}") — под это и
  задумано поле, отдельной колонки не заводить.
- `ActivityService.EmitAsync` — добавить необязательный параметр
  `Dictionary<string,object>? payload = null` (обратная совместимость,
  существующие вызовы не трогать).
- **Чтение** — `MeProfileDto`/`PublicProfileDto` получают `List<AchievementDto>
  Achievements` (только полученные, `EarnedAt` всегда заполнен). Отдельный
  `GET /api/me/achievements` — полный каталог (все 7, `EarnedAt: null` у
  ещё не полученных) — витрина "получено/не получено" для страницы
  трофеев, публичный профиль такое не показывает (не хочет светить, чего
  человек не достиг).

#### Фаза 16.1 — Backend: AchievementService + wiring

- `Features/Reputation/AchievementDtos.cs`: `AchievementDto(string Code,
  string Name, string? Description, string? Icon, int Tier, DateTime?
  EarnedAt)`.
- `Features/Reputation/AchievementService.cs`: `CheckAndAwardAsync` (логика
  выше), `GetMyAchievementsAsync(userId, locale, ct)` — полный каталог с
  `EarnedAt` из `UserAchievements` (null, если нет).
- `Domain/Enums.cs`: `NotificationType.AchievementEarned`.
- `NotificationService.WiredTypes` — добавить (первый реальный вызов).
- `ActivityService.EmitAsync` — необязательный `payload`.
- `RatingService.ApplyResultAsync` — зовёт `achievementService
  .CheckAndAwardAsync(userId, eventId, ct)` для каждого участника после
  апдейта его `SportRating`/`ReliabilityStat`.
- `Features/Reputation/ReputationEndpoints.cs`: `GET
  /api/me/achievements`, `.RequireAuthorization()`.
- `Program.cs`: `AddScoped<AchievementService>()`.

#### Фаза 16.2 — Backend: чтение в профиле

- `ProfileDtos.cs`: `MeProfileDto`/`PublicProfileDto` — добавить
  `Achievements: List<AchievementDto>`.
- `ProfileEndpoints.cs` — подтянуть полученные ачивки при маппинге
  (join `UserAchievements`+`Achievement`, резолв `NameI18n`/`DescI18n`
  по локали).

#### Фаза 16.3 — Frontend

- `reputationApi.ts` — `getMyAchievements`.
- `authApi.ts` — `Achievement`-тип, `achievements` в `MeProfile`.
- `/me` (`MeView.tsx`) — бейджи полученных ачивок, ссылка на
  `/me/achievements`.
- `/[handle]` — бейджи полученных ачивок (через `additionalData`, тот же
  Kiota-приём, что рейтинг/подписчики в Шагах 13/15).
- `/me/achievements` (новый маршрут) — витрина всех 7: получено — иконка
  + дата, не получено — иконка приглушена + описание условия.
- i18n: только текст страницы-витрины (`Reputation.achievements*` или в
  `Me`-namespace — решить по месту), названия/описания самих ачивок уже
  локализованы на бэкенде.

#### Фаза 16.4 — Сборка и проверка

- `dotnet build`/`dotnet test`, `pnpm --filter web run build`, `pnpm run
  lint`.
- В браузере не проверялось — тот же disclaimer, что в Шагах 13-15.

### Шаг 17 — Web Push (DeviceToken)

Домен готов с Шага 2 (`DeviceToken`, `NotificationChannel.Push`,
`DevicePlatform{Ios,Android,Web}`), кода нет — явно вынесено «вне охвата»
в Шаге 11 (§11.2: «Push — вне охвата, отдельная инфраструктура (VAPID,
service worker)»). Выбор пользователя — Web Push, третий канал доставки
после InApp/Telegram (Шаг 11).

**Решения по умолчанию (не вопрос пользователю):**

- **Только `DevicePlatform.Web`** (обычный Web Push API браузера, VAPID).
  `Ios`/`Android` остаются в enum'е под нативные push (FCM/APNs) — другая
  инфраструктура, другой SDK, не входит в объём.
- **`DeviceToken.Token` хранит целиком `PushSubscription` в JSON**
  (`{endpoint, keys:{p256dh, auth}}`) — Web Push API отдаёт объект
  подписки, не строку-токен (в отличие от FCM/APNs). Заводить отдельные
  колонки под `endpoint`/`p256dh`/`auth` — лишняя миграция ради того же
  результата, который уже даёт единственное `Token`-поле; десериализуем
  на отправке.
- **NuGet-пакет `WebPush`** (VAPID-подпись, отправка) — тот же паттерн,
  что `AWSSDK.S3` в Шаге 7.
- **VAPID-ключи** — новые переменные окружения `VAPID_PUBLIC_KEY`/
  `VAPID_PRIVATE_KEY`/`VAPID_SUBJECT` (`mailto:` или URL). Публичный ключ
  фронту не льём отдельной `NEXT_PUBLIC_`-переменной — отдаём с бэкенда
  через `GET /api/push/vapid-public-key` (один источник правды, не нужно
  пересобирать фронт при смене ключа). Как и с Telegram-ботом/R2 (Шаги
  5, 7) — генерация реальных ключей и жизнь в GitHub Secrets — ручной шаг
  пользователя, не могу сделать сам за пределами локальной разработки.
- **`NotificationSender.SendPushAsync`** — по образцу `SendTelegramAsync`:
  одна запись `Notification(Channel=Push)` на `(user, dedupeKey)`
  (уникальность в БД — на пару, не на устройство), но реально шлёт на
  **все** `DeviceToken` пользователя с `Platform=Web` — статус записи
  агрегированный (`Sent`, если хоть одно устройство приняло; `Failed`,
  если все отвалились). Протухшая подписка (410/404 от push-сервиса) —
  сразу удаляется из `DeviceTokens`, самоочистка без отдельного воркера.
- **Настройки уведомлений — обобщение, не дублирование.** У
  `NotificationService.GetPreferencesAsync`/`SetPreferenceAsync` уже
  зашит `NotificationChannel.Telegram` — параметризуем каналом (значение
  по умолчанию `Telegram`, чтобы не сломать существующий фронт), Push
  получает те же переключатели по тем же `WiredTypes`, без нового
  списка типов.

#### Фаза 17.1 — Backend: инфраструктура

- `GameOrg.Infrastructure.csproj`: пакет `WebPush`.
- `Infrastructure/Notifications/WebPushSender.cs` (по образцу
  `TelegramSender.cs`) — `TrySendAsync(DeviceToken device, string title,
  string body, CancellationToken ct) → (bool Sent, bool Expired)` —
  `Expired=true` на 404/410 от push-сервиса, вызывающий код сам решает
  удалить токен.
- `Program.cs`: `AddSingleton<WebPushSender>()`, конфиг `VAPID_PUBLIC_KEY`/
  `VAPID_PRIVATE_KEY`/`VAPID_SUBJECT` из плоских env-переменных.
- `.env.example`, `infra/compose.dev.yml`/`compose.prod.yml` — три новые
  переменные.

#### Фаза 17.2 — Backend: отправка и управление устройствами

- `NotificationSender.SendPushAsync` — логика выше, встроить в общий
  `SendAsync` (третий вызов рядом с `SendInAppAsync`/`SendTelegramAsync`).
- `NotificationService.GetPreferencesAsync`/`SetPreferenceAsync` —
  добавить параметр `NotificationChannel channel = NotificationChannel
  .Telegram`.
- `Features/Notifications/DeviceDtos.cs`: `RegisterDeviceRequest(string
  Token, string? Locale)`.
- `Features/Notifications/DeviceService.cs`: `RegisterAsync` (upsert по
  `Token` — переподписка не плодит дубликат, обновляет `LastSeenAt`),
  `UnregisterAsync(userId, token, ct)`.
- `NotificationsEndpoints.cs`: `GET /api/push/vapid-public-key`
  (анонимный), `POST /api/me/devices`, `DELETE /api/me/devices` (по
  токену в теле — простой девайс себя же и отписывает), оба
  `.RequireAuthorization()`. `GET/PUT /api/me/notification-preferences`
  — добавить необязательный query `channel`.
- `Program.cs`: `AddScoped<DeviceService>()`.

#### Фаза 17.3 — Frontend

- `public/sw.js` — минимальный service worker: слушает `push` (парсит
  JSON пейлоад, `self.registration.showNotification`), `notificationclick`
  (`clients.openWindow` на URL из данных).
- `pushApi.ts` — `getVapidPublicKey`, `registerDevice`, `unregisterDevice`.
- `/me/notifications` (`NotificationPreferences.tsx`, уже существует с
  Шага 11) — новая секция «Push-уведомления в браузере»: кнопка
  «Включить» → регистрация service worker → `Notification.requestPermission()`
  → `pushManager.subscribe({applicationServerKey})` → `registerDevice`;
  «Выключить» → `pushManager.getSubscription().unsubscribe()` +
  `unregisterDevice`. Плюс те же переключатели по типам, что у Telegram,
  но с `channel=Push`.
- i18n: `Notifications.push*`.

#### Фаза 17.4 — Сборка и проверка

- `dotnet build`/`dotnet test`, `pnpm --filter web run build`, `pnpm run
  lint`.
- Без реальных VAPID-ключей и HTTPS (Web Push требует either localhost,
  either HTTPS) сквозной приём push в браузере не проверить — тот же
  честный disclaimer, что был с R2/ботом в своё время: код готов, ждёт
  секретов и реального деплоя.

### Шаг 18 — Оплата (офлайн: Cash/BankTransfer/Balance)

Домен готов с Шага 2 (`Payment`, `PaymentStatus{Pending,Paid,Failed,Refunded,
Cancelled}`, `PaymentMethod{Cash,BankTransfer,CardOnline,Balance}`,
`Event.CostSplit/Cost/Currency`), кода нет вообще. Последний оставшийся
кусок генплана — раньше стоял первым в очереди после Клубов (§3), но
пользователь сознательно отложил его до выбора решения о провайдере
(см. Шаг 12 — "деньги решили отложить").

**Решение пользователя (вопрос в этой сессии):** строить только офлайн-
методы (`Cash`/`BankTransfer`/`Balance`) — организатор вручную отмечает
получение денег, без внешнего платёжного шлюза. `CardOnline` (реальная
интеграция с epoint/payriff/stripe — комментарий в `Payment.cs` уже
называет эти три варианта) — отдельный, не запланированный здесь шаг:
нужен конкретный провайдер, merchant-ключи и проверка подписи webhook'ов,
не то, что можно сделать без ручного шага пользователя.

**Решения по умолчанию (не вопрос — прямое следствие уже существующих
полей `Event.CostSplit`/`Cost`/`EventParticipant`/`Payment`):**

- **Кому выставляется `Payment`** — только участникам с `UserId`
  (гости — `PayerId` в домене обязателен, ставить не на кого, как и
  везде в Reputation/Rating). Создаётся автоматически, когда участник
  становится `Confirmed` (при записи или повышении из вейтлиста), и
  только если `CostSplit != Free`.
- **Сумма:** `CostSplit.PerPlayer` → `Payment.Amount = Event.Cost`
  фиксированно на человека. `CostSplit.Total` → `Event.Cost /
  ConfirmedCount`, округление до 2 знаков (`Math.Round`) — копейка
  расхождения при делении на неровное число участников не решается
  (нет смысла городить алгоритм честного дележа копеек ради MVP),
  пересчитывается заново при каждом изменении состава (join/leave/
  повышение), но **только для ещё не оплаченных** (`Pending`) записей —
  уже полученные деньги задним числом не трогаем.
- **Кто отменяет** — уход участника (`LeaveAsync`) переводит его
  `Pending`-платёж в `Cancelled` (не удаляет — для истории). Отмена
  события (`CancelAsync`) переводит в `Cancelled` все `Pending`-платежи
  события разом. Уже оплаченные (`Paid`) — не трогаются автоматически,
  возврат — ручное действие организатора (перевести статус в `Refunded`
  через тот же эндпоинт, что и подтверждение оплаты).
- **Кто подтверждает оплату** — только создатель события или модератор
  (тот же уровень доступа, что запись результата/явки, Шаг 14) — прямым
  действием "отметить оплаченным", без встречного самостоятельного
  репорта от участника ("я перевёл, подтвердите") — тот же принцип
  простоты, что и явка на Шаге 14 (организатор ставит галочку по факту).
- **`NotificationType.PaymentDue`/`PaymentConfirmed`** — первые реальные
  вызовы (числились в enum'е под этот шаг с Шага 11). `PaymentDue`
  шлётся сразу при создании `Payment` (не отдельным Hangfire-джобом-
  напоминанием по расписанию, как `EventReminder24h/2h`, — за пределами
  объёма, можно добавить отдельным заходом позже).
- **Метод по умолчанию** — `Cash` при создании (организатор ещё не знает,
  как заплатят), меняется на реальный при подтверждении.
- **Видимость** — список платежей события целиком видит только
  создатель/модератор (финансовые данные чужих людей). Участнику в
  `EventDetailDto` встраивается только его собственный платёж
  (`MyPayment`), как `MyMvpVote` на Шаге 14.

#### Фаза 18.1 — Backend: создание, пересчёт, отмена

- `Features/Payments/PaymentService.cs`: `EnsurePaymentAsync(eventId,
  participantId, ct)` (создаёт `Pending`, шлёт `PaymentDue`),
  `RecomputeTotalSplitAsync(eventId, ct)` (только `CostSplit.Total`,
  только `Pending`), `CancelPendingForParticipantAsync(participantId, ct)`,
  `CancelAllPendingForEventAsync(eventId, ct)`.
- `EventService.JoinAsync`/`PromoteFromWaitlistAsync` — после
  `RecomputeCountsAsync`, если участник стал `Confirmed`:
  `EnsurePaymentAsync` + `RecomputeTotalSplitAsync`.
- `EventService.LeaveAsync` — `CancelPendingForParticipantAsync` +
  `RecomputeTotalSplitAsync` после пересчёта состава.
- `EventService.CancelAsync` — `CancelAllPendingForEventAsync`.
- `Program.cs`: `AddScoped<PaymentService>()`, внедрить в `EventService`.

#### Фаза 18.2 — Backend: список организатора и подтверждение

- `Features/Payments/PaymentDtos.cs`: `PaymentDto`, `PaymentSummaryDto`,
  `UpdatePaymentStatusRequest(PaymentStatus Status, PaymentMethod? Method,
  string? Note)`.
- `PaymentService.GetForEventAsync(eventId, viewerId, isModerator, locale, ct)`
  — 403/404 через `(Result, Error)` (как везде), `SetStatusAsync(paymentId,
  actorId, isModerator, request, ct)` — на `Paid` шлёт `PaymentConfirmed`.
- `Features/Payments/PaymentsEndpoints.cs`: `GET
  /api/events/{id:guid}/payments`, `PATCH /api/payments/{id:guid}`, оба
  `.RequireAuthorization()`.
- `NotificationService.WiredTypes` — добавить `PaymentDue`/`PaymentConfirmed`.

#### Фаза 18.3 — Backend: мои платежи

- `PaymentService.GetMyPaymentsAsync(userId, locale, ct)`,
  `GetMyPaymentForEventAsync(eventId, userId, ct)`.
- `GET /api/me/payments`.
- `EventDetailDto` — добавить `MyPayment: PaymentSummaryDto?`,
  `EventService.GetByPublicIdAsync` подтягивает его для `viewerId`.

#### Фаза 18.4 — Frontend

- `paymentsApi.ts` — типы, `getEventPayments`, `updatePaymentStatus`,
  `getMyPayments`.
- `PaymentsSection.tsx` на странице события (только создатель/модератор)
  — список участников с суммой/статусом, кнопки
  Paid/Refunded/Cancelled + выбор метода + заметка.
- Банер «Вы должны X» на странице события для участника с `MyPayment`.
- `/me/payments` (новый маршрут) — все платежи/долги пользователя по
  всем событиям.
- i18n: `Payments.*`.

#### Фаза 18.5 — Сборка и проверка

- `dotnet build`/`dotnet test`, `pnpm --filter web run build`, `pnpm run
  lint`.
- В браузере не проверялось — тот же disclaimer, что в Шагах 13-17.

---

### Шаги 19-24 — Membership, discovery, admin (написано Opus, реализует Sonnet)

Written in English deliberately: this block is an implementation brief, not user-facing
copy. All bot/UI strings it produces still go through i18n in ru/az/en.

Source: user QA session on dev after Step 18. Seven issues raised; grouped below into
six steps. **Do the steps in order** — 19 and 20 change the domain that 21-24 render.

Decisions already made by the user (do not re-litigate):
- Groups are **not** a new entity: `Club` gets a `Kind` discriminator.
- Event join approval is **organizer-controlled**, defaulting to on for Public events.
- Admin panel = `/admin` with a **left sidebar**, sections: Overview, Users, Venues,
  Reports, Claims. Event/club management and audit-log viewer are explicitly out of scope.

---

#### Step 19 — Join requests for events (+ wire the club ones)

**Problem.** `EventService.JoinAsync` admits anyone instantly. Clubs already have
approval (`RequestOnly` → `MembershipStatus.Pending`), events have nothing.

**Domain.**
- `ParticipationStatus`: add `PendingApproval`. Safe — the column is
  `HasConversion<string>()` (see `EventParticipantConfiguration.cs:14`), so adding a
  member does not renumber existing rows.
- `Event`: add `bool RequiresApproval` (default `false`; migration must add it with a
  default so existing rows stay valid).
- `NotificationType`: add `EventJoinRequest`, `EventJoinApproved`, `EventJoinRejected`.
  Same string-storage argument applies.

**Rules.**
- On create/update: if `Visibility == Public`, `RequiresApproval` defaults to `true`;
  for `Club`/`Unlisted` force it to `false` — closed audiences are already gated, and the
  user explicitly accepted that risk ("если кто-то левый попал — ответственный разберётся").
- `JoinAsync`: when `RequiresApproval` and the joiner is not the creator →
  `PendingApproval`, skip capacity/waitlist resolution entirely (capacity is decided at
  approval time, not request time).
- `PendingApproval` must **not** count toward `ConfirmedCount`, must **not** create a
  `Payment`, must **not** emit an `Activity`. Grep every `ParticipationStatus.Confirmed`
  comparison and confirm each one still behaves.
- Approve → run the normal `ResolveJoinStatusAsync` path (so a full event lands the person
  on the waitlist rather than overfilling), then `EnsurePaymentAsync` +
  `RecomputeTotalSplitAsync`, exactly as `PromoteFromWaitlistAsync` does today.
- Reject → delete the participant row (not a tombstone; the person may re-apply).

**Endpoints** (`EventsEndpoints.cs`, both `.RequireAuthorization()`):
- `POST /api/events/{id:guid}/participants/{participantId:guid}/approve`
- `POST /api/events/{id:guid}/participants/{participantId:guid}/reject`
Authorization: event creator, or a club Owner/Admin when `ev.ClubId` is set, or
site Moderator/Admin. Reuse the existing string-matching error→status convention.

**Notifications.** Organizer gets `EventJoinRequest` on request; requester gets
`EventJoinApproved`/`EventJoinRejected` on decision. Add all three plus the already-existing
`ClubJoinRequest` to `NotificationService.WiredTypes` — `ClubJoinRequest` is listed there
already, but verify `ClubService.JoinAsync` actually calls `notificationSender.SendAsync`
for it; if it does not, that is a silent gap to close in this step.

**Frontend.** `EventActions`: button label becomes "Подать заявку" when approval is on;
show a "Заявка на рассмотрении" state for `PendingApproval`. New `JoinRequestsSection`
(organizer-only, mirrors `PaymentsSection`'s `canManage` pattern) listing pending
requesters with Approve/Reject.

---

#### Step 20 — Groups as a kind of Club

**Domain.** `enum ClubKind { Club, Group }`; `Club.Kind` (default `Club`,
`HasConversion<string>()`, migration with default). Nothing else changes — members,
roles, invite codes, join requests, permissions all carry over untouched. This is the
whole point of the decision.

**Backend.** `GET /api/clubs` takes `kind` (nullable → both). `CreateClubRequest` takes
`Kind`. `ClubDto`/`ClubDetailDto` expose it.

**Frontend.** `/groups` and `/groups/new` are thin wrappers over the existing club
pages with `kind=Group` pinned; `/clubs` filters to `kind=Club`. Add "Группы" to
`SiteHeader` nav. i18n: new `Groups` namespace, or reuse `Clubs` keys with a
group-flavoured heading set — the latter is cheaper and acceptable here.

Semantics to put in the UI copy: a club is a standing team, a group is situational
(one-off games, ad-hoc collaborations). No behavioural difference beyond the label.

---

#### Step 21 — Fix visibility of hidden entities

**The actual bug.** `EventService.GetListAsync` (`EventService.cs:415`) admits only
`Public` and `Club` events. An `Unlisted` event is invisible to **its own creator and
its own participants** — they can only reach it if they kept the URL. Clubs do this
correctly already (`ClubService.cs:123`); events were left as a TODO
("Unlisted — отдельная задача, ещё не запланирована") and this step closes it.

Fix the predicate to also admit events where the viewer is the creator or has any
participant row. Verify the same for `ClubService.GetListAsync`: confirm the creator is
inserted as an `Active` `Owner` at creation time, so a `Private` club is never invisible
to the person who made it.

**Copy-link affordance.** On the detail page of any non-public event/club/group, render a
"Скопировать ссылку" control (`navigator.clipboard.writeText`, with a "Скопировано"
confirmation). It must be present on every visit, not just right after creation —
post-create redirect already lands on the entity page
(`ClubForm.tsx:68`, `EventForm.tsx:138`), so no redirect work is needed.

**Discoverability.** Add a "Мои" filter to both lists (created by me, or I am a
member/participant) so hidden things have a home in the UI rather than living only in
saved links.

---

#### Step 22 — Notification deep links

**Good news.** `Notification.Data` already carries `eventId`/`clubId`/`paymentId`
(see the `SendAsync` calls). The list renders plain `<button>`s and drops that payload
on the floor — this is a frontend-only step apart from spot-fixes.

Add `notificationHref(n)` in `apps/web/src/lib/notificationsApi.ts`:

| Type | Target |
|---|---|
| `EventJoinRequest` | `/events/{id}#requests` |
| `EventJoinApproved` / `Rejected` / reminders / `EventUpdated` / `Cancelled` / `Confirmed` / `ParticipantJoined` / `ParticipantLeft` / `WaitlistPromoted` / `MvpVoteOpen` / `ResultPosted` | `/events/{id}` |
| `PaymentDue` | `/me/payments` |
| `PaymentConfirmed` | `/me/payments` |
| `ClubJoinRequest` | `/clubs/{slug}/members` |
| `ClubInvite` | `/clubs/{slug}` |
| `NewFollower` | `/{handle}` |
| `AchievementEarned` | `/me/achievements` |

Unknown/absent payload → render non-clickable, never a dead link to `/undefined`.

Two backend spot-fixes this requires:
- club notifications must put `slug` (not only `clubId`) in `Data`, or the frontend needs
  an id→slug lookup; putting the slug in `Data` is cheaper.
- `NewFollower` must carry the follower's `handle`.

Apply the mapping in **both** the bell dropdown and `/notifications`. Clicking marks read
and navigates in one action.

---

#### Step 23 — `/admin` panel

Replaces `/moderation`. Keep the old route as a redirect so existing links survive.

**Layout.** Two-column: persistent left sidebar (vertical nav, collapses to a top
`<select>` under `md`), content on the right. This is the explicit reason for dropping
tabs — the user's complaint is that tabs stop fitting as sections are added, and a
sidebar grows without a layout rewrite.

Sections: **Обзор** (counts per queue), **Пользователи**, **Площадки**, **Жалобы**,
**Заявки**. Sidebar shows a pending-count badge per section.

**New backend** (`Features/Moderation/AdminEndpoints.cs`):
- `GET /api/admin/users?query=&role=&banned=&skip=&take=` — search by handle/display
  name, paged. Policy `Moderator`.
- `PATCH /api/admin/users/{id:guid}/role` — policy **`Admin`** only.
- Existing ban/unban stay where they are; surface them in the Users section.

**Permissions.** Moderator: view all sections, resolve reports, publish/hide venues,
resolve claims, ban/unban. Admin: all of that **plus** role changes. The UI must hide
what the viewer cannot do, and the server must enforce it independently — the hidden
button is not the check.

Every mutating action goes through `AuditLogService`, as Step 10 established.

---

#### Step 24 — Discovery and filters

The user's last point ("всё вываливает в одну кучу"). The backend already accepts
`sportId`/`cityId` for both events and clubs — **there is simply no UI**, so most of this
is frontend work over endpoints that exist.

**Shared `FilterBar` component**, state synced to URL query params (shareable/back-button
correct, and the params are already the ones the API takes).

- **Events:** sport, city, type, date range, "бесплатные / платные", "мои" (created by me
  or I am in), "мои клубы". Group the result list by day with sticky date headings instead
  of one flat column. Cards show sport emoji, time, venue, filled/capacity, price,
  and an approval badge when `RequiresApproval`.
- **Venues:** sport, city, indoor/outdoor, "рядом со мной" (already exists — fold it into
  the bar rather than leaving it as a lone button).
- **Clubs/Groups:** sport, city, kind, "мои".

Backend additions needed for the above: `type`, `dateFrom`/`dateTo`, `onlyMine`,
`onlyFree` on `GET /api/events`; `onlyMine` on `GET /api/clubs`; sport/city/indoor on
`GET /api/venues` if absent.

Empty states must say what was filtered out and offer a one-click reset — an empty list
with no explanation is what the current UI already does badly.

---

**Verification for every step:** `dotnet build`, `dotnet test` (the Step 18 CI gate now
blocks deploy on red), `pnpm --filter web run build`, `pnpm run lint`, then a live pass on
dev.game.org.az. The Step 18 postmortem is the reason for that last item: build and tests
were green while the site was down, because nothing exercised a running host.

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
уже настроены. `gameorg-dev-db` в `TARGETS` `/root/pg_backup.sh` — сделано на
Шаге 4, бэкапится вместе с остальными базами.
