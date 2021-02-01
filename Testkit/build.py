"""
Executed in dotnet driver container.
Responsible for building driver and test backend.
"""
import os, subprocess


def run(args):
    subprocess.run(
        args, universal_newlines=True, stderr=subprocess.STDOUT, check=True)

if __name__ == "__main__":
    run(["dotnet", "restore", "--disable-parallel", "-v", "n", "Neo4j.Driver/Neo4j.Driver.sln"])
    run(["dotnet", "clean", "./Neo4j.Driver/Neo4j.Driver.sln"])
    run(["dotnet", "build", "./Neo4j.Driver/Neo4j.Driver.sln"])
    run(["dotnet", "publish", "./Neo4j.Driver/Neo4j.Driver.Tests.TestBackend/Neo4j.Driver.Tests.TestBackend.csproj", "--self-contained", "false", "--output", "./bin/Publish"])