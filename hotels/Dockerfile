# Build context is the repository root (see docker-compose.yml).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY proto/ proto/
COPY shared/ shared/
COPY hotels/ hotels/
RUN dotnet restore hotels/Hotels.csproj

RUN dotnet publish hotels/Hotels.csproj -c Release -o /app --no-restore

# --- runtime -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN useradd --uid 5678 --create-home appuser
USER appuser

COPY --from=build /app .

ENV HOTELS_GRPC_PORT=50052
EXPOSE 50052

ENTRYPOINT ["dotnet", "Hotels.dll"]
