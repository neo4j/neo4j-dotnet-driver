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
using System.Linq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling.Metadata;
using Neo4j.Driver.Internal.Protocol;

using HintsType = System.Collections.Generic.Dictionary<string, object>;

namespace Neo4j.Driver.Internal.MessageHandling.V4_3
{
    internal class HelloResponseHandler : V4_2.HelloResponseHandler
    {
        protected override BoltProtocolVersion MinVersion => BoltProtocolVersion.V4_3;
        public HelloResponseHandler(IConnection connection, BoltProtocolVersion version) : base(connection, version)
        {
			AddMetadata<ConfigurationHintsCollector, HintsType>();
            AddMetadata<BoltPatchCollector, string[]>();
        }

        public override void OnSuccess(IDictionary<string, object> metadata)
        {
            base.OnSuccess(metadata);

            if(GetMetadata<BoltPatchCollector, string[]>()?.Contains("utc") ?? false)
                _connection.SetUseUtcEncodedDateTime();
			
			var timeout = new ConfigHintRecvTimeout(GetMetadata<ConfigurationHintsCollector, HintsType>()).Get;
			_connection.SetRecvTimeOut(timeout);			
		}
    }
}
