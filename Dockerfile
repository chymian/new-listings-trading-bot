# Base stage using the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Build stage to restore dependencies and build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files first for better layer caching
COPY ["new-listing-bot-cs.csproj", "./"]
RUN dotnet restore "./new-listing-bot-cs.csproj"

# Copy remaining source code
COPY . .
RUN dotnet build "./new-listing-bot-cs.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Install EF Core tools in build stage
RUN dotnet tool install --global dotnet-ef --version 8.0.0

# Publish stage with migration inclusion
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./new-listing-bot-cs.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:IncludeMigrations=true \
    /p:EnableCompressionInSingleFile=true

# Final production stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install postgres client only
RUN apt-get update && \
    apt-get install -y postgresql-client --no-install-recommends && \
    rm -rf /var/lib/apt/lists/*

# Copy published output and entrypoint
COPY --from=publish /app/publish .
COPY entrypoint.sh .
RUN chmod +x entrypoint.sh

# Ensure dotnet tools from build stage are in PATH
ENV PATH="${PATH}:/root/.dotnet/tools"

# Default entry point for production
ENTRYPOINT ["./entrypoint.sh"]

# Development stage
FROM build AS dev
WORKDIR /app
COPY . .
ENTRYPOINT ["dotnet", "watch", "run", "--no-launch-profile", "--urls", "http://+:5000;https://+:5001"]

