#!/usr/bin/env bash

set -e
set -x

if dotnet nuget list source | grep -q 'https://api.bintray.com/nuget/eventuateio-oss/eventuateio-dotnet-snapshots'; then
  echo "Package already exists"
else
  echo "Add package" 
  dotnet nuget add source https://api.bintray.com/nuget/eventuateio-oss/eventuateio-dotnet-snapshots
fi

dotnet build -c release

docker-compose down
docker-compose up -d mssql

docker-compose run --rm wait-for-db

docker-compose run --rm dbsetup
docker-compose up -d zookeeper
docker-compose up -d kafka
docker-compose up -d cdcservice

# Wait for docker containers to start up
sleep 60s

# Print docker status
docker ps
docker stats --no-stream --all

# Run tests
docker-compose run --rm eventuatetramtests

# Tear down test environment
docker-compose down -v --remove-orphans
