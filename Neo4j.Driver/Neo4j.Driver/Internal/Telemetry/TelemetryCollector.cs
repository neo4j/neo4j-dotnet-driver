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
    void CollectApiUsage(string apiType);
    bool BatchSizeReached { get; }
    TelemetryMessage CreateMessage();
    void Clear();
}

internal class TelemetryCollector : ITelemetryCollector
{
    private const int BatchSize = 20;

    public static readonly TelemetryCollector Default = new();
    private readonly Counter<string> _apiUsage = new();

    /// <inheritdoc />
    public void CollectApiUsage(string apiType)
    {
        _apiUsage.Increment(apiType);
    }

    /// <inheritdoc />
    public bool BatchSizeReached => _apiUsage.CounterValues.Values.Sum() >= BatchSize;

    /// <inheritdoc />
    public TelemetryMessage CreateMessage()
    {
        return BoltProtocolMessageFactory.Instance.NewTelemetryMessage(_apiUsage.CounterValues);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _apiUsage.Clear();
    }
}
