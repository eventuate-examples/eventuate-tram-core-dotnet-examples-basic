#!/usr/bin/env bash

set -e
set -x

KEEP_RUNNING=
USE_EXISTING_CONTAINERS=

while [ ! -z "$*" ] ; do
  case $1 in
    "--keep-running" )
      KEEP_RUNNING=yes
      ;;
    "--use-existing-containers" )
      USE_EXISTING_CONTAINERS=yes
      ;;
    "--help" )
      echo ./test.sh --keep-running --use-existing-containers
      exit 0
      ;;
  esac
  shift
done

if dotnet nuget list source | grep -q 'https://api.bintray.com/nuget/eventuateio-oss/eventuateio-dotnet-snapshots'; then
  echo "Package already exists"
else
  echo "Add package"
  dotnet nuget add source https://api.bintray.com/nuget/eventuateio-oss/eventuateio-dotnet-snapshots
fi

dotnet build -c release

if [ -z "$USE_EXISTING_CONTAINERS" ] ; then
  docker-compose down
fi

docker-compose up -d mssql

docker-compose run --rm wait-for-db

if [ -z "$USE_EXISTING_CONTAINERS" ] ; then
  docker-compose run --rm dbsetup
fi

docker-compose up -d zookeeper
docker-compose up -d kafka
docker-compose up -d cdcservice

# Wait for docker containers to start up
./wait-for-services.sh

# Run tests
docker-compose run --rm eventuatetramtests


# Tear down test environment

if [ -z "$KEEP_RUNNING" ] ; then
  docker-compose down -v --remove-orphans
fi
