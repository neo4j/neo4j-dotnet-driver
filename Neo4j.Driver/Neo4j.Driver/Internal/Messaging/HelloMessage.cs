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
using System.Collections.Generic;
using Neo4j.Driver.Internal.Auth;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.IO.MessageSerializers;
using Neo4j.Driver.Internal.Messaging.Utils;
using Neo4j.Driver.Internal.Protocol;

namespace Neo4j.Driver.Internal.Messaging;

internal sealed class HelloMessage : IRequestMessage
{
    private const string UserAgentMetadataKey = "user_agent";
    private const string RoutingMetadataKey = "routing";
    private const string BoltAgentMetadataKey = "bolt_agent";
    private const string PatchBoltMetadataKey = "patch_bolt";

    public HelloMessage(
        BoltProtocolVersion version,
        string userAgent,
        IDictionary<string, object> authToken,
        IDictionary<string, string> routingContext)
    {
        if (version >= BoltProtocolVersion.V5_1)
        {
            throw new ArgumentOutOfRangeException(nameof(version), version, "Should be Bolt version 5.0 or less");
        }

        if (authToken?.Count > 0)
        {
            Metadata = new Dictionary<string, object>(authToken)
            {
                [UserAgentMetadataKey] = userAgent
            };
        }
        else
        {
            Metadata = new Dictionary<string, object> { [UserAgentMetadataKey] = userAgent };
        }

        if (version >= BoltProtocolVersion.V4_1)
        {
            Metadata.Add(RoutingMetadataKey, routingContext);
        }

        if (version >= BoltProtocolVersion.V4_3 && version < BoltProtocolVersion.V5_0)
        {
            Metadata.Add(PatchBoltMetadataKey, new[] { "utc" });
        }
    }

    public HelloMessage(
        BoltProtocolVersion version,
        string userAgent,
        IDictionary<string, string> routingContext,
        INotificationsConfig notificationsConfig)
    {
        if (version < BoltProtocolVersion.V5_1)
        {
            throw new ArgumentOutOfRangeException(nameof(version), version, "should be Bolt version 5.1+");
        }

        Metadata = new Dictionary<string, object>(3)
        {
            [UserAgentMetadataKey] = userAgent
        };

        Metadata[RoutingMetadataKey] = routingContext;

        if (version >= BoltProtocolVersion.V5_2)
        {
            NotificationsMetadataWriter.AddNotificationsConfigToMetadata(Metadata, notificationsConfig);
        }

        if (version >= BoltProtocolVersion.V5_3)
        {
            Metadata.Add(BoltAgentMetadataKey, BoltAgentBuilder.Agent);
        }
    }

    public IDictionary<string, object> Metadata { get; }

    public IPackStreamSerializer Serializer => HelloMessageSerializer.Instance;

    public override string ToString()
    {
        var metadataCopy = new Dictionary<string, object>(Metadata);

        if (metadataCopy.ContainsKey(AuthToken.CredentialsKey))
        {
            metadataCopy[AuthToken.CredentialsKey] = "******";
        }

        return $"HELLO {metadataCopy.ToContentString()}";
    }
}
