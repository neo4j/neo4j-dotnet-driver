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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Neo4j.Driver.Internal.Messaging;

namespace Neo4j.Driver.Internal.Telemetry;

internal interface ITelemetryCollector
{
    void SetQueryApiType(QueryApiType apiType);
    bool TryCreateMessage(out TelemetryMessage message);
    void Clear();
}

internal class TelemetryCollector : ITelemetryCollector
{
    public static readonly TelemetryCollector Default = new();
    private QueryApiType? _currentApiType;

    public void SetQueryApiType(QueryApiType apiType)
    {
        _currentApiType = apiType;
    }

    public bool TryCreateMessage(out TelemetryMessage message)
    {
        if (_currentApiType is not null)
        {
            message = BoltProtocolMessageFactory.Instance.NewTelemetryMessage(_currentApiType.Value);
            return true;
        }

        message = null;
        return false;
    }

    /// <inheritdoc />
    public void Clear()
    {
        _currentApiType = null;
    }
}
