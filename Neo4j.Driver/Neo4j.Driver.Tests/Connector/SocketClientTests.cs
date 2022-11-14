// Copyright (c) "Neo4j"
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.MessageHandling;
using Xunit;

namespace Neo4j.Driver.Tests;

public class SocketClientTests
{
    private static Uri FakeUri => new Uri("bolt://foo.bar:7878");

    private static (Mock<IConnectionIoFactory>, Mock<ITcpSocketClient>) CreatMockIoFactory(
        Action<Mock<ITcpSocketClient>> configureMock, Action<Mock<IConnectionIoFactory>> configureFactory = null)
    {
        var connMock = new Mock<ITcpSocketClient>();
        configureMock?.Invoke(connMock);
        var mockIoFactory = new Mock<IConnectionIoFactory>();
        mockIoFactory
            .Setup(x => x.TcpSocketClient(It.IsAny<SocketSettings>(), It.IsAny<ILogger>()))
            .Returns(connMock.Object);
        
        configureFactory?.Invoke(mockIoFactory);
        
        return (mockIoFactory, connMock);
    }
    
    public class ConnectMethod
    {
        [Fact]
        public async void ShouldThrowIOExceptionIfFailedToReadOnHandshakeAsync()
        {
            var bufferSettings = new BufferSettings(Config.Default);

            var (mockIoFactory, _) = CreatMockIoFactory(connMock =>
            {
                TcpSocketClientTestSetup.CreateReadStreamMock(connMock);
                TcpSocketClientTestSetup.CreateWriteStreamMock(connMock);
            });

            var client = new SocketClient(FakeUri, null, bufferSettings, Mock.Of<ILogger>(), mockIoFactory.Object);

            var ex = await Record.ExceptionAsync(() => 
                client.ConnectAsync(new Dictionary<string, string>(), CancellationToken.None));

            ex.Should().NotBeNull().And.BeOfType<IOException>();
        }

    }

    public class StartMethod
    {
        [Fact]
        public async Task ShouldConnectServerAsync()
        {
            var bufferSettings = new BufferSettings(Config.Default);
            var version = new BoltProtocolVersion(4, 1);

            var (factory, connMock) = CreatMockIoFactory(x =>
            {
                TcpSocketClientTestSetup.CreateReadStreamMock(x,
                    PackStreamBitConverter.GetBytes(version.PackToInt()));
                TcpSocketClientTestSetup.CreateWriteStreamMock(x);
            });
            
            var client = new SocketClient(FakeUri, null, bufferSettings, Mock.Of<ILogger>(), factory.Object);

            await client.ConnectAsync(new Dictionary<string, string>());

            // Then
            connMock.Verify(x => x.ConnectAsync(FakeUri, CancellationToken.None), Times.Once);
        }
    }

    public class SendMethod
    {
        [Fact]
        public async Task ShouldSendAllMessagesAsync()
        {
            // Given
            var writerMock = new Mock<IMessageWriter>();

            var (factory, _) = CreatMockIoFactory(null, factoryMock =>
            {
                factoryMock.Setup(y => y.Build(
                        It.IsAny<ITcpSocketClient>(), It.IsAny<BufferSettings>(), It.IsAny<ILogger>(),
                        It.IsAny<BoltProtocolVersion>()))
                    .Returns((new MessageFormat(BoltProtocolVersion.V30),
                        new ChunkWriter(new MemoryStream(), new BufferSettings(Config.Default), Mock.Of<ILogger>()),
                        new MemoryStream(), Mock.Of<IMessageReader>(), writerMock.Object));
            });
            
            
            var m1 = new RunWithMetadataMessage(BoltProtocolVersion.V30, new Query("Run message 1"));
            var m2 = new RunWithMetadataMessage(BoltProtocolVersion.V30, new Query("Run message 2"));
            var messages = new IRequestMessage[] {m1, m2};
            
            var client = new SocketClient(null, SocketSetting(), new BufferSettings(Config.Default), Mock.Of<ILogger>(), 
                factory.Object);

            // When
            await client.SendAsync(messages);

            // Then
            writerMock.Verify(x => x.Write(m1, It.IsAny<PackStreamWriter>()), Times.Once);
            writerMock.Verify(x => x.Write(m2, It.IsAny<PackStreamWriter>()), Times.Once);
            writerMock.Verify(x => x.FlushAsync(), Times.Once);
        }

        private static SocketSettings SocketSetting()
        {
            return new SocketSettings(Mock.Of<IHostResolver>(), new EncryptionManager(false, TrustManager.CreateInsecure()));
        }

        [Fact]
        public async Task ShouldCloseConnectionIfErrorAsync()
        {
            // Given
            var client = new SocketClient(FakeUri, new SocketSettings(), new BufferSettings(Config.Default));
            client.SetOpened();

            // When
            var exception = await Record.ExceptionAsync(() => client.SendAsync(null));

            // Then
            exception.Should().BeOfType<NullReferenceException>();
            connection.Verify(x => x.DisposeAsync(), Times.Once);
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
            
            var client = new SocketClient(null, null, null, socketClient: connMock.Object);
            client.SetOpened();

            // When
            await client.DisposeAsync();

            // Then
            connMock.Verify(x => x.DisposeAsync(), Times.Once);
            client.IsOpen.Should().BeFalse();
        }
    }
}