"""
Executed in dotnet driver container.
Responsible for building driver and test backend.
"""
import os, subprocess


def run(args):
    subprocess.run(
        args, universal_newlines=True, stderr=subprocess.STDOUT, check=True)

if __name__ == "__main__":
    run(["dotnet", "publish", "./Neo4j.Driver/Neo4j.Driver.Tests.TestBackend/Neo4j.Driver.Tests.TestBackend.csproj", "-c", "Release", "--self-contained", "false", "--output", "./bin/Publish"])