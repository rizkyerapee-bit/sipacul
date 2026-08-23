FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY backend/Directory.Build.props backend/
COPY backend/src/SiPacul.Shared/SiPacul.Shared.csproj backend/src/SiPacul.Shared/
COPY backend/src/SiPacul.Domain/SiPacul.Domain.csproj backend/src/SiPacul.Domain/
COPY backend/src/SiPacul.Application/SiPacul.Application.csproj backend/src/SiPacul.Application/
COPY backend/src/SiPacul.Infrastructure/SiPacul.Infrastructure.csproj backend/src/SiPacul.Infrastructure/
COPY backend/src/SiPacul.Api/SiPacul.Api.csproj backend/src/SiPacul.Api/
RUN dotnet restore backend/src/SiPacul.Api/SiPacul.Api.csproj

FROM restore AS build
COPY backend/ backend/
RUN dotnet publish backend/src/SiPacul.Api/SiPacul.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM build AS migration
RUN dotnet tool install --tool-path /tools dotnet-ef --version 10.0.10
ENTRYPOINT ["/tools/dotnet-ef"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

USER root
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && mkdir -p /home/app/.aspnet/DataProtection-Keys \
    && chown -R $APP_UID:$APP_UID /home/app \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish ./

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

USER $APP_UID

HEALTHCHECK --interval=10s --timeout=3s --start-period=15s --retries=5 \
    CMD curl --fail --silent --show-error http://127.0.0.1:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "SiPacul.Api.dll"]
