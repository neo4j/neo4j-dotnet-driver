// Copyright (c) "Neo4j"
// Neo4j Sweden AB [https://neo4j.com]
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

using System;

namespace Neo4j.Driver.Tests.TestBackend.Protocol.Driver;

internal class SimpleLogger : ILogger
{
    private string Now => DateTime.UtcNow.ToString("HH:mm:ss");

    public void Debug(string message, params object[] args)
    {

        Console.ForegroundColor = message[0] == '['
            ? ConsoleColor.DarkGreen
            : ConsoleColor.Green;

        Console.WriteLine($"{Now} DBG: {message}", args);
        Console.ResetColor();
    }

    public void Error(Exception error, string message, params object[] args)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{Now} ERR: {message}", args);
        Console.ResetColor();
    }

    public void Info(string message, params object[] args)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"{Now} INF: {message}", args);
        Console.ResetColor();
    }

    public bool IsDebugEnabled()
    {
        return true;
    }

    public bool IsTraceEnabled()
    {
        return true;
    }

    public void Trace(string message, params object[] args)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"{Now} TRC: {message}", args);
        Console.ResetColor();
    }

    public void Warn(Exception error, string message, params object[] args)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{Now} WRN: {message}", args);
        Console.ResetColor();
    }
}
