# Neo4j .NET Driver
This is the source code of the driver under development. To get the lateset statble released driver, checkout [Nuget](https://www.nuget.org/packages/Neo4j.Driver/). To find changelogs, rich examples of how to use the driver, and API documents of the driver, checkout [wiki](https://github.com/neo4j/neo4j-dotnet-driver/wiki). 

## Minimum viable snippet

Add the driver to your project using Nuget Package Manager:

    PM> Install-Package Neo4j.Driver -Pre

Connect to a Neo4j 3.1.0+ database

    using(var driver = GraphDatabase.Driver( "bolt://localhost:7687" ))
    using(var session = driver.Session())
    {
        var result = session.Run("CREATE (n) RETURN n");
    }

# Getting the Driver

The Neo4j Driver is distributed exclusively via Nuget and can be added to your project via the Package Manager.

## Milestones

Available on [Nuget](https://www.nuget.org/packages/Neo4j.Driver)

## Snapshots

Snapshot builds are available at our [MyGet feed](https://www.myget.org/feed/neo4j-driver-snapshots/package/nuget/Neo4j.Driver), add the feed to your Nuget Sources

* [https://www.myget.org/F/neo4j-driver-snapshots/api/v3/index.json](https://www.myget.org/F/neo4j-driver-snapshots/api/v3/index.json)

# Building the source code

## Visual Studio Version

The driver is written in C# 6 so will require Visual Studio 2015 (community edition).

## Integration Tests

The integration tests will use neokit Python scripts to download and install a database instance on your local machine.
They can fail for three main reasons:

1. Python.exe is not installed and added in the system PATH variable
2. The tests aren't run as Administrator (so you'll need to run Visual Studio as administrator)
3. You have an instance of Neo4j already installed / running on your local machine.

## Run tests
The simplest way to run all tests from command line is to run `runTests.ps1` powershell script:

	.\Neo4j.Driver\runTests.ps1
