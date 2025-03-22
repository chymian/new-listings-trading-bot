#!/bin/sh
# entrypoint.sh
until pg_isready -h $POSTGRES_HOST -p $POSTGRES_PORT; do
  echo "Waiting for database...on host: $POSTGRES_HOST port: $POSTGRES_PORT name: $POSTGRES_NAME user: $POSTGRES_USER "
  sleep 2
done

dotnet ef database update
exec dotnet new-listing-bot-cs.dll
