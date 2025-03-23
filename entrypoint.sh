#!/bin/sh
set -ex

# Wait for PostgreSQL to be ready
until pg_isready -h $POSTGRES_HOST -p $POSTGRES_PORT
do
  echo "Waiting for database connection on $POSTGRES_HOST:$POSTGRES_PORT..."
  sleep 2
done

# Ensure EF CLI tool is accessible
export PATH="$PATH:/root/.dotnet/tools"

# Switch to publish directory
cd /app/publish

# Apply migrations directly from the published DLL
dotnet ef database update \
    --project ./new-listing-bot-cs.dll \
    --startup-project ./new-listing-bot-cs.dll

# Start the application
exec dotnet new-listing-bot-cs.dll "$@"

