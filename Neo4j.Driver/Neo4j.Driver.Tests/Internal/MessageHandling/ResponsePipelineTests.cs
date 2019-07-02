// Copyright (c) 2002-2019 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Messaging;
using Xunit;

namespace Neo4j.Driver.Internal.MessageHandling
{
    public class ResponsePipelineTests
    {
        [Fact]
        public void ShouldStartWithNoPendingMessages()
        {
            var pipeline = new ResponsePipeline(null);

            pipeline.HasNoPendingMessages.Should().BeTrue();
        }

        [Fact]
        public void ShouldThrowOnCurrentIfNoPendingMessages()
        {
            var pipeline = new ResponsePipeline(null);

            var exc = Record.Exception(() => pipeline.Peek());

            exc.Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public void ShouldThrowIfEnqueuedMessageIsNull()
        {
            var handler = new Mock<IResponseHandler>();
            var pipeline = new ResponsePipeline(null);

            var exc = Record.Exception(() => pipeline.Enqueue(null, handler.Object));

            exc.Should().BeOfType<ArgumentNullException>()
                .Which.ParamName.Should().Be("message");
        }

        [Fact]
        public void ShouldThrowIfEnqueuedHandlerIsNull()
        {
            var msg = new Mock<IRequestMessage>();
            var pipeline = new ResponsePipeline(null);

            var exc = Record.Exception(() => pipeline.Enqueue(msg.Object, null));

            exc.Should().BeOfType<ArgumentNullException>()
                .Which.ParamName.Should().Be("handler");
        }

        [Fact]
        public void ShouldEnqueueResponseHandlers()
        {
            var (msg, handler) = (new Mock<IRequestMessage>(), new Mock<IResponseHandler>());
            var pipeline = new ResponsePipeline(null);

            pipeline.Enqueue(msg.Object, handler.Object);

            pipeline.HasNoPendingMessages.Should().BeFalse();
            pipeline.Peek().Should().Be(handler.Object);
        }

        [Fact]
        public void ShouldDequeueResponseHandlers()
        {
            var (msg, handler) = (new Mock<IRequestMessage>(), new Mock<IResponseHandler>());
            var pipeline = new ResponsePipeline(null);

            pipeline.Enqueue(msg.Object, handler.Object);
            pipeline.HasNoPendingMessages.Should().BeFalse();

            pipeline.Dequeue();
            pipeline.HasNoPendingMessages.Should().BeTrue();
        }

        [Fact]
        public void ShouldDequeueResponseHandlersInOrder()
        {
            var (msg1, handler1) = (new Mock<IRequestMessage>(), new Mock<IResponseHandler>());
            var (msg2, handler2) = (new Mock<IRequestMessage>(), new Mock<IResponseHandler>());
            var pipeline = new ResponsePipeline(null);

            pipeline.Enqueue(msg1.Object, handler1.Object);
            pipeline.Enqueue(msg2.Object, handler2.Object);

            pipeline.HasNoPendingMessages.Should().BeFalse();
            pipeline.Peek().Should().Be(handler1.Object);
            pipeline.Dequeue();

            pipeline.HasNoPendingMessages.Should().BeFalse();
            pipeline.Peek().Should().Be(handler2.Object);
            pipeline.Dequeue();

            pipeline.HasNoPendingMessages.Should().BeTrue();
        }

        public class OnSuccessAsync
        {
            [Fact]
            public void ShouldLog()
            {
                var log = new Mock<IDriverLogger>();
                log.Setup(x => x.IsDebugEnabled()).Returns(true);

                var pipeline = CreatePipelineWithHandler(log.Object);
                var metadata = new Dictionary<string, object> {{"x", 1}, {"y", true}};

                pipeline.OnSuccess(metadata);

                log.Verify(x => x.Debug("S: {0}", It.Is<SuccessMessage>(m => m.Meta.Equals(metadata))), Times.Once);
            }

            [Fact]
            public void ShouldNotLogIfDebugDisabled()
            {
                var log = new Mock<IDriverLogger>();
                log.Setup(x => x.IsDebugEnabled()).Returns(false);

                var pipeline = CreatePipelineWithHandler(log.Object);
                var metadata = new Dictionary<string, object> {{"x", 1}, {"y", true}};

                pipeline.OnSuccess(metadata);

                log.Verify(x => x.Debug("S: {0}", It.IsAny<object[]>()), Times.Never);
            }

            [Fact]
            public void ShouldDequeue()
            {
                var pipeline = CreatePipelineWithHandler(null);
                var metadata = new Dictionary<string, object> {{"x", 1}, {"y", true}};

                pipeline.HasNoPendingMessages.Should().BeFalse();

                pipeline.OnSuccess(metadata);

                pipeline.HasNoPendingMessages.Should().BeTrue();
            }

            [Fact]
            public void ShouldInvokeHandler()
            {
                var pipeline = new ResponsePipeline(null);

                var (msg, handler) = (new Mock<IRequestMessage>(), new Mock<IResponseHandler>());
                pipeline.Enqueue(msg.Object, handler.Object);

                var metadata = new Dictionary<string, object> {{"x", 1}, {"y", true}};
                pipeline.OnSuccess(metadata);

                handler.Verify(x => x.OnSuccess(metadata), Times.Once);
            }
        }

        public class OnRecordAsync
        {
            [Fact]
            public void ShouldLog()
            {
                var log = new Mock<IDriverLogger>();
                log.Setup(x => x.IsDebugEnabled()).Returns(true);

                var pipeline = CreatePipelineWithHandler(log.Object);
                var fields = new object[] {1, true, "string"};

                pipeline.OnRecord(fields);

                log.Verify(x => x.Debug("S: {0}", It.Is<RecordMessage>(m => m.Fields.Equals(fields))), Times.Once);
            }

            [Fact]
            public void ShouldNotLogIfDebugDisabled()
            {
                var log = new Mock<IDriverLogger>();
                log.Setup(x => x.IsDebugEnabled()).Returns(false);

                var pipeline = CreatePipelineWithHandler(log.Object);
                var fields = new object[] {1, true, "string"};

                pipeline.OnRecord(fields);

                log.Verify(x => x.Debug("S: {0}", It.IsAny<object[]>()), Times.Never);
            }

            [Fact]
            public void ShouldNotDequeue()
            {
                var pipeline = CreatePipelineWithHandler(null);
                var fields = new object[] {1, true, "string"};

                pipeline.HasNoPendingMessages.Should().BeFalse();
                pipeline.OnRecord(fields);

                pipeline.HasNoPendingMessages.Should().BeFalse();
                pipeline.OnRecord(fields);

                pipeline.HasNoPendingMessages.Should().BeFalse();
            }

            [Fact]
            public void ShouldInvokeHandler()
            {
                var pipeline = new ResponsePipeline(null);

                var (msg, handler) = (new Mock<IRequestMessage>(), new Mock<IResponseHandler>());
                pipeline.Enqueue(msg.Object, handler.Object);

                var fields = new object[] {1, true, "string"};
                pipeline.OnRecord(fields);

                handler.Verify(x => x.OnRecord(fields), Times.Once);
            }
        }

        public class OnFailureAsync
        {
            [Fact]
            public void ShouldLog()
            {
                var log = new Mock<IDriverLogger>();
                log.Setup(x => x.IsDebugEnabled()).Returns(true);

                var pipeline = CreatePipelineWithHandler(log.Object);
                var (code, message) = ("Neo.TransientError.Transaction.Terminated", "transaction terminated.");

                pipeline.OnFailure(code, message);

                log.Verify(
                    x => x.Debug("S: {0}",
                        It.Is<FailureMessage>(m => m.Code.Equals(code) && m.Message.Equals(message))), Times.Once);
            }

            [Fact]
            public void ShouldNotLogIfDebugDisabled()
            {
                var log = new Mock<IDriverLogger>();
                log.Setup(x => x.IsDebugEnabled()).Returns(false);

                var pipeline = CreatePipelineWithHandler(log.Object);
                var (code, message) = ("Neo.TransientError.Transaction.Terminated", "transaction terminated.");

                pipeline.OnFailure(code, message);

                log.Verify(x => x.Debug("S: {0}", It.IsAny<object[]>()), Times.Never);
            }

            [Fact]
            public void ShouldDequeue()
            {
                var pipeline = CreatePipelineWithHandler(null);
                var (code, message) = ("Neo.TransientError.Transaction.Terminated", "transaction terminated.");

                pipeline.HasNoPendingMessages.Should().BeFalse();

                pipeline.OnFailure(code, message);

                pipeline.HasNoPendingMessages.Should().BeTrue();
            }

            [Fact]
            public void ShouldInvokeHandler()
            {
                var pipeline = new ResponsePipeline(null);
                var (code, message) = ("Neo.TransientError.Transaction.Terminated", "transaction terminated.");

                var (msg, handler) = (new Mock<IRequestMessage>(), new Mock<IResponseHandler>());
                pipeline.Enqueue(msg.Object, handler.Object);

                pipeline.OnFailure(code, message);

                handler.Verify(
                    x => x.OnFailure(It.Is<IResponsePipelineError>(e =>
                        e.Is(t => t is TransientException && t.Message.Equals("transaction terminated.")))),
                    Times.Once);
            }

            [Fact]
            public void ShouldRecordErrorAndThrowOnAssertNoFailure()
            {
                var pipeline = CreatePipelineWithHandler(null);
                var (code, message) = ("Neo.TransientError.Transaction.Terminated", "transaction terminated.");

                pipeline.OnFailure(code, message);

                var exc = Record.Exception(() => pipeline.AssertNoFailure());

                exc.Should().BeOfType<TransientException>().Which
                    .Message.Should().Be("transaction terminated.");
            }

            [Fact]
            public void ShouldRecordErrorAndNotThrowOnAssertNoProtocolViolation()
            {
                var pipeline = CreatePipelineWithHandler(null);
                var (code, message) = ("Neo.TransientError.Transaction.Terminated", "transaction terminated.");

                pipeline.OnFailure(code, message);

                var exc = Record.Exception(() => pipeline.AssertNoProtocolViolation());

                exc.Should().BeNull();
            }

            [Fact]
            public void ShouldRecordErrorAndThrowOnAssertNoProtocolViolation()
            {
                var pipeline = new ResponsePipeline(null);
                var (code, message) = ("Neo.ClientError.Request.Invalid", "protocol exception.");

                var (msg, handler) = (new Mock<IRequestMessage>(), new Mock<IResponseHandler>());
                pipeline.Enqueue(msg.Object, handler.Object);

                pipeline.OnFailure(code, message);

                var exc = Record.Exception(() => pipeline.AssertNoProtocolViolation());

                exc.Should().BeOfType<ProtocolException>().Which
                    .Message.Should().Be("protocol exception.");
            }
        }

        public class OnIgnoredAsync
        {
            [Fact]
            public void ShouldLog()
            {
                var log = new Mock<IDriverLogger>();
                log.Setup(x => x.IsDebugEnabled()).Returns(true);

                var pipeline = CreatePipelineWithHandler(log.Object);

                pipeline.OnIgnored();

                log.Verify(x => x.Debug("S: {0}", It.IsAny<IgnoredMessage>()), Times.Once);
            }

            [Fact]
            public void ShouldNotLogIfDebugDisabled()
            {
                var log = new Mock<IDriverLogger>();
                log.Setup(x => x.IsDebugEnabled()).Returns(false);

                var pipeline = CreatePipelineWithHandler(log.Object);

                pipeline.OnIgnored();

                log.Verify(x => x.Debug("S: {0}", It.IsAny<object[]>()), Times.Never);
            }

            [Fact]
            public void ShouldDequeue()
            {
                var pipeline = CreatePipelineWithHandler(null);

                pipeline.HasNoPendingMessages.Should().BeFalse();

                pipeline.OnIgnored();

                pipeline.HasNoPendingMessages.Should().BeTrue();
            }

            [Fact]
            public void ShouldInvokeHandler()
            {
                var pipeline = new ResponsePipeline(null);

                var (msg, handler) = (new Mock<IRequestMessage>(), new Mock<IResponseHandler>());
                pipeline.Enqueue(msg.Object, handler.Object);

                pipeline.OnIgnored();

                handler.Verify(x => x.OnIgnored(), Times.Once);
            }

            [Fact]
            public void ShouldInvokeOnFailureAsyncOfHandlerIfHasRecordedError()
            {
                var pipeline = CreatePipelineWithHandler(null);
                var (code, message) = ("Neo.TransientError.Transaction.Terminated", "transaction terminated.");
                pipeline.OnFailure(code, message);

                var (msg, handler) = (new Mock<IRequestMessage>(), new Mock<IResponseHandler>());
                pipeline.Enqueue(msg.Object, handler.Object);

                pipeline.OnIgnored();

                handler.Verify(
                    x => x.OnFailure(It.Is<IResponsePipelineError>(e =>
                        e.Is(t => t is TransientException && t.Message.Equals("transaction terminated.")))),
                    Times.Once);
            }
        }

        private static ResponsePipeline CreatePipelineWithHandler(IDriverLogger logger)
        {
            var pipeline = new ResponsePipeline(logger);

            var (msg, handler) = (new Mock<IRequestMessage>(), new Mock<IResponseHandler>());
            pipeline.Enqueue(msg.Object, handler.Object);

            return pipeline;
        }
    }
}