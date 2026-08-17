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

using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Neo4j.Driver.TestKitBackend.Connection;
using Neo4j.Driver.TestKitBackend.Dispatch;
using Neo4j.Driver.TestKitBackend.Errors;
using Neo4j.Driver.TestKitBackend.Expectations;
using Neo4j.Driver.TestKitBackend.Messages;
using Neo4j.Driver.TestKitBackend.Serialization;
using Xunit;

namespace Neo4j.Driver.TestKitBackend.Tests.Connection;

public class MessageLoopTests
{
    private readonly AutoMocker _autoMocker = AutoMocker.ForTesting<MessageLoop>();

    [Fact]
    public async Task Dispatches_each_request_read_from_the_connection()
    {
        const string json = """{"name":"GetFeatures","data":{}}""";
        var message = Mock.Of<IProtocolMessage>();

        _autoMocker.GetMock<IConnectionInput>()
            .SetupSequence(i => i.ReadRequestAsync())
            .ReturnsAsync(json)
            .ReturnsAsync((string?)null);

        _autoMocker.GetMock<IMessageSerializer>()
            .Setup(s => s.Deserialize(json))
            .Returns(message);

        var loop = _autoMocker.CreateInstance<MessageLoop>();

        await loop.RunAsync("testkit-1");

        _autoMocker.GetMock<IMessageDispatcher>().Verify(d => d.DispatchAsync(message), Times.Once);

        _autoMocker.GetMock<IResponseWriter>().Verify(w => w.WriteAsync(It.IsAny<IProtocolMessage>()), Times.Never);
    }

    [Fact]
    public async Task A_read_failure_ends_the_loop_without_attempting_a_reply()
    {
        _autoMocker.GetMock<IConnectionInput>()
            .Setup(i => i.ReadRequestAsync())
            .ThrowsAsync(new IOException("connection reset by peer"));

        _autoMocker.GetMock<IResponseWriter>()
            .Setup(w => w.WriteAsync(It.IsAny<IProtocolMessage>()))
            .ThrowsAsync(new IOException("cannot write to a closed connection"));

        var loop = _autoMocker.CreateInstance<MessageLoop>();

        var act = () => loop.RunAsync("testkit-1");
        await act.Should().NotThrowAsync();

        _autoMocker.GetMock<IResponseWriter>().Verify(w => w.WriteAsync(It.IsAny<IProtocolMessage>()), Times.Never);
    }

    [Fact]
    public async Task Malformed_message_reports_BackendError_and_the_loop_continues()
    {
        const string badJson = """{"name":"Bogus","data":{}}""";
        const string goodJson = """{"name":"GetFeatures","data":{}}""";
        var goodMessage = Mock.Of<IProtocolMessage>();

        _autoMocker.GetMock<IConnectionInput>()
            .SetupSequence(i => i.ReadRequestAsync())
            .ReturnsAsync(badJson)
            .ReturnsAsync(goodJson)
            .ReturnsAsync((string?)null);

        _autoMocker.GetMock<IMessageSerializer>()
            .Setup(s => s.Deserialize(badJson))
            .Throws(new TestKitProtocolException("unknown message name 'Bogus'"));
        _autoMocker.GetMock<IMessageSerializer>()
            .Setup(s => s.Deserialize(goodJson))
            .Returns(goodMessage);

        var loop = _autoMocker.CreateInstance<MessageLoop>();

        await WithTimeoutAsync(loop.RunAsync("testkit-1"));

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(
                w => w.WriteAsync(It.Is<BackendErrorResponse>(e => e.Msg == "unknown message name 'Bogus'")),
                Times.Once);

        _autoMocker.GetMock<IMessageDispatcher>().Verify(d => d.DispatchAsync(goodMessage), Times.Once);
    }

    [Fact]
    public async Task Reports_DriverError_for_a_driver_exception_and_continues_the_loop()
    {
        const string failingJson = """{"name":"SessionRun","data":{}}""";
        const string goodJson = """{"name":"GetFeatures","data":{}}""";
        var failingMessage = Mock.Of<IProtocolMessage>();
        var goodMessage = Mock.Of<IProtocolMessage>();
        var exception = new ClientException("Neo.ClientError.Statement.SyntaxError", "bad cypher");
        var errorResponse = new DriverErrorResponse { Id = "error-1", ErrorType = "ClientError" };

        _autoMocker.GetMock<IConnectionInput>()
            .SetupSequence(i => i.ReadRequestAsync())
            .ReturnsAsync(failingJson)
            .ReturnsAsync(goodJson)
            .ReturnsAsync((string?)null);

        _autoMocker.GetMock<IMessageSerializer>().Setup(s => s.Deserialize(failingJson)).Returns(failingMessage);
        _autoMocker.GetMock<IMessageSerializer>().Setup(s => s.Deserialize(goodJson)).Returns(goodMessage);
        _autoMocker.GetMock<IMessageDispatcher>().Setup(d => d.DispatchAsync(failingMessage)).ThrowsAsync(exception);
        _autoMocker.GetMock<IDriverErrorMapper>().Setup(m => m.Map(exception)).Returns(errorResponse);
        _autoMocker.GetMock<IExceptionOriginClassifier>().Setup(c => c.OriginatesInDriver(exception)).Returns(true);

        var loop = _autoMocker.CreateInstance<MessageLoop>();

        await loop.RunAsync("testkit-1");

        _autoMocker.GetMock<IResponseWriter>().Verify(w => w.WriteAsync(errorResponse), Times.Once);

        _autoMocker.GetMock<IMessageDispatcher>().Verify(d => d.DispatchAsync(goodMessage), Times.Once);
    }

    [Fact]
    public async Task Reports_DriverError_for_an_argument_exception_and_continues_the_loop()
    {
        const string failingJson = """{"name":"NewDriver","data":{}}""";
        const string goodJson = """{"name":"GetFeatures","data":{}}""";
        var failingMessage = Mock.Of<IProtocolMessage>();
        var goodMessage = Mock.Of<IProtocolMessage>();
        var exception = new ArgumentException("encryption and trust cannot both be set");
        var errorResponse = new DriverErrorResponse { Id = "error-1", ErrorType = "ArgumentError" };

        _autoMocker.GetMock<IConnectionInput>()
            .SetupSequence(i => i.ReadRequestAsync())
            .ReturnsAsync(failingJson)
            .ReturnsAsync(goodJson)
            .ReturnsAsync((string?)null);

        _autoMocker.GetMock<IMessageSerializer>().Setup(s => s.Deserialize(failingJson)).Returns(failingMessage);
        _autoMocker.GetMock<IMessageSerializer>().Setup(s => s.Deserialize(goodJson)).Returns(goodMessage);
        _autoMocker.GetMock<IMessageDispatcher>().Setup(d => d.DispatchAsync(failingMessage)).ThrowsAsync(exception);
        _autoMocker.GetMock<IDriverErrorMapper>().Setup(m => m.Map(exception)).Returns(errorResponse);
        _autoMocker.GetMock<IExceptionOriginClassifier>().Setup(c => c.OriginatesInDriver(exception)).Returns(true);

        var loop = _autoMocker.CreateInstance<MessageLoop>();

        await loop.RunAsync("testkit-1");

        _autoMocker.GetMock<IResponseWriter>().Verify(w => w.WriteAsync(errorResponse), Times.Once);
        _autoMocker.GetMock<IMessageDispatcher>().Verify(d => d.DispatchAsync(goodMessage), Times.Once);
    }

    [Fact]
    public async Task Reports_DriverError_for_a_time_zone_not_found_exception_and_continues_the_loop()
    {
        const string failingJson = """{"name":"ResultNext","data":{}}""";
        const string goodJson = """{"name":"GetFeatures","data":{}}""";
        var failingMessage = Mock.Of<IProtocolMessage>();
        var goodMessage = Mock.Of<IProtocolMessage>();
        var exception = new TimeZoneNotFoundException("The time zone ID 'Europe/Neo4j' was not found");
        var errorResponse = new DriverErrorResponse { Id = "error-1", ErrorType = "TimeZoneNotFoundException" };

        _autoMocker.GetMock<IConnectionInput>()
            .SetupSequence(i => i.ReadRequestAsync())
            .ReturnsAsync(failingJson)
            .ReturnsAsync(goodJson)
            .ReturnsAsync((string?)null);

        _autoMocker.GetMock<IMessageSerializer>().Setup(s => s.Deserialize(failingJson)).Returns(failingMessage);
        _autoMocker.GetMock<IMessageSerializer>().Setup(s => s.Deserialize(goodJson)).Returns(goodMessage);
        _autoMocker.GetMock<IMessageDispatcher>().Setup(d => d.DispatchAsync(failingMessage)).ThrowsAsync(exception);
        _autoMocker.GetMock<IDriverErrorMapper>().Setup(m => m.Map(exception)).Returns(errorResponse);
        _autoMocker.GetMock<IExceptionOriginClassifier>().Setup(c => c.OriginatesInDriver(exception)).Returns(true);

        var loop = _autoMocker.CreateInstance<MessageLoop>();

        await loop.RunAsync("testkit-1");

        _autoMocker.GetMock<IResponseWriter>().Verify(w => w.WriteAsync(errorResponse), Times.Once);
        _autoMocker.GetMock<IMessageDispatcher>().Verify(d => d.DispatchAsync(goodMessage), Times.Once);
    }

    [Fact]
    public async Task Reports_DriverError_for_an_invalid_operation_exception_and_continues_the_loop()
    {
        const string failingJson = """{"name":"ResultSingle","data":{}}""";
        const string goodJson = """{"name":"GetFeatures","data":{}}""";
        var failingMessage = Mock.Of<IProtocolMessage>();
        var goodMessage = Mock.Of<IProtocolMessage>();
        var exception = new InvalidOperationException("The result is empty.");
        var errorResponse = new DriverErrorResponse { Id = "error-1", ErrorType = "InvalidOperationException" };

        _autoMocker.GetMock<IConnectionInput>()
            .SetupSequence(i => i.ReadRequestAsync())
            .ReturnsAsync(failingJson)
            .ReturnsAsync(goodJson)
            .ReturnsAsync((string?)null);

        _autoMocker.GetMock<IMessageSerializer>().Setup(s => s.Deserialize(failingJson)).Returns(failingMessage);
        _autoMocker.GetMock<IMessageSerializer>().Setup(s => s.Deserialize(goodJson)).Returns(goodMessage);
        _autoMocker.GetMock<IMessageDispatcher>().Setup(d => d.DispatchAsync(failingMessage)).ThrowsAsync(exception);
        _autoMocker.GetMock<IDriverErrorMapper>().Setup(m => m.Map(exception)).Returns(errorResponse);
        _autoMocker.GetMock<IExceptionOriginClassifier>().Setup(c => c.OriginatesInDriver(exception)).Returns(true);

        var loop = _autoMocker.CreateInstance<MessageLoop>();

        await loop.RunAsync("testkit-1");

        _autoMocker.GetMock<IResponseWriter>().Verify(w => w.WriteAsync(errorResponse), Times.Once);
        _autoMocker.GetMock<IMessageDispatcher>().Verify(d => d.DispatchAsync(goodMessage), Times.Once);
    }

    [Fact]
    public async Task Reports_FrontendError_for_a_frontend_exception_and_continues_the_loop()
    {
        const string failingJson = """{"name":"RetryableNegative","data":{}}""";
        const string goodJson = """{"name":"GetFeatures","data":{}}""";
        var failingMessage = Mock.Of<IProtocolMessage>();
        var goodMessage = Mock.Of<IProtocolMessage>();
        var exception = new FrontendException("Error from client in retryable tx");

        _autoMocker.GetMock<IConnectionInput>()
            .SetupSequence(i => i.ReadRequestAsync())
            .ReturnsAsync(failingJson)
            .ReturnsAsync(goodJson)
            .ReturnsAsync((string?)null);

        _autoMocker.GetMock<IMessageSerializer>().Setup(s => s.Deserialize(failingJson)).Returns(failingMessage);
        _autoMocker.GetMock<IMessageSerializer>().Setup(s => s.Deserialize(goodJson)).Returns(goodMessage);
        _autoMocker.GetMock<IMessageDispatcher>().Setup(d => d.DispatchAsync(failingMessage)).ThrowsAsync(exception);

        var loop = _autoMocker.CreateInstance<MessageLoop>();

        await loop.RunAsync("testkit-1");

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(
                w => w.WriteAsync(It.Is<FrontendErrorResponse>(e => e.Msg == "Error from client in retryable tx")),
                Times.Once);

        _autoMocker.GetMock<IDriverErrorMapper>().Verify(m => m.Map(It.IsAny<Exception>()), Times.Never);
        _autoMocker.GetMock<IMessageDispatcher>().Verify(d => d.DispatchAsync(goodMessage), Times.Once);
    }

    [Fact]
    public async Task Reports_a_bare_BackendError_when_the_exception_does_not_originate_in_the_driver()
    {
        const string failingJson = """{"name":"NewDriver","data":{}}""";
        const string goodJson = """{"name":"GetFeatures","data":{}}""";
        var failingMessage = Mock.Of<IProtocolMessage>();
        var goodMessage = Mock.Of<IProtocolMessage>();
        var exception = new NullReferenceException("backend bug");

        _autoMocker.GetMock<IConnectionInput>()
            .SetupSequence(i => i.ReadRequestAsync())
            .ReturnsAsync(failingJson)
            .ReturnsAsync(goodJson)
            .ReturnsAsync((string?)null);

        _autoMocker.GetMock<IMessageSerializer>().Setup(s => s.Deserialize(failingJson)).Returns(failingMessage);
        _autoMocker.GetMock<IMessageSerializer>().Setup(s => s.Deserialize(goodJson)).Returns(goodMessage);
        _autoMocker.GetMock<IMessageDispatcher>().Setup(d => d.DispatchAsync(failingMessage)).ThrowsAsync(exception);
        _autoMocker.GetMock<IExceptionOriginClassifier>().Setup(c => c.OriginatesInDriver(exception)).Returns(false);

        var loop = _autoMocker.CreateInstance<MessageLoop>();

        await loop.RunAsync("testkit-1");

        _autoMocker.GetMock<IResponseWriter>()
            .Verify(w => w.WriteAsync(It.Is<BackendErrorResponse>(e => e.Msg == "backend bug")), Times.Once);
        _autoMocker.GetMock<IDriverErrorMapper>().Verify(m => m.Map(It.IsAny<Exception>()), Times.Never);
        _autoMocker.GetMock<IMessageDispatcher>().Verify(d => d.DispatchAsync(goodMessage), Times.Once);
    }

    [Fact]
    public async Task A_held_handler_does_not_block_dispatch_of_later_messages()
    {
        const string holdingJson = """{"name":"SessionReadTransaction","data":{}}""";
        const string fulfillingJson = """{"name":"RetryablePositive","data":{}}""";
        var holdingMessage = Mock.Of<IProtocolMessage>();
        var fulfillingMessage = Mock.Of<IProtocolMessage>();
        var outcome = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _autoMocker.GetMock<IConnectionInput>()
            .SetupSequence(i => i.ReadRequestAsync())
            .ReturnsAsync(holdingJson)
            .ReturnsAsync(fulfillingJson)
            .ReturnsAsync((string?)null);

        _autoMocker.GetMock<IMessageSerializer>().Setup(s => s.Deserialize(holdingJson)).Returns(holdingMessage);
        _autoMocker.GetMock<IMessageSerializer>().Setup(s => s.Deserialize(fulfillingJson)).Returns(fulfillingMessage);
        _autoMocker.GetMock<IMessageDispatcher>().Setup(d => d.DispatchAsync(holdingMessage)).Returns(outcome.Task);
        _autoMocker.GetMock<IMessageDispatcher>()
            .Setup(d => d.DispatchAsync(fulfillingMessage))
            .Returns(() =>
            {
                outcome.SetResult();
                return Task.CompletedTask;
            });

        var loop = _autoMocker.CreateInstance<MessageLoop>();

        await WithTimeoutAsync(loop.RunAsync("testkit-1"));

        _autoMocker.GetMock<IMessageDispatcher>().Verify(d => d.DispatchAsync(fulfillingMessage), Times.Once);
    }

    [Fact]
    public async Task A_handler_with_a_blocking_synchronous_prefix_does_not_block_the_read_loop()
    {
        const string blockingJson = """{"name":"SessionReadTransaction","data":{}}""";
        const string releasingJson = """{"name":"RetryablePositive","data":{}}""";
        var blockingMessage = Mock.Of<IProtocolMessage>();
        var releasingMessage = Mock.Of<IProtocolMessage>();
        var gate = new ManualResetEventSlim();
        var released = false;

        _autoMocker.GetMock<IConnectionInput>()
            .SetupSequence(i => i.ReadRequestAsync())
            .ReturnsAsync(blockingJson)
            .ReturnsAsync(releasingJson)
            .ReturnsAsync((string?)null);

        _autoMocker.GetMock<IMessageSerializer>().Setup(s => s.Deserialize(blockingJson)).Returns(blockingMessage);
        _autoMocker.GetMock<IMessageSerializer>().Setup(s => s.Deserialize(releasingJson)).Returns(releasingMessage);
        _autoMocker.GetMock<IMessageDispatcher>()
            .Setup(d => d.DispatchAsync(blockingMessage))
            .Returns(() =>
            {
                released = gate.Wait(TimeSpan.FromSeconds(2));
                return Task.CompletedTask;
            });
        _autoMocker.GetMock<IMessageDispatcher>()
            .Setup(d => d.DispatchAsync(releasingMessage))
            .Returns(() =>
            {
                gate.Set();
                return Task.CompletedTask;
            });

        var loop = _autoMocker.CreateInstance<MessageLoop>();

        await WithTimeoutAsync(loop.RunAsync("testkit-1"));

        released.Should().BeTrue();
    }

    [Fact]
    public async Task Closing_the_connection_cancels_outstanding_expectations_and_unwinds_held_handlers()
    {
        const string holdingJson = """{"name":"SessionReadTransaction","data":{}}""";
        var holdingMessage = Mock.Of<IProtocolMessage>();
        var expectations = _autoMocker.CreateInstance<ExpectationStore>();
        _autoMocker.Use<IExpectationStore>(expectations);
        var unwound = false;

        _autoMocker.GetMock<IConnectionInput>()
            .SetupSequence(i => i.ReadRequestAsync())
            .ReturnsAsync(holdingJson)
            .ReturnsAsync((string?)null);

        _autoMocker.GetMock<IMessageSerializer>().Setup(s => s.Deserialize(holdingJson)).Returns(holdingMessage);
        _autoMocker.GetMock<IMessageDispatcher>()
            .Setup(d => d.DispatchAsync(holdingMessage))
            .Returns(async () =>
            {
                try
                {
                    await expectations.Expect<string>("key-1");
                }
                finally
                {
                    unwound = true;
                }
            });

        var loop = _autoMocker.CreateInstance<MessageLoop>();

        await WithTimeoutAsync(loop.RunAsync("testkit-1"));

        unwound.Should().BeTrue();
        _autoMocker.GetMock<IResponseWriter>().Verify(w => w.WriteAsync(It.IsAny<IProtocolMessage>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_completes_when_input_reaches_eof()
    {
        _autoMocker.GetMock<IConnectionInput>()
            .Setup(i => i.ReadRequestAsync())
            .ReturnsAsync((string?)null);

        var loop = _autoMocker.CreateInstance<MessageLoop>();

        var run = Task.Run(() => loop.RunAsync("testkit-1"), TestContext.Current.CancellationToken);
        var completed = await Task.WhenAny(
            run,
            Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));

        completed.Should().BeSameAs(run);
        await run;
    }

    [Fact]
    public async Task Eof_after_messages_cancels_outstanding_expectations_and_unwinds_held_handlers()
    {
        const string holdingJson = """{"name":"SessionReadTransaction","data":{}}""";
        var holdingMessage = Mock.Of<IProtocolMessage>();
        var expectations = _autoMocker.CreateInstance<ExpectationStore>();
        _autoMocker.Use<IExpectationStore>(expectations);
        var unwound = false;
        var readCount = 0;

        _autoMocker.GetMock<IConnectionInput>()
            .Setup(i => i.ReadRequestAsync())
            .ReturnsAsync(() => readCount++ == 0 ? holdingJson : null);

        _autoMocker.GetMock<IMessageSerializer>().Setup(s => s.Deserialize(holdingJson)).Returns(holdingMessage);
        _autoMocker.GetMock<IMessageDispatcher>()
            .Setup(d => d.DispatchAsync(holdingMessage))
            .Returns(async () =>
            {
                try
                {
                    await expectations.Expect<string>("key-1");
                }
                finally
                {
                    unwound = true;
                }
            });

        var loop = _autoMocker.CreateInstance<MessageLoop>();

        var run = Task.Run(() => loop.RunAsync("testkit-1"), TestContext.Current.CancellationToken);
        var completed = await Task.WhenAny(
            run,
            Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));

        completed.Should().BeSameAs(run);
        await run;

        unwound.Should().BeTrue();
        _autoMocker.GetMock<IResponseWriter>().Verify(w => w.WriteAsync(It.IsAny<IProtocolMessage>()), Times.Never);
    }

    private static async Task WithTimeoutAsync(Task task)
    {
        var completed = await Task.WhenAny(
            task,
            Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));

        completed.Should().BeSameAs(task);
        await task;
    }
}
