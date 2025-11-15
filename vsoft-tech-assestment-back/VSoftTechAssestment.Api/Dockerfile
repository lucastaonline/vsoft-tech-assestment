# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file first
COPY VSoftTechAssestment.sln .

# Copy project files for dependency restoration
COPY VSoftTechAssestment.Api/*.csproj ./VSoftTechAssestment.Api/
COPY VSoftTechAssestment.Api.Tests/*.csproj ./VSoftTechAssestment.Api.Tests/

# Restore dependencies
RUN dotnet restore VSoftTechAssestment.sln

# Copy all source files
COPY VSoftTechAssestment.Api/ ./VSoftTechAssestment.Api/

# Build and publish
WORKDIR /src/VSoftTechAssestment.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for healthchecks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "VSoftTechAssestment.Api.dll"]

