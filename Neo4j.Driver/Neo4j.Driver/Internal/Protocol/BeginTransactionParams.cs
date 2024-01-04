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

using System.Threading;
using Neo4j.Driver.Internal.Telemetry;

namespace Neo4j.Driver.Internal.Protocol;

internal sealed record BeginTransactionParams(
    string Database,
    Bookmarks Bookmarks,
    TransactionConfig TxConfig,
    SessionConfig SessionConfig,
    INotificationsConfig NotificationsConfig,
    TransactionInfo TransactionInfo);

internal sealed record TransactionInfo
{
    public TransactionInfo(QueryApiType apiType, bool metricsEnabled, bool awaitBegin)
    {
        ApiType = apiType;
        AwaitBegin = awaitBegin;
        _enabled = metricsEnabled;
    }

    // This is used to ensure that the transaction meta is only sent once.
    private long _interlocked;

    /// <summary>
    /// Holds if the driver enables the sending of metrics to Neo4j.
    /// </summary>
    private readonly bool _enabled;

    public QueryApiType ApiType { get; }
    public bool AwaitBegin { get; }

    /// <summary>
    /// Returns true if driver enabled and hasn't been acked yet.
    /// </summary>
    public bool TelemetryEnabled => _enabled && !Acked;

    public bool Acked => Interlocked.Read(ref _interlocked) > 0;

    public void SetAcked()
    {
        Interlocked.Increment(ref _interlocked);
    }
}
