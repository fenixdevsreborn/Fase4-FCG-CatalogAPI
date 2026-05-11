# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution file
COPY ["CatalogAPI.slnx", "./"]

# Copy project files
COPY ["src/CatalogAPI.Domain/CatalogAPI.Domain.csproj", "src/CatalogAPI.Domain/"]
COPY ["src/CatalogAPI.Application/CatalogAPI.Application.csproj", "src/CatalogAPI.Application/"]
COPY ["src/CatalogAPI.Infrastructure/CatalogAPI.Infrastructure.csproj", "src/CatalogAPI.Infrastructure/"]
COPY ["src/CatalogAPI.CrossCutting/CatalogAPI.CrossCutting.csproj", "src/CatalogAPI.CrossCutting/"]
COPY ["src/CatalogAPI.API/CatalogAPI.API.csproj", "src/CatalogAPI.API/"]

# Restore dependencies for source projects only (exclude tests)
RUN dotnet restore src/CatalogAPI.Domain/CatalogAPI.Domain.csproj && \
    dotnet restore src/CatalogAPI.Application/CatalogAPI.Application.csproj && \
    dotnet restore src/CatalogAPI.Infrastructure/CatalogAPI.Infrastructure.csproj && \
    dotnet restore src/CatalogAPI.CrossCutting/CatalogAPI.CrossCutting.csproj && \
    dotnet restore src/CatalogAPI.API/CatalogAPI.API.csproj

# Copy all source code
COPY . .

# Build the API project
WORKDIR "/src/src/CatalogAPI.API"
RUN dotnet build "CatalogAPI.API.csproj" -c Release -o /app/build

# Publish
RUN dotnet publish "CatalogAPI.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "CatalogAPI.API.dll"]
