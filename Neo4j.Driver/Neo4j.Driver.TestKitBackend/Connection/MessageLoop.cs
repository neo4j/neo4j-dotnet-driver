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
            bool readSuccess;
            string? json = null;

            do
            {
                readSuccess = false;
                try
                {
                    json = await _input.ReadRequestAsync();
                    readSuccess = true;
                }
                catch (IOException)
                {
                    // This is the exception that the legacy testkit backend threw at
                    // the end of every test, and we couldn't catch it due to the design.
                    // It's caused when testkit drops the connection (maybe we failed a test)
                    // and it doesn't represent an error - it's just the completion of the test.
                    _logger.LogDebug("Connection {ConnectionId} ended by testkit", connectionId);
                }
                catch (Exception exception)
                {
                    // Log the error but don't throw since an exception in a test is sent to 
                    // testkit as a BackEndErrorResponse and we can't do that because the connection
                    // is dead. Log the error and continue reading.
                    _logger.LogError(exception, "Connection {ConnectionId} failed while reading", connectionId);
                }

                if (!readSuccess || json is null)
                {
                    continue;
                }

                _logger.LogDebug("Request: {Request}", json);
                var message = _serializer.Deserialize(json);
                handlerTasks.Add(DispatchTrackedAsync(message));
            } while (readSuccess);
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
                await Task.WhenAll(handlerTasks);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "A handler task failed while connection {ConnectionId} was closing", connectionId);
            }

            _logger.LogDebug("Closing connection {ConnectionId}", connectionId);
        }
    }

    private async Task DispatchTrackedAsync(IProtocolMessage message)
    {
        await Task.Yield();
        try
        {
            await _dispatcher.DispatchAsync(message);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("A held handler was cancelled because its connection closed");
        }
        catch (FrontendException exception)
        {
            _logger.LogDebug(exception, "Frontend error while handling request");
            await _responseWriter.WriteAsync(new FrontendErrorResponse { Msg = exception.Message });
        }
        catch (Exception exception) when (_originClassifier.OriginatesInDriver(exception))
        {
            _logger.LogDebug(exception, "Error while handling request");
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
