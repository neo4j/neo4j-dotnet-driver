// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling.Metadata;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal.MessageHandling;

internal class HelloResponseHandlerV51 : MetadataCollectingResponseHandler
{
    private readonly IConnection _connection;

    public HelloResponseHandlerV51(IConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        AddMetadata<ServerVersionCollector, ServerVersion>();
        AddMetadata<ConnectionIdCollector, string>();
        AddMetadata<ConfigurationHintsCollector, Dictionary<string, object>>();
    }

    public override void OnSuccess(IDictionary<string, object> metadata)
    {
        base.OnSuccess(metadata);

        _connection.UpdateVersion(GetMetadata<ServerVersionCollector, ServerVersion>());
        _connection.UpdateId(GetMetadata<ConnectionIdCollector, string>());
        var configMetadata = GetMetadata<ConfigurationHintsCollector, Dictionary<string, object>>();
        if (configMetadata == null)
        {
            return;
        }
        var timeoutFound = configMetadata.TryGetValue("connection.recv_timeout_seconds", out var timeoutObject);
        if (timeoutFound && timeoutObject is long timeoutSec)
        {
            _connection.SetReadTimeoutInSeconds((int)timeoutSec);
        }
    }
}
