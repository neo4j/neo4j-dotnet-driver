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

namespace Neo4j.Driver.Tests.BenchkitBackend.Types;

/// <summary>
/// Describes a driver workload.
/// </summary>
public class Workload
{
    /// <summary>
    /// The method to use for executing the workload.
    /// </summary>
    public Method Method { get; set; }

    /// <summary>
    /// The database to use for the workload.
    /// </summary>
    public string Database { get; set; } = "";

    /// <summary>
    /// The routing method to use for the workload.
    /// </summary>
    public Routing Routing { get; set; } = Routing.Write;

    /// <summary>
    /// The series/parallel mode to use for the workload.
    /// </summary>
    public Mode Mode { get; set; } = Mode.SequentialSessions;

    /// <summary>
    /// A list of individual queries to execute as part of the workload.
    /// </summary>
    public List<WorkloadQuery> Queries { get; set; } = new();
}
