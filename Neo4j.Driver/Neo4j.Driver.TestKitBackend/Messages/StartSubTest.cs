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
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Dispatch;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record StartSubTestRequest : IProtocolMessage
{
    public string TestName { get; init; } = "";

    public Dictionary<string, ICypherValue> SubtestArguments { get; init; } = [];
}

internal class StartSubTestHandler : MessageHandler<StartSubTestRequest>
{
    private readonly ISubtestSkipPolicy _skipPolicy;
    private readonly IResponseWriter _responseWriter;
    private readonly ILogger _logger;

    public StartSubTestHandler(ISubtestSkipPolicy skipPolicy, IResponseWriter responseWriter, ILogger logger)
    {
        _skipPolicy = skipPolicy;
        _responseWriter = responseWriter;
        _logger = logger;
    }

    public override async Task ProcessAsync(StartSubTestRequest message)
    {
        if (_skipPolicy.TryGetSkipReason(message.TestName, message.SubtestArguments, out var reason))
        {
            _logger.LogDebug("Skipping subtest of '{TestName}': {Reason}", message.TestName, reason);
            await _responseWriter.WriteAsync(new SkipTestResponse(reason));
        }
        else
        {
            await _responseWriter.WriteAsync(new RunTestResponse());
        }
    }
}
