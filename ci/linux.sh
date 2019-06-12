#!/usr/bin/env bash

BASE=$(dirname $0)

docker image build -t dotnet-ci -f $BASE/linux.dockerfile $BASE/..
if [[ "$?" -ne "0" ]]; then
    echo "FATAL: docker image build failed."
    exit 1
fi

docker container run --rm --env TEAMCITY_HOST="$TEAMCITY_HOST" --env TEAMCITY_USER="$TEAMCITY_USER" \
    --env TEAMCITY_PASSWORD="$TEAMCITY_PASSWORD" --env NEOCTRLARGS="$NEOCTRLARGS" \
    --env TEAMCITY_PROJECT_NAME="$TEAMCITY_PROJECT_NAME" dotnet-ci
if [[ "$?" -ne "0" ]]; then
    echo "FATAL: docker container run failed, possible test failure."
    exit 1
fi