# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY DBH.EHR.System.sln ./
COPY src/DBH.Blockchain.Service/DBH.Blockchain.Service.csproj ./src/DBH.Blockchain.Service/
COPY src/DBH.Shared.Domain/DBH.Shared.Domain.csproj ./src/DBH.Shared.Domain/
COPY src/DBH.Shared.Contracts/DBH.Shared.Contracts.csproj ./src/DBH.Shared.Contracts/
COPY src/DBH.Shared.Infrastructure/DBH.Shared.Infrastructure.csproj ./src/DBH.Shared.Infrastructure/

# Copy source
COPY src/ ./src/

# Restore and publish
RUN dotnet restore src/DBH.Blockchain.Service/DBH.Blockchain.Service.csproj
RUN dotnet publish src/DBH.Blockchain.Service/DBH.Blockchain.Service.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5090
ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 5090

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:5090/health || exit 1

ENTRYPOINT ["dotnet", "DBH.Blockchain.Service.dll"]
