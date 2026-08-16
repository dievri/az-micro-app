# Build context is the repository root (see docker-compose.yml).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore against the project + its dependencies (deterministic).
COPY proto/ proto/
COPY shared/ shared/
COPY users/ users/
RUN dotnet restore users/Users.csproj

RUN dotnet publish users/Users.csproj -c Release -o /app --no-restore

# --- runtime -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Run as a non-root user.
RUN useradd --uid 5678 --create-home appuser
USER appuser

COPY --from=build /app .

ENV USERS_GRPC_PORT=50051
EXPOSE 50051

ENTRYPOINT ["dotnet", "Users.dll"]
