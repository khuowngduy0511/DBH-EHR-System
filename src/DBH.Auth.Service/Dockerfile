# =============================================================================
# DBH.Auth.Service - Multi-stage Docker Build
# =============================================================================
# Build and run ASP.NET Core 8 application in Docker
# =============================================================================

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY DBH.EHR.System.sln ./
COPY src/DBH.Auth.Service/DBH.Auth.Service.csproj ./src/DBH.Auth.Service/
COPY src/DBH.Shared.Domain/DBH.Shared.Domain.csproj ./src/DBH.Shared.Domain/
COPY src/DBH.Shared.Contracts/DBH.Shared.Contracts.csproj ./src/DBH.Shared.Contracts/
COPY src/DBH.Shared.Infrastructure/DBH.Shared.Infrastructure.csproj ./src/DBH.Shared.Infrastructure/

# Copy all source code first so NuGet restore can find all projects
COPY src/ ./src/

# Restore dependencies for the Auth Service
RUN dotnet restore src/DBH.Auth.Service/DBH.Auth.Service.csproj

# Build and publish
RUN dotnet publish src/DBH.Auth.Service/DBH.Auth.Service.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

LABEL maintainer="DBH-EHR Team"
LABEL description="DBH Authentication Service - ASP.NET Core 8 API"

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser

# Copy published application
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5101
ENV ASPNETCORE_ENVIRONMENT=Development

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5101/health || exit 1

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 5101

# Start the application
ENTRYPOINT ["dotnet", "DBH.Auth.Service.dll"]
