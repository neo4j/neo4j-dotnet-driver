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

using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;

namespace Neo4j.Driver.Internal.Protocol.Utility;

internal sealed class ConnectionResourceHandler : IResultResourceHandler
{
    public ConnectionResourceHandler(IConnection conn)
    {
        _connection = conn;
    }

    private IConnection _connection;

    public Task OnResultConsumedAsync()
    {
        return Task.CompletedTask;
    }

    public override string ToString()
    {
        return $"{nameof(ConnectionResourceHandler)} {{{_connection}}}";
    }
}
