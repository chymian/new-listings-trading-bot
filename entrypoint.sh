#!/bin/sh
# entrypoint.sh
until pg_isready -h $DB_HOST -p $DB_PORT; do
  echo "Waiting for database..."
  sleep 2
done

dotnet ef database update
exec dotnet new-listing-bot-cs.dll
