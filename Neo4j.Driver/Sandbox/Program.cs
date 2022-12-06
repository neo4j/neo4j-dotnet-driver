// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Neo4j.Driver;

public class Program
{
    public static async Task Main()
    {
        using var driver = GraphDatabase.Driver(
            "bolt://127.0.0.1:7687",
            AuthTokens.Basic("neo4j", "pass"),
            x => x.WithLogger(new Logger()));

        using var session = driver.AsyncSession(x => x.WithDatabase("neo4j"));
        var val = new Random();

        var cursor = await session.RunAsync(
            "CREATE (n: testNode { test: $val}) RETURN n.test",
            new { val = val.Next() });

        var value = await cursor.ConsumeAsync();
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
