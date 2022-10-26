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
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.MessageHandling.V5_0;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Messaging.V5_1;

namespace Neo4j.Driver.Internal.Protocol;

internal class BoltProtocolV5_1 : BoltProtocolV5_0
{
    public override BoltProtocolVersion Version => BoltProtocolVersion.V5_1;
    protected override IMessageFormat MessageFormat => BoltProtocolMessageFormat.V5_1;
    protected override IMessageFormat UtcMessageFormat => BoltProtocolMessageFormat.V5_1;

    public BoltProtocolV5_1(IDictionary<string, string> routingContext) : base(routingContext)
    {
    }

    protected override IRequestMessage HelloMessage(string userAgent, IDictionary<string, object> auth,
        IDictionary<string, string> routingContext, NotificationFilter[] notificationFilters)
    {
        return new HelloMessage(userAgent, auth, routingContext,
            NotificationFilterEncoder.EncodeNotificationFilters(notificationFilters));
    }

    protected override IRequestMessage GetBeginMessage(string database, Bookmarks bookmarks, TransactionConfig config,
        AccessMode mode, string impersonatedUser, NotificationFilter[] notificationFilters = null)
    {
        return new BeginMessage(database, bookmarks, config.Timeout, config.Metadata, mode, impersonatedUser,
            NotificationFilterEncoder.EncodeNotificationFilters(notificationFilters));
    }

    protected override IRequestMessage GetRunWithMetaDataMessage(Query query, Bookmarks bookmarks = null, 
        TransactionConfig config = null, AccessMode mode = AccessMode.Write, string database = null,
        string impersonatedUser = null, NotificationFilter[] notificationFilters = null)
    {
        return new RunWithMetadataMessage(query, database, bookmarks, config?.Timeout, config?.Metadata, 
            mode, impersonatedUser, NotificationFilterEncoder.EncodeNotificationFilters(notificationFilters));
    }

    protected override IResponseHandler GetHelloResponseHandler(IConnection conn)
    {
        return new HelloResponseHandler(conn, Version);
    }
}