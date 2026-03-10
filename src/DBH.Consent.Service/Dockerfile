# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY DBH.EHR.System.sln ./
COPY src/DBH.Consent.Service/DBH.Consent.Service.csproj ./src/DBH.Consent.Service/
COPY src/DBH.Shared.Domain/DBH.Shared.Domain.csproj ./src/DBH.Shared.Domain/
COPY src/DBH.Shared.Contracts/DBH.Shared.Contracts.csproj ./src/DBH.Shared.Contracts/
COPY src/DBH.Shared.Infrastructure/DBH.Shared.Infrastructure.csproj ./src/DBH.Shared.Infrastructure/

# Copy all source code first so NuGet restore can find all projects
COPY src/ ./src/

# Restore dependencies for the Consent Service
RUN dotnet restore src/DBH.Consent.Service/DBH.Consent.Service.csproj

# Build and publish
RUN dotnet publish src/DBH.Consent.Service/DBH.Consent.Service.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5004
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5004

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:5004/health || exit 1

ENTRYPOINT ["dotnet", "DBH.Consent.Service.dll"]
