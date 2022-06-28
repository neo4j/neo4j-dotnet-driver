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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling.Metadata;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal.MessageHandling.V4
{
    internal class HelloResponseHandler : V3.HelloResponseHandler
    {
        protected virtual BoltProtocolVersion MinVersion => BoltProtocolVersion.V4_0;
        private BoltProtocolVersion _version;

        protected BoltProtocolVersion Version
        {
            get => _version;
            set
            {
                _version = value ?? throw new ArgumentNullException($"Attempting to create a HelloResponseHandler v{MinVersion} with a null BoltProtocolVersion object");
                if (Version < MinVersion)
                    throw new ArgumentOutOfRangeException($"Attempting to initialise a v{MinVersion} HelloResponseHandler with a protocol version less than {MinVersion}(v{Version} passed)");
            }
        }

        public HelloResponseHandler(IConnection connection, BoltProtocolVersion version) : base(connection)
        {
            Version = version;
        }
    }
}