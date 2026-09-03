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
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Errors;
using Neo4j.Driver.TestKitBackend.Expectations;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.Serialization;

namespace Neo4j.Driver.TestKitBackend.Connection;

internal class MessageLoop : IMessageLoop
{
    private readonly IConnectionInput _input;
    private readonly IMessageSerializer _serializer;
    private readonly IMessageDispatcher _dispatcher;
    private readonly IResponseWriter _responseWriter;
    private readonly IDriverErrorMapper _driverErrorMapper;
    private readonly IExceptionOriginClassifier _originClassifier;
    private readonly IExpectationStore _expectationStore;
    private readonly ILogger _logger;

    internal TimeSpan HandlerDrainTimeout { get; set; } = TimeSpan.FromSeconds(10);

    public MessageLoop(
        IConnectionInput input,
        IMessageSerializer serializer,
        IMessageDispatcher dispatcher,
        IResponseWriter responseWriter,
        IDriverErrorMapper driverErrorMapper,
        IExceptionOriginClassifier originClassifier,
        IExpectationStore expectationStore,
        ILogger logger)
    {
        _input = input;
        _serializer = serializer;
        _dispatcher = dispatcher;
        _responseWriter = responseWriter;
        _driverErrorMapper = driverErrorMapper;
        _originClassifier = originClassifier;
        _expectationStore = expectationStore;
        _logger = logger;
    }

    public async Task RunAsync(string connectionId)
    {
        var handlerTasks = new List<Task>();
        try
        {
            while (true)
            {
                string? json;
                try
                {
                    json = await _input.ReadRequestAsync();
                }
                catch (IOException)
                {
                    _logger.LogDebug("Connection {ConnectionId} ended by testkit", connectionId);
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Connection {ConnectionId} failed while reading", connectionId);
                    break;
                }

                if (json is null)
                {
                    _logger.LogDebug("Connection {ConnectionId} ended by testkit (EOF)", connectionId);
                    break;
                }

                _logger.LogDebug("Request: {Request}", json);
                handlerTasks.Add(DispatchTrackedAsync(json));
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Connection {ConnectionId} failed", connectionId);
            await _responseWriter.WriteAsync(new BackendErrorResponse { Msg = exception.Message });
        }
        finally
        {
            _expectationStore.CancelAll();
            try
            {
                await Task.WhenAll(handlerTasks).WaitAsync(HandlerDrainTimeout);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning(
                    "Handlers were still running {Timeout} after connection {ConnectionId} closed; abandoning them",
                    HandlerDrainTimeout,
                    connectionId);
            }
            catch (Exception exception)
            {
                _logger.LogDebug(
                    "A handler task failed while connection {ConnectionId} was closing: {ExceptionType}",
                    connectionId,
                    exception.GetType().Name);
            }

            _logger.LogDebug("Closing connection {ConnectionId}", connectionId);
        }
    }

    private async Task DispatchTrackedAsync(string json)
    {
        await Task.Yield();
        try
        {
            var message = _serializer.Deserialize(json);
            await _dispatcher.DispatchAsync(message);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("A held handler was cancelled because its connection closed");
        }
        catch (FrontendException exception)
        {
            _logger.LogDebug("Frontend error while handling request: {Message}", exception.Message);
            await _responseWriter.WriteAsync(new FrontendErrorResponse { Msg = exception.Message });
        }
        catch (Exception exception) when (_originClassifier.OriginatesInDriver(exception))
        {
            _logger.LogDebug("Driver error while handling request: {ExceptionType}", exception.GetType().Name);
            var response = _driverErrorMapper.Map(exception);
            await _responseWriter.WriteAsync(response);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled error while handling request");
            await _responseWriter.WriteAsync(new BackendErrorResponse { Msg = exception.Message });
        }
    }
}
