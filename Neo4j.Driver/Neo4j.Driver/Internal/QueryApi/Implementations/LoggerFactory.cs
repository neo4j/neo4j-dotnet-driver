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
using Neo4j.Driver.Internal.QueryApi.Abstractions;

namespace Neo4j.Driver.Internal.QueryApi.Implementations;

internal class LoggerFactory : ILoggerFactory
{
    private readonly INeo4jLogger _neo4JLogger;

    public LoggerFactory(INeo4jLogger neo4JLogger)
    {
        _neo4JLogger = neo4JLogger;
    }

    public ILogger GetLoggerForType(Type type, ILoggingContextTracker tracker)
    {
        var legacyAdapter = new LegacyLoggerAdapter(_neo4JLogger, type);
        return new ContextualLogger(tracker, legacyAdapter);
    }
}
