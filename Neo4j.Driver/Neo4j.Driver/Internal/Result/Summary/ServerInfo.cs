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
using Neo4j.Driver.Internal.Protocol;

namespace Neo4j.Driver.Internal.Result;

internal sealed class ServerInfo : IServerInfo, IUpdateableInfo
{
    public ServerInfo(Uri uri)
    {
        Address = $"{uri.Host}:{uri.Port}";
    }

    internal BoltProtocolVersion Protocol { get; set; }

    public string ProtocolVersion => Protocol?.ToString() ?? "0.0";

    public string Agent { get; set; }

    public string Address { get; }

    public void Update(BoltProtocolVersion boltVersion, string agent)
    {
        Protocol = boltVersion;
        Agent = agent;
    }

    public override string ToString()
    {
        return $"{GetType().Name}{{{nameof(Address)}={Address}, " +
            $"{nameof(Agent)}={Agent}, " +
            $"{nameof(ProtocolVersion)}={ProtocolVersion}}}";
    }
}
