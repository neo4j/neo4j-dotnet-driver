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
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.Internal.MessageHandling;

namespace Neo4j.Driver.Internal.Connector;

internal sealed class SocketClient : ISocketClient
{
    private const string MessagePattern = "C: {0}";
    private readonly Uri _uri;
    private readonly BufferSettings _bufferSettings;
    private readonly ByteBuffers _readerBuffers = new();

    private readonly ITcpSocketClient _tcpSocketClient;

    private int _closedMarker = -1;

    private readonly ILogger _logger;
    private readonly IConnectionIoFactory _connectionIoFactory;

    private MessageFormat _format;
    private MemoryStream _readBufferStream;
    private IMessageReader _messageReader;
    private IMessageWriter _messageWriter;
    private ChunkWriter _chunkWriter;

    public SocketClient(Uri uri, SocketSettings socketSettings, BufferSettings bufferSettings, ILogger logger, 
        IConnectionIoFactory connectionIoFactory)
    {
        _uri = uri;
        _logger = logger;
        _connectionIoFactory = connectionIoFactory ?? new SocketClientIoFactory();
        _bufferSettings = bufferSettings;
        _tcpSocketClient = _connectionIoFactory.TcpSocketClient(socketSettings, _logger);
    }

    public bool IsOpen => _closedMarker == 0;

    public async Task ConnectAsync(IDictionary<string, string> routingContext, 
        CancellationToken cancellationToken = default)
    {
        await _tcpSocketClient.ConnectAsync(_uri, cancellationToken).ConfigureAwait(false);

        _logger?.Debug($"~~ [CONNECT] {_uri}");
        Version = await DoHandshakeAsync(cancellationToken).ConfigureAwait(false);

        (_format, _chunkWriter, _readBufferStream, _messageReader, _messageWriter) =
            _connectionIoFactory.Build(_tcpSocketClient, _bufferSettings, _logger, Version);
        
        SetOpened();
    }

    public BoltProtocolVersion Version { get; private set; }

    public async Task SendAsync(IEnumerable<IRequestMessage> messages)
    {
        try
        {
            foreach (var message in messages)
            {
                _messageWriter.Write(message, new PackStreamWriter(_format, _chunkWriter));
                _logger?.Debug(MessagePattern, message);
            }

            await _chunkWriter.SendAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.Warn(ex, $"Unable to send message to server {_uri}, connection will be terminated.");
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
            var packStreamReader = new PackStreamReader(_readBufferStream, _format,  _readerBuffers);
            await _messageReader.ReadAsync(responsePipeline, packStreamReader).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, $"Unable to read message from server {_uri}, connection will be terminated.");
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
            _logger?.Warn(exc, "A bolt protocol error has occurred with server {0}, connection will be terminated.",
                _uri.ToString());
            await DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Internal for testing, not for use outside of SocketClient.
    /// </summary>
    internal void SetOpened()
    {
        Interlocked.CompareExchange(ref _closedMarker, 0, -1);
    }

    private async Task<BoltProtocolVersion> DoHandshakeAsync(CancellationToken cancellationToken = default)
    {
        var data = BoltProtocolFactory.PackSupportedVersions();
        await _tcpSocketClient.WriterStream.WriteAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);
        await _tcpSocketClient.WriterStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        
        _logger?.Debug("C: [HANDSHAKE] {0}", data.ToHexString());
        
        var responseBytes = new byte[4];
        var read = await _tcpSocketClient.ReaderStream
            .ReadAsync(responseBytes, 0, responseBytes.Length, cancellationToken)
            .ConfigureAwait(false);

        if (read < responseBytes.Length)
            throw new IOException($"Unexpected end of stream when performing handshake, read returned {read}");
            
        var agreedVersion = BoltProtocolFactory.UnpackAgreedVersion(responseBytes);
        
        _logger?.Debug("S: [HANDSHAKE] {0}.{1}", agreedVersion.MajorVersion, agreedVersion.MinorVersion);
        
        return agreedVersion;
    }

    public void SetReadTimeoutInSeconds(int seconds)
    {
        _tcpSocketClient.ReaderStream.ReadTimeout = seconds * 1000;
    }


    public void UseUtcEncoded()
    {
        _format.UseUtcEncoder();
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _closedMarker, 1, 0) == 0)
        {
            _readBufferStream.Dispose();
            await _tcpSocketClient.DisposeAsync().ConfigureAwait(false);
        }
    }
}