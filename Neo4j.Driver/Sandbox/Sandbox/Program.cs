using System.Runtime.InteropServices.ComTypes;
using Neo4j.Driver;
using Neo4j.Driver.Experimental;
using GraphDatabase = Neo4j.Driver.GraphDatabase;

const string cypher = """
                MATCH (movie:Movie)<-[relationship:ACTED_IN|DIRECTED]-(person:Person)
                WHERE movie.title =~ '(The|A) .*'
                RETURN movie, relationship, person
                """;

var driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "password"));

var eagerResult = await driver
    .ExecutableQuery(cypher)
    .ExecuteAsync();

var (records, summary) = eagerResult;

foreach (var record in eagerResult)
{
    Console.WriteLine(record["person"].As<INode>()["name"].As<string>());
}

// ******************
Console.WriteLine("***");


var results = await driver
    .ExecutableQuery("MATCH (p:Person WHERE p.name = $name) RETURN p.born AS born")
    .WithParameters(new Dictionary<string, object> { ["name"] = "Tom Hanks" })
    .ExecuteAsync();

var tomBorn = results[0]["born"].As<long>();
Console.WriteLine($"Tom Hanks born: {tomBorn}");

var names = await driver
    .ExecutableQuery("MATCH (p:Person) RETURN p AS person")
    .WithTransformation(r => r["person"].As<INode>()["name"].As<string>())
    .WithTransformation(s => s.ToUpper())
    .WithTransformation(s => s.ToLower())
    .ExecuteAsync();

foreach (var name in names)
{
    Console.WriteLine(name);
}

Console.ReadLine();
