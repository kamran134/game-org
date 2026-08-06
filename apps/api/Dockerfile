# Собирается из корня монорепо: docker build -f apps/api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS builder
WORKDIR /src

# Сначала только csproj — слой restore кэшируется, пока зависимости не меняются.
COPY apps/api/src/GameOrg.Domain/GameOrg.Domain.csproj apps/api/src/GameOrg.Domain/
COPY apps/api/src/GameOrg.Infrastructure/GameOrg.Infrastructure.csproj apps/api/src/GameOrg.Infrastructure/
COPY apps/api/src/GameOrg.Api/GameOrg.Api.csproj apps/api/src/GameOrg.Api/
RUN dotnet restore apps/api/src/GameOrg.Api/GameOrg.Api.csproj

COPY apps/api/src/ apps/api/src/
RUN dotnet publish apps/api/src/GameOrg.Api/GameOrg.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
WORKDIR /app

RUN addgroup -g 1001 -S dotnet && adduser -S dotnet -u 1001

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true

COPY --from=builder --chown=dotnet:dotnet /app ./

USER dotnet
EXPOSE 8080

# localhost явно на 127.0.0.1 — на образе с Next та же самая проверка на
# localhost зависала из-за резолва в ::1 раньше IPv4, здесь на всякий случай тоже.
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s \
  CMD wget -qO- http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "GameOrg.Api.dll"]
