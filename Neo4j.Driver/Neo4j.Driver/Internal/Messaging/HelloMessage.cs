// Copyright (c) "Neo4j"
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

using System.Collections.Generic;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.IO.MessageSerializers;

namespace Neo4j.Driver.Internal.Messaging;

internal sealed class HelloMessage : IRequestMessage
{
    public IDictionary<string, object> MetaData { get; }
    private const string UserAgentMetadataKey = "user_agent";

    public HelloMessage(BoltProtocolVersion version, string userAgent, IDictionary<string, object> authToken, 
        IDictionary<string, object> routingContext)
    {
        if (authToken == null || authToken.Count == 0)
        {
            MetaData = new Dictionary<string, object> { [UserAgentMetadataKey] = userAgent  };
        }
        else
        {
            MetaData = new Dictionary<string, object>(authToken) { [UserAgentMetadataKey] = userAgent };
        }

        // Routing added in 4.1, subsequent hellos should include it.
        if (version >= BoltProtocolVersion.V4_1)
            MetaData.Add("routing", routingContext);

        if (version >= BoltProtocolVersion.V4_3 && version < BoltProtocolVersion.V5_0)
            MetaData.Add("patch_bolt", new[] { "utc" });
    }

    public override string ToString()
    {
        var metadataCopy = new Dictionary<string, object>(MetaData);
        if (metadataCopy.ContainsKey(AuthToken.CredentialsKey))
        {
            metadataCopy[AuthToken.CredentialsKey] = "******";
        }
        return "HELLO " + metadataCopy.ToContentString();
    }

    public IPackStreamSerializer Serializer => HelloMessageSerializer.Instance;
}