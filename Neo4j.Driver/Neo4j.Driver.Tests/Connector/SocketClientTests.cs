﻿// Copyright (c) 2002-2018 "Neo Technology,"
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
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Result;
using Neo4j.Driver.V1;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.IO;
using Xunit;
using static Neo4j.Driver.Internal.ConnectionSettings;
using static Xunit.Record;

namespace Neo4j.Driver.Tests
{
    public class SocketClientTests
    {
        private static Uri FakeUri => new Uri("bolt://foo.bar:7878");

        public class StartMethod
        {
            [Fact]
            public void ShouldConnectServer()
            {
                var bufferSettings = new BufferSettings(Config.DefaultConfig);

                var connMock = new Mock<ITcpSocketClient>();
                TcpSocketClientTestSetup.CreateReadStreamMock(connMock, new byte[] {0, 0, 0, 1});
                TcpSocketClientTestSetup.CreateWriteStreamMock(connMock);

                var client = new SocketClient(FakeUri, null, bufferSettings, socketClient: connMock.Object);

                client.Start();

                // Then
                connMock.Verify(x=>x.Connect(FakeUri), Times.Once);
            }

            [Fact]
            public async Task ShouldConnectServerAsync()
            {
                var bufferSettings = new BufferSettings(Config.DefaultConfig);

                var connMock = new Mock<ITcpSocketClient>();
                TcpSocketClientTestSetup.CreateReadStreamMock(connMock, new byte[] {0, 0, 0, 1});
                TcpSocketClientTestSetup.CreateWriteStreamMock(connMock);

                var client = new SocketClient(FakeUri, null, bufferSettings, socketClient: connMock.Object);

                await client.StartAsync();

                // Then
                connMock.Verify(x=>x.ConnectAsync(FakeUri), Times.Once);
            }
        }

        public class SendMethod
        {
            [Fact]
            public void ShouldSendAllMessages()
            {
                // Given
                var protocolMock = new Mock<IBoltProtocol>();
                var writerMock = new Mock<IBoltWriter>();
                protocolMock.Setup(x => x.Writer).Returns(writerMock.Object);

                var m1 = new RunMessage("Run message 1");
                var m2 = new RunMessage("Run message 2");
                var messages = new IRequestMessage[] {m1, m2};
                var client = new SocketClient(protocolMock.Object);

                // When
                client.Send(messages);

                // Then
                writerMock.Verify(x=>x.Write(m1), Times.Once);
                writerMock.Verify(x=>x.Write(m2), Times.Once);
                writerMock.Verify(x=>x.Flush(), Times.Once);
            }

            [Fact]
            public async Task ShouldSendAllMessagesAsync()
            {
                // Given
                var protocolMock = new Mock<IBoltProtocol>();
                var writerMock = new Mock<IBoltWriter>();
                protocolMock.Setup(x => x.Writer).Returns(writerMock.Object);

                var m1 = new RunMessage("Run message 1");
                var m2 = new RunMessage("Run message 2");
                var messages = new IRequestMessage[] {m1, m2};
                var client = new SocketClient(protocolMock.Object);

                // When
                await client.SendAsync(messages);

                // Then
                writerMock.Verify(x=>x.Write(m1), Times.Once);
                writerMock.Verify(x=>x.Write(m2), Times.Once);
                writerMock.Verify(x=>x.FlushAsync(), Times.Once);
            }

            [Fact]
            public void ShouldCloseConnectionIfError()
            {
                // Given
                var protocolMock = new Mock<IBoltProtocol>();
                var connMock = new Mock<ITcpSocketClient>();

                var client = new SocketClient(protocolMock.Object, connMock.Object);
                client.SetOpened();

                // When
                var exception = Exception(()=>client.Send(null /*cause null point excpetion in send method*/));

                // Then
                exception.Should().BeOfType<NullReferenceException>();
                connMock.Verify(x=>x.Disconnect(), Times.Once);
            }

            [Fact]
            public async Task ShouldCloseConnectionIfErrorAsync()
            {
                // Given
                var protocolMock = new Mock<IBoltProtocol>();
                var connMock = new Mock<ITcpSocketClient>();

                var client = new SocketClient(protocolMock.Object, connMock.Object);
                client.SetOpened();

                // When
                var exception = await ExceptionAsync(()=>client.SendAsync(null /*cause null point excpetion in send method*/));

                // Then
                exception.Should().BeOfType<NullReferenceException>();
                connMock.Verify(x=>x.DisconnectAsync(), Times.Once);
            }
        }

        public class ReceiveOneMethod
        {
            [Fact]
            public void ShouldReadMessage()
            {
                // Given
                var protocolMock = new Mock<IBoltProtocol>();
                var readerMock = new Mock<IBoltReader>();
                protocolMock.Setup(x => x.Reader).Returns(readerMock.Object);

                var client = new SocketClient(protocolMock.Object);
                var handlerMock = new Mock<IMessageResponseHandler>();

                // When
                client.ReceiveOne(handlerMock.Object);

                // Then
                readerMock.Verify(x=>x.Read(handlerMock.Object), Times.Once);
            }

            [Fact]
            public async Task ShouldReadMessageAsync()
            {
                // Given
                var protocolMock = new Mock<IBoltProtocol>();
                var readerMock = new Mock<IBoltReader>();
                protocolMock.Setup(x => x.Reader).Returns(readerMock.Object);

                var client = new SocketClient(protocolMock.Object);
                var handlerMock = new Mock<IMessageResponseHandler>();

                // When
                await client.ReceiveOneAsync(handlerMock.Object);

                // Then
                readerMock.Verify(x=>x.ReadAsync(handlerMock.Object), Times.Once);
            }

            [Fact]
            public void ShouldCloseConnectionIfError()
            {
                // Given
                var protocolMock = new Mock<IBoltProtocol>();
                var readerMock = new Mock<IBoltReader>();
                protocolMock.Setup(x => x.Reader).Returns(readerMock.Object);
                var connMock = new Mock<ITcpSocketClient>();

                var client = new SocketClient(protocolMock.Object, connMock.Object);
                client.SetOpened();

                var handlerMock = new Mock<IMessageResponseHandler>();
                // Throw error when try to read
                readerMock.Setup(x => x.Read(handlerMock.Object)).Throws<IOException>();

                // When
                var exception = Exception(()=>client.ReceiveOne(handlerMock.Object));

                // Then
                exception.Should().BeOfType<IOException>();
                connMock.Verify(x=>x.Disconnect(), Times.Once);
            }

            [Fact]
            public async Task ShouldCloseConnectionIfErrorAsync()
            {
                // Given
                var protocolMock = new Mock<IBoltProtocol>();
                var readerMock = new Mock<IBoltReader>();
                protocolMock.Setup(x => x.Reader).Returns(readerMock.Object);
                var connMock = new Mock<ITcpSocketClient>();

                var client = new SocketClient(protocolMock.Object, connMock.Object);
                client.SetOpened();

                var handlerMock = new Mock<IMessageResponseHandler>();
                // Throw error when try to read
                readerMock.Setup(x => x.ReadAsync(handlerMock.Object)).Throws<IOException>();

                // When
                var exception = await ExceptionAsync(()=>client.ReceiveOneAsync(handlerMock.Object));

                // Then
                exception.Should().BeOfType<IOException>();
                connMock.Verify(x=>x.DisconnectAsync(), Times.Once);
            }

            [Fact]
            public void ShouldCloseConnectionIfServerError()
            {
                // Given
                var protocolMock = new Mock<IBoltProtocol>();
                var readerMock = new Mock<IBoltReader>();
                protocolMock.Setup(x => x.Reader).Returns(readerMock.Object);
                var connMock = new Mock<ITcpSocketClient>();

                var client = new SocketClient(protocolMock.Object, connMock.Object);
                client.SetOpened();

                var handlerMock = new Mock<IMessageResponseHandler>();
                handlerMock.Setup(x => x.HasProtocolViolationError).Returns(true);
                handlerMock.Setup(x => x.Error).Returns(new DatabaseException());

                // When
                var exception = Exception(()=>client.ReceiveOne(handlerMock.Object));

                // Then
                exception.Should().BeOfType<DatabaseException>();
                readerMock.Verify(x=>x.Read(handlerMock.Object), Times.Once);
                connMock.Verify(x=>x.Disconnect(), Times.Once);
            }

            [Fact]
            public async Task ShouldCloseConnectionIfServerErrorAsync()
            {
                // Given
                var protocolMock = new Mock<IBoltProtocol>();
                var readerMock = new Mock<IBoltReader>();
                protocolMock.Setup(x => x.Reader).Returns(readerMock.Object);
                var connMock = new Mock<ITcpSocketClient>();

                var client = new SocketClient(protocolMock.Object, connMock.Object);
                client.SetOpened();

                var handlerMock = new Mock<IMessageResponseHandler>();
                handlerMock.Setup(x => x.HasProtocolViolationError).Returns(true);
                handlerMock.Setup(x => x.Error).Returns(new DatabaseException());

                // When
                var exception = await ExceptionAsync(()=>client.ReceiveOneAsync(handlerMock.Object));

                // Then
                exception.Should().BeOfType<DatabaseException>();
                readerMock.Verify(x=>x.ReadAsync(handlerMock.Object), Times.Once);
                connMock.Verify(x=>x.DisconnectAsync(), Times.Once);
            }
        }

        public class DisposeAndStopMethods
        {

            [Fact]
            public void ShouldCallDisconnectOnTheTcpSocketClientWhenDisposed()
            {
                var connMock = new Mock<ITcpSocketClient>();
                var client = new SocketClient(null, connMock.Object);
                client.SetOpened();

                // When
                client.Dispose();

                // Then
                connMock.Verify(x=>x.Disconnect(), Times.Once);
                client.IsOpen.Should().BeFalse();
            }

            [Fact]
            public void ShouldCallDisconnectOnTheTcpSocketClientWhenStopped()
            {
                var connMock = new Mock<ITcpSocketClient>();
                var client = new SocketClient(null, connMock.Object);
                client.SetOpened();

                // When
                client.Stop();

                // Then
                connMock.Verify(x=>x.Disconnect(), Times.Once);
                client.IsOpen.Should().BeFalse();
            }

            [Fact]
            public async Task ShouldCallDisconnectAsyncOnTheTcpSocketClientWhenStoppedAsync()
            {
                var connMock = new Mock<ITcpSocketClient>();
                var client = new SocketClient(null, connMock.Object);
                client.SetOpened();

                // When
                await client.StopAsync();

                // Then
                connMock.Verify(x=>x.DisconnectAsync(), Times.Once);
                client.IsOpen.Should().BeFalse();
            }
        }
    }
}
