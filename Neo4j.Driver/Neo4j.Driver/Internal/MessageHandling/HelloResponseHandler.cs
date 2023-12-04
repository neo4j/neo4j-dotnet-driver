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

        var configMetadata = GetMetadata<ConfigurationHintsCollector, Dictionary<string, object>>();

        UpdateReadTimeout(configMetadata);
        UpdateTelemetryEnabled(configMetadata);
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

    private void UpdateReadTimeout(Dictionary<string, object> configMetadata)
    {
        if (configMetadata == null || _connection.Version < BoltProtocolVersion.V4_3)
        {
            return;
        }

        var timeoutFound = configMetadata.TryGetValue("connection.recv_timeout_seconds", out var timeoutObject);
        if (timeoutFound && timeoutObject is long timeoutSec)
        {
            _connection.SetReadTimeoutInSeconds((int)timeoutSec);
        }
    }

    private void UpdateTelemetryEnabled(Dictionary<string, object> configMetadata)
    {
        if (configMetadata == null || _connection.Version < BoltProtocolVersion.V5_4)
        {
            _connection.TelemetryEnabled = false;
            return;
        }

        var found = configMetadata.TryGetValue("telemetry.enabled", out var value);
        if (found && value is bool telemetryEnabled)
        {
            _connection.TelemetryEnabled = telemetryEnabled;
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
