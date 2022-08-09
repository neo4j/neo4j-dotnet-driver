// Copyright (c) 2002-2022 "Neo4j,"
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
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.MessageHandling.V5_0;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Messaging.V5_0;

namespace Neo4j.Driver.Internal.Protocol
{
    internal class BoltProtocolV5_0 : BoltProtocolV4_4
    {
        public override BoltProtocolVersion Version => BoltProtocolVersion.V5_0;
        protected override IMessageFormat MessageFormat => BoltProtocolMessageFormat.V5_0;
        protected override IMessageFormat UtcMessageFormat => BoltProtocolMessageFormat.V5_0;

        public BoltProtocolV5_0(IDictionary<string, string> routingContext) : base(routingContext)
        {
        }

        protected override IRequestMessage HelloMessage(string userAgent,
            IDictionary<string, object> auth,
            IDictionary<string, string> routingContext)
        {
            return new HelloMessage(userAgent, auth, routingContext);
        }

        protected override IResponseHandler GetHelloResponseHandler(IConnection conn)
        {
            return new HelloResponseHandler(conn, Version);
        }
    }
}