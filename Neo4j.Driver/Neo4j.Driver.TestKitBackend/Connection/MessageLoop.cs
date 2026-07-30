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
using Neo4j.Driver.TestKitBackend.Cypher;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Errors;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.ObjectRegistry;
using Neo4j.Driver.TestKitBackend.Serialization;

namespace Neo4j.Driver.TestKitBackend.Connection;

internal class MessageLoop : IMessageLoop
{
    private readonly IConnectionInput _input;
    private readonly IMessageSerializer _serializer;
    private readonly IMessageDispatcher _dispatcher;
    private readonly IResponseWriter _responseWriter;
    private readonly IRegistry _registry;
    private readonly IExceptionTypeMapper _exceptionTypeMapper;
    private readonly INativeToCypherMapper _cypherMapper;
    private readonly ILogger _logger;

    public MessageLoop(
        IConnectionInput input,
        IMessageSerializer serializer,
        IMessageDispatcher dispatcher,
        IResponseWriter responseWriter,
        IRegistry registry,
        IExceptionTypeMapper exceptionTypeMapper,
        INativeToCypherMapper cypherMapper,
        ILogger logger)
    {
        _input = input;
        _serializer = serializer;
        _dispatcher = dispatcher;
        _responseWriter = responseWriter;
        _registry = registry;
        _exceptionTypeMapper = exceptionTypeMapper;
        _cypherMapper = cypherMapper;
        _logger = logger;
    }

    public async Task RunAsync(string connectionId)
    {
        try
        {
            string? json;
            while ((json = await _input.ReadRequestAsync()) is not null)
            {
                _logger.LogDebug("Request: {Request}", json);
                var message = _serializer.Deserialize(json);
                await DispatchWithErrorHandling(message);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Connection {ConnectionId} failed", connectionId);
            await _responseWriter.WriteAsync(new BackendErrorResponse { Msg = exception.Message });
        }
        finally
        {
            _logger.LogDebug("Closing connection {ConnectionId}", connectionId);
        }
    }

    private async Task DispatchWithErrorHandling(IProtocolMessage message)
    {
        try
        {
            await _dispatcher.DispatchAsync(message);
        }
        catch (Neo4jException exception)
        {
            _logger.LogDebug(exception, "Driver error while handling request");
            var registered = _registry.Register(exception);
            await _responseWriter.WriteAsync(
                new DriverErrorResponse
                {
                    Id = registered.Id,
                    ErrorType = _exceptionTypeMapper.Map(exception),
                    Msg = exception.Message,
                    Code = exception.Code,
                    Retryable = exception.IsRetriable,
                    GqlStatus = exception.GqlStatus,
                    StatusDescription = exception.GqlStatusDescription,
                    Classification = exception.GqlClassification,
                    RawClassification = exception.GqlRawClassification,
                    DiagnosticRecord = exception.GqlDiagnosticRecord?
                        .ToDictionary(kv => kv.Key, kv => _cypherMapper.Map(kv.Value))
                });
        }
    }
}
