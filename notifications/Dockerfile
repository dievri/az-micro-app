# Build context is the repository root (consistent with the other services).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY notifications/ notifications/
RUN dotnet restore notifications/Notifications.csproj

RUN dotnet publish notifications/Notifications.csproj -c Release -o /app --no-restore

# --- runtime -------------------------------------------------------------
# Console worker (no ASP.NET), so the smaller runtime image is enough.
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

RUN useradd --uid 5678 --create-home appuser
USER appuser

COPY --from=build /app .

ENTRYPOINT ["dotnet", "Notifications.dll"]
