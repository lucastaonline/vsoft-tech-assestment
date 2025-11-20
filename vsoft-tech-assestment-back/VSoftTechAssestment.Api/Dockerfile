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

# Stage 2: Runtime (com SDK temporário para migrations)
# NOTA: SDK incluído apenas para facilitar a avaliação do teste
# Em produção, use um container separado para migrations
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS runtime
WORKDIR /app

# Install curl, bash and postgresql-client for healthchecks and migrations
RUN apt-get update && apt-get install -y curl bash postgresql-client && rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=build /app/publish .

# Copy migrations and project file (needed for dotnet ef database update)
COPY --from=build /src/VSoftTechAssestment.Api/Migrations ./Migrations
COPY --from=build /src/VSoftTechAssestment.Api/VSoftTechAssestment.Api.csproj .

# Copy entrypoint script for automatic migrations
COPY VSoftTechAssestment.Api/entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["/app/entrypoint.sh", "dotnet", "VSoftTechAssestment.Api.dll"]

