using Neo4j.Driver;
using Neo4j.Driver.Mapping;


await using var driver = GraphDatabase.Driver(
    "http://localhost:7474",
    AuthTokens.Basic("neo4j", "password"), c=>c.WithLogger(new ConsoleLogger()));

var result = await driver.GetServerInfoAsync();
Console.WriteLine(result.Address);
Console.WriteLine(result.Agent);
Console.WriteLine(result.ProtocolVersion);

var result2 = await driver.ExecutableQuery("RETURN 1 AS n").ExecuteAsync().AsObjectsFromBlueprintAsync(new {n = 0});
Console.WriteLine(result2.Single().n);

class ConsoleLogger : INeo4jLogger
{
    public void Debug(string message, params object[] args)
    {
        Console.WriteLine("DBG " + message, args);
    }

    public void Error(Exception cause, string message, params object[] args)
    {
        Console.WriteLine("ERR " + message, args);
    }

    public void Info(string message, params object[] args)
    {
        Console.WriteLine("INF " + message, args);
    }

    public void Warn(Exception cause, string message, params object[] args)
    {
        Console.WriteLine("WRN " + message, args);
    }
    
    public bool IsDebugEnabled() => true;
    public bool IsTraceEnabled() => true;
    public void Trace(string message, params object[] args) => Debug(message, args);
}
