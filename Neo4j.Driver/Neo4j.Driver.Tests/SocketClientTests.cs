//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
using System;
using System.Threading.Tasks;
using Moq;
using Neo4j.Driver.Internal.messaging;
using Neo4j.Driver.Internal.result;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class SocketClientTests
    {
        private static Uri FakeUri => new Uri("bolt://foo.bar:7878");

        public class StartMethod
        {
            [Fact]
            public async Task ShouldConnectToTheServer()
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
                var messages = new IMessage[] {new RunMessage("Run message 1"), new RunMessage("Run message 1")};
                byte[] expectedBytes =
                {
                    0x00, 0x11, 0xB2, 0x10, 0x8D, 0x52, 0x75, 0x6E, 0x20, 0x6D, 0x65, 0x73, 0x73,
                    0x61, 0x67, 0x65, 0x20, 0x31, 0xA0, 0x00, 0x00, 0x00, 0x11, 0xB2, 0x10, 0x8D, 0x52, 0x75, 0x6E, 0x20, 0x6D, 0x65, 0x73, 0x73,
                    0x61, 0x67, 0x65, 0x20, 0x31, 0xA0, 0x00, 0x00
                };
                var expectedLength = expectedBytes.Length;
                expectedBytes = expectedBytes.PadRight(ChunkedOutputStream.BufferSize);

                var messageHandler = new MessageResponseHandler();
                messageHandler.Register(new InitMessage("MyClient/1.0"));
                var rb = new ResultBuilder();
                messageHandler.Register(messages[0], rb);
                messageHandler.Register(messages[1], rb);

                using (var harness = new SocketClientTestHarness(FakeUri, null))
                {
                    harness.SetupReadStream("00 00 00 01"
                                            + "00 03 b1 70 a0 00 00"
                                            + "00 0f b1 70  a1 86 66 69  65 6c 64 73  91 83 6e 75 6d 00 00"
                                            + "00 0f b1 70  a1 86 66 69  65 6c 64 73  91 83 6e 75 6d 00 00");
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
        }

        public class StopMethod
        {
            [Fact]
            public async Task ShouldCallDisconnectOnTheTcpSocketClient()
            {
                using (var harness = new SocketClientTestHarness(FakeUri))
                {
                    await harness.Client.Stop();
                    harness.MockTcpSocketClient.Verify(s => s.DisconnectAsync(), Times.Once);
                    harness.MockTcpSocketClient.Verify(s => s.Dispose(), Times.Once);
                }
            }
        }
    }
}