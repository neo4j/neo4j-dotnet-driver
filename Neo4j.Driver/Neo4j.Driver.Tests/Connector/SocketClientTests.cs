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

using System;
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
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver.Tests.TestUtil;
using Xunit;

namespace Neo4j.Driver.Tests.Connector;

public class SocketClientTests
{
    private static readonly BoltProtocolVersion Version = BoltProtocolVersion.V3_0;
    private static Uri FakeUri => new("bolt://foo.bar:7878");

    private static SocketClient NewClient(
        Mock<IConnectionIoFactory> factory = null,
        Mock<IPackStreamFactory> mockPackstreamFactory = null,
        Mock<IBoltHandshaker> boltHandshaker = null)
    {
        factory ??= CreateMockIoFactory(null, null).Item2;
        mockPackstreamFactory ??= new Mock<IPackStreamFactory>();

        if (boltHandshaker == null)
        {
            boltHandshaker = new Mock<IBoltHandshaker>();
            boltHandshaker
                .Setup(
                    x => x.DoHandshakeAsync(
                        It.IsAny<ITcpSocketClient>(),
                        It.IsAny<ILogger>(),
                        It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(Version));
        }

        return new SocketClient(
            FakeUri,
            TestDriverContext.With(FakeUri),
            Mock.Of<ILogger>(),
            factory.Object,
            mockPackstreamFactory.Object,
            boltHandshaker.Object);
    }

    private static (Mock<ITcpSocketClient>, Mock<IConnectionIoFactory>) CreateMockIoFactory(
        Action<Mock<ITcpSocketClient>> configureMock,
        Action<Mock<IConnectionIoFactory>> configureFactory)
    {
        var connMock = new Mock<ITcpSocketClient>();
        configureMock?.Invoke(connMock);

        var mockIoFactory = new Mock<IConnectionIoFactory>();
        mockIoFactory
            .Setup(x => x.TcpSocketClient(It.IsAny<DriverContext>(), It.IsAny<ILogger>()))
            .Returns(connMock.Object);

        configureFactory?.Invoke(mockIoFactory);

        return (connMock, mockIoFactory);
    }

    private static void SetupFactory(
        Mock<IConnectionIoFactory> factory,
        MessageFormat format = null,
        IChunkWriter writer = null,
        IMessageReader messageReader = null,
        IMessageWriter messageWriter = null)
    {
        var fmt = format ?? new MessageFormat(Version, TestDriverContext.MockContext);
        var cw = writer ??
            new ChunkWriter(
                new MemoryStream(),
                TestDriverContext.MockContext,
                Mock.Of<ILogger>());

        var mr = messageReader ?? Mock.Of<IMessageReader>();
        var mw = messageWriter ?? Mock.Of<IMessageWriter>();

        factory
            .Setup(x => x.MessageReader(It.IsAny<ITcpSocketClient>(), It.IsAny<DriverContext>(), It.IsAny<ILogger>()))
            .Returns(mr);

        factory
            .Setup(x => x.Writers(It.IsAny<ITcpSocketClient>(), It.IsAny<DriverContext>(), It.IsAny<ILogger>()))
            .Returns((cw, mw));

        factory.Setup(x => x.Format(Version, TestDriverContext.MockContext)).Returns(fmt);
    }

    public class ConnectMethod
    {
        [Fact]
        public async void ShouldNotCatchHandshakeFailuresOrConstructIoTypes()
        {
            var (_, io) = CreateMockIoFactory(null, null);
            var mockHandshaker = new Mock<IBoltHandshaker>();
            var exception = new IOException();
            mockHandshaker
                .Setup(
                    x => x.DoHandshakeAsync(
                        It.IsAny<ITcpSocketClient>(),
                        It.IsAny<ILogger>(),
                        It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            var client = NewClient(io, null, mockHandshaker);

            var ex = await Record.ExceptionAsync(() => client.ConnectAsync(CancellationToken.None));

            mockHandshaker.Verify(
                x => x.DoHandshakeAsync(
                    It.IsAny<ITcpSocketClient>(),
                    It.IsAny<ILogger>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            ex.Should().NotBeNull().And.Be(exception);

            io.Verify(x => x.Format(It.IsAny<BoltProtocolVersion>(), It.IsAny<DriverContext>()), Times.Never);
        }
    }

    public class StartMethod
    {
        [Fact]
        public async Task ShouldConnectServerAsync()
        {
            var version = new BoltProtocolVersion(4, 1);

            var (connMock, factory) = CreateMockIoFactory(
                x =>
                {
                    TcpSocketClientTestSetup.CreateReadStreamMock(
                        x,
                        PackStreamBitConverter.GetBytes(version.PackToInt()));

                    TcpSocketClientTestSetup.CreateWriteStreamMock(x);
                },
                null);

            var client = NewClient(factory);

            await client.ConnectAsync();

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
            var chunkerMock = new Mock<IChunkWriter>();

            var (_, factory) = CreateMockIoFactory(
                null,
                x => SetupFactory(x, writer: chunkerMock.Object, messageWriter: writerMock.Object));

            var m1 = new RunWithMetadataMessage(Version, new Query("Run message 1"));
            var m2 = new RunWithMetadataMessage(Version, new Query("Run message 2"));
            var messages = new IRequestMessage[] { m1, m2 };

            var client = NewClient(factory, new Mock<IPackStreamFactory>());
            await client.ConnectAsync();

            // When
            await client.SendAsync(messages);

            // Then
            writerMock.Verify(x => x.Write(m1, It.IsAny<PackStreamWriter>()), Times.Once);
            writerMock.Verify(x => x.Write(m2, It.IsAny<PackStreamWriter>()), Times.Once);

            chunkerMock.Verify(x => x.SendAsync(), Times.Once);
        }

        [Fact]
        public async Task ShouldCloseConnectionIfErrorAsync()
        {
            var (connMock, factory) = CreateMockIoFactory(null, null);

            // Given
            var client = NewClient(factory);
            await client.ConnectAsync();

            // When
            var exception = await Record.ExceptionAsync(() => client.SendAsync(null));

            // Then
            exception.Should().BeOfType<NullReferenceException>();
            connMock.Verify(x => x.Dispose(), Times.Once);
        }
    }

    public class ReceiveOneMethod
    {
        [Fact]
        public async Task ShouldReadMessageAsync()
        {
            // Given
            var pipeline = new Mock<IResponsePipeline>();
            var readerMock = new Mock<IMessageReader>();

            var (_, factory) = CreateMockIoFactory(null, x => SetupFactory(x, messageReader: readerMock.Object));

            var client = NewClient(factory);
            await client.ConnectAsync();

            // When
            await client.ReceiveOneAsync(pipeline.Object);

            // Then
            readerMock.Verify(x => x.ReadAsync(pipeline.Object, It.IsAny<MessageFormat>()), Times.Once);
        }

        [Fact]
        public async Task ShouldCloseConnectionIfErrorAsync()
        {
            // Given
            var mockPipeline = new Mock<IResponsePipeline>();

            var readerMock = new Mock<IMessageReader>();
            readerMock
                .Setup(x => x.ReadAsync(mockPipeline.Object, It.IsAny<MessageFormat>()))
                .Throws<IOException>();

            var (connMock, factory) = CreateMockIoFactory(
                null,
                x => SetupFactory(x, messageReader: readerMock.Object));

            var client = NewClient(factory);
            await client.ConnectAsync();

            // When
            var exception = await Record.ExceptionAsync(() => client.ReceiveOneAsync(mockPipeline.Object));

            // Then
            exception.Should().BeOfType<IOException>();
            readerMock.Verify(x => x.ReadAsync(mockPipeline.Object, It.IsAny<MessageFormat>()), Times.Once);
            connMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task ShouldCloseConnectionOnProtocolException()
        {
            // Given
            var mockPipeline = new Mock<IResponsePipeline>();

            var readerMock = new Mock<IMessageReader>();
            readerMock.Setup(x => x.ReadAsync(mockPipeline.Object, It.IsAny<MessageFormat>()))
                .Throws(new ProtocolException("test"));

            var (connMock, factory) = CreateMockIoFactory(
                null,
                x => SetupFactory(x, messageReader: readerMock.Object));

            var client = NewClient(factory);
            await client.ConnectAsync();

            // When
            var exception = await Record.ExceptionAsync(() => client.ReceiveOneAsync(mockPipeline.Object));

            // Then
            exception.Should().BeOfType<ProtocolException>();
            readerMock.Verify(x => x.ReadAsync(mockPipeline.Object, It.IsAny<MessageFormat>()), Times.Once);
            connMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task ShouldCloseConnectionWhenReaderThrowsException()
        {
            // Given
            var readerMock = new Mock<IMessageReader>();
            var pipeline = new Mock<IResponsePipeline>();

            var (connMock, factory) = CreateMockIoFactory(
                null,
                x => SetupFactory(x, messageReader: readerMock.Object));

            var client = NewClient(factory);
            await client.ConnectAsync();

            readerMock.Setup(x => x.ReadAsync(pipeline.Object, It.IsAny<MessageFormat>()))
                .ThrowsAsync(new DatabaseException());

            // When
            var exception = await Record.ExceptionAsync(() => client.ReceiveOneAsync(pipeline.Object));

            // Then
            exception.Should().BeOfType<DatabaseException>();
            readerMock.Verify(x => x.ReadAsync(pipeline.Object, It.IsAny<MessageFormat>()), Times.Once);
            connMock.Verify(x => x.Dispose(), Times.Once);
        }
    }

    public class DisposeAndStopMethods
    {
        [Fact]
        public async Task ShouldCallDisconnectAsyncOnTheTcpSocketClientWhenStoppedAsync()
        {
            var (connMock, factory) = CreateMockIoFactory(null, null);

            var client = NewClient(factory);
            await client.ConnectAsync();

            // When
            await client.DisposeAsync();

            // Then
            connMock.Verify(x => x.Dispose(), Times.Once);
            client.IsOpen.Should().BeFalse();
        }
    }
}
