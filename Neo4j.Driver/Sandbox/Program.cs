using Neo4j.Driver;

public class Program
{
    public static async Task Main()
    {
        using var driver = GraphDatabase.Driver(
            "bolt://localhost:7687",
            AuthTokens.Basic("neo4j", "pass"),
            x => x.WithLogger(new Logger()));

        using var session = driver.AsyncSession(x => x.WithDatabase("neo4j"));
        var val = new Random();

        var cursor = await session.RunAsync(
            "CREATE (n: testNode { test: $val}) RETURN n.test",
            new { val = val.Next() });

        var value = await cursor.ToListAsync();

        Console.WriteLine(value[0].Values.First().Value);
    }
}

public class Logger : ILogger
{
    public void Error(Exception cause, string message, params object[] args)
    {
        Console.WriteLine(cause);
    }

    public void Warn(Exception cause, string message, params object[] args)
    {
        Console.WriteLine(message, args);
    }

    public void Info(string message, params object[] args)
    {
        Console.WriteLine(message, args);
    }

    public void Debug(string message, params object[] args)
    {
        Console.WriteLine(message, args);
    }

    public void Trace(string message, params object[] args)
    {
        Console.WriteLine(message, args);
    }

    public bool IsTraceEnabled()
    {
        return true;
    }

    public bool IsDebugEnabled()
    {
        return true;
    }
}
