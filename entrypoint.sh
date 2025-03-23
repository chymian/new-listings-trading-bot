#!/bin/sh
set -ex

# Wait for PostgreSQL
until pg_isready -h $POSTGRES_HOST -p $POSTGRES_PORT
do
  echo "Waiting for database connection on $POSTGRES_HOST:$POSTGRES_PORT..."
  sleep 2
done

# Apply migrations from published DLL
dotnet ef database update \
    --project ./new-listing-bot-cs.dll \
    --startup-project ./new-listing-bot-cs.dll

# Start application
exec dotnet new-listing-bot-cs.dll "$@"
j

