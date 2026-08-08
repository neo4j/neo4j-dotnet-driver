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
using Neo4j.Driver.TestKitBackend.Errors;
using Neo4j.Driver.TestKitBackend.Messages;

namespace Neo4j.Driver.TestKitBackend.Continuations;

internal abstract class BackgroundOperationHandler<T> : MessageHandler<T> where T : IProtocolMessage
{
    private readonly IContinuationCoordinator _coordinator;
    private readonly IResponseWriter _responseWriter;
    private readonly IDriverErrorMapper _driverErrorMapper;
    private readonly ILogger _logger;

    protected BackgroundOperationHandler(
        IContinuationCoordinator coordinator,
        IResponseWriter responseWriter,
        IDriverErrorMapper driverErrorMapper,
        ILogger logger)
    {
        _coordinator = coordinator;
        _responseWriter = responseWriter;
        _driverErrorMapper = driverErrorMapper;
        _logger = logger;
    }

    protected abstract Task<IProtocolMessage> ExecuteAsync(T message);

    public override async Task ProcessAsync(T message)
    {
        var responseTask = _coordinator.WaitForNextResponseAsync();

        _ = Task.Run(() => RunInBackgroundAsync(message));

        await _responseWriter.WriteAsync(await responseTask);
    }

    private async Task RunInBackgroundAsync(T message)
    {
        try
        {
            _coordinator.CompleteNextResponse(await ExecuteAsync(message));
        }
        catch (FrontendException exception)
        {
            _coordinator.CompleteNextResponse(new FrontendErrorResponse { Msg = exception.Message });
        }
        catch (Exception exception) when (exception is Neo4jException or TimeZoneNotFoundException)
        {
            _logger.LogDebug(exception, "Driver error during background operation");
            _coordinator.CompleteNextResponse(MapDriverError(exception));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled error during background operation");
            _coordinator.CompleteNextResponse(new BackendErrorResponse { Msg = exception.Message });
        }
    }

    private IProtocolMessage MapDriverError(Exception exception)
    {
        try
        {
            return _driverErrorMapper.Map(exception);
        }
        catch (Exception mappingFailure)
        {
            // A response must still reach the slot below, or the connection hangs to
            // testkit's own receive timeout instead of failing with this error.
            _logger.LogError(mappingFailure, "Failed to map the driver error");
            return new BackendErrorResponse { Msg = exception.Message };
        }
    }
}
