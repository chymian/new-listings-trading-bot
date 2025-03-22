# Base stage using the runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

# Build stage to restore dependencies and build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["new-listing-bot-cs.csproj", "./"]
COPY . .

WORKDIR "/src/."
RUN dotnet restore "./new-listing-bot-cs.csproj"
RUN dotnet build "./new-listing-bot-cs.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage to prepare the app for production (optional for production, not needed for hot reload)
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./new-listing-bot-cs.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage to configure the runtime environment for production
FROM base AS final
WORKDIR /app
RUN apt-get update && apt-get install -y postgresql-client --no-install-recommends && rm -rf /var/lib/apt/lists/*
COPY --from=publish /app/publish .
COPY entrypoint.sh .
RUN chmod +x entrypoint.sh

# Default entry point for production
ENTRYPOINT ["./entrypoint.sh"]

# Development stage to use dotnet watch for hot reload
FROM build AS dev
WORKDIR /app
# Make sure the app source code changes can be monitored
COPY . .
# Use dotnet watch for development with hot reload
ENTRYPOINT ["dotnet", "watch", "run", "--no-launch-profile", "--urls", "http://+:5000;https://+:5001"]
