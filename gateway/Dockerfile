# Build context is the repository root (see docker-compose.yml).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY proto/ proto/
COPY shared/ shared/
COPY gateway/ gateway/
RUN dotnet restore gateway/Gateway.csproj

RUN dotnet publish gateway/Gateway.csproj -c Release -o /app --no-restore

# --- runtime -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is used by the compose HTTP health check against /health.
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

RUN useradd --uid 5678 --create-home appuser
USER appuser

COPY --from=build /app .

ENV GATEWAY_HTTP_PORT=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Gateway.dll"]
