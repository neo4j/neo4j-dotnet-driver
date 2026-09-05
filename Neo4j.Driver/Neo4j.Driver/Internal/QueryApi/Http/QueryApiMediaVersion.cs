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

#nullable enable

using System;

namespace Neo4j.Driver.Internal.QueryApi;

internal enum QueryApiMediaVersion
{
    V1_0,
    V1_1
}

internal static class QueryApiMediaVersionExtensions
{
    public static string ToMediaTypeString(this QueryApiMediaVersion version)
    {
        var (major, minor) = version switch
        {
            QueryApiMediaVersion.V1_0 => (1, 0),
            QueryApiMediaVersion.V1_1 => (1, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, null)
        };

        return $"application/vnd.neo4j.query.v{major}.{minor}";
    }
}
