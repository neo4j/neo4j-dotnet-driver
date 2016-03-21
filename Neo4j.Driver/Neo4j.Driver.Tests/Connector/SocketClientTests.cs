// Copyright (c) 2002-2016 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
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
using Neo4j.Driver.Exceptions;
using Neo4j.Driver.Extensions;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;
using Xunit;
using Record = Xunit.Record;

namespace Neo4j.Driver.Tests
{
    public class SocketClientTests
    {
        private static Uri FakeUri => new Uri("bolt://foo.bar:7878");

        public class StartMethod
        {
            [Fact]
            public async Task ShouldConnectWithoutTlsToTheServer()
            {
                using (var harness = new SocketClientTestHarness(FakeUri))
                {
                    harness.SetupReadStream(new byte[] {0, 0, 0, 1});
                    await harness.Client.Start();
                    harness.MockTcpSocketClient.Verify(t => t.ConnectAsync(FakeUri.Host, FakeUri.Port, false),
                        Times.Once);
                }
            }

            [Theory]
            [InlineData(new byte[] {0, 0, 0, 0})]
            [InlineData(new byte[] {0, 0, 0, 2})]
            public async Task ShouldThrowExceptionIfVersionIsNotSupported(byte[] response)
            {
                using (var harness = new SocketClientTestHarness(FakeUri))
                {
                    harness.SetupReadStream(response);
                    await harness.ExpectException<NotSupportedException>(() => harness.Client.Start());
                }
            }
        }

        public class SendMethod
        {
            [Fact]
            public async Task ShouldSendMessagesAsExpected()
            {
                // Given
                var messages = new IRequestMessage[] {new RunMessage("Run message 1"), new RunMessage("Run message 1")};
                byte[] expectedBytes =
                {
                    0x00, 0x11, 0xB2, 0x10, 0x8D, 0x52, 0x75, 0x6E, 0x20, 0x6D, 0x65, 0x73, 0x73,
                    0x61, 0x67, 0x65, 0x20, 0x31, 0xA0, 0x00, 0x00, 0x00, 0x11, 0xB2, 0x10, 0x8D, 0x52, 0x75, 0x6E, 0x20,
                    0x6D, 0x65, 0x73, 0x73,
                    0x61, 0x67, 0x65, 0x20, 0x31, 0xA0, 0x00, 0x00
                };
                var expectedLength = expectedBytes.Length;
                expectedBytes = expectedBytes.PadRight(ChunkedOutputStream.BufferSize);

                var messageHandler = new MessageResponseHandler();
                messageHandler.Register(new InitMessage("MyClient/1.1", new Dictionary<string, object>()));
                var rb = new ResultBuilder();
                messageHandler.Register(messages[0], rb);
                messageHandler.Register(messages[1], rb);

                using (var harness = new SocketClientTestHarness(FakeUri, null))
                {
                    harness.SetupReadStream("00 00 00 01" +
                                            "00 03 b1 70 a0 00 00" +
                                            "00 0f b1 70  a1 86 66 69  65 6c 64 73  91 83 6e 75 6d 00 00" +
                                            "00 0f b1 70  a1 86 66 69  65 6c 64 73  91 83 6e 75 6d 00 00");
                    harness.SetupWriteStream();

                    await harness.Client.Start();
                    harness.ResetCalls();

                    // When
                    harness.Client.Send(messages, messageHandler);

                    // Then
                    harness.VerifyWriteStreamUsages(2 /*write + flush*/);

                    harness.VerifyWriteStreamContent(expectedBytes, expectedLength);
                }
            }

            [Fact]
            public async Task ShouldCreateExceptionWhenErrorReceivedFromDatabase()
            {
                using (var harness = new SocketClientTestHarness(FakeUri, null))
                {
                    var messages = new IRequestMessage[] {new RunMessage("This will cause a syntax error")};
                    var messageHandler = new MessageResponseHandler();
                    messageHandler.Register(new InitMessage("MyClient/1.1", new Dictionary<string, object>()));
                    messageHandler.Register(messages[0], new ResultBuilder());

                    harness.SetupReadStream("00 00 00 01" +
                                            "00 03 b1 70 a0 00 00" +
                                            "00a0b17fa284636f6465d0274e656f2e436c69656e744572726f722e53746174656d656e742e496e76616c696453796e746178876d657373616765d065496e76616c696420696e707574202754273a206578706563746564203c696e69743e20286c696e6520312c20636f6c756d6e203120286f66667365743a203029290a22546869732077696c6c20636175736520612073796e746178206572726f72220a205e0000");

                    harness.SetupWriteStream();

                    await harness.Client.Start();
                    harness.ResetCalls();

                    // When
                    harness.Client.Send(messages, messageHandler);

                    // Then
                    harness.VerifyWriteStreamUsages(2 /*write + flush*/);

                    messageHandler.HasError.Should().BeTrue();
                    messageHandler.Error.Code.Should().Be("Neo.ClientError.Statement.InvalidSyntax");
                    messageHandler.Error.Message.Should()
                        .Be(
                            "Invalid input 'T': expected <init> (line 1, column 1 (offset: 0))\n\"This will cause a syntax error\"\n ^");
                }
            }

            [Fact]
            public async Task ShouldIgnorePullAllWhenErrorHappenedDuringRun()
            {
                using (var harness = new SocketClientTestHarness(FakeUri, null))
                {
                    var messages = new IRequestMessage[]
                    {
                        new RunMessage("This will cause a syntax error"),
                        new PullAllMessage()
                    };

                    var messageHandler = new TestResponseHandler();

                    messageHandler.Register(new InitMessage("MyClient/1.1", new Dictionary<string, object>()));
                    messageHandler.Register(messages[0], new ResultBuilder());
                    messageHandler.Register(messages[1], new ResultBuilder());

                    harness.SetupReadStream("00 00 00 01" +
                                            "00 03 b1 70 a0 00 00" +
                                            "00a0b17fa284636f6465d0274e656f2e436c69656e744572726f722e53746174656d656e742e496e76616c696453796e746178876d657373616765d065496e76616c696420696e707574202754273a206578706563746564203c696e69743e20286c696e6520312c20636f6c756d6e203120286f66667365743a203029290a22546869732077696c6c20636175736520612073796e746178206572726f72220a205e0000" +
                                            "00 02 b0 7e 00 00");

                    harness.SetupWriteStream();

                    await harness.Client.Start();
                    harness.ResetCalls();


                    // When
                    harness.Client.Send(messages, messageHandler);

                    // Then
                    harness.VerifyWriteStreamUsages(2 /*write + flush*/);

                    messageHandler.HasError.Should().BeTrue();
                    messageHandler.Error.Code.Should().Be("Neo.ClientError.Statement.InvalidSyntax");
                    messageHandler.Error.Message.Should()
                        .Be("Invalid input 'T': expected <init> (line 1, column 1 (offset: 0))\n\"This will cause a syntax error\"\n ^");
                    messageHandler.QueueIsEmpty().Should().BeTrue();
                    messageHandler.FailureMessageCalled.Should().Be(1);
                    messageHandler.IgnoreMessageCalled.Should().Be(1);
                }
            }

            [Fact]
            public async Task ShouldStopClientAndThrowExceptionWhenProtocolErrorOccurs()
            {
                using (var harness = new SocketClientTestHarness(FakeUri, null))
                {
                    var messages = new IRequestMessage[]
                    {
                        new RunMessage("This will cause a syntax error"),
                        new PullAllMessage()
                    };

                    var messageHandler = new TestResponseHandler();

                    messageHandler.Register(new InitMessage("MyClient/1.1", new Dictionary<string, object>()));
                    messageHandler.Register(messages[0], new ResultBuilder());
                    messageHandler.Register(messages[1], new ResultBuilder());

                    harness.SetupReadStream("00 00 00 01" +
                                            "00 03 b1 70 a0 00 00");

                    harness.SetupWriteStream();

                    await harness.Client.Start();

                    messageHandler.Error = new ClientException("Neo.ClientError.Request.Invalid", "Test Message");

                    // When
                    var ex = Record.Exception(() => harness.Client.Send(messages, messageHandler));
                    ex.Should().BeOfType<ClientException>();

                    harness.MockTcpSocketClient.Verify(x => x.DisconnectAsync(), Times.Once);
                    harness.MockTcpSocketClient.Verify(x => x.Dispose(), Times.Once);
                }
            }

            private class TestResponseHandler : IMessageResponseHandler
            {
                private readonly MessageResponseHandler _messageHandler;

                public TestResponseHandler()
                {
                    _messageHandler = new MessageResponseHandler();
                }

                public int FailureMessageCalled { get; private set; }
                public int IgnoreMessageCalled { get; private set; }


                public void HandleSuccessMessage(IDictionary<string, object> meta)
                {
                    _messageHandler.HandleSuccessMessage(meta);
                }

                public void HandleFailureMessage(string code, string message)
                {
                    FailureMessageCalled++;
                    _messageHandler.HandleFailureMessage(code, message);
                }

                public void HandleIgnoredMessage()
                {
                    IgnoreMessageCalled++;
                    _messageHandler.HandleIgnoredMessage();
                }

                public void HandleRecordMessage(object[] fields)
                {
                    _messageHandler.HandleRecordMessage(fields);
                }

                public void Register(IRequestMessage requestMessage, IResultBuilder resultBuilder = null)
                {
                    _messageHandler.Register(requestMessage, resultBuilder);
                }

                public void Clear()
                {
                    throw new NotImplementedException();
                }

                public bool QueueIsEmpty()
                {
                    return _messageHandler.QueueIsEmpty();
                }

                public bool HasError => _messageHandler.HasError;

                public Neo4jException Error
                {
                    get { return _messageHandler.Error; }
                    set { _messageHandler.Error = value; }
                }
            }
        }

        public class StopMethod
        {
            [Fact]
            public async Task ShouldCallDisconnectOnTheTcpSocketClient()
            {
                using (var harness = new SocketClientTestHarness(FakeUri))
                {
                    harness.SetupReadStream("00 00 00 01");
                    await harness.Client.Start();
                    await harness.Client.Stop();
                    harness.MockTcpSocketClient.Verify(s => s.DisconnectAsync(), Times.Once);
                    harness.MockTcpSocketClient.Verify(s => s.Dispose(), Times.Once);
                    harness.Client.IsOpen.Should().BeFalse();
                }
            }
        }
    }
}
