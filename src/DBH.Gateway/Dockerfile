# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY DBH.EHR.System.sln ./
COPY src/DBH.Gateway/DBH.Gateway.csproj ./src/DBH.Gateway/

# Copy all source code first so NuGet restore can find all projects
COPY src/ ./src/

# Restore dependencies for the Gateway
RUN dotnet restore src/DBH.Gateway/DBH.Gateway.csproj

# Build and publish
RUN dotnet publish src/DBH.Gateway/DBH.Gateway.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

LABEL maintainer="DBH-EHR Team"
LABEL description="DBH API Gateway - ASP.NET Core 8 API"

# Copy published application
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Development

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Expose port
EXPOSE 5000

# Start the application
ENTRYPOINT ["dotnet", "DBH.Gateway.dll"]
