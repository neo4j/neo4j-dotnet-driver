// Copyright (c) 2002-2020 "Neo4j,"
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
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal.MessageHandling.V3
{
    internal class HelloResponseHandler : MetadataCollectingResponseHandler
    {
        private readonly IConnection _connection;

        public HelloResponseHandler(IConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));

            AddMetadata<ServerVersionCollector, ServerVersion>();
            AddMetadata<ConnectionIdCollector, string>();
        }

        public override void OnSuccess(IDictionary<string, object> metadata)
        {
            base.OnSuccess(metadata);

            _connection.UpdateVersion(GetMetadata<ServerVersionCollector, ServerVersion>());
            _connection.UpdateId(GetMetadata<ConnectionIdCollector, string>());
        }
    }
}