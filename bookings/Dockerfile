# Build context is the repository root (see docker-compose.yml).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY proto/ proto/
COPY shared/ shared/
COPY bookings/ bookings/
RUN dotnet restore bookings/Bookings.csproj

RUN dotnet publish bookings/Bookings.csproj -c Release -o /app --no-restore

# --- runtime -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN useradd --uid 5678 --create-home appuser
USER appuser

COPY --from=build /app .

ENV BOOKINGS_GRPC_PORT=50053
EXPOSE 50053

ENTRYPOINT ["dotnet", "Bookings.dll"]
