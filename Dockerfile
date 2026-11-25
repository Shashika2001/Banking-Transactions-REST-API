# syntax=docker/dockerfile:1

# ---- Build stage: restore, compile and publish with the full SDK ------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy only the files restore needs first, so the package layer is cached until a project file changes.
COPY Directory.Build.props .editorconfig ./
COPY src/BankingApi.Domain/BankingApi.Domain.csproj src/BankingApi.Domain/
COPY src/BankingApi.Application/BankingApi.Application.csproj src/BankingApi.Application/
COPY src/BankingApi.Infrastructure/BankingApi.Infrastructure.csproj src/BankingApi.Infrastructure/
COPY src/BankingApi.Api/BankingApi.Api.csproj src/BankingApi.Api/
RUN dotnet restore src/BankingApi.Api/BankingApi.Api.csproj

COPY src/ src/
RUN dotnet publish src/BankingApi.Api/BankingApi.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---- Runtime stage: ASP.NET Core runtime only, running as a non-root user ---------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

# The .NET 8 images ship with a non-root "app" user; APP_UID is its numeric ID.
USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "BankingApi.Api.dll"]
