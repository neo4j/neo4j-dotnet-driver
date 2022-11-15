// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License"):
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
using System.Linq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.MessageHandling.Metadata;
using Neo4j.Driver.Internal.Util;

namespace Neo4j.Driver.Internal.MessageHandling;

internal sealed class HelloResponseHandler : MetadataCollectingResponseHandler
{
    private readonly IConnection _connection;

    public HelloResponseHandler(IConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

        AddMetadata<ServerVersionCollector, ServerVersion>();
        AddMetadata<ConnectionIdCollector, string>();

        AddMetadata<ConfigurationHintsCollector, Dictionary<string, object>>();
        AddMetadata<BoltPatchCollector, string[]>();
    }

    public override void OnSuccess(IDictionary<string, object> metadata)
    {
        base.OnSuccess(metadata);

        UpdateConnectionServerVersion();
        UpdateId();
        UpdateUtcEncodedDateTime();
        UpdateReadTimeout();
    }

    private void UpdateUtcEncodedDateTime()
    {
        // ignore all version not 4.3/4.4
        if (_connection.Version < BoltProtocolVersion.V4_3 || _connection.Version.MajorVersion != 4)
        {
            return;
        }

        if (GetMetadata<BoltPatchCollector, string[]>()?.Contains("utc") ?? false)
        {
            _connection.SetUseUtcEncodedDateTime();
        }
    }

    private void UpdateReadTimeout()
    {
        if (_connection.Version < BoltProtocolVersion.V4_3)
        {
            return;
        }

        var configMetadata = GetMetadata<ConfigurationHintsCollector, Dictionary<string, object>>();

        if (configMetadata == null)
        {
            return;
        }

        if (!configMetadata.TryGetValue("connection.recv_timeout_seconds", out var timeout))
        {
            return;
        }

        if (timeout is int readConnectionTimeoutSeconds)
        {
            _connection.SetReadTimeoutInSeconds(readConnectionTimeoutSeconds);
        }
    }

    private void UpdateConnectionServerVersion()
    {
        _connection.UpdateVersion(GetMetadata<ServerVersionCollector, ServerVersion>());
    }

    private void UpdateId()
    {
        _connection.UpdateId(GetMetadata<ConnectionIdCollector, string>());
    }
}
