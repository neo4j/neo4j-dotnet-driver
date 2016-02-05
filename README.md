# Neo4j .NET Driver

An alpha-level database driver for a new Neo4j remoting protocol. 

*Note*: This is in active development, the API is not stable. Please try it out and give us feedback, but expect 
things to break in the medium term!

## Minimum viable snippet

Add the driver to your project using Nuget Package Manager:

    PM> Install-Package Neo4j.Driver

Connect to a Neo4j 3.0.0+ database

    using(Driver driver = GraphDatabase.Driver( "bolt://localhost:7687" ))
    using(Session session = driver.Session())
    {
        var cursor = session.Run("CREATE (n) RETURN n");
    }

# Getting the Driver

The Neo4j Driver is distributed exclusively via Nuget and can be added to your project via the Package Manager.

## Milestones

* Coming soon

## Snapshots

Snapshot builds are available at our [MyGet feed](https://www.myget.org/feed/neo4j-driver-snapshots/package/nuget/Neo4j.Driver), add the feed to your Nuget Sources

* [https://www.myget.org/F/neo4j-driver-snapshots/api/v3/index.json](https://www.myget.org/F/neo4j-driver-snapshots/api/v3/index.json)

# Building

## Visual Studio Version

The driver is written in C# 6 so will require Visual Studio 2015 (community edition):

## Integration Tests

The integration tests will download and install a database instance on your local machine.
They can fail for two main reasons:

1. The tests aren't run as Administrator (so you'll need to run Visual Studio as administrator)
2. You have an instance of Neo4j already installed / running on your local machine.