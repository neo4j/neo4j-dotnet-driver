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

namespace Neo4j.Driver.Internal;

internal sealed class ConnectionPoolStatus
{
    public static readonly ConnectionPoolStatus Active = new(PoolStatus.Open);
    public static readonly ConnectionPoolStatus Closed = new(PoolStatus.Closed);
    public static readonly ConnectionPoolStatus Inactive = new(PoolStatus.Inactive);

    private readonly PoolStatus _code;

    private ConnectionPoolStatus(PoolStatus code)
    {
        _code = code;
    }

    public override string ToString()
    {
        return $"{nameof(ConnectionPoolStatus)} {{{_code}}}";
    }
}
