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

using Microsoft.Extensions.Logging;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Serialization;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record GetConnectionPoolMetricsRequest : IProtocolMessage
{
    [StoredObject]
    public required IDriver Driver { get; init; }
    public required string Address { get; init; }
}

internal record ConnectionPoolMetricsResponse(int InUse, int Idle) : IProtocolMessage;

internal class GetConnectionPoolMetricsHandler : MessageHandler<GetConnectionPoolMetricsRequest>
{
    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public GetConnectionPoolMetricsHandler(IResponseWriter responseWriter, ILogger logger)
    {
        _responseWriter = responseWriter;
        _logger = logger;
    }

    public override async Task ProcessAsync(GetConnectionPoolMetricsRequest message)
    {
        var driver = (Internal.Driver)message.Driver;
        var metrics = driver.Context.Metrics.ConnectionPoolMetrics
            .Select(x => x.Value)
            .FirstOrDefault(m => m.Id.Contains(message.Address, StringComparison.OrdinalIgnoreCase));

        if (metrics is null)
        {
            throw new TestKitProtocolException($"No connection pool matches address '{message.Address}'.");
        }

        _logger.LogDebug("Fetched connection pool metrics for address '{Address}'", message.Address);

        await _responseWriter.WriteAsync(new ConnectionPoolMetricsResponse(metrics.InUse, metrics.Idle));
    }
}
