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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.MessageHandling;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.Protocol;

namespace Neo4j.Driver.Internal.Connector;

internal sealed class SocketClient : ISocketClient
{
    private const string MessagePattern = "C: {0}";
    private readonly IConnectionIoFactory _connectionIoFactory;
    private readonly IBoltHandshaker _handshaker;
    private readonly INeo4jLogger _neo4JLogger;
    private readonly IPackStreamFactory _packstreamFactory;
    private readonly ITcpSocketClient _tcpSocketClient;

    private readonly Uri _uri;
    private IChunkWriter _chunkWriter;

    private int _closedMarker = -1;

    private MessageFormat _format;
    private IMessageReader _messageReader;
    private IMessageWriter _messageWriter;

    public SocketClient(
        Uri uri,
        DriverContext context,
        INeo4jLogger neo4JLogger,
        IConnectionIoFactory connectionIoFactory,
        IPackStreamFactory packstreamFactory = null,
        IBoltHandshaker boltHandshaker = null)
    {
        Context = context;
        Version = BoltProtocolVersion.Unknown;
        _uri = uri;
        _neo4JLogger = neo4JLogger;

        _packstreamFactory = packstreamFactory ?? PackStreamFactory.Default;
        _connectionIoFactory = connectionIoFactory ?? SocketClientIoFactory.Default;
        _handshaker = boltHandshaker ?? BoltHandshaker.Default;

        _tcpSocketClient = _connectionIoFactory.TcpSocketClient(context, _neo4JLogger);
    }

    public DriverContext Context { get; }

    public bool IsOpen => _closedMarker == 0;

    public async Task ConnectAsync(
        CancellationToken cancellationToken = default)
    {
        await _tcpSocketClient.ConnectAsync(_uri, cancellationToken).ConfigureAwait(false);

        _neo4JLogger.Debug($"~~ [CONNECT] {_uri}");

        Version = await _handshaker
            .DoHandshakeAsync(_tcpSocketClient, _neo4JLogger, cancellationToken)
            .ConfigureAwait(false);

        _format = _connectionIoFactory.Format(Version, Context);
        _messageReader = _connectionIoFactory.MessageReader(_tcpSocketClient, Context, _neo4JLogger);
        (_chunkWriter, _messageWriter) = _connectionIoFactory.Writers(_tcpSocketClient, Context, _neo4JLogger);
        SetOpened();
    }

    public BoltProtocolVersion Version { get; private set; }

    public async Task SendAsync(IEnumerable<IRequestMessage> messages)
    {
        var writer = _packstreamFactory.BuildWriter(_format, _chunkWriter);
        try
        {
            foreach (var message in messages)
            {
                _messageWriter.Write(message, writer);
                _neo4JLogger.Debug(MessagePattern, message);
            }

            await _chunkWriter.SendAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _neo4JLogger.Warn(ex, $"Unable to send message to server {_uri}, connection will be terminated. ({ex.Message})");
            await DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public async Task ReceiveAsync(IResponsePipeline responsePipeline)
    {
        while (!responsePipeline.HasNoPendingMessages)
        {
            await ReceiveOneAsync(responsePipeline).ConfigureAwait(false);
        }
    }

    public async Task ReceiveOneAsync(IResponsePipeline responsePipeline)
    {
        try
        {
            await _messageReader.ReadAsync(responsePipeline, _format).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _neo4JLogger.Error(
                ex,
                $"Unable to read message from server {_uri}, connection will be terminated. ({ex.Message})");
            await DisposeAsync().ConfigureAwait(false);
            throw;
        }

        // We force ProtocolException's to be thrown here to shortcut the communication with the server
        try
        {
            responsePipeline.AssertNoProtocolViolation();
        }
        catch (ProtocolException exc)
        {
            _neo4JLogger.Warn(
                exc,
                "A bolt protocol error has occurred with server {0}, connection will be terminated.",
                _uri.ToString());

            await DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public void SetReadTimeoutInSeconds(int seconds)
    {
        var ms = seconds * 1000;
        _messageReader.SetReadTimeoutInMs(ms);
    }

    public void UseUtcEncoded()
    {
        _format.UseUtcEncoder();
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _closedMarker, 1, 0) == 0)
        {
            await (_messageReader?.DisposeAsync() ?? new ValueTask(Task.CompletedTask));
            _tcpSocketClient.Dispose();
        }
    }

    private void SetOpened()
    {
        Interlocked.CompareExchange(ref _closedMarker, 0, -1);
    }
}
