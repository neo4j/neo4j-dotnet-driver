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
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Protocol;
using Xunit;
using static Neo4j.Driver.Internal.ConnectionSettings;
using static Xunit.Record;
using Record = Xunit.Record;

namespace Neo4j.Driver.Tests
{
    public class SocketClientTests
    {
        private static Uri FakeUri => new Uri("bolt://foo.bar:7878");

        public class ConnectMethod
        {
            [Fact]
            public async void ShouldThrowIOExceptionIfFailedToReadOnHandshakeAsync()
            {
                var bufferSettings = new BufferSettings(Config.DefaultConfig);

                var connMock = new Mock<ITcpSocketClient>();
                TcpSocketClientTestSetup.CreateReadStreamMock(connMock);
                TcpSocketClientTestSetup.CreateWriteStreamMock(connMock);

                var client = new SocketClient(FakeUri, null, bufferSettings, socketClient: connMock.Object);

                var ex = await Record.ExceptionAsync(() => client.ConnectAsync());

                ex.Should().NotBeNull().And.BeOfType<IOException>();
            }
        }

        public class StartMethod
        {
            [Fact]
            public async Task ShouldConnectServerAsync()
            {
                var bufferSettings = new BufferSettings(Config.DefaultConfig);

                var connMock = new Mock<ITcpSocketClient>();
                TcpSocketClientTestSetup.CreateReadStreamMock(connMock, new byte[] {0, 0, 0, 1});
                TcpSocketClientTestSetup.CreateWriteStreamMock(connMock);

                var client = new SocketClient(FakeUri, null, bufferSettings, socketClient: connMock.Object);

                await client.ConnectAsync();

                // Then
                connMock.Verify(x => x.ConnectAsync(FakeUri), Times.Once);
            }
        }

        public class SendMethod
        {
            [Fact]
            public async Task ShouldSendAllMessagesAsync()
            {
                // Given
                var writerMock = new Mock<IMessageWriter>();

                var m1 = new RunMessage("Run message 1");
                var m2 = new RunMessage("Run message 2");
                var messages = new IRequestMessage[] {m1, m2};
                var client = new SocketClient(null, writerMock.Object);

                // When
                await client.SendAsync(messages);

                // Then
                writerMock.Verify(x => x.Write(m1), Times.Once);
                writerMock.Verify(x => x.Write(m2), Times.Once);
                writerMock.Verify(x => x.FlushAsync(), Times.Once);
            }

            [Fact]
            public async Task ShouldCloseConnectionIfErrorAsync()
            {
                // Given
                var connMock = new Mock<ITcpSocketClient>();

                var client = new SocketClient(null, null, connMock.Object);
                client.SetOpened();

                // When
                var exception = await ExceptionAsync(() =>
                    client.SendAsync(null /*cause null point exception in send method*/));

                // Then
                exception.Should().BeOfType<NullReferenceException>();
                connMock.Verify(x => x.DisconnectAsync(), Times.Once);
            }
        }

        public class ReceiveOneMethod
        {
            [Fact]
            public async Task ShouldReadMessageAsync()
            {
                // Given
                var readerMock = new Mock<IMessageReader>();

                var client = new SocketClient(readerMock.Object, null);
                var pipeline = new Mock<IResponsePipeline>();

                // When
                await client.ReceiveOneAsync(pipeline.Object);

                // Then
                readerMock.Verify(x => x.ReadAsync(pipeline.Object), Times.Once);
            }

            [Fact]
            public async Task ShouldCloseConnectionIfErrorAsync()
            {
                // Given
                var readerMock = new Mock<IMessageReader>();
                var connMock = new Mock<ITcpSocketClient>();

                var client = new SocketClient(readerMock.Object, null, connMock.Object);
                client.SetOpened();

                var pipeline = new Mock<IResponsePipeline>();
                // Throw error when try to read
                readerMock.Setup(x => x.ReadAsync(pipeline.Object)).Throws<IOException>();

                // When
                var exception = await ExceptionAsync(() => client.ReceiveOneAsync(pipeline.Object));

                // Then
                exception.Should().BeOfType<IOException>();
                connMock.Verify(x => x.DisconnectAsync(), Times.Once);
            }

            [Fact]
            public async Task ShouldCloseConnectionOnProtocolException()
            {
                // Given
                var readerMock = new Mock<IMessageReader>();
                var connMock = new Mock<ITcpSocketClient>();

                var client = new SocketClient(readerMock.Object, null, connMock.Object);
                client.SetOpened();

                var pipeline = new Mock<IResponsePipeline>();
                pipeline.Setup(x => x.AssertNoProtocolViolation()).Throws(new ProtocolException("some protocol error"));

                // When
                var exception = await ExceptionAsync(() => client.ReceiveOneAsync(pipeline.Object));

                // Then
                exception.Should().BeOfType<ProtocolException>();
                readerMock.Verify(x => x.ReadAsync(pipeline.Object), Times.Once);
                connMock.Verify(x => x.DisconnectAsync(), Times.Once);
            }

            [Fact]
            public async Task ShouldCloseConnectionWhenReaderThrowsException()
            {
                // Given
                var readerMock = new Mock<IMessageReader>();
                var connMock = new Mock<ITcpSocketClient>();
                var pipeline = new Mock<IResponsePipeline>();

                var client = new SocketClient(readerMock.Object, null, connMock.Object);
                client.SetOpened();

                readerMock.Setup(x => x.ReadAsync(pipeline.Object)).ThrowsAsync(new DatabaseException());

                // When
                var exception = await ExceptionAsync(() => client.ReceiveOneAsync(pipeline.Object));

                // Then
                exception.Should().BeOfType<DatabaseException>();
                readerMock.Verify(x => x.ReadAsync(pipeline.Object), Times.Once);
                connMock.Verify(x => x.DisconnectAsync(), Times.Once);
            }
        }

        public class DisposeAndStopMethods
        {
            [Fact]
            public async Task ShouldCallDisconnectAsyncOnTheTcpSocketClientWhenStoppedAsync()
            {
                var connMock = new Mock<ITcpSocketClient>();
                var client = new SocketClient(null, null, connMock.Object);
                client.SetOpened();

                // When
                await client.StopAsync();

                // Then
                connMock.Verify(x => x.DisconnectAsync(), Times.Once);
                client.IsOpen.Should().BeFalse();
            }
        }
    }
}