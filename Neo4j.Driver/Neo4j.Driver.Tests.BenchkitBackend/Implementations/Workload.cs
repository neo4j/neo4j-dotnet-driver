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

namespace Neo4j.Driver.Tests.BenchkitBackend.Implementations;

public enum Method
{
    ExecuteQuery,
    SessionRun,
    ExecuteRead,
    ExecuteWrite
}

public enum Routing
{
    Write,
    Read
}

public enum Mode
{
    SequentialQueries,
    SequentialTransactions,
    SequentialSessions,
    ParallelSessions
}

public class Query
{
    public string Text { get; set; } = "";
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Describes a driver workload.
/// </summary>
public class Workload
{
    public Method Method { get; set; }
    public string Database { get; set; } = "";
    public Routing Routing { get; set; } = Routing.Write;
    public Mode Mode { get; set; } = Mode.SequentialSessions;
    public List<Query> Queries { get; set; } = new();
}
