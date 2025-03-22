#!/bin/bash

# Step 3: Determine Docker Compose file
compose_file="docker-compose.yml"

# Check for -dev argument
for arg in "$@"; do
    if [ "$arg" == "-dev" ]; then
        compose_file="docker-compose.development.yml"
        echo "Using development Docker Compose file..."
        break
    fi
done

#  Step 4. stop old containers
docker compose -f $compose_file down  --remove-orphans || true

# Step 5: Run Docker Compose
echo "Starting Docker Compose with $compose_file..."
docker compose -f $compose_file up -d --build --remove-orphans || true

# Check Docker Compose status
if [ $? -ne 0 ]; then
    echo "Docker Compose failed to start."
    exit 1
else
    echo "Docker Compose started successfully."
fi
