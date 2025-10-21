# Use the official .NET runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG GITHUB_TOKEN
WORKDIR /app

# Configure GitHub Packages authentication if token is provided
RUN if [ ! -z "$GITHUB_TOKEN" ]; then \
    dotnet nuget add source --username docker --password $GITHUB_TOKEN \
    --store-password-in-clear-text \
    --name github "https://nuget.pkg.github.com/hrs-org/index.json"; \
    fi

# Copy solution file first
COPY ["HikingRentalStore.sln", "./"]

# Copy csproj files and restore dependencies
COPY ["HRS.API/HRS.API.csproj", "HRS.API/"]
COPY ["HRS.Domain/HRS.Domain.csproj", "HRS.Domain/"]
COPY ["HRS.Infrastructure/HRS.Infrastructure.csproj", "HRS.Infrastructure/"]
COPY ["HRS.Migrations/HRS.Migrations.csproj", "HRS.Migrations/"]
COPY ["HRS.Test/HRS.Test.csproj", "HRS.Test/"]
RUN dotnet restore "HikingRentalStore.sln"

# Copy the rest of the source code (selective copying for security)
COPY ["HRS.API/", "HRS.API/"]
COPY ["HRS.Domain/", "HRS.Domain/"]
COPY ["HRS.Infrastructure/", "HRS.Infrastructure/"]
COPY ["HRS.Migrations/", "HRS.Migrations/"]
COPY ["HRS.Test/", "HRS.Test/"]
RUN dotnet build "HikingRentalStore.sln" -c "$BUILD_CONFIGURATION" --no-restore \
    -p:TreatWarningsAsErrors=false

# Publish the app
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "HRS.API/HRS.API.csproj" -c "$BUILD_CONFIGURATION" \
    -o /app/publish /p:UseAppHost=false --no-restore --no-build

# Final stage - runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create a non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Set environment variable to listen on port 8080 (non-privileged port)
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "HRS.API.dll"]
