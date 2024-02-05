# The build context for building this dockerfile needs to be {repo-root}/Neo4j.Driver

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy the Neo4j.Driver and Neo4j.Driver.Tests.BenchkitBackend source code
COPY ./common.props /src/common.props
COPY ./Neo4j.Driver /src/Neo4j.Driver
COPY ./Neo4j.Driver.Tests.BenchkitBackend /src/Neo4j.Driver.Tests.BenchkitBackend

#  build
WORKDIR /src/Neo4j.Driver.Tests.BenchkitBackend
RUN dotnet publish -c Release -o /app

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0

RUN apt update
RUN apt install wget -y
WORKDIR /app

# Copy from build stage
COPY --from=build /app .

ENTRYPOINT ["dotnet", "Neo4j.Driver.Tests.BenchkitBackend.dll"]
