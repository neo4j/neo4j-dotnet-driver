﻿// Copyright (c) 2002-2022 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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

namespace Neo4j.Driver.Tests.TestBackend;

internal class SimpleLogger : ILogger
{
    public void Debug(string message, params Object[] args)
    {
        Console.WriteLine("[DRIVER-DEBUG]" + message, args);
    }
    public void Error(System.Exception error, string message, params Object[] args)
    {
        Console.WriteLine("[DRIVER-ERROR]" + message, args);
    }
    public void Info(string message, params Object[] args)
    {
        Console.WriteLine("[DRIVER-INFO]" + message, args);
    }
    public bool IsDebugEnabled()
    {
        return true;
    }
    public bool IsTraceEnabled()
    {
        return true;
    }
    public void Trace(string message, params Object[] args)
    {
        Console.WriteLine("[DRIVER-TRACE]" + message, args);
    }
    public void Warn(System.Exception error, string message, params Object[] args)
    {
        Console.WriteLine("[DRIVER-WARN]" + message, args);
    }
}