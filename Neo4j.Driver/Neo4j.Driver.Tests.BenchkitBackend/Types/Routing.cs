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
/// Defines the routing mode in which the queries should be executed.
/// </summary>
public enum Routing
{
    /// <summary>
    /// Write routing.
    /// </summary>
    Write,

    /// <summary>
    /// Read routing.
    /// </summary>
    Read
}

internal static class RoutingExtensions
{
    public static AccessMode ToAccessMode(this Routing routing)
    {
        return routing switch
        {
            Routing.Write => AccessMode.Write,
            Routing.Read => AccessMode.Read,
            _ => throw new ArgumentOutOfRangeException(nameof(routing), routing, null)
        };
    }

    public static RoutingControl ToRoutingControl(this Routing routing)
    {
        return routing switch
        {
            Routing.Write => RoutingControl.Writers,
            Routing.Read => RoutingControl.Readers,
            _ => throw new ArgumentOutOfRangeException(nameof(routing), routing, null)
        };
    }
}
