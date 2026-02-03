FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["DBH.Gateway/DBH.Gateway.csproj", "DBH.Gateway/"]
RUN dotnet restore "DBH.Gateway/DBH.Gateway.csproj"
COPY . .
WORKDIR "/src/DBH.Gateway"
RUN dotnet build "DBH.Gateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DBH.Gateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "DBH.Gateway.dll"]
