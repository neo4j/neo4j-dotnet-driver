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

namespace Neo4j.Driver.TestKitBackend.Cypher;

internal record CypherPoint(string System, double X, double Y, double? Z) : ICypherValue
{
    private static readonly Dictionary<(string System, bool Is3D), int> SystemToSrId = new()
    {
        [("cartesian", false)] = 7203,
        [("cartesian", true)] = 9157,
        [("wgs84", false)] = 4326,
        [("wgs84", true)] = 4979
    };

    private static readonly Dictionary<int, string> SrIdToSystem =
        SystemToSrId.ToDictionary(kv => kv.Value, kv => kv.Key.System);

    private bool Is3d => Z is not null;

    internal CypherPoint(Point point)
        : this(
            SrIdToSystem[point.SrId],
            point.X,
            point.Y,
            point.Dimension == Point.TwoD ? null : point.Z)
    {
    }

    internal Point ToPoint()
    {
        var srId = SystemToSrId[(System, Is3d)];
        return Is3d 
            ? new Point(srId, X, Y, Z!.Value) 
            : new Point(srId, X, Y);
    }
}
