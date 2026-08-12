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
using Neo4j.Driver.TestKitBackend.Logging;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.ObjectStorage;

namespace Neo4j.Driver.TestKitBackend.Messages;

internal record StartTestRequest : IProtocolMessage
{
    public string TestName { get; init; } = "";
}

internal record RunTestResponse : IProtocolMessage;

internal record SkipTestResponse(string Reason) : IProtocolMessage;

internal interface ISkipPolicy
{
    bool TryGetSkipReason(string testName, out string reason);
}

internal class StartTestHandler : MessageHandler<StartTestRequest>
{
    private readonly ILoggingContext _loggingContext;
    private readonly ISkipPolicy _skipPolicy;
    private readonly LoggingDisposableCreator _createLoggingDisposable;
    private readonly IResponseWriter _responseWriter;
    private readonly IObjectStore _objectStore;
    private readonly ILogger _logger;

    public StartTestHandler(
        ILoggingContext loggingContext,
        ISkipPolicy skipPolicy,
        LoggingDisposableCreator createLoggingDisposable,
        IResponseWriter responseWriter,
        IObjectStore objectStore,
        ILogger logger)
    {
        _loggingContext = loggingContext;
        _skipPolicy = skipPolicy;
        _createLoggingDisposable = createLoggingDisposable;
        _responseWriter = responseWriter;
        _objectStore = objectStore;
        _logger = logger;
    }

    public override async Task ProcessAsync(StartTestRequest message)
    {
        _loggingContext.Set("test", message.TestName);

        IProtocolMessage response;
        if (_skipPolicy.TryGetSkipReason(message.TestName, out var reason))
        {
            _logger.LogDebug("Skipping test '{TestName}': {Reason}", message.TestName, reason);
            response = new SkipTestResponse(reason);
            await _responseWriter.WriteAsync(response);
        }
        else
        {
            _logger.LogDebug("Accepting test '{TestName}'", message.TestName);
            response = new RunTestResponse();
            await _responseWriter.WriteAsync(response);

            _logger.LogDebug("START TEST {TestName}", message.TestName);

            var endTestMsg = $"END TEST {message.TestName}";
            var endTestLogger = _createLoggingDisposable("Test closedown", endTestMsg);
            _objectStore.Store(endTestLogger);
        }
    }
}
