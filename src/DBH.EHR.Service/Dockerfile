# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY src/DBH.EHR.Service/DBH.EHR.Service.csproj ./DBH.EHR.Service/
RUN dotnet restore DBH.EHR.Service/DBH.EHR.Service.csproj

# Copy source and build
COPY src/DBH.EHR.Service/ ./DBH.EHR.Service/
WORKDIR /src/DBH.EHR.Service
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "DBH.EHR.Service.dll"]
