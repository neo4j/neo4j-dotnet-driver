﻿// Copyright (c) "Neo4j"
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
    private string Now => DateTime.UtcNow.ToString("O");

    public void Debug(string message, params object[] args)
    {
        Console.WriteLine($"[DRIVER-DEBUG][{Now}]{message}", args);
    }

    public void Error(Exception error, string message, params object[] args)
    {
        Console.WriteLine($"[DRIVER-ERROR][{Now}]{message}", args);
    }

    public void Info(string message, params object[] args)
    {
        Console.WriteLine($"[DRIVER-INFO] [{Now}]{message}", args);
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
        Console.WriteLine($"[DRIVER-TRACE][{Now}]{message}", args);
    }

    public void Warn(Exception error, string message, params object[] args)
    {
        Console.WriteLine($"[DRIVER-WARN] [{Now}]{message}", args);
    }
}
