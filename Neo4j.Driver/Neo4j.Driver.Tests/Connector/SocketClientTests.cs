// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;
using Xunit;

namespace Neo4j.Driver.Tests;

public class SocketClientTests
{
    private static BoltProtocolVersion Version = BoltProtocolVersion.V30;
    private static Uri FakeUri => new("bolt://foo.bar:7878");
    private static BufferSettings DefaultBuffers => new(Config.Default);
    private static SocketSettings SocketSetting => new(
            Mock.Of<IHostResolver>(),
            new EncryptionManager(false, TrustManager.CreateInsecure()));

    private static SocketClient Client(Mock<IConnectionIoFactory> factory)
    {
        return new SocketClient(FakeUri, SocketSetting, DefaultBuffers, Mock.Of<ILogger>(), factory.Object);
    }
    
    private static (Mock<ITcpSocketClient>, Mock<IConnectionIoFactory>) CreatMockIoFactory(
        Action<Mock<ITcpSocketClient>> configureMock,
        Action<Mock<IConnectionIoFactory>> configureFactory)
    {
        var connMock = new Mock<ITcpSocketClient>();
        configureMock?.Invoke(connMock);
        
        var mockIoFactory = new Mock<IConnectionIoFactory>();
        mockIoFactory
            .Setup(x => x.TcpSocketClient(It.IsAny<SocketSettings>(), It.IsAny<ILogger>()))
            .Returns(connMock.Object);

        configureFactory?.Invoke(mockIoFactory);

        return (connMock, mockIoFactory);
    }

    private static void SetupBuild(Mock<IConnectionIoFactory> factory, MessageFormat format = null, 
        ChunkWriter writer = null, MemoryStream stream = null,
        IMessageReader messageReader = null, IMessageWriter messageWriter = null)
    {
        factory.Setup(
                y => y.Build(
                    It.IsAny<ITcpSocketClient>(),
                    It.IsAny<BufferSettings>(),
                    It.IsAny<ILogger>(),
                    It.IsAny<BoltProtocolVersion>()))
            .Returns(
                (format ?? new MessageFormat(Version), 
                    writer ?? new ChunkWriter(
                        new MemoryStream(),
                        DefaultBuffers,
                        Mock.Of<ILogger>()),
                    stream ?? new MemoryStream(), 
                    messageReader ?? Mock.Of<IMessageReader>(), 
                    messageWriter ?? Mock.Of<IMessageWriter>()));
    }

    public class ConnectMethod
    {
        [Fact]
        public async void ShouldThrowIOExceptionIfFailedToReadOnHandshakeAsync()
        {
            var (_, mockIoFactory) = CreatMockIoFactory(
                connMock =>
                {
                    TcpSocketClientTestSetup.CreateReadStreamMock(connMock);
                    TcpSocketClientTestSetup.CreateWriteStreamMock(connMock);
                }, null);

            var client = Client(mockIoFactory);

            var ex = await Record.ExceptionAsync(
                () => client.ConnectAsync(new Dictionary<string, string>(), CancellationToken.None));

            ex.Should().NotBeNull().And.BeOfType<IOException>();
        }
    }

    public class StartMethod
    {
        [Fact]
        public async Task ShouldConnectServerAsync()
        {
            var version = new BoltProtocolVersion(4, 1);

            var (connMock, factory) = CreatMockIoFactory(
                x =>
                {
                    TcpSocketClientTestSetup.CreateReadStreamMock(x,
                        PackStreamBitConverter.GetBytes(version.PackToInt()));
                    TcpSocketClientTestSetup.CreateWriteStreamMock(x);
                }, null);

            var client = Client(factory);

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

            var (_, factory) = CreatMockIoFactory(null, factoryMock =>
                SetupBuild(factoryMock, messageWriter: writerMock.Object));

            var m1 = new RunWithMetadataMessage(Version, new Query("Run message 1"));
            var m2 = new RunWithMetadataMessage(Version, new Query("Run message 2"));
            var messages = new IRequestMessage[] { m1, m2 };

            var client = Client(factory);

            // When
            await client.SendAsync(messages);

            // Then
            writerMock.Verify(x => x.Write(m1, It.IsAny<PackStreamWriter>()), Times.Once);
            writerMock.Verify(x => x.Write(m2, It.IsAny<PackStreamWriter>()), Times.Once);
            writerMock.Verify(x => x.FlushAsync(), Times.Once);
        }


        [Fact]
        public async Task ShouldCloseConnectionIfErrorAsync()
        {
            var (tcpConn, factory) = CreatMockIoFactory(null, null);
            
            // Given
            var client = Client(factory);
            client.SetOpened();

            // When
            var exception = await Record.ExceptionAsync(() => client.SendAsync(null));

            // Then
            exception.Should().BeOfType<NullReferenceException>();
            tcpConn.Verify(x => x.DisposeAsync(), Times.Once);
        }
    }

    public class ReceiveOneMethod
    {
        [Fact]
        public async Task ShouldReadMessageAsync()
        {
            // Given
            var readerMock = new Mock<IMessageReader>();
            
            var (_, factory) = CreatMockIoFactory(null, x => SetupBuild(x, messageReader:readerMock.Object));

            var client = Client(factory);

            var pipeline = new Mock<IResponsePipeline>();

            // When
            await client.ReceiveOneAsync(pipeline.Object);

            // Then
            readerMock.Verify(x => x.ReadAsync(pipeline.Object, It.IsAny<PackStreamReader>()), Times.Once);
        }


        [Fact]
        public async Task ShouldCloseConnectionIfErrorAsync()
        {
            var psr = new PackStreamReader(new MemoryStream(), new MessageFormat(Version), new ByteBuffers());

            // Given
            var readerMock = new Mock<IMessageReader>();
            
            var pipeline = new Mock<IResponsePipeline>();
            // Throw error when try to read

            var (connMock, factory) = CreatMockIoFactory(null, x => SetupBuild(x, messageReader:readerMock.Object));
            readerMock.Setup(x => x.ReadAsync(pipeline.Object, psr)).Throws<IOException>();

            var client = Client(factory);
            client.SetOpened();

            // When
            var exception = await Record.ExceptionAsync(() => client.ReceiveOneAsync(pipeline.Object));

            // Then
            exception.Should().BeOfType<IOException>();
            connMock.Verify(x => x.DisposeAsync(), Times.Once);
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
